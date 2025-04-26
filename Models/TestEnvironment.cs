using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace APITester.Models
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | 
                               DynamicallyAccessedMemberTypes.PublicFields |
                               DynamicallyAccessedMemberTypes.PublicConstructors)]
    public class TestEnvironment
    {
        // This constructor is needed for Native AOT compatibility with JSON deserialization
        [Newtonsoft.Json.JsonConstructor]
        public TestEnvironment()
        {
            Name = string.Empty;
            Variables = new Dictionary<string, string>();
        }

        public string Name { get; set; }
        public Dictionary<string, string> Variables { get; set; }
        public string? Description { get; set; }
    }
}