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
            var profiles = environmentService.LoadConfigurationProfiles();
            
            // If there are no environment profiles, create a default one
            if (profiles.Count == 0)
            {
                ConsoleHelper.WriteInfo("No environment profiles found. Creating default profile...");
                environmentService.CreateDefaultEnvironmentFile();
                profiles = environmentService.LoadConfigurationProfiles();
            }

            // Set active environment
            string profileToUse = profileName ?? "Default";
            if (!environmentService.SetCurrentEnvironment(profileToUse, environmentName))
            {
                return; // Error message already displayed by the service
            }

            ConsoleHelper.WriteKeyValue("Using Profile", profileToUse);
            ConsoleHelper.WriteKeyValue("Active Environment", environmentService.CurrentEnvironment?.Name ?? "None");

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
                    
                    var apiDefinition = JsonHelper.DeserializeFromFile<ApiDefinition>(path);
                    if (apiDefinition == null)
                    {
                        ConsoleHelper.WriteError($"Failed to parse {path}");
                        continue;
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

            if (!string.IsNullOrEmpty(apiDefinition.Payload))
            {
                ConsoleHelper.WriteInfo("Payload:");
                Console.Write("  ");
                try
                {
                    // Try to format and colorize JSON payload
                    ConsoleHelper.WriteColoredJson(apiDefinition.Payload);
                }
                catch
                {
                    // If it's not valid JSON or formatting fails, display as-is
                    Console.WriteLine(apiDefinition.Payload);
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
        
        private void ListEnvironments(List<ConfigurationProfile> profiles)
        {
            if (profiles.Count == 0)
            {
                ConsoleHelper.WriteInfo("No environment profiles found.");
                return;
            }
            
            ConsoleHelper.WriteSection("Available Configuration Profiles:");
            
            foreach (var profile in profiles)
            {
                ConsoleHelper.WriteLineColored($"Profile: {profile.Name}", ConsoleColor.Cyan);
                
                if (!string.IsNullOrEmpty(profile.Description))
                {
                    ConsoleHelper.WriteLineColored($"  Description: {profile.Description}", ConsoleColor.DarkGray);
                }
                
                if (!string.IsNullOrEmpty(profile.DefaultEnvironment))
                {
                    ConsoleHelper.WriteLineColored($"  Default Environment: {profile.DefaultEnvironment}", ConsoleColor.DarkCyan);
                }
                
                ConsoleHelper.WriteLineColored("  Environments:", ConsoleColor.White);
                
                foreach (var env in profile.Environments)
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
                
                Console.WriteLine(); // Add blank line between profiles
            }
        }
    }
}
