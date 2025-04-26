using System.Text.Json.Serialization;

namespace APITester.Models
{
    public class ApiDefinition
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("method")]
        public string Method { get; set; } = "GET";

        [JsonPropertyName("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        [JsonPropertyName("payload")]
        public string? Payload { get; set; }

        [JsonPropertyName("payloadType")]
        public PayloadType PayloadType { get; set; } = PayloadType.Json;

        [JsonPropertyName("files")]
        public List<FileUpload>? Files { get; set; }

        [JsonPropertyName("tests")]
        public List<TestAssertion>? Tests { get; set; }

        [JsonPropertyName("timeout")]
        public int Timeout { get; set; } = 30000; // 30 seconds default timeout
    }

    public enum PayloadType
    {
        [JsonPropertyName("none")]
        None,
        
        [JsonPropertyName("text")]
        Text,
        
        [JsonPropertyName("json")]
        Json,
        
        [JsonPropertyName("formData")]
        FormData
    }

    public class FileUpload
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("fieldName")]
        public string FieldName { get; set; } = string.Empty;

        [JsonPropertyName("filePath")]
        public string FilePath { get; set; } = string.Empty;

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = "application/octet-stream";
    }
}
