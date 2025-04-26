using System.Collections.Generic;

namespace APITester.Models
{
    public class TestEnvironment
    {
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();
        public string? Description { get; set; }
    }
}