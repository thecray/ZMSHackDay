using Analiser.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Analiser.JsonConverters
{
    internal class CodeMethodBodyJsonConverter : JsonConverter<CodeMethodBody>
    {
        public override CodeMethodBody? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new Exception("Invalid token to start CodeMethodBody.");
            }

            CodeMethodBody result = new CodeMethodBody();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString()!;
                    if (propertyName == "BodyText")
                    {
                        reader.Read();
                        result.BodyText = reader.GetString()!;
                    }
                    else if (propertyName == "OriginalBodyText")
                    {
                        reader.Read();
                        result.OriginalBodyText = reader.GetString()!;
                    }
                    else if (propertyName == "ReferencedTypes")
                    {
                        result.ReferencedTypes = JsonSerializer.Deserialize<List<CodeType>>(ref reader, options)!;
                    }
                    else if (propertyName == "ReferencedStoredProcedures")
                    {
                        result.ReferencedStoredProcedures = JsonSerializer.Deserialize<List<string>>(ref reader, options)!;
                    }
                    else if (propertyName == "ReferencedExpressions")
                    {
                        result.ReferencedExpressions = JsonSerializer.Deserialize<List<CodeExpression>>(ref reader, options)!;
                    }
                }
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, CodeMethodBody value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            if (value.BodyText != string.Empty)
            {
                writer.WriteString("BodyText", value.BodyText);
            }

            if (value.OriginalBodyText != string.Empty)
            {
                writer.WriteString("OriginalBodyText", value.OriginalBodyText);
            }

            if (value.ReferencedTypes.Count > 0)
            {
                writer.WritePropertyName("ReferencedTypes");
                JsonSerializer.Serialize(writer, value.ReferencedTypes, options);
            }

            if (value.ReferencedStoredProcedures.Count > 0)
            {
                writer.WritePropertyName("ReferencedStoredProcedures");
                JsonSerializer.Serialize(writer, value.ReferencedStoredProcedures, options);
            }

            if (value.ReferencedExpressions.Count > 0)
            {
                writer.WritePropertyName("ReferencedExpressions");
                JsonSerializer.Serialize(writer, value.ReferencedExpressions, options);
            }

        //public string BodyText { get; set; } = string.Empty;
        //public string OriginalBodyText { get; set; } = string.Empty;

            writer.WriteEndObject();
        }
    }
}
