#nullable enable
using AutoFixture;
using Media.Common.Transactions;
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
using System.Collections.Generic;
using System.Threading.Tasks;
#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Tests.Repositories;

[TestFixture]
public class GroupSourceMachineRepositoryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<IAuditMessageRepository> _auditMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private IFixture _fixture = null!;

    /// <summary>What every change in these tests is recorded as (DATABASE-74).</summary>
    private static readonly AuditEntry Audit = AuditKinds.DeviceEntry(AuditKinds.DeviceAdded, 9, 7, "KITCHEN-FRAME");

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
        _auditMock = new Mock<IAuditMessageRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
    }

    private GroupSourceMachineRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        new MapGroupSourceMachineResponse(),
        _auditMock.Object,
        () => _unitOfWorkMock.Object,
        Mock.Of<ILogger<GroupSourceMachineRepository>>());

    private GroupSourceMachine CreateGroupSourceMachine() => _fixture.Create<GroupSourceMachine>();

    [Test]
    public void GroupSourceMachineRepository_Should_Implement_IGroupSourceMachineRepository()
    {
        CreateRepository().ShouldBeAssignableTo<IGroupSourceMachineRepository>();
    }

    [Test]
    public async Task UpsertAsync_Should_ReturnUpsertedRow()
    {
        var upserted = CreateGroupSourceMachine();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryGroupsSourceMachines.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupSourceMachine>>()))
            .ReturnsAsync(upserted);

        var result = await CreateRepository().UpsertAsync(upserted.GroupId, upserted.SourceMachineId, Audit);

        result.ShouldBe(upserted);
    }

    [Test]
    public async Task UpsertAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        var groupSourceMachine = CreateGroupSourceMachine();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryGroupsSourceMachines.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupSourceMachine>>()))
            .Callback<IUnitOfWork, string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, GroupSourceMachine>>((_, _, configure, _) => captured = configure)
            .ReturnsAsync(groupSourceMachine);

        await CreateRepository().UpsertAsync(4, 8, Audit);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.GroupId].Value.ShouldBe(4);
        command.Parameters[pn.SourceMachineId].Value.ShouldBe(8);
    }

    [Test]
    public void UpsertAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryGroupsSourceMachines.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupSourceMachine>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().UpsertAsync(1, 2, Audit));
    }

    [Test]
    public async Task DeactivateAsync_Should_ReturnDeactivatedRow_When_Found()
    {
        var deactivated = CreateGroupSourceMachine();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryGroupsSourceMachines.DeactivateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupSourceMachine>>()))
            .ReturnsAsync(deactivated);

        var result = await CreateRepository().DeactivateAsync(deactivated.GroupId, deactivated.SourceMachineId, Audit);

        result.ShouldBe(deactivated);
    }

    [Test]
    public async Task DeactivateAsync_Should_ReturnNull_When_NoneActive()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryGroupsSourceMachines.DeactivateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupSourceMachine>>()))
            .ReturnsAsync((GroupSourceMachine?)null);

        var result = await CreateRepository().DeactivateAsync(1, 2, Audit);

        result.ShouldBeNull();
    }

    [Test]
    public void DeactivateAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryGroupsSourceMachines.DeactivateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupSourceMachine>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().DeactivateAsync(1, 2, Audit));
    }

    [Test]
    public async Task UpsertAsync_Should_RecordTheAudit_InTheSameTransaction()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryGroupsSourceMachines.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupSourceMachine>>()))
            .ReturnsAsync(CreateGroupSourceMachine());

        await CreateRepository().UpsertAsync(4, 8, Audit);

        _auditMock.Verify(a => a.RecordAsync(_unitOfWorkMock.Object, 4, Audit), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task DeactivateAsync_Should_RecordNothing_When_NoneActive()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryGroupsSourceMachines.DeactivateSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupSourceMachine>>()))
            .ReturnsAsync((GroupSourceMachine?)null);

        await CreateRepository().DeactivateAsync(1, 2, Audit);

        _auditMock.Verify(a => a.RecordAsync(It.IsAny<IUnitOfWork>(), It.IsAny<int>(), It.IsAny<AuditEntry>()), Times.Never);
    }

    [Test]
    public void UpsertAsync_Should_RollBack_When_TheAuditFails()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryGroupsSourceMachines.UpsertSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, GroupSourceMachine>>()))
            .ReturnsAsync(CreateGroupSourceMachine());
        _auditMock.Setup(a => a.RecordAsync(It.IsAny<IUnitOfWork>(), It.IsAny<int>(), It.IsAny<AuditEntry>())).ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().UpsertAsync(1, 2, Audit));
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetSourceMachineIdentifiersByGroupIdAsync_Should_ReturnIdentifiers_From_Executor()
    {
        var expected = new List<(int SourceMachineId, string SourceMachineName)> { (SourceMachineId: 1, SourceMachineName: "a"), (SourceMachineId: 2, SourceMachineName: "b") };
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupsSourceMachines.GetSourceMachineIdentifiersByGroupIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, (int SourceMachineId, string SourceMachineName)>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetSourceMachineIdentifiersByGroupIdAsync(3, includeInactive: false, next: null, limit: 5);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetSourceMachineIdentifiersByGroupIdAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupsSourceMachines.GetSourceMachineIdentifiersByGroupIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, (int SourceMachineId, string SourceMachineName)>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, (int SourceMachineId, string SourceMachineName)>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);

        await CreateRepository().GetSourceMachineIdentifiersByGroupIdAsync(3, includeInactive: true, next: (SourceMachineId: 9, SourceMachineName: "Laptop"), limit: 5);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.GroupId].Value.ShouldBe(3);
        command.Parameters[pn.IncludeInactive].Value.ShouldBe(true);
        command.Parameters[pn.SourceMachineName].Value.ShouldBe("Laptop");
        command.Parameters[pn.SourceMachineId].Value.ShouldBe(9);
        command.Parameters[pn.Limit].Value.ShouldBe(5);
    }

    [Test]
    public async Task GetSourceMachineIdentifiersByGroupIdAsync_Should_ConfigureCursorAsDbNull_When_FirstPage()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupsSourceMachines.GetSourceMachineIdentifiersByGroupIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, (int SourceMachineId, string SourceMachineName)>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, (int SourceMachineId, string SourceMachineName)>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);

        await CreateRepository().GetSourceMachineIdentifiersByGroupIdAsync(3, includeInactive: false, next: null, limit: 5);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.SourceMachineName].Value.ShouldBe(DBNull.Value);
        command.Parameters[pn.SourceMachineId].Value.ShouldBe(DBNull.Value);
    }

    [Test]
    public async Task GetSourceMachineIdentifiersByGroupIdAsync_Should_ConfigureIncludeInactiveAsFalse_ByDefault()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupsSourceMachines.GetSourceMachineIdentifiersByGroupIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, (int SourceMachineId, string SourceMachineName)>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, (int SourceMachineId, string SourceMachineName)>>((_, configure, _) => captured = configure)
            .ReturnsAsync([]);

        await CreateRepository().GetSourceMachineIdentifiersByGroupIdAsync(3, includeInactive: false, next: null, limit: 5);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.IncludeInactive].Value.ShouldBe(false);
    }

    [Test]
    public void GetSourceMachineIdentifiersByGroupIdAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryGroupsSourceMachines.GetSourceMachineIdentifiersByGroupIdSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, (int SourceMachineId, string SourceMachineName)>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().GetSourceMachineIdentifiersByGroupIdAsync(3, false, null, 5));
    }

    [Test]
    public async Task GetActiveSourceMachineIdByGroupAndDisambiguationAsync_Should_ReturnSourceMachineId_When_ExecutorFindsMatch()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(QueryGroupsSourceMachines.GetActiveSourceMachineIdByGroupAndDisambiguationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, int>>()))
            .ReturnsAsync(42);

        var result = await CreateRepository().GetActiveSourceMachineIdByGroupAndDisambiguationAsync(3, "Kitchen Tablet", DeviceTypes.Tablet, "aB3x9");

        result.ShouldBe(42);
    }

    [Test]
    public async Task GetActiveSourceMachineIdByGroupAndDisambiguationAsync_Should_ReturnNull_When_ExecutorFindsNoMatch()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(QueryGroupsSourceMachines.GetActiveSourceMachineIdByGroupAndDisambiguationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, int>>()))
            .ReturnsAsync((int?)null);

        var result = await CreateRepository().GetActiveSourceMachineIdByGroupAndDisambiguationAsync(3, "Kitchen Tablet", DeviceTypes.Tablet, "aB3x9");

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetActiveSourceMachineIdByGroupAndDisambiguationAsync_Should_ConfigureParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(QueryGroupsSourceMachines.GetActiveSourceMachineIdByGroupAndDisambiguationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, int>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, int>>((_, configure, _) => captured = configure)
            .ReturnsAsync((int?)null);

        await CreateRepository().GetActiveSourceMachineIdByGroupAndDisambiguationAsync(3, "Kitchen Tablet", DeviceTypes.Tablet, "aB3x9");

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.GroupId].Value.ShouldBe(3);
        command.Parameters[pn.SourceMachineName].Value.ShouldBe("Kitchen Tablet");
        command.Parameters[pn.DeviceTypeId].Value.ShouldBe((int)DeviceTypes.Tablet);
        command.Parameters[pn.DisambiguationKey].Value.ShouldBe("aB3x9");
    }

    [Test]
    public void GetActiveSourceMachineIdByGroupAndDisambiguationAsync_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleValueAsync(QueryGroupsSourceMachines.GetActiveSourceMachineIdByGroupAndDisambiguationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, int>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().GetActiveSourceMachineIdByGroupAndDisambiguationAsync(3, "Kitchen Tablet", DeviceTypes.Tablet, "aB3x9"));
    }
}
