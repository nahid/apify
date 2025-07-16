using Apify.Commands;
using Apify.Models;
using Apify.Utils;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apify.Services
{
    public class ApiExecutor
    {
        private readonly HttpClient _httpClient;
      
        private ConfigService _configService;
        private ApiExecutorOptions? __options;

        public ApiExecutor(ApiExecutorOptions? options = null)
        {
            _httpClient = new HttpClient();
            _configService = new ConfigService();
            __options = options;
            
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
                            (apiDefinition.PayloadType != PayloadContentType.None || 
                             apiDefinition.Body?.Multipart != null))
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
                    if (apiDefinition.PayloadType == PayloadContentType.Multipart && apiDefinition.Body?.Multipart != null)
                    {
                        await AddMultipartContentAsync(request, apiDefinition);
                    }
                    // Then check for payload
                    else if (apiDefinition.Body != null)
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
                response.ContentType = httpResponse.Content.Headers.ContentType?.MediaType;
                
                if (httpResponse.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    try
                    {
                        // Try to parse the response body as JSON
                        response.Json = JToken.Parse(response.Body);
                    }
                    catch (JsonReaderException)
                    {
                        // If parsing fails, keep Json as null
                        response.Json = null;
                    }
                }
                else
                {
                    response.Json = null; // Not a JSON response
                }
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
                response.Body = ex.ToString();
                //response.Body = $"{{\"error\": \"{ex.Message.Replace("\"", "\\\"")}\", \"exception_type\": \"{ex.GetType().Name}\"}}";
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
                    PayloadContentType.Json => "application/json",
                    PayloadContentType.Text => "text/plain",
                    PayloadContentType.FormData => "application/x-www-form-urlencoded",
                    _ => "application/json" // Default to JSON
                };
            }
            
            // Process payload based on type
            StringContent content;
            switch (apiDefinition.PayloadType)
            {
                case PayloadContentType.Json:
                    // Payload is already an object, just serialize it with indentation
                    string formattedJson = JsonConvert.SerializeObject(apiDefinition.Body?.Json ?? new object(), Formatting.Indented);
                    request.Content = new StringContent(formattedJson, Encoding.UTF8);
                    break;

                case PayloadContentType.FormData:
                    // For form data, we need to format as key=value&key2=value2...
                    try
                    {
                        var formData = new FormUrlEncodedContent(apiDefinition.Body?.FormData ?? new Dictionary<string, string>());
                        request.Content = formData;
                        return; // Skip the content-type setting below as FormUrlEncodedContent sets it
                    }
                    catch
                    {
                        // Error is already handled by falling back to string content
                        request.Content = new StringContent(string.Empty, Encoding.UTF8);
                    }
                    break;

                case PayloadContentType.Text:
                    request.Content = new StringContent(apiDefinition.Body?.Text ?? string.Empty, Encoding.UTF8);
                    break;
                
                case PayloadContentType.Binary:
                    if (!MiscHelper.IsLikelyPath(apiDefinition.Body?.Binary ?? ""))
                    {
                        request.Content = new StringContent(apiDefinition.Body?.Binary ?? string.Empty, Encoding.UTF8);
                        contentType = "text/plain"; // Fallback to text/plain for binary content
                        break;
                    }
                    
                    byte[] binaryData = File.ReadAllBytes(apiDefinition.Body?.Binary ?? string.Empty);
                    contentType = "application/octet-stream"; // Default binary content type
                    request.Content = new ByteArrayContent(binaryData);
                    break;
                    
                case PayloadContentType.None:
                default:
                    // For "none" payload type, create an empty content
                    request.Content = new StringContent(string.Empty, Encoding.UTF8);
                    break;
            }
            
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }
        
        private async Task AddMultipartContentAsync(HttpRequestMessage request, ApiDefinition apiDefinition)
        {
            var formData = new MultipartFormDataContent();

            // Add text fields from payload if specified and if it's a form data payload
            if (apiDefinition.Body != null && apiDefinition.PayloadType == PayloadContentType.Multipart)
            {
                try
                {
                    foreach (var field in apiDefinition.Body?.Multipart ?? new List<MultipartData>())
                    {
                        if (string.IsNullOrEmpty(field.Name))
                        {
                            continue;
                        }
                        
                        if (MiscHelper.IsLikelyPath(field.Content) && File.Exists(field.Content))
                        {
                            // If the content is a file path, read the file and add it as a byte array
                            var fileBytes = await File.ReadAllBytesAsync(field.Content);
                            var fileContent = new ByteArrayContent(fileBytes);
                            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                            formData.Add(fileContent, field.Name, Path.GetFileName(field.Content));
                        }
                        else
                        {
                            // Otherwise, treat it as a regular string content
                            formData.Add(new StringContent(field.Content), field.Name);
                        }
                    }
                }
                catch
                {
                    // Silently ignore parsing errors for form data
                }
            }

            request.Content = formData;
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
        
        public void DisplayTestStats(TestResults testResults)
        {
            if (__options.Verbose == false && __options.Tests == false) return;

            if (__options.Tests == true)
            {
                ConsoleHelper.WriteSection("===============================");
                ConsoleHelper.WriteKeyValue("Test Summary", $"{testResults.Results.Count}/{testResults.PassedCount} tests passed");
                ConsoleHelper.WriteLineColored("===============================\n", ConsoleColor.Cyan);
            }

        }
        public void DisplayTestResults(TestResults testResults)
        {
            if (__options.Verbose == false && __options.Tests == false) return;
            
            if (__options.Tests == true)
            {
                return; // No need to display test results if tests are not enabled
            }
            
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
        
        private string GetStatusByCode(int statusCode)
        {
            return statusCode switch
            {
                >= 200 and < 300 => "Success",
                >= 300 and < 400 => "Redirection",
                >= 400 and < 500 => "Client Error",
                >= 500 and < 600 => "Server Error",
                _ => "Unknown Status"
            };
        }
        
        public void DisplayApiResponse(ApiResponse response)
        {
            var status = GetStatusByCode(response.StatusCode);
            var color = response.StatusCode >= 200 && response.StatusCode < 300 ? ConsoleColor.Green : 
                        response.StatusCode >= 300 && response.StatusCode < 400 ? ConsoleColor.Cyan :
                        response.StatusCode >= 400 && response.StatusCode < 500 ? ConsoleColor.Yellow : 
                        response.StatusCode >= 500 && response.StatusCode < 600 ? ConsoleColor.Red :
                        ConsoleColor.DarkRed;
            
            ConsoleHelper.WriteColored("Response: ", ConsoleColor.White);
            ConsoleHelper.WriteColored(status, color);
            Console.WriteLine("");
            ConsoleHelper.WriteFeatures("Status Code", $"{response.StatusCode} ({MiscHelper.GetHttpStatusCodeName(response.StatusCode)})");
            ConsoleHelper.WriteFeatures("Content Type", response.ContentType);
            ConsoleHelper.WriteFeatures("Response Time", $"{response.ResponseTimeMs} ms");

            if (__options.ShowResponse == true || __options.Verbose == true)
            {
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
            }
            
            if (__options.ShowOnlyResponse == false && __options.Verbose == false)
            {
                return;
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
            if (__options.ShowRequest == false && __options.Verbose == false)
            {
                return;
            }
            
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

            if (apiDefinition.Body != null)
            {
                ConsoleHelper.WriteKeyValue("Payload Type", apiDefinition.PayloadType.ToString() ?? "None");
                ConsoleHelper.WriteInfo("Payload:");
                Console.Write("  ");
                
                if (apiDefinition.PayloadType == PayloadContentType.Json)
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
                        Console.WriteLine(apiDefinition.GetBodyPayload());
                    }
                }
                else
                {
                    // For non-JSON payloads, display as-is
                    var payloadString = apiDefinition.GetPayloadAsString();
                    Console.WriteLine(payloadString ?? "[null payload]");
                }
            }

            if (apiDefinition.Tests?.Count > 0)
            {
                ConsoleHelper.WriteKeyValue("Tests", $"{apiDefinition.Tests.Count} defined");
            }
        }
    }
    
    public record ApiExecutorOptions(
        bool? Tests,
        bool? ShowRequest,
        bool? ShowResponse,
        bool? ShowOnlyResponse,
        bool? Verbose,
        bool Debug
    );
}
