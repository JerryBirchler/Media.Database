namespace Media.Database.Repositories;

/// <summary>
/// Encrypts and decrypts a saved search list's name and payload.
///
/// An interface rather than a direct call to <c>Encryptor</c> for two reasons. The key belongs to
/// the host application, so Media.Database never sees it or knows where it came from. And
/// <c>Encryptor</c> is static, which would otherwise drag real AES through every repository test
/// -- this is the seam that keeps those tests about storage.
///
/// The repository takes plaintext and encrypts on the way in, so there is no path through it that
/// stores a list in the clear by accident.
/// </summary>
public interface ISearchListCipher
{
    /// <summary>Encrypts a value for storage.</summary>
    string Encrypt(string plaintext);

    /// <summary>
    /// Decrypts a stored value.
    /// </summary>
    /// <exception cref="System.Security.Cryptography.CryptographicException">
    /// The key is wrong or the value is corrupt. AES-GCM authenticates, so this fails closed
    /// rather than returning plausible nonsense.
    /// </exception>
    string Decrypt(string ciphertext);
}
