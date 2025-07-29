using Jint;
using System.Reflection;

namespace Apify.Services;

public class DynamicScriptingManager
{
     private Engine? _interpreter;
     private Assembly _assembly = Assembly.GetExecutingAssembly();
     
    public DynamicScriptingManager()
    {
        _interpreter = new Engine();

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

    public DynamicScriptingManager SetFunction(string name, Delegate func)
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
        
        
        if (string.IsNullOrWhiteSpace(expr))
        {
            return;
        }
        
        _interpreter!.Execute(expr);
    }
    
    public string Compile(string expr)
    {
        return _interpreter?.Evaluate(expr).AsObject().ToString() ?? string.Empty;
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
            return default!; // Return default value for type T
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
}