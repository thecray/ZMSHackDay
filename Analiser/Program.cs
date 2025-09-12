using Microsoft.CodeAnalysis;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis.MSBuild;
using Analiser.Data;

namespace Analiser
{
    internal class Program
    {
        private static bool _isDemo = true;
        private static string _demoIhoFromVersion = "3.1.695.0";
        private static string _demoIhoToVersion = "3.1.778.0";
        private static string _demoClient = "isleofman";

        private static bool _buildIho = true;
        private static bool _importIho = false;

        private static bool _buildIsleOfMan = false;
        private static bool _importIsleOfMan = false;

        private static bool _processChanges = false;

        #region Main

        static void Main(string[] args)
        {
            Console.Clear();

            List<MetadataReference> references = new List<MetadataReference>();

            if (_isDemo)
            {
                if (args.Length < 6)
                {
                    Console.WriteLine();
                    Console.WriteLine("Usage: CodeAnalyser -from [version] -to [version] -client [client]");
                    Console.WriteLine();
                    return;
                }

                string fromVersion = args[1];
                string toVersion = args[3];
                string client = args[5];

                Console.WriteLine();
                Console.WriteLine($"Analysing code for iho versions '{fromVersion}' - '{toVersion}'");
                Console.WriteLine($"Analysing code for current version of '{client}'");
                Console.WriteLine();

                RunDemo();
                return;
            }





            if (_buildIho || _importIho)
            {
                Console.WriteLine("=======");
                Console.WriteLine("= Iho =");
                Console.WriteLine("=======");

                if (_buildIho)
                {
                    CodeTreeBuilder builder = BuildIho();
                    PrintProjectOverview(builder.Projects);
                    DataExporter.Export(builder.Projects, "output-iho-3_1_778_0.json");
                }

                if (_importIho)
                {
                    DataImporter importer = new DataImporter();
                    if (importer.Import("output-iho-3_1_695_0.json"))
                    {
                        PrintProjectOverview(importer.Projects);
                        DataExporter.Export(importer.Projects, "output-iho-compare.json"); // save the file again, so we can ensure that what was loaded was 100% correct
                    }
                }

                Console.WriteLine();
                Console.WriteLine();
            }

            if (_buildIsleOfMan || _importIsleOfMan)
            {
                Console.WriteLine("===============");
                Console.WriteLine("= Isle of Man =");
                Console.WriteLine("===============");

                if (_buildIsleOfMan)
                {
                    CodeTreeBuilder builder = BuildIsleOfMan();
                    PrintProjectOverview(builder.Projects);
                    DataExporter.Export(builder.Projects, "output-iom.json");
                }

                if (_importIsleOfMan)
                {
                    DataImporter importer = new DataImporter();
                    if (importer.Import("output-iom.json"))
                    {
                        PrintProjectOverview(importer.Projects);
                        DataExporter.Export(importer.Projects, "output-iom-compare.json"); // save the file again, so we can ensure that what was loaded was 100% correct
                    }
                }

                Console.WriteLine();
                Console.WriteLine();
            }

            if (_processChanges)
            {
                Console.WriteLine("===========");
                Console.WriteLine("= Changes =");
                Console.WriteLine("===========");

                Comparitorator comparitorator = new Comparitorator("output-iho-3_1_776_0.json", "output-iho-3_1_778_0.json");
                comparitorator.Run("changes-iho-3_1_776_0__3_1_778_0.json");
                PrintProjectOverview(comparitorator.Changes);
            }

        }

        private static void RunDemo()
        {
            DataImporter importer = new DataImporter();

            Console.WriteLine("===================================");
            Console.WriteLine($"Analysing Iho From {_demoIhoFromVersion}");
            Console.WriteLine("===================================");
            importer.Import($"input-iho-{_demoIhoFromVersion.Replace(".", "_")}.json");
            Thread.Sleep(1500);
            PrintProjectOverview(importer.Projects);
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("===================================");
            Console.WriteLine($"Analysing Iho To {_demoIhoToVersion}");
            Console.WriteLine("===================================");
            importer.Import($"input-iho-{_demoIhoToVersion.Replace(".", "_")}.json");
            Thread.Sleep(1500);
            PrintProjectOverview(importer.Projects);
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("===================================");
            Console.WriteLine($"Analysing Client {_demoClient}");
            Console.WriteLine("===================================");
            importer.Import($"input-{_demoClient}-current.json");
            Thread.Sleep(1000);
            PrintProjectOverview(importer.Projects);
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("===================================");
            Console.WriteLine($"Analysing Changes");
            Console.WriteLine("===================================");
            Comparitorator comparitorator = new Comparitorator($"input-iho-{_demoIhoFromVersion.Replace(".", "_")}.json", $"input-iho-{_demoIhoToVersion.Replace(".", "_")}.json");
            comparitorator.Run($"changes-iho-{_demoIhoFromVersion.Replace(".", "_")}-{_demoIhoToVersion.Replace(".", "_")}.json");
            PrintChangesOverview(comparitorator.Changes);
            Console.WriteLine();
            Console.WriteLine();

            Console.WriteLine("===========");
            Console.WriteLine("Complete!");
            Console.WriteLine("===========");

            Console.ReadKey();
        }

