using System.CommandLine;
using Apify.Models;
using Apify.Utils;

namespace Apify.Commands
{
    public class CreateRequestCommand
    {
        public Command Command { get; set; }
        private const string DefaultApiDirectory = ".apify";

        public CreateRequestCommand()
        {
            Command = new Command("create:request", "Create a new API request file or mock response");
            // Create a new API request command
            //var requestCommand = new Command("request", "Create a new API request file");

            var fileOption = new Option<string>(
                "--file",
                "The file path where the new request will be saved (e.g., users.all)"
            )
            { IsRequired = true };
            
            
            var nameOption = new Option<string>(
                "--name",
                () => "",
                "Name of the API request (optional, will be prompted if not provided)"
            );       
            
            var methodOption = new Option<string>(
                "--method",
                () => "GET",
                "HTTP method for the request (default: GET)"
            );
            
            var urlOption = new Option<string>(
                "--url",
                () => "",
                "URI for the request (e.g., {{baseUrl}}/users/{{userId}} or https://api.example.com/users)"
            );

            var forceOption = new Option<bool>(
                "--force",
                () => false,
                "Force overwrite if the file already exists"
            );
            
            var promptOption = new Option<bool>(
                "--prompt",
                () => false,
                "Prompt for required information interactively"
            );

            Command.AddOption(fileOption);
            Command.AddOption(forceOption);
            Command.AddOption(promptOption);
            Command.AddOption(nameOption);
            Command.AddOption(methodOption);
            Command.AddOption(urlOption);
            
            Command.SetHandler(
                (file, name, method, uri, force, debug, prompt) => ExecuteAsync(file, name, method, uri, force, debug, prompt),
                fileOption, nameOption, methodOption, urlOption, forceOption, RootCommand.DebugOption, promptOption
            );
        }

        private async Task ExecuteAsync(string filePath, string name, string method, string uri, bool force, bool debug, bool prompt)
        {
            ConsoleHelper.WriteHeader("Creating New API Request");

            if (debug)
            {
                ConsoleHelper.WriteDebug($"Creating API request in file: {filePath}");
            }

            // Process file path to convert dot notation if needed
            string processedPath = MiscHelper.HandlePath(filePath, DefaultApiDirectory);
            
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
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"Failed to create directory {directory}: {ex.Message}");
                    return;
                }
            }

            if (prompt)
            {
                // Prompt for required information
                name = ConsoleHelper.PromptInput<string>("API request name (e.g., Get User)");
                method = ConsoleHelper.PromptChoice("Choose HTTP Method?", new[] { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" });
                uri = ConsoleHelper.PromptInput<string>("URI (e.g., {{baseUrl}}/users/{{userId}} or https://api.example.com/users)", required: true);
            }


            bool addHeaders = false;
            // Optional inputs
            if (prompt)
            {
                addHeaders = ConsoleHelper.PromptYesNo("Add request headers?", false);
            }

            Dictionary<string, string> headers = new Dictionary<string, string>();
            
            if (addHeaders)
            {
                ConsoleHelper.WriteInfo("Enter headers (empty name to finish):");
                
                while (true)
                {
                    string headerName = ConsoleHelper.PromptInput<string>("Header name (e.g., Content-Type):", "");
                    if (string.IsNullOrWhiteSpace(headerName)) break;
                    
                    string headerValue = ConsoleHelper.PromptInput<string>($"Value for {headerName}:", required: true);
                    headers[headerName] = headerValue;
                }
            }

            // Determine if a payload is needed
            bool needsPayload = method == "POST" || method == "PUT" || method == "PATCH";
            PayloadType payloadType = PayloadType.None;
            object? payload = null;
            
            if (needsPayload && prompt && ConsoleHelper.PromptYesNo("Add request payload?", false))
            {
                string[] payloadOptions = { "JSON", "Text", "FormData" };
                int payloadOptionIndex = ConsoleHelper.PromptChoiceWithIndex("Payload type:", payloadOptions);
                
                switch (payloadOptionIndex)
                {
                    case 0: // JSON
                        payloadType = PayloadType.Json;
                        string jsonPayload = ConsoleHelper.PromptInput("Enter JSON payload:");
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
                        payload = ConsoleHelper.PromptInput("Enter text payload");
                        break;
                    
                    case 2: // FormData
                        payloadType = PayloadType.FormData;
                        var formData = new Dictionary<string, string>();
                        
                        ConsoleHelper.WriteInfo("Enter form fields (empty name to finish)");
                        while (true)
                        {
                            string fieldName = ConsoleHelper.PromptInput("Field name:", "");
                            if (string.IsNullOrWhiteSpace(fieldName)) break;
                            
                            string fieldValue = ConsoleHelper.PromptInput($"Value for {fieldName}");
                            formData[fieldName] = fieldValue;
                        }
                        
                        payload = formData;
                        break;
                }
            }

            // Ask if user wants to add file uploads
            List<FileUpload> files = new List<FileUpload>();
            if (needsPayload && prompt && ConsoleHelper.PromptYesNo("Add file uploads?", false))
            {
                ConsoleHelper.WriteInfo("Enter file uploads (empty field name to finish):");
                
                while (true)
                {
                    string fieldName = ConsoleHelper.PromptInput("Field name for the file", "");
                    if (string.IsNullOrWhiteSpace(fieldName)) break;
                    
                    string fileLocation = ConsoleHelper.PromptInput("Path to file");
                    string contentType = ConsoleHelper.PromptInput("Content type (e.g., image/jpeg)");
                    
                    files.Add(new FileUpload
                    {
                        Name = Path.GetFileName(fileLocation),
                        FieldName = fieldName,
                        FilePath = fileLocation,
                        ContentType = contentType
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
                Tests = new List<AssertionEntity>(), // Start with empty tests
            };

            try
            {
                // Serialize to JSON and save
                string jsonContent = JsonHelper.SerializeObject(apiDefinition);
                await File.WriteAllTextAsync(processedPath, jsonContent);
                
                ConsoleHelper.WriteSuccess($"API request is successfully created to: {processedPath}");
                ConsoleHelper.WriteInfo($"You can run it with: apify run {filePath.Replace(".json", "")}");
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Failed to save API request: {ex.Message}");
            }
        }
    }
}