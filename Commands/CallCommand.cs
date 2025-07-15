using Apify.Models;
using Apify.Services;
using Apify.Utils;
using Newtonsoft.Json.Linq;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace Apify.Commands
{
    public class CallCommand: Command
    {
        public CallCommand(): base("call", "Call API from the .apify directory (uses simplified path format)")
        {
            // Add arguments
            var fileArgument = new Argument<string>(
                name: "file",
                description: "API definition files to test (located in .apify directory by default, no need to specify file extension, supports dot notation for nested directories and wildcards)")
            {
                Arity = ArgumentArity.ExactlyOne
            };
            AddArgument(fileArgument);

            // Add options
            var verboseOption = new Option<bool>(
                name: "--verbose",
                description: "Display detailed output including request/response details");
            verboseOption.AddAlias("-v");
            
            var environmentOption = new Option<string?>(
                name: "--env",
                description: "EnvironmentSchema to use from the configuration profile");
            environmentOption.AddAlias("-e");

            var vars = new Option<string?>(
                name: "--vars",
                description: "Runtime variables for the configuration");
            
            var tests = new Option<bool>(
                name: "--tests",
                () => false,
                description: "Run tests defined in the API definition");
            tests.AddAlias("-t");
            
            var showRequest = new Option<bool>(
                name: "--show-request",
                () => false,
                description: "Display the request details");
            showRequest.AddAlias("-sr");
            
            var showResponse = new Option<bool>(
                name: "--show-response",
                () => false,
                description: "Display the response details");
            showResponse.AddAlias("-srp");
            
            var showOnlyResponse = new Option<bool>(
                name: "--show-only-response",
                () => false,
                description: "Display only the response details without request");
            showOnlyResponse.AddAlias("-r");
            
            

            AddOption(verboseOption);
            AddOption(environmentOption);
            AddOption(vars);
            AddOption(tests);
            AddOption(showRequest);
            AddOption(showResponse);
            AddOption(showOnlyResponse);
            
            this.SetHandler(async (InvocationContext context) =>
            {
                // Get the parsed arguments and options
                var file = context.ParseResult.GetValueForArgument(fileArgument);
                var verbose = context.ParseResult.GetValueForOption(verboseOption);
                var environment = context.ParseResult.GetValueForOption(environmentOption);
                var variables = context.ParseResult.GetValueForOption(vars);
                var debug = context.ParseResult.GetValueForOption(RootOption.DebugOption);
                
                var options = new CallCommandOptions(
                    FilePath: file,
                    Vars: variables,
                    Environment: environment,
                    Tests: context.ParseResult.GetValueForOption(tests),
                    ShowRequest: context.ParseResult.GetValueForOption(showRequest),
                    ShowResponse: context.ParseResult.GetValueForOption(showResponse),
                    ShowOnlyResponse: context.ParseResult.GetValueForOption(showOnlyResponse),
                    Verbose: verbose,
                    Debug: debug
                );
                
                await ExecuteRunCommand(options);
            });
            
        }

        private async Task ExecuteRunCommand(CallCommandOptions options)
        {
           // ConsoleHelper.DisplayTitle("Apify - API Request Runner");

            var configService = new ConfigService(options.Debug);
            var envName = options.Environment ?? configService.LoadConfiguration()?.DefaultEnvironment ?? "Development";
            var apiExecutor = new ApiExecutor(new ApiExecutorOptions (
                Tests: options.Tests,
                ShowRequest: options.ShowRequest,
                ShowResponse: options.ShowResponse,
                ShowOnlyResponse: options.ShowOnlyResponse,
                Verbose: options.Verbose,
                Debug: options.Debug
            ));

            //var expandedPaths = ExpandWildcards(filePath);
            var path = MiscHelper.HandlePath(options.FilePath);

            try
            {
                var apiDefinition = JsonHelper.DeserializeFromFile<ApiDefinition>(path);
                

                if (apiDefinition == null)
                {
                    ConsoleHelper.WriteError($"Failed to parse {path}");
                    return;
                }

                var variables = MiscHelper.ParseArgsVariables(options.Vars ?? "");
                var argVars = new Dictionary<string, Dictionary<string, string>>();
                argVars.Add("vars", variables);
                
                apiDefinition = apiExecutor.ApplyEnvToApiDefinition(apiDefinition, envName, argVars);
            
                apiExecutor.DisplayApiDefinition(apiDefinition);
        

                var response = await apiExecutor.ExecuteRequestAsync(apiDefinition);
              
                apiExecutor.DisplayApiResponse(response);
                

                var assertionExecutor = new AssertionExecutor(response, apiDefinition);
                var testResults = await assertionExecutor.RunAsync(apiDefinition.Tests ?? new List<AssertionEntity>());
                
                apiExecutor.DisplayTestStats(testResults);
                apiExecutor.DisplayTestResults(testResults);
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Error processing {path}: {ex.Message}");
            }
         
        }
        
    }
    
    public record CallCommandOptions(
        string FilePath,
        string? Vars,
        string? Environment,
        bool Tests,
        bool ShowRequest,
        bool ShowResponse,
        bool ShowOnlyResponse,
        bool Verbose,
        bool Debug
    );
}
