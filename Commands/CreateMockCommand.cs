using System.CommandLine;
using Apify.Models;
using Apify.Utils;

namespace Apify.Commands
{
    public class CreateMockCommand: Command
    {

        public CreateMockCommand(): base("create:mock", "Create a new mock API response file")
        {
            
            var fileArgument = new Argument<string>(
                name: "file",
                description: "The file path where the new mock API will be saved (e.g., users.get)")
            {
                Arity = ArgumentArity.ExactlyOne
            };
            AddArgument(fileArgument);

            var forceOption = new Option<bool>(
                "--force",
                () => false,
                "Force overwrite if the file already exists"
            );
            
            var nameOption = new Option<string>(
                "--name",
                () => "",
                "Name of the mock API (optional, will be prompted if not provided)"
            );
            
            var methodOption = new Option<string>(
                "--method",
                () => "GET",
                "HTTP method for the mock API (default: GET)"
            );
            
            var endpointOption = new Option<string>(
                "--endpoint",
                () => "",
                "Endpoint path for the mock API (e.g., /api/users/1 or /users)"
            );
            
            var contentTypeOption = new Option<string>(
                "--content-type",
                () => "application/json",
                "Content type for the mock API response (default: application/json)"
            );
            
            var statusCodeOption = new Option<int>(
                "--status-code",
                () => 200,
                "HTTP status code for the mock API response (default: 200)"
            );
            
            var responseBodyOption = new Option<string>(
                "--response-body",
                () => "",
                "Response body for the mock API (if not provided, will prompt for JSON input)"
            );
            
                
            var promptOption = new Option<bool>(
                "--prompt",
                () => false,
                "Prompt for required information interactively"
            );
            
            AddOption(forceOption);
            AddOption(nameOption);
            AddOption(methodOption);
            AddOption(endpointOption);
            AddOption(contentTypeOption);
            AddOption(statusCodeOption);
            AddOption(responseBodyOption);
            AddOption(promptOption);
            
            
            this.SetHandler(async (context) => {
                    var file = context.ParseResult.GetValueForArgument(fileArgument);
                var force = context.ParseResult.GetValueForOption(forceOption);
                    var name = context.ParseResult.GetValueForOption(nameOption);
                    var method = context.ParseResult.GetValueForOption(methodOption);
                    var endpoint = context.ParseResult.GetValueForOption(endpointOption);
                    var contentType = context.ParseResult.GetValueForOption(contentTypeOption);
                    var statusCode = context.ParseResult.GetValueForOption(statusCodeOption);
                    var responseBody = context.ParseResult.GetValueForOption(responseBodyOption);
                    var prompt = context.ParseResult.GetValueForOption(promptOption);
                    var debug = context.ParseResult.GetValueForOption(RootOption.DebugOption);

                    var options = new CreateMockCommandOptions(
                        file,
                        name ?? "",
                        method ?? "GET",
                        endpoint ?? "/new-endpoint",
                        statusCode,
                        contentType ?? "application/json",
                        responseBody ?? "",
                        force,
                        prompt,
                        debug
                    );
                    
                    await ExecuteAsync(options);
                });
        }

        private async Task ExecuteAsync(CreateMockCommandOptions options)
        {
            ConsoleHelper.WriteHeader("Creating New Mock API Response");

            if (options.Debug)
            {
                ConsoleHelper.WriteDebug($"Creating mock API response in file: {options.FilePath}");
            }

            // Process file path to add .mock.json extension and handle dot notation
            string processedPath = MiscHelper.HandlePath(options.FilePath, ".mock.json");
            
            if (options.Debug)
            {
                ConsoleHelper.WriteDebug($"Processed file path: {processedPath}");
            }
            
            // Check if a file already exists
            if (File.Exists(processedPath) && !options.Force)
            {
                ConsoleHelper.WriteError($"File already exists: {processedPath}");
                ConsoleHelper.WriteInfo("Use --force to overwrite the existing file.");
                return;
            }

            // Create directories if they don't exist
            string? directory = Path.GetDirectoryName(processedPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                    ConsoleHelper.WriteInfo($"Created directory: {directory}");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"Failed to create directory {directory}: {ex.Message}");
                    return;
                }
            }

            // Gather mock API information through interactive prompts
            MockDefinitionSchema mockDefinitionApi = await GatherMockApiInformation(options);

