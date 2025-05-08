using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace Apify.Models
{
    // Class to support the legacy test format where tests were grouped
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | 
                             DynamicallyAccessedMemberTypes.PublicFields | 
                             DynamicallyAccessedMemberTypes.PublicMethods |
                             DynamicallyAccessedMemberTypes.PublicConstructors)]
    public class TestGroup
    {
        [Newtonsoft.Json.JsonConstructor]
        public TestGroup()
        {
            Name = string.Empty;
        }
        
        [JsonPropertyName("name")]
        public string Name { get; set; }
        
        [JsonPropertyName("description")]
        public string? Description { get; set; }
        
        [JsonPropertyName("assertions")]
        public List<TestDefinition>? Assertions { get; set; }
    }
    
    // Class to represent the assertion object within a test group
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | 
                             DynamicallyAccessedMemberTypes.PublicFields | 
                             DynamicallyAccessedMemberTypes.PublicMethods |
                             DynamicallyAccessedMemberTypes.PublicConstructors)]
    public class TestDefinition
    {
        [Newtonsoft.Json.JsonConstructor]
        public TestDefinition()
        {
            Type = string.Empty;
        }
        
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        [JsonPropertyName("property")]
        public string? Property { get; set; }
        
        [JsonPropertyName("value")]
        public string? Value { get; set; }
        
        [JsonPropertyName("exists")]
        public bool? Exists { get; set; }
    }
}