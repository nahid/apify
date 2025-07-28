using Apify.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apify.Services
{
    /// <summary>
    /// A service for evaluating condition expressions for mock server responses
    /// </summary>
    public class ConditionEvaluator
    {
        private readonly DynamicExpressionManager _dynamicExpression;

        public ConditionEvaluator()
        {
            _dynamicExpression = new DynamicExpressionManager();
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
                var headersString = JsonConvert.SerializeObject(headers);
                headersString = headersString.Replace("\"", "\\\"").Replace("'", "\\'"); // Escape single quotes for JS
                _dynamicExpression.SetPropertyToAppObject("headers", $"tryParseJson('{headersString}')");
                
                var bodyString = JsonConvert.SerializeObject(body);
                bodyString = bodyString.Replace("\"", "\\\"").Replace("'", "\\'"); // Escape single quotes for JS
                _dynamicExpression.SetPropertyToAppObject("body", $"tryParseJson('{bodyString}')");
                
                var queryParamsString = JsonConvert.SerializeObject(queryParams);
                queryParamsString = queryParamsString.Replace("\"", "\\\"").Replace("'", "\\'"); // Escape single quotes for JS
                _dynamicExpression.SetPropertyToAppObject("query", $"tryParseJson('{queryParamsString}')");
                
                var pathParamsString = JsonConvert.SerializeObject(pathParams);
                pathParamsString = pathParamsString.Replace("\"", "\\\"").Replace("'", "\\'"); // Escape single quotes for JS
                _dynamicExpression.SetPropertyToAppObject("path", $"tryParseJson('{pathParamsString}')");
                
                
                // // Add special accessor objects for query parameters and headers
                // // For accessing query parameters in a more natural way (q.parameter)
                // _dynamicExpression.GetInterpreter().SetValue("q", MiscHelper.DictionaryToExpandoObject(queryParams));
                //
                // // For accessing headers in a more natural way (h.header)
                // _dynamicExpression.GetInterpreter().SetValue("h", MiscHelper.DictionaryToExpandoObject(headers));
                // _dynamicExpression.GetInterpreter().SetValue("p", MiscHelper.DictionaryToExpandoObject(pathParams));

                // Evaluate the expression
                var result = _dynamicExpression.Compile<bool>(condition);
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