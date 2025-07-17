using Apify.Services;
using Bogus;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.Text.RegularExpressions;

namespace Apify.Utils;

public static class StubManager
{
    private readonly static Regex PlaceholderRe = new Regex(@"\{\{\s*(.+?)\s*\}\}",
        RegexOptions.Compiled);

    private readonly static DynamicExpressionManager DynamicExpression;
    
    static StubManager()
    {
        DynamicExpression = new DynamicExpressionManager();
        var faker = new Faker();
        DynamicExpression.GetInterpreter().SetVariable("Faker", faker);
   
    }

    /// <summary>
    /// Replaces all {{path.to.value}} stubs in <paramref name="template"/> by
    /// looking up nested dictionaries in  <paramref>
    ///     <name>variables</name>
    /// </paramref>
    /// .
    /// </summary>
    public static string Replace(
        string template,
        Dictionary<string, object> vars)
    {
        SetVariables(vars);
        
        return PlaceholderRe.Replace(template, match =>
        {
            if (DynamicExpression.IsEvalExpression(match.Groups[1].Value))
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
        var expression = DynamicExpression.GetExpression(expr);
        if (string.IsNullOrEmpty(expression))
        {
            return expr;
        }
        
        var result = DynamicExpression.Compile(expression);
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
                    IDictionary<string, object> expandoDict = expando as IDictionary<string, object>;
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
        return current.ToString() ?? "";
    }
    
    private static void SetVariables(Dictionary<string, object> vars)
    {
        foreach (var kvp in vars)
        {
            if (kvp.Value is JToken jtoken)
            {
                DynamicExpression.GetInterpreter().SetVariable(kvp.Key, jtoken);
            }

            if (kvp.Value is Dictionary<string, string> sdict)
            {
                DynamicExpression.GetInterpreter().SetVariable(kvp.Key, sdict);
            }
            
            if (kvp.Value is Dictionary<string, object> odict)
            {
                DynamicExpression.GetInterpreter().SetVariable(kvp.Key, odict);
            }
            else
            {
                DynamicExpression.GetInterpreter().SetVariable(kvp.Key, kvp.Value);
            }
        }
    }
}