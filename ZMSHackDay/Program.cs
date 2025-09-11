namespace ZMSHackDay
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ReleaseNotesToMarkdown releaseNotesToMarkdown = new ReleaseNotesToMarkdown();
            releaseNotesToMarkdown.Run("release-notes-3.1.776.0-3.1.779.0-with-version.json", "release-notes-3.1.776.0-3.1.779.0-ordered.json");
        }
    }
}
