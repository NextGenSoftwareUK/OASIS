using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Drawing.Text;
using System.Linq;
using ADRaffy.ENSNormalize;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Enums.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Events.STARNETHolon;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.CelestialSpace;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Enums;
using NextGenSoftware.OASIS.STAR.CLI.Lib.Objects;
using Org.BouncyCastle.Utilities;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class STARNETUIBase<T1, T2, T3, T4>
    {
        public async Task<OASISResult<ISTARNETDNA>> AddDependenciesAsync(ISTARNETDNA STARNETDNA, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<ISTARNETDNA> result = new OASISResult<ISTARNETDNA>();
            DependencyType dependencyTypeEnum = DependencyType.OAPP;

            //Console.WriteLine("");

            if (CLIEngine.GetConfirmation($"Do you wish to add any dependencies (Smartbricks) to the {STARNETDNA.STARNETHolonType} with name '{STARNETDNA.Name}'? (you do not need to add the OASIS or STAR runtimes, they are added automatically)"))
            {
                do
                {
                    Console.WriteLine("");
                    object depType = CLIEngine.GetValidInputForEnum("What type of dependency do you wish to add?", typeof(DependencyType));
                    if (depType != null)
                    {
                        if (depType.ToString() == "exit" || depType.ToString() == "None")
                        {
                            result.Message = "User Exited";
                            return result;
                        }
                        dependencyTypeEnum = (DependencyType)depType;
                    }


                    Guid dependencyId = Guid.Empty;
                    //Console.WriteLine("");
                    if (!CLIEngine.GetConfirmation($"Does the {Enum.GetName(typeof(DependencyType), dependencyTypeEnum)} already exist?"))
                    {
                        Console.WriteLine("");

                        switch (dependencyTypeEnum)
                        {
                            case DependencyType.OAPP:
                                {
                                    OASISResult<OAPP> createResult = await STARCLI.OAPPs.CreateAsync(null, providerType: providerType);

                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.Template:
                                {
                                    OASISResult<OAPPTemplate> createResult = await STARCLI.OAPPTemplates.CreateAsync(null, providerType: providerType);

                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.Zome:
                                {
                                    OASISResult<STARZome> createResult = await STARCLI.Zomes.CreateAsync(null, providerType: providerType);

                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            //TODO: Implement same pattern above to the other dependency types below.
                            case DependencyType.NFT:
                                {
                                    OASISResult<STARNFT> createResult = await STARCLI.NFTs.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.GeoNFT:
                                {
                                    OASISResult<STARGeoNFT> createResult = await STARCLI.GeoNFTs.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.NFTCollection:
                                {
                                    OASISResult<STARNFTCollection> createResult = await STARCLI.NFTCollections.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.GeoNFTCollection:
                                {
                                    OASISResult<STARGeoNFTCollection> createResult = await STARCLI.GeoNFTCollections.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.Chapter:
                                {
                                    OASISResult<Chapter> createResult = await STARCLI.Chapters.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.CelestialBody:
                                {
                                    OASISResult<STARCelestialBody> createResult = await STARCLI.CelestialBodies.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.CelestialBodyMetaDataDNA:
                                {
                                    OASISResult<CelestialBodyMetaDataDNA> createResult = await STARCLI.CelestialBodiesMetaDataDNA.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.CelestialSpace:
                                {
                                    OASISResult<STARCelestialSpace> createResult = await STARCLI.CelestialSpaces.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.Holon:
                                {
                                    OASISResult<STARHolon> createResult = await STARCLI.Holons.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.HolonMetaDataDNA:
                                {
                                    OASISResult<HolonMetaDataDNA> createResult = await STARCLI.HolonsMetaDataDNA.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.InventoryItem:
                                {
                                    OASISResult<InventoryItem> createResult = await STARCLI.InventoryItems.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.Library:
                                {
                                    OASISResult<Library> createResult = await STARCLI.Libs.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.Mission:
                                {
                                    OASISResult<Mission> createResult = await STARCLI.Missions.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.Quest:
                                {
                                    OASISResult<Quest> createResult = await STARCLI.Quests.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.Runtime:
                                {
                                    OASISResult<Runtime> createResult = await STARCLI.Runtimes.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;

                            case DependencyType.ZomeMetaDataDNA:
                                {
                                    OASISResult<ZomeMetaDataDNA> createResult = await STARCLI.ZomesMetaDataDNA.CreateAsync(null, providerType: providerType);
                                    if (createResult != null && createResult.Result != null && !createResult.IsError)
                                        dependencyId = createResult.Result.Id;
                                }
                                break;
                        }
                    }
                    //else
                    //{
                        Console.WriteLine("");
                        OASISResult<T1> addResult = await AddDependencyAsync(idOrNameOfDependency: dependencyId.ToString(), dependencyType: Enum.GetName(typeof(DependencyType), dependencyTypeEnum), parentSTARNETDNA: STARNETDNA, providerType: providerType);

                        if (addResult != null && addResult.Result != null && !addResult.IsError)
                        {
                            result.Result = STARNETDNA;
                            OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(addResult, result);
                        }
                        else
                        {
                            result.IsError = true;
                            result.Message = addResult.Message;
                            //return result;
                        }
                    //} 
                }
                while (CLIEngine.GetConfirmation($"Do you wish to add another dependency to the {STARNETDNA.STARNETHolonType} with name '{STARNETDNA.Name}'?"));
            }

            Console.WriteLine("");
            CLIEngine.ShowDivider();
            return result;
        }

        public virtual async Task<OASISResult<T1>> DeleteAsync(string idOrName = "", bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("delete", idOrName, default, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (CLIEngine.GetConfirmation($"Are you sure you wish to delete this {STARNETManager.STARNETHolonUIName}? This will also delete the {STARNETManager.STARNETHolonUIName} from the Source and Published folders and remove it from the STARNET Store (if you have already published it)"))
                {
                    Console.WriteLine("");
                    bool deleteDownload = CLIEngine.GetConfirmation($"Do you wish to also delete the correponding downloaded {STARNETManager.STARNETHolonUIName}? (if there is any)");

                    Console.WriteLine("");
                    bool deleteInstall = CLIEngine.GetConfirmation($"Do you wish to also delete the correponding installed {STARNETManager.STARNETHolonUIName}? (if there is any). This is different to uninstalling because uninstalled {STARNETManager.STARNETHolonUIName}s are still visible with the 'list uninstalled' sub-command and have the option to re-install. Whereas once it is deleted it is gone forever!");

                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation($"ARE YOU SURE YOU WITH TO PERMANENTLY DELETE THE {STARNETManager.STARNETHolonUIName}? IT WILL NOT BE POSSIBLE TO RECOVER AFTRWARDS!", ConsoleColor.Red))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage($"Deleting {STARNETManager.STARNETHolonUIName}...");
                        result = await STARNETManager.DeleteAsync(STAR.BeamedInAvatar.Id, result.Result, result.Result.STARNETDNA.VersionSequence, true, deleteDownload, deleteInstall, providerType);

                        if (result != null && !result.IsError && result.Result != null)
                        {
                            result.IsDeleted = true;
                            CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Deleted.");
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"An error occured deleting the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
                    }
                }
            }
            else
                OASISErrorHandling.HandleError(ref result, $"An error occured loading the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");

            return result;
        }

        //public virtual async Task<OASISResult<T1>> PublishAsync(string sourcePath = "", bool edit = false, DefaultLaunchMode defaultLaunchMode = DefaultLaunchMode.Optional, bool askToInstallAtEnd = true, ProviderType providerType = ProviderType.Default)
        public virtual async Task<OASISResult<T1>> PublishAsync(string sourcePath = "", bool edit = false, DefaultLaunchMode defaultLaunchMode = DefaultLaunchMode.None, bool askToInstallAtEnd = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> publishResult = new OASISResult<T1>();
            bool generateOAPP = true;
            bool uploadOAPPToCloud = true;
            ProviderType OAPPBinaryProviderType = ProviderType.None;
            // string publishPath = "";

            OASISResult<BeginPublishResult> beginPublishResult = await BeginPublishingAsync(sourcePath, defaultLaunchMode, providerType);

            if (beginPublishResult != null && !beginPublishResult.IsError && beginPublishResult.Result != null)
            {
                Console.WriteLine("");
                bool registerOnSTARNET = CLIEngine.GetConfirmation($"Do you wish to publish to STARNET? If you select 'Y' to this question then your {STARNETManager.STARNETHolonUIName} will be published to STARNET where others will be able to find, download and install. If you select 'N' then only the .{STARNETManager.STARNETHolonFileExtention} install file will be generated on your local device, which you can distribute as you please. This file will also be generated even if you publish to STARNET.");
                Console.WriteLine("");

                if (registerOnSTARNET && !beginPublishResult.Result.SimpleWizard)
                {
                    CLIEngine.ShowMessage($"Do you wish to publish/upload the .{STARNETManager.STARNETHolonFileExtention} file to an OASIS Provider or to the cloud or both? Depending on which OASIS Provider is chosen such as IPFSOASIS there may issues such as speed, relialbility etc for such a large file. If you choose to upload to the cloud this could be faster and more reliable (but there is a limit of 5 OAPPs on the free plan and you will need to upgrade to upload more than 5 OAPPs). You may want to choose to use both to add an extra layer of redundancy (recommended).");

                    if (!CLIEngine.GetConfirmation("Do you wish to upload to the cloud?"))
                        uploadOAPPToCloud = false;

                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation("Do you wish to upload to an OASIS Provider? Make sure you select a provider that can handle large files such as IPFSOASIS, HoloOASIS etc. Also remember the OASIS Hyperdrive will only be able to auto-replicate to other providers that also support large files and are free or cost effective. By default it will NOT auto-replicate large files, you will need to manually configure this in your OASIS Profile settings."))
                    {
                        Console.WriteLine("");
                        object largeProviderTypeObject = CLIEngine.GetValidInputForEnum("What provider do you wish to publish the OAPP to? (The default is IPFSOASIS)", typeof(ProviderType));

                        if (largeProviderTypeObject != null)
                            OAPPBinaryProviderType = (ProviderType)largeProviderTypeObject;
                    }
                    else
                        Console.WriteLine("");
                }

                publishResult = await FininaliazePublishingAsync(beginPublishResult.Result, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, askToInstallAtEnd, providerType, OAPPBinaryProviderType);
            }
            else
                CLIEngine.ShowErrorMessage($"Error Occured: {beginPublishResult.Message}");

            return publishResult;
        }

        //protected async Task<OASISResult<BeginPublishResult>> BeginPublishingAsync(string sourcePath, DefaultLaunchMode defaultLaunchMode = DefaultLaunchMode.Optional, ProviderType providerType = ProviderType.Default)
        protected async Task<OASISResult<BeginPublishResult>> BeginPublishingAsync(string sourcePath, DefaultLaunchMode defaultLaunchMode = DefaultLaunchMode.None, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<BeginPublishResult> result = new OASISResult<BeginPublishResult>(new BeginPublishResult());
            bool generateOAPP = true;
            bool uploadOAPPToCloud = true;
            ProviderType OAPPBinaryProviderType = ProviderType.None;
            string launchTargetQuestion = $"What is the relative path (from the root of the path given above, e.g bin/launch.exe) to the launch target for the {STARNETManager.STARNETHolonUIName}? (This could be the exe or batch file for a desktop or console app, or the index.html page for a website, etc)";
            result.Result.SimpleWizard = CLIEngine.GetConfirmation("Do you wish to launch the Simple or Advanced Wizard? The Simple Wizard will use defaults (recommended) but the Advanced Wizard will allow greater control and customisation. Press 'Y' for Simple or 'N' for Advanced.");

            if (string.IsNullOrEmpty(sourcePath))
            {
                Console.WriteLine("");
                //launchTargetQuestion = $"What is the relative path (from the root of the path given above, e.g bin/launch.exe) to the launch target for the {STARNETManager.STARNETHolonUIName}? (This could be the exe or batch file for a desktop or console app, or the index.html page for a website, etc)";
                sourcePath = CLIEngine.GetValidFolder($"What is the full path to the {STARNETManager.STARNETHolonUIName} directory?", false);
            }

            result.Result.SourcePath = sourcePath;
            OASISResult<STARNETDNA> DNAResult = await STARNETManager.ReadDNAFromSourceOrInstallFolderAsync<STARNETDNA>(sourcePath);

            if (DNAResult != null && DNAResult.Result != null && !DNAResult.IsError)
            {
                OASISResult<T1> loadResult = await STARNETManager.LoadAsync(STAR.BeamedInAvatar.Id, DNAResult.Result.Id, 0, providerType: providerType);

                if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                {
                    loadResult.Result.STARNETDNA.Version = DNAResult.Result.Version; //Update the version from the JSON file.
                    await ShowAsync(loadResult.Result);

                    if (!CLIEngine.GetConfirmation($"Is this the correct {STARNETManager.STARNETHolonUIName} you wish to publish?"))
                    {
                        Console.WriteLine("");
                        result.Message = "User Exited";
                        result.IsError = true;
                        return result;
                    }

                    result.Result.LaunchTarget = loadResult.Result.STARNETDNA.LaunchTarget;
                    Console.WriteLine("");

                    //object templateType = Enum.Parse(STARNETManager.STARNETHolonSubType, DNAResult.Result.STARNETHolonType.ToString());
                    //Type Type = (Type)templateType;

                    //switch (Type)
                    //{
                    //    case Type.Console:
                    //    case Type.WPF:
                    //    case Type.WinForms:
                    //        launchTarget = $"{DNAResult.Result.Name}.exe"; //TODO: For this line to work need to remove the namespace question so it just uses the OAPPName as the namespace. //TODO: Eventually this will be set in the  and/or can also be set when I add the command line dotnet publish integration.
                    //        break;

                    //    case Type.Blazor:
                    //    case Type.MAUI:
                    //    case Type.WebMVC:
                    //        launchTarget = $"index.html";
                    //        break;
                    //}

                    if (defaultLaunchMode != DefaultLaunchMode.None)
                    {
                        bool hasDefaultLaunchTarget = false;

                        if (defaultLaunchMode == DefaultLaunchMode.Optional)
                            hasDefaultLaunchTarget = CLIEngine.GetConfirmation($"Do you wish to set a default launch target?");

                        else if (defaultLaunchMode == DefaultLaunchMode.Mandatory)
                            hasDefaultLaunchTarget = true;

                        if (hasDefaultLaunchTarget)
                        {
                            Console.WriteLine("");
                            if (!string.IsNullOrEmpty(result.Result.LaunchTarget))
                            {
                                if (!CLIEngine.GetConfirmation($"{launchTargetQuestion} Do you wish to use the following default launch target: {result.Result.LaunchTarget}?"))
                                {
                                    Console.WriteLine("");
                                    result.Result.LaunchTarget = CLIEngine.GetValidFile("What launch target do you wish to use? ", sourcePath);
                                }
                                else
                                    result.Result.LaunchTarget = Path.Combine(sourcePath, result.Result.LaunchTarget);
                            }
                            else
                                result.Result.LaunchTarget = CLIEngine.GetValidFile(launchTargetQuestion, sourcePath);
                        }
                    }

                    if (!result.Result.SimpleWizard && STARNETManager.GetNumberOfDependendies(DNAResult.Result) > 0 && CLIEngine.GetConfirmation($"Do you wish to embed any of the dependencies (Smartbricks)? It is not recommended because will increase the storage space/cost & upload/download time. If you choose 'N' then they will be automatically downloaded and installed when someone installs your {STARNETManager.STARNETHolonUIName}. Only choose 'Y' if you want them embedded in case there is an issue downloading/installing them seperatley later (unlikely) or if you want the {STARNETManager.STARNETHolonUIName} to be fully self-contained with no external dependencies (Smartbricks) (useful if you wish to install it offline from the {STARNETManager.STARNETHolonFileExtention} file)."))
                    {
                        if (DNAResult.Result.Dependencies.Templates.Count > 0)
                            result.Result.EmbedTemplates = CLIEngine.GetConfirmation("Do you wish to embed the sub-templates?");

                        if (DNAResult.Result.Dependencies.Runtimes.Count > 0)
                            result.Result.EmbedRuntimes = CLIEngine.GetConfirmation("Do you wish to embed the runtimes?");

                        if (DNAResult.Result.Dependencies.Libraries.Count > 0)
                            result.Result.EmbedLibs = CLIEngine.GetConfirmation("Do you wish to embed the libraries?");

                        //TODO: Add rest here!
                    }
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"The {STARNETManager.STARNETHolonUIName} could not be found for id {DNAResult.Result.Id} found in the {STARNETManager.STARNETDNAFileName} file. It could be corrupt, the id could be wrong or you may not have permission, please check and try again, or create a new {STARNETManager.STARNETHolonUIName}.");
            }
            else
                OASISErrorHandling.HandleError(ref result, $"The {STARNETManager.STARNETDNAFileName} file could not be found! Please ensure it is in the folder you specified.");

            return result;
        }
    }
}
