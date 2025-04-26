using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using APITester.Models;
using Newtonsoft.Json;
using System.Linq;

namespace APITester.Services
{
    public class EnvironmentService
    {
        private const string EnvironmentFolderName = "Environments";
        private static readonly Regex VariablePattern = new Regex(@"{{(.+?)}}", RegexOptions.Compiled);
        
        private TestEnvironment? _currentEnvironment;
        
        public TestEnvironment? CurrentEnvironment => _currentEnvironment;

        public List<ConfigurationProfile> LoadConfigurationProfiles()
        {
            var profiles = new List<ConfigurationProfile>();
            
            if (!Directory.Exists(EnvironmentFolderName))
            {
                Directory.CreateDirectory(EnvironmentFolderName);
                return profiles;
            }
            
            foreach (var file in Directory.GetFiles(EnvironmentFolderName, "*.json"))
            {
                try
                {
                    var content = File.ReadAllText(file);
                    var profile = JsonConvert.DeserializeObject<ConfigurationProfile>(content);
                    if (profile != null)
                    {
                        profiles.Add(profile);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading configuration profile from {file}: {ex.Message}");
                }
            }
            
            return profiles;
        }
        
        public bool SetCurrentEnvironment(string profileName, string? environmentName = null)
        {
            var profiles = LoadConfigurationProfiles();
            var profile = profiles.FirstOrDefault(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase));
            
            if (profile == null)
            {
                Console.WriteLine($"Configuration profile '{profileName}' not found.");
                return false;
            }
            
            string envName = environmentName ?? profile.DefaultEnvironment ?? profile.Environments.FirstOrDefault()?.Name ?? string.Empty;
            
            if (string.IsNullOrEmpty(envName))
            {
                Console.WriteLine($"No environment specified or found in profile '{profileName}'.");
                return false;
            }
            
            var environment = profile.Environments.FirstOrDefault(e => e.Name.Equals(envName, StringComparison.OrdinalIgnoreCase));
            
            if (environment == null)
            {
                Console.WriteLine($"Environment '{envName}' not found in profile '{profileName}'.");
                return false;
            }
            
            _currentEnvironment = environment;
            Console.WriteLine($"Using environment: {environment.Name}");
            return true;
        }
        
        public string ApplyEnvironmentVariables(string input)
        {
            if (_currentEnvironment == null)
                return input;
                
            return VariablePattern.Replace(input, match =>
            {
                var variableName = match.Groups[1].Value.Trim();
                if (_currentEnvironment.Variables.TryGetValue(variableName, out var value))
                {
                    return value;
                }
                return match.Value; // Keep the original {{variable}} if not found
            });
        }
        
        public ApiDefinition ApplyEnvironmentVariables(ApiDefinition apiDefinition)
        {
            if (_currentEnvironment == null)
                return apiDefinition;
                
            var modifiedApi = new ApiDefinition
            {
                Name = ApplyEnvironmentVariables(apiDefinition.Name),
                Uri = ApplyEnvironmentVariables(apiDefinition.Uri),
                Method = apiDefinition.Method,
                Payload = apiDefinition.Payload != null ? ApplyEnvironmentVariables(apiDefinition.Payload) : null,
                Tests = apiDefinition.Tests?.ToList() ?? new List<TestAssertion>()
            };
            
            if (apiDefinition.Headers != null)
            {
                modifiedApi.Headers = new Dictionary<string, string>();
                foreach (var header in apiDefinition.Headers)
                {
                    modifiedApi.Headers[header.Key] = ApplyEnvironmentVariables(header.Value);
                }
            }
            
            return modifiedApi;
        }
        
        public void CreateDefaultEnvironmentFile()
        {
            if (!Directory.Exists(EnvironmentFolderName))
            {
                Directory.CreateDirectory(EnvironmentFolderName);
            }
            
            var defaultProfile = new ConfigurationProfile
            {
                Name = "Default",
                Description = "Default configuration profile",
                DefaultEnvironment = "Development",
                Environments = new List<TestEnvironment>
                {
                    new TestEnvironment
                    {
                        Name = "Development",
                        Description = "Development environment",
                        Variables = new Dictionary<string, string>
                        {
                            { "baseUrl", "https://api.example.com/dev" },
                            { "apiKey", "dev-api-key" },
                            { "timeout", "30000" }
                        }
                    },
                    new TestEnvironment
                    {
                        Name = "Production",
                        Description = "Production environment",
                        Variables = new Dictionary<string, string>
                        {
                            { "baseUrl", "https://api.example.com" },
                            { "apiKey", "prod-api-key" },
                            { "timeout", "10000" }
                        }
                    }
                }
            };
            
            var filePath = Path.Combine(EnvironmentFolderName, "default.json");
            var json = JsonConvert.SerializeObject(defaultProfile, Formatting.Indented);
            File.WriteAllText(filePath, json);
            
            Console.WriteLine($"Created default environment file at {filePath}");
        }
    }
}