using System.CommandLine;
using Apify.Models;
using Apify.Utils;

namespace Apify.Commands
{
    public class CreateRequestCommand : Command
    {
        private const string DefaultApiDirectory = ".apify";

        public CreateRequestCommand() : base("create", "Create a new API request file or mock response")
        {
            // Create a new API request command
            var requestCommand = new Command("request", "Create a new API request file");

            var fileOption = new Option<string>(
                "--file",
                "The file path where the new request will be saved (e.g., users.all)"
            )
            { IsRequired = true };

            var forceOption = new Option<bool>(
                "--force",
                () => false,
                "Force overwrite if the file already exists"
            );

            requestCommand.AddOption(fileOption);
            requestCommand.AddOption(forceOption);
            
            requestCommand.SetHandler(
                (file, force, debug) => ExecuteAsync(file, force, debug),
                fileOption, forceOption, RootCommand.DebugOption
            );

            // Add mock command
            var mockCommand = new CreateMockCommand();

            // Add subcommands
            AddCommand(requestCommand);
            AddCommand(mockCommand);
        }

        private async Task ExecuteAsync(string filePath, bool force, bool debug)
        {
            ConsoleHelper.WriteHeader("Creating New API Request");

            if (debug)
            {
                ConsoleHelper.WriteDebug($"Creating API request in file: {filePath}");
            }

            // Process file path to convert dot notation if needed
            string processedPath = ProcessFilePath(filePath);
            
            // Check if file already exists
            if (File.Exists(processedPath) && !force)
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

            // Prompt for required information
            string name = PromptForInput("API request name (e.g., Get User):");
            string method = PromptForHttpMethod();
            string uri = PromptForInput("URI (e.g., {{baseUrl}}/users/{{userId}} or https://api.example.com/users):");

            // Optional inputs
            bool addHeaders = PromptYesNo("Add request headers?");
            Dictionary<string, string> headers = new Dictionary<string, string>();
            
            if (addHeaders)
            {
                ConsoleHelper.WriteInfo("Enter headers (empty name to finish):");
                
                while (true)
                {
                    string headerName = PromptForInput("Header name (e.g., Content-Type):", false);
                    if (string.IsNullOrWhiteSpace(headerName)) break;
                    
                    string headerValue = PromptForInput($"Value for {headerName}:");
                    headers[headerName] = headerValue;
                }
            }

            // Determine if a payload is needed
            bool needsPayload = method == "POST" || method == "PUT" || method == "PATCH";
            PayloadType payloadType = PayloadType.None;
            object? payload = null;
            
            if (needsPayload && PromptYesNo("Add request payload?"))
            {
                string[] payloadOptions = { "JSON", "Text", "FormData" };
                int payloadOptionIndex = PromptChoice("Payload type:", payloadOptions);
                
                switch (payloadOptionIndex)
                {
                    case 0: // JSON
                        payloadType = PayloadType.Json;
                        string jsonPayload = PromptForInput("Enter JSON payload:");
                        try
                        {
                            // Attempt to parse as JSON using Newtonsoft
                            payload = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonPayload);
                        }
                        catch
                        {
                            // If parsing fails, store as string
                            ConsoleHelper.WriteWarning("JSON parsing failed, storing as raw string.");
                            payload = jsonPayload;
                        }
                        break;
                    
                    case 1: // Text
                        payloadType = PayloadType.Text;
                        payload = PromptForInput("Enter text payload:");
                        break;
                    
                    case 2: // FormData
                        payloadType = PayloadType.FormData;
                        var formData = new Dictionary<string, string>();
                        
                        ConsoleHelper.WriteInfo("Enter form fields (empty name to finish):");
                        while (true)
                        {
                            string fieldName = PromptForInput("Field name:", false);
                            if (string.IsNullOrWhiteSpace(fieldName)) break;
                            
                            string fieldValue = PromptForInput($"Value for {fieldName}:");
                            formData[fieldName] = fieldValue;
                        }
                        
                        payload = formData;
                        break;
                }
            }

            // Ask if user wants to add file uploads
            List<FileUpload> files = new List<FileUpload>();
            if (needsPayload && PromptYesNo("Add file uploads?"))
            {
                ConsoleHelper.WriteInfo("Enter file uploads (empty field name to finish):");
                
                while (true)
                {
                    string fieldName = PromptForInput("Field name for the file:", false);
                    if (string.IsNullOrWhiteSpace(fieldName)) break;
                    
                    string fileLocation = PromptForInput("Path to file:");
                    string contentType = PromptForInput("Content type (e.g., image/jpeg):");
                    
                    files.Add(new FileUpload
                    {
                        Name = Path.GetFileName(fileLocation),
                        FieldName = fieldName,
                        FilePath = fileLocation,
                        ContentType = contentType
                    });
                }
            }

