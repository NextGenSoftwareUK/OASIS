using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class OAPPTemplates : STARNETUIBase<OAPPTemplate, DownloadedOAPPTemplate, InstalledOAPPTemplate, OAPPTemplateDNA>
    public class OAPPTemplates : STARNETUIBase<OAPPTemplate, DownloadedOAPPTemplate, InstalledOAPPTemplate, STARNETDNA>
    {
        public OAPPTemplates(Guid avatarId, STARDNA STARDNA) : base(new OAPPTemplateManager(avatarId, STARDNA),
            "Welcome to the OAPP Template Wizard", new List<string> 
            {
                "This wizard will allow you create an OAPP Template from which OAPP's can be created from.",
                "You can also plug in different Runtimes into your OAPP to allow it to run on different platforms, devices and operating systems. You can make unique cominations of different OAPP Template's and Runtimes's to create OAPP's from so you can re-use the same OAPP Tempalte with a different backend/runtime if you wish, the possibilityies are endless! :)",
                "The OAPP Template can be created from anything you like such as a website, javascript template, game, app, service, etc in any language, platform or OS.",
                "You simply need to add specefic STAR ODK OAPP Template reserved tags where dynamic data will be injected in from the OAPP meta data.",
                //"",
                "{{CUSTOM_TAG_NAME}} = You can define custom holon tags that people can map the fields/properties (nodes) defined in the holons in the CelestialBodyMetaDataDNA when creating an OAPP. The tags can be called anything you like as long as they are contained inside the {{}} tags so {{MYTAG}} is valid. These will then appear during the OAPP Creation (Light) Wizard and be given the option to map.",
                "[[CUSTOM_TAG_NAME]] = You can define custom tags that people can map to any custom text they like when creating an OAPP. The tags can be called anything you like as long as they are contained inside the [[]] tags so [[MYTAG]] is valid. These will then appear during the OAPP Creation (Light) Wizard and be given the option to map.",
                //"",
                "In addition to custom tags, there are a number of built-in tags, these are as follows:",
                //"",
                "{INITCUSTOMTAGHOLONS} = This currently needs to be defined somewhere within the same code block BEFORE you use a custom holon tag (above). This defines and creates a new instance of any holons mapped to your tag. This requirement will be removed in future versions (holons will use instance singleton pattern so no need to define or create new instances).",
                "{OAPPNAMESPACE} = The namespace of the OAPP.",
                "{OAPPNAME} = The name of the OAPP.",
                "{CELESTIALBODY} = The name of the CelestialBody (same name as your OAPP) in the CelestialBodyMetaDataDNA. e.g. SuperMoon (SuperMoon superMoon = new SuperMoon();)",
                "{CELESTIALBODYVAR} = The variable name of the CelestialBody (same name as your OAPP) in the CelestialBodyMetaDataDNA. e.g superMoon.",
                "//CelestialBodyOnly:BEGIN = Beginning of a block of code/content that will only be rendered if the GenesisMode (in the Light Wizard/OAPP Creation) is NOT GeneratedCodeOnly (i.e is a CelestialBody such as Moon, Planet, Star etc).",
                "//CelestialBodyOnly:END = End of the block above.",
                "//ZomesAndHolonsOnly: = Any code/content on this line will only be rendered if the GenesisMode is GeneratedCodeOnly.",
                "{ZOME1} = The name of the first Zome in the CelestialBodyMetaDataDNA.",
                "{HOLON1} = The name of the first holon defined in your CelestialBodyMetaDataDNA.",
                "{HOLON1_STRINGPROPERTY1} = The name of the first string property/field defined in your first holon in the CelestialBodyMetaDataDNA.",
                "{HOLON1_INTPROPERTY1} = The name of the first int property/field defined in your first holon in the CelestialBodyMetaDataDNA.",
                "{HOLON1_BOOLEANPROPERTY1} = The name of the first boolean property/field defined in your first holon in the CelestialBodyMetaDataDNA.",
                "{HOLON1_DATETIMEPROPERTY1} = The name of the first datetime property/field defined in your first holon in the CelestialBodyMetaDataDNA.",
                "{HOLON2 = The name of the second holon defined in your CelestialBodyMetaDataDNA.",
                "{HOLON2_STRINGPROPERTY1} = The name of the first string property/field defined in your second holon in the CelestialBodyMetaDataDNA.",
                "{HOLON2_STRINGPROPERTY2} = The name of the second string property/field defined in your second holon in the CelestialBodyMetaDataDNA.",
                "etc...",
                "More to come soon! ;-)",
                //"",
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
            {
                //Install any dependencies that are required for the OAPP Template to run (such as runtimes etc).
                OASISResult<bool> installRuntimesResult = await STARCLI.Runtimes.InstallOASISAndSTARRuntimesAsync(createResult.Result.STARNETDNA, createResult.Result.STARNETDNA.SourcePath, InstallRuntimesFor.OAPPTemplate, providerType);

                if (!(installRuntimesResult != null && installRuntimesResult.Result && !installRuntimesResult.IsError))
                {
                    CLIEngine.ShowErrorMessage($"Error occured installing dependent runtimes for OAPP Template. Reason: {installRuntimesResult.Message}. Please install these manually using the sub-command 'runtime install' or below when asked if you wish to install any custom runtimes.");
                    //createResult.IsError = true;
                    //createResult.Message = installRuntimesResult.Message;
                }

                //await AddDependenciesAsync(createResult.Result.STARNETDNA, "OAPP Template", providerType);
                await AddDependenciesAsync(createResult.Result.STARNETDNA, providerType);
            }

            return createResult;
        }

        public override async Task<OASISResult<InstalledOAPPTemplate>> DownloadAndInstallAsync(string idOrName = "", InstallMode installMode = InstallMode.DownloadAndInstall, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<InstalledOAPPTemplate> installResult = await base.DownloadAndInstallAsync(idOrName, installMode, providerType);

            if (installResult != null && installResult.Result != null && !installResult.IsError)
            {
                //Install any dependencies that are required for the OAPP Template to run (such as runtimes etc).
                OASISResult<bool> installRuntimesResult = await STARCLI.Runtimes.InstallOASISAndSTARRuntimesAsync(installResult.Result.STARNETDNA, installResult.Result.InstalledPath, InstallRuntimesFor.OAPPTemplate, providerType);

                if (!(installRuntimesResult != null && installRuntimesResult.Result && !installRuntimesResult.IsError))
                {
                    CLIEngine.ShowErrorMessage($"Error occured installing dependent runtimes for OAPP Template. Reason: {installRuntimesResult.Message}. Please install these manually using the sub-command 'runtime install'");
                    installResult.IsError = true;
                    installResult.Message = installRuntimesResult.Message;
                }
            }

            return installResult;
        }
    }
}