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
            // Only use the current working directory (where the command is executed from)
            string currentDir = Directory.GetCurrentDirectory();
            string currentPath = Path.Combine(currentDir, ConfigFileName);
            
            // If the file exists, log it
            if (File.Exists(currentPath))
            {
                string absolutePath = Path.GetFullPath(currentPath);
                Console.WriteLine($"Using configuration file: {absolutePath}");
            }
            else
            {
                // File doesn't exist yet, so we'll use this path for creating it
                Console.WriteLine($"Configuration file not found. Will use: {Path.GetFullPath(currentPath)}");
            }
            
            // Always use the current directory path
            _configFilePath = currentPath;
            return currentPath;
        }

        public TestEnvironmentConfig LoadConfigurationProfile()
        {
            // Always get the latest config file path from the current directory
            string configPath = GetConfigFilePath();
            
            // Create a default config that will be used if we can't load from file
            var defaultConfig = CreateDefaultConfig();
            
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"Configuration file not found at {configPath}");
                return defaultConfig;
            }
            
            try
            {
                var content = File.ReadAllText(configPath);
                if (string.IsNullOrWhiteSpace(content))
                {
                    Console.WriteLine($"Configuration file at {configPath} is empty");
                    return defaultConfig;
                }
                
                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) => 
                    {
                        Console.WriteLine($"JSON Error: {args.ErrorContext.Error.Message}");
                        args.ErrorContext.Handled = true;
                    },
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.None // For security
                };
                
                // Try to deserialize the config
                TestEnvironmentConfig? config = JsonConvert.DeserializeObject<TestEnvironmentConfig>(content, settings);
                
                if (config != null)
                {
                    Console.WriteLine("Successfully loaded environment configuration");
                    
                    // Ensure Environments collection is initialized
                    if (config.Environments == null)
                    {
                        config.Environments = new List<TestEnvironment>();
                        Console.WriteLine("Initialized empty Environments collection");
                    }
                    
                    return config;
                }
                
                // If deserialize failed, fall back to default
                Console.WriteLine("Deserialization failed, using default configuration");
                return defaultConfig;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration from {configPath}: {ex.Message}");
                return defaultConfig;
            }
        }
        
        private TestEnvironmentConfig CreateDefaultConfig()
        {
            // Use a default URL for testing, which can be overridden by config file
            // Using httpbin.org as it's a reliable testing endpoint with both GET and POST support
            string baseUrl = "https://httpbin.org"; 
            
            return new TestEnvironmentConfig
            {
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
        
        public bool SetCurrentEnvironment(string? environmentName = null)
        {
            // Load the configuration from the current directory
            var config = LoadConfigurationProfile();
            
            // Ensure the config has environments
            if (config.Environments == null || config.Environments.Count == 0)
            {
                // Add a default environment
                config.Environments = new List<TestEnvironment>
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
                Console.WriteLine("Added default environment to configuration.");
            }
            
            string envName = environmentName ?? config.DefaultEnvironment ?? config.Environments.FirstOrDefault()?.Name ?? "Development";
            
            var environment = config.Environments.FirstOrDefault(e => e.Name?.Equals(envName, StringComparison.OrdinalIgnoreCase) == true);
            
            // If environment is not found, use the first one or create a default
            if (environment == null)
            {
                if (config.Environments.Count > 0)
                {
                    environment = config.Environments[0];
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
                    config.Environments.Add(environment);
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
                
                // Create a default configuration
                var defaultConfig = CreateDefaultConfig();
                
                // Serialize to JSON
                var json = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
                
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