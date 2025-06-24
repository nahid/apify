using Apify.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using DynamicExpresso;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Dynamic;

namespace Apify.Services
{
    /// <summary>
    /// A service for evaluating condition expressions for mock server responses
    /// </summary>
    public class ConditionEvaluator
    {
        private readonly Interpreter _interpreter;

        public ConditionEvaluator()
        {
            _interpreter = new Interpreter(InterpreterOptions.Default);
            
            // Register helper methods to be used in expressions
            _interpreter.SetFunction("int", new Func<string, int>(s => int.TryParse(s, out int result) ? result : 0));
            _interpreter.SetFunction("Parse", new Func<string, int>(s => int.TryParse(s, out int result) ? result : 0));
            _interpreter.SetFunction("ToLower", new Func<string, string>(s => s?.ToLower() ?? string.Empty));
            _interpreter.SetFunction("ToUpper", new Func<string, string>(s => s?.ToUpper() ?? string.Empty));
            _interpreter.SetFunction("Contains", new Func<string, string, bool>((source, value) => 
                source?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false));
        }
        
 
        /// <summary>
        /// Evaluates a condition expression against the provided context
        /// </summary>
        /// <param name="condition">The condition expression to evaluate</param>
        /// <param name="headers">The request headers</param>
        /// <param name="body">The request body (parsed from JSON if applicable)</param>
        /// <param name="queryParams">The query parameters</param>
        /// <param name="pathParams">Path parameters extracted from the URL</param>
        /// <returns>True if the condition is satisfied, false otherwise</returns>
        public bool EvaluateCondition(
            string condition,
            Dictionary<string, string> headers,
            JToken body,
            Dictionary<string, string> queryParams,
            Dictionary<string, string> pathParams)
        {

            try
            {
                // Set up parameters for the expression
                _interpreter.SetVariable("headers", headers);
                _interpreter.SetVariable("body", body);
                _interpreter.SetVariable("query", queryParams);
                _interpreter.SetVariable("params", pathParams);
                
                
                // Add special accessor objects for query parameters and headers
                // For accessing query parameters in a more natural way (q.parameter)
                _interpreter.SetVariable("q", MiscHelper.DictionaryToExpandoObject(queryParams ?? new Dictionary<string, string>()));
                
                // For accessing headers in a more natural way (h.header)
                _interpreter.SetVariable("h", MiscHelper.DictionaryToExpandoObject(headers ?? new Dictionary<string, string>()));
                _interpreter.SetVariable("p", MiscHelper.DictionaryToExpandoObject(pathParams ?? new Dictionary<string, string>()));

                // Evaluate the expression
                var result = _interpreter.Eval<bool>(condition);
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
        
        /// <summary>
        /// Checks if a condition is a default condition without evaluating it
        /// </summary>
        public bool IsDefaultCondition(string condition)
        {
            return string.IsNullOrEmpty(condition) || 
                string.Equals(condition, "default", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(condition, "true", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(condition, "1", StringComparison.OrdinalIgnoreCase);
        }
    }
}