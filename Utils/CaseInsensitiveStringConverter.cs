using Newtonsoft.Json;
using System;

namespace APITester.Utils
{
    public class CaseInsensitiveStringConverter : JsonConverter<string>
    {
        public override string ReadJson(JsonReader reader, Type objectType, string? existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return reader.Value?.ToString() ?? string.Empty;
        }

        public override void WriteJson(JsonWriter writer, string? value, JsonSerializer serializer)
        {
            writer.WriteValue(value ?? string.Empty);
        }
    }
}