using Analiser.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Analiser.JsonConverters
{
    public class CodeExpressionJsonConverter : JsonConverter<CodeExpression>
    {
        public override CodeExpression? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                CodeType codeExpression = CodeType.Empty;
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
                            codeExpression = JsonSerializer.Deserialize<CodeType>(ref reader, options)!;
                        }
                        else if (propertyName == "Name")
                        {
                            reader.Read();
                            codeMethodParameterName = reader.GetString()!;
                        }
                    }
                }

                return new CodeExpression(codeExpression, codeMethodParameterName);
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                string value = reader.GetString()!;
                return CodeExpression.Parse(value);
            }

            return CodeExpression.Empty;
        }

        public override void Write(Utf8JsonWriter writer, CodeExpression value, JsonSerializerOptions options)
        {
            if (value.Equals(CodeExpression.Empty))
            {
                writer.WriteStringValue(string.Empty);
                return;
            }

            writer.WriteStringValue($"{value.Type.FullName} {value.Name}");
        }
    }
}
