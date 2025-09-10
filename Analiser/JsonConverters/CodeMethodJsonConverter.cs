using Analiser.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Analiser.JsonConverters
{
    public class CodeMethodJsonConverter : JsonConverter<CodeMethod>
    {
        public override CodeMethod? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new Exception("Invalid token to start CodeMethod.");
            }

            CodeMethod result = new CodeMethod([], CodeType.Empty, string.Empty, string.Empty);

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    break;
                }

                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    string propertyName = reader.GetString()!;
                    if (propertyName == "Modifiers")
                    {
                        reader.Read();
                        if (reader.TokenType == JsonTokenType.String)
                        {
                            string modifiers = reader.GetString()!;
                            result.Modifiers = modifiers.Split(' ');
                        }
                        else
                        {
                            List<string> modifiers = new List<string>();
                            while (reader.Read())
                            {
                                if (reader.TokenType == JsonTokenType.EndArray)
                                {
                                    break;
                                }

                                modifiers.Add(reader.GetString()!);
                            }
                            result.Modifiers = modifiers.ToArray();
                        }
                    }
                    else if (propertyName == "ReturnType")
                    {
                        result.ReturnType = JsonSerializer.Deserialize<CodeType>(ref reader, options)!;
                    }
                    else if (propertyName == "Name")
                    {
                        reader.Read();
                        result.Name = reader.GetString()!;
                    }
                    else if (propertyName == "Hash")
                    {
                        reader.Read();
                        result.Hash = reader.GetString()!;
                    }
                    else if (propertyName == "Parameters")
                    {
                        result.Parameters = JsonSerializer.Deserialize<List<CodeMethodParameter>>(ref reader, options)!;
                    }
                    else if (propertyName == "Body")
                    {
                        result.Body = JsonSerializer.Deserialize<CodeMethodBody>(ref reader, options)!;
                    }
                }
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, CodeMethod value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            if (value.Modifiers.Length > 0)
            {
                writer.WriteString("Modifiers", string.Join(" ", value.Modifiers));
            }

            writer.WritePropertyName("ReturnType");
            JsonSerializer.Serialize(writer, value.ReturnType, options);

            writer.WriteString("Name", value.Name);
            writer.WriteString("Hash", value.Hash);

            if (value.Parameters.Count > 0)
            {
                writer.WritePropertyName("Parameters");
                JsonSerializer.Serialize(writer, value.Parameters, options);
            }

            if (!value.Body.Equals(CodeMethodBody.Empty))
            {
                writer.WritePropertyName("Body");
                JsonSerializer.Serialize(writer, value.Body, options);
            }

            writer.WriteEndObject();
        }
    }
}
