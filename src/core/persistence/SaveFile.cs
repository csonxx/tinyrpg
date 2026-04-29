using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core.Persistence
{
    /// <summary>
    /// Data class representing the save file format on disk.
    /// </summary>
    [Serializable]
    public sealed class SaveFile
    {
        private const string CURRENT_VERSION = "1.0";

        [JsonPropertyName("version")]
        public string Version { get; set; }

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }

        [JsonPropertyName("chapterIndex")]
        public int ChapterIndex { get; set; }

        [JsonPropertyName("sceneId")]
        public string SceneId { get; set; }

        [JsonPropertyName("nsmState")]
        public string NsmState { get; set; }

        [JsonPropertyName("nsmHash")]
        public string NsmHash { get; set; }

        [JsonPropertyName("playTimeSeconds")]
        public int PlayTimeSeconds { get; set; }

        [JsonPropertyName("choiceCount")]
        public int ChoiceCount { get; set; }

        // JSON serialization options used for NSM state
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        /// <summary>
        /// Creates a new SaveFile with the current version and timestamp.
        /// </summary>
        public SaveFile()
        {
            Version = CURRENT_VERSION;
            Timestamp = DateTime.UtcNow.ToString("o");
        }

        /// <summary>
        /// Deserializes a SaveFile from JSON string.
        /// </summary>
        /// <param name="json">Raw JSON string</param>
        /// <returns>Parsed SaveFile instance</returns>
        /// <exception cref="ArgumentException">If JSON is null or empty</exception>
        /// <exception cref="JsonException">If JSON does not match expected format</exception>
        public static SaveFile FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON cannot be null or empty", nameof(json));

            return JsonSerializer.Deserialize<SaveFile>(json, JsonOptions)
                ?? throw new JsonException("Deserialized SaveFile was null");
        }

        /// <summary>
        /// Serializes this SaveFile to a JSON string.
        /// </summary>
        /// <returns>JSON string representation</returns>
        public string ToJson()
        {
            return JsonSerializer.Serialize(this, JsonOptions);
        }

        /// <summary>
        /// Returns true if the save file format version is compatible.
        /// </summary>
        public bool IsVersionCompatible()
        {
            return Version == CURRENT_VERSION;
        }

        /// <summary>
        /// Returns the formatted play time as "HH:mm:ss".
        /// </summary>
        public string FormattedPlayTime()
        {
            var ts = TimeSpan.FromSeconds(PlayTimeSeconds);
            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        /// <summary>
        /// Returns the timestamp as a DateTime (UTC).
        /// </summary>
        /// <returns>DateTime representation of timestamp</returns>
        /// <exception cref="FormatException">If timestamp is not valid ISO8601</exception>
        public DateTime TimestampAsDateTime()
        {
            return DateTime.Parse(Timestamp, null, System.Globalization.DateTimeStyles.RoundtripKind);
        }
    }
}
