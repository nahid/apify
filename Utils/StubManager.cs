using Apify.Services;
using Bogus;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.Text.RegularExpressions;

namespace Apify.Utils;

public static class StubManager
{
    private readonly static Regex PlaceholderRe = new Regex(@"\{\{\s*(.+?)\s*\}\}|\{#\s*(.+?)\s*#\}",
        RegexOptions.Compiled);

    private readonly static DynamicExpressionManager DynamicExpression;
    
    static StubManager()
    {
        DynamicExpression = new DynamicExpressionManager();
        DynamicExpression.ExecuteScriptFromAssembly("Apify.includes.faker.min.js");
        DynamicExpression.SetPropertyToAppObject("faker", "faker");
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
        SetValues(vars);
        
        return PlaceholderRe.Replace(template, match =>
        {
            if (match.Groups[1].Success)
            {
                // {{ path.to.value }}
                if (DynamicExpression.IsEvalExpression(match.Groups[1].Value))
                {
                    try
                    {
                        return ExecExpression(match.Groups[1].Value);
                    } catch (Exception ex)
                    {
                        Console.WriteLine($"Error executing expression: {ex.Message}");
                        return match.Value; // Return original if error occurs
                    }
                
                }
            
                // split "users.posts.comment.id" → ["users","posts","comment","id"]
                var parts = match.Groups[1]
                .Value
                .Split('.', StringSplitOptions.RemoveEmptyEntries);

                return AccessNestedValue(match, vars, parts);
            }
            
            if (match.Groups[2].Success)
            {
                try
                {
                    return ExecExpression(match.Groups[2].Value);
                } catch (Exception ex)
                {
                    Console.WriteLine($"Error executing expression: {ex.Message}");
                    return match.Value; // Return original if error occurs
                }
            }
            
            // If neither group matched, return the original match
            
            return match.Value;
           

        });
    }
    
    private static string ExecExpression(string expr)
    {
        var expression = DynamicExpression.GetExpression(expr);
        if (string.IsNullOrEmpty(expression))
        {
            return expr;
        }
        
        var output = DynamicExpression.Compile<object>(expression);

        var result = output.ToString() ?? string.Empty;
        
     

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
    
    private static void SetValues(Dictionary<string, object> vars)
    {
        foreach (var kvp in vars)
        {
            if (kvp.Value is JToken jtoken)
            {
                DynamicExpression.GetInterpreter().SetValue(kvp.Key, jtoken);
            }

            if (kvp.Value is Dictionary<string, string> sdict)
            {
                DynamicExpression.GetInterpreter().SetValue(kvp.Key, sdict);
            }
            
            if (kvp.Value is Dictionary<string, object> odict)
            {
                DynamicExpression.GetInterpreter().SetValue(kvp.Key, odict);
            }
            else
            {
                DynamicExpression.GetInterpreter().SetValue(kvp.Key, kvp.Value);
            }
        }
    }
}