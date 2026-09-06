#nullable enable
using AutoFixture;
using Media.Common.Settings;
using Media.Common.Transactions;
using Media.Database.Models;
using Media.Database.Repositories;
using Media.Database.Repositories.Queries;
using Media.Database.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
/// same pattern as FileRepositoryQueryTests. AddBySourceInformation/UpdateSourceInformation/
/// ResendOtp now run inside an IUnitOfWork transaction (mocked here, not a real Postgres
/// transaction, per the same abstraction level as FileRepositoryQueryTests' Upsert/Update tests),
/// so their SQL executor mocks take the unit of work as their first argument.
/// ToRegistrationIds is still declared as an async method and so still produces a nested Task
/// (Task&lt;SortedSet&lt;int&gt;&gt; as T) when used as the `map` delegate; ToAddRegistrationResponse
/// was fixed to map synchronously like the other single-row mappers (see QueryRegistrations), so
/// its mocks use Func&lt;NpgsqlDataReader, AddRegistrationResponse&gt; directly.
/// </summary>
[TestFixture]
public class RegistrationRepositoryTests
{
    private Mock<ISqlQueryExecutor> _sqlExecutorMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private IFixture _fixture = null!;

    [SetUp]
    public void Setup()
    {
        _fixture = AutoMoqFixture.Create();
        _sqlExecutorMock = new Mock<ISqlQueryExecutor>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
    }

    private RegistrationRepository CreateRepository() => new(
        _sqlExecutorMock.Object,
        () => _unitOfWorkMock.Object,
        Options.Create(new RegistrationSettings { OtpWindow = TimeSpan.FromHours(1) }),
        Mock.Of<ILogger<RegistrationRepository>>(),
        new LoggingLevelSwitch());

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
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync((SourceMachineRegistrations?)null);
        var request = _fixture.Create<AddSourceInformationRequest>();

        var result = await CreateRepository().AddBySourceInformation(request);

