#nullable enable
using Media.Common.Transactions;
using Media.Database.Models;
using Media.Database.Repositories;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Tests.Repositories;

/// <summary>Notifications (WORKER-29, API-188): made once per recipient, read by them, moved on with an audit entry.</summary>
[TestFixture]
public class NotificationRepositoryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<IAuditMessageRepository> _auditMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;

    [SetUp]
    public void Setup()
    {
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
        _auditMock = new Mock<IAuditMessageRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
    }

    private NotificationRepository CreateRepository() =>
        new(_sqlExecutorMock.Object, _auditMock.Object, () => _unitOfWorkMock.Object, Mock.Of<ILogger<NotificationRepository>>());

    [Test]
    public async Task CreateAsync_Should_NotifyEachRecipientOfTheMessage()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(QueryNotifications.CreateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, long>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, long>>((_, configure, _) => captured = configure)
            .ReturnsAsync(2L);

        var made = await CreateRepository().CreateAsync(41, [4, 9]);

        made.ShouldBe(2);
        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.MessageId].Value.ShouldBe(41L);
        command.Parameters[pn.RecipientPersonIds].Value.ShouldBe(new[] { 4, 9 });
    }

    [Test]
    public async Task CreateAsync_Should_AskNothing_When_NobodyIsTold()
    {
        var made = await CreateRepository().CreateAsync(41, []);

        made.ShouldBe(0);
        _sqlExecutorMock.VerifyNoOtherCalls();
    }

    [Test]
    public void CreateSql_Should_NotifyNobodyTwice()
    {
        QueryNotifications.CreateSql.ShouldContain("ON CONFLICT (\"MessageId\", \"RecipientPersonId\") DO NOTHING");
    }

    [Test]
    public async Task ListOpenAsync_Should_ReadTheFirstPage_When_NoCursor()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        var page = new List<NotificationView>();
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryNotifications.ListOpenByRecipientSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, NotificationView>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, NotificationView>>((_, configure, _) => captured = configure)
            .ReturnsAsync(page);

        var result = await CreateRepository().ListOpenAsync(9, before: null, limit: 20);

        result.ShouldBeSameAs(page);
        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.RecipientPersonId].Value.ShouldBe(9);
        command.Parameters[pn.NotificationId].Value.ShouldBe(DBNull.Value);
    }

    [TestCase(NotificationStatus.Seen, "notification.seen")]
    [TestCase(NotificationStatus.Dismissed, "notification.dismissed")]
    [TestCase(NotificationStatus.Acted, "notification.acted")]
    public async Task SetStatusAsync_Should_RecordTheChange_AboutTheNotification(NotificationStatus status, string kind)
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryNotifications.SetStatusSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, long>>()))
            .ReturnsAsync(77L);

        var changed = await CreateRepository().SetStatusAsync(Guid.NewGuid(), 9, status);

        changed.ShouldBeTrue();
        _auditMock.Verify(a => a.RecordAsync(_unitOfWorkMock.Object, null,
            It.Is<AuditEntry>(e => e.Kind == kind && e.SubjectPersonId == 9 && e.ActorPersonId == 9 && e.NotificationId == 77L)), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task SetStatusAsync_Should_RecordNothing_When_NotTheirsOrAlreadyPast()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryNotifications.SetStatusSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, long>>()))
            .ReturnsAsync((long?)null);

        var changed = await CreateRepository().SetStatusAsync(Guid.NewGuid(), 9, NotificationStatus.Dismissed);

        changed.ShouldBeFalse();
        _auditMock.Verify(a => a.RecordAsync(It.IsAny<IUnitOfWork>(), It.IsAny<int?>(), It.IsAny<AuditEntry>()), Times.Never);
    }

    [Test]
    public async Task SetStatusAsync_Should_RollBack_When_RecordingFails()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryNotifications.SetStatusSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, long>>()))
            .ReturnsAsync(77L);
        _auditMock.Setup(a => a.RecordAsync(_unitOfWorkMock.Object, null, It.IsAny<AuditEntry>())).ThrowsAsync(new InvalidOperationException("down"));

        await Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().SetStatusAsync(Guid.NewGuid(), 9, NotificationStatus.Seen));

        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestCase(NotificationStatus.New)]
    [TestCase(NotificationStatus.Expired)]
    [TestCase(NotificationStatus.Answered)]
    public async Task SetStatusAsync_Should_RefuseWhatOnlyTheSystemSets(NotificationStatus status)
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(() => CreateRepository().SetStatusAsync(Guid.NewGuid(), 9, status));
    }

    [TestCase("member.left", Told.GroupAdmins)]
    [TestCase("member.removed", Told.Subject)]
    [TestCase("member.disabled", Told.Subject)]
    [TestCase("member.made-admin", Told.Subject)]
    [TestCase("member.request-accepted", Told.Subject)]
    [TestCase("invite.queued", Told.Invitee)]
    [TestCase("invite.accepted", Told.GroupAdmins)]
    [TestCase("invite.declined", Told.GroupAdmins)]
    [TestCase("request.queued", Told.GroupAdmins)]
    [TestCase("request.declined", Told.Subject)]
    [TestCase("request.accepted", Told.Nobody)]
    [TestCase("request.ignored", Told.GroupAdmins)]
    [TestCase("group.created", Told.Nobody)]
    [TestCase("notification.dismissed", Told.Nobody)]
    [TestCase(null, Told.Nobody)]
    public void AuditKinds_Should_SayWhoIsTold(string? kind, Told told)
    {
        AuditKinds.WhoIsTold(kind).ShouldBe(told);
    }
}
