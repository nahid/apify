
using Newtonsoft.Json;

namespace Apify.Models
{
    public class PostmanCollection
    {
        [JsonProperty("info")]
        public PostmanInfo? Info { get; set; }

        [JsonProperty("item")]
        public PostmanItem[]? Items { get; set; }

        [JsonProperty("auth")]
        public PostmanAuth? Auth { get; set; }
    }

    public class PostmanAuth
    {
        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("bearer")]
        public PostmanAuthDetails[]? Bearer { get; set; }

        [JsonProperty("basic")]
        public PostmanAuthDetails[]? Basic { get; set; }
    }

    public class PostmanAuthDetails
    {
        [JsonProperty("key")]
        public string? Key { get; set; }

        [JsonProperty("value")]
        public string? Value { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }
    }

    public class PostmanInfo
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
    }

    public class PostmanItem
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("item")]
        public PostmanItem[]? Items { get; set; }

        [JsonProperty("request")]
        public PostmanRequest? Request { get; set; }
    }

    public class PostmanRequest
    {
        [JsonProperty("method")]
        public string? Method { get; set; }

        [JsonProperty("header")]
        public PostmanHeader[]? Headers { get; set; }

        [JsonProperty("body")]
        public PostmanBody? Body { get; set; }

        [JsonProperty("url")]
        public PostmanUrl? Url { get; set; }
    }

    public class PostmanHeader
    {
        [JsonProperty("key")]
        public string? Key { get; set; }

        [JsonProperty("value")]
        public string? Value { get; set; }
    }

    public class PostmanBody
    {
        [JsonProperty("mode")]
        public string? Mode { get; set; }

        [JsonProperty("raw")]
        public string? Raw { get; set; }

        [JsonProperty("formdata")]
        public PostmanFormData[]? FormData { get; set; }
    }

    public class PostmanFormData
    {
        [JsonProperty("key")]
        public string? Key { get; set; }

        [JsonProperty("value")]
        public string? Value { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }
    }

    public class PostmanUrl
    {
        [JsonProperty("raw")]
        public string? Raw { get; set; }
    }

    public class PostmanEnvironment
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("values")]
        public PostmanEnvironmentVariable[]? Values { get; set; }
    }

    public class PostmanEnvironmentVariable
    {
        [JsonProperty("key")]
        public string? Key { get; set; }

        [JsonProperty("value")]
        public string? Value { get; set; }

        [JsonProperty("enabled")]
        public bool Enabled { get; set; }
    }
}
