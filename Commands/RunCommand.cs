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

            var envOption = new Option<string?>(
                name: "--env",
                description: "Environment file to use for variable substitution");
            envOption.AddAlias("-e");
            Command.AddOption(envOption);

            // Set the handler
            Command.SetHandler(async (files, verbose, env) =>
            {
                await ExecuteRunCommand(files, verbose, env);
            }, fileArgument, verboseOption, envOption);
        }

        private async Task ExecuteRunCommand(string[] filePaths, bool verbose, string? envFile)
        {
            Console.WriteLine("API Tester - Running Tests");
            Console.WriteLine("==========================");

            if (envFile != null)
            {
                Console.WriteLine($"Using environment file: {envFile}");
                // Environment file handling would go here
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
                    Console.WriteLine($"\nProcessing {path}...");
                    
                    var apiDefinition = JsonHelper.DeserializeFromFile<ApiDefinition>(path);
                    if (apiDefinition == null)
                    {
                        ConsoleHelper.WriteError($"Failed to parse {path}");
                        continue;
                    }

                    if (verbose)
                    {
                        DisplayApiDefinition(apiDefinition);
                    }

                    var response = await apiExecutor.ExecuteRequestAsync(apiDefinition);
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

            Console.WriteLine("\n==========================");
            Console.WriteLine($"Test Summary: {passedTests}/{totalTests} tests passed");
            Console.WriteLine("==========================");
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
            Console.WriteLine("\nAPI Definition:");
            Console.WriteLine($"Name: {apiDefinition.Name}");
            Console.WriteLine($"URI: {apiDefinition.Uri}");
            Console.WriteLine($"Method: {apiDefinition.Method}");
            
            if (apiDefinition.Headers?.Count > 0)
            {
                Console.WriteLine("Headers:");
                foreach (var header in apiDefinition.Headers)
                {
                    Console.WriteLine($"  {header.Key}: {header.Value}");
                }
            }

            if (!string.IsNullOrEmpty(apiDefinition.Payload))
            {
                Console.WriteLine("Payload:");
                Console.WriteLine($"  {apiDefinition.Payload}");
            }

            if (apiDefinition.Tests?.Count > 0)
            {
                Console.WriteLine($"Tests: {apiDefinition.Tests.Count} defined");
            }
        }

        private void DisplayTestResults(List<TestResult> results, bool verbose)
        {
            Console.WriteLine("\nTest Results:");
            
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
                        Console.WriteLine($"  Error: {result.ErrorMessage}");
                    }
                }
            }
        }
    }
}
