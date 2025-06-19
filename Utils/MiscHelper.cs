using System.Dynamic;

namespace Apify.Utils;

public static class MiscHelper
{
    public static ExpandoObject? ToExpandoObject(Dictionary<string, string> dict)
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
}