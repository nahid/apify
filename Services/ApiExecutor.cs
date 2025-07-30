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
        private readonly ApiExecutorOptions? _options;

        public ApiExecutor(ApiExecutorOptions? options = null)
        {
            _httpClient = new HttpClient();
            _configService = new ConfigService();
            _options = options;
            
        }


        public async Task<ResponseDefinitionSchema> ExecuteRequestAsync(RequestDefinitionSchema requestDefinitionSchema)
        {
            var stopwatch = new Stopwatch();
            var response = new ResponseDefinitionSchema();
            
            try
            {
                // Create a request message
                // Check if URL has a valid scheme (http:// or https://)
                var url = requestDefinitionSchema.Url;
                
                // If the URL doesn't start with http:// or https://, add https:// prefix
                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                    !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    url = "https://" + url;
                }
                
                var request = new HttpRequestMessage(new HttpMethod(requestDefinitionSchema.Method), url);

                if (_configService.LoadConfiguration().Authorization != null)
                {
                    // If authorization is set in the config, add it to the request headers
                    var authHeader = _configService.LoadConfiguration().Authorization;

                    switch (authHeader?.Type)
                    {
                        case AuthorizationType.Bearer:
                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _configService.LoadConfiguration().Authorization?.Token);
                            break;
                        case AuthorizationType.Basic:
                            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _configService.LoadConfiguration().Authorization?.Token);
                            break;
                        case AuthorizationType.ApiKey:
                            // For API Key, we assume the token is the key itself
                            request.Headers.Add("Api-Key", _configService.LoadConfiguration().Authorization?.Token ?? string.Empty);
                            break;
                        default:
                            request.Headers.Authorization = null; // No authorization header if type is not recognized
                            break;
                    }
                }

                // Add headers
                if (requestDefinitionSchema.Headers != null)
                {
                    foreach (var header in requestDefinitionSchema.Headers)
                    {
                        if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) && 
                            (requestDefinitionSchema.PayloadType != PayloadContentType.None || 
                             requestDefinitionSchema.Body?.Multipart != null))
                        {
                            // Content-Type will be set with the content
                            continue;
                        }
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                // Add content for appropriate methods
                bool isMethodWithBody = requestDefinitionSchema.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) || 
                                       requestDefinitionSchema.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) || 
                                       requestDefinitionSchema.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase);
                
                if (isMethodWithBody)
                {
                    // Check for file uploads first (takes precedence if files are present)
                    if (requestDefinitionSchema.PayloadType == PayloadContentType.Multipart && requestDefinitionSchema.Body?.Multipart != null)
                    {
                        await AddMultipartContentAsync(request, requestDefinitionSchema);
                    }
                    // Then check for payload
                    else if (requestDefinitionSchema.Body != null)
                    {
                        AddPayloadContent(request, requestDefinitionSchema);
                    }
                }

                // Set timeout
                _httpClient.Timeout = TimeSpan.FromMilliseconds(requestDefinitionSchema.Timeout);

                // Send a request and measure time
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
                
                // The detailed error info is already included in the response object,
                // so we don't need to log anything here
                
                // Include the error message in the response body for test assertion context
                response.Body = ex.ToString();
                //response.Body = $"{{\"error\": \"{ex.Message.Evaluate("\"", "\\\"")}\", \"exception_type\": \"{ex.GetType().Name}\"}}";
            }

            return response;
        }

        private void AddPayloadContent(HttpRequestMessage request, RequestDefinitionSchema requestDefinitionSchema)
        {
            string contentType;
            if (requestDefinitionSchema.Headers != null && 
                requestDefinitionSchema.Headers.TryGetValue("Content-Type", out string? headerContentType))
            {
                contentType = headerContentType;
            }
            else
            {
                // Set the appropriate content type based on a payload type
                contentType = requestDefinitionSchema.PayloadType switch
                {
                    PayloadContentType.Json => "application/json",
                    PayloadContentType.Text => "text/plain",
                    PayloadContentType.FormData => "application/x-www-form-urlencoded",
                    _ => "application/json" // Default to JSON
                };
            }
            
            // Process payload based on type
            switch (requestDefinitionSchema.PayloadType)
            {
                case PayloadContentType.Json:
                    // Payload is already an object, just serialize it with indentation
                    string formattedJson = JsonConvert.SerializeObject(requestDefinitionSchema.Body?.Json ?? new object(), Formatting.Indented);
                    request.Content = new StringContent(formattedJson, Encoding.UTF8);
                    break;

                case PayloadContentType.FormData:
                    // For form data, we need to format as a key=value&key2=value2...
                    try
                    {
                        var formData = new FormUrlEncodedContent(requestDefinitionSchema.Body?.FormData ?? new Dictionary<string, string>());
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
                    request.Content = new StringContent(requestDefinitionSchema.Body?.Text ?? string.Empty, Encoding.UTF8);
                    break;
                
                case PayloadContentType.Binary:
                    if (!MiscHelper.IsLikelyPath(requestDefinitionSchema.Body?.Binary ?? ""))
                    {
                        request.Content = new StringContent(requestDefinitionSchema.Body?.Binary ?? string.Empty, Encoding.UTF8);
                        contentType = "text/plain"; // Fallback to text/plain for binary content
                        break;
                    }
                    
                    byte[] binaryData = File.ReadAllBytes(requestDefinitionSchema.Body?.Binary ?? string.Empty);
                    contentType = "application/octet-stream"; // Default binary content type
                    request.Content = new ByteArrayContent(binaryData);
                    break;

                default:
                    // For the "none" payload type, create an empty content
                    request.Content = new StringContent(string.Empty, Encoding.UTF8);
                    break;
            }
            
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        }
        
        private async Task AddMultipartContentAsync(HttpRequestMessage request, RequestDefinitionSchema requestDefinitionSchema)
        {
            var formData = new MultipartFormDataContent();

            // Add text fields from payload if specified and if it's a form data payload
            if (requestDefinitionSchema.Body != null && requestDefinitionSchema.PayloadType == PayloadContentType.Multipart)
            {
                try
                {
                    foreach (var field in requestDefinitionSchema.Body?.Multipart ?? new List<MultipartData>())
                    {
                        if (string.IsNullOrEmpty(field.Name))
                        {
                            continue;
                        }
                        
                        if (field.Content != null && MiscHelper.IsLikelyPath(field.Content) && File.Exists(field.Content))
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
                            if (field.Content != null)
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
        
        public RequestDefinitionSchema ApplyEnvToApiDefinition(RequestDefinitionSchema requestDefinitionSchema, string environment, Dictionary<string, string>? variables)
        {
            var apiDefContent = JsonHelper.SerializeToJson(requestDefinitionSchema);
            
            if (string.IsNullOrEmpty(apiDefContent))
            {
                return requestDefinitionSchema;
            }
            
            var vars = _configService.GetEnvironmentVariables(environment, variables);
            vars = MiscHelper.MergeDictionaries(vars, requestDefinitionSchema.Variables ?? new Dictionary<string, string>());
            

            var stubReplacor = new Dictionary<string, object>();
            
            stubReplacor.Add("env", vars);
            
            apiDefContent = TagInterpolationManager.Evaluate(apiDefContent, stubReplacor);
            
            if (string.IsNullOrEmpty(apiDefContent))
            {
                return requestDefinitionSchema;
            }

            requestDefinitionSchema = JsonHelper.DeserializeString<RequestDefinitionSchema>(apiDefContent) ?? new RequestDefinitionSchema();

            return requestDefinitionSchema;
        }
        
        public void DisplayTestStats(TestResults testResults)
        {
            if (_options is { Verbose: false, Tests: false }) return;

            if (_options?.Tests == true)
            {
                ConsoleHelper.WriteSection("===============================");
                ConsoleHelper.WriteKeyValue("Test Summary", $"{testResults.Results.Count}/{testResults.PassedCount} tests passed");
                ConsoleHelper.WriteLineColored("===============================\n", ConsoleColor.Cyan);
            }

        }
        public void DisplayTestResults(TestResults testResults)
        {
            if (_options is { Verbose: false, Tests: false }) return;
            
            if (_options?.Tests == false)
            {
                if (_options.Debug) 
                {
                    ConsoleHelper.WriteWarning("Tests are not enabled, skipping test results display.");
                }
                
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
        
        public void DisplayApiResponse(ResponseDefinitionSchema responseDefinitionSchema)
        {
            var status = GetStatusByCode(responseDefinitionSchema.StatusCode);
            var color = responseDefinitionSchema.StatusCode >= 200 && responseDefinitionSchema.StatusCode < 300 ? ConsoleColor.Green : 
                        responseDefinitionSchema.StatusCode >= 300 && responseDefinitionSchema.StatusCode < 400 ? ConsoleColor.Cyan :
                        responseDefinitionSchema.StatusCode >= 400 && responseDefinitionSchema.StatusCode < 500 ? ConsoleColor.Yellow : 
                        responseDefinitionSchema.StatusCode >= 500 && responseDefinitionSchema.StatusCode < 600 ? ConsoleColor.Red :
                        ConsoleColor.DarkRed;
            
            ConsoleHelper.WriteColored("Response: ", ConsoleColor.White);
            ConsoleHelper.WriteColored(status, color);
            Console.WriteLine("");
            ConsoleHelper.WriteFeatures("Status Code", $"{responseDefinitionSchema.StatusCode} ({MiscHelper.GetHttpStatusCodeName(responseDefinitionSchema.StatusCode)})");

            if (responseDefinitionSchema.ContentType != null)
                ConsoleHelper.WriteFeatures("Content Type", responseDefinitionSchema.ContentType);

            ConsoleHelper.WriteFeatures("Response Time", $"{responseDefinitionSchema.ResponseTimeMs} ms");

            if (_options?.ShowResponse == true || _options?.Verbose == true)
            {
                if (responseDefinitionSchema.Headers.Count > 0)
                {
                    ConsoleHelper.WriteSection("Response Headers:");
                    foreach (var header in responseDefinitionSchema.Headers)
                    {
                        Console.Write("  ");
                        ConsoleHelper.WriteKeyValue(header.Key, header.Value);
                    }
                }
            
                if (responseDefinitionSchema.ContentHeaders.Count > 0)
                {
                    ConsoleHelper.WriteSection("Content Headers:");
                    foreach (var header in responseDefinitionSchema.ContentHeaders)
                    {
                        Console.Write("  ");
                        ConsoleHelper.WriteKeyValue(header.Key, header.Value);
                    }
                }
            }
            
            if (_options is { ShowOnlyResponse: false, Verbose: false, ShowResponse: false })
            {
                return;
            }
            
            ConsoleHelper.WriteSection("Response Body:");
            try
            {
                // Try to format and colorize JSON for better readability
                ConsoleHelper.WriteColoredJson(responseDefinitionSchema.Body);
            }
            catch
            {
                // If formatting fails, display raw response
                Console.WriteLine(responseDefinitionSchema.Body);
            }
        }
        
        public void DisplayApiDefinition(RequestDefinitionSchema requestDefinitionSchema)
        {
            if (_options is { ShowRequest: false, Verbose: false })
            {
                return;
            }
            
            ConsoleHelper.WriteSection("API Definition:");
            ConsoleHelper.WriteKeyValue("Name", requestDefinitionSchema.Name);
            
            Console.Write("URL: ");
            ConsoleHelper.WriteUrl(requestDefinitionSchema.Url);
            
            Console.Write("Method: ");
            ConsoleHelper.WriteMethod(requestDefinitionSchema.Method);
            
            if (requestDefinitionSchema.Headers?.Count > 0)
            {
                ConsoleHelper.WriteInfo("Headers:");
                foreach (var header in requestDefinitionSchema.Headers)
                {
                    Console.Write("  ");
                    ConsoleHelper.WriteKeyValue(header.Key, header.Value);
                }
            }

            if (requestDefinitionSchema.Body != null)
            {
                ConsoleHelper.WriteKeyValue("Payload Type", requestDefinitionSchema.PayloadType.ToString() ?? "None");
                ConsoleHelper.WriteInfo("Payload:");
                Console.Write("  ");
                
                if (requestDefinitionSchema.PayloadType == PayloadContentType.Json)
                {
                    try
                    {
                        // Try to format and colorize JSON payload
                        var jsonString = requestDefinitionSchema.GetPayloadAsString();
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
                        Console.WriteLine(requestDefinitionSchema.GetBodyPayload());
                    }
                }
                else
                {
                    // For non-JSON payloads, display as-is
                    var payloadString = requestDefinitionSchema.GetPayloadAsString();
                    Console.WriteLine(payloadString ?? "[null payload]");
                }
            }

            if (requestDefinitionSchema.Tests?.Count > 0)
            {
                ConsoleHelper.WriteKeyValue("Tests", $"{requestDefinitionSchema.Tests.Count} defined");
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
