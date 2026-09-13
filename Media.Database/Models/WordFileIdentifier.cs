namespace Media.Database.Models;

/// <summary>
/// The ordering-relevant identity of one word/file pairing -- just enough to compute a keyset
/// pagination cursor and to hydrate the full row later (see IWordRepository.GetByIds), without
/// paying for a full <see cref="ViewWordFiles"/> row when it isn't going to be rendered (e.g. a
/// look-ahead page's own identifiers, only ever used for their cursor value).
/// </summary>
public class WordFileIdentifier
{
    /// <summary>Gets or sets the unique identifier of the word.</summary>
    public int WordId { get; set; }

    /// <summary>Gets or sets the unique identifier of the file.</summary>
    public Guid FileId { get; set; }

    /// <summary>Gets or sets the word text.</summary>
    public string Word { get; set; } = string.Empty;

    /// <summary>Gets or sets the original path of the file.</summary>
    public string OriginalFilePath { get; set; } = string.Empty;
}
