using Apify.Services;
using Bogus;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.Text.RegularExpressions;

namespace Apify.Utils;

public static class StubManager
{
    private static readonly Regex _placeholderRe = new Regex(@"\{\{\s*(.+?)\s*\}\}",
        RegexOptions.Compiled);

    private static DynamicExpressionManager _dynamicExpression;
    
    static StubManager()
    {
        _dynamicExpression = new DynamicExpressionManager();
        var faker = new Faker("en");
        _dynamicExpression.GetInterpreter().SetVariable("Faker", faker);
   
    }

    /// <summary>
    /// Replaces all {{path.to.value}} stubs in <paramref name="template"/> by
    /// looking up nested dictionaries in <paramref name="variables"/>.
    /// </summary>
    public static string Replace(
        string template,
        Dictionary<string, object> vars)
    {
        SetVariables(vars);
        
        return _placeholderRe.Replace(template, match =>
        {
            if (_dynamicExpression.IsEvalExpression(match.Groups[1].Value))
            {
                return ExecExpression(match.Groups[1].Value);
            }
            
            // split "users.posts.comment.id" → ["users","posts","comment","id"]
            var parts = match.Groups[1]
            .Value
            .Split('.', StringSplitOptions.RemoveEmptyEntries);

            return AccessNestedValue(match, vars, parts);

        });
    }
    
    private static string ExecExpression(string expr)
    {
        var expression = _dynamicExpression.GetExpression(expr);
        if (string.IsNullOrEmpty(expression))
        {
            return expr;
        }
        
        var result = _dynamicExpression.Compile(expression);
        if (string.IsNullOrEmpty(result))
        {
            return expr;
        }

        return result;
    }
    
    private static string AccessNestedValue(
        Match match,
        Dictionary<string, object> vars,
        string[] parts)
    {
        object current = vars;

        foreach (var part in parts)
        {
            switch (current)
            {
                case Dictionary<string, object> dictObj when dictObj.TryGetValue(part, out var nextObj):
                    current = nextObj;
                    break;

                case Dictionary<string, string> dictStr when dictStr.TryGetValue(part, out var nextStr):
                    current = nextStr;
                    break;

                case ExpandoObject expando:
                    var expandoDict = (IDictionary<string, object>)expando;
                    if (expandoDict.TryGetValue(part, out var nextExpando))
                    {
                        current = nextExpando;
                        break;
                    }
                    return match.Value; // not found

                case JToken jtoken:
                    var token = jtoken[part];
                    if (token != null)
                    {
                        current = token;
                        break;
                    }
                    return match.Value; // not found

                default:
                    return match.Value; // not found or not navigable
            }
        }

// ✅ Leaf node found
        return current?.ToString() ?? "";
    }
    
    private static void SetVariables(Dictionary<string, object> vars)
    {
        foreach (var kvp in vars)
        {
            if (kvp.Value is JToken jtoken)
            {
                _dynamicExpression.GetInterpreter().SetVariable(kvp.Key, jtoken);
            }

            if (kvp.Value is Dictionary<string, string> sdict)
            {
                _dynamicExpression.GetInterpreter().SetVariable(kvp.Key, sdict);
            }
            
            if (kvp.Value is Dictionary<string, object> odict)
            {
                _dynamicExpression.GetInterpreter().SetVariable(kvp.Key, odict);
            }
            else
            {
                _dynamicExpression.GetInterpreter().SetVariable(kvp.Key, kvp.Value);
            }
        }
    }
}