using APITester.Models;
using APITester.Services;
using APITester.Utils;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace APITester.Commands
{
    public class RunCommand
    {
        public Command Command { get; }

        public RunCommand()
        {
            Command = new Command("run", "Run API tests from JSON definition files");

            // Add arguments
            var fileArgument = new Argument<string[]>(
                name: "files",
                description: "API definition files to test (supports wildcards)")
            {
                Arity = ArgumentArity.OneOrMore
            };
            Command.AddArgument(fileArgument);

            // Add options
            var verboseOption = new Option<bool>(
                name: "--verbose",
                description: "Display detailed output including request/response details");
            verboseOption.AddAlias("-v");
            Command.AddOption(verboseOption);

            var profileOption = new Option<string?>(
                name: "--profile",
                description: "Configuration profile to use (defaults to 'Default')");
            profileOption.AddAlias("-p");
            Command.AddOption(profileOption);

            var environmentOption = new Option<string?>(
                name: "--env",
                description: "Environment to use from the configuration profile");
            environmentOption.AddAlias("-e");
            Command.AddOption(environmentOption);

            // Set the handler
            Command.SetHandler(async (files, verbose, profile, environment) =>
            {
                await ExecuteRunCommand(files, verbose, profile, environment);
            }, fileArgument, verboseOption, profileOption, environmentOption);
        }

        private async Task ExecuteRunCommand(string[] filePaths, bool verbose, string? profileName, string? environmentName)
        {
            ConsoleHelper.DisplayTitle("API Tester - Running Tests");

            var environmentService = new EnvironmentService();
            
            // Load the profile from the current directory
            var profile = environmentService.LoadConfigurationProfile();
            
            if (profile == null)
            {
                ConsoleHelper.WriteInfo("No environment profile found. Creating default profile...");
                environmentService.CreateDefaultEnvironmentFile();
                profile = environmentService.LoadConfigurationProfile();
            }

            // Set active environment - we don't need profile name anymore
            if (!environmentService.SetCurrentEnvironment(environmentName))
            {
                return; // Error message already displayed by the service
            }

            ConsoleHelper.WriteKeyValue("Using Configuration", "From current directory");
            
            var currentEnv = environmentService.CurrentEnvironment;
            ConsoleHelper.WriteKeyValue("Active Environment", currentEnv?.Name ?? "None");
            
            // Display environment variables for better transparency
            if (currentEnv?.Variables?.Count > 0)
            {
                ConsoleHelper.WriteInfo("Environment Variables:");
                foreach (var variable in currentEnv.Variables)
                {
                    if (variable.Key.Contains("key", StringComparison.OrdinalIgnoreCase) || 
                        variable.Key.Contains("password", StringComparison.OrdinalIgnoreCase) ||
                        variable.Key.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
                        variable.Key.Contains("token", StringComparison.OrdinalIgnoreCase))
                    {
                        // Mask sensitive values
                        Console.Write("  ");
                        ConsoleHelper.WriteKeyValue(variable.Key, "********");
                    }
                    else
                    {
                        // Display non-sensitive values
                        Console.Write("  ");
                        ConsoleHelper.WriteKeyValue(variable.Key, variable.Value);
                    }
                }
            }

            int totalTests = 0;
            int passedTests = 0;
            var testRunner = new TestRunner();
            var apiExecutor = new ApiExecutor();

            var expandedPaths = ExpandWildcards(filePaths);
            if (expandedPaths.Count == 0)
            {
                ConsoleHelper.WriteError("No matching files found.");
                return;
            }

            foreach (var path in expandedPaths)
            {
                try
                {
                    ConsoleHelper.WriteSection($"Processing {path}...");
                    
                    // Extract property paths directly from the JSON file before deserialization
                    // This helps us work around issues with property name case sensitivity
                    var propertyPaths = JsonHelper.ExtractPropertyPaths(path);
                    
                    var apiDefinition = JsonHelper.DeserializeFromFile<ApiDefinition>(path);
                    if (apiDefinition == null)
                    {
                        ConsoleHelper.WriteError($"Failed to parse {path}");
                        continue;
                    }
                    
                    // Apply extracted property paths to the test assertions if available
                    if (propertyPaths.Count > 0 && apiDefinition.Tests != null)
                    {
                        foreach (var test in apiDefinition.Tests)
                        {
                            if (propertyPaths.TryGetValue(test.Name, out var propertyPath) && 
                                string.IsNullOrEmpty(test.PropertyPath))
                            {
                                test.PropertyPath = propertyPath;
                                Console.WriteLine($"Applied propertyPath '{propertyPath}' to test '{test.Name}'");
                            }
                        }
                    }

                    // Apply environment variables
                    apiDefinition = environmentService.ApplyEnvironmentVariables(apiDefinition);

                    if (verbose)
                    {
                        DisplayApiDefinition(apiDefinition);
                    }

                    var response = await apiExecutor.ExecuteRequestAsync(apiDefinition);
                    
                    if (verbose)
                    {
                        DisplayApiResponse(response);
                    }
                    
                    var testResults = await testRunner.RunTestsAsync(apiDefinition, response);

                    totalTests += testResults.Count;
                    passedTests += testResults.Count(r => r.Passed);

                    DisplayTestResults(testResults, verbose);
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"Error processing {path}: {ex.Message}");
                }
            }

            ConsoleHelper.WriteSection("==========================");
            ConsoleHelper.WriteKeyValue("Test Summary", $"{passedTests}/{totalTests} tests passed");
            ConsoleHelper.WriteLineColored("==========================", ConsoleColor.Cyan);
        }

        private List<string> ExpandWildcards(string[] filePaths)
        {
            var expandedPaths = new List<string>();
            
            foreach (var path in filePaths)
            {
                if (path.Contains("*") || path.Contains("?"))
                {
                    var directory = Path.GetDirectoryName(path) ?? ".";
                    var filePattern = Path.GetFileName(path);
                    var matchingFiles = Directory.GetFiles(directory, filePattern)
                                                .Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
                    expandedPaths.AddRange(matchingFiles);
                }
                else if (File.Exists(path))
                {
                    expandedPaths.Add(path);
                }
            }

            return expandedPaths;
        }

        private void DisplayApiDefinition(ApiDefinition apiDefinition)
        {
            ConsoleHelper.WriteSection("API Definition:");
            ConsoleHelper.WriteKeyValue("Name", apiDefinition.Name);
            
            Console.Write("URI: ");
            ConsoleHelper.WriteUrl(apiDefinition.Uri);
            
            Console.Write("Method: ");
            ConsoleHelper.WriteMethod(apiDefinition.Method);
            
            if (apiDefinition.Headers?.Count > 0)
            {
                ConsoleHelper.WriteInfo("Headers:");
                foreach (var header in apiDefinition.Headers)
                {
                    Console.Write("  ");
                    ConsoleHelper.WriteKeyValue(header.Key, header.Value);
                }
            }

            if (apiDefinition.Payload != null)
            {
                ConsoleHelper.WriteKeyValue("Payload Type", apiDefinition.PayloadType.ToString());
                ConsoleHelper.WriteInfo("Payload:");
                Console.Write("  ");
                
                if (apiDefinition.PayloadType == PayloadType.Json)
                {
                    try
                    {
                        // Try to format and colorize JSON payload
                        var jsonString = apiDefinition.GetPayloadAsString();
                        if (jsonString != null)
                        {
                            ConsoleHelper.WriteColoredJson(jsonString);
                        }
                        else
                        {
                            Console.WriteLine("[null payload]");
                        }
                    }
                    catch
                    {
                        // If it's not valid JSON or formatting fails, display as-is
                        Console.WriteLine(apiDefinition.Payload);
                    }
                }
                else
                {
                    // For non-JSON payloads, display as-is
                    var payloadString = apiDefinition.GetPayloadAsString();
                    Console.WriteLine(payloadString ?? "[null payload]");
                }
            }
            
            // Display file upload information if present
            if (apiDefinition.Files?.Count > 0)
            {
                ConsoleHelper.WriteInfo($"Files to Upload ({apiDefinition.Files.Count}):");
                foreach (var file in apiDefinition.Files)
                {
                    Console.Write("  ");
                    ConsoleHelper.WriteLineColored($"- {file.Name}", ConsoleColor.Cyan);
                    Console.Write("    ");
                    ConsoleHelper.WriteKeyValue("Field Name", file.FieldName);
                    Console.Write("    ");
                    ConsoleHelper.WriteKeyValue("Path", file.FilePath);
                    Console.Write("    ");
                    ConsoleHelper.WriteKeyValue("Content Type", file.ContentType);
                }
            }

            if (apiDefinition.Tests?.Count > 0)
            {
                ConsoleHelper.WriteKeyValue("Tests", $"{apiDefinition.Tests.Count} defined");
            }
        }

        private void DisplayTestResults(List<TestResult> results, bool verbose)
        {
            ConsoleHelper.WriteSection("Test Results:");
            
            foreach (var result in results)
            {
                if (result.Passed)
                {
                    ConsoleHelper.WriteSuccess($"✓ {result.TestName}");
                }
                else
                {
                    ConsoleHelper.WriteError($"✗ {result.TestName}");
                    if (verbose)
                    {
                        Console.Write("  ");
                        ConsoleHelper.WriteKeyValue("Error", result.ErrorMessage ?? "Unknown error");
                    }
                }
            }
        }
        
        private void DisplayApiResponse(ApiResponse response)
        {
            ConsoleHelper.WriteSection("API Response:");
            ConsoleHelper.WriteStatusCode(response.StatusCode);
            ConsoleHelper.WriteTiming(response.ResponseTimeMs);
            
            if (response.Headers.Count > 0)
            {
                ConsoleHelper.WriteSection("Response Headers:");
                foreach (var header in response.Headers)
                {
                    Console.Write("  ");
                    ConsoleHelper.WriteKeyValue(header.Key, header.Value);
                }
            }
            
            if (response.ContentHeaders.Count > 0)
            {
                ConsoleHelper.WriteSection("Content Headers:");
                foreach (var header in response.ContentHeaders)
                {
                    Console.Write("  ");
                    ConsoleHelper.WriteKeyValue(header.Key, header.Value);
                }
            }
            
            ConsoleHelper.WriteSection("Response Body:");
            try
            {
                // Try to format and colorize JSON for better readability
                ConsoleHelper.WriteColoredJson(response.Body);
            }
            catch
            {
                // If formatting fails, display raw response
                Console.WriteLine(response.Body);
            }
        }
        
        private void ListEnvironments(TestEnvironmentConfig config)
        {
            if (config == null)
            {
                ConsoleHelper.WriteInfo("No environment configuration found.");
                return;
            }
            
            ConsoleHelper.WriteSection("Available Configuration:");
            
            ConsoleHelper.WriteLineColored($"Name: {config.Name}", ConsoleColor.Cyan);
            
            if (!string.IsNullOrEmpty(config.Description))
            {
                ConsoleHelper.WriteLineColored($"  Description: {config.Description}", ConsoleColor.DarkGray);
            }
            
            if (!string.IsNullOrEmpty(config.DefaultEnvironment))
            {
                ConsoleHelper.WriteLineColored($"  Default Environment: {config.DefaultEnvironment}", ConsoleColor.DarkCyan);
            }
            
            ConsoleHelper.WriteLineColored("  Environments:", ConsoleColor.White);
            
            foreach (var env in config.Environments)
            {
                ConsoleHelper.WriteLineColored($"    - {env.Name}", ConsoleColor.Green);
                
                if (!string.IsNullOrEmpty(env.Description))
                {
                    ConsoleHelper.WriteLineColored($"      Description: {env.Description}", ConsoleColor.DarkGray);
                }
                
                ConsoleHelper.WriteLineColored($"      Variables: {env.Variables.Count}", ConsoleColor.DarkYellow);
                
                // Display variable names (not values to protect sensitive information)
                if (env.Variables.Count > 0)
                {
                    var variableNames = string.Join(", ", env.Variables.Keys);
                    ConsoleHelper.WriteLineColored($"      Names: {variableNames}", ConsoleColor.DarkGray);
                }
            }
        }
    }
}
