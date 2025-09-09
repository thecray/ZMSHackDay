using Analiser.Data;

namespace Analiser
{
    public class Comparitorator
    {
        private readonly string _oldFilename;
        private readonly string _newFilename;

        public List<CodeProject> Changes { get; } = new List<CodeProject>();

        public Comparitorator(string oldFilename, string newFilename)
        {
            _oldFilename = oldFilename;
            _newFilename = newFilename;
        }

        public void Run(string outputFilename)
        {
            DataImporter importer = new DataImporter();

            if (!importer.Import(_oldFilename))
            {
                Console.WriteLine($"Unable to import from file '{_oldFilename}'");
                return;
            }


            List<CodeProject> oldData = new List<CodeProject>(importer.Projects);

            if (!importer.Import(_newFilename))
            {
                Console.WriteLine($"Unable to import from file '{_newFilename}'");
                return;
            }

            List<CodeProject> newData = new List<CodeProject>(importer.Projects);

            Compare(oldData, newData);

            DataExporter.Export(Changes, outputFilename);
        }

        private void Compare(List<CodeProject> oldData, List<CodeProject> newData)
        {
            Changes.Clear();

            foreach (CodeProject currentProject in newData)
            {
                CodeProject? oldProject = oldData.FirstOrDefault(p => p.Name == currentProject.Name);
                if (oldProject == null)
                {
                    // it's a new project, add the entire thing
                    Changes.Add(currentProject);
                    continue;
                }

                if (CloneWithChanges(currentProject, oldProject, out CodeProject? clonedProject))
                {
                    Changes.Add(clonedProject!);
                }
            }
        }

        private bool CloneWithChanges(CodeProject currentProject, CodeProject oldProject, out CodeProject? clonedProject)
        {
            if (currentProject.Equals(oldProject))
            {
                clonedProject = null;
                return false;
            }

            clonedProject = new CodeProject(currentProject.Name);
            foreach (CodeClass currentClass in currentProject.Classes)
            {
                CodeClass? oldClass = oldProject.Classes.FirstOrDefault(c => c.FullName == currentClass.FullName);
                if (oldClass == null)
                {
                    // it's a new class, so add it
                    clonedProject.Classes.Add(currentClass);
                    continue;
                }

                if (CloneWithChanges(currentClass, oldClass, out CodeClass? clonedClass))
                {
                    clonedProject.Classes.Add(clonedClass!);
                }
            }

            return true;
        }

        private bool CloneWithChanges(CodeClass currentClass, CodeClass oldClass, out CodeClass? clonedClass)
        {
            if (currentClass.Equals(oldClass))
            {
                clonedClass = null;
                return false;
            }

            clonedClass = new CodeClass(currentClass.Namespace, currentClass.Name, currentClass.BaseType, currentClass.InterfaceTypes);

            foreach (CodeProperty currentProperty in currentClass.Properties)
            {
                CodeProperty? oldProperty = oldClass.Properties.FirstOrDefault(c => c.Name == currentProperty.Name);
                if (oldProperty == null)
                {
                    // it's a new property, so add it
                    clonedClass.Properties.Add(currentProperty);
                    continue;
                }

                if (CloneWithChanges(currentProperty, oldProperty, out CodeProperty? clonedProperty))
                {
                    clonedClass.Properties.Add(clonedProperty!);
                }
            }

            foreach (CodeMethod currentMethod in currentClass.Methods)
            {
                CodeMethod? oldMethod = oldClass.Methods.FirstOrDefault(c => c.Name == currentMethod.Name);
                if (oldMethod == null)
                {
                    // it's a new property, so add it
                    clonedClass.Methods.Add(currentMethod);
                    continue;
                }

                if (CloneWithChanges(currentMethod, oldMethod, out CodeMethod? clonedMethod))
                {
                    clonedClass.Methods.Add(clonedMethod!);
                }
            }

            return true;
        }

        private bool CloneWithChanges(CodeProperty currentProperty, CodeProperty oldProperty, out CodeProperty? clonedProperty)
        {
            if (currentProperty.Equals(oldProperty))
            {
                clonedProperty = null;
                return false;
            }

            clonedProperty = new CodeProperty(currentProperty.Type, currentProperty.Name);

            return true;
        }

        private bool CloneWithChanges(CodeMethod currentMethod, CodeMethod oldMethod, out CodeMethod? clonedMethod)
        {
            if (currentMethod.Equals(oldMethod))
            {
                clonedMethod = null;
                return false;
            }

            clonedMethod = new CodeMethod(currentMethod.Modifiers, currentMethod.ReturnType, currentMethod.Name, currentMethod.Hash);
            return true;
        }
    }
}
