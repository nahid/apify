using Apify.Models;

namespace Apify.Services
{
    public class TestRunner
    {
        private readonly AssertionEvaluator _assertionEvaluator;

        public TestRunner()
        {
            _assertionEvaluator = new AssertionEvaluator();
        }

        public Task<List<TestResult>> RunTestsAsync(ApiDefinition apiDefinition, ApiResponse response)
        {
            var results = new List<TestResult>();

            if (apiDefinition.Tests == null || apiDefinition.Tests.Count == 0)
            {
                // No tests defined, add a default status code check
                var defaultTest = new TestAssertion
                {
                    Name = "Status code is 2xx (Success)",
                    Assertion = "status >= 200 && status < 300"
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
    }
}
