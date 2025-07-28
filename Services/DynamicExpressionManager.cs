using Jint;
using Newtonsoft.Json;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Apify.Services;

public class DynamicExpressionManager
{
     private Engine? _interpreter;
     private Assembly _assembly = Assembly.GetExecutingAssembly();
     private string _exprPattern = @"^ *expr *\|>";
    public DynamicExpressionManager()
    {
        _interpreter = new Engine();
        
        _interpreter.SetValue("parse", new Func<string, int>(s => int.TryParse(s, out int result) ? result : 0)); // Assuming Faker is a class that provides methods for generating fake data
        _interpreter.SetValue("toLower", new Func<string, string>(s => s.ToLower()));
        _interpreter.SetValue("toUpper", new Func<string, string>(s => s.ToUpper()));
        _interpreter.SetValue("contains", new Func<string, string, bool>((source, value) => 
        source.Contains(value, StringComparison.OrdinalIgnoreCase)));

        _interpreter.Execute("window = this;");
        ExecuteScriptFromAssembly("Apify.includes.app.js");
        
        
    }

    public void SetVariables(Dictionary<string, object> vars)
    {
        if (_interpreter == null)
        {
            throw new InvalidOperationException("Interpreter is not initialized.");
        }

        foreach (var v in vars)
        {
            _interpreter.SetValue(v.Key, v.Value);
        }
  
    }
    
    public void SetVariable(string name, object value)
    {
        if (_interpreter == null)
        {
            throw new InvalidOperationException("Interpreter is not initialized.");
        }

        _interpreter.SetValue(name, value);
    }
    
    public void SetPropertyToAppObject(string name, string expression)
    {
        if (_interpreter == null)
        {
            throw new InvalidOperationException("Interpreter is not initialized.");
        }

        _interpreter.Execute($"apify.{name} = {expression};");
    }

    public Engine GetInterpreter()
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
            _interpreter.SetValue(func.Key, func.Value);
        }
    }

    public DynamicExpressionManager SetFunction(string name, Delegate func)
    {
        if (_interpreter == null)
        {
            throw new InvalidOperationException("Interpreter is not initialized.");
        }

        _interpreter.SetValue(name, func);
        
        return this;
    }
    
    public void Execute(string expr)
    {
        if (_interpreter == null)
        {
            throw new InvalidOperationException("Interpreter is not initialized.");
        }

        if (string.IsNullOrWhiteSpace(expr))
        {
            return;
        }

        expr = GetExpression(expr);
        
        if (string.IsNullOrWhiteSpace(expr))
        {
            return;
        }
        
        _interpreter!.Execute(expr);
    }
    
    public string? Compile(string expr)
    {
        return _interpreter?.Evaluate(expr)?.AsObject()?.ToString() ?? string.Empty;
    }
    
    public T Compile<T>(string expr)
    {
        expr = expr.Replace("\\", "");
        try
        {
            var result = _interpreter!.Evaluate(expr);
            if (result.IsNull() || result.IsUndefined())
            {
                return default(T)!; // Return default value for type T
            }
            
            return (T)Convert.ChangeType(result.ToObject(), typeof(T))!;
        
        } catch (Exception)
        {
            return default(T)!; // Return default value for type T
        }
    }
    
    public void ExecuteScriptFromAssembly(string resourceName)
    {
        if (_interpreter == null)
        {
            throw new InvalidOperationException("Interpreter is not initialized.");
        }

        using (Stream? stream = _assembly.GetManifestResourceStream(resourceName))
        {
            if (stream == null)
            {
                throw new ArgumentException($"Resource '{resourceName}' not found in assembly.");
            }
            
            using (StreamReader reader = new StreamReader(stream))
            {
                string js = reader.ReadToEnd();
                _interpreter.Execute(js);
            }
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