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

        [JsonPropertyName("tests")]
        public List<TestAssertion>? Tests { get; set; }

        [JsonPropertyName("timeout")]
        public int Timeout { get; set; } = 30000; // 30 seconds default timeout
    }
}