            try
            {
                // Serialize to JSON and save
                string jsonContent = JsonHelper.SerializeObject(mockDefinitionApi);
                await File.WriteAllTextAsync(processedPath, jsonContent);
                
                ConsoleHelper.WriteSuccess($"Mock API response saved to: {processedPath}");
                ConsoleHelper.WriteInfo($"You can test it with: apify server:mock --port=1988");
                ConsoleHelper.WriteInfo($"Then access: http://localhost:1988{mockDefinitionApi.Endpoint}");
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to save mock API response: {ex.Message}");
            }
        }

        private Task<MockDefinitionSchema> GatherMockApiInformation(CreateMockCommandOptions options)
        {
            string name = options.Name;
            string endpoint = options.Endpoint;
            string method = options.Method;
            
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "New Mock API";
            }
            
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                endpoint = "/api/new-endpoint"; // Default endpoint if not provided
            }
            
            if (string.IsNullOrWhiteSpace(method))
            {
                method = "GET"; // Default method if isn't provided
            }
            
            if (options.Prompt)
            {
                // Basic mock API information
                name = ConsoleHelper.PromptInput("Mock API name (e.g., Get User)");
                endpoint = ConsoleHelper.PromptInput("Endpoint path (e.g., /api/users/1 or /users):");
                method = ConsoleHelper.PromptChoice<string>("HTTP method:", ["GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS"]);
            }

            
            // Ensure the endpoint starts with /
            if (!endpoint.StartsWith("/"))
            {
                endpoint = "/" + endpoint;
            }
            
            // Response 
            
            int statusCode = options.StatusCode;
            string contentType = options.ContentType;
            
            if (string.IsNullOrWhiteSpace(contentType))
            {
                contentType = "application/json"; // Default content type
            }
            
            if (string.IsNullOrWhiteSpace(statusCode.ToString()))
            {
                statusCode = 200; // Default status code
            }

            if (options.Prompt)
            {
                statusCode = PromptForStatusCode();

                contentType = ConsoleHelper.PromptChoice("Content Type:", [
                    "application/json",
                    "text/plain",
                    "text/html",
                    "application/xml",
                    "text/csv",
                    "application/octet-stream",
                    "application/x-www-form-urlencoded"
                ]);
            }
      
            
            // Response body
            object? responseBody;
            if (contentType.Contains("json") && options.Prompt)
            {
                responseBody = ConsoleHelper.PromptMultiLineInput("Enter JSON Body(Plain Text):");
            }
            else if (options.Prompt)
            {
                string textResponse = ConsoleHelper.PromptInput("Response body (plain text):");
                responseBody = textResponse;
            }
            else
            {
                responseBody = options.ResponseBody;
            }
            
            // Headers
            Dictionary<string, string>? headers = null;
            if (options.Prompt && ConsoleHelper.PromptYesNo("Add custom response headers?"))
            {
                headers = new Dictionary<string, string>();
                ConsoleHelper.WriteInfo("Enter headers (empty name to finish):");
                
                while (true)
                {
                    string headerName = ConsoleHelper.PromptInput<string>("Header name (e.g., Cache-Control)", required:false);
                    if (string.IsNullOrWhiteSpace(headerName)) break;
                    
                    string headerValue = ConsoleHelper.PromptInput($"Value for {headerName}");
                    headers[headerName] = headerValue;
                }
            }
            
            // Advanced options
           

            if (options.Prompt && ConsoleHelper.PromptYesNo("Add response delay (simulates latency)?"))
            {
                while (true)
                {
                    string delayStr = ConsoleHelper.PromptInput("Delay in milliseconds (e.g., 500)");

                    if (int.TryParse(delayStr, out var delay) && delay >= 0)
                    {
                        break;
                    }
                    ConsoleHelper.WriteWarning("Please enter a valid non-negative number.");
                }
            }
            return Task.FromResult(new MockDefinitionSchema {
                Name = name,
                Endpoint = endpoint,
                Method = method,
                Responses = [
                    new ConditionalResponse {
                        Condition = "default", // Default condition for the main response
                        StatusCode = statusCode,
                        Headers = headers ?? new Dictionary<string, string>(),
                        ResponseTemplate = responseBody,
                    }
                ]
            });
        }

        private int PromptForStatusCode()
        {
            string[] statusOptions = [
                "200 - OK",
                "201 - Created",
                "204 - No Content",
                "400 - Bad Request",
                "401 - Unauthorized",
                "403 - Forbidden",
                "404 - Not Found",
                "500 - Server Error",
                "Custom Status Code"
            ];
            
            int[] statusCodes = [200, 201, 204, 400, 401, 403, 404, 500, 0];
            
            int selectedIndex = ConsoleHelper.PromptChoiceWithIndex("Status Code:", statusOptions);
            
            // If custom status code selected
            if (selectedIndex == statusOptions.Length - 1)
            {
                while (true)
                {
                    int customCode = ConsoleHelper.PromptInput<int>("Enter custom status code (100-599)");
                    if (customCode is >= 100 and <= 599)
                    {
                        return customCode;
                    }
                    ConsoleHelper.WriteWarning("Please enter a valid HTTP status code between 100 and 599.");
                }
            }
            
            return statusCodes[selectedIndex];
        }
    }
}

public record CreateMockCommandOptions(
    string FilePath,
    string Name,
    string Method,
    string Endpoint,
    int StatusCode,
    string ContentType,
    string ResponseBody,
    bool Force,
    bool Prompt,
    bool Debug
);