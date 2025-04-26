using System.CommandLine;
using System.Text.Json;
using APITester.Models;
using APITester.Utils;
using Newtonsoft.Json;

namespace APITester.Commands
{
    public class InitCommand : Command
    {
        private const string DefaultConfigFileName = "apify-config.json";
        private const string DefaultApiDirectoryName = "apis";

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
            if (File.Exists(DefaultConfigFileName) && !force)
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

                // Create environment configuration
                var defaultEnvironment = new TestEnvironment
                {
                    Name = environment,
                    Description = $"{environment} environment",
                    Variables = new Dictionary<string, string>
                    {
                        { "baseUrl", baseUrl },
                        { "timeout", "30000" }
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
                        { "timeout", "30000" }
                    }
                };

                // Create configuration profile
                var configProfile = new ConfigurationProfile
                {
                    Name = "Default",
                    Description = $"Configuration for {projectName}",
                    DefaultEnvironment = environment,
                    Environments = new List<TestEnvironment> { defaultEnvironment, productionEnvironment }
                };

                // Create the sample API test file
                var sampleApiTest = new ApiDefinition
                {
                    Name = "Sample API Test",
                    Uri = "{{baseUrl}}/users/1",
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
                            Name = "Response contains user data", 
                            Description = "Response contains user id",
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
                            Name = "Status code is created", 
                            Description = "Status code is 201",
                            AssertType = "StatusCode",
                            ExpectedValue = "201"
                        },
                        new TestAssertion { 
                            Name = "Response contains post ID", 
                            Description = "Response contains id property",
                            AssertType = "ContainsProperty",
                            ExpectedValue = "id"
                        }
                    }
                };
                
                string samplePostFilePath = Path.Combine(DefaultApiDirectoryName, "sample-post.json");
                await File.WriteAllTextAsync(samplePostFilePath, JsonHelper.SerializeObject(samplePostTest));
                ConsoleHelper.WriteSuccess($"Created sample POST API test: {samplePostFilePath}");

                // Save the configuration file
                try
                {
                    string configJson = JsonHelper.SerializeObject(configProfile);
                    
                    // Verify that the serialized JSON is valid by attempting to deserialize it
                    var testDeserialized = JsonConvert.DeserializeObject<ConfigurationProfile>(configJson);
                    if (testDeserialized == null || testDeserialized.Environments == null || testDeserialized.Environments.Count == 0)
                    {
                        ConsoleHelper.WriteError("Generated invalid configuration. Using fallback.");
                        // Create a minimal valid JSON as fallback
                        configJson = @"{
  ""Name"": ""Default"",
  ""Description"": ""Configuration for " + projectName + @""",
  ""DefaultEnvironment"": """ + environment + @""",
  ""Environments"": [
    {
      ""Name"": """ + environment + @""",
      ""Description"": """ + environment + @" environment"",
      ""Variables"": {
        ""baseUrl"": """ + baseUrl + @""",
        ""timeout"": ""30000""
      }
    }
  ]
}";
                    }
                    
                    await File.WriteAllTextAsync(DefaultConfigFileName, configJson);
                    ConsoleHelper.WriteSuccess($"Created configuration file: {DefaultConfigFileName}");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"Error creating configuration file: {ex.Message}");
                }

                // Display help information
                ConsoleHelper.WriteInfo("\nProject initialized successfully!");
                ConsoleHelper.WriteInfo("To run tests, use: dotnet run run apis/sample-api.json");
                ConsoleHelper.WriteInfo("To list environments, use: dotnet run list-env");
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Error initializing project: {ex.Message}");
            }
        }
    }
}