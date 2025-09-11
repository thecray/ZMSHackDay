namespace ZMSHackDay
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ReleaseNotesToMarkdown releaseNotesToMarkdown = new ReleaseNotesToMarkdown();
            releaseNotesToMarkdown.Run("release-notes-3.1.695.0-3.1.778.0.json", "release-notes-3.1.695.0-3.1.778.0-structured.json", "release-notes-3.1.698.0-3.1.778.0.md");
        }
    }
}
