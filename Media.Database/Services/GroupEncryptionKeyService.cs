using Media.Common.Helpers;
using Media.Common.Serialization;
using Media.Database.Models;
using Media.Database.Repositories;
using System.Security.Cryptography;

namespace Media.Database.Services;

/// <inheritdoc cref="IGroupEncryptionKeyService"/>
public class GroupEncryptionKeyService(
    IGroupShellRepository groupShellRepository,
    IGroupEncryptionKeyRepository groupEncryptionKeyRepository) : IGroupEncryptionKeyService
{
    private const int DekSizeBytes = 32;

    private readonly IGroupShellRepository _groupShellRepository = groupShellRepository;
    private readonly IGroupEncryptionKeyRepository _groupEncryptionKeyRepository = groupEncryptionKeyRepository;

    public async Task<(GroupShell Shell, string RawKey)> CreateShellWithDefaultKeyAsync()
    {
        var shell = await _groupShellRepository.CreateAsync();
        var rawKey = GroupEncryptionKeyGenerator.Generate();

        await GenerateAndStoreDekAsync(shell.GroupShellId, EncryptionDataCategory.UuidOrchestration, rawKey);

        return (shell, rawKey);
    }

    public async Task<byte[]?> UnwrapDekAsync(int groupShellId, EncryptionDataCategory dataCategory, string rawKey)
    {
        var key = await _groupEncryptionKeyRepository.GetActiveAsync(groupShellId, dataCategory);
        if (key is null)
            return null;

        return UnwrapDek(key.WrappedDek, rawKey);
    }

    public async Task<string> ResolvePiiFieldEncryptionKeyAsync(int groupShellId, string rawKey)
    {
        var existing = await _groupEncryptionKeyRepository.GetActiveAsync(groupShellId, EncryptionDataCategory.PiiMetadata);

        var dek = existing is not null
            ? UnwrapDek(existing.WrappedDek, rawKey)
            : await GenerateAndStoreDekAsync(groupShellId, EncryptionDataCategory.PiiMetadata, rawKey);

        return Convert.ToBase64String(dek);
    }

    public async Task<bool> HasEncryptedPiiAsync(int groupShellId)
        => await _groupEncryptionKeyRepository.GetActiveAsync(groupShellId, EncryptionDataCategory.PiiMetadata) is not null;

    public async Task<string> RegenerateAsync(int groupShellId)
    {
        var rawKey = GroupEncryptionKeyGenerator.Generate();

        // Must clear the way first: the unique partial index allows only one active key per
        // (shell, category).
        await _groupEncryptionKeyRepository.DeactivateAllAsync(groupShellId);
        await GenerateAndStoreDekAsync(groupShellId, EncryptionDataCategory.UuidOrchestration, rawKey);

        return rawKey;
    }

    private async Task<byte[]> GenerateAndStoreDekAsync(int groupShellId, EncryptionDataCategory dataCategory, string rawKey)
    {
        var dek = RandomNumberGenerator.GetBytes(DekSizeBytes);
        var wrappedDek = Encryptor.Encrypt(Convert.ToBase64String(dek), rawKey);

        await _groupEncryptionKeyRepository.CreateAsync(groupShellId, dataCategory, wrappedDek);

        return dek;
    }

    private static byte[] UnwrapDek(string wrappedDek, string rawKey)
    {
        var dekBase64 = Encryptor.Decrypt(wrappedDek, rawKey);
        return Convert.FromBase64String(dekBase64);
    }
}
