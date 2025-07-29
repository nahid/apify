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
        private readonly DynamicScriptingManager _scriptMan;

        public ConditionEvaluator()
        {
            _scriptMan = new DynamicScriptingManager();
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
                var headersString = JsonHelper.SerializeWithEscapeSpecialChars(headers); 
                _scriptMan.SetPropertyToAppObject("headers", $"tryParseJson('{headersString}')");

                var bodyString = JsonHelper.SerializeWithEscapeSpecialChars(body);
                _scriptMan.SetPropertyToAppObject("body", $"tryParseJson('{bodyString}')");

                var queryParamsString = JsonHelper.SerializeWithEscapeSpecialChars(queryParams);
                _scriptMan.SetPropertyToAppObject("query", $"tryParseJson('{queryParamsString}')");

                var pathParamsString = JsonHelper.SerializeWithEscapeSpecialChars(pathParams);
                _scriptMan.SetPropertyToAppObject("path", $"tryParseJson('{pathParamsString}')");
                
                
                // // Add special accessor objects for query parameters and headers
                // // For accessing query parameters in a more natural way (q.parameter)
                // _scriptMan.GetInterpreter().SetValue("q", MiscHelper.DictionaryToExpandoObject(queryParams));
                //
                // // For accessing headers in a more natural way (h.header)
                // _scriptMan.GetInterpreter().SetValue("h", MiscHelper.DictionaryToExpandoObject(headers));
                // _scriptMan.GetInterpreter().SetValue("p", MiscHelper.DictionaryToExpandoObject(pathParams));

                // Evaluate the expression
                var result = _scriptMan.Compile<bool>(condition);
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