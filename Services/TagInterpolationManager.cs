using Apify.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.Text.RegularExpressions;

namespace Apify.Services;

public static class TagInterpolationManager
{
    /// Regex to match {{path.to.value}} or {# expression #}
    private readonly static Regex TagRegex = new Regex(@"\{\{\s*(.+?)\s*\}\}|\{#\s*(.+?)\s*#\}",
        RegexOptions.Compiled);

    private readonly static DynamicScriptingManager ScriptMan;
    
    static TagInterpolationManager()
    {
        ScriptMan = new DynamicScriptingManager();
        ScriptMan.ExecuteScriptFromAssembly("Apify.includes.faker.min.js");
        ScriptMan.SetPropertyToAppObject("faker", "faker");
        ScriptMan.Execute("delete faker;");
    }

    /// <summary>
    /// Replaces all {{path.to.value}} stubs in <paramref name="template"/> by
    /// looking up nested dictionaries in  <paramref>
    ///     <name>variables</name>
    /// </paramref>
    /// .
    /// </summary>
    public static string Evaluate(
        string template,
        Dictionary<string, object> args)
    {
        SetValues(args);
        
        return TagRegex.Replace(template, match =>
        {
            // Process if the match is a {{path.to.value}}
            if (match.Groups[1].Success)
            {
                // split "users.posts.comment.id" → ["users","posts","comment","id"]
                var parts = match.Groups[1]
                .Value
                .Split('.', StringSplitOptions.RemoveEmptyEntries);

                return AccessNestedValue(match, args, parts);
            }
            
            // Process if the match is a {# expression #}
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

        if (string.IsNullOrEmpty(expr))
        {
            return expr;
        }
        
        var output = ScriptMan.Compile<object>(expr);
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
        
        return current.ToString() ?? "";
    }
    
    private static void SetValues(Dictionary<string, object> vars)
    {
        foreach (var kvp in vars)
        {
            var jsonStr = JsonConvert.SerializeObject(kvp.Value);
            jsonStr = MiscHelper.EscapeSpecialChars(jsonStr); //jsonStr.Replace("\"", "\\\"").Replace("'", "\\'"); // Escape single quotes for JS
            var jsExpression = $"JSON.parse('{jsonStr}')";
            ScriptMan.SetPropertyToAppObject(kvp.Key, jsExpression);
        }
    }
}