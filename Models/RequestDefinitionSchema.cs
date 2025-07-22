using Newtonsoft.Json;
using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace Apify.Models
{
    // Add DynamicallyAccessedMembers attribute to preserve public members for reflection
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | 
                               DynamicallyAccessedMemberTypes.PublicFields | 
                               DynamicallyAccessedMemberTypes.PublicMethods |
                               DynamicallyAccessedMemberTypes.PublicConstructors)]
    [method: Newtonsoft.Json.JsonConstructor]
    public class RequestDefinitionSchema()
    {
        // Constructor for JSON deserialization

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; } = string.Empty;

        [JsonProperty("method")]
        public string Method { get; set; } = "GET";

        [JsonProperty("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        [JsonProperty("body")]
        public Body? Body { get; set; }
        
        [JsonProperty("payloadType")]
        public PayloadContentType? PayloadType { get; set; }

        [JsonProperty("tests")]
        public List<AssertionEntity>? Tests { get; set; }

        [JsonProperty("timeout")]
        public int Timeout { get; set; } = 30000; // 30 seconds default timeout
        
        [JsonProperty("variables")]
        public Dictionary<string, string>? Variables { get; set; } = new Dictionary<string, string>();

        [JsonProperty("tags")]
        public List<string>? Tags { get; set; }
        
        public string? GetPayloadAsString()
        {
            var payload = GetBodyPayload();

            if (payload == null)
                return null;
            
     
            return Newtonsoft.Json.JsonConvert.SerializeObject(payload);
        }
        public PayloadContentType GetPayloadType()
        {
            if (PayloadType == null)
            {
                return  PayloadContentType.None ;
            }
            
            if (Body == null)
            {
                return PayloadContentType.None;
            }
            
            if (Body.Binary != null)
            {
                return PayloadContentType.Binary;
            }
            
            if (Body.FormData != null)
            {
                return PayloadContentType.FormData;
            }
            
            if (Body.Json != null)
            {
                return PayloadContentType.Json;
            }
            
            if (Body.Multipart != null)
            {
                return PayloadContentType.Multipart;
            }
            
            if (Body.Text != null)
            {
                return PayloadContentType.Text;
            }
            

            return PayloadContentType.None;
        }
        
        
        public object? GetBodyPayload()
        {
            if (PayloadType == PayloadContentType.Json)
            {
                return Body?.Json;
            }
            
            if (PayloadType == PayloadContentType.Text)
            {
                return Body?.Text;
            }
            
            if (PayloadType == PayloadContentType.FormData)
            {
                return Body?.FormData;
            }
            
            if (PayloadType == PayloadContentType.Multipart)
            {
                return Body?.Multipart;
            } 
            
            if (PayloadType == PayloadContentType.Binary)
            {
                return Body?.Binary;
            }
            
            if (PayloadType == PayloadContentType.None)
            {
                // If no payload type is set, return null
                return "";
            }
       
            
            return null;
        }
    }

    public enum PayloadContentType
    {
        [JsonProperty("none")]
        None,
        
        [JsonProperty("text")]
        Text,
        
        [JsonProperty("json")]
        Json,
        
        [JsonProperty("formData")]
        FormData,
        
        [JsonProperty("multipart")]
        Multipart,
        
        [JsonProperty("binary")]
        Binary
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | 
                               DynamicallyAccessedMemberTypes.PublicFields |
                               DynamicallyAccessedMemberTypes.PublicConstructors)]
    
    public class MultipartData
    {
        [JsonProperty("name")]
        public string? Name { get; set; }
        
        [JsonProperty("content")]
        public string? Content { get; set; }
    }

    public class Body
    {
        [JsonProperty("json")]
        public JToken? Json { get; set; }
        
        [JsonProperty("text")]
        public string? Text { get; set; }
        
        [JsonProperty("formData")]
        public Dictionary<string, string>? FormData { get; set; }
        
        [JsonProperty("multipart")]
        public List<MultipartData>? Multipart { get; set; }
        
        [JsonProperty("binary")]
        public string? Binary { get; set; }
        
    }
}
