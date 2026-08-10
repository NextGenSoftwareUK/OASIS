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

        protected async Task<OASISResult<T1>> FininaliazePublishingAsync(BeginPublishResult pubParams, bool edit, bool registerOnSTARNET, bool generateOAPP, bool uploadOAPPToCloud, bool askToInstallAtEnd = true, ProviderType providerType = ProviderType.Default, ProviderType OAPPBinaryProviderType = ProviderType.Default)
        {
            OASISResult<T1> publishResult = new OASISResult<T1>();
            //OASISResult<string> pubPathResult = await GetPublishPathAsync(pubParams, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType);
            OASISResult<string> pubPathResult = await GetPublishPathAsync(pubParams.SourcePath, pubParams.SimpleWizard, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType);

            if (pubPathResult != null && !string.IsNullOrEmpty(pubPathResult.Result) && !pubPathResult.IsError)
            {
                publishResult = await STARNETManager.PublishAsync(STAR.BeamedInAvatar.Id, pubParams.SourcePath, pubParams.LaunchTarget, pubPathResult.Result, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType, pubParams.EmbedRuntimes, pubParams.EmbedLibs, pubParams.EmbedTemplates);
                await PostFininaliazePublishingAsync(publishResult, pubParams.SourcePath, askToInstallAtEnd, providerType);
            }
            else
                OASISErrorHandling.HandleError(ref publishResult, $"Error occured in STARNETUIBase.FininaliazePublishingAsync calling PreFininaliazePublishingAsync. Reason: {pubPathResult.Message}");

            return publishResult;
        }

        //protected async Task<OASISResult<string>> GetPublishPathAsync(BeginPublishResult pubParams, bool edit, bool registerOnSTARNET, bool generateOAPP, bool uploadOAPPToCloud, ProviderType providerType, ProviderType OAPPBinaryProviderType)
        protected async Task<OASISResult<string>> GetPublishPathAsync(string sourcePath, bool simpleWizard, bool edit, bool registerOnSTARNET, bool generateOAPP, bool uploadOAPPToCloud, ProviderType providerType, ProviderType OAPPBinaryProviderType)
        {
            OASISResult<string> result = new OASISResult<string>();
            string publishPath = "";

            if (Path.IsPathRooted(PublishedPath) || string.IsNullOrEmpty(STAR.STARDNA.STARNETBasePath))
                publishPath = PublishedPath;
            else
                publishPath = Path.Combine(STAR.STARDNA.STARNETBasePath, PublishedPath);

            if (!simpleWizard)
            {
                if (!CLIEngine.GetConfirmation($"Do you wish to publish the {STARNETManager.STARNETHolonUIName} to the default publish folder defined in the STARDNA as {PublishedSTARDNAKey} : {publishPath}?"))
                {
                    Console.WriteLine("");

                    if (CLIEngine.GetConfirmation($"Do you wish to publish the {STARNETManager.STARNETHolonUIName} to: {Path.Combine(sourcePath, "Published")}?"))
                        publishPath = Path.Combine(sourcePath, "Published");
                    else
                    {
                        Console.WriteLine("");
                        publishPath = CLIEngine.GetValidFolder($"Where do you wish to publish the {STARNETManager.STARNETHolonUIName}?", true);
                    }
                }
            }

            publishPath = new DirectoryInfo(publishPath).FullName;

            //Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Publishing {STARNETManager.STARNETHolonUIName}...");
            result.Result = publishPath;
            return result;
        }

        protected async Task<OASISResult<T1>> PostFininaliazePublishingAsync(OASISResult<T1> publishResult, string sourcePath, bool askToInstallAtEnd = true, ProviderType providerType = ProviderType.Default)
        {

            if (publishResult != null && !publishResult.IsError && publishResult.Result != null)
            {
                await ShowAsync(publishResult.Result);

                if (askToInstallAtEnd && CLIEngine.GetConfirmation($"Do you wish to install the {STARNETManager.STARNETHolonUIName} now?"))
                    await DownloadAndInstallAsync(publishResult.Result.STARNETDNA.Id.ToString(), InstallMode.DownloadAndInstall, providerType);

                Console.WriteLine("");
            }
            else
            {
                //if (publishResult.Message.Contains("Please make sure you increment the version"))
                if (publishResult.Message.Contains(STARNETManager.STARNETDNAFileName))
                {
                    if (CLIEngine.GetConfirmation($"Do you wish to open the {STARNETManager.STARNETDNAFileName} file now?"))
                        Process.Start("explorer.exe", Path.Combine(sourcePath, STARNETManager.STARNETDNAFileName));
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured publishing the {STARNETManager.STARNETHolonUIName}. Reason: {publishResult.Message}");

                Console.WriteLine("");
            }

            return publishResult;
        }


        public virtual async Task UnpublishAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("unpublish", idOrName, default, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T1> unpublishResult = await STARNETManager.UnpublishAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                if (unpublishResult != null && !unpublishResult.IsError && unpublishResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Unpublished.");
                    await ShowAsync(result.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the {STARNETManager.STARNETHolonUIName}. Reason: {unpublishResult.Message}");
            }
        }

        public virtual async Task RepublishAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("republish", idOrName, default, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T1> republishResult = await STARNETManager.RepublishAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                if (republishResult != null && !republishResult.IsError && republishResult.Result != null)
                {
                    CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Republished.");
                    await ShowAsync(result.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured unpublishing the {STARNETManager.STARNETHolonUIName}. Reason: {republishResult.Message}");
            }
        }

        public virtual async Task ActivateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("activate", idOrName, default, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (result.MetaData != null && result.MetaData.ContainsKey("Active") && result.MetaData["Active"] != null && result.MetaData["Active"] == "1")
                {
                    OASISResult<T1> activateResult = await STARNETManager.ActivateAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                    if (activateResult != null && !activateResult.IsError && activateResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Activated.");
                        await ShowAsync(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured activating the {STARNETManager.STARNETHolonUIName}. Reason: {activateResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"The {STARNETManager.STARNETHolonUIName} is already activated!");
            }
        }

        public virtual async Task DeactivateAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("deactivate", idOrName, default, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (result.MetaData != null && result.MetaData.ContainsKey("Active") && result.MetaData["Active"] != null && result.MetaData["Active"] == "0")
                {
                    OASISResult<T1> deactivateResult = await STARNETManager.DeactivateAsync(STAR.BeamedInAvatar.Id, result.Result, providerType);

                    if (deactivateResult != null && !deactivateResult.IsError && deactivateResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Deactivated.");
                        await ShowAsync(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"An error occured deactivating the {STARNETManager.STARNETHolonUIName}. Reason: {deactivateResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"The {STARNETManager.STARNETHolonUIName} is already deactivated!");
            }
        }

        public virtual async Task<OASISResult<T3>> DownloadAndInstallAsync(string idOrName = "", InstallMode installMode = InstallMode.DownloadAndInstall, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> installResult = new OASISResult<T3>();
            string downloadPath = "";
            string installPath = "";
            bool simpleWizard = false;
            string operation = "install";

            if (installMode == InstallMode.DownloadOnly)
                operation = "download";

            if (Path.IsPathRooted(DownloadedPath) || string.IsNullOrEmpty(STAR.STARDNA.STARNETBasePath))
                downloadPath = DownloadedPath;
            else
                downloadPath = Path.Combine(STAR.STARDNA.STARNETBasePath, DownloadedPath);


            if (Path.IsPathRooted(InstalledPath) || string.IsNullOrEmpty(STAR.STARDNA.STARNETBasePath))
                installPath = InstalledPath;
            else
                installPath = Path.Combine(STAR.STARDNA.STARNETBasePath, InstalledPath);

            Console.WriteLine("");

            if (CLIEngine.GetConfirmation("Do you wish to launch the Simple or Advanced Wizard? The Simple Wizard will use defaults (recommended) but the Advanced Wizard will allow greater control and customisation. Press 'Y' for Simple or 'N' for Advanced."))
                simpleWizard = true;

            if (!simpleWizard)
            {
                Console.WriteLine("");

                if (!CLIEngine.GetConfirmation($"Do you wish to download the {STARNETManager.STARNETHolonUIName} to the default download folder defined in the STARDNA as {DownloadSTARDNAKey} : {downloadPath}?"))
                {
                    Console.WriteLine("");
                    downloadPath = CLIEngine.GetValidFolder($"What is the full path to where you wish to download the {STARNETManager.STARNETHolonUIName}?", true);
                }

                downloadPath = new DirectoryInfo(downloadPath).FullName;

                if (installMode != InstallMode.DownloadAndInstall)
                {
                    Console.WriteLine("");

                    if (!CLIEngine.GetConfirmation($"Do you wish to install the {STARNETManager.STARNETHolonUIName} to the default install folder defined in the STARDNA as {InstalledSTARDNAKey} : {installPath}?"))
                    {
                        Console.WriteLine("");
                        installPath = CLIEngine.GetValidFolder($"What is the full path to where you wish to install the {STARNETManager.STARNETHolonUIName}?", true);
                    }

                    installPath = new DirectoryInfo(installPath).FullName;
                }
            }

            if (!string.IsNullOrEmpty(idOrName))
            {
                Console.WriteLine("");
                OASISResult<T1> result = await FindForProviderAsync(operation, idOrName, false, false, true, providerType);

                if (result != null && result.Result != null && !result.IsError)
                {
                    if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(result.MetaData["Reinstall"]) && result.MetaData["Reinstall"] == "1" && installMode == InstallMode.DownloadAndInstall)
                        installMode = InstallMode.DownloadAndReInstall;

                    installResult = await CheckIfInstalledAndInstallAsync(result.Result, downloadPath, installPath, installMode, "", providerType);
                }
            }
            else
            {
                Console.WriteLine("");
                if (installMode != InstallMode.DownloadOnly && CLIEngine.GetConfirmation($"Do you wish to install the {STARNETManager.STARNETHolonUIName} from a local .{STARNETManager.STARNETDNAFileName} file or from STARNET? Press 'Y' for local .{STARNETManager.STARNETDNAFileName} file or 'N' for STARNET."))
                {
                    Console.WriteLine("");
                    string oappPath = CLIEngine.GetValidFile($"What is the full path to the .{STARNETManager.STARNETDNAFileName} file?");

                    if (oappPath == "exit")
                        return installResult;

                    OASISResult<ISTARNETDNA> starHolonDNAResult = await STARNETManager.ReadDNAFromPublishedFileAsync<ISTARNETDNA>(oappPath);

                    if (starHolonDNAResult != null && starHolonDNAResult.Result != null && !starHolonDNAResult.IsError)
                    {
                        OASISResult<T1> starHolonResult = await STARNETManager.LoadAsync(STAR.BeamedInAvatar.Id, starHolonDNAResult.Result.Id, 0, providerType: providerType);

                        if (starHolonResult != null && starHolonResult.Result != null && !starHolonResult.IsError)
                        {
                            installMode = InstallMode.InstallOnly;

                            if (starHolonResult.MetaData != null && starHolonResult.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(starHolonResult.MetaData["Reinstall"]) && starHolonResult.MetaData["Reinstall"] == "1")
                                installMode = InstallMode.ReInstall;

                            installResult = await CheckIfInstalledAndInstallAsync(starHolonResult.Result, downloadPath, installPath, installMode, oappPath, providerType);
                        }
                        else
                            CLIEngine.ShowErrorMessage($"The {STARNETManager.STARNETHolonUIName} could not be found for id {starHolonDNAResult.Result.Id} found in the STARNETDNA.json file. It could be corrupt or the id could be wrong, please check and try again, or create a new {STARNETManager.STARNETHolonUIName}.");
                    }
                    else
                        CLIEngine.ShowErrorMessage($"The {STARNETManager.STARNETHolonUIName} could not be found or is not valid! Please ensure it is in the folder you specified.");
                }
                else
                {
                    Console.WriteLine("");
                    OASISResult<T1> result = await FindForProviderAsync(operation, "", false, false, true, providerType);

                    if (result != null && result.Result != null && !result.IsError)
                    {
                        if (result.MetaData != null && result.MetaData.ContainsKey("Reinstall") && !string.IsNullOrEmpty(result.MetaData["Reinstall"]) && result.MetaData["Reinstall"] == "1" && installMode == InstallMode.DownloadAndInstall)
                            installMode = InstallMode.DownloadAndReInstall;

                        installResult = await CheckIfInstalledAndInstallAsync(result.Result, downloadPath, installPath, installMode, "", providerType);
                    }
                    else
                    {
                        installResult.Message = result.Message;
                        installResult.IsError = true;
                    }
                }
            }

            if (installResult != null)
            {
                if (!installResult.IsError && installResult.Result != null)
                {
                    ShowInstalled(installResult.Result);

                    if (CLIEngine.GetConfirmation($"Do you wish to open the folder to the {STARNETManager.STARNETHolonUIName} now?"))
                        STARNETManager.OpenSTARNETHolonFolder(STAR.BeamedInAvatar.Id, installResult.Result);
                    //await STARNETManager.OpenSTARNETHolonFolderAsync(STAR.BeamedInAvatar.Id, installResult.Result.STARNETDNA.Id, installResult.Result.STARNETDNA.Version);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error {operation}ing {STARNETManager.STARNETHolonUIName}. Reason: {installResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error {operation}ing {STARNETManager.STARNETHolonUIName}. Reason: Unknown error occured!");

            Console.WriteLine("");
            return installResult;
        }

        public virtual OASISResult<T3> DownloadAndInstall(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T3> installResult = new OASISResult<T3>();
            string downloadPath = "";
            string installPath = "";

            if (Path.IsPathRooted(DownloadedPath) || string.IsNullOrEmpty(STAR.STARDNA.STARNETBasePath))
                downloadPath = SourcePath;
            else
                downloadPath = Path.Combine(STAR.STARDNA.STARNETBasePath, DownloadedPath);


            if (Path.IsPathRooted(InstalledPath) || string.IsNullOrEmpty(STAR.STARDNA.STARNETBasePath))
                installPath = SourcePath;
            else
                installPath = Path.Combine(STAR.STARDNA.STARNETBasePath, InstalledPath);

            Console.WriteLine("");

            if (!CLIEngine.GetConfirmation($"Do you wish to download the {STARNETManager.STARNETHolonUIName} to the default download folder defined in the STARDNA as {DownloadSTARDNAKey} : {downloadPath}?"))
            {
                Console.WriteLine("");
                downloadPath = CLIEngine.GetValidFolder($"What is the full path to where you wish to download the {STARNETManager.STARNETHolonUIName}?", true);
            }

            downloadPath = new DirectoryInfo(downloadPath).FullName;

            Console.WriteLine("");

            if (!CLIEngine.GetConfirmation($"Do you wish to install the {STARNETManager.STARNETHolonUIName} to the default install folder defined in the STARDNA as {DownloadSTARDNAKey} : {installPath}?"))
            {
                Console.WriteLine("");
                installPath = CLIEngine.GetValidFolder($"What is the full path to where you wish to install the {STARNETManager.STARNETHolonUIName}?", true);
            }

            installPath = new DirectoryInfo(installPath).FullName;

            if (!string.IsNullOrEmpty(idOrName))
            {
                Console.WriteLine("");
                OASISResult<T1> result = FindForProvider("install", idOrName, false, false, true, providerType);

                if (result != null && result.Result != null && !result.IsError)
                    installResult = STARNETManager.DownloadAndInstall(STAR.BeamedInAvatar.Id, result.Result, installPath, downloadPath, true, false, providerType);
            }
            else
            {
                Console.WriteLine("");
                if (CLIEngine.GetConfirmation($"Do you wish to install the {STARNETManager.STARNETHolonUIName} from a local .{STARNETManager.STARNETDNAFileName} file or from STARNET? Press 'Y' for local .{STARNETManager.STARNETDNAFileName} file or 'N' for STARNET."))
                {
                    Console.WriteLine("");
                    string oappPath = CLIEngine.GetValidFile($"What is the full path to the {STARNETManager.STARNETDNAFileName} file?");

                    if (oappPath == "exit")
                        return installResult;

                    installResult = STARNETManager.Install(STAR.BeamedInAvatar.Id, oappPath, installPath, true, null, false, providerType);
                }
                else
                {
                    Console.WriteLine("");
                    CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}s...");
                    OASISResult<IEnumerable<T1>> starHolonsResult = ListAll();

                    if (starHolonsResult != null && starHolonsResult.Result != null && !starHolonsResult.IsError && starHolonsResult.Result.Count() > 0)
                    {
                        OASISResult<T1> result = FindForProvider("", "install", false, false, true, providerType);

                        if (result != null && result.Result != null && !result.IsError)
                            installResult = STARNETManager.DownloadAndInstall(STAR.BeamedInAvatar.Id, result.Result, installPath, downloadPath, true, false, providerType);
                        else
                        {
                            installResult.Message = result.Message;
                            installResult.IsError = true;
                        }
                    }
                    else
                    {
                        installResult.Message = $"No {STARNETManager.STARNETHolonUIName}s found to install.";
                        installResult.IsError = true;
                    }
                }
            }

            if (installResult != null)
            {
                if (!installResult.IsError && installResult.Result != null)
                {
                    ShowInstalled(installResult.Result);

                    if (CLIEngine.GetConfirmation($"Do you wish to open the folder to the {STARNETManager.STARNETHolonUIName} now?"))
                        STARNETManager.OpenSTARNETHolonFolder(STAR.BeamedInAvatar.Id, installResult.Result);
                }
                else
                    CLIEngine.ShowErrorMessage($"Error installing {STARNETManager.STARNETHolonUIName}. Reason: {installResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error installing {STARNETManager.STARNETHolonUIName}. Reason: Unknown error occured!");

            Console.WriteLine("");
            return installResult;
        }

        public virtual async Task UninstallAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = await FindAsync("uninstall", idOrName, default, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T3> uninstallResult = await STARNETManager.UninstallAsync(STAR.BeamedInAvatar.Id, result.Result.Id, result.Result.STARNETDNA.Version, providerType);

                if (uninstallResult != null)
                {
                    if (!uninstallResult.IsError && uninstallResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Uninstalled.");
                        await ShowAsync(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Error installing {STARNETManager.STARNETHolonUIName}. Reason: {uninstallResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error uninstalling {STARNETManager.STARNETHolonUIName}. Reason: Unknown error occured!");
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
        }

        public virtual void Uninstall(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<T1> result = Find("uninstall", idOrName, true, providerType: providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                OASISResult<T3> uninstallResult = STARNETManager.Uninstall(STAR.BeamedInAvatar.Id, result.Result.Id, result.Result.STARNETDNA.Version, providerType);

                if (uninstallResult != null)
                {
                    if (!uninstallResult.IsError && uninstallResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Uninstalled.");
                        ShowAsync(result.Result);
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Error installing {STARNETManager.STARNETHolonUIName}. Reason: {uninstallResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error uninstalling {STARNETManager.STARNETHolonUIName}. Reason: Unknown error occured!");
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the {STARNETManager.STARNETHolonUIName}. Reason: {result.Message}");
        }

        public virtual async Task<OASISResult<IEnumerable<T1>>> ListAllAsync(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}'s...");
            return ListStarHolons(await STARNETManager.LoadAllAsync(STAR.BeamedInAvatar.Id, null, true, showAllVersions, version, providerType: providerType), showDetailedInfo: showDetailedInfo);
        }

        public virtual OASISResult<IEnumerable<T1>> ListAll(bool showAllVersions = false, bool showDetailedInfo = false, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            Console.WriteLine("");
            CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}'s...");
            return ListStarHolons(STARNETManager.LoadAll(STAR.BeamedInAvatar.Id, null, true, showAllVersions, version, providerType: providerType), showDetailedInfo: showDetailedInfo);
        }

        public virtual async Task ListAllCreatedByBeamedInAvatarAsync(bool showAllVersions = false, bool showDetailedInfo = false, ProviderType providerType = ProviderType.Default)
        {
            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading {STARNETManager.STARNETHolonUIName}'s...");
                ListStarHolons(await STARNETManager.LoadAllForAvatarAsync(STAR.BeamedInAvatar.AvatarId), showDetailedInfo: showDetailedInfo);
            }
            else
                CLIEngine.ShowErrorMessage("No Avatar Is Beamed In. Please Beam In First!");
        }

        public virtual async Task<OASISResult<IEnumerable<T3>>> ListAllInstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = new OASISResult<IEnumerable<T3>>();

            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading Installed {STARNETManager.STARNETHolonUIName}'s...");
                result = await STARNETManager.ListInstalledAsync(STAR.BeamedInAvatar.AvatarId);
                ListStarHolonsInstalled(result);
            }
            else
                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T3>>> ListAllUninstalledForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T3>> result = new OASISResult<IEnumerable<T3>>();

            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading Uninstalled {STARNETManager.STARNETHolonUIName}s...");
                result = await STARNETManager.ListUninstalledAsync(STAR.BeamedInAvatar.AvatarId);
                ListStarHolonsInstalled(result, true, true);

                if (result != null && !result.IsError && result.Result != null && result.Result.Count() > 0 && CLIEngine.GetConfirmation("Would you like to re-install any of the above?"))
                {
                    int number = 0;

                    do
                    {
                        Console.WriteLine("");
                        number = CLIEngine.GetValidInputForInt("What number do you wish to re-install? (It will be downloaded and installed to the previous paths)");

                        if (number < 0 || number > result.Result.Count())
                            CLIEngine.ShowErrorMessage($"Invalid number, it needs to be between 1 and {result.Result.Count()}");
                    }
                    while (number < 0 || number > result.Result.Count());

                    if (number > 0)
                    {
                        T3 template = result.Result.ElementAt(number - 1);

                        if (template != null)
                        {
                            OASISResult<T3> installResult = await DownloadAndInstallAsync(template.STARNETDNA.Id.ToString(), InstallMode.DownloadAndReInstall, providerType);

                            if (installResult != null && !installResult.IsError && installResult.Result != null)
                            {
                                ShowInstalled(installResult.Result);
                                CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Re-Installed");
                            }
                            else
                                CLIEngine.ShowErrorMessage($"An error occured re-installing the {STARNETManager.STARNETHolonUIName}. Reason: {installResult.Message}");
                        }
                        else
                            CLIEngine.ShowErrorMessage($"An error occured re-installing the {STARNETManager.STARNETHolonUIName}. Reason: {STARNETManager.STARNETHolonIdName} not found in the metadata!");
                    }
                }
                else
                    Console.WriteLine("");
            }
            else
                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

            return result;
        }

        public virtual async Task<OASISResult<IEnumerable<T1>>> ListAllUnpublishedForBeamedInAvatarAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<T1>> result = new OASISResult<IEnumerable<T1>>();

            if (STAR.BeamedInAvatar != null)
            {
                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Loading Unpublished {STARNETManager.STARNETHolonUIName}'s...");
                result = await STARNETManager.ListUnpublishedAsync(STAR.BeamedInAvatar.AvatarId);
                ListStarHolons(result, true);

                if (result != null && !result.IsError && result.Result != null && result.Result.Count() > 0 && CLIEngine.GetConfirmation("Would you like to republish any of the above?"))
                {
                    int number = 0;

                    do
                    {
                        Console.WriteLine("");
                        number = CLIEngine.GetValidInputForInt("What number do you wish to republish?");

                        if (number < 0 || number > result.Result.Count())
                            CLIEngine.ShowErrorMessage($"Invalid number, it needs to be between 1 and {result.Result.Count()}");
                    }
                    while (number < 0 || number > result.Result.Count());

                    if (number > 0)
                    {
                        T1 template = result.Result.ElementAt(number - 1);
                        Guid id = Guid.Empty;

                        if (template != null)
                        {
                            OASISResult<T1> republishResult = await STARNETManager.RepublishAsync(STAR.BeamedInAvatar.Id, template, providerType);

                            if (republishResult != null && !republishResult.IsError && republishResult.Result != null)
                            {
                                await ShowAsync(republishResult.Result);
                                CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Republished");
                            }
                            else
                                CLIEngine.ShowErrorMessage($"An error occured republishing the {STARNETManager.STARNETHolonUIName}. Reason: {republishResult.Message}");
                        }
                    }
                }
                else
                    Console.WriteLine("");
            }
            else
                OASISErrorHandling.HandleError(ref result, "No Avatar Is Beamed In. Please Beam In First!");

            return result;
        }

    }
}
