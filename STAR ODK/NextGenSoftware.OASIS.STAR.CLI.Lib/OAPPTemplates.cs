using NextGenSoftware.OASIS.API.ONODE.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class OAPPTemplates : STARUIBase<OAPPTemplate, DownloadedMission, InstalledMission>
    {
        public OAPPTemplates(Guid avatarId) : base(new API.ONODE.Core.Managers.OAPPTemplateManager(avatarId),
            "Welcome to the OAPP Template Wizard", new List<string> 
            {
                "This wizard will allow you create an OAPP (Moon, Planet, Star & More) which will appear in the MagicVerse within the OASIS Omniverse.",
                "The OAPP Template can be created from anything you like such as a website, javascript template, game, app, service, etc in any language, platform or OS.",
                "You simply need to add specefic STAR ODK OAPP Template reserved tags where dynamic data will be injected in from the OAPP meta data.",
                "The wizard will create an empty folder with a OAPPTemplateDNA.json file in it. You then simply place any files/folders you need into this folder.",
                "Finally you run the sub-command 'oapp template publish' to convert the folder containing the OAPP Template (can contain any number of files and sub-folders) into a OAPP Template file (.oapptemplate).",
                "You can then share the .oapptemplate file with others from which you can create OAPP's from. You can also optionally choose to upload the .oapptemplate file to the STARNET store so others can search, download and install the OAPP Template. They can then create OAPP's from the template.",
            },
            STAR.STARDNA.DefaultOAPPTemplatesSourcePath, "DefaultOAPPTemplatesSourcePath",
            STAR.STARDNA.DefaultOAPPTemplatesPublishedPath, "DefaultOAPPTemplatesPublishedPath",
            STAR.STARDNA.DefaultOAPPTemplatesDownloadedPath, "DefaultOAPPTemplatesDownloadedPath",
            STAR.STARDNA.DefaultOAPPTemplatesInstalledPath, "DefaultOAPPTemplatesInstalledPath")
        { }
    }
}