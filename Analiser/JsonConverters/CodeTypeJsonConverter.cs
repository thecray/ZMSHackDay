using Analiser.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Analiser.JsonConverters
{
    public class CodeTypeJsonConverter : JsonConverter<CodeType>
    {
        public override CodeType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.StartObject)
            {
                string codeTypeNameSpace = string.Empty;
                string codeTypeName = string.Empty;
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType == JsonTokenType.PropertyName)
                    {
                        string propertyName = reader.GetString()!;
                        if (propertyName == "Namespace")
                        {
                            reader.Read();
                            codeTypeNameSpace = reader.GetString()!;
                        }
                        else if (propertyName == "Name")
                        {
                            reader.Read();
                            codeTypeName = reader.GetString()!;
                        }
                    }
                }

                return new CodeType(codeTypeNameSpace, codeTypeName);
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                string value = reader.GetString()!;
                return CodeType.Parse(value);
            }

            return CodeType.Empty;
        }

        public override void Write(Utf8JsonWriter writer, CodeType value, JsonSerializerOptions options)
        {
            if (value.Equals(CodeType.Empty))
            {
                writer.WriteStringValue(string.Empty);
                return;
            }

            if (value == CodeType.Void)
            {
                writer.WriteStringValue("void");
                return;
            }

            writer.WriteStringValue(value.FullName);
        }
    }
}
