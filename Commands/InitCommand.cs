using System.CommandLine;
using System.Text.Json;
using Apify.Models;
using Apify.Utils;
using Newtonsoft.Json;

namespace Apify.Commands
{
    public class InitCommand : Command
    {
        private const string DefaultConfigFileName = "apify-config.json";
        private const string DefaultApiDirectoryName = ".apify";

        public InitCommand() : base("init", "Initialize a new API testing project in the current directory")
        {
            var forceOption = new Option<bool>(
                "--force",
                () => false,
                "Force overwrite of existing files"
            );

            AddOption(forceOption);

            this.SetHandler(
                (force) => ExecuteAsync(force),
                forceOption
            );
        }

        private async Task ExecuteAsync(bool force)
        {
            ConsoleHelper.WriteHeader("Initializing API Testing Project");

            // Check if configuration file already exists
            string configFilePath = Path.Combine(Directory.GetCurrentDirectory(), DefaultConfigFileName);
            
            if (File.Exists(configFilePath) && !force)
            {
                if (PromptYesNo($"Configuration file '{DefaultConfigFileName}' already exists. Overwrite?"))
                {
                    force = true;
                }
                else
                {
                    ConsoleHelper.WriteWarning("Initialization canceled.");
                    return;
                }
            }

            // Check if API directory already exists
            if (Directory.Exists(DefaultApiDirectoryName) && !force)
            {
                if (PromptYesNo($"API directory '{DefaultApiDirectoryName}' already exists. Overwrite sample files?"))
                {
                    force = true;
                }
                else
                {
                    ConsoleHelper.WriteWarning("Initialization canceled.");
                    return;
                }
            }

            try
            {
                // Prompt for required information
                string projectName = PromptForInput("Enter project name:");
                string defaultEnvironment = PromptForInput("Enter default environment name:", () => "Development");
                string baseUrl = PromptForInput("Enter base URL for API endpoints (e.g., https://api.example.com):");

                // Advanced options
                bool configureAdditionalVariables = PromptYesNo("Configure additional environment variables?");
                Dictionary<string, string> additionalVariables = new Dictionary<string, string>();
                
                if (configureAdditionalVariables)
                {
                    ConsoleHelper.WriteInfo("Enter environment variables (empty name to finish):");
                    
                    while (true)
                    {
                        string variableName = PromptForInput("Variable name:", false);
                        if (string.IsNullOrWhiteSpace(variableName)) break;
                        
                        string variableValue = PromptForInput($"Value for {variableName}:");
                        additionalVariables[variableName] = variableValue;
                    }
                }

                // Create API directory if it doesn't exist
                if (!Directory.Exists(DefaultApiDirectoryName))
                {
                    Directory.CreateDirectory(DefaultApiDirectoryName);
                    ConsoleHelper.WriteSuccess($"Created API directory: {DefaultApiDirectoryName}");
                }
                else
                {
                    ConsoleHelper.WriteInfo($"Using existing API directory: {DefaultApiDirectoryName}");
                }

                // Create environment configuration
                var defaultEnvVariables = new Dictionary<string, string>
                {
                    { "baseUrl", baseUrl },
                    { "timeout", "30000" },
                    { "userId", "1" },
                    { "apiKey", "dev-api-key" }
                };

                // Add user-defined variables
                foreach (var variable in additionalVariables)
                {
                    defaultEnvVariables[variable.Key] = variable.Value;
                }

                var defaultEnv = new TestEnvironment
                {
                    Name = defaultEnvironment,
                    Description = $"{defaultEnvironment} environment",
                    Variables = defaultEnvVariables
                };

                // Create production environment as an example
                var productionEnvVariables = new Dictionary<string, string>
                {
                    { "baseUrl", baseUrl },
                    { "timeout", "10000" },
                    { "userId", "1" },
                    { "apiKey", "prod-api-key" }
                };

                // Add user-defined variables to production too
                foreach (var variable in additionalVariables)
                {
                    productionEnvVariables[variable.Key] = variable.Value;
                }

                var productionEnvironment = new TestEnvironment
                {
                    Name = "Production",
                    Description = "Production environment",
                    Variables = productionEnvVariables
                };

                // Ask if user wants to add additional environments
                List<TestEnvironment> environments = new List<TestEnvironment> { defaultEnv, productionEnvironment };
                
                if (PromptYesNo("Add additional environments?"))
                {
                    while (true)
                    {
                        string envName = PromptForInput("Environment name (empty to finish):", false);
                        if (string.IsNullOrWhiteSpace(envName)) break;
                        
                        string envDescription = PromptForInput($"Description for {envName}:", 
                            () => $"{envName} environment");
                        
                        var envVariables = new Dictionary<string, string>
                        {
                            { "baseUrl", PromptForInput($"Base URL for {envName}:", () => baseUrl) },
                            { "timeout", PromptForInput($"Timeout for {envName} (ms):", () => "20000") },
                            { "userId", "1" },
                            { "apiKey", $"{envName.ToLower()}-api-key" }
                        };
                        
                        // Add user-defined variables
                        foreach (var variable in additionalVariables)
                        {
                            envVariables[variable.Key] = PromptForInput($"{variable.Key} for {envName}:", 
                                () => variable.Value);
                        }
                        
                        environments.Add(new TestEnvironment 
                        { 
                            Name = envName, 
                            Description = envDescription,
                            Variables = envVariables
                        });
                    }
                }

                // Create environment configuration with project-level variables
                var config = new TestEnvironmentConfig
                {
                    Name = "Default",
                    Description = $"Configuration for {projectName}",
                    // Add project-level variables (shared across all environments)
                    Variables = new Dictionary<string, string>
                    {
                        { "projectId", $"{projectName.ToLower().Replace(" ", "-")}" },
                        { "version", "1.0.0" },
                        { "apiVersion", "v1" }
                    },
                    DefaultEnvironment = defaultEnvironment,
                    Environments = environments
                };

                // Create the sample API test file
                var sampleApiTest = new ApiDefinition
                {
                    Name = "Sample API Test",
                    Uri = "{{baseUrl}}/posts/1",
                    Method = "GET",
                    Headers = new Dictionary<string, string>
                    {
                        { "Accept", "application/json" }
                    },
                    PayloadType = PayloadType.None,
                    Tests = new List<TestAssertion>
                    {
                        new TestAssertion { 
                            Name = "Status code is successful", 
                            Description = "Status code is 200",
                            AssertType = "StatusCode",
                            ExpectedValue = "200"
                        },
                        new TestAssertion { 
                            Name = "Response format is JSON", 
                            Description = "Content-Type header is application/json",
                            AssertType = "HeaderContains",
                            Property = "Content-Type",
                            ExpectedValue = "application/json"
                        },
                        new TestAssertion { 
                            Name = "Response contains id property", 
                            Description = "Response contains id property",
                            AssertType = "ContainsProperty",
                            ExpectedValue = "id"
                        }
                    }
                };

                // Create an example API file in the apis directory
                string sampleApiFilePath = Path.Combine(DefaultApiDirectoryName, "sample-api.json");
                await File.WriteAllTextAsync(sampleApiFilePath, JsonHelper.SerializeObject(sampleApiTest));
                ConsoleHelper.WriteSuccess($"Created sample API test: {sampleApiFilePath}");
                
                // Create a POST sample with JSON payload
                var samplePostTest = new ApiDefinition
                {
                    Name = "Sample POST API Test",
                    Uri = "{{baseUrl}}/posts",
                    Method = "POST",
                    Headers = new Dictionary<string, string>
                    {
                        { "Accept", "application/json" },
                        { "Content-Type", "application/json" }
                    },
                    Payload = new Dictionary<string, object>
                    {
                        { "title", "Sample Post" },
                        { "body", "This is a sample post body" },
                        { "userId", 1 }
                    },
                    PayloadType = PayloadType.Json,
                    Tests = new List<TestAssertion>
                    {
                        new TestAssertion { 
                            Name = "Status code is Created", 
                            Description = "Status code is 201",
                            AssertType = "StatusCode",
                            ExpectedValue = "201"
                        },
                        new TestAssertion { 
                            Name = "Response contains id property", 
                            Description = "Response contains id property",
                            AssertType = "ContainsProperty",
                            ExpectedValue = "id"
                        },
                        new TestAssertion { 
                            Name = "Response contains title property", 
                            Description = "Response contains title property",
                            AssertType = "ContainsProperty",
                            ExpectedValue = "title"
                        }
                    }
                };
                
                string samplePostFilePath = Path.Combine(DefaultApiDirectoryName, "sample-post.json");
                await File.WriteAllTextAsync(samplePostFilePath, JsonHelper.SerializeObject(samplePostTest));
                ConsoleHelper.WriteSuccess($"Created sample POST API test: {samplePostFilePath}");

                // Save the configuration file
                try
                {
                    string configJson = JsonHelper.SerializeObject(config);
                    
                    // Skip the validation step that's causing issues
                    bool useRawJson = false;
                    
                    try 
                    {
                        // Try to manually deserialize the JSON to verify it's correctly formatted
                        var jsonSettings = new JsonSerializerSettings
                        {
                            MissingMemberHandling = MissingMemberHandling.Ignore,
                            NullValueHandling = NullValueHandling.Ignore
                        };
                        var testObject = JsonConvert.DeserializeObject<TestEnvironmentConfig>(configJson, jsonSettings);
                        
                        // Check if the deserialized object is valid
                        if (testObject == null || testObject.Environments == null || testObject.Environments.Count == 0)
                        {
                            useRawJson = true;
                        }
                    }
                    catch (Exception)
                    {
                        // If any error occurs during validation, use the raw JSON
                        useRawJson = true;
                    }
                    
                    if (useRawJson)
                    {
                        ConsoleHelper.WriteInfo("Using direct JSON format for configuration.");
                        // Create a manual JSON object directly - this would need to be more complex to handle custom environments
                        configJson = BuildConfigJson(projectName, defaultEnvironment, baseUrl, environments, additionalVariables);
                    }
                    
                    try 
                    {
                        string currentDir = Directory.GetCurrentDirectory();
                        ConsoleHelper.WriteInfo($"Current working directory: {currentDir}");
                        ConsoleHelper.WriteInfo($"Creating config file at: {configFilePath}");

                        await File.WriteAllTextAsync(configFilePath, configJson);
                        
                        // Verify file was created
                        if (File.Exists(configFilePath))
                        {
                            ConsoleHelper.WriteSuccess($"Created configuration file: {DefaultConfigFileName}");
                            ConsoleHelper.WriteInfo($"Configuration file exists at: {configFilePath}");
                        }
                        else
                        {
                            ConsoleHelper.WriteError($"Failed to create configuration file at: {configFilePath}");
                        }
                    }
                    catch (Exception fileEx)
                    {
                        ConsoleHelper.WriteError($"Error writing config file: {fileEx.Message}");
                        if (fileEx.InnerException != null)
                        {
                            ConsoleHelper.WriteError($"Inner exception: {fileEx.InnerException.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"Error creating configuration file: {ex.Message}");
                }

                // Display success message
                ConsoleHelper.WriteSuccess("\nProject initialized successfully!");

                // Check if we're running as a compiled executable or via dotnet run
                string exeName = Path.GetFileName(Environment.ProcessPath ?? "apitester");
                bool isCompiledExecutable = !exeName.Equals("dotnet", StringComparison.OrdinalIgnoreCase);
                
                // Display the interactive quick start guide
                ConsoleHelper.DisplayQuickStartGuide(configFilePath, DefaultApiDirectoryName, isCompiledExecutable);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Error initializing project: {ex.Message}");
            }
        }

        private string BuildConfigJson(string projectName, string defaultEnvironment, string baseUrl, 
                                     List<TestEnvironment> environments, Dictionary<string, string> additionalVariables)
        {
            // Start building the JSON manually
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"Name\": \"Default\",");
            sb.AppendLine($"  \"Description\": \"Configuration for {projectName}\",");
            sb.AppendLine($"  \"DefaultEnvironment\": \"{defaultEnvironment}\",");
            
            // Add project variables
            sb.AppendLine("  \"Variables\": {");
            sb.AppendLine($"    \"projectId\": \"{projectName.ToLower().Replace(" ", "-")}\",");
            sb.AppendLine("    \"version\": \"1.0.0\",");
            sb.AppendLine("    \"apiVersion\": \"v1\"");
            sb.AppendLine("  },");
            
            // Add environments
            sb.AppendLine("  \"Environments\": [");
            
            for (int i = 0; i < environments.Count; i++)
            {
                var env = environments[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"Name\": \"{env.Name}\",");
                sb.AppendLine($"      \"Description\": \"{env.Description}\",");
                sb.AppendLine("      \"Variables\": {");
                
                // Add all variables for this environment
                var variables = env.Variables;
                int varCount = 0;
                foreach (var variable in variables)
                {
                    varCount++;
                    string comma = varCount < variables.Count ? "," : "";
                    sb.AppendLine($"        \"{variable.Key}\": \"{variable.Value}\"{comma}");
                }
                
                sb.AppendLine("      }");
                
                // Add comma if not the last environment
                if (i < environments.Count - 1)
                {
                    sb.AppendLine("    },");
                }
                else
                {
                    sb.AppendLine("    }");
                }
            }
            
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            
            return sb.ToString();
        }

        private string PromptForInput(string prompt, bool required = true)
        {
            while (true)
            {
                string? input = ConsoleHelper.PromptInput(prompt);
                
                if (string.IsNullOrWhiteSpace(input))
                {
                    if (!required) return string.Empty;
                    ConsoleHelper.WriteWarning("This field is required. Please try again.");
                }
                else
                {
                    return input;
                }
            }
        }

        private string PromptForInput(string prompt, Func<string> defaultValueProvider, bool required = true)
        {
            string defaultValue = defaultValueProvider();
            string input = ConsoleHelper.PromptInput(prompt, defaultValue);
                
            if (string.IsNullOrWhiteSpace(input))
            {
                if (!required) return string.Empty;
                return defaultValue;
            }
            else
            {
                return input;
            }
        }

        private bool PromptYesNo(string prompt)
        {
            while (true)
            {
                string? input = ConsoleHelper.PromptInput($"{prompt} (y/n)").Trim().ToLower();
                
                if (input == "y" || input == "yes") return true;
                if (input == "n" || input == "no") return false;
                
                ConsoleHelper.WriteWarning("Please enter 'y' or 'n'.");
            }
        }

        private int PromptChoice(string prompt, string[] options)
        {
            Console.WriteLine();
            ConsoleHelper.WriteInfo(prompt);
            
            for (int i = 0; i < options.Length; i++)
            {
                Console.WriteLine($"{i+1}. {options[i]}");
            }
            
            while (true)
            {
                string? input = ConsoleHelper.PromptInput($"Enter selection (1-{options.Length})");
                
                if (int.TryParse(input, out int selection) && selection >= 1 && selection <= options.Length)
                {
                    return selection - 1; // Return zero-based index
                }
                
                ConsoleHelper.WriteWarning($"Please enter a number between 1 and {options.Length}.");
            }
        }
    }
}