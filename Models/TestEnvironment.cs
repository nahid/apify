using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace APITester.Models
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public class TestEnvironment
    {
        // Default constructor for general use
        public TestEnvironment()
        {
            Name = string.Empty;
            Variables = new Dictionary<string, string>();
            Description = string.Empty;
        }

        // Parameterized constructor for creating environments programmatically
        public TestEnvironment(string name, Dictionary<string, string> variables, string? description)
        {
            Name = name;
            Variables = variables ?? new Dictionary<string, string>();
            Description = description;
        }
        
        // Special constructor for Newtonsoft.Json deserialization
        [JsonConstructor]
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private TestEnvironment(bool _) : this()
        {
            // This constructor is used by Newtonsoft.Json during deserialization
            // The properties will be populated through property setters
        }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Variables")]
        public Dictionary<string, string> Variables { get; set; }

        [JsonProperty("Description")]
        public string? Description { get; set; }
    }
}