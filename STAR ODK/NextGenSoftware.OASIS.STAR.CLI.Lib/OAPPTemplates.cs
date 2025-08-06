using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public class OAPPTemplates : STARNETUIBase<OAPPTemplate, DownloadedOAPPTemplate, InstalledOAPPTemplate, OAPPTemplateDNA>
    {
        public OAPPTemplates(Guid avatarId) : base(new OAPPTemplateManager(avatarId),
            "Welcome to the OAPP Template Wizard", new List<string> 
            {
                "This wizard will allow you create an OAPP Template from which OAPP's can be created from.",
                "You can also plug in different Runtimes into your OAPP to allow it to run on different platforms, devices and operating systems. You can make unique cominations of different OAPP Template's and Runtimes's to create OAPP's from so you can re-use the same OAPP Tempalte with a different backend/runtime if you wish, the possibilityies are endless! :)",
                "The OAPP Template can be created from anything you like such as a website, javascript template, game, app, service, etc in any language, platform or OS.",
                "You simply need to add specefic STAR ODK OAPP Template reserved tags where dynamic data will be injected in from the OAPP meta data.",
                "The wizard will create an empty folder with a OAPPTemplateDNA.json file in it. You then simply place any files/folders you need into this folder.",
                "Finally you run the sub-command 'oapp template publish' to convert the folder containing the OAPP Template (can contain any number of files and sub-folders) into a OAPP Template file (.oapptemplate) as well as optionally upload to STARNET.",
                "You can then share the .oapptemplate file with others across any platform or OS, who can then install the OAPP Template from the file using the sub-command 'oapp template install'. You can also optionally choose to upload the .oapptemplate file to STARNET so others can search, download and install the OAPP Template. They can then create OAPP's from the template.",
                "You can also optionally choose to upload the .oapptemplate file to the STARNET store so others can search, download and install the quest."
            },
            STAR.STARDNA.DefaultOAPPTemplatesSourcePath, "DefaultOAPPTemplatesSourcePath",
            STAR.STARDNA.DefaultOAPPTemplatesPublishedPath, "DefaultOAPPTemplatesPublishedPath",
            STAR.STARDNA.DefaultOAPPTemplatesDownloadedPath, "DefaultOAPPTemplatesDownloadedPath",
            STAR.STARDNA.DefaultOAPPTemplatesInstalledPath, "DefaultOAPPTemplatesInstalledPath")
        { }

        public override async Task<OASISResult<OAPPTemplate>> CreateAsync(object createParams, OAPPTemplate newHolon = null, bool showHeaderAndInro = true, bool checkIfSourcePathExists = true, object holonSubType = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<OAPPTemplate> createResult = await base.CreateAsync(createParams, newHolon, showHeaderAndInro, checkIfSourcePathExists, holonSubType, providerType);

            if (createResult != null && createResult.Result != null && !createResult.IsError)
                 await AddLibsRuntimesAndTemplatesAsync(createResult.Result.STARNETDNA, "OAPP Template", providerType);

            return createResult;
        }
    }
}