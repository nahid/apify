using Apify.Models;
using Apify.Services;
using Apify.Utils;
using Bogus;
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
            
            var testsOption = new Option<bool>(
                name: "--tests",
                description: "Run tests defined in the API definition");
            testsOption.AddAlias("-t");
            
            var showRequestOption = new Option<bool>(
                name: "--show-request",
                description: "Display the request details");
            showRequestOption.AddAlias("-sr");
            
            var showResponseOption = new Option<bool>(
                name: "--show-response",
                description: "Display the response details");
            showResponseOption.AddAlias("-srp");
            
            var showOnlyResponseOption = new Option<bool>(
                name: "--show-only-response",
                description: "Display only the response details without request");
            showOnlyResponseOption.AddAlias("-r");
            
            

            AddOption(verboseOption);
            AddOption(environmentOption);
            AddOption(vars);
            AddOption(testsOption);
            AddOption(showRequestOption);
            AddOption(showResponseOption);
            AddOption(showOnlyResponseOption);
            
            
            
            this.SetHandler(async (InvocationContext context) =>
            {
                // Get the parsed arguments and options
                var file = context.ParseResult.GetValueForArgument(fileArgument);
                var verbose = context.ParseResult.GetValueForOption(verboseOption);
                var environment = context.ParseResult.GetValueForOption(environmentOption);
                var variables = context.ParseResult.GetValueForOption(vars);
                var debug = context.ParseResult.GetValueForOption(RootOption.DebugOption);
               var tests = context.ParseResult.GetValueForOption(testsOption);
               var showResponse = context.ParseResult.GetValueForOption(showResponseOption);
               var showOnlyResponse = context.ParseResult.GetValueForOption(showOnlyResponseOption);
               var showRequest = context.ParseResult.GetValueForOption(showRequestOption);

               
               var options = new CallCommandOptions(
                    FilePath: file,
                    Vars: variables,
                    Environment: environment,
                    Tests: context.ParseResult.FindResultFor(testsOption) != null && tests,
                    ShowRequest: context.ParseResult.FindResultFor(showRequestOption) != null && showRequest,
                    ShowResponse: context.ParseResult.FindResultFor(showResponseOption) != null && showResponse,
                    ShowOnlyResponse: context.ParseResult.FindResultFor(showOnlyResponseOption) == null || showOnlyResponse,
                    Verbose: context.ParseResult.FindResultFor(verboseOption) != null && verbose,
                    Debug: debug
                );
                
                await ExecuteRunCommand(options);
            });
            
        }

        private async Task ExecuteRunCommand(CallCommandOptions options)
        {
           // ConsoleHelper.DisplayTitle("Apify - API Request Runner");
           var configService = new ConfigService(options.Debug);;
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
                var requestSchema = JsonHelper.DeserializeFromFile<RequestDefinitionSchema>(path);
                

                if (requestSchema == null)
                {
                    ConsoleHelper.WriteError($"Failed to parse {path}");
                    return;
                }

                var variables = MiscHelper.ParseArgsVariables(options.Vars ?? "");
                var argVars = new Dictionary<string, Dictionary<string, string>>();
                argVars.Add("vars", variables);
                
                requestSchema = apiExecutor.ApplyEnvToApiDefinition(requestSchema, envName, argVars);



                var response = await apiExecutor.ExecuteRequestAsync(requestSchema);

                apiExecutor.DisplayApiResponse(response);
                apiExecutor.DisplayApiDefinition(requestSchema);


                var assertionExecutor = new AssertionExecutor(response, requestSchema);
                var testResults = await assertionExecutor.RunAsync(requestSchema.Tests ?? new List<AssertionEntity>());
                
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
        bool? Tests,
        bool? ShowRequest,
        bool? ShowResponse,
        bool? ShowOnlyResponse,
        bool? Verbose,
        bool Debug
    );
}
