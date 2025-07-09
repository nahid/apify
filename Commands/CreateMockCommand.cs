using System.CommandLine;
using Apify.Models;
using Apify.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.CommandLine.Invocation;

namespace Apify.Commands
{
    public class CreateMockCommand
    {
        public Command Command { get; set; }

        public CreateMockCommand()
        {
            Command = new Command("create:mock", "Create a new mock API response file");
            var fileOption = new Option<string>(
                "--file",
                "The file path where the new mock API will be saved (e.g., users.get)"
            ) { IsRequired = true };

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

            Command.AddOption(fileOption);
            Command.AddOption(forceOption);
            Command.AddOption(nameOption);
            Command.AddOption(methodOption);
            Command.AddOption(endpointOption);
            Command.AddOption(contentTypeOption);
            Command.AddOption(statusCodeOption);
            Command.AddOption(responseBodyOption);
            Command.AddOption(promptOption);
            
            
            Command.SetHandler(async (InvocationContext context) => {
                    var file = context.ParseResult.GetValueForOption(fileOption);
                var force = context.ParseResult.GetValueForOption(forceOption);
                    var name = context.ParseResult.GetValueForOption(nameOption);
                    var method = context.ParseResult.GetValueForOption(methodOption);
                    var endpoint = context.ParseResult.GetValueForOption(endpointOption);
                    var contentType = context.ParseResult.GetValueForOption(contentTypeOption);
                    var statusCode = context.ParseResult.GetValueForOption(statusCodeOption);
                    var responseBody = context.ParseResult.GetValueForOption(responseBodyOption);
                    var prompt = context.ParseResult.GetValueForOption(promptOption);
                    var debug = context.ParseResult.GetValueForOption(RootOption.DebugOption);

                    var options = new CommandOptions(
                        file,
                        name,
                        method,
                        endpoint,
                        statusCode,
                        contentType,
                        responseBody,
                        force,
                        prompt,
                        debug
                    );
                    
                    await ExecuteAsync(options);
                }
        
            );
        }

        private async Task ExecuteAsync(CommandOptions options)
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
            
            // Check if file already exists
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
            MockSchema mockApi = await GatherMockApiInformation(options);

            try
            {
                // Serialize to JSON and save
                string jsonContent = JsonHelper.SerializeObject(mockApi);
                await File.WriteAllTextAsync(processedPath, jsonContent);
                
                ConsoleHelper.WriteSuccess($"Mock API response saved to: {processedPath}");
                ConsoleHelper.WriteInfo($"You can test it with: apify mock-server");
                ConsoleHelper.WriteInfo($"Then access: http://localhost:8080{mockApi.Endpoint}");
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to save mock API response: {ex.Message}");
            }
        }

