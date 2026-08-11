using System.Diagnostics;
using NextGenSoftware.Utilities;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.STAR.Interfaces;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;
using NextGenSoftware.OASIS.STAR.DNA;
using Ipfs.CoreApi;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class OAPPs
    {
        public override async Task<OASISResult<InstalledOAPP>> DownloadAndInstallAsync(string idOrName = "", InstallMode installMode = InstallMode.DownloadAndInstall, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<InstalledOAPP> installResult = await base.DownloadAndInstallAsync(idOrName, installMode, providerType);

            if (installResult != null && installResult.Result != null && !installResult.IsError)
            {
                //Install any dependencies that are required for the OAPP to run (such as runtimes etc).
                OASISResult<bool> installRuntimesResult = await STARCLI.Runtimes.InstallOASISAndSTARRuntimesAsync(installResult.Result.STARNETDNA, installResult.Result.InstalledPath, InstallRuntimesFor.OAPP, providerType);

                if (!(installRuntimesResult != null && installRuntimesResult.Result && !installRuntimesResult.IsError))
                {
                    CLIEngine.ShowErrorMessage($"Error occured installing dependent runtimes for OAPP. Reason: {installRuntimesResult.Message}. Please install these manually using the sub-command 'runtime install'");
                    installResult.IsError = true;
                    installResult.Message = installRuntimesResult.Message;
                }
            }

            return installResult;
        }

        public override async Task ShowAsync<OAPP>(OAPP oapp, bool showHeader = true, bool showFooter = true, bool showNumbers = false, int number = 0, bool showDetailedInfo = false, int displayFieldLength = DEFAULT_FIELD_LENGTH, object customData = null)
        {
            if (DisplayFieldLength > displayFieldLength)
                displayFieldLength = DisplayFieldLength;

            if (showHeader)
                CLIEngine.ShowDivider();

            Console.WriteLine("");

            if (showNumbers)
                DisplayProperty("Number", number.ToString(), displayFieldLength);

            CLIEngine.ShowMessage(string.Concat($"Id:".PadRight(displayFieldLength), oapp.STARNETDNA.Id != Guid.Empty ? oapp.STARNETDNA.Id : "None"), false);
            DisplayProperty("Name", !string.IsNullOrEmpty(oapp.STARNETDNA.Name) ? oapp.STARNETDNA.Name : "None", displayFieldLength);
            DisplayProperty("Description", !string.IsNullOrEmpty(oapp.STARNETDNA.Description) ? oapp.STARNETDNA.Description : "None", displayFieldLength);
            DisplayProperty("Type", oapp.STARNETDNA.STARNETHolonType, displayFieldLength);
            DisplayProperty("Category", oapp.STARNETDNA.STARNETCategory.ToString(), displayFieldLength);
            DisplayProperty("Genesis Type", ParseMetaDataForEnum(oapp.MetaData, "GenesisType", typeof(GenesisType)), displayFieldLength);
            DisplayProperty("Celestial Body Id", ParseMetaData(oapp.MetaData, "CelestialBodyId"), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("OAPP TEMPLATE", "", displayFieldLength, false);
            DisplayProperty("Id", ParseMetaData(oapp.MetaData, "OAPPTemplateId"), displayFieldLength);
            DisplayProperty("Name", ParseMetaData(oapp.MetaData, "OAPPTemplateName"), displayFieldLength);
            DisplayProperty("Description", ParseMetaData(oapp.MetaData, "OAPPTemplateDescription"), displayFieldLength);
            DisplayProperty("Category", ParseMetaDataForEnum(oapp.MetaData, "OAPPTemplateType", typeof(OAPPTemplateType)), displayFieldLength);
            DisplayProperty("Version", ParseMetaData(oapp.MetaData, "OAPPTemplateVersion"), displayFieldLength);
            DisplayProperty("Version Sequence", ParseMetaData(oapp.MetaData, "OAPPTemplateVersionSequence"), displayFieldLength);
            DisplayProperty("Installed Path", ParseMetaData(oapp.MetaData, "OAPPTemplateInstalledPath"), displayFieldLength);
            ShowHolonMetaTagMappings(oapp.STARNETDNA.MetaTagMappings.MetaHolonTags, showDetailedInfo, displayFieldLength);
            ShowMetaTagMappings(oapp.STARNETDNA.MetaTagMappings.MetaTags, showDetailedInfo, displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("CELESTIAL BODY META DATA DNA", "", displayFieldLength, false);
            DisplayProperty("Id", ParseMetaData(oapp.MetaData, "CelestialBodyMetaDataId"), displayFieldLength);
            DisplayProperty("Name", ParseMetaData(oapp.MetaData, "CelestialBodyMetaDataName"), displayFieldLength);
            DisplayProperty("Description", ParseMetaData(oapp.MetaData, "CelestialBodyMetaDataDescription"), displayFieldLength);
            DisplayProperty("Type", ParseMetaData(oapp.MetaData, "CelestialBodyMetaDataType"), displayFieldLength);
            DisplayProperty("Version", ParseMetaData(oapp.MetaData, "CelestialBodyMetaDataVersion"), displayFieldLength);
            DisplayProperty("Version Sequence", ParseMetaData(oapp.MetaData, "CelestialBodyMetaDataVersionSequence"), displayFieldLength);
            DisplayProperty("Installed Path", ParseMetaData(oapp.MetaData, "CelestialBodyMetaDataInstalledPath"), displayFieldLength);
            DisplayProperty("Generated Path", ParseMetaData(oapp.MetaData, "CelestialBodyMetaDataGeneratedPath"), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("Our World Lat/Long", ParseMetaDataForLatLong(oapp.MetaData, "OurWorldLat", "OurWorldLong"), displayFieldLength);
            DisplayProperty("Our World 3D Object", ParseMetaDataForBinaryUploadAndURI(oapp.MetaData, "OurWorld3dObject", "OurWorld3dObjectURI"), displayFieldLength);
            DisplayProperty("Our World 2D Sprite", ParseMetaDataForBinaryUploadAndURI(oapp.MetaData, "OurWorld2dSprite", "OurWorld2dSpriteURI"), displayFieldLength);
            DisplayProperty("One World Lat/Long", ParseMetaDataForLatLong(oapp.MetaData, "OneWorldLat", "OneWorldLong"), displayFieldLength);
            DisplayProperty("One World 3D Object", ParseMetaDataForBinaryUploadAndURI(oapp.MetaData, "OneWorld3dObject", "OneWorld3dObjectURI"), displayFieldLength);
            DisplayProperty("One World 2D Sprite", ParseMetaDataForBinaryUploadAndURI(oapp.MetaData, "OneWorld2dSprite", "OneWorld2dSpriteURI"), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("Source Path", !string.IsNullOrEmpty(oapp.STARNETDNA.SourcePath) ? oapp.STARNETDNA.SourcePath : "None", displayFieldLength);
            DisplayProperty("Published On", oapp.STARNETDNA.PublishedOn != DateTime.MinValue ? oapp.STARNETDNA.PublishedOn.ToString() : "None", displayFieldLength);
            DisplayProperty("Published By", oapp.STARNETDNA.PublishedByAvatarId != Guid.Empty ? string.Concat(oapp.STARNETDNA.PublishedByAvatarUsername, " (", oapp.STARNETDNA.PublishedByAvatarId.ToString(), ")") : "None", displayFieldLength);
            DisplayProperty("Published Path", !string.IsNullOrEmpty(oapp.STARNETDNA.PublishedPath) ? oapp.STARNETDNA.PublishedPath : "None", displayFieldLength);
            DisplayProperty("Filesize", oapp.STARNETDNA.FileSize > 0 ? oapp.STARNETDNA.FileSize.ToString() : "None", displayFieldLength);
            DisplayProperty("Published On STARNET", oapp.STARNETDNA.PublishedOnSTARNET ? "True" : "False", displayFieldLength);
            DisplayProperty("Published To Cloud", oapp.STARNETDNA.PublishedToCloud ? "True" : "False", displayFieldLength);
            DisplayProperty("Published To OASIS Provider", oapp.STARNETDNA.PublishedProviderType, displayFieldLength);
            DisplayProperty("Launch Target", !string.IsNullOrEmpty(oapp.STARNETDNA.LaunchTarget) ? oapp.STARNETDNA.LaunchTarget : "None", displayFieldLength);
            DisplayProperty($"{STARNETManager.STARNETHolonUIName} Version", oapp.STARNETDNA.Version, displayFieldLength);
            DisplayProperty("Version Sequence", oapp.STARNETDNA.VersionSequence.ToString(), displayFieldLength);
            DisplayProperty("Number Of Versions", oapp.STARNETDNA.NumberOfVersions.ToString(), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("SELF CONTAINED", "", displayFieldLength, false);
            DisplayProperty("Published Path", ParseMetaData(oapp.MetaData, "SelfContainedPublishedPath"), displayFieldLength);
            DisplayProperty("Filesize", ParseMetaData(oapp.MetaData, "SelfContainedFileSize"), displayFieldLength);
            DisplayProperty("Published To Cloud", ParseMetaData(oapp.MetaData, "SelfContainedPublishedToCloud", "False"), displayFieldLength);
            DisplayProperty("Published To OASIS Provider", ParseMetaData(oapp.MetaData, "SelfContainedPublishedProviderType"), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("SELF CONTAINED FULL", "", displayFieldLength, false);
            DisplayProperty("Published Path", ParseMetaData(oapp.MetaData, "SelfContainedFullPublishedPath"), displayFieldLength);
            DisplayProperty("Filesize", ParseMetaData(oapp.MetaData, "SelfContainedFullFileSize"), displayFieldLength);
            DisplayProperty("Published To Cloud", ParseMetaData(oapp.MetaData, "SelfContainedFullPublishedToCloud", "False"), displayFieldLength);
            DisplayProperty("Published To OASIS Provider", ParseMetaData(oapp.MetaData, "SelfContainedFullPublishedProviderType"), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("SOURCE CODE ONLY", "", displayFieldLength, false);
            DisplayProperty("Published Path", ParseMetaData(oapp.MetaData, "SourcePublishedPath"), displayFieldLength);
            DisplayProperty("Filesize", ParseMetaData(oapp.MetaData, "SourceFileSize"), displayFieldLength);
            DisplayProperty("Published On STARNET", ParseMetaData(oapp.MetaData, "SourcePublishedOnSTARNET", "False"), displayFieldLength);
            DisplayProperty("Public On STARNET", ParseMetaData(oapp.MetaData, "SourcePublicOnSTARNET", "False"), displayFieldLength);


            //DisplayProperty("OASIS Holon Version:                        ", oapp.Version), displayFieldLength);
            //DisplayProperty("OASIS Holon VersionId:                      ", oapp.VersionId), displayFieldLength);
            //DisplayProperty("OASIS Holon PreviousVersionId:              ", oapp.PreviousVersionId), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("Downloads", oapp.STARNETDNA.Downloads.ToString(), displayFieldLength);
            DisplayProperty("Installs", oapp.STARNETDNA.Installs.ToString(), displayFieldLength);
            DisplayProperty("Total Downloads", oapp.STARNETDNA.TotalDownloads.ToString(), displayFieldLength);
            DisplayProperty("Total Installs", oapp.STARNETDNA.TotalInstalls.ToString(), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("OASIS Runtime Version", oapp.STARNETDNA.OASISRuntimeVersion.ToString(), displayFieldLength);
            DisplayProperty("OASIS API Version", oapp.STARNETDNA.OASISAPIVersion.ToString(), displayFieldLength);
            DisplayProperty("COSMIC Version", oapp.STARNETDNA.COSMICVersion.ToString(), displayFieldLength);
            DisplayProperty("STAR Runtime Version", oapp.STARNETDNA.STARRuntimeVersion.ToString(), displayFieldLength);
            DisplayProperty("STAR ODK Version", oapp.STARNETDNA.STARODKVersion.ToString(), displayFieldLength);
            DisplayProperty("STARNET Version", oapp.STARNETDNA.STARNETVersion.ToString(), displayFieldLength);
            DisplayProperty("STAR API Version", oapp.STARNETDNA.STARAPIVersion.ToString(), displayFieldLength);
            DisplayProperty(".NET Version", oapp.STARNETDNA.DotNetVersion.ToString(), displayFieldLength);

            Console.WriteLine("");
            DisplayProperty("Created On", oapp.STARNETDNA.CreatedOn != DateTime.MinValue ? oapp.STARNETDNA.CreatedOn.ToString() : "None", displayFieldLength);
            DisplayProperty("Created By", oapp.STARNETDNA.CreatedByAvatarId != Guid.Empty ? string.Concat(oapp.STARNETDNA.CreatedByAvatarUsername, " (", oapp.STARNETDNA.CreatedByAvatarId.ToString(), ")") : "None", displayFieldLength);
            DisplayProperty("Modified On", oapp.STARNETDNA.ModifiedOn != DateTime.MinValue ? oapp.STARNETDNA.CreatedOn.ToString() : "None", displayFieldLength);
            DisplayProperty("Modified By", oapp.STARNETDNA.ModifiedByAvatarId != Guid.Empty ? string.Concat(oapp.STARNETDNA.ModifiedByAvatarUsername, " (", oapp.STARNETDNA.ModifiedByAvatarId.ToString(), ")") : "None", displayFieldLength);
            DisplayProperty("Active", oapp.MetaData != null && oapp.MetaData.ContainsKey("Active") && oapp.MetaData["Active"] != null && oapp.MetaData["Active"].ToString() == "1" ? "True" : "False", displayFieldLength);

            ShowAllDependencies(oapp, showDetailedInfo, displayFieldLength);

            if (customData != null)
            {
                List<IZome> zomes = customData as List<IZome>;

                if (zomes != null && zomes.Count > 0)
                {
                    Console.WriteLine("");
                    STARCLI.Zomes.ShowZomesAndHolons(zomes);
                }
            }

            if (showFooter)
                CLIEngine.ShowDivider();
        }

    }
}
