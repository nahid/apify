using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apify.Models
{
    // Add DynamicallyAccessedMembers attribute to preserve public members for reflection
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | 
                               DynamicallyAccessedMemberTypes.PublicFields | 
                               DynamicallyAccessedMemberTypes.PublicMethods |
                               DynamicallyAccessedMemberTypes.PublicConstructors)]
    public class ApiDefinition
    {
        // Constructor for JSON deserialization
        [Newtonsoft.Json.JsonConstructor]
        public ApiDefinition()
        {
            Name = string.Empty;
            Uri = string.Empty;
            Method = "GET";
            Timeout = 30000;
            Variables = new Dictionary<string, string>();
        }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("method")]
        public string Method { get; set; } = "GET";

        [JsonPropertyName("headers")]
        public Dictionary<string, string>? Headers { get; set; }

        [JsonPropertyName("body")]
        public Body? Body { get; set; } = null;
        
        [JsonPropertyName("payloadType")]
        public PayloadContentType? PayloadType { get; set; } = null;

        [JsonPropertyName("tests")]
        public List<AssertionEntity>? Tests { get; set; }

        [JsonPropertyName("timeout")]
        public int Timeout { get; set; } = 30000; // 30 seconds default timeout
        
        [JsonPropertyName("variables")]
        public Dictionary<string, string>? Variables { get; set; }
        
        [JsonPropertyName("tags")]
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
        [JsonPropertyName("none")]
        None,
        
        [JsonPropertyName("text")]
        Text,
        
        [JsonPropertyName("json")]
        Json,
        
        [JsonPropertyName("formData")]
        FormData,
        
        [JsonPropertyName("multipart")]
        Multipart,
        
        [JsonPropertyName("binary")]
        Binary
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | 
                               DynamicallyAccessedMemberTypes.PublicFields |
                               DynamicallyAccessedMemberTypes.PublicConstructors)]
    public class FileUpload
    {
        // This constructor is needed for Native AOT compatibility with JSON deserialization
        [Newtonsoft.Json.JsonConstructor]
        public FileUpload()
        {
            Name = string.Empty;
            FieldName = string.Empty;
            FilePath = string.Empty;
            ContentType = "application/octet-stream";
        }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("fieldName")]
        public string FieldName { get; set; } = string.Empty;

        [JsonPropertyName("filePath")]
        public string FilePath { get; set; } = string.Empty;

        [JsonPropertyName("contentType")]
        public string ContentType { get; set; } = "application/octet-stream";
    }
    
    public class MultipartData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class Body
    {
        [JsonPropertyName("json")]
        public JToken? Json { get; set; } = null;
        
        [JsonPropertyName("text")]
        public string? Text { get; set; } = null;
        
        [JsonPropertyName("formData")]
        public Dictionary<string, string>? FormData { get; set; } = null;
        
        [JsonPropertyName("multipart")]
        public List<MultipartData>? Multipart { get; set; } = null;
        
        [JsonPropertyName("binary")]
        public string? Binary { get; set; } = null;
        
    }
}
