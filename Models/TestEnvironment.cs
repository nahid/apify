using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace APITester.Models
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | 
                               DynamicallyAccessedMemberTypes.PublicFields)]
    public class TestEnvironment
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
        public string? Description { get; set; }
    }
}