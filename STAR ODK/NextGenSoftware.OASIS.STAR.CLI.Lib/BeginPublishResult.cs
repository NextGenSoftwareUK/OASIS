
namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class BeginPublishResult
    {
        public string SourcePath { get; set; }
        public string PublishedPath { get; set; }
        public bool SimpleWizard { get; set; }
        public string LaunchTarget { get; set; }
        public bool EmbedRuntimes { get; set; }
        public bool EmbedLibs { get; set; }
        public bool EmbedTemplates { get; set; }
    }
}
