using Apify.Models;
using Apify.Services;
using Apify.Utils;
using System.CommandLine;

namespace Apify.Commands
{
    public class CallCommand
    {
        public Command Command { get; }

        public CallCommand()
        {
            Command = new Command("call", "Call API from the .apify directory (uses simplified path format)");

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

            var vars = new Option<string?>(
                name: "--vars",
                description: "Runtime variables for the configuration");
            
            Command.AddOption(vars);

            // Set the handler
            Command.SetHandler(async (file, verbose, variables, environment, debug) =>
            {
                await ExecuteRunCommand(file, verbose, variables, environment, debug);
            }, fileArgument, verboseOption, vars, environmentOption, RootOption.DebugOption);
        }

        private async Task ExecuteRunCommand(string filePath, bool verbose, string? varString, string? environmentName, bool debug)
        {
            ConsoleHelper.DisplayTitle("Apify - API Request Runner");

            var configService = new ConfigService(debug);
            var envName = environmentName ?? configService.LoadConfiguration()?.DefaultEnvironment ?? "Development";
            var apiExecutor = new ApiExecutor();

            //var expandedPaths = ExpandWildcards(filePath);
            var path = MiscHelper.HandlePath(filePath, RootOption.DefaultApiDirectory);

            try
            {
                ConsoleHelper.WriteSection($"Processing {path}...");
                
                var apiDefinition = JsonHelper.DeserializeFromFile<ApiDefinition>(path);
                if (apiDefinition == null)
                {
                    ConsoleHelper.WriteError($"Failed to parse {path}");
                    return;
                }

                var variables = MiscHelper.ParseArgsVariables(varString ?? "");
                var pathVars = new Dictionary<string, Dictionary<string, string>>();
                pathVars.Add("vars", variables);
                
                apiDefinition = apiExecutor.ApplyEnvToApiDefinition(apiDefinition, envName, pathVars);
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
        
    }
}
