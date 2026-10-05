using NextGenSoftware.Utilities;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class Libs : STARNETUIBase<Library, DownloadedLibrary, InstalledLibrary, LibraryDNA>
    public class Libs : STARNETUIBase<Library, DownloadedLibrary, InstalledLibrary, STARNETDNA>
    {
        public Libs(Guid avatarId, STARDNA STARDNA) : base(new LibraryManager(avatarId, STARDNA),
            "Welcome to the Library Wizard", new List<string> 
            {
                "This wizard will allow you create a Library which can be used in a OAPP from (along with a OAPP Template & Runtime).",
                "The library can be created from anything you like from any stack, platform, OS etc.",
                "You can even link an existing library from any site or package store to this one. When you publish you will be given an option to do so.",
                "The wizard will create an empty folder with a LibraryDNA.json file in it. You then simply place any files/folders you need into this folder (if you are linking to an existing library then you can skip this step).",
                "Finally you run the sub-command 'library publish' to convert the folder containing the library (can contain any number of files and sub-folders) into a OASIS Library file (.olibrary) as well as optionally upload to STARNET.",
                "You can then share the .olibrary file with others across any platform or OS from which they can include in OAPP's. They can install the Library from the file using the sub-command 'library install'.",
                "You can also optionally choose to upload the .olibrary file to the STARNET store so others can search, download and install the library. They can then create OAPP's from the library."
            },
            STAR.STARDNA.DefaultLibsSourcePath, "DefaultLibsSourcePath",
            STAR.STARDNA.DefaultLibsPublishedPath, "DefaultLibsPublishedPath",
            STAR.STARDNA.DefaultLibsDownloadedPath, "DefaultLibsDownloadedPath",
            STAR.STARDNA.DefaultLibsInstalledPath, "DefaultLibsInstalledPath")
        { }

        /// <summary>
        /// Override CreateAsync to add Language property prompt
        /// </summary>
        public override async Task<OASISResult<Library>> CreateAsync(ISTARNETCreateOptions<Library, STARNETDNA> createOptions = null, object holonSubType = null, bool showHeaderAndInro = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<Library> result = new OASISResult<Library>();

            if (showHeaderAndInro)
                ShowHeader();

            string holonName = CLIEngine.GetValidInput($"What is the name of the {STARNETManager.STARNETHolonUIName}?");

            if (holonName == "exit")
            {
                result.Message = "User Exited";
                return result;
            }

            string holonDesc = CLIEngine.GetValidInput($"What is the description of the {STARNETManager.STARNETHolonUIName}?");

            if (holonDesc == "exit")
            {
                result.Message = "User Exited";
                return result;
            }

            // Prompt for Library Type
            if (holonSubType == null)
                holonSubType = CLIEngine.GetValidInputForEnum($"What type of {STARNETManager.STARNETHolonUIName} do you wish to create?", STARNETManager.STARNETCategory);

            if (holonSubType != null)
            {
                if (holonSubType.ToString() == "exit")
                {
                    result.Message = "User Exited";
                    return result;
                }

                // Prompt for Language (STARNETSubCategory)
                object language = CLIEngine.GetValidInputForEnum("What Language is this library written in?", typeof(Languages));
                
                if (language != null && language.ToString() != "exit")
                {
                    // Create or update createOptions with Language
                    if (createOptions == null)
                    {
                        createOptions = new STARNETCreateOptions<Library, STARNETDNA>
                        {
                            CustomCreateParams = new Dictionary<string, object>()
                        };
                    }
                    else if (createOptions.CustomCreateParams == null)
                    {
                        createOptions.CustomCreateParams = new Dictionary<string, object>();
                    }

                    createOptions.CustomCreateParams["STARNETSubCategory"] = language;
                }
                else if (language != null && language.ToString() == "exit")
                {
                    result.Message = "User Exited";
                    return result;
                }

                string holonPath = "";

                if (Path.IsPathRooted(SourcePath) || string.IsNullOrEmpty(STAR.STARDNA.BaseSTARNETPath))
                    holonPath = SourcePath;
                else
                    holonPath = Path.Combine(STAR.STARDNA.BaseSTARNETPath, SourcePath);

                (result, holonPath) = GetValidFolder(result, holonPath, STARNETManager.STARNETHolonUIName, SourceSTARDNAKey, true, holonName);

                if (result.IsError)
                    return result;

                Console.WriteLine("");
                CLIEngine.ShowWorkingMessage($"Generating {STARNETManager.STARNETHolonUIName}...");
                result = await STARNETManager.CreateAsync(STAR.BeamedInAvatar.Id, holonName, holonDesc, holonSubType, holonPath, createOptions: createOptions, providerType: providerType);

                if (result != null)
                {
                    if (!result.IsError && result.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"{STARNETManager.STARNETHolonUIName} Successfully Generated.");
                        await ShowAsync(result.Result);
                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation($"Do you wish to open the {STARNETManager.STARNETHolonUIName} folder now?"))
                            Process.Start("explorer.exe", holonPath);

                        Console.WriteLine("");
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"Unknown Error Occured.");
            }
            else
                OASISErrorHandling.HandleError(ref result, "holonSubType is null!");

            return result;
        }
    }
}