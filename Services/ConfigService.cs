using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Apify.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Apify.Services;

public class ConfigService
{
    private const string ConfigFileName = "apify-config.json";
    private static readonly Regex VariablePattern = new Regex(@"{{(.+?)}}", RegexOptions.Compiled);

    private EnvironmentSchema? _defaultEnvironment = null;
    private ApifyConfigSchema? _config = null;
    private string? _configFilePath = null;
    private bool _debug;
    
    public EnvironmentSchema? DefaultEnvironment => _defaultEnvironment;
    
    public ConfigService(bool debug = false)
    {
        // Initialize config path to null - it will be determined dynamically when needed
        _configFilePath = null;
        _debug = debug;
    }

    public void SetConfigFilePath(string path)
    {
        _configFilePath = path;
    }
    
    // Function to get the configuration file path - always uses current working directory
    private string GetConfigFilePath()
    {
        // If the config file path is already set, return it
        if (_configFilePath != null)
        {
            return _configFilePath;
        }
        
        // Only use the current working directory (where the command is executed from)
        string currentDir = Directory.GetCurrentDirectory();
        string currentPath = Path.Combine(currentDir, ConfigFileName);
        
        // Always use the current directory path
        return _configFilePath = currentPath;
    }

    public ApifyConfigSchema LoadConfiguration()
    {
        // If config is already loaded, return it
        if (_config != null)
        {
            return _config;
        }
        
        // Always get the latest config file path from the current directory
        string configPath = GetConfigFilePath();
        
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Configuration file not found at {configPath}");
        }
     
        var content = File.ReadAllText(configPath);
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new FormatException("The configuration file is empty or contains only whitespace.");
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

        if (config == null)
        {
            throw new FormatException("No valid configuration found in the file.");
        }
    
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
        
        return _config = config;
    }
    
    public EnvironmentSchema? GetDefaultEnvironment()
    {
        // If no environment is set, use the default one
        if (_defaultEnvironment == null)
        {
            var config = LoadConfiguration();
            _defaultEnvironment = LoadEnvironment(config.DefaultEnvironment ?? "Development");
        }
        
        return _defaultEnvironment;
    }
    
    
    public EnvironmentSchema? LoadEnvironment(string? environmentName = null)
    {
        // Load the configuration from the current directory
        var config = LoadConfiguration();
        
        string defaultEnvName = environmentName ?? config.DefaultEnvironment ?? config.Environments.FirstOrDefault()?.Name ?? "Development";
        var environment = config.Environments.FirstOrDefault(e => e.Name?.Equals(defaultEnvName, StringComparison.OrdinalIgnoreCase) == true);
        
        // If environment is not found, use the first one or create a default
        if (environment == null)
        {
            if (config.Environments.Count > 0)
            {
                environment = config.Environments[0];
                Console.WriteLine($"EnvironmentSchema '{defaultEnvName}' not found. Using '{environment.Name}' instead.");
            }
            else
            {
                return null;
            }
           
        }
        
        return environment;
    }
    
    public void ListEnvironmentVariables(Dictionary<string, string>? customVariables = null)
    {
        bool hasEnvironmentVars = _defaultEnvironment != null && _defaultEnvironment.Variables.Count > 0;
        bool hasCustomVars = customVariables != null && customVariables.Count > 0;
        
        if (!hasEnvironmentVars && !hasCustomVars)
        {
            Console.WriteLine("No environment or custom variables available.");
            return;
        }
        
        if (hasEnvironmentVars)
        {
            Console.WriteLine($"EnvironmentSchema Variables (from '{_defaultEnvironment!.Name}' environment):");
            foreach (var variable in _defaultEnvironment.Variables)
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
