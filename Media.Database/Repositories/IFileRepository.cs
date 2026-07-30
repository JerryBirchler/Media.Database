using Media.Database.Models;

namespace Media.Database.Repositories;

public interface IFileRepository
{
    Task<Models.File?> GetById(Guid id);

    Task<Models.File?> GetCurrentBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5);

    Task<List<Models.File>> GetCurrentPagesBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5);

    Task<List<Models.File>> GetHistoryPagesBySourceMachineId(int sourceMachineId, string originalFilePath, int limit = 5);

    Task<Models.File?> Create(CreateFileRequest request);

    Task<Models.File?> Update(Guid id, UpdateFileRequest request);

    Task<Models.File?> Delete(Guid id);

    Task<List<Models.File>> DeleteHistoryBySourceMachineId(int sourceMachineId, string originalFilePath);
}