        private static void PrintProjectOverview(IEnumerable<CodeProject> projects)
        {
            Console.WriteLine($"Projects: {projects.Count()}");

            CodeClass[] allClasses = projects.SelectMany(p => p.Classes).ToArray();
            Console.WriteLine($"Classes: {allClasses.Length}");

            int numProperties = allClasses.Sum(c => c.Properties.Count);
            Console.WriteLine($"Properties: {numProperties}");

            int numMethods = allClasses.Sum(c => c.Methods.Count);
            Console.WriteLine($"Methods: {numMethods}");
        }

        private static void PrintChangesOverview(IEnumerable<CodeProject> projects)
        {
            Console.WriteLine($"Changed Projects: {projects.Count()}");

            CodeClass[] allClasses = projects.SelectMany(p => p.Classes).ToArray();
            Console.WriteLine($"Changed Classes: {allClasses.Length}");

            int numProperties = allClasses.Sum(c => c.Properties.Count);
            Console.WriteLine($"Changed Properties: {numProperties}");

            int numMethods = allClasses.Sum(c => c.Methods.Count);
            Console.WriteLine($"Changed Methods: {numMethods}");
        }

        #endregion

        #region Builders

        private static CodeTreeBuilder BuildIho()
        {
            List<MetadataReference> references = new List<MetadataReference>();
            Workspace workspace = CreateIhoWorkspace(references);
            CodeTreeBuilder codeTreeCreator = new CodeTreeBuilder(workspace, references);
            codeTreeCreator.ProcessProject("IhoBusinessObjects(net462)");
            codeTreeCreator.ProcessProject("IhoUserInterface");

            return codeTreeCreator;
        }

        private static CodeTreeBuilder BuildIsleOfMan()
        {
            List<MetadataReference> references = new List<MetadataReference>();
            Workspace workspace = CreateIsleOfManWorkSpace(references);
            CodeTreeBuilder codeTreeCreator = new CodeTreeBuilder(workspace, references);
            codeTreeCreator.ProcessProject("IsleOfManBusinessObjects(net462)");
            codeTreeCreator.ProcessProject("IsleOfManUserInterface");

            return codeTreeCreator;
        }

        #endregion

        #region Workspace Initialisation

        private static MSBuildWorkspace CreateIhoWorkspace(List<MetadataReference> references)
        {
            string solutionpath = @"D:\Repos\Iho\iho-v3\Iho.sln";

            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            Solution solution = workspace.OpenSolutionAsync(solutionpath).Result;

            references.AddRange(ReferenceAssemblies.Net472);

            // We should attempt to exclude certain projects here, mainly the framework libraries, since changes to those trigger changes to everything.

            return workspace;
        }

        private static MSBuildWorkspace CreateIsleOfManWorkSpace(List<MetadataReference> references)
        {
            string solutionpath = @"D:\Development\Iho\IsleOfMan\Src\IsleOfMan.sln";

            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            Solution solution = workspace.OpenSolutionAsync(solutionpath).Result;

            string ihoVersion = "3.1.695";

            references.AddRange(ReferenceAssemblies.Net472);

            // load iho nuget references
            // it's ideal to not load BusinessLayerFramework, since it means changes to base types don't just trigger changes to everything.
            AddIhoNugetReference(references, ihoVersion, "DataTorque.Iho.Interfaces.dll");
            AddIhoNugetReference(references, ihoVersion, "DataTorque.Iho.BusinessObjects.dll");
            AddIhoNugetReference(references, ihoVersion, "DataTorque.Iho.UserInterface.dll");

            return workspace;
        }

        private static void AddIhoNugetReference(List<MetadataReference> references, string ihoVersion, string libraryName)
        {
            string path = @$"C:\Users\craigr\.nuget\packages\{Path.GetFileNameWithoutExtension(libraryName).ToLower()}\{ihoVersion}\lib\net462\{libraryName}";
            if (!File.Exists(path))
            {
                throw new Exception($"File not found at '{path}'");
            }

            references.Add(MetadataReference.CreateFromFile(path));
        }

        #endregion
    }
}
