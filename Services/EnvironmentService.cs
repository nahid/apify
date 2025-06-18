using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Apify.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Apify.Services
{
    public class EnvironmentService
    {
        private const string ConfigFileName = "apify-config.json";
        private static readonly Regex VariablePattern = new Regex(@"{{(.+?)}}", RegexOptions.Compiled);
        
        private EnvironmentSchema? _currentEnvironment;
        private string? _configFilePath;
        private bool _debug;
        
        public EnvironmentSchema? CurrentEnvironment => _currentEnvironment;
        
        public EnvironmentService(bool debug = false)
        {
            // Initialize config path to null - it will be determined dynamically when needed
            _configFilePath = null;
            _debug = debug;
        }
        
        public async Task LoadConfig()
        {
            // Load configuration file
            var config = LoadConfigurationProfile();
            
            // Set the environment
            SetCurrentEnvironment();
            
            await Task.CompletedTask;
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
                if (_debug)
                {
                    Console.WriteLine($"Configuration file not found. Will use: {Path.GetFullPath(currentPath)}");
                }
            }
            
            // Always use the current directory path
            _configFilePath = currentPath;
            return currentPath;
        }

        public ApifyConfigSchema LoadConfigurationProfile()
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
                ApifyConfigSchema? config = JsonConvert.DeserializeObject<ApifyConfigSchema>(content, settings);
                
                if (config != null)
                {
                    if (_debug)
                    {
                        Console.WriteLine("Successfully loaded environment configuration");
                    }
                    
                    // Ensure Environments collection is initialized
                    if (config.Environments == null)
                    {
                        config.Environments = new List<EnvironmentSchema>();
                        
                        if (_debug)
                        {
                            Console.WriteLine("Initialized empty Environments collection");
                        }
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
        
        private ApifyConfigSchema CreateDefaultConfig()
        {
            // Use a default URL for testing, which can be overridden by config file
            // Using httpbin.org as it's a reliable testing endpoint with both GET and POST support
            string baseUrl = "https://httpbin.org"; 
            
            return new ApifyConfigSchema
            {
                DefaultEnvironment = "Development",
                // Project-level variables (shared across all environments)
                Variables = new Dictionary<string, string>
                {
                    { "projectId", "api-test-1" },
                    { "version", "1.0.0" }
                },
                Environments = new List<EnvironmentSchema>
                {
                    new EnvironmentSchema
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
                config.Environments = new List<EnvironmentSchema>
                {
                    new EnvironmentSchema
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
                    Console.WriteLine($"EnvironmentSchema '{envName}' not found. Using '{environment.Name}' instead.");
                }
                else
                {
                    // Create a default environment
                    environment = new EnvironmentSchema
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
            Console.WriteLine($"Active EnvironmentSchema: {environment.Name}");
            return true;
        }
        
        // Apply environment variables from the current environment only
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
        
        // Apply variables from all sources - project, environment, and request-specific
        public string ApplyVariablesToString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            
            // Load config to access project-level variables
            var config = LoadConfigurationProfile();
            var mergedVariables = new Dictionary<string, string>();
            
            // Add project-level variables (lowest priority)
            if (config.Variables != null)
            {
                foreach (var projectVar in config.Variables)
                {
                    mergedVariables[projectVar.Key] = projectVar.Value;
                }
            }
            
            // Add environment-specific variables (medium priority)
            if (_currentEnvironment != null && _currentEnvironment.Variables != null)
            {
                foreach (var envVar in _currentEnvironment.Variables)
                {
                    mergedVariables[envVar.Key] = envVar.Value;
                }
            }
            
            // Apply all variables to the input string
            return VariablePattern.Replace(input, match =>
            {
                var variableName = match.Groups[1].Value.Trim();
                if (mergedVariables.TryGetValue(variableName, out var value))
                {
                    return value;
                }
                return match.Value; // Keep the original {{variable}} if not found
            });
        }
        
        public ApiDefinition ApplyEnvironmentVariables(ApiDefinition apiDefinition)
        {
            // Create a merged dictionary of variables with updated priority:
            // 1. Request-specific variables (highest priority)
            // 2. EnvironmentSchema variables (medium priority)
            // 3. Project-level variables (lowest priority)
            var mergedVariables = new Dictionary<string, string>();
            
            // Load config to access project-level variables
            var config = LoadConfigurationProfile();
            
            // First, add project-level variables (lowest priority)
            if (config.Variables != null && config.Variables.Count > 0)
            {
                if (_debug)
                {
                    Console.WriteLine("Applying project-level variables from apify-config.json...");
                }
                foreach (var projectVar in config.Variables)
                {
                    if (_debug)
                    {
                        Console.WriteLine($"  Added project-level variable: {projectVar.Key}");
                    }
                    mergedVariables[projectVar.Key] = projectVar.Value;
                }
            }
            
            // Next, add environment-specific variables (medium priority - overrides project variables)
            if (_currentEnvironment == null)
            {
                if (_debug)
                {
                    Console.WriteLine("Warning: No active environment set. Only project and request variables will be applied.");
                }
            }
            else
            {
                if (_debug)
                {
                    Console.WriteLine($"Applying environment variables from '{_currentEnvironment.Name}' environment...");
                }
                
                // Add environment variables from the current environment
                foreach (var envVar in _currentEnvironment.Variables)
                {
                    if (_debug)
                    {
                        if (mergedVariables.ContainsKey(envVar.Key))
                        {
                            Console.WriteLine($"  EnvironmentSchema variable '{envVar.Key}' overrides project variable with same name");
                        }
                        else
                        {
                            Console.WriteLine($"  Added environment variable: {envVar.Key}");
                        }
                    }
                    mergedVariables[envVar.Key] = envVar.Value;
                }
            }
            
            // Finally, add request-specific variables (highest priority - overrides both project and environment variables)
            if (apiDefinition.Variables != null && apiDefinition.Variables.Count > 0)
            {
                if (_debug)
                {
                    Console.WriteLine("Applying request-specific variables from API definition...");
                }
                foreach (var customVar in apiDefinition.Variables)
                {
                    if (_debug)
                    {
                        if (mergedVariables.ContainsKey(customVar.Key))
                        {
                            Console.WriteLine($"  Request-specific variable '{customVar.Key}' overrides variable with same name");
                        }
                        else
                        {
                            Console.WriteLine($"  Added request-specific variable: {customVar.Key}");
                        }
                    }
                    mergedVariables[customVar.Key] = customVar.Value;
                }
            }
            
            var modifiedApi = new ApiDefinition
            {
                Name = ApplyVariables(apiDefinition.Name, mergedVariables),
                Uri = ApplyVariables(apiDefinition.Uri, mergedVariables),
                Method = apiDefinition.Method,
                Payload = ProcessPayload(apiDefinition.Payload, mergedVariables),
                PayloadType = apiDefinition.PayloadType,
                Files = apiDefinition.Files, // Preserve file upload configurations
                Tests = new List<TestAssertion>(),
                Variables = apiDefinition.Variables // Keep the original variables for reference
            };
            
            // Log URI transformation for debugging
            if (_debug && apiDefinition.Uri != modifiedApi.Uri)
            {
                Console.WriteLine($"  Transformed URI: {apiDefinition.Uri} -> {modifiedApi.Uri}");
            }
            
            if (apiDefinition.Headers != null)
            {
                modifiedApi.Headers = new Dictionary<string, string>();
                foreach (var header in apiDefinition.Headers)
                {
                    var transformedValue = ApplyVariables(header.Value, mergedVariables);
                    modifiedApi.Headers[header.Key] = transformedValue;
                    
                    // Log header transformation for debugging
                    if (_debug && header.Value != transformedValue)
                    {
                        Console.WriteLine($"  Transformed Header {header.Key}: {header.Value} -> {transformedValue}");
                    }
                }
            }
            
            // Process test assertions and apply variables
            if (apiDefinition.Tests != null)
            {
                foreach (var test in apiDefinition.Tests)
                {
                    var originalValue = test.ExpectedValue;
                    var transformedValue = originalValue != null ? ApplyVariables(originalValue, mergedVariables) : null;
                    
                    var modifiedTest = new TestAssertion
                    {
                        Name = test.Name,
                        Description = test.Description,
                        Assertion = ApplyVariables(test.Assertion, mergedVariables),
                        AssertType = test.AssertType,
                        Property = test.Property,
                        ExpectedValue = transformedValue,
                        Expected = test.Expected
                    };
                    
                    // Log test value transformation for debugging
                    if (_debug && originalValue != transformedValue)
                    {
                        Console.WriteLine($"  Transformed Test Value: {originalValue} -> {transformedValue}");
                    }
                    
                    modifiedApi.Tests.Add(modifiedTest);
                }
            }
            
            // Check if there are any variables that might be needed but are missing
            CheckForMissingVariables(apiDefinition.Uri, mergedVariables);
            
            return modifiedApi;
        }
        
        // Method to apply variable substitution using a provided dictionary
        private string ApplyVariables(string input, Dictionary<string, string> variables)
        {
            if (string.IsNullOrEmpty(input) || variables == null || variables.Count == 0)
                return input;
                
            return VariablePattern.Replace(input, match =>
            {
                var variableName = match.Groups[1].Value.Trim();
                if (variables.TryGetValue(variableName, out var value))
                {
                    return value;
                }
                return match.Value; // Keep original if not found
            });
        }
        
        // Process JToken (JObject or JArray) recursively to apply variables
        private JToken ProcessJToken(JToken token, Dictionary<string, string> mergedVariables)
        {
            switch (token.Type)
            {
                case JTokenType.Object:
                    foreach (var prop in token.Children<JProperty>().ToList())
                    {
                        var value = ProcessJToken(prop.Value, mergedVariables);
                        prop.Value = value;
                    }
                    break;
                case JTokenType.Array:
                    for (int i = 0; i < token.Count(); i++)
                    {
                        var item = token[i];
                        if (item != null)
                        {
                            token[i] = ProcessJToken(item, mergedVariables);
                        }
                    }
                    break;
                case JTokenType.String:
                    // Apply variables to string values
                    var stringValue = token.Value<string>();
                    if (stringValue != null)
                    {
                        var transformed = ApplyVariables(stringValue, mergedVariables);
                        if (transformed != stringValue)
                        {
                            return new JValue(transformed);
                        }
                    }
                    break;
            }
            
            return token;
        }
        
        // Process payload data based on type using merged variables
        private object? ProcessPayload(object? payload, Dictionary<string, string> mergedVariables)
        {
            if (payload == null)
                return null;
                
            if (payload is string strPayload)
            {
                return ApplyVariables(strPayload, mergedVariables);
            }
            else if (payload is JObject jObject)
            {
                // Process JObject to apply variables
                var processed = ProcessJToken(jObject, mergedVariables);
                return processed;
            }
            else if (payload is JArray jArray)
            {
                // Process JArray to apply variables
                var processed = ProcessJToken(jArray, mergedVariables);
                return processed;
            }
            
            // For other payload types (like Dictionary), try serializing and deserializing
            try
            {
                var json = JsonConvert.SerializeObject(payload);
                var processedJson = ApplyVariables(json, mergedVariables);
                return JsonConvert.DeserializeObject(processedJson);
            }
            catch
            {
                // If conversion fails, return the original payload
                return payload;
            }
        }
        
        // Check for missing variables in the input string
        private void CheckForMissingVariables(string input, Dictionary<string, string> mergedVariables)
        {
            if (string.IsNullOrEmpty(input))
                return;
                
            var matches = VariablePattern.Matches(input);
            foreach (Match match in matches)
            {
                var variableName = match.Groups[1].Value.Trim();
                if (!mergedVariables.ContainsKey(variableName))
                {
                    // Show a warning for missing variables only when debug is enabled
                    if (_debug)
                    {
                        Console.WriteLine($"  Warning: Variable '{variableName}' is referenced but not defined in environment or custom variables.");
                    }
                }
            }
        }
        
        // Legacy method for backward compatibility
        private JToken ProcessJToken(JToken token)
        {
            var vars = _currentEnvironment?.Variables ?? new Dictionary<string, string>();
            return ProcessJToken(token, vars);
        }
        
        // Legacy method for backward compatibility
        private object? ProcessPayload(object? payload)
        {
            var vars = _currentEnvironment?.Variables ?? new Dictionary<string, string>();
            return ProcessPayload(payload, vars);
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
        
        public void ListEnvironmentVariables(Dictionary<string, string>? customVariables = null)
        {
            bool hasEnvironmentVars = _currentEnvironment != null && _currentEnvironment.Variables.Count > 0;
            bool hasCustomVars = customVariables != null && customVariables.Count > 0;
            
            if (!hasEnvironmentVars && !hasCustomVars)
            {
                Console.WriteLine("No environment or custom variables available.");
                return;
            }
            
            if (hasEnvironmentVars)
            {
                Console.WriteLine($"EnvironmentSchema Variables (from '{_currentEnvironment!.Name}' environment):");
                foreach (var variable in _currentEnvironment.Variables)
                {
                    string displayValue = variable.Key.ToLower().Contains("key") || 
                                          variable.Key.ToLower().Contains("secret") || 
                                          variable.Key.ToLower().Contains("password") ? 
                                          "********" : variable.Value;
                    
                    Console.WriteLine($"  {variable.Key}: {displayValue}");
                }
            }
            
            if (hasCustomVars)
            {
                Console.WriteLine("Custom Variables (from API definition):");
                foreach (var variable in customVariables!)
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
}