        private Task<MockSchema> GatherMockApiInformation(CommandOptions options)
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
                method = "GET"; // Default method if not provided
            }
            
            if (options.Prompt)
            {
                // Basic mock API information
                 name = PromptForInput("Mock API name (e.g., Get User):");
                 endpoint = PromptForInput("Endpoint path (e.g., /api/users/1 or /users):");
                 method = ConsoleHelper.PromptChoice<string>("HTTP method:", new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" });
            }

            
            // Ensure endpoint starts with /
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
                contentType = PromptForContentType();
            }
      
            
            // Response body
            object? responseBody = null;
            if (contentType.Contains("json") && options.Prompt)
            {
                responseBody = PromptForJsonResponse();
            }
            else if (options.Prompt)
            {
                string textResponse = PromptForInput("Response body (plain text):");
                responseBody = textResponse;
            }
            else
            {
                responseBody = options.ResponseBody;
            }
            
            // Headers
            Dictionary<string, string>? headers = null;
            if (options.Prompt && PromptYesNo("Add custom response headers?"))
            {
                headers = new Dictionary<string, string>();
                ConsoleHelper.WriteInfo("Enter headers (empty name to finish):");
                
                while (true)
                {
                    string headerName = PromptForInput("Header name (e.g., Cache-Control):", false);
                    if (string.IsNullOrWhiteSpace(headerName)) break;
                    
                    string headerValue = PromptForInput($"Value for {headerName}:");
                    headers[headerName] = headerValue;
                }
            }
            
            // Advanced options
            int delay = 0;
            if (options.Prompt && PromptYesNo("Add response delay (simulates latency)?"))
            {
                while (true)
                {
                    string delayStr = PromptForInput("Delay in milliseconds (e.g., 500):");
                    if (int.TryParse(delayStr, out delay) && delay >= 0)
                    {
                        break;
                    }
                    ConsoleHelper.WriteWarning("Please enter a valid non-negative number.");
                }
            }
            
            // Conditional responses
            List<MockCondition>? conditions = null;
            if (options.Prompt && PromptYesNo("Add conditional responses based on request parameters?"))
            {
                conditions = new List<MockCondition>();
                
                ConsoleHelper.WriteInfo("Enter conditions (empty name to finish):");
                while (true)
                {
                    string conditionName = PromptForInput("Condition name (e.g., 'When id=1'):", false);
                    if (string.IsNullOrWhiteSpace(conditionName)) break;
                    
                    var condition = new MockCondition { Name = conditionName };
                    
                    // Ask about query parameters
                    if (PromptYesNo("Match query parameters?"))
                    {
                        condition.QueryParams = new Dictionary<string, string>();
                        ConsoleHelper.WriteInfo("Enter query parameters (empty name to finish):");
                        
                        while (true)
                        {
                            string paramName = PromptForInput("Parameter name:", false);
                            if (string.IsNullOrWhiteSpace(paramName)) break;
                            
                            string paramValue = PromptForInput($"Value for {paramName}:");
                            condition.QueryParams[paramName] = paramValue;
                        }
                    }
                    
                    // Ask about headers to match
                    if (PromptYesNo("Match request headers?"))
                    {
                        condition.Headers = new Dictionary<string, string>();
                        ConsoleHelper.WriteInfo("Enter headers to match (empty name to finish):");
                        
                        while (true)
                        {
                            string headerName = PromptForInput("Header name:", false);
                            if (string.IsNullOrWhiteSpace(headerName)) break;
                            
                            string headerValue = PromptForInput($"Value for {headerName}:");
                            condition.Headers[headerName] = headerValue;
                        }
                    }
                    
                    // Response for this condition
                    if (contentType.Contains("json"))
                    {
                        ConsoleHelper.WriteInfo("Enter JSON response for this condition:");
                        condition.Response = PromptForJsonResponse();
                    }
                    else
                    {
                        string textResponse = PromptForInput("Response body for this condition (plain text):");
                        condition.Response = textResponse;
                    }
                    
                    // Status code for this condition
                    if (PromptYesNo("Set a specific status code for this condition?"))
                    {
                        condition.StatusCode = PromptForStatusCode();
                    }
                    
                    conditions.Add(condition);
                }
            }

            return Task.FromResult(new MockSchema {
                Name = name,
                Endpoint = endpoint,
                Method = method,
                Responses = new List<ConditionalResponse>
                {
                    new ConditionalResponse
                    {
                        Condition = "default", // Default condition for the main response
                        StatusCode = statusCode,
                        Headers = headers ?? new Dictionary<string, string>(),
                        ResponseTemplate = responseBody,
                    }
                }
            });
        }

        private string ProcessFilePath(string filePath)
        {
            // Apply the same logic as in CreateRequestCommand to handle dot notation
            // but ensure .mock.json extension
            
            // Start with original path
            string processedPath = filePath;
            
            // Convert dot notation to directory separators if present
            string filenameWithoutExt = Path.GetFileNameWithoutExtension(processedPath);
            
            // If there are dots in the filename part
            if (filenameWithoutExt.Contains('.'))
            {
                // Only handle as dot notation if no directory separators already exist
                bool hasDirectorySeparator = processedPath.Contains(Path.DirectorySeparatorChar) || 
                                            processedPath.Contains(Path.AltDirectorySeparatorChar);
                
                if (!hasDirectorySeparator && !processedPath.StartsWith("."))
                {
                    string[] parts = filenameWithoutExt.Split('.');
                    string filename = parts[parts.Length - 1]; // Last part becomes the filename
                    string[] folderParts = parts.Take(parts.Length - 1).ToArray(); // Earlier parts become folders
                    
                    // Join with directory separators and add .mock.json extension
                    processedPath = string.Join(Path.DirectorySeparatorChar.ToString(), folderParts) + 
                                   Path.DirectorySeparatorChar + filename + ".mock.json";
                    
                    ConsoleHelper.WriteInfo($"Converted dot notation: {filePath} → {processedPath}");
                }
                else
                {
                    // Ensure .mock.json extension
                    if (!processedPath.EndsWith(".mock.json", StringComparison.OrdinalIgnoreCase))
                    {
                        // Remove .json extension if present
                        if (processedPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        {
                            processedPath = processedPath.Substring(0, processedPath.Length - 5);
                        }
                        processedPath += ".mock.json";
                    }
                }
            }
            else
            {
                // Simple filename without dots, ensure .mock.json extension
                if (!processedPath.EndsWith(".mock.json", StringComparison.OrdinalIgnoreCase))
                {
                    // Remove .json extension if present
                    if (processedPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        processedPath = processedPath.Substring(0, processedPath.Length - 5);
                    }
                    processedPath += ".mock.json";
                }
            }
            
            // Add .apify prefix if not already present
            bool alreadyHasApiDirectory = processedPath.StartsWith(RootOption.DefaultApiDirectory + Path.DirectorySeparatorChar) || 
                                         processedPath.StartsWith(RootOption.DefaultApiDirectory + Path.AltDirectorySeparatorChar);
            
            if (!alreadyHasApiDirectory)
            {
                // Ensure the .apify directory exists
                EnsureApiDirectoryExists();
                
                processedPath = Path.Combine(RootOption.DefaultApiDirectory, processedPath);
            }
            
            return processedPath;
        }
        
        private void EnsureApiDirectoryExists()
        {
            if (!Directory.Exists(RootOption.DefaultApiDirectory))
            {
                try
                {
                    Directory.CreateDirectory(RootOption.DefaultApiDirectory);
                    ConsoleHelper.WriteInfo($"Created '{RootOption.DefaultApiDirectory}' directory as it didn't exist.");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"Failed to create '{RootOption.DefaultApiDirectory}' directory: {ex.Message}");
                }
            }
        }

        private string PromptForInput(string prompt, bool required = true)
        {
            while (true)
            {
                Console.Write($"{prompt} ");
                string? input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input))
                {
                    if (!required) return string.Empty;
                    ConsoleHelper.WriteWarning("This field is required. Please try again.");
                }
                else
                {
                    return input;
                }
            }
        }

        private string PromptForHttpMethod()
        {
            string[] methods = { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };
            int selectedIndex = PromptChoice("HTTP method:", methods);
            return methods[selectedIndex];
        }

        private string PromptForContentType()
        {
            string[] contentTypes = { 
                "application/json", 
                "text/plain", 
                "text/html", 
                "application/xml",
                "text/csv", 
                "application/octet-stream",
                "application/x-www-form-urlencoded"
            };
            
            int selectedIndex = PromptChoice("Content Type:", contentTypes);
            return contentTypes[selectedIndex];
        }

        private int PromptForStatusCode()
        {
            string[] statusOptions = {
                "200 - OK",
                "201 - Created",
                "204 - No Content",
                "400 - Bad Request",
                "401 - Unauthorized",
                "403 - Forbidden",
                "404 - Not Found",
                "500 - Server Error",
                "Custom Status Code"
            };
            
            int[] statusCodes = { 200, 201, 204, 400, 401, 403, 404, 500, 0 };
            
            int selectedIndex = PromptChoice("Status Code:", statusOptions);
            
            // If custom status code selected
            if (selectedIndex == statusOptions.Length - 1)
            {
                while (true)
                {
                    string statusCodeStr = PromptForInput("Enter custom status code (100-599):");
                    if (int.TryParse(statusCodeStr, out int customCode) && customCode >= 100 && customCode <= 599)
                    {
                        return customCode;
                    }
                    ConsoleHelper.WriteWarning("Please enter a valid HTTP status code between 100 and 599.");
                }
            }
            
            return statusCodes[selectedIndex];
        }

        private object? PromptForJsonResponse()
        {
            ConsoleHelper.WriteInfo("Enter JSON response (enter on a blank line to finish):");
            
            // Collect multi-line input
            var lines = new List<string>();
            string? line;
            Console.WriteLine("Enter JSON (end with blank line):");
            while (!string.IsNullOrWhiteSpace(line = Console.ReadLine()))
            {
                lines.Add(line);
            }
            
            string jsonInput = string.Join("\n", lines);
            
            // If empty, return null
            if (string.IsNullOrWhiteSpace(jsonInput))
            {
                return null;
            }
            
            try
            {
                // Try to parse as JObject or JArray
                if (jsonInput.TrimStart().StartsWith("{"))
                {
                    return JsonConvert.DeserializeObject<JObject>(jsonInput);
                }
                else if (jsonInput.TrimStart().StartsWith("["))
                {
                    return JsonConvert.DeserializeObject<JArray>(jsonInput);
                }
                else
                {
                    // Parse as generic object
                    return JsonConvert.DeserializeObject(jsonInput);
                }
            }
            catch (JsonException ex)
            {
                ConsoleHelper.WriteWarning($"Invalid JSON format: {ex.Message}");
                ConsoleHelper.WriteInfo("Storing as raw text instead.");
                return jsonInput;
            }
        }

        private bool PromptYesNo(string prompt)
        {
            while (true)
            {
                Console.Write($"{prompt} (y/n): ");
                string? input = Console.ReadLine()?.Trim().ToLower();
                
                if (input == "y" || input == "yes") return true;
                if (input == "n" || input == "no") return false;
                
                ConsoleHelper.WriteWarning("Please enter 'y' or 'n'.");
            }
        }

        private int PromptChoice(string prompt, string[] options)
        {
            Console.WriteLine(prompt);
            
            for (int i = 0; i < options.Length; i++)
            {
                Console.WriteLine($"{i+1}. {options[i]}");
            }
            
            while (true)
            {
                Console.Write($"Enter selection (1-{options.Length}): ");
                string? input = Console.ReadLine();
                
                if (int.TryParse(input, out int selection) && selection >= 1 && selection <= options.Length)
                {
                    return selection - 1; // Return zero-based index
                }
                
                ConsoleHelper.WriteWarning($"Please enter a number between 1 and {options.Length}.");
            }
        }
    }
}

public record CommandOptions(
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