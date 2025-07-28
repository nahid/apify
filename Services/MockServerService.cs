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
        private readonly string _projectDirectory;
        private readonly List<MockDefinitionSchema> _mockSchemaDefinitions = [];
        private readonly ConfigService _configService;
        private readonly ConditionEvaluator _conditionEvaluator = new ConditionEvaluator();
        private bool _verbose;
        private bool _debug;
        private HttpListener? _listener;
        private bool _isRunning;
        private FileSystemWatcher? _watcher;
        private Timer? _reloadDebounceTimer;
        private readonly object _reloadLock = new object();
        private int _port;
        private readonly TaskCompletionSource<object> _exitSignal = new TaskCompletionSource<object>();

        public MockServerService(string projectDirectory, bool debug = false)
        {
            _projectDirectory = projectDirectory;
            
            if (_projectDirectory.StartsWith("~"))
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                _projectDirectory = Path.Combine(home, _projectDirectory.Substring(2)); // remove "~/" and combine
            }

            if (string.IsNullOrEmpty(projectDirectory))
            {
                _projectDirectory = Directory.GetCurrentDirectory();
            }
            
            _debug = debug;
            _configService = new ConfigService(debug);
            _configService.SetConfigFilePath(_projectDirectory);

        }

        public async Task StartAsync(int port, bool verbose, bool watch = false)
        {
            _verbose = verbose;
            _port = port;

            if (_port == 0)
            {
                _port = _configService.LoadConfiguration().MockServer?.Port ?? 1988;
            }

            if (watch)
            {
                // Handle Ctrl+C to gracefully exit
                Console.CancelKeyPress += (sender, eventArgs) => {
                    eventArgs.Cancel = true;
                    _exitSignal.TrySetResult(null!);
                };

                SetupWatcher();
                _ = StartServerAsync(); // Fire-and-forget the server task

                await _exitSignal.Task; // Wait for Ctrl+C
                Stop(); // Stop the server on exit
                ConsoleHelper.WriteInfo("Mock server shut down.");
            }
            else
            {
                await StartServerAsync(); // Original behavior for non-watch mode
            }
        }

        private async Task StartServerAsync()
        {
            // Load all mock definitions
            LoadMockSchemaDefinitions();

            if (_mockSchemaDefinitions.Count == 0)
            {
                if (_verbose)
                {
                    ConsoleHelper.WriteWarning("No mock API definitions found. Create .mock.json files in your .apify directory.");
                    ConsoleHelper.WriteInfo("Example path: .apify/users/all.mock.json");
                }
                // If watching, we don't return, as new files might be added.
                if (_watcher == null) return;
            }

            // Start HTTP listener
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http" + $"://*:{_port}/");

            try
            {
                _listener.Start();
                _isRunning = true;

                ConsoleHelper.WriteSuccess($"Mock API Server started on http://localhost:{_port}");
                ConsoleHelper.WriteInfo("Available endpoints:");

                // Display advanced mock endpoints
                foreach (var mock in _mockSchemaDefinitions)
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
                if (OperatingSystem.IsWindows() && _debug)
                {
                    Console.WriteLine();
                    ConsoleHelper.WriteInfo("This error commonly occurs on Windows when binding to HTTP ports without administrator privileges.");
                    Console.WriteLine();
                    ConsoleHelper.WriteInfo("To resolve this issue, you can:");
                    Console.WriteLine("1. Run your command prompt as Administrator");
                    Console.WriteLine("   Right-click on cmd/PowerShell and select 'Run as administrator'");
                    Console.WriteLine();
                    Console.WriteLine("2. Add a URL reservation (one-time setup, preferred solution):");
                    Console.WriteLine($"   Run this in an Administrator PowerShell: netsh http add urlacl url=https://+:{_port}/ user=Everyone");
                    Console.WriteLine();
                    Console.WriteLine("3. Try using a port number above 1024 (e.g., 8080):");
                    Console.WriteLine($"   dotnet run mock-server --port 8080");
                    Console.WriteLine();
                }

                if (_debug)
                {
                    Console.WriteLine(ex.StackTrace);
                }
            }
            catch (Exception ex)
            {
                if (_isRunning) // Only log if the error happened while the server was supposed to be running
                {
                    ConsoleHelper.WriteError($"Error with mock server: {ex.Message}");
                    if (_debug)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            }
        }

        private void SetupWatcher()
        {
            if (!Directory.Exists(_projectDirectory))
            {
                Directory.CreateDirectory(_projectDirectory);
            }

            _watcher = new FileSystemWatcher(_projectDirectory)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = "*.mock.json",
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                InternalBufferSize = 65536
            };

            _watcher.Changed += OnMockFileChanged;
            _watcher.Created += OnMockFileChanged;
            _watcher.Deleted += OnMockFileChanged;
            _watcher.Renamed += OnMockFileRenamed;

            ConsoleHelper.WriteInfo($"Watching for changes in '{Path.GetFullPath(_projectDirectory)}'...");
        }

        private void OnMockFileChanged(object sender, FileSystemEventArgs e)
        {
            ConsoleHelper.WriteInfo($"File change detected: {e.ChangeType} - {e.Name}");
            DebounceReload();
        }

        private void OnMockFileRenamed(object sender, RenamedEventArgs e)
        {
            ConsoleHelper.WriteInfo($"File renamed detected: {e.OldName} -> {e.Name}");
            DebounceReload();
        }

        private void DebounceReload()
        {
            lock (_reloadLock)
            {
                _reloadDebounceTimer?.Dispose();
                _reloadDebounceTimer = new Timer(ReloadServer, null, 200, Timeout.Infinite); // 200ms debounce
            }
        }

        private void ReloadServer(object? state)
        {
            lock (_reloadLock)
            {
                ConsoleHelper.WriteInfo("Reloading mock server...");
                Stop();

                // Give a moment for the port to be released
                Thread.Sleep(100);

                // The StartServerAsync needs to run on a separate thread to not block the timer thread
                Task.Run(StartServerAsync);
            }
        }
        
        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            try
            {
                _listener?.Stop();
            }
            catch (ObjectDisposedException) { /* Listener is already disposed, which is fine. */ }
            _listener?.Close();
            _listener = null;
            ConsoleHelper.WriteInfo("Mock server instance stopped.");
        }
        
        private void LoadMockSchemaDefinitions()
        {
            _mockSchemaDefinitions.Clear();
            if (!Directory.Exists(_projectDirectory))
            {
                Directory.CreateDirectory(_projectDirectory);
                if (_debug)
                {
                    ConsoleHelper.WriteDebug($"Mock API directory created: {_projectDirectory}");
                }
                return;
            }
            
            // Find all .mock.json files in the directory and subdirectories
            var mockFiles = Directory.GetFiles(_projectDirectory, "*.mock.json", SearchOption.AllDirectories);
            
            // Use a debug flag for detailed logs but show the count regardless
            if (_debug)
            {
                ConsoleHelper.WriteDebug($"Searching for mock API definition files in: {_projectDirectory}");
                Console.WriteLine($"Found {mockFiles.Length} mock API definition files");
            }
            
            foreach (var file in mockFiles)
            {
                try
                {
                    var json = File.ReadAllText(file);

                    if (_debug)
                    {
                        ConsoleHelper.WriteInfo($"Attempting to load mock from: {file}");
                    }

                    // First, try to parse as an advanced mock definition
                    MockDefinitionSchema mockDefinitionDef;
                    
                    mockDefinitionDef = JsonConvert.DeserializeObject<MockDefinitionSchema>(json) ??
                                      new MockDefinitionSchema();

                    if (_debug)
                    {
                        ConsoleHelper.WriteInfo($"Successfully parsed {file} as MockSchema");
                    }

                    if (mockDefinitionDef.Responses.Count > 0)
                    {
                        _mockSchemaDefinitions.Add(mockDefinitionDef);

                        // Always show loaded API info
                        ConsoleHelper.WriteInfo(
                            $"Loaded mock API: {mockDefinitionDef.Name} [{mockDefinitionDef.Method}] {mockDefinitionDef.Endpoint}");
                    }

                }
                catch (FileLoadException ex)
                {
                    ConsoleHelper.WriteError($"Error loading mock definition from {file}: {ex.Message}");

                    if (_debug)
                    {
                        Console.WriteLine(ex.StackTrace);
                    }
                }
                catch (Exception ex)
                {
                    ConsoleHelper.WriteError($"Error parsing {file} as MockSchema: {ex.Message}");
                    if (_debug)
                    {
                        Console.WriteLine(ex.StackTrace);
                        if (ex.InnerException != null)
                        {
                            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                        }
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
            
            Console.WriteLine($"Received request: {method} {requestUrl}");
                
            if (_debug)
            {
                foreach (var headerKey in request.Headers.AllKeys)
                {
                    ConsoleHelper.WriteInfo($"  Header: {headerKey}: {request.Headers[headerKey]}");
                }
            }
            
            // Look for advanced mock definition first
            var mockDefinition = FindMatchingMockDefinition(request);
       
            
            if (mockDefinition != null)
            {
                await ProcessMockResponseAsync(context, mockDefinition);
                return;
            }
 
            // No matching mock definition found
            response.StatusCode = 404;
            response.ContentType = "application/json";
            
            // Combine both legacy and advanced mock endpoints in the error message
            var availableEndpoints = new List<object>();
            
            availableEndpoints.AddRange(_mockSchemaDefinitions.Select(m => new { 
                method = m.Method, 
                endpoint = m.Endpoint,
                name = m.Name,
                type = "advanced"
            }));
            
            string notFoundResponse = JsonConvert.SerializeObject(new
            {
                error = "Not Found",
                message = $"No mock defined for {method} {requestUrl}",
                availableEndpoints
            }, Formatting.Indented);
            
            byte[] buffer = Encoding.UTF8.GetBytes(notFoundResponse);
            response.ContentLength64 = buffer.Length;
            
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            ConsoleHelper.WriteWarning($"No mock found for {method} {requestUrl}");
            
            response.Close();
        
        }
        
        private bool IsJsonObject(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;
                
            text = text.Trim();
            return text.StartsWith("{") && text.EndsWith("}");
        }
        
        private MockDefinitionSchema? FindMatchingMockDefinition(HttpListenerRequest request)
        {
            string requestUrl = request.Url?.AbsolutePath ?? string.Empty;
            string method = request.HttpMethod;
            
            // First try exact match
            var exactMatch = _mockSchemaDefinitions.FirstOrDefault(m => 
                string.Equals(m.Endpoint, requestUrl, StringComparison.OrdinalIgnoreCase) && 
                string.Equals(m.Method, method, StringComparison.OrdinalIgnoreCase));
                
            if (exactMatch != null)
                return exactMatch;
                
            // Try wildcard/pattern match (for path parameters like /users/{id})
            foreach (var mockDef in _mockSchemaDefinitions)
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
        
        private async Task ProcessMockResponseAsync(HttpListenerContext context, MockDefinitionSchema mockDefinitionDef)
        {
            var request = context.Request;
            var response = context.Response;
            string requestUrl = request.Url?.AbsolutePath ?? string.Empty;
            string method = request.HttpMethod;

            // Extract path parameters for use in templates
            ExtractPathParameters(mockDefinitionDef.Endpoint, requestUrl, out var pathParams);
            
            // Get Headers
            Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (request.Headers.AllKeys.Length > 0)
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
            if (request.Url?.Query is { Length: > 0 })
            {
                // Process the query string
                
                // Process query string manually to ensure correct handling
                string query = request.Url.Query.TrimStart('?');
                string[] pairs = query.Split('&');
                
                foreach (string pair in pairs)
                {
                    string[] keyValue = pair.Split('=');
                    if (keyValue.Length >= 2)
                    {
                        string key = keyValue[0];
                        string value = keyValue[1];
                        queryParams[key] = Uri.UnescapeDataString(value);

                    }
                }
            }
            
            // Get Body as JToken (null if not a valid JSON)
            JToken? bodyContent = null;
          
            
            if (request.HasEntityBody && request.ContentLength64 > 0)
            {
                string bodyString;
                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        request.InputStream.CopyTo(memoryStream);
                        memoryStream.Position = 0; // rewind for reading

                        using (var reader = new StreamReader(memoryStream, request.ContentEncoding, leaveOpen: true))
                        {
                            bodyString = reader.ReadToEnd();
                        }

                        memoryStream.Position = 0; // rewind again if needed later

                        if (IsJsonObject(bodyString) || bodyString.Trim().StartsWith("["))
                        {
                            bodyContent = JsonConvert.DeserializeObject<JToken>(bodyString);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error reading JSON body: {e.Message}");
                    bodyContent = JsonConvert.DeserializeObject<JToken>("{}");
                }
            }
            else
            {
                bodyContent = JsonConvert.DeserializeObject<JToken>("{}");
            }
            
            var envVars = _configService.GetDefaultEnvironment()?.Variables ?? [];
            
            var mockDefString = JsonConvert.SerializeObject(mockDefinitionDef, Formatting.Indented);
            
            mockDefString = StubManager.Replace(mockDefString, new Dictionary<string, object>
            {
                {"env", envVars},
                {"headers", headers},
                {"params", pathParams},
                {"query", queryParams},
                {"body", bodyContent ?? new JObject()}
            });
            
            mockDefinitionDef = JsonConvert.DeserializeObject<MockDefinitionSchema>(mockDefString) ??
                                      new MockDefinitionSchema();
            
            // First process the responses in two groups: defaults and non-defaults
            var defaultResponses = new List<ConditionalResponse>();
            var regularResponses = new List<ConditionalResponse>();
            
            // Sort the responses into appropriate lists
            foreach (var resp in mockDefinitionDef.Responses)
            {
                if (_conditionEvaluator.IsDefaultCondition(resp.Condition))
                {
                    defaultResponses.Add(resp);
                }
                else
                {
                    regularResponses.Add(resp);
                }
            }
            
            if (_debug)
            {
                Console.WriteLine($"DEBUG: Found {regularResponses.Count} regular condition responses and {defaultResponses.Count} default responses.");
            }
            
            // First try to match any regular (non-default) condition
            ConditionalResponse? matchedResponse = null;
            
            foreach (var resp in regularResponses)
            {
                bool conditionMet = _conditionEvaluator.EvaluateCondition(
                    resp.Condition, 
                    headers, 
                    bodyContent ?? new JObject(),
                    queryParams,
                    pathParams);
                    
                if (conditionMet)
                {
                    matchedResponse = resp;
                    if (_debug)
                    {
                        Console.WriteLine($"DEBUG: Matched condition: '{resp.Condition}'");
                    }
                    break;
                }
            }
            
            // If no regular condition matched, use the first default response if available
            if (matchedResponse == null)
            {
                // Debug - log all available conditions
                if (_debug)
                {
                    Console.WriteLine($"DEBUG: No regular conditions matched. Looking for default response.");
                }

                if (defaultResponses.Count > 0)
                {
                    matchedResponse = defaultResponses[0]; // Take the first default response
                    
                    // Write additional debug info
                    if (_debug)
                    {
                        Console.WriteLine($"DEBUG: Using default response with condition '{matchedResponse.Condition}'");
                    }
                }
                else
                {
                    // If no explicit default, use the last response as fallback
                    matchedResponse = mockDefinitionDef.Responses.LastOrDefault();
                    if (matchedResponse != null && _debug)
                    {
                        Console.WriteLine($"DEBUG: No explicit default found, using last response with condition '{matchedResponse.Condition}' as fallback");
                    }
                }
            }
            
            if (matchedResponse == null)
            {
                ConsoleHelper.WriteWarning($"No matching response found for {method} {requestUrl} and no default response.");
                
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

            if (_configService.LoadConfiguration().MockServer?.DefaultHeaders != null)
            {
                foreach (var header in _configService.LoadConfiguration().MockServer?.DefaultHeaders ?? [])
                {
                    response.Headers.Add(header.Key, header.Value);
                    
                    // Set the content type if it's in the headers
                    if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        response.ContentType = header.Value;
                    }
                }
            }
            // Add headers
            if (matchedResponse.Headers.Count > 0)
            {
                foreach (var header in matchedResponse.Headers)
                {
                    
                    
                    /*string headerValue = StubManager.Replace(header.Value, new Dictionary<string, object>
                    {
                        {"env", envVars},
                        {"headers", headers},
                        {"params", pathParams},
                        {"query", queryParams},
                        {"body", bodyContent ?? new JObject()}
                    });*/
                    
                    response.Headers.Add(header.Key, header.Value);
                    
                    // Set content type if it's in the headers
                    if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                         response.ContentType = header.Value;
                    }
                }
            }
            
            // Process response template
            string responseContent;
            
         
                if (matchedResponse.ResponseTemplate is string str)
                {
                    responseContent = str;
                }
                else
                {
                    responseContent = JsonConvert.SerializeObject(matchedResponse.ResponseTemplate, Formatting.Indented);
                }

                
                // Replace any template variables with actual values
                // responseContent = ApplyTemplateVariables(responseContent, pathParams);
                /*responseContent = StubManager.Replace(responseContent, new Dictionary<string, object>
                {
                    {"env", envVars},
                    {"headers", headers},
                    {"path", pathParams},
                    {"query", queryParams},
                    {"body", bodyContent ?? new JObject()}
                });*/
                
                // Process dynamic template expressions (e.g., {{$random:int:1000:1999}})
                // responseContent = ProcessDynamicTemplate(responseContent, request);
                
                // Apply advanced template replacements
                
                // 1. Replace body.X references with actual body values
            
            
            byte[] responseBuffer = Encoding.UTF8.GetBytes(responseContent);
            response.ContentLength64 = responseBuffer.Length;
            
            try
            {
                await response.OutputStream.WriteAsync(responseBuffer, 0, responseBuffer.Length);
                
                // Always show basic response info
                Console.WriteLine($"{DateTime.Now.ToString("HH:mm:ss")} - {method} {requestUrl} - {response.StatusCode} (Advanced)");
                
                // Show detailed info only in debug mode
                if (_debug)
                {
                    ConsoleHelper.WriteSuccess($"Responded to {method} {requestUrl} with status {response.StatusCode} (Advanced Mock)");
                    ConsoleHelper.WriteInfo($"Response body: {responseContent}");
                }
            }
            catch (Exception ex)
            {
                // Always show basic error
                ConsoleHelper.WriteError($"Error sending response: {ex.Message}");
                
                // Show detailed stack trace only in debug mode
                if (_debug)
                {
                    Console.WriteLine(ex.StackTrace);
                }
            }
            finally
            {
                response.Close();
            }
        }
        
        // Extract path parameters from a URL
        private void ExtractPathParameters(string pattern, string path, out Dictionary<string, string> pathParams)
        {
            pathParams = new Dictionary<string, string>();
            
            if (!pattern.Contains("{") || !pattern.Contains("}"))
                return;

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
                return;

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

            }

        }
    }
}