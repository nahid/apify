using System.CommandLine;
using System.CommandLine.Invocation;
using Apify.Models;
using Apify.Services;
using Apify.Utils;
using Newtonsoft.Json;

namespace Apify.Commands
{
    public class TestsCommand
    {
        public Command Command { get; }
        private const string DefaultApiDirectoryName = ".apify";
        
        public TestsCommand()
        {
            Command = new Command("tests", "Run all tests in the .apify directory");
            
            var verboseOption = new Option<bool>(
                "--verbose",
                () => false,
                "Display detailed test results"
            );
            
            var dirOption = new Option<string>(
                "--dir",
                () => DefaultApiDirectoryName,
                "The directory containing API test files"
            );
            
            var tagOption = new Option<string>(
                "--tag",
                "Filter tests by tag"
            );
            
            Command.AddOption(verboseOption);
            Command.AddOption(dirOption);
            Command.AddOption(tagOption);
            
            Command.SetHandler(
                (verbose, dir, tag) => RunAllTestsAsync(verbose, dir, tag),
                verboseOption, dirOption, tagOption
            );
        }
        
        private async Task RunAllTestsAsync(bool verbose, string directory, string? tag)
        {
            ConsoleHelper.DisplayTitle("Apify - Running All Tests");
            
            if (!Directory.Exists(directory))
            {
                ConsoleHelper.WriteError($"Directory {directory} does not exist!");
                return;
            }
            
            var apiFiles = GetApiFiles(directory);
            
            if (apiFiles.Count == 0)
            {
                ConsoleHelper.WriteWarning($"No API definition files found in {directory}");
                return;
            }
            
            var environmentService = new EnvironmentService();
            await environmentService.LoadConfig();
            
            int totalTests = 0;
            int totalPassed = 0;
            int totalFailed = 0;
            var testResults = new List<(string ApiName, string TestName, bool Success, string? ErrorMessage)>();
            long totalResponseTime = 0;
            
            var startTime = DateTime.Now;
            
            // Spinner characters for animation
            var spinner = new string[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
            var spinnerIndex = 0;
            var spinnerTimer = new System.Timers.Timer(100); // Update spinner every 100ms
            
            // Setup a timer to update the spinner animation
            spinnerTimer.Elapsed += (sender, e) => 
            {
                spinnerIndex++;
                // Only update if we're not at the end of a line (to avoid flickering)
                try
                {
                    int cursorLeft = Console.CursorLeft;
                    int cursorTop = Console.CursorTop;
                    
                    if (cursorLeft > 2) // Make sure we're not at beginning of line
                    {
                        Console.SetCursorPosition(1, cursorTop);
                        Console.Write(spinner[spinnerIndex % spinner.Length]);
                        Console.SetCursorPosition(cursorLeft, cursorTop);
                    }
                }
                catch
                {
                    // Ignore any console errors - they can happen if window is resized
                }
            };
            spinnerTimer.Start();
            
            for (int i = 0; i < apiFiles.Count; i++)
            {
                var apiFile = apiFiles[i];
                
                // Display progress with file counter and clear the whole line first
                Console.Write($"\r{new string(' ', Console.WindowWidth - 1)}"); // Clear line
                Console.Write($"\r[{spinner[spinnerIndex % spinner.Length]}] Processing {i+1}/{apiFiles.Count}: {Path.GetFileName(apiFile)}");
                
                try
                {
                    string json = await File.ReadAllTextAsync(apiFile);
                    ApiDefinition? apiDefinition = JsonConvert.DeserializeObject<ApiDefinition>(json);
                    
                    if (apiDefinition == null)
                    {
                        Console.WriteLine();
                        ConsoleHelper.WriteError($"Failed to parse API definition from {apiFile}");
                        continue;
                    }
                    
                    // Skip if tag filtering is enabled and this API doesn't match
                    if (!string.IsNullOrEmpty(tag) && 
                        (apiDefinition.Tags == null || !apiDefinition.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))
                    {
                        continue;
                    }
                    
                    // Show current API being processed with highlighted box
                    Console.WriteLine();
                    Console.WriteLine(new string('─', 50));
                    ConsoleHelper.WriteLineColored($"▶ TESTING: {apiDefinition.Name}", ConsoleColor.Cyan);
                    Console.WriteLine(new string('─', 50));
                    
                    // Apply environment variables (this was missing!)
                    apiDefinition = environmentService.ApplyEnvironmentVariables(apiDefinition);
                    
                    var apiExecutor = new ApiExecutor();
                    apiExecutor.SetEnvironmentService(environmentService);
                    
                    var testRunner = new TestRunner();
                    testRunner.SetApiExecutor(apiExecutor);
                    
                    var testResult = await testRunner.RunApiTestAsync(apiDefinition, verbose);
                    
                    if (testResult != null)
                    {
                        totalResponseTime += testResult.ResponseTimeMs;
                        
                        // Count tests and results
                        foreach (var assertion in testResult.AssertionResults)
                        {
                            totalTests++;
                            bool isSuccess = assertion.Success;
                            if (isSuccess)
                            {
                                totalPassed++;
                            }
                            else
                            {
                                totalFailed++;
                            }
                            
                            testResults.Add((apiDefinition.Name, assertion.Name, isSuccess, assertion.ErrorMessage));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    ConsoleHelper.WriteError($"Error processing {apiFile}: {ex.Message}");
                }
            }
            
            var endTime = DateTime.Now;
            var executionTime = (endTime - startTime).TotalSeconds;
            
            // Stop and dispose the spinner timer
            spinnerTimer.Stop();
            spinnerTimer.Dispose();
            
            // Display summary
            Console.WriteLine();
            Console.WriteLine();
            ConsoleHelper.DisplayTitle("Test Results Summary");
            
            if (testResults.Count == 0)
            {
                ConsoleHelper.WriteWarning("No tests were executed!");
                return;
            }
            
            ConsoleHelper.WriteKeyValue("Total API Files", $"{apiFiles.Count}");
            ConsoleHelper.WriteKeyValue("Total Tests", $"{totalTests}");
            ConsoleHelper.WriteKeyValue("Passed", $"{totalPassed}");
            ConsoleHelper.WriteKeyValue("Failed", $"{totalFailed}");
            ConsoleHelper.WriteKeyValue("Success Rate", $"{(totalTests > 0 ? (double)totalPassed / totalTests * 100 : 0):F2}%");
            ConsoleHelper.WriteKeyValue("Total Execution Time", $"{executionTime:F2} seconds");
            
            if (totalTests > 0)
            {
                ConsoleHelper.WriteKeyValue("Average Response Time", $"{(double)totalResponseTime / totalTests:F2} ms");
            }
            
            // Display failed tests if any
            if (totalFailed > 0)
            {
                Console.WriteLine();
                ConsoleHelper.WriteHeader("Failed Tests:");
                foreach (var result in testResults.Where(r => !r.Success))
                {
                    ConsoleHelper.WriteWarning($"- [{result.ApiName}] {result.TestName}: {result.ErrorMessage}");
                }
            }
        }
        
        private List<string> GetApiFiles(string directory)
        {
            var result = new List<string>();
            
            try
            {
                // Get all JSON files from the directory and its subdirectories
                result.AddRange(Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories));
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Error scanning directory: {ex.Message}");
            }
            
            return result;
        }
    }
}