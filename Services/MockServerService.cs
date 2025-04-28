using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Apify.Models;
using Apify.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apify.Services
{
    public class MockServerService
    {
        private readonly string _mockDirectory;
        private readonly List<MockApiDefinition> _mockDefinitions = new();
        private readonly List<AdvancedMockApiDefinition> _advancedMockDefinitions = new();
        private readonly EnvironmentService _environmentService;
        private readonly ConditionEvaluator _conditionEvaluator = new();
        private bool _verbose;
        private HttpListener? _listener;
        private bool _isRunning;
        
        public MockServerService(string mockDirectory)
        {
            _mockDirectory = mockDirectory;
            _environmentService = new EnvironmentService();
        }
        
        public async Task StartAsync(int port, bool verbose)
        {
            _verbose = verbose;
            await _environmentService.LoadConfig(); // Load env variables for templates
            
            // Load all mock definitions
            LoadMockDefinitions();
            
            if (_mockDefinitions.Count == 0 && _advancedMockDefinitions.Count == 0)
            {
                ConsoleHelper.WriteWarning("No mock API definitions found. Create .mock.json files in your .apify directory.");
                ConsoleHelper.WriteInfo("Example path: .apify/users/all.mock.json");
                return;
            }
            
            // Start HTTP listener
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://*:{port}/");
            
            try
            {
                _listener.Start();
                _isRunning = true;
                
                ConsoleHelper.WriteSuccess($"Mock API Server started on http://0.0.0.0:{port}");
                ConsoleHelper.WriteInfo("Available endpoints:");
                
                // Display legacy mock endpoints
                foreach (var mock in _mockDefinitions)
                {
                    ConsoleHelper.WriteSuccess($"[{mock.Method}] {mock.Endpoint} - {mock.Name}");
                }
                
                // Display advanced mock endpoints
                foreach (var mock in _advancedMockDefinitions)
                {
                    ConsoleHelper.WriteSuccess($"[{mock.Method}] {mock.Endpoint} - {mock.Name} (Advanced)");
                }
                
                Console.WriteLine("\nPress Ctrl+C to stop the server...");
                
                // Handle requests
                while (_isRunning)
                {
                    var context = await _listener.GetContextAsync();
                    _ = ProcessRequestAsync(context);
                }
            }
            catch (HttpListenerException ex) when (ex.Message.Contains("Access is denied") || ex.ErrorCode == 5)
            {
                ConsoleHelper.WriteError($"Error starting mock server: {ex.Message}");
                
                // Provide helpful guidance for Windows users
                if (OperatingSystem.IsWindows())
                {
                    Console.WriteLine();
                    ConsoleHelper.WriteInfo("This error commonly occurs on Windows when binding to HTTP ports without administrator privileges.");
                    Console.WriteLine();
                    ConsoleHelper.WriteInfo("To resolve this issue, you can:");
                    Console.WriteLine("1. Run your command prompt as Administrator");
                    Console.WriteLine("   Right-click on cmd/PowerShell and select 'Run as administrator'");
                    Console.WriteLine();
                    Console.WriteLine("2. Add a URL reservation (one-time setup, preferred solution):");
                    Console.WriteLine($"   Run this in an Administrator PowerShell: netsh http add urlacl url=http://+:{port}/ user=Everyone");
                    Console.WriteLine();
                    Console.WriteLine("3. Try using a port number above 1024 (e.g., 8080):");
                    Console.WriteLine($"   dotnet run mock-server --port 8080");
                    Console.WriteLine();
                }
                
                if (_verbose)
                {
                    Console.WriteLine(ex.StackTrace);
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError($"Error starting mock server: {ex.Message}");
                if (_verbose)
                {
                    Console.WriteLine(ex.StackTrace);
                }
            }
        }
        
        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
            _listener?.Close();
        }
        
        private void LoadMockDefinitions()
        {
            if (!Directory.Exists(_mockDirectory))
            {
                Directory.CreateDirectory(_mockDirectory);
                ConsoleHelper.WriteInfo($"Created mock API directory: {_mockDirectory}");
                return;
            }
            
            // Find all .mock.json files in the directory and subdirectories
            var mockFiles = Directory.GetFiles(_mockDirectory, "*.mock.json", SearchOption.AllDirectories);
            
            if (_verbose)
            {
                ConsoleHelper.WriteInfo($"Found {mockFiles.Length} mock API definition files");
            }
            
            foreach (var file in mockFiles)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    
                    // Always log this file regardless of verbose setting
                    bool isProductSearchMock = file.Contains("search.mock.json");
                    if (isProductSearchMock || _verbose)
                    {
                        ConsoleHelper.WriteInfo($"Attempting to load mock from: {file}");
                        if (isProductSearchMock)
                        {
                            Console.WriteLine($"Search mock JSON content: {json.Substring(0, Math.Min(100, json.Length))}...");
                        }
                    }
                    
                    // First try to parse as advanced mock definition
                    AdvancedMockApiDefinition advancedMockDef;
                    try
                    {
                        advancedMockDef = JsonConvert.DeserializeObject<AdvancedMockApiDefinition>(json) ?? new AdvancedMockApiDefinition();
                        if (_verbose)
                        {
                            ConsoleHelper.WriteInfo($"Successfully parsed {file} as AdvancedMockApiDefinition");
                        }
                    }
                    catch (Exception ex)
                    {
                        bool isProductSearchMockError = file.Contains("search.mock.json");
                        ConsoleHelper.WriteError($"Error parsing {file} as AdvancedMockApiDefinition: {ex.Message}");
                        if (isProductSearchMockError || _verbose)
                        {
                            Console.WriteLine($"Error details for {file}:");
                            Console.WriteLine(ex.Message);
                            Console.WriteLine(ex.StackTrace);
                            if (ex.InnerException != null)
                            {
                                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                                Console.WriteLine(ex.InnerException.StackTrace);
                            }
                        }
                        continue;
                    }
                    
                    if (advancedMockDef != null && advancedMockDef.Responses != null && advancedMockDef.Responses.Count > 0)
                    {
                        _advancedMockDefinitions.Add(advancedMockDef);
                        if (_verbose)
                        {
                            ConsoleHelper.WriteInfo($"Loaded advanced mock API: {advancedMockDef.Name} [{advancedMockDef.Method}] {advancedMockDef.Endpoint}");
                        }
                        continue; // Skip legacy mock format if advanced format was detected
                    }
                    
                    // If not advanced format, try legacy format
                    var mockDef = JsonConvert.DeserializeObject<MockApiDefinition>(json);
                    if (mockDef != null)
                    {
                        // If the mock definition has a responseFile, load its content
                        if (!string.IsNullOrEmpty(mockDef.ResponseFile))
                        {
                            var responseFilePath = Path.Combine(Path.GetDirectoryName(file) ?? string.Empty, mockDef.ResponseFile);
                            if (File.Exists(responseFilePath))
                            {
                                string responseContent = File.ReadAllText(responseFilePath);
                                
                                // Try to parse as JSON, otherwise use as string
                                try
                                {
                                    mockDef.Response = JsonConvert.DeserializeObject(responseContent);
                                }
                                catch
                                {
                                    mockDef.Response = responseContent;
                                }
                            }
                            else
                            {
                                ConsoleHelper.WriteWarning($"Response file not found: {responseFilePath}");
                            }
                        }
                        
                        // Process conditions if present
                        if (mockDef.Conditions != null)
                        {
                            foreach (var condition in mockDef.Conditions)
                            {
                                if (!string.IsNullOrEmpty(condition.ResponseFile))
                                {
                                    var responseFilePath = Path.Combine(Path.GetDirectoryName(file) ?? string.Empty, condition.ResponseFile);
                                    if (File.Exists(responseFilePath))
                                    {
                                        string responseContent = File.ReadAllText(responseFilePath);
                                        
                                        // Try to parse as JSON, otherwise use as string
                                        try
                                        {
                                            condition.Response = JsonConvert.DeserializeObject(responseContent);
                                        }
                                        catch
                                        {
                                            condition.Response = responseContent;
                                        }
                                    }
                                }
                            }
                        }
                        
                        _mockDefinitions.Add(mockDef);
                        if (_verbose)
                        {
                            ConsoleHelper.WriteInfo($"Loaded mock API: {mockDef.Name} [{mockDef.Method}] {mockDef.Endpoint}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"Error loading mock definition from {file}: {ex.Message}");
                    if (_verbose)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            }
        }
        
        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            
            string requestUrl = request.Url?.AbsolutePath ?? string.Empty;
            string method = request.HttpMethod;
            
            if (_verbose)
            {
                ConsoleHelper.WriteInfo($"Received request: {method} {requestUrl}");
                
                foreach (var headerKey in request.Headers.AllKeys)
                {
                    if (_verbose)
                    {
                        ConsoleHelper.WriteInfo($"  Header: {headerKey}: {request.Headers[headerKey]}");
                    }
                }
            }
            
            // Look for advanced mock definition first
            var advancedMockDef = FindMatchingAdvancedMockDefinition(request);
            Dictionary<string, string> pathParams = new Dictionary<string, string>();
            
            if (advancedMockDef != null)
            {
                await ProcessAdvancedMockResponseAsync(context, advancedMockDef, pathParams);
                return;
            }
            
            // If no advanced mock definition found, try legacy format
            var mockDef = FindMatchingMockDefinition(request);            
            if (mockDef != null)
            {
                // Extract path parameters for use in templates
                string urlPath = request.Url?.AbsolutePath ?? string.Empty;
                ExtractPathParameters(mockDef.Endpoint, urlPath, out pathParams);
                
                // Check authentication if required
                if (mockDef.RequireAuthentication)
                {
                    string? authHeader = request.Headers[mockDef.AuthHeaderName];
                    bool isAuthenticated = false;
                    
                    if (!string.IsNullOrEmpty(authHeader))
                    {
                        if (string.IsNullOrEmpty(mockDef.AuthHeaderPrefix) || 
                            authHeader.StartsWith(mockDef.AuthHeaderPrefix, StringComparison.OrdinalIgnoreCase))
                        {
                            string token = authHeader;
                            
                            // If prefix is specified, extract the token part
                            if (!string.IsNullOrEmpty(mockDef.AuthHeaderPrefix) && 
                                authHeader.StartsWith(mockDef.AuthHeaderPrefix, StringComparison.OrdinalIgnoreCase))
                            {
                                token = authHeader.Substring(mockDef.AuthHeaderPrefix.Length).Trim();
                            }
                            
                            // Validate token if tokens are specified
                            if (mockDef.ValidTokens != null && mockDef.ValidTokens.Count > 0)
                            {
                                isAuthenticated = mockDef.ValidTokens.Contains(token);
                            }
                            else
                            {
                                // If no specific tokens are specified, any non-empty token is considered valid
                                isAuthenticated = !string.IsNullOrEmpty(token);
                            }
                        }
                    }
                    
                    // If authentication failed, return 401 Unauthorized
                    if (!isAuthenticated)
                    {
                        response.StatusCode = 401;
                        response.ContentType = "application/json";
                        response.Headers.Add("WWW-Authenticate", $"{mockDef.AuthHeaderPrefix} realm=\"Mock API Server\"");
                        
                        string unauthorizedContent;
                        if (mockDef.UnauthorizedResponse != null)
                        {
                            unauthorizedContent = mockDef.UnauthorizedResponse is string str 
                                ? str 
                                : JsonConvert.SerializeObject(mockDef.UnauthorizedResponse, Formatting.Indented);
                        }
                        else
                        {
                            unauthorizedContent = JsonConvert.SerializeObject(new { 
                                error = "Unauthorized", 
                                message = "Authentication required" 
                            }, Formatting.Indented);
                        }
                        
                        byte[] authBuffer = Encoding.UTF8.GetBytes(unauthorizedContent);
                        response.ContentLength64 = authBuffer.Length;
                        await response.OutputStream.WriteAsync(authBuffer);
                        
                        if (_verbose)
                        {
                            ConsoleHelper.WriteWarning($"Authentication failed for {method} {requestUrl}");
                        }
                        
                        return;
                    }
                }
                
                // Handle file uploads if the endpoint accepts them
                if (mockDef.AcceptsFileUpload && request.ContentType != null && request.ContentType.StartsWith("multipart/form-data"))
                {
                    try
                    {
                        if (_verbose)
                        {
                            ConsoleHelper.WriteInfo($"Processing file upload for {method} {requestUrl}");
                        }
                        
                        Dictionary<string, string> formFields = new Dictionary<string, string>();
                        Dictionary<string, (string FileName, byte[] Data)> files = new Dictionary<string, (string, byte[])>();
                        
                        await ProcessMultipartFormDataAsync(request, formFields, files);
                        
                        if (files.Count > 0)
                        {
                            string uploadDir = mockDef.SaveUploadedFilesTo ?? "uploads";
                            
                            // Create directory if it doesn't exist
                            if (!Directory.Exists(uploadDir))
                            {
                                Directory.CreateDirectory(uploadDir);
                            }
                            
                            List<string> savedFiles = new List<string>();
                            
                            foreach (var file in files)
                            {
                                string fileName = Path.GetFileName(file.Value.FileName);
                                string filePath = Path.Combine(uploadDir, fileName);
                                
                                await File.WriteAllBytesAsync(filePath, file.Value.Data);
                                savedFiles.Add(fileName);
                                
                                if (_verbose)
                                {
                                    ConsoleHelper.WriteSuccess($"Saved uploaded file: {fileName} ({file.Value.Data.Length} bytes)");
                                }
                            }
                            
                            // Add file info to path params for template substitution
                            pathParams["fileName"] = files.Count == 1 ? Path.GetFileName(files.First().Value.FileName) : "";
                            pathParams["fileCount"] = files.Count.ToString();
                            pathParams["files"] = string.Join(",", savedFiles);
                        }
                    }
                    catch (Exception ex)
                    {
                        ConsoleHelper.WriteError($"Error processing file upload: {ex.Message}");
                    }
                }
                
                // Apply any delay specified in the mock
                if (mockDef.Delay > 0)
                {
                    await Task.Delay(mockDef.Delay);
                }
                
                // Set response status code
                response.StatusCode = mockDef.StatusCode;
                
                // Set content type
                response.ContentType = mockDef.ContentType;
                
                // Add any custom headers
                if (mockDef.Headers != null)
                {
                    foreach (var header in mockDef.Headers)
                    {
                        response.Headers.Add(header.Key, header.Value);
                    }
                }
                
                // Prepare response content
                string responseContent;
                
                // Check for matching condition first
                var matchedCondition = FindMatchingCondition(mockDef, request);
                if (matchedCondition != null)
                {
                    if (matchedCondition.StatusCode.HasValue)
                    {
                        response.StatusCode = matchedCondition.StatusCode.Value;
                    }
                    
                    if (matchedCondition.Response != null)
                    {
                        responseContent = matchedCondition.Response is string str 
                            ? str 
                            : JsonConvert.SerializeObject(matchedCondition.Response, Formatting.Indented);
                    }
                    else
                    {
                        responseContent = "{}";
                    }
                }
                else if (mockDef.IsDynamic && !string.IsNullOrEmpty(mockDef.DynamicTemplate))
                {
                    // Process dynamic template
                    responseContent = ProcessDynamicTemplate(mockDef.DynamicTemplate, request);
                }
                else
                {
                    // Use static response
                    responseContent = mockDef.GetResponseAsString();
                }
                
                // Apply environment variables and path parameters to the response content
                responseContent = ApplyTemplateVariables(responseContent, pathParams);
                
                byte[] buffer = Encoding.UTF8.GetBytes(responseContent);
                response.ContentLength64 = buffer.Length;
                
                try
                {
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    
                    if (_verbose)
                    {
                        ConsoleHelper.WriteSuccess($"Responded to {method} {requestUrl} with status {response.StatusCode}");
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - {method} {requestUrl} - {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    if (_verbose)
                    {
                        ConsoleHelper.WriteError($"Error sending response: {ex.Message}");
                    }
                }
                finally
                {
                    response.Close();
                }
            }
            else
            {
                // No matching mock definition found
                response.StatusCode = 404;
                response.ContentType = "application/json";
                
                // Combine both legacy and advanced mock endpoints in the error message
                var availableEndpoints = new List<object>();
                
                availableEndpoints.AddRange(_mockDefinitions.Select(m => new { 
                    method = m.Method, 
                    endpoint = m.Endpoint,
                    name = m.Name,
                    type = "legacy"
                }));
                
                availableEndpoints.AddRange(_advancedMockDefinitions.Select(m => new { 
                    method = m.Method, 
                    endpoint = m.Endpoint,
                    name = m.Name,
                    type = "advanced"
                }));
                
                string notFoundResponse = JsonConvert.SerializeObject(new
                {
                    error = "Not Found",
                    message = $"No mock defined for {method} {requestUrl}",
                    availableEndpoints = availableEndpoints
                }, Formatting.Indented);
                
                byte[] buffer = Encoding.UTF8.GetBytes(notFoundResponse);
                response.ContentLength64 = buffer.Length;
                
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                ConsoleHelper.WriteWarning($"No mock found for {method} {requestUrl}");
                
                response.Close();
            }
        }
        
        private MockApiDefinition? FindMatchingMockDefinition(HttpListenerRequest request)
        {
            string requestUrl = request.Url?.AbsolutePath ?? string.Empty;
            string method = request.HttpMethod;
            
            // First try exact match
            var exactMatch = _mockDefinitions.FirstOrDefault(m => 
                string.Equals(m.Endpoint, requestUrl, StringComparison.OrdinalIgnoreCase) && 
                string.Equals(m.Method, method, StringComparison.OrdinalIgnoreCase));
                
            if (exactMatch != null)
                return exactMatch;
                
            // Try wildcard/pattern match (for path parameters like /users/{id})
            foreach (var mockDef in _mockDefinitions)
            {
                if (!string.Equals(mockDef.Method, method, StringComparison.OrdinalIgnoreCase))
                    continue;
                    
                // Convert endpoint pattern to regex
                // Example: "/users/{id}" becomes "^/users/[^/]+$"
                if (mockDef.Endpoint.Contains("{") && mockDef.Endpoint.Contains("}"))
                {
                    string pattern = "^" + Regex.Escape(mockDef.Endpoint)
                        .Replace("\\{", "{")
                        .Replace("\\}", "}")
                        .Replace("{[^/]+}", "[^/]+") + "$";
                        
                    // Convert parameters like {id} or {name} to regex capture groups
                    pattern = Regex.Replace(pattern, "{([^/]+)}", "([^/]+)");
                    
                    if (Regex.IsMatch(requestUrl, pattern))
                    {
                        return mockDef;
                    }
                }
            }
            
            return null;
        }
        
        private MockCondition? FindMatchingCondition(MockApiDefinition mockDef, HttpListenerRequest request)
        {
            if (mockDef.Conditions == null || mockDef.Conditions.Count == 0)
                return null;
                
            foreach (var condition in mockDef.Conditions)
            {
                bool matches = true;
                
                // Check query parameters
                if (condition.QueryParams != null && condition.QueryParams.Count > 0)
                {
                    foreach (var param in condition.QueryParams)
                    {
                        if (request.QueryString[param.Key] != param.Value)
                        {
                            matches = false;
                            break;
                        }
                    }
                    
                    if (!matches) continue;
                }
                
                // Check for exact header matches
                if (condition.Headers != null && condition.Headers.Count > 0)
                {
                    foreach (var header in condition.Headers)
                    {
                        if (request.Headers[header.Key] != header.Value)
                        {
                            matches = false;
                            break;
                        }
                    }
                    
                    if (!matches) continue;
                }
                
                // Check for header value contains 
                if (condition.HeadersContain != null && condition.HeadersContain.Count > 0)
                {
                    foreach (var header in condition.HeadersContain)
                    {
                        string? headerValue = request.Headers[header.Key];
                        if (string.IsNullOrEmpty(headerValue) || !headerValue.Contains(header.Value))
                        {
                            matches = false;
                            break;
                        }
                    }
                    
                    if (!matches) continue;
                }
                
                // Check if specified headers exist
                if (condition.HeaderExists != null && condition.HeaderExists.Count > 0)
                {
                    foreach (var headerName in condition.HeaderExists)
                    {
                        if (string.IsNullOrEmpty(request.Headers[headerName]))
                        {
                            matches = false;
                            break;
                        }
                    }
                    
                    if (!matches) continue;
                }
                
                // Check for body content if applicable
                if (condition.Body != null || condition.BodyContains != null || condition.BodyMatches != null)
                {
                    // Try to read request body (if it has one)
                    string requestBody = string.Empty;
                    
                    try
                    {
                        if (request.ContentLength64 > 0 && request.HasEntityBody)
                        {
                            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                            {
                                requestBody = reader.ReadToEnd();
                            }
                            
                            // Reset the stream position for further reads
                            request.InputStream.Position = 0;
                        }
                        
                        // Check for exact body match
                        if (condition.Body != null)
                        {
                            string expectedBody = condition.Body is string strBody 
                                ? strBody 
                                : JsonConvert.SerializeObject(condition.Body);
                                
                            if (!string.Equals(requestBody, expectedBody, StringComparison.OrdinalIgnoreCase))
                            {
                                matches = false;
                            }
                            
                            if (!matches) continue;
                        }
                        
                        // Check if body contains specific strings
                        if (condition.BodyContains != null && condition.BodyContains.Count > 0)
                        {
                            foreach (var fragment in condition.BodyContains)
                            {
                                if (!requestBody.Contains(fragment))
                                {
                                    matches = false;
                                    break;
                                }
                            }
                            
                            if (!matches) continue;
                        }
                        
                        // Check if body matches specific patterns (property values in JSON)
                        if (condition.BodyMatches != null && condition.BodyMatches.Count > 0 &&
                            !string.IsNullOrEmpty(requestBody) && IsJsonObject(requestBody))
                        {
                            try
                            {
                                var bodyObject = JsonConvert.DeserializeObject<JObject>(requestBody);
                                
                                foreach (var match in condition.BodyMatches)
                                {
                                    // Use JPath-like expressions to find values
                                    var token = bodyObject?.SelectToken(match.Key);
                                    
                                    if (token == null || !token.ToString().Equals(match.Value))
                                    {
                                        matches = false;
                                        break;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                if (_verbose)
                                {
                                    ConsoleHelper.WriteWarning($"Failed to parse JSON body for matching: {ex.Message}");
                                }
                                matches = false;
                            }
                            
                            if (!matches) continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_verbose)
                        {
                            ConsoleHelper.WriteWarning($"Error reading request body: {ex.Message}");
                        }
                        matches = false;
                        continue;
                    }
                }
                
                if (matches)
                    return condition;
            }
            
            return null;
        }
        
        private bool IsJsonObject(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
                
            text = text.Trim();
            return text.StartsWith("{") && text.EndsWith("}");
        }
        
        private AdvancedMockApiDefinition? FindMatchingAdvancedMockDefinition(HttpListenerRequest request)
        {
            string requestUrl = request.Url?.AbsolutePath ?? string.Empty;
            string method = request.HttpMethod;
            
            // First try exact match
            var exactMatch = _advancedMockDefinitions.FirstOrDefault(m => 
                string.Equals(m.Endpoint, requestUrl, StringComparison.OrdinalIgnoreCase) && 
                string.Equals(m.Method, method, StringComparison.OrdinalIgnoreCase));
                
            if (exactMatch != null)
                return exactMatch;
                
            // Try wildcard/pattern match (for path parameters like /users/{id})
            foreach (var mockDef in _advancedMockDefinitions)
            {
                if (!string.Equals(mockDef.Method, method, StringComparison.OrdinalIgnoreCase))
                    continue;
                    
                // Convert endpoint pattern to regex
                // Example: "/users/{id}" becomes "^/users/[^/]+$"
                if (mockDef.Endpoint.Contains("{") && mockDef.Endpoint.Contains("}"))
                {
                    string pattern = "^" + Regex.Escape(mockDef.Endpoint)
                        .Replace("\\{", "{")
                        .Replace("\\}", "}")
                        .Replace("{[^/]+}", "[^/]+") + "$";
                        
                    // Convert parameters like {id} or {name} to regex capture groups
                    pattern = Regex.Replace(pattern, "{([^/]+)}", "([^/]+)");
                    
                    if (Regex.IsMatch(requestUrl, pattern))
                    {
                        return mockDef;
                    }
                }
            }
            
            return null;
        }
        
        private async Task ProcessAdvancedMockResponseAsync(HttpListenerContext context, AdvancedMockApiDefinition mockDef, Dictionary<string, string> pathParams)
        {
            var request = context.Request;
            var response = context.Response;
            string requestUrl = request.Url?.AbsolutePath ?? string.Empty;
            string method = request.HttpMethod;
            
            // Extract path parameters for use in templates
            ExtractPathParameters(mockDef.Endpoint, requestUrl, out pathParams);
            
            // Get Headers
            Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (request.Headers.AllKeys != null)
            {
                foreach (string? key in request.Headers.AllKeys)
                {
                    if (!string.IsNullOrEmpty(key) && request.Headers[key] != null)
                    {
                        string headerValue = request.Headers[key] ?? string.Empty;
                        headers[key] = headerValue;
                    }
                }
            }
            
            // Get Query Parameters
            Dictionary<string, string> queryParams = new Dictionary<string, string>();
            if (request.QueryString.Keys != null)
            {
                foreach (string? key in request.QueryString.Keys)
                {
                    if (key != null && request.QueryString[key] != null)
                    {
                        string queryValue = request.QueryString[key] ?? string.Empty;
                        queryParams[key] = queryValue;
                    }
                }
            }
            
            // Get Body as JToken (null if not a valid JSON)
            JToken? bodyContent = null;
            string bodyString = string.Empty;
            
            if (request.HasEntityBody && request.ContentLength64 > 0)
            {
                try
                {
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding, leaveOpen: true))
                    {
                        bodyString = reader.ReadToEnd();
                    }
                    
                    // Reset stream position for further reads
                    request.InputStream.Position = 0;
                    
                    if (IsJsonObject(bodyString) || bodyString.Trim().StartsWith("["))
                    {
                        bodyContent = JsonConvert.DeserializeObject<JToken>(bodyString);
                    }
                }
                catch
                {
                    // If we can't parse as JSON, we'll use empty object 
                    bodyContent = JToken.Parse("{}");
                }
            }
            else
            {
                bodyContent = JToken.Parse("{}");
            }
            
            // Find the matching response based on conditions
            ConditionalResponse? matchedResponse = null;
            
            foreach (var responseOption in mockDef.Responses)
            {
                bool conditionMet = _conditionEvaluator.EvaluateCondition(
                    responseOption.Condition, 
                    headers, 
                    bodyContent ?? JToken.Parse("{}"), 
                    queryParams,
                    pathParams);
                    
                if (conditionMet)
                {
                    matchedResponse = responseOption;
                    break;
                }
            }
            
            // Use fallback response if none of the conditions matched
            if (matchedResponse == null)
            {
                matchedResponse = mockDef.Responses.FirstOrDefault(r => 
                    string.Equals(r.Condition, "default", StringComparison.OrdinalIgnoreCase));
            }
            
            if (matchedResponse == null)
            {
                if (_verbose)
                {
                    ConsoleHelper.WriteWarning($"No matching response found for {method} {requestUrl} and no default response.");
                }
                
                // Return 500 error if no matching response
                response.StatusCode = 500;
                response.ContentType = "application/json";
                
                string errorContent = JsonConvert.SerializeObject(new
                {
                    error = "No Response Match",
                    message = "No condition matched the request and no default response was defined"
                }, Formatting.Indented);
                
                byte[] errorBuffer = Encoding.UTF8.GetBytes(errorContent);
                response.ContentLength64 = errorBuffer.Length;
                await response.OutputStream.WriteAsync(errorBuffer);
                response.Close();
                return;
            }
            
            // Process the matched response
            response.StatusCode = matchedResponse.StatusCode;
            response.ContentType = "application/json"; // Default
            
            // Add headers
            if (matchedResponse.Headers != null)
            {
                foreach (var header in matchedResponse.Headers)
                {
                    string headerValue = ApplyTemplateVariables(header.Value, pathParams);
                    response.Headers.Add(header.Key, headerValue);
                    
                    // Set content type if it's in the headers
                    if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        response.ContentType = headerValue;
                    }
                }
            }
            
            // Process response template
            string responseContent;
            
            if (matchedResponse.ResponseTemplate != null)
            {
                if (matchedResponse.ResponseTemplate is string str)
                {
                    responseContent = str;
                }
                else
                {
                    responseContent = JsonConvert.SerializeObject(matchedResponse.ResponseTemplate, Formatting.Indented);
                }
                
                // Replace any template variables with actual values
                responseContent = ApplyTemplateVariables(responseContent, pathParams);
                
                // Apply advanced template replacements
                
                // 1. Replace body.X references with actual body values
                if (bodyContent != null && bodyContent.Type == JTokenType.Object)
                {
                    foreach (var prop in (JObject)bodyContent)
                    {
                        string placeholder = $"{{{{body.{prop.Key}}}}}";
                        if (responseContent.Contains(placeholder))
                        {
                            string replacement = prop.Value?.ToString() ?? string.Empty;
                            responseContent = responseContent.Replace(placeholder, replacement);
                        }
                    }
                }
                
                // 2. Process random and date placeholders
                responseContent = ProcessDynamicTemplate(responseContent, request);
            }
            else
            {
                responseContent = "{}";
            }
            
            byte[] responseBuffer = Encoding.UTF8.GetBytes(responseContent);
            response.ContentLength64 = responseBuffer.Length;
            
            try
            {
                await response.OutputStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                
                if (_verbose)
                {
                    ConsoleHelper.WriteSuccess($"Responded to {method} {requestUrl} with status {response.StatusCode} (Advanced Mock)");
                    ConsoleHelper.WriteInfo($"Response body: {responseContent}");
                }
                else
                {
                    Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - {method} {requestUrl} - {response.StatusCode} (Advanced)");
                }
            }
            catch (Exception ex)
            {
                if (_verbose)
                {
                    ConsoleHelper.WriteError($"Error sending response: {ex.Message}");
                }
            }
            finally
            {
                response.Close();
            }
        }
        
        private async Task ProcessMultipartFormDataAsync(HttpListenerRequest request, 
            Dictionary<string, string> formFields, 
            Dictionary<string, (string FileName, byte[] Data)> files)
        {
            // Get the boundary from the Content-Type header
            string boundary = GetBoundaryFromContentType(request.ContentType ?? string.Empty);
            if (string.IsNullOrEmpty(boundary))
            {
                throw new ArgumentException("Content-Type header does not contain boundary");
            }
            
            using (var memoryStream = new MemoryStream())
            {
                // Copy the request stream to a memory stream so we can read it multiple times
                byte[] fileBuffer = new byte[4096];
                int bytesRead;
                
                while ((bytesRead = await request.InputStream.ReadAsync(fileBuffer, 0, fileBuffer.Length)) > 0)
                {
                    await memoryStream.WriteAsync(fileBuffer, 0, bytesRead);
                }
                
                // Reset the stream position so we can read from the beginning
                memoryStream.Position = 0;
                
                // Parse the multipart form data
                try
                {
                    string boundaryMarker = "--" + boundary;
                    string endBoundaryMarker = boundaryMarker + "--";
                    
                    using (var reader = new StreamReader(memoryStream, request.ContentEncoding))
                    {
                        string? line;
                        bool isFirstBoundary = true;
                        
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (isFirstBoundary && line.StartsWith(boundaryMarker))
                            {
                                isFirstBoundary = false;
                                continue;
                            }
                            
                            if (line.StartsWith(boundaryMarker))
                            {
                                if (line.Equals(endBoundaryMarker))
                                {
                                    // End of form data
                                    break;
                                }
                                
                                // Parse headers
                                string? contentDisposition = null;
                                string? contentType = null;
                                
                                while ((line = reader.ReadLine()) != null && !string.IsNullOrEmpty(line))
                                {
                                    if (line.StartsWith("Content-Disposition:", StringComparison.OrdinalIgnoreCase))
                                    {
                                        contentDisposition = line.Substring("Content-Disposition:".Length).Trim();
                                    }
                                    else if (line.StartsWith("Content-Type:", StringComparison.OrdinalIgnoreCase))
                                    {
                                        contentType = line.Substring("Content-Type:".Length).Trim();
                                    }
                                }
                                
                                // Parse content disposition to get name and filename
                                string? name = null;
                                string? filename = null;
                                
                                if (contentDisposition != null)
                                {
                                    var parts = contentDisposition.Split(';');
                                    foreach (var part in parts)
                                    {
                                        var trimmedPart = part.Trim();
                                        
                                        if (trimmedPart.StartsWith("name=", StringComparison.OrdinalIgnoreCase))
                                        {
                                            name = trimmedPart.Substring(5).Trim('"');
                                        }
                                        else if (trimmedPart.StartsWith("filename=", StringComparison.OrdinalIgnoreCase))
                                        {
                                            filename = trimmedPart.Substring(9).Trim('"');
                                        }
                                    }
                                }
                                
                                // Read content
                                var contentBuilder = new StringBuilder();
                                MemoryStream? fileData = null;
                                
                                if (!string.IsNullOrEmpty(filename))
                                {
                                    fileData = new MemoryStream();
                                }
                                
                                while ((line = reader.ReadLine()) != null)
                                {
                                    if (line.StartsWith(boundaryMarker))
                                    {
                                        break;
                                    }
                                    
                                    if (fileData != null)
                                    {
                                        byte[] lineBytes = request.ContentEncoding.GetBytes(line + "\r\n");
                                        await fileData.WriteAsync(lineBytes, 0, lineBytes.Length);
                                    }
                                    else
                                    {
                                        contentBuilder.AppendLine(line);
                                    }
                                }
                                
                                // Add to collection
                                if (!string.IsNullOrEmpty(name))
                                {
                                    if (fileData != null && !string.IsNullOrEmpty(filename))
                                    {
                                        files[name] = (filename, fileData.ToArray());
                                        fileData.Dispose();
                                    }
                                    else
                                    {
                                        formFields[name] = contentBuilder.ToString().Trim();
                                    }
                                }
                                
                                if (line?.Equals(endBoundaryMarker) == true)
                                {
                                    // End of form data
                                    break;
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (_verbose)
                    {
                        ConsoleHelper.WriteError($"Error parsing multipart form data: {ex.Message}");
                    }
                    throw;
                }
            }
        }
        
        private string GetBoundaryFromContentType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
                return string.Empty;
                
            int index = contentType.IndexOf("boundary=");
            if (index == -1)
                return string.Empty;
                
            string boundary = contentType.Substring(index + 9); // 9 is the length of "boundary="
            
            if (boundary.StartsWith("\"") && boundary.EndsWith("\""))
                boundary = boundary.Substring(1, boundary.Length - 2);
                
            return boundary;
        }
        
        private string ProcessDynamicTemplate(string template, HttpListenerRequest request)
        {
            // Parse request parameters and populate template variables
            var templateVars = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            
            // Add URL path parameters
            if (request.Url != null)
            {
                string requestPath = request.Url.AbsolutePath;
                
                Dictionary<string, string> extractedParams = new Dictionary<string, string>();
                var mockDef = _mockDefinitions.FirstOrDefault(m => 
                    string.Equals(m.Method, request.HttpMethod, StringComparison.OrdinalIgnoreCase) &&
                    PatternMatchesPath(m.Endpoint, requestPath, out var tempParams) && 
                    (extractedParams = tempParams ?? new Dictionary<string, string>()) != null);
                    
                if (mockDef != null && extractedParams.Count > 0)
                {
                    foreach (var param in extractedParams)
                    {
                        templateVars[param.Key] = param.Value;
                    }
                }
                
                // Add query parameters
                foreach (string key in request.QueryString.Keys)
                {
                    if (key != null)
                    {
                        templateVars["query." + key] = request.QueryString[key];
                    }
                }
            }
            
            // Add headers
            foreach (string key in request.Headers.Keys)
            {
                if (key != null)
                {
                    templateVars["header." + key] = request.Headers[key];
                }
            }
            
            // Add request body (if any)
            if (request.HasEntityBody)
            {
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    string body = reader.ReadToEnd();
                    templateVars["body"] = body;
                    
                    // Try to parse as JSON
                    try
                    {
                        var jsonObj = JsonConvert.DeserializeObject<JObject>(body);
                        if (jsonObj != null)
                        {
                            foreach (var prop in jsonObj.Properties())
                            {
                                templateVars["body." + prop.Name] = prop.Value?.ToString();
                            }
                        }
                    }
                    catch
                    {
                        // If not JSON, just use the raw body
                    }
                }
            }
            
            // Generate random ID values
            templateVars["random.id"] = Guid.NewGuid().ToString();
            templateVars["random.number"] = new Random().Next(10000).ToString();
            templateVars["timestamp"] = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            
            // Replace template variables in the template
            string result = template;
            foreach (var kv in templateVars)
            {
                result = result.Replace($"{{{{{kv.Key}}}}}", kv.Value ?? string.Empty);
            }
            
            return result;
        }
        
        // Method to apply template variables to a string
        private string ApplyTemplateVariables(string input, Dictionary<string, string>? pathParams = null)
        {
            if (string.IsNullOrEmpty(input))
                return input;
            
            // Create combined dictionary with path parameters and environment variables
            var allVariables = new Dictionary<string, string>();
            
            // Add path parameters (highest priority)
            if (pathParams != null)
            {
                foreach (var param in pathParams)
                {
                    allVariables[param.Key] = param.Value;
                }
            }
            
            // Add random/built-in values
            allVariables["random.id"] = Guid.NewGuid().ToString();
            allVariables["random.number"] = new Random().Next(10000).ToString();
            allVariables["timestamp"] = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
            
            // Add environment variables (lowest priority, won't override path params)
            if (_environmentService.CurrentEnvironment != null && _environmentService.CurrentEnvironment.Variables != null)
            {
                foreach (var envVar in _environmentService.CurrentEnvironment.Variables)
                {
                    if (!allVariables.ContainsKey(envVar.Key))
                    {
                        allVariables[envVar.Key] = envVar.Value;
                    }
                }
            }
            
            // Use regex to find and replace {{variable}} patterns
            var variablePattern = new Regex(@"{{(.+?)}}", RegexOptions.Compiled);
            
            return variablePattern.Replace(input, match =>
            {
                var variableName = match.Groups[1].Value.Trim();
                if (allVariables.TryGetValue(variableName, out var value))
                {
                    return value;
                }
                return match.Value; // Keep the original {{variable}} if not found
            });
        }
        
        // Extract path parameters from a URL
        private bool ExtractPathParameters(string pattern, string path, out Dictionary<string, string> pathParams)
        {
            pathParams = new Dictionary<string, string>();
            
            if (!pattern.Contains("{") || !pattern.Contains("}"))
                return false;
                
            // Extract parameter names from pattern
            var paramNames = new List<string>();
            int paramIndex = 0;
            while ((paramIndex = pattern.IndexOf('{', paramIndex)) != -1)
            {
                int endIndex = pattern.IndexOf('}', paramIndex);
                if (endIndex == -1) break;
                
                string paramName = pattern.Substring(paramIndex + 1, endIndex - paramIndex - 1);
                paramNames.Add(paramName);
                paramIndex = endIndex + 1;
            }
            
            if (paramNames.Count == 0)
                return false;
                
            // Convert pattern to regex with named capture groups
            string regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\{", "{")
                .Replace("\\}", "}");
                
            for (int i = 0; i < paramNames.Count; i++)
            {
                regexPattern = regexPattern.Replace("{" + paramNames[i] + "}", "([^/]+)");
            }
            
            regexPattern += "$";
            
            // Match against path
            var match = Regex.Match(path, regexPattern);
            if (match.Success && match.Groups.Count > 1)
            {
                for (int i = 0; i < paramNames.Count; i++)
                {
                    if (i + 1 < match.Groups.Count)
                    {
                        pathParams[paramNames[i]] = match.Groups[i + 1].Value;
                    }
                }
                
                return pathParams.Count > 0;
            }
            
            return false;
        }
        
        private bool PatternMatchesPath(string pattern, string path, out Dictionary<string, string>? pathParams)
        {
            pathParams = null;
            
            if (!pattern.Contains("{") && !pattern.Contains("}"))
            {
                return string.Equals(pattern, path, StringComparison.OrdinalIgnoreCase);
            }
            
            // Extract parameter names from pattern
            var paramNames = new List<string>();
            int paramIndex = 0;
            while ((paramIndex = pattern.IndexOf('{', paramIndex)) != -1)
            {
                int endIndex = pattern.IndexOf('}', paramIndex);
                if (endIndex == -1) break;
                
                string paramName = pattern.Substring(paramIndex + 1, endIndex - paramIndex - 1);
                paramNames.Add(paramName);
                paramIndex = endIndex + 1;
            }
            
            // Convert pattern to regex
            string regexPattern = "^" + Regex.Escape(pattern)
                .Replace("\\{", "{")
                .Replace("\\}", "}");
                
            for (int i = 0; i < paramNames.Count; i++)
            {
                regexPattern = regexPattern.Replace("{" + paramNames[i] + "}", "([^/]+)");
            }
            
            regexPattern += "$";
            
            // Match against path
            var match = Regex.Match(path, regexPattern);
            if (match.Success)
            {
                pathParams = new Dictionary<string, string>();
                for (int i = 0; i < paramNames.Count; i++)
                {
                    pathParams[paramNames[i]] = match.Groups[i + 1].Value;
                }
                
                return true;
            }
            
            return false;
        }
    }
}