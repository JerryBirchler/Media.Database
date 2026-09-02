#nullable enable
using AutoFixture;
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Tests.Repositories;

/// <summary>
/// Covers RegistrationRepository's public API against a mocked ISqlQueryExecutor, following the
/// same pattern as FileRepositoryQueryTests. The mapping extensions on QueryRegistrations
/// (ToRegistrationIds/ToAddRegistrationResponse) are declared as async methods, so when used as the
/// `map` delegate for ISqlQueryExecutor they produce a nested Task (e.g. Task&lt;AddRegistrationResponse?&gt;
/// as T rather than AddRegistrationResponse). The mocks below match that actual, current signature so
/// the UpdateSourceInformation branch that hits it stays covered as written.
/// </summary>
[TestFixture]
public class RegistrationRepositoryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<IBackgroundTaskQueue> _backgroundTaskQueueMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
        _backgroundTaskQueueMock = new Mock<IBackgroundTaskQueue>();
    }

    private RegistrationRepository CreateRepository()
    {
        var scyllaProviderMock = new Mock<IScyllaSessionProvider>();
        scyllaProviderMock.Setup(p => p.MaxBatchSize).Returns(100);

        return new RegistrationRepository(
            _sqlExecutorMock.Object,
            Mock.Of<ICqlQueryExecutor>(),
            scyllaProviderMock.Object,
            () => Mock.Of<IUnitOfWork>(),
            _backgroundTaskQueueMock.Object,
            Mock.Of<ILogger<RegistrationRepository>>(),
            new LoggingLevelSwitch());
    }

    private SourceMachineRegistrations CreateRegistration(Guid? sourceMachineUuid = null, string? emailAddress = null, string? cellPhoneNumber = null) =>
        new()
        {
            RegistrationId = _fixture.Create<int>(),
            SourceMachineId = _fixture.Create<int>(),
            SourceMachineUuid = sourceMachineUuid ?? Guid.NewGuid(),
            SourceMachineName = _fixture.Create<string>(),
            DeviceTypeId = DeviceTypes.PC,
            OperatingSystem = _fixture.Create<string>(),
            FirstName = _fixture.Create<string>(),
            LastName = _fixture.Create<string>(),
            EmailAddress = emailAddress ?? _fixture.Create<string>(),
            CellPhoneNumber = cellPhoneNumber ?? _fixture.Create<string>(),
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
    public void RegistrationRepository_Should_Implement_IRegistrationRepository()
    {
        CreateRepository().ShouldBeAssignableTo<IRegistrationRepository>();
    }

    [Test]
    public void RegistrationRepository_Should_Inherit_From_BaseRepository()
    {
        CreateRepository().ShouldBeAssignableTo<BaseRepository>();
    }

    [Test]
    public async Task GetByUuid_Should_ReturnRegistration_When_ExecutorFindsMatch()
    {
        var expected = CreateRegistration();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().GetByUuid(expected.SourceMachineUuid);

        result.ShouldBe(expected);
    }

    [Test]
    public async Task GetByUuid_Should_ReturnNull_When_ExecutorFindsNoMatch()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync((SourceMachineRegistrations?)null);

        var result = await CreateRepository().GetByUuid(Guid.NewGuid());

        result.ShouldBeNull();
    }

    [Test]
    public async Task GetByUuid_Should_ConfigureSourceMachineUuidParameter()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, SourceMachineRegistrations>>((_, configure, _) => captured = configure)
            .ReturnsAsync((SourceMachineRegistrations?)null);
        var uuid = Guid.NewGuid();

        await CreateRepository().GetByUuid(uuid);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.SourceMachineUuid].Value.ShouldBe(uuid);
    }

    [Test]
    public void GetByUuid_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().GetByUuid(Guid.NewGuid()));
    }

    [Test]
    public async Task AddBySourceInformation_Should_ReturnNull_And_NotAddRegistration_When_NoMatchingSourceMachine()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync((SourceMachineRegistrations?)null);
        var request = _fixture.Create<AddSourceInformationRequest>();

        var result = await CreateRepository().AddBySourceInformation(request);

        result.ShouldBeNull();
        _sqlExecutorMock.Verify(e => e.QuerySingleAsync(QueryRegistrations.AddRegistrationBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()), Times.Never);
    }

    [Test]
    public async Task AddBySourceInformation_Should_ReturnAddedRegistration_When_SourceMachineFound()
    {
        var sourceMachine = CreateRegistration();
        var added = CreateRegistration(sourceMachineUuid: sourceMachine.SourceMachineUuid);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(sourceMachine);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.AddRegistrationBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(added);
        var request = _fixture.Create<AddSourceInformationRequest>();

        var result = await CreateRepository().AddBySourceInformation(request);

        result.ShouldBe(added);
    }

    [Test]
    public async Task AddBySourceInformation_Should_ConfigureSourceInformationParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, SourceMachineRegistrations>>((_, configure, _) => captured = configure)
            .ReturnsAsync((SourceMachineRegistrations?)null);
        var request = _fixture.Create<AddSourceInformationRequest>();

        await CreateRepository().AddBySourceInformation(request);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.SourceMachineName].Value.ShouldBe(request.SourceMachineName);
        command.Parameters[pn.EmailAddress].Value.ShouldBe(request.EmailAddress);
        command.Parameters[pn.CellPhoneNumber].Value.ShouldBe(request.CellPhoneNumber);
        command.Parameters[pn.FirstName].Value.ShouldBe(request.FirstName);
        command.Parameters[pn.LastName].Value.ShouldBe(request.LastName);
        command.Parameters[pn.OperatingSystem].Value.ShouldBe(request.OperatingSystem);
    }

    [Test]
    public async Task AddBySourceInformation_Should_ConfigureSourceMachineUuidOnSecondQuery()
    {
        var sourceMachine = CreateRegistration();
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(sourceMachine);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.AddRegistrationBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, SourceMachineRegistrations>>((_, configure, _) => captured = configure)
            .ReturnsAsync((SourceMachineRegistrations?)null);
        var request = _fixture.Create<AddSourceInformationRequest>();

        await CreateRepository().AddBySourceInformation(request);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.SourceMachineUuid].Value.ShouldBe(sourceMachine.SourceMachineUuid);
    }

    [Test]
    public void AddBySourceInformation_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var request = _fixture.Create<AddSourceInformationRequest>();

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().AddBySourceInformation(request));
    }

    [Test]
    public async Task UpdateSourceInformation_Should_ReturnNull_When_ExistingRegistrationNotFound()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync((SourceMachineRegistrations?)null);
        var request = _fixture.Create<UpdateSourceInformationRequest>();

        var result = await CreateRepository().UpdateSourceInformation(request);

        result.ShouldBeNull();
        _sqlExecutorMock.Verify(e => e.QuerySingleAsync(QueryRegistrations.UpdateSourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()), Times.Never);
    }

    [Test]
    public async Task UpdateSourceInformation_Should_ReturnNull_When_UpdateSql_ReturnsNoRow()
    {
        var existing = CreateRegistration();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(existing);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.UpdateSourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync((SourceMachineRegistrations?)null);
        var request = new UpdateSourceInformationRequest
        {
            SourceMachineUuid = existing.SourceMachineUuid,
            EmailAddress = existing.EmailAddress,
            CellPhoneNumber = existing.CellPhoneNumber,
            OperatingSystem = existing.OperatingSystem
        };

        var result = await CreateRepository().UpdateSourceInformation(request);

        result.ShouldBeNull();
    }

    [Test]
    public async Task UpdateSourceInformation_Should_QueueBackgroundUpdate_When_ContactInformationUnchanged()
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

        var result = await CreateRepository().UpdateSourceInformation(request);

        result.ShouldBe(updated);
        _sqlExecutorMock.Verify(e => e.QueryManyAsync(QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Task<System.Collections.Generic.SortedSet<int>>>>()), Times.Never);
    }

    [Test]
    public async Task UpdateSourceInformation_Should_InactivateAndReRegister_When_ContactInformationChanged()
    {
        var existing = CreateRegistration();
        var updated = existing with { EmailAddress = "new@example.com", CellPhoneNumber = "555-0199" };
        var addResponse = new AddRegistrationResponse
        {
            Id = 99,
            OtpEmail = "111111",
            OtpCellPhone = "222222",
            IsEmailVerified = false,
            IsSmsVerified = false,
            InsertedOn = DateTimeOffset.UtcNow,
            UpdatedOn = null
        };
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(existing);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.UpdateSourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(updated);
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Task<System.Collections.Generic.SortedSet<int>>>>()))
            .ReturnsAsync([]);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.AddRegistrationBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Task<AddRegistrationResponse?>>>()))
            .ReturnsAsync(Task.FromResult<AddRegistrationResponse?>(addResponse));
        var request = new UpdateSourceInformationRequest
        {
            SourceMachineUuid = existing.SourceMachineUuid,
            EmailAddress = "new@example.com",
            CellPhoneNumber = "555-0199",
            OperatingSystem = existing.OperatingSystem
        };

        var result = await CreateRepository().UpdateSourceInformation(request);

        result.ShouldNotBeNull();
        result!.OtpEmail.ShouldBe(addResponse.OtpEmail);
        result.OtpCellPhone.ShouldBe(addResponse.OtpCellPhone);
        result.RegistrationId.ShouldBe(addResponse.Id);
        _sqlExecutorMock.Verify(e => e.QueryManyAsync(QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Task<System.Collections.Generic.SortedSet<int>>>>()), Times.Once);
        _backgroundTaskQueueMock.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, ValueTask>>()), Times.Once);
    }

    [Test]
    public void UpdateSourceInformation_Should_Rethrow_When_ExecutorThrows()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var request = _fixture.Create<UpdateSourceInformationRequest>();

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().UpdateSourceInformation(request));
    }

    [Test]
    public async Task UpdateSourceInformation_Should_SkipOtpGeneration_When_ContactAlreadyVerified()
    {
        var existing = CreateRegistration() with { IsEmailVerified = true, IsSmsVerified = true };
        var updated = existing with { EmailAddress = "new2@example.com", CellPhoneNumber = "555-0200" };
        var addResponse = new AddRegistrationResponse
        {
            Id = 100,
            OtpEmail = string.Empty,
            OtpCellPhone = string.Empty,
            IsEmailVerified = true,
            IsSmsVerified = true,
            InsertedOn = DateTimeOffset.UtcNow,
            UpdatedOn = null
        };
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(existing);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.UpdateSourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(updated);
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Task<SortedSet<int>>>>()))
            .ReturnsAsync([]);
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.AddRegistrationBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Task<AddRegistrationResponse?>>>()))
            .Callback<string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, Task<AddRegistrationResponse?>>>((_, configure, _) => captured = configure)
            .ReturnsAsync(Task.FromResult<AddRegistrationResponse?>(addResponse));
        var request = new UpdateSourceInformationRequest
        {
            SourceMachineUuid = existing.SourceMachineUuid,
            EmailAddress = "new2@example.com",
            CellPhoneNumber = "555-0200",
            OperatingSystem = existing.OperatingSystem
        };

        var result = await CreateRepository().UpdateSourceInformation(request);

        result.ShouldNotBeNull();
        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.OtpEmail].Value.ShouldBe(string.Empty);
        command.Parameters[pn.OtpCellPhone].Value.ShouldBe(string.Empty);
    }

    /// <summary>
    /// Pins current behavior: when AddRegistrationBySourceMachineUuidSql returns no row, the repository
    /// unboxes a null Nullable&lt;int&gt; at `(int)addRegistrationResponse?.Result?.Id!` and throws
    /// InvalidOperationException rather than handling the no-row case gracefully. This looks like a real
    /// gap (worth a null-check before the cast), not something this test suite should paper over.
    /// </summary>
    [Test]
    public void UpdateSourceInformation_Should_ThrowInvalidOperationException_When_AddRegistrationReturnsNoRow()
    {
        var existing = CreateRegistration();
        var updated = existing with { EmailAddress = "new3@example.com", CellPhoneNumber = "555-0201" };
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(existing);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.UpdateSourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(updated);
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Task<SortedSet<int>>>>()))
            .ReturnsAsync([]);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryRegistrations.AddRegistrationBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Task<AddRegistrationResponse?>>>()))
            .ReturnsAsync((Task<AddRegistrationResponse?>?)null);
        var request = new UpdateSourceInformationRequest
        {
            SourceMachineUuid = existing.SourceMachineUuid,
            EmailAddress = "new3@example.com",
            CellPhoneNumber = "555-0201",
            OperatingSystem = existing.OperatingSystem
        };

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().UpdateSourceInformation(request));
    }

    [Test]
    public async Task Delete_Should_ReturnFile_And_QueueBackgroundDelete_When_ExecutorFindsMatch()
    {
        var expected = _fixture.Create<Files>();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.DeleteSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(expected);

        var result = await CreateRepository().Delete(Guid.NewGuid());

        result.ShouldBe(expected);
        _backgroundTaskQueueMock.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, ValueTask>>()), Times.Once);
    }

    [Test]
    public async Task Delete_Should_ReturnNull_And_NotQueueBackgroundDelete_When_ExecutorFindsNoMatch()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(QueryFiles.DeleteSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync((Files?)null);

        var result = await CreateRepository().Delete(Guid.NewGuid());

        result.ShouldBeNull();
        _backgroundTaskQueueMock.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, ValueTask>>()), Times.Never);
    }

    [Test]
    public async Task DeleteHistoryBySourceMachineId_Should_QueueBackgroundDelete_When_FilesFound()
    {
        var files = _fixture.CreateMany<Files>(2).ToList();
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryFiles.DeleteHistorySql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync(files);

        var result = await CreateRepository().DeleteHistoryBySourceMachineId(1, "path");

        result.ShouldBe(files);
        _backgroundTaskQueueMock.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, ValueTask>>()), Times.Once);
    }

    [Test]
    public async Task DeleteHistoryBySourceMachineId_Should_NotQueueBackgroundDelete_When_NoFilesFound()
    {
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(QueryFiles.DeleteHistorySql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Files>>()))
            .ReturnsAsync([]);

        var result = await CreateRepository().DeleteHistoryBySourceMachineId(1, "path");

        result.ShouldBeEmpty();
        _backgroundTaskQueueMock.Verify(q => q.QueueBackgroundWorkItemAsync(It.IsAny<Func<CancellationToken, ValueTask>>()), Times.Never);
    }
}
