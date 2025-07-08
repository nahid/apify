using System.CommandLine;
using System.Text.Json;
using Apify.Models;
using Apify.Utils;
using Newtonsoft.Json;

namespace Apify.Commands
{
    public class InitCommand : Command
    {
        public InitCommand() : base("init", "Initialize a new API testing project in the current directory")
        {
            var projectName = new Option<string>(
                "--name",
                "Name of the API testing project"
            );
            AddOption(projectName);
            
            var mockConfigOption = new Option<bool>(
                "--mock",
                () => false,
                "Create sample mock API definitions"
            );
            AddOption(mockConfigOption);
            
            var forceOption = new Option<bool>(
                "--force",
                () => false,
                "Force overwrite of existing files"
            );
            AddOption(forceOption);

            this.SetHandler(
                (name, mock, force) => ExecuteAsync(name, mock, force),
                projectName, mockConfigOption, forceOption
            );
        }

        private async Task ExecuteAsync(string? name, bool mock, bool force)
        {
            bool hasAnyOptions = name != null || mock || force;
            
            ConsoleHelper.WriteHeader("Initializing API Testing Project");

            // Check if configuration file already exists
            string configFilePath = Path.Combine(Directory.GetCurrentDirectory(), RootOption.DefaultConfigFileName);
            
            if (File.Exists(configFilePath) && force)
            {
                if (Directory.Exists(RootOption.DefaultApiDirectory))
                {
                    Directory.Delete(RootOption.DefaultApiDirectory, true);
                }

                File.Delete(configFilePath);
            }

            try
            {
                string projectName;

                if (hasAnyOptions && string.IsNullOrWhiteSpace(name))
                {
                    ConsoleHelper.WriteWarning("Project name is required when using options. Please provide a name.");
                    return;
                }
                // Prompt for required information
                if (hasAnyOptions)
                {
                    projectName = name;
                }
                else
                {
                    projectName = ConsoleHelper.PromptInput<string>("Enter project name", required: true);
                }
                
                string defaultEnvironment = hasAnyOptions ? "Development" : ConsoleHelper.PromptInput<string>("Enter default environment name", "Development", true);

                // Advanced options
                bool configureAdditionalVariables = !hasAnyOptions && ConsoleHelper.PromptYesNo("Configure additional environment variables?");
                Dictionary<string, string> additionalVariables = new Dictionary<string, string>();
                
                if (!hasAnyOptions && configureAdditionalVariables)
                {
                    ConsoleHelper.WriteInfo("Enter environment variables (empty name to finish):");
                    
                    while (true)
                    {
                        string variableName = ConsoleHelper.PromptInput<string>("Variable name", "");
                        if (string.IsNullOrWhiteSpace(variableName)) break;
                        
                        string variableValue = ConsoleHelper.PromptInput<string>($"Value for {variableName}:");
                        additionalVariables[variableName] = variableValue;
                    }
                }

                // Create API directory if it doesn't exist
                if (!Directory.Exists(RootOption.DefaultApiDirectory))
                {
                    Directory.CreateDirectory(RootOption.DefaultApiDirectory);
                    ConsoleHelper.WriteSuccess($"Created API directory: {RootOption.DefaultApiDirectory}");
                }
                else
                {
                    ConsoleHelper.WriteInfo($"Using existing API directory: {RootOption.DefaultApiDirectory}");
                }

                // Create environment configuration
                var defaultEnvVariables = new Dictionary<string, string>();

                // Add user-defined variables
                foreach (var variable in additionalVariables)
                {
                    defaultEnvVariables[variable.Key] = variable.Value;
                }

                var defaultEnv = new EnvironmentSchema
                {
                    Name = defaultEnvironment,
                    Description = $"{defaultEnvironment} environment",
                    Variables = defaultEnvVariables
                };

                // Create production environment as an example
                var productionEnvVariables = new Dictionary<string, string>();

                // Add user-defined variables to production too
                foreach (var variable in additionalVariables)
                {
                    productionEnvVariables[variable.Key] = variable.Value;
                }

                var productionEnvironment = new EnvironmentSchema
                {
                    Name = "Production",
                    Description = "Production environment",
                    Variables = productionEnvVariables
                };

                // Ask if user wants to add additional environments
                List<EnvironmentSchema> environments = new List<EnvironmentSchema> { defaultEnv, productionEnvironment };
                
                if (!hasAnyOptions && ConsoleHelper.PromptYesNo("Add additional environments?", false))
                {
                    while (true)
                    {
                        string envName = ConsoleHelper.PromptInput<string>("EnvironmentSchema name (empty to finish)", "");
                        if (string.IsNullOrWhiteSpace(envName)) break;
                        
                        string envDescription = ConsoleHelper.PromptInput<string>($"Description for {envName}",  $"{envName} environment");
                        
                        var envVariables = new Dictionary<string, string>();
                        
                        // Add user-defined variables
                        foreach (var variable in additionalVariables)
                        {
                            envVariables[variable.Key] = ConsoleHelper.PromptInput<string>($"{variable.Key} for {envName}", variable.Value);
                        }
                        
                        environments.Add(new EnvironmentSchema 
                        { 
                            Name = envName, 
                            Description = envDescription,
                            Variables = envVariables
                        });
                    }
                }

                // Create environment configuration with project-level variables
                var config = new ApifyConfigSchema
                {
                    Name = projectName,
                    Description = $"Configuration for {projectName}",
                    // Add project-level variables (shared across all environments)
                    Variables = new Dictionary<string, string>
                    {
                        { "version", "1.0.0" },
                        { "apiVersion", "v1" },
                        { "apiToken", "reqres-free-v1" },
                        { "baseUrl", "https://reqres.in/api"}
                    },
                    DefaultEnvironment = defaultEnvironment,
                    Environments = environments,
                    MockServer = new MockServer() {
                        Port = 8088,
                        Verbose = true,
                        EnableCors = false,
                        DefaultHeaders = new Dictionary<string, string>() {
                            { "Content-Type", "application/json"}
                        }
                    }
                };
    
                // Create the sample API test file
                var sampleUserApi = new ApiDefinition
                {
                    Name = "Sample User API Test",
                    Uri = "{{env.baseUrl}}/users/1",
                    Method = "GET",
                    Headers = new Dictionary<string, string>
                    {
                        { "Accept", "application/json" },
                        { "x-api-key", "{{env.apiToken}}" }
                    },
                    PayloadType = PayloadType.None,
                    Tests = new List<AssertionEntity>
                    {
                        new AssertionEntity { 
                            Title = "Status code is successful", 
                            Case = "Assert.Response.StatusCodeIs(200)",
                        }
                    }
                };

                // Create an example API file in the apis directory
                string sampleApiFilePath = Path.Combine(RootOption.DefaultApiDirectory, "get.json");
                await File.WriteAllTextAsync(sampleApiFilePath, JsonHelper.SerializeObject(sampleUserApi));
                ConsoleHelper.WriteSuccess($"Created sample API test: {sampleApiFilePath}");
                
                // Create a POST sample with JSON payload
                var samplePostTest = new ApiDefinition
                {
                    Name = "Sample Users Create API Test",
                    Uri = "{{env.baseUrl}}/users",
                    Method = "POST",
                    Headers = new Dictionary<string, string>
                    {
                        { "Accept", "application/json" },
                        { "Content-Type", "application/json" },
                        { "x-api-key", "{{env.apiToken}}" }
                    },
                    Payload = new Dictionary<string, object>
                    {
                        { "name", "{{expr|> Faker.Name.FirstName()}}" },
                        { "job", "{{expr|> Faker.Name.JobTitle()}}" }
                    },
                    PayloadType = PayloadType.Json,
                    Tests = new List<AssertionEntity>
                    {
                        new AssertionEntity { 
                            Title = "Status code is Created", 
                            Case = "Assert.Response.StatusCodeIs(201)",
                        }
                    }
                };
                
                string samplePostFilePath = Path.Combine(RootOption.DefaultApiDirectory, "create.json");
                await File.WriteAllTextAsync(samplePostFilePath, JsonHelper.SerializeObject(samplePostTest));
                ConsoleHelper.WriteSuccess($"Created sample POST API test: {samplePostFilePath}");

                if (!hasAnyOptions)
                {
                    mock = ConsoleHelper.PromptYesNo("Create sample mock API definitions?");
                }
                // Ask if user wants to create mock API examples
                if (mock)
                {
                    // Create users directory for mock examples if it doesn't exist
                    string usersDirPath = Path.Combine(RootOption.DefaultApiDirectory, "users");
                    if (!Directory.Exists(usersDirPath))
                    {
                        Directory.CreateDirectory(usersDirPath);
                    }
                    
                                      // Create a sample Get User by ID mock definition
                    string getUserByIdMockJson = @"{
  ""Name"": ""Mock User by ID"",
  ""Method"": ""GET"",
  ""Endpoint"": ""/api/users/{id}"",
  ""Responses"": [
    {
      ""Condition"": ""q.id == \""2\"""",
      ""StatusCode"": 200,
      ""Headers"": {
        ""X-Apify-Version"": ""{{env.apiKey}}""
      },
      ""ResponseTemplate"": {
        ""id"": 1,
        ""name"": ""{{expr|> Faker.Name.FirstName()}} {{expr|> Faker.Name.LastName()}}"",
        ""email"": ""{{expr|> Faker.Internet.Email()}}""
      }
    },
    {
      ""Condition"": ""true"", 
      ""StatusCode"": 200,
      ""ResponseTemplate"": {
        ""id"": 1,
        ""name"": ""Nahid Bin Azhar"",
        ""email"": ""nahid@jouleslabs.com""
      }
    }
  ]
}";
                    
                    string getUserByIdPath = Path.Combine(usersDirPath, "user.mock.json");
                    
                    await File.WriteAllTextAsync(getUserByIdPath, getUserByIdMockJson);
                    
                    ConsoleHelper.WriteSuccess($"Created sample mock API definitions in {usersDirPath}");
                    ConsoleHelper.WriteInfo("To start the mock server: apify run mock-server --port 8088 --verbose");
                }

          
                string configJson = JsonHelper.SerializeObject(config);
                
                try 
                {
                    string currentDir = Directory.GetCurrentDirectory();
                    ConsoleHelper.WriteInfo($"Current working directory: {currentDir}");
                    ConsoleHelper.WriteInfo($"Creating config file at: {configFilePath}");

                    await File.WriteAllTextAsync(configFilePath, configJson);
                    
                    // Verify file was created
                    if (File.Exists(configFilePath))
                    {
                        ConsoleHelper.WriteSuccess($"Created configuration file: {RootOption.DefaultConfigFileName}");
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

                // Display success message
                ConsoleHelper.WriteSuccess("\nProject initialized successfully!");

                // Check if we're running as a compiled executable or via dotnet run
                string exeName = Path.GetFileName(System.Environment.ProcessPath ?? "apitester");
                bool isCompiledExecutable = !exeName.Equals("dotnet", StringComparison.OrdinalIgnoreCase);
                
                // Display the interactive quick start guide
                ConsoleHelper.DisplayQuickStartGuide(configFilePath, RootOption.DefaultApiDirectory, isCompiledExecutable);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Error initializing project: {ex.Message}");
            }
        }
        
    }
}