using System.Diagnostics;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONode.Core.Holons;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public static partial class STARCLI
    {
        public static async Task CreateOAPPTemplateAsync(ProviderType providerType = ProviderType.Default)
        {
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

            string OAPPTemplateName = CLIEngine.GetValidInput("What is the name of the OAPP Template?");

            if (OAPPTemplateName == "exit")
                return;

            string OAPPTemplateDesc = CLIEngine.GetValidInput("What is the description of the OAPP Template?");

            if (OAPPTemplateDesc == "exit")
                return;

            object value = CLIEngine.GetValidInputForEnum("What type of OAPP Template do you wish to create?", typeof(OAPPTemplateType));

            if (value != null)
            {
                if (value.ToString() == "exit")
                    return;

                OAPPTemplateType OAPPTemplateType = (OAPPTemplateType)value;
                string oappTemplatePath = "";

                if (!string.IsNullOrEmpty(STAR.STARDNA.BasePath))
                    oappTemplatePath = Path.Combine(STAR.STARDNA.BasePath, STAR.STARDNA.OAPPDNATemplatePath);
                else
                    oappTemplatePath = STAR.STARDNA.OAPPDNATemplatePath;

                if (!CLIEngine.GetConfirmation($"Do you wish to create the OAPP Template in the default path defined in the STARDNA as 'OAPPDNATemplatePath'? The current path points to: {oappTemplatePath}"))
                    oappTemplatePath = CLIEngine.GetValidFolder("Where do you wish to create the OAPP Template?");

                oappTemplatePath = Path.Combine(oappTemplatePath, OAPPTemplateName);

                CLIEngine.ShowWorkingMessage("Generating OAPP Template...");
                OASISResult<IOAPPTemplate> oappTemplateResult = await STAR.OASISAPI.OAPPTemplates.SaveOAPPTemplateAsync(new OAPPTemplate() 
                {
                    Name = OAPPTemplateName,
                    Description = OAPPTemplateDesc,
                    OAPPTemplateType = OAPPTemplateType,
                    OAPPTemplatePath = oappTemplatePath 
                }, STAR.BeamedInAvatar.AvatarId, providerType);


                if (oappTemplateResult != null)
                {
                    if (!oappTemplateResult.IsError && oappTemplateResult.Result != null)
                    {
                        CLIEngine.ShowSuccessMessage($"OAPP Template Successfully Generated. ({oappTemplateResult.Message})");
                        ShowOAPPTemplate(oappTemplateResult.Result);
                        Console.WriteLine("");

                        if (CLIEngine.GetConfirmation("Do you wish to open the OAPP Template folder now?"))
                            Process.Start("explorer.exe", oappTemplatePath);

                        Console.WriteLine("");
                    }
                    else
                        CLIEngine.ShowErrorMessage($"Error Occured: {oappTemplateResult.Message}");
                }
                else
                    CLIEngine.ShowErrorMessage($"Unknown Error Occured.");
            }
        }



        public static void ShowOAPPTemplate(IOAPPTemplate oappTemplate)
        {
            CLIEngine.ShowMessage(string.Concat($"Id:                                         ", oappTemplate.Id != Guid.Empty ? oappTemplate.Id : "None"));
            CLIEngine.ShowMessage(string.Concat($"Name:                                       ", !string.IsNullOrEmpty(oappTemplate.Name) ? oappTemplate.Name : "None"));
            CLIEngine.ShowMessage(string.Concat($"Description:                                ", !string.IsNullOrEmpty(oappTemplate.Description) ? oappTemplate.Description : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Type:                         ", Enum.GetName(typeof(OAPPTemplateType), oappTemplate.OAPPTemplateType)));
            CLIEngine.ShowMessage(string.Concat($"Created On:                                 ", oappTemplate.CreatedDate != DateTime.MinValue ? oappTemplate.CreatedDate.ToString() : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Created By:                                 ", oappTemplate.CreatedByAvatarId != Guid.Empty ? string.Concat(oappTemplate.CreatedByAvatarUsername, " (", oappTemplate.CreatedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Created By:                                 ", oappTemplate.CreatedByAvatarId != Guid.Empty ? string.Concat(oappTemplate.CreatedByAvatar.Username, " (", oappTemplate.CreatedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Published On:                               ", oappTemplate.PublishedOn != DateTime.MinValue ? oappTemplate.PublishedOn.ToString() : "None"));
            //CLIEngine.ShowMessage(string.Concat($"Published By:                               ", oappTemplate.PublishedByAvatarId != Guid.Empty ? string.Concat(oappTemplate.PublishedByAvatarUsername, " (", oappTemplate.PublishedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"Published By:                               ", oappTemplate.PublishedByAvatarId != Guid.Empty ? string.Concat(oappTemplate.PublishedByAvatar.Username, " (", oappTemplate.PublishedByAvatarId.ToString(), ")") : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published Path:                        ", !string.IsNullOrEmpty(oappTemplate.OAPPTemplatePublishedPath) ? oappTemplate.OAPPTemplatePublishedPath : "None"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Filesize:                     ", oappTemplate.OAPPTemplateFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Template Self Contained Published Path:                   ", !string.IsNullOrEmpty(oappTemplate.OAPPSelfContainedPublishedPath) ? oappTemplate.OAPPPublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Filesize:                         ", oappTemplate.OAPPSelfContainedFileSize.ToString()));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published Path:              ", !string.IsNullOrEmpty(oappTemplate.OAPPSelfContainedFullPublishedPath) ? oappTemplate.OAPPPublishedPath : "None"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Filesize:                    ", oappTemplate.OAPPSelfContainedFullFileSize.ToString()));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published On STARNET:                            ", oappTemplate.OAPPTemplatePublishedOnSTARNET ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published To Cloud:           ", oappTemplate.OAPPTemplatePublishedToCloud ? "True" : "False"));
            CLIEngine.ShowMessage(string.Concat($"OAPP Template Published To OASIS Provider:  ", Enum.GetName(typeof(ProviderType), oappTemplate.OAPPTemplatePublishedProviderType)));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To Cloud:               ", oappTemplate.OAPPSelfContainedPublishedToCloud ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Published To OASIS Provider:      ", Enum.GetName(typeof(ProviderType), oappTemplate.OAPPSelfContainedPublishedProviderType)));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To Cloud:          ", oappTemplate.OAPPSelfContainedFullPublishedToCloud ? "True" : "False"));
            //CLIEngine.ShowMessage(string.Concat($"OAPP Self Contained Full Published To OASIS Provider: ", Enum.GetName(typeof(ProviderType), oappTemplate.OAPPSelfContainedFullPublishedProviderType)));{}90
            CLIEngine.ShowMessage(string.Concat($"Version:                                              ", oappTemplate.Version));
            CLIEngine.ShowMessage(string.Concat($"Versions:                                              ", oappTemplate.Versions));
            CLIEngine.ShowMessage(string.Concat($"Downloads:                                              ", oappTemplate.Downloads));
            //CLIEngine.ShowMessage(string.Concat($"STAR ODK Version:                                     ", oappTemplate.STARODKVersion));
            //CLIEngine.ShowMessage(string.Concat($"OASIS Version:                                        ", oappTemplate.OASISVersion));
            //CLIEngine.ShowMessage(string.Concat($"COSMIC Version:                                       ", oappTemplate.COSMICVersion));
            //CLIEngine.ShowMessage(string.Concat($".NET Version:                                         ", oappTemplate.DotNetVersion));

            CLIEngine.ShowDivider();
        }

        private static void ListOAPPTemplates(OASISResult<IEnumerable<IOAPPTemplate>> oappTemplates)
        {
            if (oappTemplates != null)
            { 
                if (!oappTemplates.IsError)
                {
                    if (oappTemplates.Result != null && oappTemplates.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (oappTemplates.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{oappTemplates.Result.Count()} OAPP Templates Found:");
                        else
                            CLIEngine.ShowMessage($"{oappTemplates.Result.Count()} OAPP Templates Found:");

                        CLIEngine.ShowDivider();

                        foreach (IOAPP oapp in oappTemplates.Result)
                            ShowOAPP(oapp);

                        ShowOAPPListFooter();
                    }
                    else
                        CLIEngine.ShowWarningMessage("No OAPP Templates Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading OAPP Temmplates. Reason: {oappTemplates.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading OAPP Templates.");
        }
    }
}