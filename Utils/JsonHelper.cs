using Newtonsoft.Json;
using Newtonsoft.Json.Linq; // Add this for JObject
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Apify.Utils
{
    public static class JsonHelper
    {
        // Custom method to extract propertyPath from JSON files
        public static Dictionary<string, string> ExtractPropertyPaths(string filePath)
        {
            var result = new Dictionary<string, string>();
            
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"File not found: {filePath}");
                    return result;
                }
                
                string json = File.ReadAllText(filePath);
                var jObject = JObject.Parse(json);
                
                if (jObject["Tests"] is JArray tests)
                {
                    foreach (var test in tests)
                    {
                        if (test is JObject testObj && 
                            test["AssertType"]?.ToString().ToLowerInvariant() == "equal" &&
                            test["propertyPath"] != null)
                        {
                            var testName = test["Name"]?.ToString() ?? "Unknown Test";
                            var propertyPath = test["propertyPath"]?.ToString();
                            
                            Console.WriteLine($"Direct Access - Found propertyPath '{propertyPath}' in test '{testName}'");
                            
                            if (!string.IsNullOrEmpty(propertyPath))
                            {
                                result[testName] = propertyPath;
                            }
                        }
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting properties from {filePath}: {ex.Message}");
                return result;
            }
        }
        
        // Add UnconditionalSuppressMessage attribute to suppress trimming warnings
        [UnconditionalSuppressMessage("AOT", "IL2026:RequiresUnreferencedCode", 
            Justification = "JSON types are preserved in TrimmerRoots.xml")]
        // Add DynamicallyAccessedMembers to ensure properties are preserved
        public static T? DeserializeFromFile<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | 
                                       DynamicallyAccessedMemberTypes.PublicProperties)]
            T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            string json = File.ReadAllText(filePath);

            return DeserializeString<T>(json);
        }

        public static T? DeserializeString<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
                                        DynamicallyAccessedMemberTypes.PublicProperties)]
            T>(string json)
        {
            var settings = new JsonSerializerSettings
            {
                Error = (sender, args) => 
                {
                    Console.WriteLine($"JSON Error: {args.ErrorContext.Error.Message}");
                    args.ErrorContext.Handled = true;
                },
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver
                {
                    NamingStrategy = new Newtonsoft.Json.Serialization.CamelCaseNamingStrategy
                    {
                        ProcessDictionaryKeys = true,
                        OverrideSpecifiedNames = false
                    }
                }
            };
            
            try
            {
                return JsonConvert.DeserializeObject<T>(json, settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing JSON: {ex.Message}");
                return default;
            }
        }

        [UnconditionalSuppressMessage("AOT", "IL2026:RequiresUnreferencedCode", 
            Justification = "JSON types are preserved in TrimmerRoots.xml")]
        public static string? SerializeToJson<
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
            T>(T obj, bool indented = true)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) => 
                    {
                        Console.WriteLine($"JSON Error: {args.ErrorContext.Error.Message}");
                        args.ErrorContext.Handled = true;
                    },
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };
                
                return JsonConvert.SerializeObject(obj, 
                    indented ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None,
                    settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing object: {ex.Message}");
                
                // Fallback to a simpler serialization method if possible
                try
                {
                    return System.Text.Json.JsonSerializer.Serialize(obj);
                }
                catch
                {
                    return "{}"; // Return empty JSON object as a last resort
                }
            }
        }

        [UnconditionalSuppressMessage("AOT", "IL2026:RequiresUnreferencedCode", 
            Justification = "JSON types are preserved in TrimmerRoots.xml")]
        public static string SerializeObject<T>(T obj, bool indented = true)
        {
            try
            {
                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) => 
                    {
                        Console.WriteLine($"JSON Error: {args.ErrorContext.Error.Message}");
                        args.ErrorContext.Handled = true;
                    },
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                };
                
                return JsonConvert.SerializeObject(obj, 
                    indented ? Newtonsoft.Json.Formatting.Indented : Newtonsoft.Json.Formatting.None,
                    settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error serializing object: {ex.Message}");
                
                // Fallback to a simpler serialization method if possible
                try
                {
                    return System.Text.Json.JsonSerializer.Serialize(obj);
                }
                catch
                {
                    return "{}"; // Return empty JSON object as a last resort
                }
            }
        }

        [UnconditionalSuppressMessage("AOT", "IL2026:RequiresUnreferencedCode", 
            Justification = "We don't care about specific types here, just validating JSON")]
        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;
                
            try
            {
                // Try with Newtonsoft first
                try
                {
                    var settings = new JsonSerializerSettings
                    {
                        Error = (sender, args) => args.ErrorContext.Handled = true
                    };
                    JsonConvert.DeserializeObject(json, settings);
                    return true;
                }
                catch
                {
                    // Fallback to System.Text.Json
                    System.Text.Json.JsonDocument.Parse(json);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        [UnconditionalSuppressMessage("AOT", "IL2026:RequiresUnreferencedCode", 
            Justification = "We don't care about specific types here, just formatting JSON")]
        public static string FormatJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return "{}";
                
            try
            {
                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) => 
                    {
                        Console.WriteLine($"Format JSON Error: {args.ErrorContext.Error.Message}");
                        args.ErrorContext.Handled = true;
                    },
                    Formatting = Newtonsoft.Json.Formatting.Indented
                };
                
                var obj = JsonConvert.DeserializeObject(json, settings);
                return JsonConvert.SerializeObject(obj, settings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error formatting JSON: {ex.Message}");
                
                try
                {
                    // Fallback to System.Text.Json
                    var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                    var element = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
                    return System.Text.Json.JsonSerializer.Serialize(element, options);
                }
                catch
                {
                    return json; // Return original if all formatting attempts fail
                }
            }
        }
    }
}
