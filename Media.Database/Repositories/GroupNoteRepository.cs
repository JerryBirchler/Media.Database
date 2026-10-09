using Media.Common.Helpers.Fluent;
using Media.Common.Transactions;
using Media.Database.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;

namespace Media.Database.Repositories;

/// <inheritdoc cref="IGroupNoteRepository"/>
public class GroupNoteRepository(
    IAuditMessageRepository auditMessageRepository,
    Func<IUnitOfWork> unitOfWorkFactory,
    ILogger<GroupNoteRepository> logger)
    : IGroupNoteRepository
{
    private readonly IAuditMessageRepository _auditMessageRepository = auditMessageRepository;
    private readonly Func<IUnitOfWork> _unitOfWorkFactory = unitOfWorkFactory;
    private readonly FluentLogger<GroupNoteRepository> _logger = logger.Initializer();

    public async Task<long> SendAsync(int groupId, int senderPersonId, IReadOnlyCollection<int> recipientPersonIds, string note)
    {
        if (recipientPersonIds.Count == 0)
            throw new ArgumentException("A note goes to someone.", nameof(recipientPersonIds));

        var parameters = new JsonObject
        {
            [AuditKinds.NoteParameter] = note,
            [AuditKinds.RecipientsParameter] = new JsonArray([.. recipientPersonIds.Distinct().Select(id => (JsonNode?)id)])
        };

        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();
            var id = await _auditMessageRepository.RecordAsync(uow, groupId,
                new AuditEntry(AuditKinds.NoteSent, SubjectPersonId: null, ActorPersonId: senderPersonId, parameters.ToJsonString()));
            await uow.CommitAsync();
            return id;
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync();
            // The words are the sender's own: never logged.
            _logger.LogError(ex, "SendAsync failed for GroupId: [{GroupId}], SenderPersonId: [{SenderPersonId}], Recipients: [{Count}]",
                groupId, senderPersonId, recipientPersonIds.Count);
            throw;
        }
    }
}
