using System.Text.Json.Serialization;

namespace Media.Database.Models
{
    /// <summary>
    /// Enumerates the actions that can be performed against a word record.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum WordProducerActions
    {
        /// <summary>
        /// Add a new record.
        /// </summary>
        Add,

        /// <summary>
        /// Insert or update a record.
        /// </summary>
        Upsert,

        /// <summary>
        /// Update an existing record.
        /// </summary>
        Update,

        /// <summary>
        /// Delete a record.
        /// </summary>
        Delete
    }
}
