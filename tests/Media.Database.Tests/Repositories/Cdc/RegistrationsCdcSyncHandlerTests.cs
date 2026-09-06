#nullable enable
using AutoFixture;
using Media.Common.Providers;
using Media.Database.Models;
using Media.Database.Repositories;
using Media.Database.Repositories.Cdc;
using Media.Database.Repositories.Queries;
using Media.Database.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using NUnit.Framework;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Media.Database.Tests.Repositories.Cdc;

[TestFixture]
public class RegistrationsCdcSyncHandlerTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<ICqlQueryExecutor> _cqlExecutorMock = null!;
    private Mock<IScyllaSessionProvider> _scyllaProviderMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
        _cqlExecutorMock = new Mock<ICqlQueryExecutor>();
        _scyllaProviderMock = new Mock<IScyllaSessionProvider>();
        _scyllaProviderMock.Setup(p => p.MaxBatchSize).Returns(100);
    }

    private RegistrationsCdcSyncHandler CreateHandler() => new(
        _sqlExecutorMock.Object,
        _cqlExecutorMock.Object,
        _scyllaProviderMock.Object,
        Mock.Of<ILogger<RegistrationsCdcSyncHandler>>());

    private static JsonElement AfterWithSourceMachineId(int sourceMachineId) =>
        JsonDocument.Parse(JsonSerializer.Serialize(new { SourceMachineId = sourceMachineId })).RootElement;

    [Test]
    public void Topics_Should_Contain_Both_Registration_Topics()
    {
        var sut = CreateHandler();

        sut.Topics.ShouldBe(["cdc.public.Registrations", "cdc.public.SourceMachineRegistrations"]);
    }

    [Test]
    public async Task ApplyAsync_Should_Upsert_CurrentJoinedState_When_ReQuery_Finds_A_Row()
    {
        var registration = _fixture.Create<SourceMachineRegistrations>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(
                QueryRegistrations.GetBySourceMachineIdSql,
                It.IsAny<Action<NpgsqlParameterCollection>>(),
                It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(registration);

        var sut = CreateHandler();
        var record = new Media.Common.Cdc.CdcChangeRecord(
            "cdc.public.SourceMachineRegistrations", "key", AfterWithSourceMachineId(registration.SourceMachineId), IsDeleted: false, SourceTimestampMs: 0, Offset: 0);

        await sut.ApplyAsync(record, CancellationToken.None);

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryRegistrations.UpsertRegistrationCql, It.IsAny<Action<SortedDictionary<string, object>>>()), Times.Once);
    }

    [Test]
    public async Task ApplyAsync_Should_Pass_Correct_Field_Values_To_UpsertRegistrationCql()
    {
        var registration = _fixture.Create<SourceMachineRegistrations>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(
                QueryRegistrations.GetBySourceMachineIdSql,
                It.IsAny<Action<NpgsqlParameterCollection>>(),
                It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(registration);

        SortedDictionary<string, object>? captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryRegistrations.UpsertRegistrationCql, It.IsAny<Action<SortedDictionary<string, object>>>()))
            .Callback<string, Action<SortedDictionary<string, object>>>((_, configure) =>
            {
                captured = new SortedDictionary<string, object>();
                configure(captured);
            })
            .Returns(Task.CompletedTask);

        var sut = CreateHandler();
        var record = new Media.Common.Cdc.CdcChangeRecord(
            "cdc.public.Registrations", "key", AfterWithSourceMachineId(registration.SourceMachineId), IsDeleted: false, SourceTimestampMs: 0, Offset: 0);

        await sut.ApplyAsync(record, CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!["@SOURCEMACHINEUUID"].ShouldBe(registration.SourceMachineUuid);
        captured["@SOURCEMACHINEID"].ShouldBe(registration.SourceMachineId);
        captured["@SOURCEMACHINENAME"].ShouldBe(registration.SourceMachineName);
    }

    [Test]
    public async Task ApplyAsync_Should_Not_Touch_Scylla_When_ReQuery_Finds_No_Current_Row()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(
                QueryRegistrations.GetBySourceMachineIdSql,
                It.IsAny<Action<NpgsqlParameterCollection>>(),
                It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync((SourceMachineRegistrations?)null);

        var sut = CreateHandler();
        var record = new Media.Common.Cdc.CdcChangeRecord(
            "cdc.public.Registrations", "key", AfterWithSourceMachineId(42), IsDeleted: false, SourceTimestampMs: 0, Offset: 0);

        await Should.NotThrowAsync(() => sut.ApplyAsync(record, CancellationToken.None));

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(It.IsAny<string>(), It.IsAny<Action<SortedDictionary<string, object>>>()), Times.Never);
    }

    [Test]
    public async Task ApplyAsync_Should_Do_Nothing_When_Record_Has_No_After_Payload()
    {
        var sut = CreateHandler();
        var record = new Media.Common.Cdc.CdcChangeRecord(
            "cdc.public.Registrations", "key", After: null, IsDeleted: true, SourceTimestampMs: 0, Offset: 0);

        await Should.NotThrowAsync(() => sut.ApplyAsync(record, CancellationToken.None));

        _sqlExecutorMock.Verify(e => e.QuerySingleAsync(
            It.IsAny<string>(), It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()), Times.Never);
    }
}
