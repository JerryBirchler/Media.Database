#nullable enable
using Media.Common.Transactions;
using Media.Database.Models;
using Media.Database.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Shouldly;
using System;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Media.Database.Tests.Repositories;

[TestFixture]
public class GroupNoteRepositoryTests
{
    private Mock<IAuditMessageRepository> _auditMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;

    [SetUp]
    public void Setup()
    {
        _auditMock = new Mock<IAuditMessageRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
    }

    private GroupNoteRepository CreateRepository() => new(_auditMock.Object, () => _unitOfWorkMock.Object, Mock.Of<ILogger<GroupNoteRepository>>());

    [Test]
    public async Task SendAsync_Should_RecordTheNote_WithItsWordsAndRecipients_InItsOwnTransaction()
    {
        AuditEntry? recorded = null;
        _auditMock.Setup(a => a.RecordAsync(_unitOfWorkMock.Object, 7, It.IsAny<AuditEntry>()))
            .Callback<IUnitOfWork, int?, AuditEntry>((_, _, entry) => recorded = entry)
            .ReturnsAsync(41L);

        var id = await CreateRepository().SendAsync(7, 3, [4, 5, 4], "Dinner at six.");

        id.ShouldBe(41L);
        recorded!.Kind.ShouldBe("note.sent");
        recorded.ActorPersonId.ShouldBe(3);
        var parameters = JsonNode.Parse(recorded.Parameters!)!;
        parameters["note"]!.GetValue<string>().ShouldBe("Dinner at six.");
        parameters["recipients"]!.AsArray().Select(n => n!.GetValue<int>()).ShouldBe([4, 5]);
        _unitOfWorkMock.Verify(u => u.CommitAsync(), Times.Once);
    }

    [Test]
    public async Task SendAsync_Should_Throw_When_ItGoesToNobody()
    {
        await Should.ThrowAsync<ArgumentException>(() => CreateRepository().SendAsync(7, 3, [], "Hello"));
    }

    [Test]
    public async Task SendAsync_Should_RollBack_When_RecordingFails()
    {
        _auditMock.Setup(a => a.RecordAsync(_unitOfWorkMock.Object, 7, It.IsAny<AuditEntry>())).ThrowsAsync(new InvalidOperationException("boom"));

        await Should.ThrowAsync<InvalidOperationException>(() => CreateRepository().SendAsync(7, 3, [4], "Hello"));
        _unitOfWorkMock.Verify(u => u.RollbackAsync(), Times.Once);
    }

    [Test]
    public void AuditKinds_Should_TellANote_ToThePeopleItNames()
    {
        AuditKinds.WhoIsTold("note.sent").ShouldBe(Told.Named);
    }
}
