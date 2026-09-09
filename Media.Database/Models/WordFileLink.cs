namespace Media.Database.Models;

/// <summary>
/// A word currently linked to a file, with the origin it was indexed under.
/// </summary>
/// <param name="WordId">The word's unique identifier.</param>
/// <param name="Word">The word text.</param>
/// <param name="Origin">The origin the word was indexed under for this file.</param>
public sealed record WordFileLink(int WordId, string Word, WordOrigin Origin);
