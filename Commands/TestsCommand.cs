using System.CommandLine;
using System.CommandLine.Invocation;
using Apify.Models;
using Apify.Services;
using Apify.Utils;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace Apify.Commands
{
    public class TestsCommand: Command
    {
        private const string DefaultApiDirectoryName = ".apify";
        
        public TestsCommand(): base("tests", "Run all tests in the .apify directory")
        {
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
            
            var environmentOption = new Option<string?>(
                name: "--env",
                description: "EnvironmentSchema to use from the configuration profile");
            environmentOption.AddAlias("-e");
            AddOption(environmentOption);    
            
            var varsOption = new Option<string?>(
                name: "--vars",
                description: "Runtime variables for configurations");
            AddOption(varsOption);
            
            var tagOption = new Option<string>(
                "--tag",
                "Filter tests by tag"
            );
            
            AddOption(verboseOption);
            AddOption(dirOption);
            AddOption(tagOption);
            
            this.SetHandler(
                (verbose, dir, envName, vars, tag, debug) => RunAllTestsAsync(verbose, dir, envName, vars, tag, debug),
                verboseOption, dirOption, environmentOption, varsOption, tagOption, RootOption.DebugOption
            );
        }
        
        private async Task RunAllTestsAsync(bool verbose, string directory, string? envName, string? vars, string? tag, bool debug = false)
        {
            var configService = new ConfigService();
            envName = envName ?? configService.LoadConfiguration()?.DefaultEnvironment ?? "Development";
            
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
            
            int totalTests = 0;
            long totalResponseTime = 0;
            var totalTestResults = new AllTestResults();
            
            var startTime = DateTime.Now;
            
            Console.Clear();
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.Write("Testing  ");

            var spinner = new SpinnerAnimation();
            spinner.Start();

            var cursorPosition = 0;
            
            for (int i = 0; i < apiFiles.Count; i++)
            {
                cursorPosition = i + 1;
                var apiFile = apiFiles[i];
                Console.SetCursorPosition(0, i + 1);
                
                try
                {
                    string json = await File.ReadAllTextAsync(apiFile);
                    var apiDefinition = JsonConvert.DeserializeObject<ApiDefinition>(json);
                    
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
                    
                    Console.SetCursorPosition(0, cursorPosition);
                    // Show current API being processed with highlighted box
                    ConsoleHelper.WriteLineColored($"▶ TESTING: {apiDefinition.Name}", ConsoleColor.Cyan);
                    
                    
                    var apiExecutor = new ApiExecutor();
                    
                    var variables = MiscHelper.ParseArgsVariables(vars ?? "");
                    var runtimeVars = new Dictionary<string, Dictionary<string, string>>();
                    runtimeVars.Add("vars", variables);
                    
                    apiDefinition = apiExecutor.ApplyEnvToApiDefinition(apiDefinition, envName, runtimeVars);
                    var response = await apiExecutor.ExecuteRequestAsync(apiDefinition);
                    
                    var assertionExecutor = new AssertionExecutor(response, apiDefinition);
                    
                    var testResults = await assertionExecutor.RunAsync(apiDefinition.Tests ?? new List<AssertionEntity>());
                    totalTestResults.AddTestResults(apiDefinition.Uri, testResults);
                    
                    Console.SetCursorPosition(0, cursorPosition);
                    if (testResults.FailedCount > 0)
                    {
                        ConsoleHelper.WriteError($"✗ FAILED: {apiDefinition.Name} ({response.ResponseTimeMs:F2} ms)");
                    }
                    else
                    {
                        ConsoleHelper.WriteSuccess($"✓ PASSED: {apiDefinition.Name} ({response.ResponseTimeMs:F2} ms)");
                    }
                    
                    //apiExecutor.DisplayTestResults(testResults, verbose);

                    totalTests += 1;
                    totalResponseTime += response.ResponseTimeMs;

                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    ConsoleHelper.WriteError($"Error processing {apiFile}: {ex.Message}");
                }
            }

            spinner.Stop();
            
            Console.SetCursorPosition(0, 0);
            ConsoleHelper.WritePrompt("Completed!");

            var endTime = DateTime.Now;
            var executionTime = (endTime - startTime).TotalSeconds;
       
            Console.SetCursorPosition(0, cursorPosition + 1);
            
            Console.WriteLine();
            ConsoleHelper.WriteColored("Test Summary", ConsoleColor.DarkYellow);
            Console.WriteLine();
            ConsoleHelper.WriteRepeatChar('=', Console.WindowWidth - 1);
            ConsoleHelper.WriteKeyValue("Result: ", $"{totalTests}/{totalTestResults.GetTotalPassed()} tests passed");
            if (totalTests > 0)
            {
                ConsoleHelper.WriteKeyValue("Total Execution Time", $"{executionTime:F2} seconds");
                ConsoleHelper.WriteKeyValue("Total Response Time", $"{(double)(totalResponseTime / 1000.0):F2} seconds");
                ConsoleHelper.WriteKeyValue("Average Response Time", $"{(double)(totalResponseTime / totalTests):F2} ms");
            }
            
            ConsoleHelper.WriteRepeatChar('=', Console.WindowWidth - 1);
            
            Console.WriteLine();
            ConsoleHelper.WriteColored("Test Report", ConsoleColor.DarkYellow);
            Console.WriteLine();
            ConsoleHelper.WriteRepeatChar('=', 20);

            var apiExec = new ApiExecutor(new ApiExecutorOptions (
                Tests: true,
                ShowRequest: false,
                ShowResponse: false,
                ShowOnlyResponse: false,
                Verbose: verbose,
                Debug: debug
            ));
            
            foreach (var testResult in totalTestResults.GetAllResults())
            {
                foreach (var tResult in testResult)
                {
                    Console.WriteLine();
                    ConsoleHelper.WriteColored(tResult.Key, ConsoleColor.Cyan);
                    Console.WriteLine();
                    ConsoleHelper.WriteRepeatChar('-', tResult.Key.Length);
                    apiExec.DisplayTestResults(tResult.Value);
                }

                
            }
        }
        
        private List<string> GetApiFiles(string directory)
        {
            var result = new List<string>();
            
            try
            {
                var allFiles = Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories);
                var allApiFiles = allFiles.Where(f => !f.EndsWith(".mock.json", StringComparison.OrdinalIgnoreCase))
                .ToArray();
                // Get all JSON files from the directory and its subdirectories
                result.AddRange(allApiFiles);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Error scanning directory: {ex.Message}");
            }
            
            return result;
        }
  
    }

    public class AllTestResults
    {
        private List<Dictionary<string, TestResults>> _allTestResults = new List<Dictionary<string, TestResults>>();
        
        public void AddTestResults(string name, TestResults results)
        {
            var resultsDict = new Dictionary<string, TestResults>();
            resultsDict.Add(name, results);
            
            _allTestResults.Add(resultsDict);
        }

        public List<Dictionary<string, TestResults>> GetAllResults()
        {
            return _allTestResults;
        }    
        
        
        public int GetTotalAssertions()
        {
            return _allTestResults.Sum(r => r.Values.Sum(v => v.Results.Count));
        }
        
        public int GetTotalAssertionPassed()
        {
            return _allTestResults.Sum(r => r.Values.Sum(v => v.PassedCount));
        }
        
        public int GetTotalAssertionFailed()
        {
            return _allTestResults.Sum(r => r.Values.Sum(v => v.FailedCount));
        }
        
           
        public int GetTotalPassed()
        {
            return _allTestResults.Sum(r => r.Values.Sum(v => v.IsPassed() ? 1 : 0));
        }     
        public int GetTotalFailed()
        {
            return _allTestResults.Sum(r => r.Values.Sum(v => v.IsPassed() ? 0 : 1));
        }
        
        public bool IsPassed()
        {
            return GetTotalAssertionFailed() == 0;
        }
        
    }
 
}