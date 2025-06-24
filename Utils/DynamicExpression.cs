using Bogus;
using DynamicExpresso;
using System.Text.RegularExpressions;

namespace Apify.Utils;

public static class DynamicExpression
{
    
    private  static Dictionary<string, Delegate> _funcs = new Dictionary<string, Delegate> {
        { "RandomInt", RandomInt },
        { "RandomString", RandomString },
        { "RandomEmail", RandomEmail },
        { "Timestamp", Timestamp },
        { "TimestampString", TimestampString }
    };
    public static Dictionary<string, Delegate> GetFunctions()
    {
        return _funcs;
    }

    private static Interpreter RegisterFunctionsToInterpreter()
    {
        var interpreter = new Interpreter();
        foreach (var func in _funcs)
        {
            interpreter.SetFunction(func.Key, func.Value);
        }
        
        var faker = new Faker("en");
        interpreter.SetVariable("Faker", faker);
        
        return interpreter;
    }
    

    public static string Execute(string expr)
    {
        if (!IsAFunction(expr))
        {
            return expr;
        }

        var funcName = GetFunctionName(expr);
        
        if (!expr.StartsWith("Faker.") && !_funcs.ContainsKey(funcName))
        {
            return expr;
        }
        
        try
        {
            var interpreter = RegisterFunctionsToInterpreter();

            var result = interpreter.Eval(expr);

            return result.ToString() ?? string.Empty;
        } catch (Exception ex)
        {
            Console.WriteLine($"Error executing expression '{expr}': {ex.Message}");
            return expr;
        }
        
    }
    

    public static bool IsAFunction(string expr)
    {
        if (string.IsNullOrWhiteSpace(expr)) return false;

        if (expr.StartsWith("Faker."))
        {
            return true;
        }

        // Simple regex for matching C#-style function calls like Func("arg", 2)
        var pattern = @"^[A-Za-z_][A-Za-z0-9_]*\s*\((.*)?\)$";
        return Regex.IsMatch(expr.Trim(), pattern);
    }
    
    public static bool IsEvalExpression(string expr)
    {
        if (string.IsNullOrWhiteSpace(expr)) return false;
        
        if (expr.StartsWith("Faker.") || IsAFunction(expr))
        {
            return true;
        }

        if (expr.StartsWith("expr|>") || expr.StartsWith("expr |>"))
        {
            return true;
        }

        // Check if the expression starts with "Faker." or is a function call
        return false;
    }
    
    public static string GetExpression(string expr)
    {
        if (string.IsNullOrWhiteSpace(expr)) return string.Empty;
        
        if (!IsEvalExpression(expr)) return expr;

        // Remove "expr|>" or "expr |>" prefix if it exists
        if (expr.StartsWith("expr|>") || expr.StartsWith("expr |>"))
        {
            return expr.Substring(6).Trim();
        }

        return expr.Trim();
    }
    
    public static string? GetFunctionName(string expr)
    {
        var match = Regex.Match(expr.Trim(), @"^([A-Za-z_][A-Za-z0-9_]*)\s*\(");
        return match.Success ? match.Groups[1].Value : null;
    }


    
    public static void RegisterFunction(string name, Delegate function)
    {
        if (string.IsNullOrWhiteSpace(name) || function == null)
            throw new ArgumentException("Function name and function cannot be null or empty.");

        if (_funcs.ContainsKey(name))
            throw new ArgumentException($"Function {name} is already registered.");
 
        _funcs.Add(name, function); // Add new function
    }
    public static int RandomInt(int min = 0, int max = 100)
    {
        var random = new Random();
        return random.Next(min, max);
    }
    
    public static string RandomString(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
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