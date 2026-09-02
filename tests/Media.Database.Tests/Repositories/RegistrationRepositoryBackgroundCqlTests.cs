#nullable enable
using AutoFixture;
using Cassandra;
using Media.Common.BackgroundJobs;
using Media.Common.Providers;
using Media.Common.Transactions;
using Media.Database.Models;
using Media.Database.Repositories;
using Media.Database.Repositories.Queries;
using Media.Database.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using NUnit.Framework;
using Serilog.Core;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Tests.Repositories;

/// <summary>
/// Covers RegistrationRepository's fire-and-forget background CQL sync (queued via
/// IBackgroundTaskQueue after UpdateSourceInformation's SQL write succeeds), the same way
/// FileRepositoryBackgroundCqlTests covers FileRepository's. Per the dotnet-test-standards
/// "fire-and-forget Task.Run" gotcha, we capture the queued callback and invoke it directly.
/// </summary>
[TestFixture]
public class RegistrationRepositoryBackgroundCqlTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<ICqlQueryExecutor> _cqlExecutorMock = null!;
    private Mock<IScyllaSessionProvider> _scyllaProviderMock = null!;
    private Func<CancellationToken, ValueTask>? _capturedCallback;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
        _cqlExecutorMock = new Mock<ICqlQueryExecutor>();
        _capturedCallback = null;

        _scyllaProviderMock = new Mock<IScyllaSessionProvider>();
        _scyllaProviderMock.Setup(p => p.MaxBatchSize).Returns(100);
    }

    private RegistrationRepository CreateRepository(IBackgroundTaskQueue backgroundTaskQueue) => new(
        _sqlExecutorMock.Object,
        _cqlExecutorMock.Object,
        _scyllaProviderMock.Object,
        () => Mock.Of<IUnitOfWork>(),
        backgroundTaskQueue,
        Mock.Of<ILogger<RegistrationRepository>>(),
        new LoggingLevelSwitch());

    private Mock<IBackgroundTaskQueue> CaptureBackgroundTaskQueue()
    {
        var mock = new Mock<IBackgroundTaskQueue>();
        mock.Setup(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, ValueTask>>()))
            .Callback<Func<CancellationToken, ValueTask>>(cb => _capturedCallback = cb)
            .Returns(ValueTask.CompletedTask);
        return mock;
    }

    private SourceMachineRegistrations CreateRegistration() => new()
    {
        RegistrationId = _fixture.Create<int>(),
        SourceMachineId = _fixture.Create<int>(),
        SourceMachineUuid = Guid.NewGuid(),
        SourceMachineName = _fixture.Create<string>(),
        DeviceTypeId = DeviceTypes.PC,
        OperatingSystem = _fixture.Create<string>(),
        FirstName = _fixture.Create<string>(),
        LastName = _fixture.Create<string>(),
        EmailAddress = _fixture.Create<string>(),
        CellPhoneNumber = _fixture.Create<string>(),
        HasRegistration = true,
        IsEmailVerified = false,
        IsSmsVerified = false,
        InsertedOn = DateTimeOffset.UtcNow,
        UpdatedOn = null,
        IsActive = true,
        OtpEmail = _fixture.Create<string>(),
        OtpCellPhone = _fixture.Create<string>(),
        RegistrationInsertedOn = DateTimeOffset.UtcNow,
        RegistrationUpdatedOn = null
    };

    [Test]
    public async Task UpdateSourceInformation_BackgroundUpdate_Should_UpsertRegistrationCql()
    {
        var existing = CreateRegistration();
        var updated = existing with { OperatingSystem = "updated-os" };
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(existing);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.UpdateSourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(updated);
        var request = new UpdateSourceInformationRequest
        {
            SourceMachineUuid = existing.SourceMachineUuid,
            EmailAddress = existing.EmailAddress,
            CellPhoneNumber = existing.CellPhoneNumber,
            OperatingSystem = "updated-os"
        };
        var queueMock = CaptureBackgroundTaskQueue();
        await CreateRepository(queueMock.Object).UpdateSourceInformation(request);

        await _capturedCallback!(CancellationToken.None);

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryRegistrations.UpsertRegistrationCql, It.IsAny<Action<SortedDictionary<string, object>>>()), Times.Once);
    }

    [Test]
    public async Task UpdateSourceInformation_BackgroundUpdate_Should_ConfigureRegistrationParameters()
    {
        var existing = CreateRegistration();
        var updated = existing with { OperatingSystem = "updated-os" };
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(existing);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.UpdateSourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(updated);
        var request = new UpdateSourceInformationRequest
        {
            SourceMachineUuid = existing.SourceMachineUuid,
            EmailAddress = existing.EmailAddress,
            CellPhoneNumber = existing.CellPhoneNumber,
            OperatingSystem = "updated-os"
        };
        Action<SortedDictionary<string, object>>? captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryRegistrations.UpsertRegistrationCql, It.IsAny<Action<SortedDictionary<string, object>>>()))
            .Callback<string, Action<SortedDictionary<string, object>>>((_, configure) => captured = configure)
            .Returns(Task.CompletedTask);
        var queueMock = CaptureBackgroundTaskQueue();
        await CreateRepository(queueMock.Object).UpdateSourceInformation(request);

        await _capturedCallback!(CancellationToken.None);

        var parameters = new SortedDictionary<string, object>();
        captured!(parameters);
        parameters[pn.SourceMachineUuid.ToUpperInvariant()].ShouldBe(updated.SourceMachineUuid);
        parameters[pn.EmailAddress.ToUpperInvariant()].ShouldBe(updated.EmailAddress);
        parameters[pn.OperatingSystem.ToUpperInvariant()].ShouldBe("updated-os");
    }

    [Test]
    public async Task UpdateSourceInformation_BackgroundUpdate_Should_HealScyllaSession_When_ConnectivityExceptionThrown()
    {
        var existing = CreateRegistration();
        var updated = existing with { OperatingSystem = "updated-os" };
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(existing);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.UpdateSourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(updated);
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryRegistrations.UpsertRegistrationCql, It.IsAny<Action<SortedDictionary<string, object>>>()))
            .ThrowsAsync(new NoHostAvailableException(new Dictionary<IPEndPoint, Exception>()));
        var sessionId = Guid.NewGuid();
        _scyllaProviderMock.Setup(p => p.GetCurrentSessionId()).Returns(sessionId);
        _scyllaProviderMock.Setup(p => p.HealSessionAsync(sessionId, It.IsAny<string>())).Returns(Task.CompletedTask);
        var request = new UpdateSourceInformationRequest
        {
            SourceMachineUuid = existing.SourceMachineUuid,
            EmailAddress = existing.EmailAddress,
            CellPhoneNumber = existing.CellPhoneNumber,
            OperatingSystem = "updated-os"
        };
        var queueMock = CaptureBackgroundTaskQueue();
        await CreateRepository(queueMock.Object).UpdateSourceInformation(request);

        await Should.ThrowAsync<NoHostAvailableException>(async () => await _capturedCallback!(CancellationToken.None));

        _scyllaProviderMock.Verify(p => p.HealSessionAsync(sessionId, It.IsAny<string>()), Times.Once);
    }
}
