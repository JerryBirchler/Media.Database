using System.Text.Json.Serialization;

namespace Media.Database.Models;

/// <summary>
/// The part of speech a word was tagged as when it was extracted.
///
/// These are the Universal Dependencies tags, which is what Catalyst's own <c>PartOfSpeech</c>
/// enum already is. Mirrored here rather than persisting Catalyst's type directly: the schema
/// should not be coupled to a third-party library's versioning, and UD is the vocabulary that
/// Azure Language, spaCy and the rest speak too, so a different tagger later is a mapping change
/// and not a migration.
///
/// Values are explicit because they are persisted. <c>Words.WordType</c> holds the number and the
/// <c>WordTypes</c> table seeds these exact ids, so reordering the members would silently
/// reinterpret every stored row. The sibling <see cref="WordOrigin"/> leaves its values implicit;
/// this deliberately does not.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WordType
{
    /// <summary>Not classified. The default, so an unset row never reads as a noun by accident.</summary>
    None = 0,

    /// <summary>Adjective. Indexed: "vintage", "red".</summary>
    Adjective = 1,

    /// <summary>Adposition, such as "of" or "in". Not indexed.</summary>
    Adposition = 2,

    /// <summary>Adverb. Indexed: "slowly".</summary>
    Adverb = 3,

    /// <summary>Auxiliary verb, such as "is" or "have". Not indexed.</summary>
    Auxiliary = 4,

    /// <summary>Coordinating conjunction, such as "and". Not indexed.</summary>
    CoordinatingConjunction = 5,

    /// <summary>
    /// The deprecated Universal Dependencies v1 conjunction tag, which Catalyst still carries.
    /// Present so the mapping is total; nothing is expected to produce it.
    /// </summary>
    Conjunction = 6,

    /// <summary>Determiner, such as "the". Not indexed.</summary>
    Determiner = 7,

    /// <summary>Interjection. Not indexed.</summary>
    Interjection = 8,

    /// <summary>Common noun. Indexed.</summary>
    Noun = 9,

    /// <summary>Numeral. Indexed.</summary>
    Numeral = 10,

    /// <summary>Particle. Not indexed.</summary>
    Particle = 11,

    /// <summary>Pronoun. Not indexed.</summary>
    Pronoun = 12,

    /// <summary>
    /// Proper noun. Indexed, and the only type for which casing is preserved.
    /// <see cref="Words.IsProperName"/> is this and nothing else.
    /// </summary>
    ProperNoun = 13,

    /// <summary>Punctuation. Not indexed.</summary>
    Punctuation = 14,

    /// <summary>Subordinating conjunction. Not indexed.</summary>
    SubordinatingConjunction = 15,

    /// <summary>Symbol. Not indexed.</summary>
    Symbol = 16,

    /// <summary>Verb. Indexed: "swimming", "graduating".</summary>
    Verb = 17,

    /// <summary>Anything the tagger could not place. Not indexed.</summary>
    Other = 18
}
