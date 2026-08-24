using System.Text.Json.Serialization;

namespace Media.Database.Models
{
    /// <summary>
    /// Enumerates the actions that can be performed by a Kafka producer.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum KafkaProducerActions
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
