using DynamicExpresso;
using System.Text.RegularExpressions;

namespace Apify.Services;

public class DynamicExpressionManager
{
    private Interpreter? _interpreter;
    private string _exprPattern = @"^ *expr *\|>";
    
    public DynamicExpressionManager()
    {
        _interpreter = new Interpreter(InterpreterOptions.Default);
        
        // Register helper methods to be used in expressions
        _interpreter.SetFunction("int", new Func<string, int>(s => int.TryParse(s, out int result) ? result : 0));
        _interpreter.SetFunction("Parse", new Func<string, int>(s => int.TryParse(s, out int result) ? result : 0));
        _interpreter.SetFunction("ToLower", new Func<string, string>(s => s.ToLower()));
        _interpreter.SetFunction("ToUpper", new Func<string, string>(s => s.ToUpper()));
        _interpreter.SetFunction("Contains", new Func<string, string, bool>((source, value) => 
            source.Contains(value, StringComparison.OrdinalIgnoreCase)));
    }

    public void SetVariables(Dictionary<string, object> vars)
    {
        if (_interpreter == null)
        {
            throw new InvalidOperationException("Interpreter is not initialized.");
        }

        foreach (var v in vars)
        {
            _interpreter.SetVariable(v.Key, v.Value);
        }
  
    }

    public Interpreter GetInterpreter()
    {
        return _interpreter ?? throw new InvalidOperationException("Interpreter is not initialized.");
    }

    public void SetFunctions(Dictionary<string, Delegate> funcs)
    {
        if (_interpreter == null)
        {
            throw new InvalidOperationException("Interpreter is not initialized.");
        }

        foreach (var func in funcs)
        {
            _interpreter.SetFunction(func.Key, func.Value);
        }
    }

    public DynamicExpressionManager SetFunction(string name, Delegate func)
    {
        if (_interpreter == null)
        {
            throw new InvalidOperationException("Interpreter is not initialized.");
        }

        _interpreter.SetFunction(name, func);
        
        return this;
    }
    
    public string Compile(string expr)
    {
        return Compile<string>(expr);
        
    }
    
    public T Compile<T>(string expr)
    {
        expr = expr.Replace("\\", "");
        try
        {
            var result = _interpreter!.Eval<T>(expr);
            return result;
        } catch (Exception)
        {
            return default(T)!; // Return default value for type T
        }
    }
    
    public bool IsEvalExpression(string expr)
    {
        if (string.IsNullOrWhiteSpace(expr)) return false;
        
        var funcPattern = @"^[A-Za-z_][A-Za-z0-9_]*\s*\((.*)?\)$";
        
        if (expr.StartsWith("Faker.") || Regex.IsMatch(expr.Trim(), funcPattern) || Regex.IsMatch(expr.Trim(), _exprPattern))
        {
            return true;
        }
        
        return false;
    }
    
    public string GetExpression(string expr)
    {
        if (_interpreter == null)
        {
            throw new InvalidOperationException("Interpreter is not initialized.");
        }

        if (string.IsNullOrWhiteSpace(expr))
        {
            return string.Empty;
        }

        if (Regex.IsMatch(expr.Trim(), _exprPattern))
        {
            var pattern = @"^ *expr *\|>(.*)";
            var match = Regex.Match(expr, pattern);
            
            if (!match.Success)
            {
                return string.Empty;
            }
            
            return match.Groups[1].Value.Trim();
        }

        return expr.Trim();
    }
}