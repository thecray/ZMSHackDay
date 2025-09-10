using Analiser.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Analiser.JsonConverters
{
    public class CodePropertyJsonConverter : JsonConverter<CodeProperty>
    {
        public override CodeProperty? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                CodeType codePropertyType = CodeType.Empty;
                string codePropertyName = string.Empty;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        string propertyName = reader.GetString()!;
                        if (propertyName == "Type")
                        {
                            codePropertyType = JsonSerializer.Deserialize<CodeType>(ref reader, options)!;
                        }
                        else if (propertyName == "Name")
                        {
                            reader.Read();
                            codePropertyName = reader.GetString()!;
                        }
                    }
                }

                return new CodeProperty(codePropertyType, codePropertyName);
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                string value = reader.GetString()!;
                return CodeProperty.Parse(value);
            }

            return CodeProperty.Empty;
        }

        public override void Write(Utf8JsonWriter writer, CodeProperty value, JsonSerializerOptions options)
        {
            if (value == CodeProperty.Empty)
            {
                writer.WriteStringValue(string.Empty);
                return;
            }

            writer.WriteStringValue($"{value.Type.FullName} {value.Name}");
        }
    }
}
