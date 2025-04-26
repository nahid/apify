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
            // Initialize config path to null - it will be determined dynamically when needed
            _configFilePath = null;
        }
        
        // Function to get the configuration file path - always uses current working directory
        private string GetConfigFilePath()
        {
            // Try multiple locations for the config file, in order of priority:
            // 1. Current working directory (where the user is executing from)
            // 2. Application base directory (where the executable is located)
            // 3. Parent directory of the executable
            
            // Get current directory path
            string currentDir = Directory.GetCurrentDirectory();
            string currentPath = Path.Combine(currentDir, ConfigFileName);
            
            // Check if the file exists in the current directory
            if (File.Exists(currentPath))
            {
                string absolutePath = Path.GetFullPath(currentPath);
                Console.WriteLine($"Using configuration file: {absolutePath}");
                _configFilePath = currentPath;
                return currentPath;
            }
            
            // If not found, try the application base directory
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string appPath = Path.Combine(appDir, ConfigFileName);
            
            if (File.Exists(appPath))
            {
                string absolutePath = Path.GetFullPath(appPath);
                Console.WriteLine($"Using configuration file: {absolutePath}");
                _configFilePath = appPath;
                return appPath;
            }
            
            // If still not found, try parent directory of the executable
            string? parentDir = Path.GetDirectoryName(appDir);
            if (!string.IsNullOrEmpty(parentDir))
            {
                string parentPath = Path.Combine(parentDir, ConfigFileName);
                if (File.Exists(parentPath))
                {
                    string absolutePath = Path.GetFullPath(parentPath);
                    Console.WriteLine($"Using configuration file: {absolutePath}");
                    _configFilePath = parentPath;
                    return parentPath;
                }
            }
            
            // If we get here, the file doesn't exist yet, so return the current directory path
            // as the preferred location for creating it
            Console.WriteLine($"Configuration file not found. Will use: {Path.GetFullPath(currentPath)}");
            _configFilePath = currentPath;
            return currentPath;
        }

        public List<ConfigurationProfile> LoadConfigurationProfiles()
        {
            var profiles = new List<ConfigurationProfile>();
            
            // Always get the latest config file path from the current directory
            string configPath = GetConfigFilePath();
            
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"Configuration file not found at {configPath}");
                return profiles;
            }
            
            try
            {
                var content = File.ReadAllText(configPath);
                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) => 
                    {
                        Console.WriteLine($"JSON Error: {args.ErrorContext.Error.Message}");
                        args.ErrorContext.Handled = true;
                    },
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                    TypeNameHandling = TypeNameHandling.None, // For security
                    MetadataPropertyHandling = MetadataPropertyHandling.Ignore
                };
                
                // For troubleshooting, try to use the direct, safe constructor approach
                try {
                    var profile = new ConfigurationProfile();
                    JsonConvert.PopulateObject(content, profile, settings);
                    
                    if (profile != null)
                    {
                        profiles.Add(profile);
                        Console.WriteLine($"Successfully loaded profile: {profile.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"PopulateObject failed: {ex.Message}");
                    
                    // Fall back to deserialize
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration profile from {configPath}: {ex.Message}");
                
                // Create and add a default profile on error
                Console.WriteLine("No environment profiles found. Creating default profile...");
                var defaultProfile = CreateDefaultProfile();
                profiles.Add(defaultProfile);
            }
            
            return profiles;
        }
        
        private ConfigurationProfile CreateDefaultProfile()
        {
            // Use a default URL for testing, which can be overridden by config file
            // Using httpbin.org as it's a reliable testing endpoint with both GET and POST support
            string baseUrl = "https://httpbin.org"; 
            
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
                            { "baseUrl", baseUrl },
                            { "timeout", "30000" },
                            { "userId", "1" },
                            { "apiKey", "dev-api-key" }
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
                            { "baseUrl", "https://httpbin.org" },
                            { "timeout", "30000" },
                            { "userId", "1" },
                            { "apiKey", "dev-api-key" }
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
                            { "baseUrl", "https://httpbin.org" },
                            { "timeout", "30000" },
                            { "userId", "1" },
                            { "apiKey", "dev-api-key" }
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
                    var stringValue = token.Value<string>();
                    if (stringValue != null)
                    {
                        var transformed = ApplyEnvironmentVariables(stringValue);
                        if (transformed != stringValue)
                        {
                            return new JValue(transformed);
                        }
                    }
                    break;
            }
            
            return token;
        }
        
        public void CreateDefaultEnvironmentFile()
        {
            try
            {
                // Get the file path in the current directory
                var filePath = GetConfigFilePath();
                
                // Check if the file already exists
                if (File.Exists(filePath))
                {
                    Console.WriteLine($"Configuration file already exists at {filePath}");
                    return;
                }
                
                // Create a default profile
                var defaultProfile = CreateDefaultProfile();
                
                // Serialize to JSON
                var json = JsonConvert.SerializeObject(defaultProfile, Formatting.Indented);
                
                // Write to file
                File.WriteAllText(filePath, json);
                
                Console.WriteLine($"Created default configuration file at {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating default environment file: {ex.Message}");
            }
        }
        
        public void ListEnvironmentVariables()
        {
            if (_currentEnvironment == null)
            {
                Console.WriteLine("No active environment set.");
                return;
            }
            
            Console.WriteLine("Environment Variables:");
            foreach (var variable in _currentEnvironment.Variables)
            {
                string displayValue = variable.Key.ToLower().Contains("key") || 
                                      variable.Key.ToLower().Contains("secret") || 
                                      variable.Key.ToLower().Contains("password") ? 
                                      "********" : variable.Value;
                
                Console.WriteLine($"  {variable.Key}: {displayValue}");
            }
        }
    }
}