using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using APITester.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace APITester.Services
{
    public class EnvironmentService
    {
        private const string ConfigFileName = "apify-config.json";
        private static readonly Regex VariablePattern = new Regex(@"{{(.+?)}}", RegexOptions.Compiled);
        
        private TestEnvironment? _currentEnvironment;
        
        public TestEnvironment? CurrentEnvironment => _currentEnvironment;

        public List<ConfigurationProfile> LoadConfigurationProfiles()
        {
            var profiles = new List<ConfigurationProfile>();
            
            if (!File.Exists(ConfigFileName))
            {
                return profiles;
            }
            
            try
            {
                var content = File.ReadAllText(ConfigFileName);
                var profile = JsonConvert.DeserializeObject<ConfigurationProfile>(content);
                if (profile != null)
                {
                    profiles.Add(profile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration profile from {ConfigFileName}: {ex.Message}");
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
                Payload = ProcessPayload(apiDefinition.Payload),
                PayloadType = apiDefinition.PayloadType,
                Tests = new List<TestAssertion>()
            };
            
            if (apiDefinition.Headers != null)
            {
                modifiedApi.Headers = new Dictionary<string, string>();
                foreach (var header in apiDefinition.Headers)
                {
                    modifiedApi.Headers[header.Key] = ApplyEnvironmentVariables(header.Value);
                }
            }
            
            // Process test assertions and apply environment variables
            if (apiDefinition.Tests != null)
            {
                foreach (var test in apiDefinition.Tests)
                {
                    var modifiedTest = new TestAssertion
                    {
                        Name = test.Name,
                        Description = test.Description,
                        Assertion = ApplyEnvironmentVariables(test.Assertion),
                        AssertType = test.AssertType,
                        Property = test.Property,
                        ExpectedValue = test.ExpectedValue != null ? ApplyEnvironmentVariables(test.ExpectedValue) : null,
                        Expected = test.Expected
                    };
                    
                    modifiedApi.Tests.Add(modifiedTest);
                }
            }
            
            return modifiedApi;
        }
        
        // Process payload data based on type
        private object? ProcessPayload(object? payload)
        {
            if (payload == null)
                return null;
                
            if (payload is string strPayload)
            {
                return ApplyEnvironmentVariables(strPayload);
            }
            else if (payload is JObject jObject)
            {
                // Process JObject to apply environment variables
                var processed = ProcessJToken(jObject);
                return processed;
            }
            else if (payload is JArray jArray)
            {
                // Process JArray to apply environment variables
                var processed = ProcessJToken(jArray);
                return processed;
            }
            
            // For other payload types (like Dictionary), try serializing and deserializing
            try
            {
                var json = JsonConvert.SerializeObject(payload);
                var processedJson = ApplyEnvironmentVariables(json);
                return JsonConvert.DeserializeObject(processedJson);
            }
            catch
            {
                // If conversion fails, return the original payload
                return payload;
            }
        }
        
        // Process JToken (JObject or JArray) recursively to apply environment variables
        private JToken ProcessJToken(JToken token)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var prop in token.Children<JProperty>().ToList())
                    {
                        var value = ProcessJToken(prop.Value);
                        prop.Value = value;
                    }
                    break;
                case JTokenType.Array:
                    for (int i = 0; i < token.Count(); i++)
                    {
                        var item = token[i];
                        if (item != null)
                        {
                            token[i] = ProcessJToken(item);
                        }
                    }
                    break;
                case JTokenType.String:
                    // Apply environment variables to string values
                    var strValue = token.Value<string>();
                    if (strValue != null)
                    {
                        return new JValue(ApplyEnvironmentVariables(strValue));
                    }
                    break;
            }
            
            return token;
        }
        
        public void CreateDefaultEnvironmentFile()
        {
            try
            {
                // Check if config file already exists
                if (File.Exists(ConfigFileName))
                {
                    Console.WriteLine($"Configuration file '{ConfigFileName}' already exists.");
                    return;
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
                
                // Make sure we have a proper non-null object
                if (defaultProfile != null && defaultProfile.Environments != null)
                {
                    var json = JsonConvert.SerializeObject(defaultProfile, Formatting.Indented);
                    
                    // Verify the JSON is valid before writing to file
                    var testDeserialized = JsonConvert.DeserializeObject<ConfigurationProfile>(json);
                    if (testDeserialized == null || testDeserialized.Environments == null || testDeserialized.Environments.Count == 0)
                    {
                        Console.WriteLine("Error: Generated invalid configuration. Using fallback.");
                        // Create a minimal valid JSON as fallback
                        json = @"{
  ""Name"": ""Default"",
  ""Description"": ""Default configuration profile"",
  ""DefaultEnvironment"": ""Development"",
  ""Environments"": [
    {
      ""Name"": ""Development"",
      ""Description"": ""Development environment"",
      ""Variables"": {
        ""baseUrl"": ""https://api.example.com/dev"",
        ""timeout"": ""30000""
      }
    }
  ]
}";
                    }
                    
                    File.WriteAllText(ConfigFileName, json);
                }
                else
                {
                    Console.WriteLine("Error: Could not create configuration profile.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating default environment file: {ex.Message}");
            }
            
            Console.WriteLine($"Created configuration file at {ConfigFileName}");
        }
    }
}