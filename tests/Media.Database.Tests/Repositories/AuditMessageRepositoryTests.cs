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
using System.Threading.Tasks;
#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Tests.Repositories;

/// <summary>The audit (DATABASE-63): an entry recorded in the change's own transaction, and a group's history read back.</summary>
[TestFixture]
public class AuditMessageRepositoryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;

    [SetUp]
    public void Setup()
    {
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
    }

    private AuditMessageRepository CreateRepository() => new(_sqlExecutorMock.Object, Mock.Of<ILogger<AuditMessageRepository>>());

    [Test]
    public async Task RecordAsync_Should_WriteTheEntry_InTheGivenTransaction_WithItsValues()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryAuditMessages.RecordSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, long>>()))
            .Callback<IUnitOfWork, string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, long>>((_, _, configure, _) => captured = configure)
            .ReturnsAsync(41L);

        var id = await CreateRepository().RecordAsync(_unitOfWorkMock.Object, 5, new AuditEntry("member.left", SubjectPersonId: 9, ActorPersonId: 9));

        id.ShouldBe(41L);
        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.Kind].Value.ShouldBe("member.left");
        command.Parameters[pn.GroupId].Value.ShouldBe(5);
        command.Parameters[pn.SubjectPersonId].Value.ShouldBe(9);
        command.Parameters[pn.ActorPersonId].Value.ShouldBe(9);
        command.Parameters[pn.Parameters].Value.ShouldBe(DBNull.Value);
    }

    [Test]
    public async Task RecordAsync_Should_RecordTheSystem_AsNoActor()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryAuditMessages.RecordSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, long>>()))
            .Callback<IUnitOfWork, string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, long>>((_, _, configure, _) => captured = configure)
            .ReturnsAsync(1L);

        await CreateRepository().RecordAsync(_unitOfWorkMock.Object, 5, new AuditEntry("member.expired", SubjectPersonId: 9, ActorPersonId: null, Parameters: "{\"days\":30}"));

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.ActorPersonId].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.Parameters].Value.ShouldBe("{\"days\":30}");
    }

    [Test]
    public async Task RecordAsync_Should_Rethrow_When_TheStatementFails()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(_unitOfWorkMock.Object, QueryAuditMessages.RecordSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, long>>()))
            .ThrowsAsync(new InvalidOperationException("down"));

        await Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().RecordAsync(_unitOfWorkMock.Object, 5, new AuditEntry("member.left", 9, 9)));
    }

    [Test]
    public async Task ListByGroupAsync_Should_ReadTheFirstPage_When_NoCursor()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        var page = new List<AuditMessage>();
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryAuditMessages.ListByGroupSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, AuditMessage>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, AuditMessage>>((_, configure, _) => captured = configure)
            .ReturnsAsync(page);

        var result = await CreateRepository().ListByGroupAsync(5, before: null, limit: 20);

        result.ShouldBeSameAs(page);
        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.AuditMessageId].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.Limit].Value.ShouldBe(20);
    }

    [Test]
    public void RecordSql_Should_WriteTheMessage_AndItsEntry_InOneStatement()
    {
        var sql = QueryAuditMessages.RecordSql;
        sql.ShouldContain("WITH message AS (");
        sql.ShouldContain("INSERT INTO public.\"Messages\"");
        sql.ShouldContain("INSERT INTO public.\"AuditMessages\"");
        sql.ShouldContain("RETURNING");
    }

    [Test]
    public void ListByGroupSql_Should_ReadNewestFirst_ByKeyset()
    {
        var sql = QueryAuditMessages.ListByGroupSql;
        sql.ShouldContain("ORDER BY a.\"AuditMessageId\" DESC");
        sql.ShouldContain("LIMIT");
    }
}
