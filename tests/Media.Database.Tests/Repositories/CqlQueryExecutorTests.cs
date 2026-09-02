#nullable enable
using Cassandra;
using Media.Common.Providers;
using Media.Database.Repositories;
using Media.Database.Repositories.Queries.Helpers;
using Moq;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Media.Database.Tests.Repositories;

/// <summary>
/// Covers CqlQueryExecutor, the Scylla/Cassandra counterpart to SqlQueryExecutor, against a mocked
/// ISession/PreparedStatement/BoundStatement (all interface/virtual and therefore mockable, unlike
/// Npgsql's connection/command/reader types - which is why SqlQueryExecutor has no equivalent test
/// file). Row-to-T mapping with populated data is not covered here: Cassandra.Row has no public,
/// mockable construction path, so only the zero-row branch of each method is exercised - the same
/// boundary the rest of this suite already accepts for the Postgres side.
/// </summary>
[TestFixture]
public class CqlQueryExecutorTests
{
    private Mock<ISession> _sessionMock = null!;
    private Mock<IScyllaSessionProvider> _scyllaProviderMock = null!;
    private CqlQueryExecutor _executor = null!;

    [SetUp]
    public void Setup()
    {
        _sessionMock = new Mock<ISession>();
        var preparedStatementMock = new Mock<PreparedStatement>();
        preparedStatementMock.Setup(ps => ps.Bind(It.IsAny<object[]>())).Returns(new Mock<BoundStatement>().Object);
        _sessionMock.Setup(s => s.Prepare(It.IsAny<string>())).Returns(preparedStatementMock.Object);
        _sessionMock.Setup(s => s.ExecuteAsync(It.IsAny<Statement>())).ReturnsAsync(new RowSet());

        _scyllaProviderMock = new Mock<IScyllaSessionProvider>();
        _scyllaProviderMock.Setup(p => p.GetSession()).Returns(_sessionMock.Object);

        _executor = new CqlQueryExecutor(_scyllaProviderMock.Object);
    }

    [Test]
    public void CqlQueryExecutor_Should_Implement_ICqlQueryExecutor()
    {
        _executor.ShouldBeAssignableTo<ICqlQueryExecutor>();
    }

    [Test]
    public async Task QuerySingleAsync_Should_ReturnNull_When_NoRowsMatch()
    {
        var result = await _executor.QuerySingleAsync("SELECT * FROM t WHERE id = @id", p => p.AddWithValue("@ID", Guid.NewGuid()), row => row.GetValue<string>("name"));

        result.ShouldBeNull();
    }

    [Test]
    public async Task QuerySingleValueAsync_Should_ReturnNull_When_NoRowsMatch()
    {
        var result = await _executor.QuerySingleValueAsync("SELECT * FROM t WHERE id = @id", p => p.AddWithValue("@ID", Guid.NewGuid()), row => row.GetValue<int>("count"));

        result.ShouldBeNull();
    }

    [Test]
    public async Task QueryManyAsync_Should_ReturnEmptyList_When_NoRowsMatch()
    {
        var result = await _executor.QueryManyAsync("SELECT * FROM t", p => { }, row => row.GetValue<string>("name"));

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task ExecuteAsync_Should_InvokeConfigureParameters_And_ExecuteAgainstSession()
    {
        SortedDictionary<string, object>? captured = null;

        await _executor.ExecuteAsync("DELETE FROM t WHERE id = @id", p =>
        {
            captured = p;
            p.AddWithValue("@ID", Guid.NewGuid());
        });

        captured.ShouldNotBeNull();
        captured.ShouldContainKey("@ID");
        _sessionMock.Verify(s => s.ExecuteAsync(It.IsAny<Statement>()), Times.Once);
    }

    [Test]
    public async Task QueryManyAsync_Should_PrepareAndBind_UsingConfiguredParameters()
    {
        var id = Guid.NewGuid();

        await _executor.QueryManyAsync(
            "SELECT * FROM t WHERE id = @id",
            p => p.AddWithValue("@ID", id),
            row => row.GetValue<string>("name"));

        _sessionMock.Verify(s => s.Prepare("SELECT * FROM t WHERE id = ?"), Times.Once);
        _sessionMock.Verify(s => s.ExecuteAsync(It.IsAny<Statement>()), Times.Once);
    }
}
