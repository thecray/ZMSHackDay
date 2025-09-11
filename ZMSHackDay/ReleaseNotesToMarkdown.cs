using System.Text;
using System.Text.Json;

namespace ZMSHackDay
{
    public class ReleaseNotesToMarkdown
    {
        public class ReleaseNotesVersion
        {
            public string Version { get; set; } = string.Empty;
            public List<WorkItem> WorkItems { get; set; } = new List<WorkItem>();
        }

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

        public void Run(string inputFile, string outputFile, string outputMarkdownFile)
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

            List<ReleaseNotesVersion> outputVersions = new List<ReleaseNotesVersion>();
            IEnumerable<string> versions = orderedItems.Select(i => i.Version).Distinct();
            foreach (var version in versions)
            {
                var versionItem = new ReleaseNotesVersion();
                versionItem.Version = version;

                versionItem.WorkItems.AddRange(orderedItems.Where(i => i.Version == version));

                outputVersions.Add(versionItem);
            }

            string newJson = JsonSerializer.Serialize(outputVersions, options);
            File.WriteAllText(outputFile, newJson);

            BuildMarkdown(outputVersions, outputMarkdownFile);
        }

        private void BuildMarkdown(List<ReleaseNotesVersion> outputVersions, string filename)
        {
            StringBuilder builder = new StringBuilder();

            foreach (ReleaseNotesVersion version in outputVersions)
            {
                builder.AppendLine($"# {version.Version}");
                foreach (WorkItem item in version.WorkItems)
                {
                    builder.AppendLine($"* **{item.Title}**  ");
                    builder.AppendLine(item.FunctionalNotes + "  ");
                    builder.AppendLine();
                }
                builder.AppendLine();
            }

            File.WriteAllText(filename, builder.ToString());
        }
    }
}
