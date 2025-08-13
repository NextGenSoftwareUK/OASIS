using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class Plugins : STARNETUIBase<Plugin, DownloadedPlugin, InstalledPlugin, PluginDNA>
    public class Plugins : STARNETUIBase<Plugin, DownloadedPlugin, InstalledPlugin, STARNETDNA>
    {
        public Plugins(Guid avatarId) : base(new API.ONODE.Core.Managers.PluginManager(avatarId),
            "Welcome to the Plugin Wizard", new List<string> 
            {
                "This wizard will allow you create a Plugin, that allow you to extend STAR & STARNET.",
                "The wizard will create an empty folder with a PluginDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the mission into this folder.",
                "Finally you run the sub-command 'plugin publish' to convert the folder containing the plugin (can contain any number of files and sub-folders) into a OASIS Plugin file (.oplugin) as well as optionally upload to STARNET.",
                "You can then share the .oplugin file with others across any platform or OS, who can then install the Plugin from the file using the sub-command 'plugin install'.",
                "You can also optionally choose to upload the .oplugin file to the STARNET store so others can search, download and install the plugin."
            },
            STAR.STARDNA.DefaultPluginsSourcePath, "DefaultPluginsSourcePath",
            STAR.STARDNA.DefaultPluginsPublishedPath, "DefaultPluginsPublishedPath",
            STAR.STARDNA.DefaultPluginsDownloadedPath, "DefaultPluginsDownloadedPath",
            STAR.STARDNA.DefaultPluginsInstalledPath, "DefaultPluginsInstalledPath")
        { }
    }
}