#nullable enable
using AutoFixture;
using Media.Database.Mappers;
using Media.Database.Models;
using Media.Database.Repositories;
using Media.Database.Repositories.Queries;
using Media.Database.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using NUnit.Framework;
using Shouldly;
using System;
using System.Threading.Tasks;
#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Tests.Repositories;

/// <summary>
/// Covers GroupRepository's public API against a mocked ISqlQueryExecutor, following the same
/// pattern as PersonRepositoryTests.
/// </summary>
[TestFixture]
public class GroupRepositoryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
    }

    private GroupRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        new MapGroupResponse(),
        Mock.Of<ILogger<GroupRepository>>());

    private Group CreateGroup(string? name = null, string? title = null, string? description = null)
    {
        var group = _fixture.Create<Group>();
        return group with
        {
            Name = name ?? group.Name,
            Title = title ?? group.Title,
            Description = description ?? group.Description
        };
    }

    [Test]
    public void GroupRepository_Should_Implement_IGroupRepository()
    {
        CreateRepository().ShouldBeAssignableTo<IGroupRepository>();
    }

    [Test]
    public async Task CreateAsync_Should_ReturnCreatedGroup()
    {
        var created = CreateGroup();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroups.AddGroupSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Group>>()))
            .ReturnsAsync(created);

        var result = await CreateRepository().CreateAsync(created.Name, created.Title, created.Description, created.IsActive);

        result.ShouldBe(created);
    }

    [Test]
    public void CreateAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroups.AddGroupSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Group>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().CreateAsync("Name", "Title", "Description", true));
    }

    [Test]
    public async Task GetByUuidAsync_Should_ReturnGroup_When_Found()
    {
        var expected = CreateGroup();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroups.GetByUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Group>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetByUuidAsync(expected.GroupUuid);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetByUuidAsync_Should_ReturnNull_When_NotFound()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroups.GetByUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Group>>()))
            .ReturnsAsync((Group?)null);

        var result = await CreateRepository().GetByUuidAsync(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetByNameAsync_Should_ReturnGroup_When_Found()
    {
        var expected = CreateGroup();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroups.GetByNameSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Group>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetByNameAsync(expected.Name);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task UpdateAsync_Should_ReturnUpdatedGroup_When_Found()
    {
        var updated = CreateGroup();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroups.UpdateGroupSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Group>>()))
            .ReturnsAsync(updated);

        var result = await CreateRepository().UpdateAsync(updated.GroupId, updated.Title, updated.Description);

        result.ShouldBe(updated);
    }

    [Test]
    public async Task UpdateAsync_Should_ReturnNull_When_GroupDoesNotExist()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroups.UpdateGroupSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Group>>()))
            .ReturnsAsync((Group?)null);

        var result = await CreateRepository().UpdateAsync(_fixture.Create<int>(), "Title", "Description");

        result.ShouldBeNull();
    }

    [Test]
    public async Task UpdateAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroups.UpdateGroupSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Group>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, Group>>((_, configure, _) => captured = configure)
            .ReturnsAsync((Group?)null);

        await CreateRepository().UpdateAsync(42, "New Title", null);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.GroupId].Value.ShouldBe(42);
        command.Parameters[pn.Title].Value.ShouldBe("New Title");
        command.Parameters[pn.Description].Value.ShouldBe(DBNull.Value);
    }

    [Test]
    public async Task SetActiveAsync_Should_ReturnUpdatedGroup_When_Found()
    {
        var updated = CreateGroup();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroups.SetActiveSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Group>>()))
            .ReturnsAsync(updated);

        var result = await CreateRepository().SetActiveAsync(updated.GroupId, false);

        result.ShouldBe(updated);
    }

    [Test]
    public void SetActiveAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryGroups.SetActiveSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Group>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().SetActiveAsync(1, true));
    }
}
