using Analiser.Data;

namespace Analiser
{
    public class DataImporter
    {
        public List<CodeProject> Projects { get; } = new List<CodeProject>();

        public bool Import(string filename)
        {
            Projects.Clear();

            string json = File.ReadAllText(filename);
            CodeProject[]? projectsFromJson = System.Text.Json.JsonSerializer.Deserialize<CodeProject[]>(json);
            if (projectsFromJson == null)
            {
                return false;
            }

            Projects.AddRange(projectsFromJson);

            return true;
        }
    }
}
