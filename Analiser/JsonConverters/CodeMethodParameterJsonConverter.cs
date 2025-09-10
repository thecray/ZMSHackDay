using Analiser.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Analiser.JsonConverters
{
    public class CodeMethodParameterJsonConverter : JsonConverter<CodeMethodParameter>
    {
        public override CodeMethodParameter? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                CodeType codeMethodParameterType = CodeType.Empty;
                string codeMethodParameterName = string.Empty;
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
                            codeMethodParameterType = JsonSerializer.Deserialize<CodeType>(ref reader, options)!;
                        }
                        else if (propertyName == "Name")
                        {
                            reader.Read();
                            codeMethodParameterName = reader.GetString()!;
                        }
                    }
                }

                return new CodeMethodParameter(codeMethodParameterType, codeMethodParameterName);
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                string value = reader.GetString()!;
                return CodeMethodParameter.Parse(value);
            }

            return CodeMethodParameter.Empty;
        }

        public override void Write(Utf8JsonWriter writer, CodeMethodParameter value, JsonSerializerOptions options)
        {
            if (value == CodeMethodParameter.Empty)
            {
                writer.WriteStringValue(string.Empty);
                return;
            }

            writer.WriteStringValue($"{value.Type.FullName} {value.Name}");
        }
    }
}
