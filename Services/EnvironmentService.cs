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
        private string? _configFilePath;
        
        public TestEnvironment? CurrentEnvironment => _currentEnvironment;
        
        public EnvironmentService()
        {
            // Find the configuration file in the current directory or parent directories
            _configFilePath = FindConfigFile();
        }
        
        private string FindConfigFile()
        {
            // First check the current directory
            if (File.Exists(ConfigFileName))
            {
                Console.WriteLine($"Using configuration file: {Path.GetFullPath(ConfigFileName)}");
                return ConfigFileName;
            }
            
            // Try to find in parent directories (up to 3 levels)
            var currentDir = Directory.GetCurrentDirectory();
            for (int i = 0; i < 3; i++)
            {
                var parentDir = Directory.GetParent(currentDir);
                if (parentDir == null)
                    break;
                    
                currentDir = parentDir.FullName;
                var configPath = Path.Combine(currentDir, ConfigFileName);
                
                if (File.Exists(configPath))
                {
                    Console.WriteLine($"Using configuration file from parent directory: {configPath}");
                    return configPath;
                }
            }
            
            // If not found, default to the working directory
            Console.WriteLine($"Configuration file not found in current or parent directories. Using default path: {ConfigFileName}");
            return ConfigFileName;
        }

        public List<ConfigurationProfile> LoadConfigurationProfiles()
        {
            var profiles = new List<ConfigurationProfile>();
            
            // Use the discovered config file path
            if (!File.Exists(_configFilePath))
            {
                Console.WriteLine($"Configuration file not found at {_configFilePath}");
                return profiles;
            }
            
            try
            {
                var content = File.ReadAllText(_configFilePath);
                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) => 
                    {
                        Console.WriteLine($"JSON Error: {args.ErrorContext.Error.Message}");
                        args.ErrorContext.Handled = true;
                    },
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };
                
                var profile = JsonConvert.DeserializeObject<ConfigurationProfile>(content, settings);
                
                if (profile != null)
                {
                    profiles.Add(profile);
                }
                else
                {
                    // Create a default profile if deserialization returns null
                    Console.WriteLine("Creating default profile...");
                    var defaultProfile = CreateDefaultProfile();
                    profiles.Add(defaultProfile);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration profile from {_configFilePath}: {ex.Message}");
                
                // Create and add a default profile on error
                Console.WriteLine("No environment profiles found. Creating default profile...");
                var defaultProfile = CreateDefaultProfile();
                profiles.Add(defaultProfile);
            }
            
            return profiles;
        }
        
        private ConfigurationProfile CreateDefaultProfile()
        {
            return new ConfigurationProfile
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
                            { "baseUrl", "https://api.example.com" },
                            { "timeout", "30000" }
                        }
                    }
                }
            };
        }
        
        public bool SetCurrentEnvironment(string profileName, string? environmentName = null)
        {
            var profiles = LoadConfigurationProfiles();
            var profile = profiles.FirstOrDefault(p => p.Name?.Equals(profileName, StringComparison.OrdinalIgnoreCase) == true);
            
            // Create a default profile if none exists or the requested one is not found
            if (profile == null)
            {
                Console.WriteLine($"Configuration profile '{profileName}' not found.");
                
                // If we have any profiles, use the first one
                if (profiles.Count > 0)
                {
                    profile = profiles[0];
                    Console.WriteLine($"Using Profile: {profile.Name}");
                }
                else
                {
                    // Create a default profile
                    profile = CreateDefaultProfile();
                    profiles.Add(profile);
                    Console.WriteLine($"Created and using default profile: {profile.Name}");
                }
            }
            else
            {
                Console.WriteLine($"Using Profile: {profile.Name}");
            }
            
            // Ensure the profile has environments
            if (profile.Environments == null || profile.Environments.Count == 0)
            {
                // Add a default environment
                profile.Environments = new List<TestEnvironment>
                {
                    new TestEnvironment
                    {
                        Name = "Development",
                        Description = "Development environment",
                        Variables = new Dictionary<string, string>
                        {
                            { "baseUrl", "https://api.example.com" },
                            { "timeout", "30000" }
                        }
                    }
                };
                Console.WriteLine("Added default environment to profile.");
            }
            
            string envName = environmentName ?? profile.DefaultEnvironment ?? profile.Environments.FirstOrDefault()?.Name ?? "Development";
            
            var environment = profile.Environments.FirstOrDefault(e => e.Name?.Equals(envName, StringComparison.OrdinalIgnoreCase) == true);
            
            // If environment is not found, use the first one or create a default
            if (environment == null)
            {
                if (profile.Environments.Count > 0)
                {
                    environment = profile.Environments[0];
                    Console.WriteLine($"Environment '{envName}' not found. Using '{environment.Name}' instead.");
                }
                else
                {
                    // Create a default environment
                    environment = new TestEnvironment
                    {
                        Name = "Development",
                        Description = "Development environment",
                        Variables = new Dictionary<string, string>
                        {
                            { "baseUrl", "https://api.example.com" },
                            { "timeout", "30000" }
                        }
                    };
                    profile.Environments.Add(environment);
                    Console.WriteLine($"Created and using default environment: {environment.Name}");
                }
            }
            
            _currentEnvironment = environment;
            Console.WriteLine($"Active Environment: {environment.Name}");
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
            {
                Console.WriteLine("Warning: No active environment set. Environment variables will not be applied.");
                return apiDefinition;
            }
                
            Console.WriteLine($"Applying environment variables from '{_currentEnvironment.Name}' environment...");
            
            var modifiedApi = new ApiDefinition
            {
                Name = ApplyEnvironmentVariables(apiDefinition.Name),
                Uri = ApplyEnvironmentVariables(apiDefinition.Uri),
                Method = apiDefinition.Method,
                Payload = ProcessPayload(apiDefinition.Payload),
                PayloadType = apiDefinition.PayloadType,
                Files = apiDefinition.Files, // Preserve file upload configurations
                Tests = new List<TestAssertion>()
            };
            
            // Log URI transformation for debugging
            if (apiDefinition.Uri != modifiedApi.Uri)
            {
                Console.WriteLine($"  Transformed URI: {apiDefinition.Uri} -> {modifiedApi.Uri}");
            }
            
            if (apiDefinition.Headers != null)
            {
                modifiedApi.Headers = new Dictionary<string, string>();
                foreach (var header in apiDefinition.Headers)
                {
                    var transformedValue = ApplyEnvironmentVariables(header.Value);
                    modifiedApi.Headers[header.Key] = transformedValue;
                    
                    // Log header transformation for debugging
                    if (header.Value != transformedValue)
                    {
                        Console.WriteLine($"  Transformed Header {header.Key}: {header.Value} -> {transformedValue}");
                    }
                }
            }
            
            // Process test assertions and apply environment variables
            if (apiDefinition.Tests != null)
            {
                foreach (var test in apiDefinition.Tests)
                {
                    var originalValue = test.ExpectedValue;
                    var transformedValue = originalValue != null ? ApplyEnvironmentVariables(originalValue) : null;
                    
                    var modifiedTest = new TestAssertion
                    {
                        Name = test.Name,
                        Description = test.Description,
                        Assertion = ApplyEnvironmentVariables(test.Assertion),
                        AssertType = test.AssertType,
                        Property = test.Property,
                        ExpectedValue = transformedValue,
                        Expected = test.Expected
                    };
                    
                    // Log test value transformation for debugging
                    if (originalValue != transformedValue)
                    {
                        Console.WriteLine($"  Transformed Test Value: {originalValue} -> {transformedValue}");
                    }
                    
                    modifiedApi.Tests.Add(modifiedTest);
                }
            }
            
            // Check if the environment has all variables that might be needed
            CheckForMissingVariables(apiDefinition.Uri);
            
            return modifiedApi;
        }
        
        private void CheckForMissingVariables(string input)
        {
            if (_currentEnvironment == null || string.IsNullOrEmpty(input))
                return;
                
            var matches = VariablePattern.Matches(input);
            foreach (Match match in matches)
            {
                var variableName = match.Groups[1].Value.Trim();
                if (!_currentEnvironment.Variables.ContainsKey(variableName))
                {
                    // Show a warning for missing variables
                    Console.WriteLine($"  Warning: Environment variable '{variableName}' is referenced but not defined in the environment.");
                }
            }
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
                // Use the current directory for the config file
                string configFilePath = Path.Combine(Directory.GetCurrentDirectory(), ConfigFileName);
                
                // Check if config file already exists
                if (File.Exists(configFilePath))
                {
                    Console.WriteLine($"Configuration file '{configFilePath}' already exists.");
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
                                { "userId", "1" },
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
                                { "userId", "1" },
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
        ""timeout"": ""30000"",
        ""userId"": ""1"",
        ""apiKey"": ""dev-api-key""
      }
    }
  ]
}";
                    }
                    
                    File.WriteAllText(configFilePath, json);
                    _configFilePath = configFilePath; // Update the path to the newly created file
                }
                else
                {
                    Console.WriteLine("Error: Could not create configuration profile.");
                }
                
                Console.WriteLine($"Created configuration file at {configFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating default environment file: {ex.Message}");
            }
        }
    }
}