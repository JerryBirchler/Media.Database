using Media.Database.Models;

namespace Media.Database.Repositories;

public interface IWordRepository
{
    Task<Models.Words?> GetById(int id);
    Task<List<Models.ViewWordFiles>> GetFilePagesByWordOrigin(string? word, WordOrigin? origin, Guid? fileId, int limit = 5);
    Task<List<Models.ViewWordFiles>> GetFilePagesByWordFileId(string? word, WordOrigin? origin, Guid? fileId, int limit = 5);
    Task<List<Models.ViewWordFiles>> GetFilePagesByFileIdOrigin(string? word, WordOrigin? origin, Guid? fileId, int limit = 5);
    Task<List<Models.ViewWordFiles>> GetFilePagesByFileIdWord(string? word, WordOrigin? origin, Guid? fileId, int limit = 5);
    Task Upsert(UpsertWordRequest request);
    Task Delete(int id);
}