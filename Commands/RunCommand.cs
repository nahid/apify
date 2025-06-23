using Apify.Models;
using Apify.Services;
using Apify.Utils;
using System.CommandLine;

namespace Apify.Commands
{
    public class RunCommand
    {
        public Command Command { get; }

        public RunCommand()
        {
            Command = new Command("run", "Run API tests from the .apify directory (uses simplified path format)");

            // Add arguments
            var fileArgument = new Argument<string>(
                name: "file",
                description: "API definition files to test (located in .apify directory by default, no need to specify file extension, supports dot notation for nested directories and wildcards)")
            {
                Arity = ArgumentArity.ExactlyOne
            };
            Command.AddArgument(fileArgument);

            // Add options
            var verboseOption = new Option<bool>(
                name: "--verbose",
                description: "Display detailed output including request/response details");
            verboseOption.AddAlias("-v");
            Command.AddOption(verboseOption);

            var environmentOption = new Option<string?>(
                name: "--env",
                description: "EnvironmentSchema to use from the configuration profile");
            environmentOption.AddAlias("-e");
            Command.AddOption(environmentOption);

            // Set the handler
            Command.SetHandler(async (file, verbose, environment, debug) =>
            {
                await ExecuteRunCommand(file, verbose, environment, debug);
            }, fileArgument, verboseOption, environmentOption, RootCommand.DebugOption);
        }

        private async Task ExecuteRunCommand(string filePath, bool verbose, string? environmentName, bool debug)
        {
            ConsoleHelper.DisplayTitle("Apify - API Request Runner");

            var configService = new ConfigService(debug);
            var envName = environmentName ?? configService.LoadConfiguration()?.DefaultEnvironment ?? "Development";
            var apiExecutor = new ApiExecutor();

            //var expandedPaths = ExpandWildcards(filePath);
            var path = ProcessFilePath(filePath);

            try
            {
                ConsoleHelper.WriteSection($"Processing {path}...");
                
                var apiDefinition = JsonHelper.DeserializeFromFile<ApiDefinition>(path);
                if (apiDefinition == null)
                {
                    ConsoleHelper.WriteError($"Failed to parse {path}");
                    return;
                }
                
                apiDefinition = apiExecutor.ApplyEnvToApiDefinition(apiDefinition, envName);
                if (verbose)
                {
                    apiExecutor.DisplayApiDefinition(apiDefinition);
                }

                var response = await apiExecutor.ExecuteRequestAsync(apiDefinition);
                if (verbose)
                {
                    apiExecutor.DisplayApiResponse(response);
                }

                var assertionExecutor = new AssertionExecutor(response);
                var testResults = await assertionExecutor.RunAsync(apiDefinition.Tests ?? new List<AssertionEntity>());
                
                apiExecutor.DisplayTestStats(testResults, verbose);
                apiExecutor.DisplayTestResults(testResults, verbose);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Error processing {path}: {ex.Message}");
            }
         
        }

        private const string DefaultApiDirectory = ".apify";

        private List<string> ExpandWildcards(string[] filePaths)
        {
            var expandedPaths = new List<string>();
            
            foreach (var originalPath in filePaths)
            {
                // Use the ProcessFilePath method to ensure consistent behavior with CreateRequestCommand
                string finalPath = ProcessFilePath(originalPath);
                
                // STEP 5: Handle wildcards, or add the path if the file exists
                if (finalPath.Contains("*") || finalPath.Contains("?"))
                {
                    var directory = Path.GetDirectoryName(finalPath) ?? ".";
                    var filePattern = Path.GetFileName(finalPath);
                    
                    if (Directory.Exists(directory))
                    {
                        var matchingFiles = Directory.GetFiles(directory, filePattern)
                                                  .Where(f => f.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
                        expandedPaths.AddRange(matchingFiles);
                        
                        if (!matchingFiles.Any())
                        {
                            ConsoleHelper.WriteWarning($"No files matching '{filePattern}' found in directory '{directory}'");
                        }
                    }
                    else
                    {
                        ConsoleHelper.WriteError($"Directory does not exist: {directory}");
                    }
                }
                else if (File.Exists(finalPath))
                {
                    expandedPaths.Add(finalPath);
                }
                else
                {
                    // Show detailed error for missing file
                    ConsoleHelper.WriteError($"Could not find file: {finalPath}");
                    
                    // Display how the path was processed for debugging
                    if (originalPath != finalPath)
                    {
                        ConsoleHelper.WriteInfo($"Original input was '{originalPath}', which was processed as '{finalPath}'");
                    }
                    
                    // Check if the directory exists
                    var dir = Path.GetDirectoryName(finalPath);
                    if (dir != null && !Directory.Exists(dir))
                    {
                        ConsoleHelper.WriteError($"Directory does not exist: {dir}");
                    }
                }
            }

            return expandedPaths;
        }
        

        
        /// <summary>
        /// Process a file path using the same logic as in CreateRequestCommand.
        /// This ensures consistent behavior between commands.
        /// </summary>
        private string ProcessFilePath(string filePath)
        {
            
            // Apply the same logic as in CreateRequestCommand to handle dot notation
            // and ensure .json extension 
            
            // Start with original path
            string processedPath = filePath;
            
            if (processedPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                if (Path.IsPathRooted(processedPath))
                {
                    // If it's an absolute path, just return it
                    return processedPath;
                }
                
                if (!Directory.Exists(DefaultApiDirectory))
                {
                    throw new DirectoryNotFoundException($"Default API directory '{DefaultApiDirectory}' does not exist. Please create it or specify a different path.");
                }
                
                // If it already has .json extension, just return it
                return Path.Combine(DefaultApiDirectory, filePath);
            }
            
            if (!Directory.Exists(DefaultApiDirectory))
            {
                throw new DirectoryNotFoundException($"Default API directory '{DefaultApiDirectory}' does not exist. Please create it or specify a different path.");
            }
            
            var pathWithoutExtension = filePath.Replace(".", Path.DirectorySeparatorChar.ToString());
            filePath = Path.Combine(DefaultApiDirectory, pathWithoutExtension + ".json");

            return filePath;
        }
        

    }
}
