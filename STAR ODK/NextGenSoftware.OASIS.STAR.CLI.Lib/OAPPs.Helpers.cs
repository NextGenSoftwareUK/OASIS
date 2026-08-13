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

        private void DisplaySTARNETHolonMetaData(ISTARNETDependency metaData, int displayFieldLength)
        {
            DisplayProperty("Id", metaData.STARNETHolonId.ToString(), displayFieldLength);
            DisplayProperty("Name", metaData.Name, displayFieldLength);
            DisplayProperty("Description", metaData.Description, displayFieldLength);
            DisplayProperty("Version", metaData.Version, displayFieldLength);
            DisplayProperty("Version Sequence", metaData.VersionSequence.ToString(), displayFieldLength);
            DisplayProperty("Installed From", metaData.InstalledFrom, displayFieldLength);
            DisplayProperty("Installed To", metaData.InstalledTo, displayFieldLength);
            Console.WriteLine("");
        }

        private async Task<(OASISResult<CoronalEjection>, CelestialBodyMetaDataDNA)> CreateMetaDataOnSTARNETAsync(OASISResult<CoronalEjection> lightResult, IGenerateMetaDataDNAResult generateResult, GenesisType genesisType, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            CelestialBodyMetaDataDNA celestialBodyMetaDataDNA = null;

            if (CLIEngine.GetConfirmation("Would you like to upload the generated metadata DNA to STARNET so you or others (if you choose to make it public) can re-use for other OAPP's?"))
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Would you like to upload the CelestialBody generated metadata DNA to STARNET?"))
                {
                    Console.WriteLine("");
                    CelestialBodyType celestialBodyType = CelestialBodyType.Moon;

                    switch (genesisType)
                    {
                        case GenesisType.Moon:
                            celestialBodyType = CelestialBodyType.Moon;
                            break;

                        case GenesisType.Planet:
                            celestialBodyType = CelestialBodyType.Planet;
                            break;

                        case GenesisType.Star:
                            celestialBodyType = CelestialBodyType.Star;
                            break;

                        case GenesisType.SuperStar:
                            celestialBodyType = CelestialBodyType.SuperStar;
                            break;

                        case GenesisType.GrandSuperStar:
                            celestialBodyType = CelestialBodyType.GrandSuperStar;
                            break;
                    }

                    OASISResult<CelestialBodyMetaDataDNA> createResult = await STARCLI.CelestialBodiesMetaDataDNA.CreateAsync(holonSubType: celestialBodyType, providerType : providerType);

                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                    {
                        celestialBodyMetaDataDNA = createResult.Result;

                        try
                        {
                            DirectoryHelper.CopyFilesRecursively(generateResult.CelestialBodyMetaDataDNAPath, createResult.Result.STARNETDNA.SourcePath);
                            CLIEngine.ShowSuccessMessage("CelestialBody MetaData DNA successfully created on STARNET!");

                            if (CLIEngine.GetConfirmation("Would you like to publish it now?"))
                            {
                                Console.WriteLine("");
                                OASISResult<CelestialBodyMetaDataDNA> publishResult = await STARCLI.CelestialBodiesMetaDataDNA.PublishAsync(createResult.Result.STARNETDNA.SourcePath, false, DefaultLaunchMode.None, providerType: providerType);

                                if (publishResult != null && publishResult.Result != null && !publishResult.IsError)
                                    CLIEngine.ShowSuccessMessage("CelestialBody MetaData DNA successfully uploaded to STARNET!");
                                else
                                    OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured publishing the CelestialBody MetaData DNA in STAR.CLI.Lib.CelestialBodiesMetaDataDNA.PublishAsync. Reason: {publishResult.Message}");
                            }
                            else
                                Console.WriteLine("");
                        }
                        catch (Exception e)
                        {
                            OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured attempting to copy the CelestialBodyMetaDataDNA from {generateResult.CelestialBodyMetaDataDNAPath} to {createResult.Result.STARNETDNA.SourcePath}. Reason: {e}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured in STAR.CLI.Lib.CelestialBodiesMetaDataDNA.CreateAsync. Reason: {createResult.Message}");
                }

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Would you like to upload the Zome generated metadata DNA to STARNET?"))
                {
                    Console.WriteLine("");
                    OASISResult<ZomeMetaDataDNA> createResult = await STARCLI.ZomesMetaDataDNA.CreateAsync(new STARNETCreateOptions<ZomeMetaDataDNA, STARNETDNA>() { CheckIfSourcePathExists = false }, providerType: providerType);

                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                    {
                        try
                        {
                            DirectoryHelper.CopyFilesRecursively(generateResult.ZomeMetaDataDNAPath, createResult.Result.STARNETDNA.SourcePath);
                            CLIEngine.ShowSuccessMessage("Zome MetaData DNA successfully created on STARNET!");

                            if (CLIEngine.GetConfirmation("Would you like to publish it/them now?"))
                            {
                                Console.WriteLine("");
                                OASISResult<ZomeMetaDataDNA> publishResult = await STARCLI.ZomesMetaDataDNA.PublishAsync(createResult.Result.STARNETDNA.SourcePath, defaultLaunchMode: DefaultLaunchMode.None, providerType: providerType);

                                if (publishResult != null && publishResult.Result != null && !publishResult.IsError)
                                    CLIEngine.ShowSuccessMessage("Zome MetaData DNA successfully uploaded to STARNET!");
                                else
                                    OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured publishing the Zome MetaData DNA in STAR.CLI.Lib.ZomesMetaDataDNA.PublishAsync. Reason: {publishResult.Message}");
                            }
                            else
                                Console.WriteLine("");
                        }
                        catch (Exception e)
                        {
                            OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured attempting to copy the ZomeMetaDataDNA from {generateResult.CelestialBodyMetaDataDNAPath} to {createResult.Result.STARNETDNA.SourcePath}. Reason: {e}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured in STAR.CLI.Lib.ZomesMetaDataDNA.CreateAsync. Reason: {createResult.Message}");
                }

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Would you like to upload the Holon generated metadata DNA to STARNET?"))
                {
                    Console.WriteLine("");
                    OASISResult<HolonMetaDataDNA> createResult = await STARCLI.HolonsMetaDataDNA.CreateAsync(new STARNETCreateOptions<HolonMetaDataDNA, STARNETDNA>() { CheckIfSourcePathExists = false }, providerType: providerType);

                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                    {
                        try
                        {
                            DirectoryHelper.CopyFilesRecursively(generateResult.HolonMetaDataDNAPath, createResult.Result.STARNETDNA.SourcePath);
                            CLIEngine.ShowSuccessMessage("Holon MetaData DNA successfully created on STARNET!");

                            if (CLIEngine.GetConfirmation("Would you like to publish it/them now?"))
                            {
                                Console.WriteLine("");
                                OASISResult<HolonMetaDataDNA> publishResult = await STARCLI.HolonsMetaDataDNA.PublishAsync(createResult.Result.STARNETDNA.SourcePath, defaultLaunchMode: DefaultLaunchMode.None, providerType: providerType);

                                if (publishResult != null && publishResult.Result != null && !publishResult.IsError)
                                    CLIEngine.ShowSuccessMessage("Holon MetaData DNA successfully uploaded to STARNET!");
                                else
                                    OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured publishing the Holon MetaData DNA in STAR.CLI.Lib.HolonsMetaDataDNA.PublishAsync. Reason: {publishResult.Message}");
                            }
                            else
                                Console.WriteLine("");
                        }
                        catch (Exception e)
                        {
                            OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured attempting to copy the ZomeMetaDataDNA from {generateResult.HolonMetaDataDNAPath} to {createResult.Result.STARNETDNA.SourcePath}. Reason: {e}");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured in STAR.CLI.Lib.HolonsMetaDataDNA.CreateAsync. Reason: {createResult.Message}");
                }
                else
                    Console.WriteLine("");
            }
            else
                Console.WriteLine("");

            return (lightResult, celestialBodyMetaDataDNA);
        }

        private OASISResult<CoronalEjection> CopyGeneratedCodeToSTARNET<T>(OASISResult<CoronalEjection> result, OASISResult<T> createResult, string holonDisplayName, string sourcePath, string generatedCodeSubFolder, string errorMessage, ProviderType providerType = ProviderType.Default) where T : ISTARNETHolon
        {
            string path = Path.Combine(sourcePath, STAR.STARDNA.OAPPGeneratedCodeFolder, "CSharp", generatedCodeSubFolder);

            try
            {
                DirectoryHelper.CopyFilesRecursively(path, createResult.Result.STARNETDNA.SourcePath);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to copy the {holonDisplayName} from {path} to {createResult.Result.STARNETDNA.SourcePath}. Reason: {e}");
            }

            path = Path.Combine(sourcePath, STAR.STARDNA.OAPPGeneratedCodeFolder, "CSharp", "Interfaces", generatedCodeSubFolder);

            try
            {
                DirectoryHelper.CopyFilesRecursively(path, createResult.Result.STARNETDNA.SourcePath);
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} Error occured attempting to copy the {holonDisplayName} from {path} to {createResult.Result.STARNETDNA.SourcePath}. Reason: {e}");
            }

            return result;
        }

        private async Task<OASISResult<CoronalEjection>> CreateOAPPComponentsOnSTARNETAsync(OASISResult<CoronalEjection> lightResult, string OAPPSourcePath, string errorMessage, ProviderType providerType = ProviderType.Default)
        {
            if (CLIEngine.GetConfirmation("Would you like to upload the generated OAPP Components (CelestialBody, Zomes & Holons) to STARNET so you or others (if you choose to make it public) can re-use for other OAPP's?"))
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Would you like to upload the CelestialBody generated to STARNET?"))
                {
                    Console.WriteLine("");
                    //OASISResult<STARCelestialBody> createResult = await STARCLI.CelestialBodies.CreateAsync(new STARNETCreateOptions<STARCelestialBody, STARNETDNA>() { CheckIfSourcePathExists = false, DefaultSTARNETCategory = Enum.GetName(typeof(HolonType), lightResult.Result.CelestialBody.HolonType) }, holonSubType: Enum.GetName(typeof(HolonType), lightResult.Result.CelestialBody.HolonType), providerType: providerType);
                    //OASISResult<STARCelestialBody> createResult = await STARCLI.CelestialBodies.CreateAsync(new STARNETCreateOptions<STARCelestialBody, STARNETDNA>() { CheckIfSourcePathExists = false }, holonSubType: Enum.GetName(typeof(HolonType), lightResult.Result.CelestialBody.HolonType), providerType: providerType);
                    OASISResult<STARCelestialBody> createResult = await STARCLI.CelestialBodies.CreateAsync(new STARNETCreateOptions<STARCelestialBody, STARNETDNA>() { CheckIfSourcePathExists = false }, holonSubType: lightResult.Result.CelestialBody.HolonType, providerType: providerType);

                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                    {
                        lightResult = CopyGeneratedCodeToSTARNET(lightResult, createResult, "CelestialBody", OAPPSourcePath, "CelestialBodies", errorMessage, providerType);

                        if (lightResult != null && lightResult.Result != null && !lightResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage("CelestialBody successfully created on STARNET!");

                            if (CLIEngine.GetConfirmation("Would you like to publish it now?"))
                            {
                                Console.WriteLine("");
                                OASISResult<STARCelestialBody> publishResult = await STARCLI.CelestialBodies.PublishAsync(createResult.Result.STARNETDNA.SourcePath, defaultLaunchMode: DefaultLaunchMode.None, providerType: providerType);

                                if (publishResult != null && publishResult.Result != null && !publishResult.IsError)
                                    CLIEngine.ShowSuccessMessage("CelestialBody successfully uploaded to STARNET!");
                                else
                                    OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured publishing the Zome(s) in STAR.CLI.Lib.CelestialBodies.PublishAsync. Reason: {publishResult.Message}");
                            }
                            else
                                Console.WriteLine("");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured creating the STARNET CelestialBody in STAR.CLI.Lib.CelestialBodies.CreateAsync. Reason: {createResult.Message}");
                }

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Would you like to upload the Zome(s) generated to STARNET?"))
                {
                    OASISResult<STARZome> createResult = await STARCLI.Zomes.CreateAsync(new STARNETCreateOptions<STARZome, STARNETDNA>() { CheckIfSourcePathExists = false }, providerType: providerType);

                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                    {
                        lightResult = CopyGeneratedCodeToSTARNET(lightResult, createResult, "Zomes", OAPPSourcePath, "Zomes", errorMessage, providerType);
                        CLIEngine.ShowSuccessMessage("Zome(s) successfully created on STARNET!");

                        if (CLIEngine.GetConfirmation("Would you like to publish it/them now?"))
                        {
                            Console.WriteLine("");
                            OASISResult<STARZome> publishResult = await STARCLI.Zomes.PublishAsync(createResult.Result.STARNETDNA.SourcePath, defaultLaunchMode: DefaultLaunchMode.None, providerType: providerType);

                            if (publishResult != null && publishResult.Result != null && !publishResult.IsError)
                                CLIEngine.ShowSuccessMessage("Zome(s) successfully uploaded to STARNET!");
                            else
                                OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured publishing the Zome(s) in STAR.CLI.Lib.Zomes.PublishAsync. Reason: {publishResult.Message}");
                        }
                        else
                            Console.WriteLine("");
                    }
                    else
                        OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured creating the Zome in STAR.CLI.Lib.Zomes.CreateAsync. Reason: {createResult.Message}");
                }

                Console.WriteLine("");
                if (CLIEngine.GetConfirmation("Would you like to upload the Holon(s) generated to STARNET?"))
                {
                    OASISResult<STARHolon> createResult = await STARCLI.Holons.CreateAsync(new STARNETCreateOptions<STARHolon, STARNETDNA>() { CheckIfSourcePathExists = false }, providerType: providerType);

                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                    {
                        lightResult = CopyGeneratedCodeToSTARNET(lightResult, createResult, "Holons", OAPPSourcePath, "Holons", errorMessage, providerType);

                        if (lightResult != null && lightResult.Result != null && !lightResult.IsError)
                        {
                            CLIEngine.ShowSuccessMessage("Holon(s) successfully created on STARNET!");

                            if (CLIEngine.GetConfirmation("Would you like to publish it/them now?"))
                            {
                                Console.WriteLine("");
                                OASISResult<STARHolon> publishResult = await STARCLI.Holons.PublishAsync(createResult.Result.STARNETDNA.SourcePath, defaultLaunchMode: DefaultLaunchMode.None, providerType: providerType);

                                if (publishResult != null && publishResult.Result != null && !publishResult.IsError)
                                    CLIEngine.ShowSuccessMessage("Holon(s) successfully uploaded to STARNET!");
                                else
                                    OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured publishing the Holon(s) in STAR.CLI.Lib.Holons.PublishAsync. Reason: {publishResult.Message}");

                                //    createResult.Result.STARNETDNA.IsPublic = CLIEngine.GetConfirmation("Would you like to make the Holon(s) public on STARNET?");
                                //else
                                //    createResult.Result.STARNETDNA.IsPublic = false;
                            }
                            else
                                Console.WriteLine("");
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref lightResult, $"{errorMessage} Error occured creating the Holon(s) in STAR.CLI.Lib.Holons.CreateAsync. Reason: {createResult.Message}");
                }
                else
                    Console.WriteLine("");
            }
            else
                Console.WriteLine("");

            return lightResult;
        }

        //private OASISResult<Dictionary<string, (string, string)>> MapCustomHolonMetaTagsToTemplate(IInstalledOAPPTemplate oappTemplate, Dictionary<string, IList<INode>> nodes)
        private OASISResult<List<MetaHolonTag>> MapCustomHolonMetaTagsToTemplate(IInstalledOAPPTemplate oappTemplate, Dictionary<string, IList<INode>> nodes)
        {
            //OASISResult<Dictionary<string, (string, string)>> result = new OASISResult<Dictionary<string, (string, string)>>(new Dictionary<string, (string, string)>());
            OASISResult<List<MetaHolonTag>> result = new OASISResult<List<MetaHolonTag>>(new List<MetaHolonTag>());

            try
            {
                int nodesTotal = CountNodes(nodes);
                OASISResult<List<string>> getCustomTagsResult = GetCustomTagsFromTemplate(oappTemplate.InstalledPath, new List<string>(), "{{", "}}");

                if (getCustomTagsResult != null && getCustomTagsResult.Result != null && !getCustomTagsResult.IsError)
                {
                    CLIEngine.ShowMessage(string.Concat($"Found {getCustomTagsResult.Result.Count} custom holon tag(s) in the OAPP Template '{oappTemplate.STARNETDNA.Name}'", getCustomTagsResult.Result.Count > 0 ? ":" : "."));

                    if (getCustomTagsResult.Result.Count > 0)
                    {
                        Console.WriteLine("");

                        foreach (string tag in getCustomTagsResult.Result)
                            CLIEngine.ShowMessage(tag, false);
                    }

                    CLIEngine.ShowMessage(string.Concat($"Found {nodesTotal} holon meta data node(s)", nodesTotal > 0 ? ":" : "."));
                    Console.WriteLine("");
                    CLIEngine.ShowMessage(string.Concat("HOLON".PadRight(20), "NODE".PadRight(20), "TYPE".PadRight(20)), false);
                    Console.WriteLine("");

                    foreach (string holonName in nodes.Keys)
                    {
                        foreach (INode node in nodes[holonName])
                            CLIEngine.ShowMessage(string.Concat(holonName.PadRight(20), node.NodeName.PadRight(20), Enum.GetName(typeof(NodeType), node.NodeType).PadRight(20)), false);
                    }

                    if (getCustomTagsResult.Result.Count > 0 && nodesTotal > 0 && CLIEngine.GetConfirmation("Would you like to map any of these tags to your holon meta data?"))
                    {
                        bool mapTags = true;
                        Console.WriteLine("");

                        do
                        {
                            string tag = "";
                            string metaField = "";
                            INode selectedNode = null;

                            do
                            {
                                tag = CLIEngine.GetValidInput("Please enter the tag you wish to map (case sensitive). Enter 'exit' to cancel:");

                                if (tag == "exit")
                                    break;

                                if (string.IsNullOrEmpty(tag) || !getCustomTagsResult.Result.Contains(tag))
                                    CLIEngine.ShowErrorMessage($"The tag '{tag}' was not found. Please try again.");

                                else if (result.Result.Any(x => x.MetaTag == tag))
                                {
                                    MetaHolonTag matchedTag = result.Result.FirstOrDefault(x => x.MetaTag == metaField);

                                    if (matchedTag != null)
                                        CLIEngine.ShowErrorMessage($"The tag '{tag}' has already been mapped to '{matchedTag.NodeName}'. Please try again.");
                                    else
                                        CLIEngine.ShowErrorMessage($"The tag '{tag}' has already been mapped. Please try again.");

                                    tag = "";
                                }

                            } while (!getCustomTagsResult.Result.Contains(tag));

                            if (tag != "exit")
                            {
                                do
                                {
                                    metaField = CLIEngine.GetValidInput("Please enter the holon meta data node (field) you wish to map to this tag (case sensitive). Enter 'exit' to cancel:");

                                    if (metaField == "exit")
                                        break;

                                    selectedNode = GetNode(metaField, nodes);

                                    //if (string.IsNullOrEmpty(metaField) || !IsNodeFound(metaField, nodes))
                                    if (string.IsNullOrEmpty(metaField) || selectedNode == null)
                                        CLIEngine.ShowErrorMessage($"The holon meta data node '{metaField}' was not found. Please try again.");
                                    
                                    else if (result.Result.Any(x => x.MetaTag == metaField))
                                    {
                                        MetaHolonTag matchedTag = result.Result.FirstOrDefault(x => x.MetaTag == metaField);

                                        if (matchedTag != null)
                                            CLIEngine.ShowErrorMessage($"The holon meta data node '{metaField}' has already been mapped to '{matchedTag.NodeName}'. Please try again.");
                                        else
                                            CLIEngine.ShowErrorMessage($"The holon meta data node'{metaField}' has already been mapped. Please try again.");

                                        metaField = "";
                                    }
                                    else if (selectedNode.NodeType != NodeType.String) //TODO: Add support for other types later!
                                    {
                                        CLIEngine.ShowErrorMessage($"The node must be a string, please try again.");
                                        metaField = "";
                                    }
                                } while (!IsNodeFound(metaField, nodes));
                            }

                            if (tag != "exit" && metaField != "exit" && CLIEngine.GetConfirmation($"Please confirm you wish to map the tag '{tag}' to the holon meta data node '{metaField}'?"))
                            {
                                //result.Result[tag] = (GetHolonThatNodeBelongsTo(metaField, nodes), metaField);
                                //result.Result.Add(new MetaHolonTag() { HolonName = GetHolonThatNodeBelongsTo(metaField, nodes), NodeName = metaField, NodeType = new EnumValue<NodeType>(selectedNode.NodeType), MetaTag = tag });
                                result.Result.Add(new MetaHolonTag() { HolonName = GetHolonThatNodeBelongsTo(metaField, nodes), NodeName = metaField, NodeType = Enum.GetName(typeof(NodeType), selectedNode.NodeType), MetaTag = tag });
                                Console.WriteLine("");
                                CLIEngine.ShowSuccessMessage($"Meta tag '{tag}' mapped to holon meta data node '{metaField}'");
                                Console.WriteLine("");
                                ShowHolonMetaTagMappings(result.Result, true);
                            }
                            else
                                Console.WriteLine("");

                            if (tag == "exit" || metaField == "exit")
                                break;

                            mapTags = CLIEngine.GetConfirmation("Would you like to map more tags?");
                            Console.WriteLine("");

                        } while (mapTags);
                    }
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPP.MapCustomMetaTagsToTemplate. Reason: {e.Message}");
            }

            return result;
        }

        private OASISResult<Dictionary<string, string>> MapCustomMetaTagsToTemplate(IInstalledOAPPTemplate oappTemplate)
        {
            OASISResult<Dictionary<string, string>> result = new OASISResult<Dictionary<string, string>>(new Dictionary<string, string>());

            try
            {
                //OASISResult<List<string>> getCustomTagsResult = GetCustomTagsFromTemplate(oappTemplate.InstalledPath, new List<string>(), "{{{", "}}}");
                OASISResult<List<string>> getCustomTagsResult = GetCustomTagsFromTemplate(oappTemplate.InstalledPath, new List<string>(), "[[", "]]");

                if (getCustomTagsResult != null && getCustomTagsResult.Result != null && !getCustomTagsResult.IsError)
                {
                    Console.WriteLine("");
                    CLIEngine.ShowMessage(string.Concat($"Found {getCustomTagsResult.Result.Count} custom tag(s) in the OAPP Template '{oappTemplate.STARNETDNA.Name}'", getCustomTagsResult.Result.Count > 0 ? ":" : "."));

                    if (getCustomTagsResult.Result.Count > 0)
                    {
                        Console.WriteLine("");

                        foreach (string tag in getCustomTagsResult.Result)
                            CLIEngine.ShowMessage(tag, false);
                    }

                    if (getCustomTagsResult.Result.Count > 0 && CLIEngine.GetConfirmation("Would you like to map any of these tags to your meta data?"))
                    {
                        bool mapTags = true;
                        Console.WriteLine("");

                        do
                        {
                            string tag = "";
                            string metaField = "";

                            do
                            {
                                tag = CLIEngine.GetValidInput("Please enter the tag you wish to map (case sensitive). Enter 'exit' to cancel:");

                                if (tag == "exit")
                                    break;

                                if (string.IsNullOrEmpty(tag) || !getCustomTagsResult.Result.Contains(tag))
                                    CLIEngine.ShowErrorMessage($"The tag '{tag}' was not found. Please try again.");

                                else if (result.Result.Keys.Contains(tag))
                                {
                                    CLIEngine.ShowErrorMessage($"The tag '{tag}' has already been mapped to '{result.Result[tag]}'. Please try again.");
                                    tag = "";
                                }

                            } while (!getCustomTagsResult.Result.Contains(tag));

                            if (tag != "exit")
                            {
                                metaField = CLIEngine.GetValidInput("Please enter the meta data you wish to map to this tag. Enter 'exit' to cancel:");

                                if (tag != "exit" && metaField != "exit" && CLIEngine.GetConfirmation($"Please confirm you wish to map the tag '{tag}' to the meta data '{metaField}'?"))
                                {
                                    result.Result[tag] = metaField;
                                    Console.WriteLine("");
                                    CLIEngine.ShowSuccessMessage($"Meta tag '{tag}' mapped to meta data '{metaField}'");
                                    Console.WriteLine("");
                                    ShowMetaTagMappings(result.Result, true);
                                }
                                else
                                    Console.WriteLine("");

                                mapTags = CLIEngine.GetConfirmation("Would you like to map more tags?");
                                Console.WriteLine("");
                            }
                            else
                                break;
                        } while (mapTags);
                    }
                    else
                        Console.WriteLine("");
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPP.MapCustomMetaTagsToTemplate. Reason: {e.Message}");
            }

            return result;
        }

        private bool IsNodeFound(string nodeName, Dictionary<string, IList<INode>> nodes)
        {
            if (nodes != null)
            {
                foreach (string key in nodes.Keys)
                {
                    if (nodes[key].Any(x => x.NodeName == nodeName))
                        return true;
                }
            }

            return false;
        }

        private INode GetNode(string nodeName, Dictionary<string, IList<INode>> nodes)
        {
            INode node = null;

            if (nodes != null)
            {
                foreach (string key in nodes.Keys)
                {
                    node = nodes[key].FirstOrDefault(x => x.NodeName == nodeName);

                    if (node != null)
                        break;
                }
            }

            return node;
        }

        private int CountNodes(Dictionary<string, IList<INode>> nodes)
        {
            int total = 0;

            if (nodes != null)
            {
                foreach (string key in nodes.Keys)
                    total += nodes[key].Count;
            }

            return total;
        }

        private string GetHolonThatNodeBelongsTo(string nodeName, Dictionary<string, IList<INode>> nodes)
        {
            if (nodes != null)
            {
                foreach (string key in nodes.Keys)
                {
                    if (nodes[key].Any(x => x.NodeName == nodeName))
                        return key;
                }
            }

            return "";
        }

        private bool IsMetaMatchFound(string nodeName, Dictionary<string, (string, string)> nodes)
        {
            if (nodes != null)
            {
                foreach (string key in nodes.Keys)
                {
                    if (nodes[key].Item2 == nodeName)
                        return true;
                }
            }

            return false;
        }

        //private void ShowMetaTagMappings(Dictionary<string, string> metaTagMappings)
        //{
        //    CLIEngine.ShowMessage(string.Concat("Tag".PadRight(22), "Meta Data".PadRight(22)), false);

        //    foreach (string key in metaTagMappings.Keys)
        //        CLIEngine.ShowMessage(string.Concat(key.PadRight(22), metaTagMappings[key].PadRight(22)), false);
        //}

        private OASISResult<List<string>> GetCustomTagsFromTemplate(string pathToTemplate, List<string> tags, string startTag, string endTag)
        {
            OASISResult<List<string>> result = new OASISResult<List<string>>();

            try
            {
                foreach (DirectoryInfo dir in new DirectoryInfo(pathToTemplate).GetDirectories())
                {
                    if (dir.Name != "bin" && dir.Name != "obj")
                    {
                        OASISResult<List<string>> getTagsResult = GetCustomTagsFromTemplate(dir.FullName, tags, startTag, endTag);

                        if (getTagsResult != null && getTagsResult.Result != null && !getTagsResult.IsError)
                        {
                            if (getTagsResult.Result.Count > 0)
                                tags.AddRange(getTagsResult.Result);
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"Error occured in OAPP.GetCustomTagsFromTemplate. Reason: {getTagsResult.Message}");
                    }
                }

                foreach (FileInfo file in new DirectoryInfo(pathToTemplate).GetFiles("*.cs"))
                {
                    using (TextReader tr = File.OpenText(file.FullName))
                    {
                        string line;
                        while ((line = tr.ReadLine()) != null)
                        {
                            if (line.Contains(startTag))
                            {
                                int start = line.IndexOf(startTag);
                                start += 2;
                                int end = line.IndexOf(endTag, start);
                                string tag = line.Substring(start, end - start);

                                if (tag.Substring(0, 1) == "{")
                                    tag = tag.Substring(1);

                                if (!tags.Contains(tag))
                                    tags.Add(tag);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in OAPP.GetCustomTagsFromTemplate. Reason: {e.Message}");
            }

            result.Result = tags;
            return result;
        }    
        
        private OASISResult<Dictionary<string, IList<INode>>> ValidateCelestialBodyDataDNA(string dnaFolder)
        {
            OASISResult<Dictionary<string, IList<INode>>> result = new OASISResult<Dictionary<string, IList<INode>>>();
            OASISResult<Dictionary<string, IList<INode>>> nodesResult = STAR.ExtractNodesFromCelestialBodyMetaDataDNA(dnaFolder);

            if (nodesResult != null && nodesResult.Result != null && !nodesResult.IsError)
            {
                if (nodesResult != null && nodesResult.Result != null && !nodesResult.IsError)
                {
                    result.Result = nodesResult.Result;

                    if (result.Result.Count == 0)
                        CLIEngine.ShowWarningMessage($"The CelestialBody MetaData DNA does not contain any valid data! Please try again!");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured extracting nodes from CelestialBody MetaData DNA. Reason: {nodesResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error occured extracting nodes from CelestialBody MetaData DNA. Reason: {nodesResult.Message}");

            return result;
        }
    }
}
