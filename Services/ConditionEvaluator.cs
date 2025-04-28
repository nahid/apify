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
        }

        /// <summary>
        /// Evaluates a condition expression against the provided context
        /// </summary>
        /// <param name="condition">The condition expression to evaluate</param>
        /// <param name="headers">The request headers</param>
        /// <param name="body">The request body (parsed from JSON if applicable)</param>
        /// <param name="query">The query parameters</param>
        /// <param name="pathParams">Path parameters extracted from the URL</param>
        /// <returns>True if the condition is satisfied, false otherwise</returns>
        public bool EvaluateCondition(
            string condition,
            Dictionary<string, string> headers,
            JToken body,
            Dictionary<string, string> query,
            Dictionary<string, string> pathParams)
        {
            // Special case for the default fallback
            if (string.Equals(condition, "default", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            try
            {
                // Set up parameters for the expression
                _interpreter.SetVariable("headers", new DynamicObject(headers));
                _interpreter.SetVariable("body", new DynamicObject(body));
                _interpreter.SetVariable("query", new DynamicObject(query));
                _interpreter.SetVariable("params", new DynamicObject(pathParams));

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

            public dynamic GetValue(string key)
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

            public override bool Equals(object obj)
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
                get => GetValue(key);
            }

            // ToString for debugging
            public override string ToString()
            {
                return JsonConvert.SerializeObject(_value);
            }
        }
    }
}