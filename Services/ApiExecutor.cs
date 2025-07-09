using Apify.Models;
using Apify.Utils;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Apify.Services
{
    public class ApiExecutor
    {
        private readonly HttpClient _httpClient;
      
        private ConfigService _configService;

        public ApiExecutor()
        {
            _httpClient = new HttpClient();
            _configService = new ConfigService();
        }


        public async Task<ApiResponse> ExecuteRequestAsync(ApiDefinition apiDefinition)
        {
            var stopwatch = new Stopwatch();
            var response = new ApiResponse();
            
            try
            {
                // Create request message
                // Check if URI has a valid scheme (http:// or https://)
                var uri = apiDefinition.Uri;
                
                // If URI doesn't start with http:// or https://, add https:// prefix
                if (!uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                    !uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    uri = "https://" + uri;
                }
                
                var request = new HttpRequestMessage(new HttpMethod(apiDefinition.Method), uri);

                // Add headers
                if (apiDefinition.Headers != null)
                {
                    foreach (var header in apiDefinition.Headers)
                    {
                        if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) && 
                            (apiDefinition.PayloadType != PayloadType.None || 
                             apiDefinition.Files?.Count > 0))
                        {
                            // Content-Type will be set with the content
                            continue;
                        }
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                // Add content for appropriate methods
                bool isMethodWithBody = apiDefinition.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) || 
                                       apiDefinition.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) || 
                                       apiDefinition.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase);
                
                if (isMethodWithBody)
                {
                    // Check for file uploads first (takes precedence if files are present)
                    if (apiDefinition.Files?.Count > 0)
                    {
                        await AddMultipartFormDataContentAsync(request, apiDefinition);
                    }
                    // Then check for payload
                    else if (apiDefinition.Payload != null)
                    {
                        AddPayloadContent(request, apiDefinition);
                    }
                }

                // Set timeout
                _httpClient.Timeout = TimeSpan.FromMilliseconds(apiDefinition.Timeout);

                // Send request and measure time
                stopwatch.Start();
                var httpResponse = await _httpClient.SendAsync(request);
                stopwatch.Stop();

                // Process response
                response.StatusCode = (int)httpResponse.StatusCode;
                response.Headers = httpResponse.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
                response.ContentHeaders = httpResponse.Content.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));
                response.Body = await httpResponse.Content.ReadAsStringAsync();
                response.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                response.IsSuccessful = true;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                response.IsSuccessful = false;
                response.ErrorMessage = ex.Message;
                response.ResponseTimeMs = stopwatch.ElapsedMilliseconds;
                
                // The detailed error info is already included in the response object
                // so we don't need to log anything here
                
                // Include the error message in the response body for test assertion context
                response.Body = $"{{\"error\": \"{ex.Message.Replace("\"", "\\\"")}\", \"exception_type\": \"{ex.GetType().Name}\"}}";
            }

            return response;
        }

        private void AddPayloadContent(HttpRequestMessage request, ApiDefinition apiDefinition)
        {
            string contentType = "application/json";
            if (apiDefinition.Headers != null && 
                apiDefinition.Headers.TryGetValue("Content-Type", out string? headerContentType))
            {
                contentType = headerContentType;
            }
            else
            {
                // Set appropriate content type based on payload type
                contentType = apiDefinition.PayloadType switch
                {
                    PayloadType.Json => "application/json",
                    PayloadType.Text => "text/plain",
                    PayloadType.FormData => "application/x-www-form-urlencoded",
                    _ => "application/json" // Default to JSON
                };
            }

            // Get the payload as a string or object depending on the type
            string? payloadString = apiDefinition.GetPayloadAsString();
            
            // Process payload based on type
            StringContent content;
            switch (apiDefinition.PayloadType)
            {
                case PayloadType.Json:
                    // Payload is already an object, just serialize it with indentation
                    string formattedJson = JsonConvert.SerializeObject(apiDefinition.Payload, Formatting.Indented);
                    content = new StringContent(formattedJson, Encoding.UTF8);
                    break;

                case PayloadType.FormData:
                    // For form data, we need to format as key=value&key2=value2...
                    try
                    {
                        // Use the GetPayloadAsObject method to get the form fields
                        var formFields = apiDefinition.GetPayloadAsObject<Dictionary<string, string>>();
                        if (formFields != null)
                        {
                            var formData = new FormUrlEncodedContent(formFields);
                            request.Content = formData;
                            return; // Skip the content-type setting below as FormUrlEncodedContent sets it
                        }
                        else
                        {
                            // Fall back to string if conversion fails
                            content = new StringContent(payloadString ?? string.Empty, Encoding.UTF8);
                        }
                    }
                    catch
                    {
                        // Error is already handled by falling back to string content
                        content = new StringContent(payloadString ?? string.Empty, Encoding.UTF8);
                    }
                    break;

                case PayloadType.Text:
                    content = new StringContent(payloadString ?? string.Empty, Encoding.UTF8);
                    break;
                    
                case PayloadType.None:
                default:
                    // For "none" payload type, create an empty content
                    content = new StringContent(string.Empty, Encoding.UTF8);
                    break;
            }

            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            request.Content = content;
        }

        private async Task AddMultipartFormDataContentAsync(HttpRequestMessage request, ApiDefinition apiDefinition)
        {
            var multipartContent = new MultipartFormDataContent();

            // Add text fields from payload if specified and if it's a form data payload
            if (apiDefinition.Payload != null && apiDefinition.PayloadType == PayloadType.FormData)
            {
                try
                {
                    var formFields = apiDefinition.GetPayloadAsObject<Dictionary<string, string>>();
                    if (formFields != null)
                    {
                        foreach (var field in formFields)
                        {
                            multipartContent.Add(new StringContent(field.Value), field.Key);
                        }
                    }
                }
                catch
                {
                    // Silently ignore parsing errors for form data
                }
            }

            // Add files
            foreach (var file in apiDefinition.Files!)
            {
                try
                {
                    if (!File.Exists(file.FilePath))
                    {
                        ConsoleHelper.WriteWarning($"File not found: {file.FilePath}");
                        continue;
                    }

                    // Read file as byte array
                    var fileBytes = await File.ReadAllBytesAsync(file.FilePath);
                    var fileContent = new ByteArrayContent(fileBytes);
                    
                    // Set the content type for the file
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                    
                    // Set the filename in the Content-Disposition header
                    string fileName = Path.GetFileName(file.FilePath);
                    multipartContent.Add(fileContent, file.FieldName, fileName);
                }
                catch
                {
                    // Only show file errors when they're critical to the request
                    if (!File.Exists(file.FilePath))
                    {
                        ConsoleHelper.WriteWarning($"File not found: {file.FilePath}");
                    }
                }
            }

            request.Content = multipartContent;
        }
        
        public ApiDefinition ApplyEnvToApiDefinition(ApiDefinition apiDefinition, string environment, params Dictionary<string, Dictionary<string, string>>[]? variables)
        {
            var apiDefContent = JsonHelper.SerializeToJson(apiDefinition);
            
            if (string.IsNullOrEmpty(apiDefContent))
            {
                return apiDefinition;
            }

            var conf = _configService.LoadConfiguration();
            var vars = MiscHelper.MergeDictionaries(conf.Variables, _configService.LoadEnvironment(environment)?.Variables ?? new Dictionary<string, string>());
            vars = MiscHelper.MergeDictionaries(vars, apiDefinition.Variables ?? new Dictionary<string, string>());
            

            var stubReplacor = new Dictionary<string, object>();
            
            stubReplacor.Add("env", vars);
            
            if (variables != null)
            {
                foreach (var v in variables)
                {
                    foreach (var kvp in v)
                    {
                        stubReplacor.Add(kvp.Key, kvp.Value);
                    }
                }
            }
            
            apiDefContent = StubManager.Replace(apiDefContent, stubReplacor);
            
            if (string.IsNullOrEmpty(apiDefContent))
            {
                return apiDefinition;
            }

            apiDefinition = JsonHelper.DeserializeString<ApiDefinition>(apiDefContent) ?? new ApiDefinition();

            return apiDefinition;
        }
        
        public void DisplayTestStats(TestResults testResults, bool verbose = false)
        {
            if (!verbose) return;
            
            ConsoleHelper.WriteSection("===============================");
            ConsoleHelper.WriteKeyValue("Test Summary", $"{testResults.Results.Count}/{testResults.PassedCount} tests passed");
            ConsoleHelper.WriteLineColored("===============================\n", ConsoleColor.Cyan);
        }
        public void DisplayTestResults(TestResults testResults, bool verbose = false)
        {
            if (!verbose) return;
            
            
            foreach (var testResult in testResults.Results)
            {
                if (testResult.Status)
                {
                    ConsoleHelper.WriteSuccess($"✓ {testResult.Name} >>", true);
                }
                else
                {
                    ConsoleHelper.WriteError($"✗ {testResult.Name} >>", true);
                }

                foreach (var assertResult in testResult.Result)
                {
                    if (assertResult.IsPassed())
                    {
                        ConsoleHelper.WriteSuccess($"{" ",-2}✓ - {assertResult.GetMessage()}");
                    }
                    else
                    {
                        ConsoleHelper.WriteError($"{" ",-2}✗ - {assertResult.GetMessage()}");
                    }
                }
            }
        }
        
        public void DisplayApiResponse(ApiResponse response)
        {
            ConsoleHelper.WriteSection("API Response:");
            ConsoleHelper.WriteStatusCode(response.StatusCode);
            ConsoleHelper.WriteTiming(response.ResponseTimeMs);
            
            if (response.Headers.Count > 0)
            {
                ConsoleHelper.WriteSection("Response Headers:");
                foreach (var header in response.Headers)
                {
                    Console.Write("  ");
                    ConsoleHelper.WriteKeyValue(header.Key, header.Value);
                }
            }
            
            if (response.ContentHeaders.Count > 0)
            {
                ConsoleHelper.WriteSection("Content Headers:");
                foreach (var header in response.ContentHeaders)
                {
                    Console.Write("  ");
                    ConsoleHelper.WriteKeyValue(header.Key, header.Value);
                }
            }
            
            ConsoleHelper.WriteSection("Response Body:");
            try
            {
                // Try to format and colorize JSON for better readability
                ConsoleHelper.WriteColoredJson(response.Body);
            }
            catch
            {
                // If formatting fails, display raw response
                Console.WriteLine(response.Body);
            }
        }
        
        public void DisplayApiDefinition(ApiDefinition apiDefinition)
        {
            ConsoleHelper.WriteSection("API Definition:");
            ConsoleHelper.WriteKeyValue("Name", apiDefinition.Name);
            
            Console.Write("URI: ");
            ConsoleHelper.WriteUrl(apiDefinition.Uri);
            
            Console.Write("Method: ");
            ConsoleHelper.WriteMethod(apiDefinition.Method);
            
            if (apiDefinition.Headers?.Count > 0)
            {
                ConsoleHelper.WriteInfo("Headers:");
                foreach (var header in apiDefinition.Headers)
                {
                    Console.Write("  ");
                    ConsoleHelper.WriteKeyValue(header.Key, header.Value);
                }
            }

            if (apiDefinition.Payload != null)
            {
                ConsoleHelper.WriteKeyValue("Payload Type", apiDefinition.PayloadType.ToString());
                ConsoleHelper.WriteInfo("Payload:");
                Console.Write("  ");
                
                if (apiDefinition.PayloadType == PayloadType.Json)
                {
                    try
                    {
                        // Try to format and colorize JSON payload
                        var jsonString = apiDefinition.GetPayloadAsString();
                        if (jsonString != null)
                        {
                            ConsoleHelper.WriteColoredJson(jsonString);
                        }
                        else
                        {
                            Console.WriteLine("[null payload]");
                        }
                    }
                    catch
                    {
                        // If it's not valid JSON or formatting fails, display as-is
                        Console.WriteLine(apiDefinition.Payload);
                    }
                }
                else
                {
                    // For non-JSON payloads, display as-is
                    var payloadString = apiDefinition.GetPayloadAsString();
                    Console.WriteLine(payloadString ?? "[null payload]");
                }
            }
            
            // Display file upload information if present
            if (apiDefinition.Files?.Count > 0)
            {
                ConsoleHelper.WriteInfo($"Files to Upload ({apiDefinition.Files.Count}):");
                foreach (var file in apiDefinition.Files)
                {
                    Console.Write("  ");
                    ConsoleHelper.WriteLineColored($"- {file.Name}", ConsoleColor.Cyan);
                    Console.Write("    ");
                    ConsoleHelper.WriteKeyValue("Field Name", file.FieldName);
                    Console.Write("    ");
                    ConsoleHelper.WriteKeyValue("Path", file.FilePath);
                    Console.Write("    ");
                    ConsoleHelper.WriteKeyValue("Content Type", file.ContentType);
                }
            }

            if (apiDefinition.Tests?.Count > 0)
            {
                ConsoleHelper.WriteKeyValue("Tests", $"{apiDefinition.Tests.Count} defined");
            }
        }
    }

    public class ApiResponse
    {
        public bool IsSuccessful { get; set; }
        public int StatusCode { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> ContentHeaders { get; set; } = new Dictionary<string, string>();
        public string Body { get; set; } = string.Empty;
        public long ResponseTimeMs { get; set; }
        public string? ErrorMessage { get; set; }

        public string GetHeader(string name)
        {
            if (Headers.TryGetValue(name, out string? value))
                return value;
            
            if (ContentHeaders.TryGetValue(name, out string? contentValue))
                return contentValue;
            
            return string.Empty;
        }

        public bool HasHeader(string name)
        {
            return Headers.ContainsKey(name) || ContentHeaders.ContainsKey(name);
        }
        
        
    }
}
