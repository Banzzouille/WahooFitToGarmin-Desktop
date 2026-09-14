using System.Text.Json.Serialization;

namespace WahooFitToGarmin_Desktop.Core.GARMIN.Dto.Garmin
{
    // Every reference member here is nullable by design: these types are
    // populated by deserialization, and the service is free to omit any of them.
    public class UploadResponse
    {
        [JsonPropertyName("detailedImportResult")]
        public DetailedImportResult? DetailedImportResult { get; set; }
    }

    public class DetailedImportResult
    {
        public UploadUuid? uploadUuid { get; set; }
        public int owner { get; set; }
        public int fileSize { get; set; }
        public int processingTime { get; set; }
        public string? creationDate { get; set; }
        public object? ipAddress { get; set; }
        public string? fileName { get; set; }
        public object? report { get; set; }
        public List<Success>? successes { get; set; }
        public List<Failure>? failures { get; set; }
    }

    public class Success
    {
        [JsonPropertyName("externalId")]
        public object? ExternalId { get; set; }

        [JsonPropertyName("internalId")]
        public object? InternalId { get; set; }

        [JsonPropertyName("messages")]
        public List<Messages>? Messages { get; set; }
    }

    public class Failure
    {
        [JsonPropertyName("externalId")]
        public object? ExternalId { get; set; }

        [JsonPropertyName("internalId")]
        public object? InternalId { get; set; }

        [JsonPropertyName("messages")]
        public List<Messages>? Messages { get; set; }
    }

    public class Messages
    {
        [JsonPropertyName("code")]
        public int? Code { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    public class UploadUuid
    {
        public string? uuid { get; set; }
    }
}
