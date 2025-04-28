using Apify.Models;
using Apify.Utils;

namespace Apify.Services
{
    public class TestRunner
    {
        private readonly AssertionEvaluator _assertionEvaluator;
        private ApiExecutor _apiExecutor;

        public TestRunner(ApiExecutor apiExecutor)
        {
            _assertionEvaluator = new AssertionEvaluator();
            _apiExecutor = apiExecutor;
        }

        // Constructor for backward compatibility
        public TestRunner()
        {
            _assertionEvaluator = new AssertionEvaluator();
            _apiExecutor = new ApiExecutor();
        }
        
        public void SetApiExecutor(ApiExecutor apiExecutor)
        {
            _apiExecutor = apiExecutor;
        }

        public Task<List<TestResult>> RunTestsAsync(ApiDefinition apiDefinition, ApiResponse response)
        {
            var results = new List<TestResult>();

            if (apiDefinition.Tests == null || apiDefinition.Tests.Count == 0)
            {
                // No tests defined, add a default status code check
                var defaultTest = new TestAssertion
                {
                    Name = "Status code should be 2xx (Success)",
                    AssertType = "StatusCode",
                    ExpectedValue = "200-299"
                };
                
                var result = _assertionEvaluator.EvaluateAssertion(defaultTest, response);
                results.Add(result);
                return Task.FromResult(results);
            }

            foreach (var test in apiDefinition.Tests)
            {
                var result = _assertionEvaluator.EvaluateAssertion(test, response);
                results.Add(result);
            }

            return Task.FromResult(results);
        }

        /// <summary>
        /// Runs an API test and returns a comprehensive result object
        /// </summary>
        /// <param name="apiDefinition">The API definition to test</param>
        /// <param name="verbose">Whether to display detailed output</param>
        /// <returns>A comprehensive test result object</returns>
        public async Task<ApiTestResult?> RunApiTestAsync(ApiDefinition apiDefinition, bool verbose = false)
        {
            try
            {
                // Execute the API request
                var response = await _apiExecutor.ExecuteRequestAsync(apiDefinition);
                
                if (response == null)
                {
                    if (verbose)
                    {
                        ConsoleHelper.WriteError("Failed to execute API request");
                    }
                    return null;
                }
                
                // Run the tests
                var testResults = await RunTestsAsync(apiDefinition, response);
                
                // Only show the test results
                if (!verbose)
                {
                    Console.Write("  ");
                    foreach (var result in testResults)
                    {
                        if (result.Success)
                        {
                            ConsoleHelper.WriteColored("✓", ConsoleColor.Green);
                        }
                        else
                        {
                            ConsoleHelper.WriteColored("✗", ConsoleColor.Red);
                        }
                        Console.Write(" ");
                    }
                    Console.WriteLine();
                }
                else
                {
                    // Display detailed test results
                    Console.WriteLine("Test Results:");
                    foreach (var result in testResults)
                    {
                        if (result.Success)
                        {
                            ConsoleHelper.WriteSuccess($"✓ {result.Name}");
                        }
                        else
                        {
                            ConsoleHelper.WriteError($"✗ {result.Name}");
                            ConsoleHelper.WriteError($"  Error: {result.ErrorMessage}");
                        }
                    }

                    // Display summary
                    Console.WriteLine("==========================");
                    int passedCount = testResults.Count(r => r.Success);
                    ConsoleHelper.WriteInfo($"Test Summary: {passedCount}/{testResults.Count} tests passed");
                    Console.WriteLine("==========================");
                }
                
                return new ApiTestResult
                {
                    ApiName = apiDefinition.Name,
                    Uri = apiDefinition.Uri,
                    Method = apiDefinition.Method,
                    StatusCode = response.StatusCode,
                    ResponseTimeMs = response.ResponseTimeMs,
                    AssertionResults = testResults
                };
            }
            catch (Exception ex)
            {
                if (verbose)
                {
                    ConsoleHelper.WriteError($"Error executing API test: {ex.Message}");
                }
                return null;
            }
        }
    }

    /// <summary>
    /// Comprehensive result of an API test including the API details and assertion results
    /// </summary>
    public class ApiTestResult
    {
        public string ApiName { get; set; } = string.Empty;
        public string Uri { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public int StatusCode { get; set; }
        public long ResponseTimeMs { get; set; }
        public List<TestResult> AssertionResults { get; set; } = new();
        
        public bool AllTestsPassed => AssertionResults.All(r => r.Success == true);
        public int PassedCount => AssertionResults.Count(r => r.Success == true);
        public int FailedCount => AssertionResults.Count(r => r.Success == false);
    }
}
