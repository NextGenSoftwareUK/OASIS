using System.Diagnostics;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public static partial class STARCLI
    {
        public static async Task CreateOAPPTemplateAsync(ProviderType providerType = ProviderType.Default)
        {
            OASISResult<CoronalEjection> lightResult = null;

            CLIEngine.ShowDivider();
            CLIEngine.ShowMessage("Welcome to the OAPP Template Wizard");
            CLIEngine.ShowDivider();
            Console.WriteLine();
            CLIEngine.ShowMessage("This wizard will allow you create an OAPP Template which can be used to create a OAPP from.", false);
            CLIEngine.ShowMessage("The OAPP Template can be created from anything you like such as a website, javascript template, game, app, service, etc in any language, platform or os.");
            CLIEngine.ShowMessage("You simply need to add specefic STAR ODK OAPP Template reserved tags where dynamic data will be injected in from the OAPP meta data.");
            CLIEngine.ShowMessage("You then simply run this wizard to convert the folder containing the template (can contain any number of files and sub-folders) into a OAPP Template file (.oapptemplate).");
            CLIEngine.ShowMessage("You can then share the .oapptemplate file with others from which you can create OAPP's from. You can also optionally choose to upload the .oapptemplate file to the STARNET store so others can search, download and install the OAPP Template. They can then create OAPP's from the template.");
            CLIEngine.ShowDivider();

            string OAPPName = CLIEngine.GetValidInput("What is the name of the OAPP Template?");

            if (OAPPName == "exit")
                return;

            string OAPPDesc = CLIEngine.GetValidInput("What is the description of the OAPP Template?");

            if (OAPPDesc == "exit")
                return;

            object value = CLIEngine.GetValidInputForEnum("What type of OAPP Template do you wish to create?", typeof(OAPPTemplateType));

            if (value != null)
            {
                if (value.ToString() == "exit")
                    return;

                OAPPTemplateType OAPPTemplateType = (OAPPTemplateType)value;

                CLIEngine.ShowWorkingMessage("Generating OAPP Template...");
                lightResult = await STAR.LightAsync(OAPPName, OAPPDesc, OAPPType, genesisType, dnaFolder, genesisFolder, genesisNamespace, providerType);
                      
                if (lightResult != null)
                {
                    if (!lightResult.IsError && lightResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"OAPP Template Successfully Generated. ({lightResult.Message})");
                        ShowOAPP(lightResult.Result.OAPPDNA, lightResult.Result.CelestialBody.CelestialBodyCore.Zomes);
                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation("Do you wish to open the OAPP now?"))
                            Process.Start("explorer.exe", Path.Combine(genesisFolder, string.Concat(OAPPName, " OAPP"), string.Concat(genesisNamespace, ".csproj")));

                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation("Do you wish to open the OAPP folder now?"))
                            Process.Start("explorer.exe", Path.Combine(genesisFolder, string.Concat(OAPPName, " OAPP")));

                        Console.WriteLine("");
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Error Occured: {lightResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"Unknown Error Occured.");
            }
        }
    }
}

