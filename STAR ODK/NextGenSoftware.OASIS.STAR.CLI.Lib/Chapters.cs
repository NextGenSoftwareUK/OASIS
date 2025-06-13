using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class Chapters : STARUIBase<Runtime, DownloadedQuest, InstalledRuntime>
    {
        public Chapters(Guid avatarId) : base(new API.ONODE.Core.Managers.RuntimeManager(avatarId),
            "Welcome to the Chapter Wizard", new List<string> 
            {
                "This wizard will allow you create a Chapter which can contain Mission's (which in turn contain Quest's)"
            },
            STAR.STARDNA.DefaultRuntimesSourcePath, "DefaultRuntimesSourcePath",
            STAR.STARDNA.DefaultRuntimesPublishedPath, "DefaultRuntimesPublishedPath",
            STAR.STARDNA.DefaultRuntimesDownloadedPath, "DefaultRuntimesDownloadedPath",
            STAR.STARDNA.DefaultRuntimesInstalledPath, "DefaultRuntimesInstalledPath")
        { }
    }
}