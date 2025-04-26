using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace APITester.Models
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | 
                               DynamicallyAccessedMemberTypes.PublicFields |
                               DynamicallyAccessedMemberTypes.PublicConstructors)]
    public class TestEnvironmentConfig
    {
        // Default constructor
        public TestEnvironmentConfig()
        {
            Name = "Default";
            Environments = new List<TestEnvironment>();
            DefaultEnvironment = "Development";
        }

        // Parameterized constructor for AOT compatibility
        [JsonConstructor]
        public TestEnvironmentConfig(
            [JsonProperty("Name")] string name,
            [JsonProperty("Description")] string? description,
            [JsonProperty("Environments")] List<TestEnvironment> environments,
            [JsonProperty("DefaultEnvironment")] string? defaultEnvironment)
        {
            Name = name ?? "Default";
            Description = description;
            Environments = environments ?? new List<TestEnvironment>();
            DefaultEnvironment = defaultEnvironment ?? "Development";
        }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Description")]
        public string? Description { get; set; }

        [JsonProperty("Environments")]
        public List<TestEnvironment> Environments { get; set; }

        [JsonProperty("DefaultEnvironment")]
        public string? DefaultEnvironment { get; set; }
    }
}