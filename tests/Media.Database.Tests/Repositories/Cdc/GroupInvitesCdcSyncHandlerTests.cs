#nullable enable
using Media.Common.Cdc;
using Media.Common.Providers;
using Media.Database.Repositories;
using Media.Database.Repositories.Cdc;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Media.Database.Tests.Repositories.Cdc;

[TestFixture]
public class GroupInvitesCdcSyncHandlerTests
{
    private Mock<ICqlQueryExecutor> _cqlExecutorMock = null!;
    private Mock<IScyllaSessionProvider> _scyllaProviderMock = null!;
    private Dictionary<string, object>? _captured;

    [SetUp]
    public void Setup()
    {
        _cqlExecutorMock = new Mock<ICqlQueryExecutor>();
        _scyllaProviderMock = new Mock<IScyllaSessionProvider>();
        _scyllaProviderMock.Setup(p => p.MaxBatchSize).Returns(100);
        _captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<Dictionary<string, object>>>()))
            .Callback<string, Action<Dictionary<string, object>>>((_, configure) =>
            {
                _captured = new Dictionary<string, object>();
                configure(_captured);
            })
            .Returns(Task.CompletedTask);
    }

    private GroupInvitesCdcSyncHandler CreateHandler() => new(
        _cqlExecutorMock.Object,
        _scyllaProviderMock.Object,
        Mock.Of<ILogger<GroupInvitesCdcSyncHandler>>());

    private static string BuildKey(int id) => JsonSerializer.Serialize(new { GroupInviteId = id });

    private static CdcChangeRecord UpsertRecord(object payload, int id)
    {
        var after = JsonDocument.Parse(JsonSerializer.Serialize(payload)).RootElement;
        return new CdcChangeRecord("cdc.public.GroupInvites", BuildKey(id), after, IsDeleted: false, SourceTimestampMs: 1791431924810, Offset: 0);
    }

    private static readonly Guid Uuid = Guid.Parse("cf92a6ab-1525-428d-82d4-f45c4ea954d6");

    private static object Answered(int id) => new
    {
        GroupInviteId = id,
        GroupInviteUuid = Uuid,
        GroupId = 2,
        InvitedByPersonId = 4,
        EmailAddress = "c@example.test",
        LastName = "Doe",
        Status = 1,
        ExpiresOn = "2026-10-15T04:00:00.000Z",
        AcceptedByPersonId = 6,
        AnsweredOn = "2026-10-09T04:00:00.000Z",
        InsertedOn = "2026-10-08T04:00:00.000Z",
        UpdatedOn = "2026-10-09T04:00:00.000Z",
        __source_ts_ms = 1791431924810L,
        __op = "u"
    };

    private static object Unanswered(int id) => new
    {
        GroupInviteId = id,
        GroupInviteUuid = Uuid,
        GroupId = 2,
        InvitedByPersonId = 4,
        EmailAddress = "c@example.test",
        LastName = "Doe",
        Status = 0,
        ExpiresOn = "2026-10-15T04:00:00.000Z",
        AcceptedByPersonId = (int?)null,
        AnsweredOn = (string?)null,
        InsertedOn = "2026-10-08T04:00:00.000Z",
        UpdatedOn = (string?)null,
        __source_ts_ms = 1791431924810L,
        __op = "c"
    };

    [Test]
    public async Task ApplyAsync_Should_UpsertEveryColumn_When_TheRecordIsNotDeleted()
    {
        await CreateHandler().ApplyAsync(UpsertRecord(Answered(7), 7), CancellationToken.None);

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryGroupInvites.UpsertCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Once);
        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryGroupInvites.DeleteCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Never);
        _captured.ShouldNotBeNull();
        _captured!["@GROUPINVITEID"].ShouldBe(7);
        _captured["@GROUPINVITEUUID"].ShouldBe(Uuid);
        _captured["@GROUPID"].ShouldBe(2);
        _captured["@INVITEDBYPERSONID"].ShouldBe(4);
        _captured["@EMAILADDRESS"].ShouldBe("c@example.test");
        _captured["@LASTNAME"].ShouldBe("Doe");
        _captured["@STATUS"].ShouldBe(1);
        _captured["@EXPIRESON"].ShouldBe(DateTimeOffset.Parse("2026-10-15T04:00:00.000Z"));
        _captured["@ACCEPTEDBYPERSONID"].ShouldBe(6);
        _captured["@ANSWEREDON"].ShouldBe(DateTimeOffset.Parse("2026-10-09T04:00:00.000Z"));
    }

    [Test]
    public async Task ApplyAsync_Should_PassNulls_When_TheNullableColumnsAreJsonNull()
    {
        await CreateHandler().ApplyAsync(UpsertRecord(Unanswered(8), 8), CancellationToken.None);

        _captured.ShouldNotBeNull();
        _captured!["@ACCEPTEDBYPERSONID"].ShouldBeNull();
        _captured["@ANSWEREDON"].ShouldBeNull();
        _captured["@UPDATEDON"].ShouldBeNull();
    }

    [Test]
    public async Task ApplyAsync_Should_Delete_When_TheRecordIsDeleted()
    {
        var record = new CdcChangeRecord("cdc.public.GroupInvites", BuildKey(3), After: null, IsDeleted: true, SourceTimestampMs: 0, Offset: 1);

        await CreateHandler().ApplyAsync(record, CancellationToken.None);

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryGroupInvites.UpsertCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Never);
        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryGroupInvites.DeleteCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Once);
        _captured!["@GROUPINVITEID"].ShouldBe(3);
    }

    [Test]
    public async Task ApplyAsync_Should_Rethrow_When_ScyllaFails()
    {
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryGroupInvites.UpsertCql, It.IsAny<Action<Dictionary<string, object>>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        await Should.ThrowAsync<InvalidOperationException>(() => CreateHandler().ApplyAsync(UpsertRecord(Answered(1), 1), CancellationToken.None));
    }

    [Test]
    public void Topics_Should_Contain_Only_CdcPublicGroupInvites()
    {
        CreateHandler().Topics.ShouldBe(["cdc.public.GroupInvites"]);
    }
}
