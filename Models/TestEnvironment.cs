using System.Collections.Generic;
using Newtonsoft.Json;

namespace APITester.Models
{
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

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Variables")]
        public Dictionary<string, string> Variables { get; set; }

        [JsonProperty("Description")]
        public string? Description { get; set; }
    }
}