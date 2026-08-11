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


        public override async Task<OASISResult<OAPP>> PublishAsync(string sourcePath = "", bool edit = false, DefaultLaunchMode defaultLaunchMode = DefaultLaunchMode.Optional, bool askToInstallAtEnd = true, ProviderType providerType = ProviderType.Default)
        {
            return await PublishAsync(sourcePath, edit, defaultLaunchMode, askToInstallAtEnd, providerType);
        }

        public async Task<OASISResult<IOAPP>> PublishAsync(string sourcePath = "", bool edit = false, bool dotNetPublish = false, bool askToInstallAtEnd = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOAPP> result = new OASISResult<IOAPP>();
            bool generateOAPPSource = false;
            bool uploadOAPPSource = false;
            bool generateOAPP = true;
            bool uploadOAPPToCloud = false;
            bool generateOAPPSelfContained = false;
            bool uploadOAPPSelfContainedToCloud = false;
            bool generateOAPPSelfContainedFull = false;
            bool uploadOAPPSelfContainedFullToCloud = false;
            bool makeOAPPSourcePublic = false;
            ProviderType OAPPBinaryProviderType = ProviderType.None; //ProviderType.IPFSOASIS;
            ProviderType OAPPSelfContainedBinaryProviderType = ProviderType.None; //ProviderType.IPFSOASIS;
            ProviderType OAPPSelfContainedFullBinaryProviderType = ProviderType.None; //ProviderType.IPFSOASIS;
            bool registerOnSTARNET = false;
            string STARNETInfo = "If you select 'Y' to this question then your OAPP will be published to STARNET where others will be able to find, download and install. If you select 'N' then only the .oapp install file will be generated on your local device, which you can distribute as you please. This file will also be generated even if you publish to STARNET.";

            CLIEngine.ShowDivider();
            CLIEngine.ShowMessage("Welcome to the OAPP Publish Wizard!");
            CLIEngine.ShowDivider();
            Console.WriteLine();
            CLIEngine.ShowMessage("This wizard will publish your OAPP to STARNET. There are 4 ways of doing this:");
            CLIEngine.ShowMessage("1. Publish the standard OAPP (.oapp) file with no runtimes bundled with it. (Default & recommended). The target machine will need to have the .NET, OASIS & STAR runtimes installed.");
            CLIEngine.ShowMessage("2. Publish the standard OAPP (.oappselfcontained) file bundled with the OASIS & STAR runtimes (approx 210MB). The target machine will need to have the .NET runtime installed.");
            CLIEngine.ShowMessage("3. Publish the standard OAPP (.oappselfcontainedfull) file bundled with the OASIS, STAR runtimes & .NET runtimes (approx 500MB). No dependencies needed, fully self-contained.");
            CLIEngine.ShowMessage("4. Publish the OAPP source (.oappsource) file which only contains the source. People can then download the source and build the OAPP on their machine (if they are missing any of the dependencies such as the runtimes these will be automatically restored). NOTE: This means your source would NEED to be made public (not a problem for Open Source etc).");
            CLIEngine.ShowMessage("Each approach has pros and cons with 4 being the smallest and then 2,3 and 4. Smaller means quicker upload and download and less storage space required (lower hosting costs) but also comes with the risk there may be problems building (4 only) or running the OAPP on the target machine if they are missing any of the dependencies such as the runtimes etc. Another advantage of 1,2 & 3 is the launch target is verified in the pre-built OAPP.");
            CLIEngine.ShowMessage("If you choose the Simple Wizard yhen option 1 will be chosen by default, if you wish to choose another option or a combination of options you must choose the Advanced Wizard.");

            CLIEngine.ShowDivider();

            if (string.IsNullOrEmpty(sourcePath))
            {
                string OAPPPathQuestion = "What is the full path to the (dotnet) published output for the OAPP you wish to publish?";
                //launchTargetQuestion = "What is the relative path (from the root of the path given above, e.g bin\\launch.exe) to the launch target for the OAPP? (This could be the exe or batch file for a desktop or console app, or the index.html page for a website, etc)";

                if (!CLIEngine.GetConfirmation("Have you already published the OAPP within Visual Studio (VS), Visual Studio Code (VSCode) or using the dotnet command? (If your OAPP is using a non dotnet template you can answer 'N')."))
                {
                    OAPPPathQuestion = "What is the full path to the OAPP you wish to publish?";
                    dotNetPublish = true;
                    Console.WriteLine();
                    CLIEngine.ShowMessage("No worries, we will do that for you (if it's a dotnet OAPP)! ;-)");
                }
                else
                    Console.WriteLine();

                sourcePath = CLIEngine.GetValidFolder(OAPPPathQuestion, false);
            }

            //OASISResult<IOAPPDNA> OAPPDNAResult = await STAR.STARAPI.OAPPs.ReadDNAFromSourceOrInstallFolderAsync<IOAPPDNA>(sourcePath);

            //if (OAPPDNAResult != null && OAPPDNAResult.Result != null && !OAPPDNAResult.IsError)
            //{
            //    switch (OAPPDNAResult.Result.OAPPTemplateType)
            //    {
            //        case OAPPTemplateType.Console:
            //        case OAPPTemplateType.WPF:
            //        case OAPPTemplateType.WinForms:
            //            launchTarget = $"{OAPPDNAResult.Result.Name}.exe"; //TODO: For this line to work need to remove the namespace question so it just uses the OAPPName as the namespace. //TODO: Eventually this will be set in the OAPPTemplate and/or can also be set when I add the command line dotnet publish integration.
            //                                                                   //launchTarget = $"bin\\Release\\net8.0\\{OAPPDNAResult.Result.OAPPName}.exe"; //TODO: For this line to work need to remove the namespace question so it just uses the OAPPName as the namespace. //TODO: Eventually this will be set in the OAPPTemplate and/or can also be set when I add the command line dotnet publish integration.
            //            break;

            //        case OAPPTemplateType.Blazor:
            //        case OAPPTemplateType.MAUI:
            //        case OAPPTemplateType.WebMVC:
            //            //launchTarget = $"bin\\Release\\net8.0\\index.html"; 
            //            launchTarget = $"index.html";
            //            break;
            //    }

            //    if (!string.IsNullOrEmpty(launchTarget))
            //    {
            //        if (!CLIEngine.GetConfirmation($"{launchTargetQuestion} Do you wish to use the following default launch target: {launchTarget}?"))
            //            launchTarget = CLIEngine.GetValidFile("What launch target do you wish to use? ", sourcePath);
            //        else
            //            launchTarget = Path.Combine(sourcePath, launchTarget);
            //    }
            //    else
            //        launchTarget = CLIEngine.GetValidFile(launchTargetQuestion, sourcePath);


            //((OAPPManager)this.STARNETManager).OnOAPPDownloadStatusChanged += OnDownloadStatusChanged;
            //((OAPPManager)this.STARNETManager).OnOAPPInstallStatusChanged += OnInstallStatusChanged;
            //((OAPPManager)this.STARNETManager).OnOAPPPublishStatusChanged += OnPublishStatusChanged;
            //((OAPPManager)this.STARNETManager).OnOAPPUploadStatusChanged += OnUploadStatusChanged;


            OASISResult<BeginPublishResult> beginPublishResult = await BeginPublishingAsync(sourcePath, DefaultLaunchMode.Mandatory, providerType);

            if (beginPublishResult != null && !beginPublishResult.IsError && beginPublishResult.Result != null)
            {
                if (beginPublishResult.Result.SimpleWizard)
                {
                    registerOnSTARNET = true;
                    uploadOAPPToCloud = true;
                }
                else
                {
                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation("Do you wish to generate the standard .oapp file? (Recommended). This file contains only the built & published OAPP source code. NOTE: You will need to make sure the target machine that runs this OAPP has both the appropriate OASIS & STAR ODK Runtimes installed along with the appropriate .NET Runtime."))
                    {
                        generateOAPP = true;
                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation($"Do you wish to upload/publish the .oapp file to STARNET? {STARNETInfo}"))
                        {
                            Console.WriteLine("");
                            if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapp file to cloud storage?"))
                                uploadOAPPToCloud = true;

                            Console.WriteLine("");
                            if (!beginPublishResult.Result.SimpleWizard)
                            {
                                object OAPPBinaryProviderTypeObject = CLIEngine.GetValidInputForEnum("Do you wish to upload/publish the .oapp file to The OASIS? If so what provider do you wish to upload to? If you do not wish to then enter 'None'.", typeof(ProviderType));

                                if (OAPPBinaryProviderTypeObject != null)
                                {
                                    if (OAPPBinaryProviderTypeObject.ToString() == "exit")
                                    {
                                        result.Message = "User Exited";
                                        return result;
                                    }
                                    else
                                        OAPPBinaryProviderType = (ProviderType)OAPPBinaryProviderTypeObject;
                                }
                            }
                        }
                    }

                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation("Do you wish to generate the self-contained .oapp file? This file contains the built & published OAPP source code along with the OASIS & STAR ODK Runtimes. NOTE: You will need to make sure the target machine that runs this OAPP has the appropriate .NET runtime installed. The file will be a minimum of 210 MB."))
                    {
                        generateOAPPSelfContained = true;
                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation($"Do you wish to upload/publish the self-contained .oapp file to STARNET?"))
                        {
                            Console.WriteLine("");
                            if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapp file to cloud storage?"))
                                uploadOAPPSelfContainedToCloud = true;

                            Console.WriteLine("");
                            object OAPPBinaryProviderTypeObject = CLIEngine.GetValidInputForEnum("Do you wish to upload/publish the .oapp file to The OASIS? If so what provider do you wish to upload to? If you do not wish to then enter 'None'.", typeof(ProviderType));

                            if (OAPPBinaryProviderTypeObject != null)
                            {
                                if (OAPPBinaryProviderTypeObject.ToString() == "exit")
                                {
                                    result.Message = "User Exited";
                                    return result;
                                }
                                else
                                    OAPPSelfContainedBinaryProviderType = (ProviderType)OAPPBinaryProviderTypeObject;
                            }
                        }
                    }

                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation("Do you wish to generate the self-contained (full) .oapp file? This file contains the built & published OAPP source code along with the OASIS, STAR ODK & .NET Runtimes. NOTE: The file will be a minimum of 500 MB."))
                    {
                        generateOAPPSelfContainedFull = true;
                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation($"Do you wish to upload/publish the self-contained (full) .oapp file to STARNET?"))
                        {
                            Console.WriteLine("");
                            if (CLIEngine.GetConfirmation("Do you wish to upload/publish the .oapp file to cloud storage?"))
                                uploadOAPPSelfContainedFullToCloud = true;

                            Console.WriteLine("");
                            object OAPPBinaryProviderTypeObject = CLIEngine.GetValidInputForEnum("Do you wish to upload/publish the .oapp file to The OASIS? If so what provider do you wish to upload to? If you do not wish to then enter 'None'.", typeof(ProviderType));

                            if (OAPPBinaryProviderTypeObject != null)
                            {
                                if (OAPPBinaryProviderTypeObject.ToString() == "exit")
                                {
                                    result.Message = "User Exited";
                                    return result;
                                }
                                else
                                    OAPPSelfContainedFullBinaryProviderType = (ProviderType)OAPPBinaryProviderTypeObject;
                            }
                        }
                    }

                    if (!uploadOAPPToCloud && OAPPBinaryProviderType == ProviderType.None &&
                        !uploadOAPPSelfContainedToCloud && OAPPSelfContainedBinaryProviderType == ProviderType.None &&
                        !uploadOAPPSelfContainedFullToCloud && OAPPSelfContainedFullBinaryProviderType == ProviderType.None)
                        CLIEngine.ShowMessage("Since you did not select to upload to the cloud or OASIS storage the oapp will not be published to STARNET.");
                    else
                        registerOnSTARNET = true;

                    Console.WriteLine("");
                    Console.WriteLine("");
                    if (CLIEngine.GetConfirmation("Do you wish to generate a .oappsource file?"))
                    {
                        generateOAPPSource = true;
                        Console.WriteLine("");
                        if (CLIEngine.GetConfirmation("Do you wish to upload the .oappsource file to STARNET? The next question will ask if you wish to make this public. You may choose to upload and keep private as an extra backup for your code for example."))
                        {
                            uploadOAPPSource = true;
                            Console.WriteLine("");

                            if (CLIEngine.GetConfirmation("Do you wish to make the .oappsource public? People will be able to view your code so only do this if you are happy with this. NOTE: If you select 'N' to this question then people will not be able to download, build, publish and install your OAPP from your .oappsource file. You will need to upload the full pre-built & published .oapp file using one of the other options above if you want people to be able to download and install your OAPP from STARNET. If you wish people to be able to download and install from your .oappsource file then select 'Y'."))
                                makeOAPPSourcePublic = true;
                        }
                    }
                }

                //Console.WriteLine("");
                Console.WriteLine("");
                //OASISResult<string> pubPathResult = await GetPublishPathAsync(beginPublishResult.Result, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType);
                OASISResult<string> pubPathResult = await GetPublishPathAsync(beginPublishResult.Result.SourcePath, beginPublishResult.Result.SimpleWizard, edit, registerOnSTARNET, generateOAPP, uploadOAPPToCloud, providerType, OAPPBinaryProviderType);

                if (pubPathResult != null && !string.IsNullOrEmpty(pubPathResult.Result) && !pubPathResult.IsError)
                {
                    result = await ((OAPPManager)STARNETManager).PublishOAPPAsync(STAR.BeamedInAvatar.Id, sourcePath, beginPublishResult.Result.LaunchTarget, pubPathResult.Result, edit, registerOnSTARNET, dotNetPublish, generateOAPPSource, uploadOAPPSource, makeOAPPSourcePublic, generateOAPP, generateOAPPSelfContained, generateOAPPSelfContainedFull, uploadOAPPToCloud, uploadOAPPSelfContainedToCloud, uploadOAPPSelfContainedFullToCloud, providerType, OAPPBinaryProviderType, OAPPSelfContainedBinaryProviderType, OAPPSelfContainedFullBinaryProviderType, beginPublishResult.Result.EmbedRuntimes, beginPublishResult.Result.EmbedLibs, beginPublishResult.Result.EmbedTemplates);
                    OASISResult<OAPP> publishResult = new OASISResult<OAPP>((OAPP)result.Result);
                    OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult(result, publishResult);
                    await PostFininaliazePublishingAsync(publishResult, sourcePath, askToInstallAtEnd, providerType);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in STARNETUIBase.FininaliazePublishingAsync calling PreFininaliazePublishingAsync. Reason: {pubPathResult.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Error Occured: {beginPublishResult.Message}");

            return result;
        }

    }
}
