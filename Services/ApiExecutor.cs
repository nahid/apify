using APITester.Models;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

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
                        if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(apiDefinition.Payload))
                        {
                            // Content-Type will be set with the content
                            continue;
                        }
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }

                // Add payload for appropriate methods
                if (!string.IsNullOrEmpty(apiDefinition.Payload) && 
                    (apiDefinition.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) || 
                     apiDefinition.Method.Equals("PUT", StringComparison.OrdinalIgnoreCase) || 
                     apiDefinition.Method.Equals("PATCH", StringComparison.OrdinalIgnoreCase)))
                {
                    string contentType = "application/json";
                    if (apiDefinition.Headers != null && 
                        apiDefinition.Headers.TryGetValue("Content-Type", out string? headerContentType))
                    {
                        contentType = headerContentType;
                    }

                    var content = new StringContent(apiDefinition.Payload, Encoding.UTF8);
                    content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
                    request.Content = content;
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
