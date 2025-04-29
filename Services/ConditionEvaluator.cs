using System;
using System.Collections.Generic;
using System.Linq;
using DynamicExpresso;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            _interpreter = new Interpreter();
            
            // Register helper methods to be used in expressions
            _interpreter.SetFunction("int", new Func<string, int>(s => int.TryParse(s, out int result) ? result : 0));
            _interpreter.SetFunction("Parse", new Func<string, int>(s => int.TryParse(s, out int result) ? result : 0));
            _interpreter.SetFunction("ToLower", new Func<string, string>(s => s?.ToLower() ?? string.Empty));
            _interpreter.SetFunction("ToUpper", new Func<string, string>(s => s?.ToUpper() ?? string.Empty));
            _interpreter.SetFunction("Contains", new Func<string, string, bool>((source, value) => 
                source?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        /// <summary>
        /// Helper class for direct access to dictionary parameters
        /// </summary>
        private class QueryAccessor
        {
            private readonly Dictionary<string, string> _parameters;
            
            public QueryAccessor(Dictionary<string, string> parameters)
            {
                _parameters = parameters ?? new Dictionary<string, string>();
            }
            
            public string Get(string key)
            {
                if (_parameters.TryGetValue(key, out var value))
                {
                    return value;
                }
                
                return string.Empty;
            }
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
            // Special case for the default fallback
            if (string.Equals(condition, "default", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            
            // Log the query parameters for debugging
            Console.WriteLine($"Query parameters count: {queryParams.Count}");
            foreach (var param in queryParams)
            {
                Console.WriteLine($"Query parameter: {param.Key} = {param.Value}");
            }

            try
            {
                // Set up parameters for the expression
                _interpreter.SetVariable("headers", new DynamicObject(headers));
                _interpreter.SetVariable("body", new DynamicObject(body));
                _interpreter.SetVariable("query", new DynamicObject(queryParams));
                _interpreter.SetVariable("params", new DynamicObject(pathParams));
                
                // Add individual query parameters directly to the interpreter
                if (queryParams != null)
                {
                    foreach (var param in queryParams)
                    {
                        // Individual parameters
                        _interpreter.SetVariable(param.Key, param.Value);
                    }
                }
                
                // Add special accessor functions for query parameters and headers
                // For accessing query parameters in a more natural way
                _interpreter.SetFunction("q", new Func<string, string>(key => 
                {
                    if (queryParams != null && queryParams.TryGetValue(key, out var val))
                    {
                        Console.WriteLine($"q function accessed parameter '{key}' with value '{val}'");
                        return val;
                    }
                    return null;
                }));
                
                // For accessing headers in a more natural way
                _interpreter.SetFunction("h", new Func<string, string>(key => 
                {
                    if (headers != null && headers.TryGetValue(key, out var val))
                    {
                        Console.WriteLine($"h function accessed header '{key}' with value '{val}'");
                        return val;
                    }
                    return null;
                }));

                // Evaluate the expression
                var result = _interpreter.Eval<bool>(condition);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error evaluating condition '{condition}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// A dynamic wrapper class for objects to allow property access in expressions
        /// </summary>
        private class DynamicObject
        {
            private readonly object _value;

            public DynamicObject(object value)
            {
                _value = value;
            }

            public bool ContainsKey(string key)
            {
                switch (_value)
                {
                    case Dictionary<string, string> dict:
                        return dict.ContainsKey(key);
                    case JObject jObj:
                        return jObj.ContainsKey(key);
                    default:
                        return false;
                }
            }

            public dynamic? GetValue(string key)
            {
                switch (_value)
                {
                    case Dictionary<string, string> dict:
                        return dict.TryGetValue(key, out var value) ? value : null;
                    case JObject jObj:
                        var prop = jObj[key];
                        return prop?.Type == JTokenType.Object || prop?.Type == JTokenType.Array
                            ? new DynamicObject(prop)
                            : prop?.ToObject<object>();
                    case JArray jArr when int.TryParse(key, out int index) && index >= 0 && index < jArr.Count:
                        var item = jArr[index];
                        return item?.Type == JTokenType.Object || item?.Type == JTokenType.Array
                            ? new DynamicObject(item)
                            : item?.ToObject<object>();
                    default:
                        return null;
                }
            }

            public override bool Equals(object? obj)
            {
                if (obj is DynamicObject other)
                {
                    return JsonConvert.SerializeObject(_value) == JsonConvert.SerializeObject(other._value);
                }
                return _value?.Equals(obj) ?? obj == null;
            }

            public override int GetHashCode()
            {
                return _value?.GetHashCode() ?? 0;
            }

            // Dynamic operator overloads for common operations
            public static bool operator ==(DynamicObject left, object right)
            {
                return left?.Equals(right) ?? right == null;
            }

            public static bool operator !=(DynamicObject left, object right)
            {
                return !(left == right);
            }

            public static bool operator !(DynamicObject obj)
            {
                if (obj._value == null) return true;
                if (obj._value is bool b) return !b;
                if (obj._value is string s) return string.IsNullOrEmpty(s);
                if (obj._value is JValue jv && jv.Type == JTokenType.Null) return true;
                return false;
            }

            // Dynamic property accessor via indexer
            public dynamic this[string key]
            {
                get
                {
                    var value = GetValue(key);
                    return value ?? new DynamicObject(new object()); // Return DynamicObject with empty object instead of null
                }
            }

            // ToString for debugging
            public override string ToString()
            {
                return JsonConvert.SerializeObject(_value);
            }
        }
    }
}