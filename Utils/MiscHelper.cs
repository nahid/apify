using Newtonsoft.Json.Linq;
using System.Dynamic;

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

}