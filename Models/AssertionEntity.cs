using System.Text.Json.Serialization;

namespace Apify.Models
{
    public class AssertionEntity
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        
        [JsonPropertyName("case")]
        public string Case { get; set; } = string.Empty;
    }
}
