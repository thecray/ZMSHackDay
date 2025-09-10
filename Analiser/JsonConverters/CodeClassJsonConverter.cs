using Analiser.Data;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Analiser.JsonConverters
{
    internal class CodeClassJsonConverter : JsonConverter<CodeClass>
    {
        public override CodeClass? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new Exception("Invalid token to start CodeClass.");
            }

            CodeClass result = new CodeClass(string.Empty, string.Empty, null, []);

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
                        result.Namespace = reader.GetString()!;
                    }
                    else if (propertyName == "Name")
                    {
                        reader.Read();
                        result.Name = reader.GetString()!;
                    }
                    else if (propertyName == "BaseType")
                    {
                        result.BaseType = JsonSerializer.Deserialize<CodeType>(ref reader, options);
                    }
                    else if (propertyName == "InterfaceTypes")
                    {
                        result.InterfaceTypes = JsonSerializer.Deserialize<CodeType[]>(ref reader, options)!;
                    }
                    else if (propertyName == "Properties")
                    {
                        result.Properties = JsonSerializer.Deserialize<List<CodeProperty>>(ref reader, options)!;
                    }
                    else if (propertyName == "Methods")
                    {
                        result.Methods = JsonSerializer.Deserialize<List<CodeMethod>>(ref reader, options)!;
                    }
                }
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, CodeClass value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString("Namespace", value.Namespace);
            writer.WriteString("Name", value.Name);

            writer.WritePropertyName("BaseType");
            JsonSerializer.Serialize(writer, value.BaseType, options);

            if (value.InterfaceTypes.Length > 0)
            {
                writer.WritePropertyName("InterfaceTypes");
                JsonSerializer.Serialize(writer, value.InterfaceTypes, options);
            }

            if (value.Properties.Count > 0)
            {
                writer.WritePropertyName("Properties");
                JsonSerializer.Serialize(writer, value.Properties, options);
            }

            if (value.Methods.Count > 0)
            {
                writer.WritePropertyName("Methods");
                JsonSerializer.Serialize(writer, value.Methods, options);
            }

            writer.WriteEndObject();
        }
    }
}