        result.ShouldBeNull();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sqlExecutorMock.Verify(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.AddRegistrationBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()), Times.Never);
    }

    [Test]
    public async Task AddBySourceInformation_Should_ReturnAddedRegistration_When_SourceMachineFound()
    {
        var sourceMachine = CreateRegistration();
        var added = CreateRegistration(sourceMachineUuid: sourceMachine.SourceMachineUuid);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(sourceMachine);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.AddRegistrationBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(added);
        var request = _fixture.Create<AddSourceInformationRequest>();

        var result = await CreateRepository().AddBySourceInformation(request);

        result.ShouldNotBeNull();
        result.OtpEmail.ShouldBe(added.OtpEmail);
        result.OtpCellPhone.ShouldBe(added.OtpCellPhone);
        result.RegistrationId.ShouldBe(added.RegistrationId);
        result.RegistrationInsertedOn.ShouldBe(added.RegistrationInsertedOn);
        result.RegistrationUpdatedOn.ShouldBe(added.RegistrationUpdatedOn);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task AddBySourceInformation_Should_ConfigureSourceInformationParameters()
    {
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .Callback<IUnitOfWork, string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, SourceMachineRegistrations>>((_, _, configure, _) => captured = configure)
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
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(sourceMachine);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.AddRegistrationBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .Callback<IUnitOfWork, string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, SourceMachineRegistrations>>((_, _, configure, _) => captured = configure)
            .ReturnsAsync((SourceMachineRegistrations?)null);
        var request = _fixture.Create<AddSourceInformationRequest>();

        await CreateRepository().AddBySourceInformation(request);

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.SourceMachineUuid].Value.ShouldBe(sourceMachine.SourceMachineUuid);
    }

    [Test]
    public async Task AddBySourceInformation_Should_ReturnNull_When_AddRegistrationReturnsNoRow()
    {
        var sourceMachine = CreateRegistration();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(sourceMachine);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.AddRegistrationBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync((SourceMachineRegistrations?)null);
        var request = _fixture.Create<AddSourceInformationRequest>();

        var result = await CreateRepository().AddBySourceInformation(request);

        result.ShouldBeNull();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void AddBySourceInformation_Should_Rethrow_When_ExecutorThrows()
    {
        _unitOfWorkMock.Setup(u => u.CurrentTransaction).Returns((NpgsqlTransaction)null!);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var request = _fixture.Create<AddSourceInformationRequest>();

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().AddBySourceInformation(request));
    }

    [Test]
    public async Task UpdateSourceInformation_Should_ReturnNull_When_ExistingRegistrationNotFound()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync((SourceMachineRegistrations?)null);
        var request = _fixture.Create<UpdateSourceInformationRequest>();

        var result = await CreateRepository().UpdateSourceInformation(request);

        result.ShouldBeNull();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sqlExecutorMock.Verify(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.UpdateSourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()), Times.Never);
    }

    [Test]
    public async Task UpdateSourceInformation_Should_ReturnNull_When_UpdateSql_ReturnsNoRow()
    {
        var existing = CreateRegistration();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(existing);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.UpdateSourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
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
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateSourceInformation_Should_Commit_When_ContactInformationUnchanged()
    {
        var existing = CreateRegistration();
        var updated = existing with { OperatingSystem = "updated-os" };
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(existing);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.UpdateSourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
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
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sqlExecutorMock.Verify(e => e.QueryManyAsync(_unitOfWorkMock.Object, QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Task<SortedSet<int>>>>()), Times.Never);
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
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(existing);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.UpdateSourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(updated);
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(_unitOfWorkMock.Object, QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Task<SortedSet<int>>>>()))
            .ReturnsAsync([]);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.AddRegistrationBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, AddRegistrationResponse>>()))
            .ReturnsAsync(addResponse);
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
        _sqlExecutorMock.Verify(e => e.QueryManyAsync(_unitOfWorkMock.Object, QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Task<SortedSet<int>>>>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void UpdateSourceInformation_Should_Rethrow_When_ExecutorThrows()
    {
        _unitOfWorkMock.Setup(u => u.CurrentTransaction).Returns((NpgsqlTransaction)null!);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
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
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(existing);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.UpdateSourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(updated);
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(_unitOfWorkMock.Object, QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Task<SortedSet<int>>>>()))
            .ReturnsAsync([]);
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.AddRegistrationBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, AddRegistrationResponse>>()))
            .Callback<IUnitOfWork, string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, AddRegistrationResponse>>((_, _, configure, _) => captured = configure)
            .ReturnsAsync(addResponse);
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
    /// When AddRegistrationBySourceMachineUuidSql returns no row, the repository now returns null
    /// gracefully instead of unboxing a null Nullable&lt;int&gt; and throwing InvalidOperationException
    /// (the previous behavior, which this test used to pin).
    /// </summary>
    [Test]
    public async Task UpdateSourceInformation_Should_ReturnNull_When_AddRegistrationReturnsNoRow()
    {
        var existing = CreateRegistration();
        var updated = existing with { EmailAddress = "new3@example.com", CellPhoneNumber = "555-0201" };
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(existing);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.UpdateSourceInformationSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(updated);
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(_unitOfWorkMock.Object, QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Task<SortedSet<int>>>>()))
            .ReturnsAsync([]);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.AddRegistrationBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, AddRegistrationResponse>>()))
            .ReturnsAsync((AddRegistrationResponse?)null);
        var request = new UpdateSourceInformationRequest
        {
            SourceMachineUuid = existing.SourceMachineUuid,
            EmailAddress = "new3@example.com",
            CellPhoneNumber = "555-0201",
            OperatingSystem = existing.OperatingSystem
        };

        var result = await CreateRepository().UpdateSourceInformation(request);

        result.ShouldBeNull();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ResendOtp_Should_ReturnNull_When_RegistrationNotFound()
    {
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync((SourceMachineRegistrations?)null);

        var result = await CreateRepository().ResendOtp(Guid.NewGuid());

        result.ShouldBeNull();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sqlExecutorMock.Verify(e => e.QueryManyAsync(_unitOfWorkMock.Object, QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Task<SortedSet<int>>>>()), Times.Never);
    }

    [Test]
    public async Task ResendOtp_Should_ReturnBothFalse_And_SkipReRegistration_When_AlreadyFullyVerified()
    {
        var existing = CreateRegistration() with { IsEmailVerified = true, IsSmsVerified = true };
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(existing);

        var result = await CreateRepository().ResendOtp(existing.SourceMachineUuid);

        result.ShouldNotBeNull();
        result!.EmailOtpSent.ShouldBeFalse();
        result.SmsOtpSent.ShouldBeFalse();
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _sqlExecutorMock.Verify(e => e.QueryManyAsync(_unitOfWorkMock.Object, QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Task<SortedSet<int>>>>()), Times.Never);
    }

    [Test]
    public async Task ResendOtp_Should_GenerateOtpOnlyForUnverifiedChannel_When_OneChannelAlreadyVerified()
    {
        var existing = CreateRegistration() with { IsEmailVerified = true, IsSmsVerified = false };
        var addResponse = new AddRegistrationResponse
        {
            Id = 42,
            OtpEmail = string.Empty,
            OtpCellPhone = "654321",
            IsEmailVerified = true,
            IsSmsVerified = false,
            InsertedOn = DateTimeOffset.UtcNow,
            UpdatedOn = null
        };
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(existing);
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(_unitOfWorkMock.Object, QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Task<SortedSet<int>>>>()))
            .ReturnsAsync([]);
        Action<NpgsqlParameterCollection>? captured = null;
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.AddRegistrationBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, AddRegistrationResponse>>()))
            .Callback<IUnitOfWork, string, Action<NpgsqlParameterCollection>, Func<NpgsqlDataReader, AddRegistrationResponse>>((_, _, configure, _) => captured = configure)
            .ReturnsAsync(addResponse);

        var result = await CreateRepository().ResendOtp(existing.SourceMachineUuid);

        result.ShouldNotBeNull();
        result!.EmailOtpSent.ShouldBeFalse();
        result.SmsOtpSent.ShouldBeTrue();

        using var command = new NpgsqlCommand();
        captured!(command.Parameters);
        command.Parameters[pn.OtpEmail].Value.ShouldBe(string.Empty);
        command.Parameters[pn.OtpCellPhone].Value.ShouldNotBe(string.Empty);
    }

    [Test]
    public async Task ResendOtp_Should_ReturnNull_When_AddRegistrationReturnsNoRow()
    {
        var existing = CreateRegistration();
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ReturnsAsync(existing);
        _sqlExecutorMock
            .Setup(e => e.QueryManyAsync(_unitOfWorkMock.Object, QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, Task<SortedSet<int>>>>()))
            .ReturnsAsync([]);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.AddRegistrationBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, AddRegistrationResponse>>()))
            .ReturnsAsync((AddRegistrationResponse?)null);

        var result = await CreateRepository().ResendOtp(existing.SourceMachineUuid);

        result.ShouldBeNull();
        _unitOfWorkMock.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public void ResendOtp_Should_Rethrow_When_ExecutorThrows()
    {
        _unitOfWorkMock.Setup(u => u.CurrentTransaction).Returns((NpgsqlTransaction)null!);
        _sqlExecutorMock
            .Setup(e => e.QuerySingleAsync(_unitOfWorkMock.Object, QueryRegistrations.GetBySourceMachineUuidSql, It.IsAny<Action<NpgsqlParameterCollection>>(), It.IsAny<Func<NpgsqlDataReader, SourceMachineRegistrations>>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().ResendOtp(Guid.NewGuid()));
    }
}
