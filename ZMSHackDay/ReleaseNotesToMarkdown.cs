using System.Text.Json;

namespace ZMSHackDay
{
    public class ReleaseNotesToMarkdown
    {
        public class WorkItem : IComparable<WorkItem>
        {
            public class WorkItemVersion : IComparable<WorkItemVersion>
            {
                public int Major { get; set; }
                public int Minor { get; set; }
                public int Build { get; set; }
                public int Revision { get; set; }

                public int CompareTo(WorkItemVersion? other)
                {
                    if (other == null) return 1;

                    if (other.Major < Major) { return -1; }
                    else if (other.Major > Major) { return 1; }
                    else
                    {
                        if (other.Minor < Minor) { return -1; }
                        else if (other.Minor > Minor) { return 1; }
                        else
                        {
                            if (other.Build < Build) { return -1; }
                            else if (other.Build > Build) { return 1; }
                            else
                            {
                                if (other.Revision < Revision) { return -1; }
                                else if (other.Revision > Revision) { return 1; }
                                else { return 0; }
                            }
                        }
                    }
                }
            }

            public int Id { get; set; }
            public string Version { get; set; }
            public string Title { get; set; }
            public string FunctionalNotes { get; set; }
            public string DeveloperNotes { get; set; }
            public WorkItemVersion VersionObject { get; set; }

            public WorkItem()
            {
                Id = 0;
                Version = string.Empty;
                Title = string.Empty;
                FunctionalNotes = string.Empty;
                DeveloperNotes = string.Empty;
                VersionObject = new WorkItemVersion();
            }

            public int CompareTo(WorkItem? other)
            {
                if (other == null)
                {
                    return 1;
                }

                return other.VersionObject.CompareTo(VersionObject);
            }
        }

        public void Run(string inputFile, string outputFile)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                IndentCharacter = '\t',
                IndentSize = 1,
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
            };

            string json = File.ReadAllText(inputFile);
            WorkItem[] allitems = JsonSerializer.Deserialize<WorkItem[]>(json, options)!;

            WorkItem[] orderedItems = allitems.Where(p => !string.IsNullOrEmpty(p.FunctionalNotes)).OrderBy(i => i).ToArray();
            string newJson = JsonSerializer.Serialize(orderedItems, options);
            File.WriteAllText(outputFile, newJson);
        }
    }
}
