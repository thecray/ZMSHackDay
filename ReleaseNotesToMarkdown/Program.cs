using ZMSHackDay;

namespace ReleaseNotesToMarkdown
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Processor processor = new Processor();

            string inputFilename = "release-notes-3.1.695.0-3.1.778.0.json";
            string outputFilename = Path.GetFileNameWithoutExtension(inputFilename) + ".md";

            processor.Run(inputFilename, outputFilename);
        }
    }
}
