using Media.Database.Models;

namespace Media.Database.Repositories;

public interface IFileRepository
{
    Task<Models.Files?> GetById(Guid id);

    Task<Models.Files?> GetCurrentBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5);

    Task<List<Models.Files>> GetCurrentPagesBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5);

    Task<List<Models.Files>> GetHistoryPagesBySourceMachineId(int sourceMachineId, string originalFilePath, int limit = 5);

    Task<Models.Files?> Create(UploadFileRequest request);

    Task<Models.Files?> Update(Guid id, UpdateFileRequest request);

    Task<Models.Files?> Delete(Guid id);

    Task<List<Models.Files>> DeleteHistoryBySourceMachineId(int sourceMachineId, string originalFilePath);
}
