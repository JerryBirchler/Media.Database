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
public class PersonsCdcSyncHandlerTests
{
    private Mock<ICqlQueryExecutor> _cqlExecutorMock = null!;
    private Mock<IScyllaSessionProvider> _scyllaProviderMock = null!;

    [SetUp]
    public void Setup()
    {
        _cqlExecutorMock = new Mock<ICqlQueryExecutor>();
        _scyllaProviderMock = new Mock<IScyllaSessionProvider>();
        _scyllaProviderMock.Setup(p => p.MaxBatchSize).Returns(100);
    }

    private PersonsCdcSyncHandler CreateHandler() => new(
        _cqlExecutorMock.Object,
        _scyllaProviderMock.Object,
        Mock.Of<ILogger<PersonsCdcSyncHandler>>());

    private static CdcChangeRecord UpsertRecord(int personId, int? createdByPersonId = 1, bool isSuperAdmin = false)
    {
        var payload = new
        {
            PersonId = personId,
            PersonUuid = Guid.NewGuid(),
            EmailAddress = "jane@example.com",
            CellPhoneNumber = "555-1234",
            FirstName = "Jane",
            LastName = "Doe",
            SpokenName = (string?)null,
            IsActive = true,
            CreatedByPersonId = createdByPersonId,
            IsSuperAdmin = isSuperAdmin,
            IsEmailVerified = false,
            IsSmsVerified = false,
            OtpWindowOverrideMinutes = (int?)null,
            InsertedOn = "2026-07-25T01:35:45.110Z",
            UpdatedOn = "2026-08-27T18:28:07.082Z",
            __source_ts_ms = 1788574857777L,
            __op = "u"
        };
        var after = JsonDocument.Parse(JsonSerializer.Serialize(payload)).RootElement;
        return new CdcChangeRecord("cdc.public.Persons", BuildKey(personId), after, IsDeleted: false, SourceTimestampMs: 1788574857777, Offset: 0);
    }

    private static CdcChangeRecord DeleteRecord(int personId) =>
        new("cdc.public.Persons", BuildKey(personId), After: null, IsDeleted: true, SourceTimestampMs: 0, Offset: 1);

    private static string BuildKey(int personId) => JsonSerializer.Serialize(new { PersonId = personId });

    [Test]
    public async Task ApplyAsync_Should_Upsert_When_Record_Is_Not_Deleted()
    {
        var sut = CreateHandler();

        await sut.ApplyAsync(UpsertRecord(1), CancellationToken.None);

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryPersons.UpsertCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Once);
        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryPersons.DeleteCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Never);
    }

    [Test]
    public async Task ApplyAsync_Should_Pass_Correct_Field_Values_To_UpsertCql()
    {
        var sut = CreateHandler();
        Dictionary<string, object>? captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryPersons.UpsertCql, It.IsAny<Action<Dictionary<string, object>>>()))
            .Callback<string, Action<Dictionary<string, object>>>((_, configure) =>
            {
                captured = new Dictionary<string, object>();
                configure(captured);
            })
            .Returns(Task.CompletedTask);

        await sut.ApplyAsync(UpsertRecord(7, isSuperAdmin: true), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!["@PERSONID"].ShouldBe(7);
        captured["@EMAILADDRESS"].ShouldBe("jane@example.com");
        captured["@FIRSTNAME"].ShouldBe("Jane");
        captured["@ISSUPERADMIN"].ShouldBe(true);
    }

    [Test]
    public async Task ApplyAsync_Should_Pass_Null_CreatedByPersonId_When_Json_Null()
    {
        var sut = CreateHandler();
        Dictionary<string, object>? captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryPersons.UpsertCql, It.IsAny<Action<Dictionary<string, object>>>()))
            .Callback<string, Action<Dictionary<string, object>>>((_, configure) =>
            {
                captured = new Dictionary<string, object>();
                configure(captured);
            })
            .Returns(Task.CompletedTask);

        await sut.ApplyAsync(UpsertRecord(1, createdByPersonId: null), CancellationToken.None);

        captured.ShouldNotBeNull();
        captured!["@CREATEDBYPERSONID"].ShouldBeNull();
        captured["@OTPWINDOWOVERRIDEMINUTES"].ShouldBeNull();
    }

    [Test]
    public async Task ApplyAsync_Should_Delete_When_Record_Is_Deleted()
    {
        var sut = CreateHandler();
        Dictionary<string, object>? captured = null;
        _cqlExecutorMock
            .Setup(e => e.ExecuteAsync(QueryPersons.DeleteCql, It.IsAny<Action<Dictionary<string, object>>>()))
            .Callback<string, Action<Dictionary<string, object>>>((_, configure) =>
            {
                captured = new Dictionary<string, object>();
                configure(captured);
            })
            .Returns(Task.CompletedTask);

        await sut.ApplyAsync(DeleteRecord(3), CancellationToken.None);

        _cqlExecutorMock.Verify(e => e.ExecuteAsync(QueryPersons.UpsertCql, It.IsAny<Action<Dictionary<string, object>>>()), Times.Never);
        captured.ShouldNotBeNull();
        captured!["@PERSONID"].ShouldBe(3);
    }

    [Test]
    public void Topics_Should_Contain_Only_CdcPublicPersons()
    {
        var sut = CreateHandler();

        sut.Topics.ShouldBe(["cdc.public.Persons"]);
    }
}
