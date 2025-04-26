using System.Collections.Generic;

namespace APITester.Models
{
    public class ConfigurationProfile
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<TestEnvironment> Environments { get; set; } = new List<TestEnvironment>();
        public string? DefaultEnvironment { get; set; }
    }
}