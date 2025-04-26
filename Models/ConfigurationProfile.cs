using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace APITester.Models
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | 
                               DynamicallyAccessedMemberTypes.PublicFields |
                               DynamicallyAccessedMemberTypes.PublicConstructors)]
    public class ConfigurationProfile
    {
        // This constructor is needed for Native AOT compatibility with JSON deserialization
        [JsonConstructor]
        public ConfigurationProfile()
        {
            Name = string.Empty;
            Environments = new List<TestEnvironment>();
        }

        public string Name { get; set; }
        public string? Description { get; set; }
        public List<TestEnvironment> Environments { get; set; }
        public string? DefaultEnvironment { get; set; }
    }
}