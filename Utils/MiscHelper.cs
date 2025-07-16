using Apify.Commands;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.Net;
using System.Text.RegularExpressions;

namespace Apify.Utils;

public static class MiscHelper
{
    public static ExpandoObject? DictionaryToExpandoObject(Dictionary<string, string> dict)
    {
        if (dict == null)
        {
            throw new ArgumentNullException(nameof(dict), "Input dictionary cannot be null.");
        }
            
        var expando = new ExpandoObject() as IDictionary<string, object>;

        foreach (var kvp in dict)
        {
            expando[kvp.Key] = kvp.Value;
        }
            
        return expando as ExpandoObject;
    }
    
    public static Dictionary<string, string> MergeDictionaries(
        Dictionary<string, string> dict1, 
        Dictionary<string, string> dict2)
    {
        if (dict1 == null) throw new ArgumentNullException(nameof(dict1), "First dictionary cannot be null.");
        if (dict2 == null) throw new ArgumentNullException(nameof(dict2), "Second dictionary cannot be null.");

        var merged = new Dictionary<string, string>(dict1);

        foreach (var kvp in dict2)
        {
            merged[kvp.Key] = kvp.Value;
        }

        return merged;
    }
    
    public static Dictionary<string, string> ParseArgsVariables(string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            return new Dictionary<string, string>();
        }

        var dict = new Dictionary<string, string>();
        foreach (string pair in args.Split(';'))
        {
            string[] keyValue = pair.Split('=');
            if (keyValue.Length == 2)
            {
                dict[keyValue[0]] = keyValue[1];
            }
        }

        return dict;
    }
    
    
    public static string HandlePath(string path, string extension = ".json")
    {
        string defaultDirectory = RootOption.DefaultApiDirectory;
        string processedPath = path;
            
        if (processedPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            if (Path.IsPathRooted(processedPath))
            {
                // If it's an absolute path, just return it
                return processedPath;
            }
                
            if (!Directory.Exists(defaultDirectory))
            {
                throw new DirectoryNotFoundException($"Default API directory '{defaultDirectory}' does not exist. Please create it or specify a different path.");
            }
                
            // If it already has .json extension, just return it
            return Path.Combine(defaultDirectory, path);
        }
            
        if (!Directory.Exists(defaultDirectory))
        {
            throw new DirectoryNotFoundException($"Default API directory '{defaultDirectory}' does not exist. Please create it or specify a different path.");
        }
            
        var pathWithoutExtension = path.Replace(".", Path.DirectorySeparatorChar.ToString());
        path = Path.Combine(defaultDirectory, pathWithoutExtension + extension);

        return path;
    }
    
    public static bool IsLikelyPath(string input)
    {
        return Path.IsPathRooted(input) && input.IndexOfAny(Path.GetInvalidPathChars()) == -1;
    }

    public static string GetHttpStatusCodeName(object statusCode)
    {
        var code = statusCode.ToString();
        string name = "Unknown";
        if (Enum.TryParse<HttpStatusCode>(code, out HttpStatusCode parsedCode))
        {
            name = parsedCode.ToString(); // Returns "NotFound"
        }

        return PascalCaseToSpaceCase(name);

    }
    
    public static string PascalCaseToSpaceCase(string input)
    {
        // প্রথমে, দুটি Capital এর মাঝে স্পেস (যেমন: "TooManyRedirection")
        string result = Regex.Replace(input, "([a-z])([A-Z])", "$1 $2");
        // পরে, একাধিক Capital letter থাকলে (যেমন: "HTTPStatusCode" => "HTTP Status Code")
        result = Regex.Replace(result, "([A-Z])([A-Z][a-z])", "$1 $2");
        return result.Trim();
    }

}