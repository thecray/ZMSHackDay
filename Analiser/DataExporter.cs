using Analiser.Data;
using Analiser.JsonConverters;
using System.Text.Json;

namespace Analiser
{
    public class DataExporter
    {
        public static void Export(List<CodeProject> projects, string filename)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                AllowTrailingCommas = true,
                IndentCharacter = '\t',
                IndentSize = 1,
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
                WriteIndented = true,
                Converters =
                {
                    new CodeClassJsonConverter(),
                    new CodeExpressionJsonConverter(),
                    new CodeMethodJsonConverter(),
                    new CodeMethodBodyJsonConverter(),
                    new CodeMethodParameterJsonConverter(),
                    new CodePropertyJsonConverter(),
                    new CodeTypeJsonConverter()
                }
            };

            string json = JsonSerializer.Serialize(projects, options);
            File.WriteAllText(filename, json);

            Console.WriteLine($"Output saved to '{filename}' ({json.Length} chars)");
        }
    }
}
