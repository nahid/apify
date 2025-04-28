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
        private readonly EnvironmentService _environmentService;
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
            
            if (_mockDefinitions.Count == 0)
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
                
                foreach (var mock in _mockDefinitions)
                {
                    ConsoleHelper.WriteSuccess($"[{mock.Method}] {mock.Endpoint} - {mock.Name}");
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
            }
            
            // Find matching mock definition
            var mockDef = FindMatchingMockDefinition(request);
            Dictionary<string, string> pathParams = new Dictionary<string, string>();
            
            if (mockDef != null)
            {
                // Extract path parameters for use in templates
                string urlPath = request.Url?.AbsolutePath ?? string.Empty;
                ExtractPathParameters(mockDef.Endpoint, urlPath, out pathParams);
                
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
                
                string notFoundResponse = JsonConvert.SerializeObject(new
                {
                    error = "Not Found",
                    message = $"No mock defined for {method} {requestUrl}",
                    availableEndpoints = _mockDefinitions.Select(m => new { 
                        method = m.Method, 
                        endpoint = m.Endpoint,
                        name = m.Name
                    }).ToList()
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
                
                // Check headers
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
                
                // TODO: Add body matching if needed
                
                if (matches)
                    return condition;
            }
            
            return null;
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