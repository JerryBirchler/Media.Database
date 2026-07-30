using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Configuration;

#pragma warning disable CS8981 
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981 

namespace Media.Database.Repositories
{
    public class FileRepository(IConfiguration configuration) 
        : BaseRepository(configuration), IFileRepository
    {
        public async Task<Models.File?> GetById(Guid id)
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.GetByIdSql);
            sqlCommand.Parameters.AddWithValue(pn.Id, id);
            await using var reader = await sqlCommand.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return reader.ToFile();
        }

        public async Task<Models.File?> GetCurrentBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5)
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.GetCurrentBySourceMachineIdSql);
            sqlCommand.Parameters.AddWithValue(pn.SourceMachineId, sourceMachineId);
            sqlCommand.Parameters.AddWithValue(pn.OriginalFilePath, originalFilePath.ToNullableValueForSql());
            await using var reader = await sqlCommand.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return reader.ToFile();
        }

        public async Task<List<Models.File>> GetCurrentPagesBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5)
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.GetCurrentPagesBySourceMachineIdSql);
            sqlCommand.Parameters.AddWithValue(pn.SourceMachineId, sourceMachineId);
            sqlCommand.Parameters.AddWithValue(pn.OriginalFilePath, originalFilePath.ToNullableValueForSql());
            sqlCommand.Parameters.AddWithValue(pn.Limit, limit);
            await using var reader = await sqlCommand.ExecuteReaderAsync();
            return await reader.ToFiles();
        }

        public async Task<List<Models.File>> GetHistoryPagesBySourceMachineId(int sourceMachineId, string originalFilePath, int limit = 5)
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.GetHistoryPagesBySourceMachineIdSql);
            sqlCommand.Parameters.AddWithValue(pn.SourceMachineId, sourceMachineId);
            sqlCommand.Parameters.AddWithValue(pn.OriginalFilePath, originalFilePath);
            sqlCommand.Parameters.AddWithValue(pn.Limit, limit);
            await using var reader = await sqlCommand.ExecuteReaderAsync();
            return await reader.ToFiles();
        }

        public async Task<Models.File?> Create(CreateFileRequest request)
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.GetPreviousIdsSql);
            sqlCommand.Parameters.AddWithValue(pn.SourceMachineId, request.SourceMachineId);
            sqlCommand.Parameters.AddWithValue(pn.OriginalFilePath, request.OriginalFilePath);
            
            List<Guid> previousIds = [];
            
            await using (var reader = await sqlCommand.ExecuteReaderAsync())
            {
                previousIds = await reader.ToIds();
            }

            await using var sqlCommand2 = await sqlConnection.GetCommand(QueryFiles.CreateSql);
            sqlCommand2.Parameters.AddWithValue(pn.SourceMachineId, request.SourceMachineId);
            sqlCommand2.Parameters.AddWithValue(pn.OriginalFilePath, request.OriginalFilePath);
            sqlCommand2.Parameters.AddWithValue(pn.LastFileUpdate, request.LastFileUpdate.AdjustPrecision().ToNullableValueForSql());
            sqlCommand2.Parameters.AddWithValue(pn.Metadata, NpgsqlTypes.NpgsqlDbType.Json, request.Metadata.ToNullableValueForSql()?.ToJsonString()!);
            await using var reader2 = await sqlCommand2.ExecuteReaderAsync();

            if (!await reader2.ReadAsync())            
                return null;

            var file = reader2.ToFile();
            var noSqlConnection = GetNoSqlConnection();

            NoSqlCommand noSqlCommand = noSqlConnection.GetNoSqlCommand(
                QueryFiles.InactivateNoSql, _scyllaSettings.MaxBatchsize)!;

            noSqlCommand.BeginBatch();
            
            var tasks = previousIds.Select(previousId => noSqlCommand.AddQuery(previousId));
            
            await Task.WhenAll(tasks);
            await noSqlCommand.EndBatch();

            var noSqlCommand2 = noSqlConnection.GetNoSqlCommand(QueryFiles.CreateNoSql);
            
            noSqlCommand2.Parameters.AddWithValue(pn.Id, file.Id);
            noSqlCommand2.Parameters.AddWithValue(pn.SourceMachineId, file.SourceMachineId);
            noSqlCommand2.Parameters.AddWithValue(pn.OriginalFilePath, file.OriginalFilePath);
            noSqlCommand2.Parameters.AddWithValue(pn.InsertedOn, file.InsertedOn);
            noSqlCommand2.Parameters.AddWithValue(pn.LastFileUpdate, file.LastFileUpdate!);
            noSqlCommand2.Parameters.AddWithValue(pn.IsCurrent, file.IsCurrent);
            noSqlCommand2.Parameters.AddWithValue(pn.Metadata, file.Metadata!.ToJsonString());
            await noSqlCommand2.ExecuteRowSet();
            return file;
        }

        public async Task<Models.File?> Update(Guid id, UpdateFileRequest request)
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.UpdateSql);
            sqlCommand.Parameters.AddWithValue(pn.Id, id);
            sqlCommand.Parameters.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow.AdjustPrecision());
            sqlCommand.Parameters.AddWithValue(pn.LastFileUpdate, request.LastFileUpdate.AdjustPrecision().ToNullableValueForSql());
            sqlCommand.Parameters.AddWithValue(pn.Metadata, NpgsqlTypes.NpgsqlDbType.Json, request.Metadata.ToNullableValueForSql()?.ToJsonString()!);
            await using var reader = await sqlCommand.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            var file = reader.ToFile();
            var noSqlConnection = GetNoSqlConnection();
            var noSqlCommand = noSqlConnection.GetNoSqlCommand(QueryFiles.UpdateNoSql);

            noSqlCommand.Parameters.AddWithValue(pn.Id, file.Id);
            noSqlCommand.Parameters.AddWithValue(pn.UpdatedOn, file.UpdatedOn!);
            noSqlCommand.Parameters.AddWithValue(pn.LastFileUpdate, file.LastFileUpdate!);
            noSqlCommand.Parameters.AddWithValue(pn.Metadata, file.Metadata!.ToJsonString());
            await noSqlCommand.ExecuteRowSet();
            return file;
        }

        public async Task<Models.File?> Delete(Guid id)
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.DeleteSql);
            sqlCommand.Parameters.AddWithValue(pn.Id, id);
            await using var reader = await sqlCommand.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            var file = reader.ToFile();
            var noSqlConnection = GetNoSqlConnection();
            var noSqlCommand = noSqlConnection.GetNoSqlCommand(QueryFiles.DeleteNoSql);

            noSqlCommand.Parameters.AddWithValue(pn.Id, id);
            await noSqlCommand.ExecuteRowSet();
            return file;
        }

        public async Task<List<Models.File>> DeleteHistoryBySourceMachineId(int sourceMachineId, string originalFilePath)
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.DeleteHistorySql);
            sqlCommand.Parameters.AddWithValue(pn.SourceMachineId, sourceMachineId);
            sqlCommand.Parameters.AddWithValue(pn.OriginalFilePath, originalFilePath);
            await using var reader = await sqlCommand.ExecuteReaderAsync();
            
            var files = await reader.ToFiles();
            var noSqlConnection = GetNoSqlConnection();

            NoSqlCommand noSqlCommand = noSqlConnection.GetNoSqlCommand(
                QueryFiles.DeleteNoSql, _scyllaSettings.MaxBatchsize)!;

            noSqlCommand.BeginBatch();
            
            var tasks = files.Select(file => noSqlCommand.AddQuery(file.Id));
            
            await Task.WhenAll(tasks);
            await noSqlCommand.EndBatch();
            return files;
        }
    }
}