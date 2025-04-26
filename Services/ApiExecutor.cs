using APITester.Models;
using APITester.Utils;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace APITester.Services
{
    public class ApiExecutor
    {
        private readonly HttpClient _httpClient;

        public ApiExecutor()
        {
            _httpClient = new HttpClient();
        }

        public async Task<ApiResponse> ExecuteRequestAsync(ApiDefinition apiDefinition)
        {
            var stopwatch = new Stopwatch();
            var response = new ApiResponse();
            
            try
            {
                // Create request message
                var request = new HttpRequestMessage(new HttpMethod(apiDefinition.Method), apiDefinition.Uri);

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
                    else if (!string.IsNullOrEmpty(apiDefinition.Payload))
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

            // Process payload based on type
            StringContent content;
            switch (apiDefinition.PayloadType)
            {
                case PayloadType.Json:
                    // Try to format JSON if it's not already formatted
                    try
                    {
                        // Verify it's valid JSON and format it
                        var jsonObj = JsonConvert.DeserializeObject(apiDefinition.Payload!);
                        string formattedJson = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                        content = new StringContent(formattedJson, Encoding.UTF8);
                    }
                    catch
                    {
                        // If not valid JSON or error occurs, use as-is
                        content = new StringContent(apiDefinition.Payload!, Encoding.UTF8);
                    }
                    break;

                case PayloadType.FormData:
                    // For form data, we need to format as key=value&key2=value2...
                    try
                    {
                        // Try to parse as a dictionary or use as is
                        var formData = new FormUrlEncodedContent(
                            JsonConvert.DeserializeObject<Dictionary<string, string>>(apiDefinition.Payload!)!
                        );
                        request.Content = formData;
                        return; // Skip the content-type setting below as FormUrlEncodedContent sets it
                    }
                    catch
                    {
                        // If not valid JSON dictionary, use as-is
                        content = new StringContent(apiDefinition.Payload!, Encoding.UTF8);
                    }
                    break;

                case PayloadType.Text:
                default:
                    content = new StringContent(apiDefinition.Payload!, Encoding.UTF8);
                    break;
            }

            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            request.Content = content;
        }

        private async Task AddMultipartFormDataContentAsync(HttpRequestMessage request, ApiDefinition apiDefinition)
        {
            var multipartContent = new MultipartFormDataContent();

            // Add text fields from payload if specified and if it's a form data payload
            if (!string.IsNullOrEmpty(apiDefinition.Payload) && apiDefinition.PayloadType == PayloadType.FormData)
            {
                try
                {
                    var formFields = JsonConvert.DeserializeObject<Dictionary<string, string>>(apiDefinition.Payload);
                    if (formFields != null)
                    {
                        foreach (var field in formFields)
                        {
                            multipartContent.Add(new StringContent(field.Value), field.Key);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteWarning($"Error parsing form data payload: {ex.Message}");
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
                catch (Exception ex)
                {
                    ConsoleHelper.WriteWarning($"Error adding file {file.FilePath}: {ex.Message}");
                }
            }

            request.Content = multipartContent;
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
    }
}
