using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Apify.Utils
{
    /// <summary>
    /// Custom JsonConverter that can handle both objects and arrays
    /// </summary>
    public class ObjectArrayJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            // This converter can be used for any type
            return true;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            // Read the JSON token
            JToken token = JToken.Load(reader);
            
            // Return token as is - it could be an object, array, or primitive
            return token;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            // If the value is already a JToken, write it directly
            if (value is JToken token)
            {
                token.WriteTo(writer);
                return;
            }
            
            // Otherwise, serialize it normally
            if (value != null)
            {
                JToken.FromObject(value).WriteTo(writer);
            }
            else
            {
                JValue.CreateNull().WriteTo(writer);
            }
        }
    }
}