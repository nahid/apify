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
            var projectNameOption = new Option<string>(
                "--name",
                "The name of the API testing project"
            ) { IsRequired = true };

            var environmentOption = new Option<string>(
                "--environment",
                () => "Development",
                "The default environment (e.g., Development, Staging, Production)"
            );

            var baseUrlOption = new Option<string>(
                "--base-url",
                "The base URL for API endpoints"
            ) { IsRequired = true };

            var forceOption = new Option<bool>(
                "--force",
                () => false,
                "Force overwrite of existing files"
            );

            AddOption(projectNameOption);
            AddOption(environmentOption);
            AddOption(baseUrlOption);
            AddOption(forceOption);

            this.SetHandler(
                (projectName, environment, baseUrl, force) => ExecuteAsync(projectName, environment, baseUrl, force),
                projectNameOption, environmentOption, baseUrlOption, forceOption
            );
        }

        private async Task ExecuteAsync(string projectName, string environment, string baseUrl, bool force)
        {
            ConsoleHelper.WriteHeader("Initializing API Testing Project");

            // Check if configuration file already exists
            string configFilePath = Path.Combine(Directory.GetCurrentDirectory(), DefaultConfigFileName);
            
            if (File.Exists(configFilePath) && !force)
            {
                ConsoleHelper.WriteError($"Configuration file '{DefaultConfigFileName}' already exists. Use --force to overwrite.");
                return;
            }

            // Check if API directory already exists
            if (Directory.Exists(DefaultApiDirectoryName) && !force)
            {
                ConsoleHelper.WriteError($"API directory '{DefaultApiDirectoryName}' already exists. Use --force to overwrite.");
                return;
            }

            try
            {
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

                // Create environment configuration with additional variables
                var defaultEnvironment = new TestEnvironment
                {
                    Name = environment,
                    Description = $"{environment} environment",
                    Variables = new Dictionary<string, string>
                    {
                        { "baseUrl", baseUrl },
                        { "timeout", "30000" },
                        { "userId", "1" },
                        { "apiKey", "dev-api-key" }
                    }
                };

                // Create production environment as an example
                var productionEnvironment = new TestEnvironment
                {
                    Name = "Production",
                    Description = "Production environment",
                    Variables = new Dictionary<string, string>
                    {
                        { "baseUrl", baseUrl },
                        { "timeout", "10000" },
                        { "userId", "1" },
                        { "apiKey", "prod-api-key" }
                    }
                };

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
                    DefaultEnvironment = environment,
                    Environments = new List<TestEnvironment> { defaultEnvironment, productionEnvironment }
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
                        // Create a manual JSON object directly
                        configJson = @"{
  ""Name"": ""Default"",
  ""Description"": ""Configuration for " + projectName + @""",
  ""DefaultEnvironment"": """ + environment + @""",
  ""Variables"": {
    ""projectId"": """ + projectName.ToLower().Replace(" ", "-") + @""",
    ""version"": ""1.0.0"",
    ""apiVersion"": ""v1""
  },
  ""Environments"": [
    {
      ""Name"": """ + environment + @""",
      ""Description"": """ + environment + @" environment"",
      ""Variables"": {
        ""baseUrl"": """ + baseUrl + @""",
        ""timeout"": ""30000"",
        ""userId"": ""1"",
        ""apiKey"": ""dev-api-key""
      }
    },
    {
      ""Name"": ""Production"",
      ""Description"": ""Production environment"",
      ""Variables"": {
        ""baseUrl"": """ + baseUrl + @""",
        ""timeout"": ""10000"",
        ""userId"": ""1"",
        ""apiKey"": ""prod-api-key""
      }
    }
  ]
}";
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

                // Display help information
                ConsoleHelper.WriteInfo("\nProject initialized successfully!");
                
                // Check if we're running as a compiled executable or via dotnet run
                string exeName = Path.GetFileName(Environment.ProcessPath ?? "apitester");
                bool isCompiledExecutable = !exeName.Equals("dotnet", StringComparison.OrdinalIgnoreCase);
                
                if (isCompiledExecutable)
                {
                    // Running as a standalone executable like ./apify
                    ConsoleHelper.WriteInfo($"To run tests, use: ./{exeName} run sample-api");
                    ConsoleHelper.WriteInfo($"To list environments, use: ./{exeName} list-env");
                    ConsoleHelper.WriteInfo($"Tests will use the configuration from the current directory: {configFilePath}");
                    ConsoleHelper.WriteInfo($"The .apify directory is the default location for your API test files");
                }
                else
                {
                    // Running via dotnet run
                    ConsoleHelper.WriteInfo("To run tests, use: dotnet run run sample-api");
                    ConsoleHelper.WriteInfo("To list environments, use: dotnet run list-env");
                    ConsoleHelper.WriteInfo($"Tests will use the configuration from the current directory: {configFilePath}");
                    ConsoleHelper.WriteInfo($"The .apify directory is the default location for your API test files");
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Error initializing project: {ex.Message}");
            }
        }
    }
}