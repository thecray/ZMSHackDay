using Analiser.Data;
using Analiser.JsonConverters;
using System.Text.Json;

namespace Analiser
{
    public class DataImporter
    {
        public List<CodeProject> Projects { get; } = new List<CodeProject>();

        public bool Import(string filename)
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

            Projects.Clear();

            string json = File.ReadAllText(filename);
            CodeProject[]? projectsFromJson = JsonSerializer.Deserialize<CodeProject[]>(json, options);
            if (projectsFromJson == null)
            {
                return false;
            }

            Projects.AddRange(projectsFromJson);

            return true;
        }
    }
}
