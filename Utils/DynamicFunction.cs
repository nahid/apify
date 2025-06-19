namespace Apify.Utils;

public static class DynamicFunction
{
    
    private  static Dictionary<string, Delegate> _funcs = new Dictionary<string, Delegate> {
        { "RandomInt()", new Func<int>(RandomInt) },
        { "RandomString()", RandomString },
        { "RandomEmail()", new Func<string>(RandomEmail) },
        { "Timestamp()", new Func<int>(Timestamp) },
        { "TimestampString()", new Func<string>(TimestampString) }
    };
    public static Dictionary<string, Delegate> GetFunctions()
    {
        return _funcs;
    }
    
    public static string Execute(string expr)
    {
        var funcs = GetFunctions();

        var parts = expr.Split(':');
        
        if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
        {
            return expr;
        }
        
        string funcName = parts[0];
        string[] argStrings = parts.Length > 1 ? parts[1].Split(',') : Array.Empty<string>();

        if (!funcs.TryGetValue(funcName, out var del))
            return expr;

        var methodParams = del.Method.GetParameters();

        if (methodParams.Length != argStrings.Length)
            return expr;

        var typedArgs = new object[argStrings.Length];
        for (int i = 0; i < argStrings.Length; i++)
        {
            var targetType = methodParams[i].ParameterType;
            typedArgs[i] = Convert.ChangeType(argStrings[i], targetType); // Basic type conversion
        }

        var result = del.DynamicInvoke(typedArgs);
        if (result == null)
            return string.Empty;
        
        return result as string;
    }
    
    public static void RegisterFunction(string name, Delegate function)
    {
        if (string.IsNullOrWhiteSpace(name) || function == null)
            throw new ArgumentException("Function name and function cannot be null or empty.");

        if (_funcs.ContainsKey(name))
            throw new ArgumentException($"Function {name} is already registered.");
 
        _funcs.Add(name, function); // Add new function
    }
    public static int RandomInt()
    {
        var random = new Random();
        return random.Next(1, 10000);
    }
    
    public static string RandomString(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
    
    public static string RandomEmail()
    {
        return $"{RandomString(10)}@example.com";
    }
    
    public static int Timestamp()
    {
        return (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
    }
    
    public static string TimestampString()
    {
        return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
    
    
    
}