using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Apify.Models;


public class AuthorizationSchema
{
    [JsonProperty("type")]
    public AuthorizationType? Type { get; set; }
        
    [JsonProperty("token")]
    public string? Token { get; set; }
}

    
[JsonConverter(typeof(StringEnumConverter))]
public enum AuthorizationType
{
    [JsonProperty("none")]
    [EnumMember(Value = "none")]
    None,
        
    [JsonProperty("bearer")]
    [EnumMember(Value = "bearer")]
    Bearer,
        
    [JsonProperty("basic")]
    [EnumMember(Value = "basic")]
    Basic,
        
    [JsonProperty("apiKey")]
    [EnumMember(Value = "apiKey")]
    ApiKey
}