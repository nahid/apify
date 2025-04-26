using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace APITester.Models
{
    // Add DynamicallyAccessedMembers attribute to preserve public members for reflection
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | 
                               DynamicallyAccessedMemberTypes.PublicFields | 
                               DynamicallyAccessedMemberTypes.PublicMethods |
                               DynamicallyAccessedMemberTypes.PublicConstructors)]
    public class ApiDefinition
    {
        // This constructor is needed for Native AOT compatibility with JSON deserialization
        [JsonConstructor]
        public ApiDefinition()
        {
            Name = string.Empty;
            Uri = string.Empty;
            Method = "GET";
            Timeout = 30000;
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

        [JsonPropertyName("payload")]
        [System.Text.Json.Serialization.JsonIgnore] // This is ignored in system.text.json serialization
        private string? _payloadString;

        [JsonPropertyName("payloadObject")]
        [System.Text.Json.Serialization.JsonIgnore] // This is ignored in system.text.json serialization
        private object? _payloadObject;

        // This property is used by Newtonsoft.Json for serialization/deserialization
        [Newtonsoft.Json.JsonProperty("payload")]
        public object? Payload 
        { 
            get => PayloadType == PayloadType.Json ? (object?)_payloadObject : _payloadString;
            set 
            {
                if (value is JObject || value is JArray)
                {
                    _payloadObject = value;
                    PayloadType = PayloadType.Json;
                }
                else if (value is string str)
                {
                    _payloadString = str;
                    
                    // Try to parse as JSON if payloadType is set to JSON
                    if (PayloadType == PayloadType.Json)
                    {
                        try
                        {
                            _payloadObject = JToken.Parse(str);
                        }
                        catch
                        {
                            // If not valid JSON, keep as string
                            _payloadString = str;
                        }
                    }
                }
                else if (value != null)
                {
                    _payloadObject = value;
                    PayloadType = PayloadType.Json;
                }
            }
        }

        // Methods to get the payload in different formats
        public string? GetPayloadAsString()
        {
            if (_payloadString != null)
                return _payloadString;
            
            if (_payloadObject != null)
                return Newtonsoft.Json.JsonConvert.SerializeObject(_payloadObject);
            
            return null;
        }

        public T? GetPayloadAsObject<T>()
        {
            if (_payloadObject != null)
            {
                if (_payloadObject is JToken jToken)
                {
                    return jToken.ToObject<T>();
                }
                
                try
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(
                        Newtonsoft.Json.JsonConvert.SerializeObject(_payloadObject));
                }
                catch
                {
                    return default;
                }
            }
            
            if (_payloadString != null)
            {
                try
                {
                    return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(_payloadString);
                }
                catch
                {
                    return default;
                }
            }
            
            return default;
        }

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

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | 
                               DynamicallyAccessedMemberTypes.PublicFields |
                               DynamicallyAccessedMemberTypes.PublicConstructors)]
    public class FileUpload
    {
        // This constructor is needed for Native AOT compatibility with JSON deserialization
        [JsonConstructor]
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
}