            // Ask if user wants to add tests
            List<AssertionEntity> tests = new List<AssertionEntity>();
            if (PromptYesNo("Add tests?"))
            {
                ConsoleHelper.WriteInfo("Enter tests (empty name to finish):");
                
                while (true)
                {
                    string testName = PromptForInput("Test name:", false);
                    if (string.IsNullOrWhiteSpace(testName)) break;
                    
                    string[] assertionTypes = { 
                        "StatusCode", "ContainsText", "ContainsProperty", 
                        "Equals", "HeaderContains", "ResponseTime" 
                    };
                    
                    int assertionTypeIndex = PromptChoice("Assertion type:", assertionTypes);
                    string assertionType = assertionTypes[assertionTypeIndex];
                    
                    string property = string.Empty;
                    if (assertionType == "ContainsProperty" || assertionType == "Equals" || assertionType == "HeaderContains")
                    {
                        property = PromptForInput("Property name:");
                    }
                    
                    string expectedValue = PromptForInput("Expected value:");
                    
                    tests.Add(new AssertionEntity()
                    {
                        Title = testName,
                        Case = assertionType
                    });
                }
            }

            // Create the API definition
            var apiDefinition = new ApiDefinition
            {
                Name = name,
                Uri = uri,
                Method = method,
                Headers = headers.Count > 0 ? headers : null,
                PayloadType = payloadType,
                Payload = payload,
                Files = files.Count > 0 ? files : null,
                Tests = tests.Count > 0 ? tests : null
            };

            try
            {
                // Serialize to JSON and save
                string jsonContent = JsonHelper.SerializeObject(apiDefinition);
                await File.WriteAllTextAsync(processedPath, jsonContent);
                
                ConsoleHelper.WriteSuccess($"API request saved to: {processedPath}");
                ConsoleHelper.WriteInfo($"You can run it with: apify run {filePath.Replace(".json", "")}");
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to save API request: {ex.Message}");
            }
        }

        private string ProcessFilePath(string filePath)
        {
            // Apply the same logic as in RunCommand to handle dot notation
            // and ensure .json extension 
            
            // Start with original path
            string processedPath = filePath;
            
            // Add .json extension if missing
            if (!processedPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                processedPath += ".json";
            }
            
            // Convert dot notation to directory separators if present
            string filenameWithoutExt = Path.GetFileNameWithoutExtension(processedPath);
            
            // If there are dots in the filename part (not in the extension)
            if (filenameWithoutExt.Contains('.'))
            {
                string extension = Path.GetExtension(processedPath);
                
                // Only handle as dot notation if no directory separators already exist
                bool hasDirectorySeparator = processedPath.Contains(Path.DirectorySeparatorChar) || 
                                            processedPath.Contains(Path.AltDirectorySeparatorChar);
                
                if (!hasDirectorySeparator && !processedPath.StartsWith("."))
                {
                    string[] parts = filenameWithoutExt.Split('.');
                    string filename = parts[parts.Length - 1]; // Last part becomes the filename
                    string[] folderParts = parts.Take(parts.Length - 1).ToArray(); // Earlier parts become folders
                    
                    // Join with directory separators
                    processedPath = string.Join(Path.DirectorySeparatorChar.ToString(), folderParts) + 
                                   Path.DirectorySeparatorChar + filename + extension;
                    
                    ConsoleHelper.WriteInfo($"Converted dot notation: {filePath} → {processedPath}");
                }
            }
            
            // Add .apify prefix if not already present
            bool alreadyHasApiDirectory = processedPath.StartsWith(DefaultApiDirectory + Path.DirectorySeparatorChar) || 
                                         processedPath.StartsWith(DefaultApiDirectory + Path.AltDirectorySeparatorChar);
            
            if (!alreadyHasApiDirectory)
            {
                // Ensure the .apify directory exists
                EnsureApiDirectoryExists();
                
                processedPath = Path.Combine(DefaultApiDirectory, processedPath);
                ConsoleHelper.WriteInfo($"Added default directory: {processedPath}");
            }
            
            return processedPath;
        }
        
        private void EnsureApiDirectoryExists()
        {
            if (!Directory.Exists(DefaultApiDirectory))
            {
                try
                {
                    Directory.CreateDirectory(DefaultApiDirectory);
                    ConsoleHelper.WriteInfo($"Created '{DefaultApiDirectory}' directory as it didn't exist.");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"Failed to create '{DefaultApiDirectory}' directory: {ex.Message}");
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