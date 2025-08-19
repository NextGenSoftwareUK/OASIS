using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class Zomes : STARNETUIBase<STARZome, DownloadedZome, InstalledZome, ZomeDNA>
    public class Zomes : STARNETUIBase<STARZome, DownloadedZome, InstalledZome, STARNETDNA>
    {
        public Zomes(Guid avatarId, STARDNA STARDNA) : base(new API.ONODE.Core.Managers.STARZomeManager(avatarId, STARDNA),
            "Welcome to the Zome Wizard", new List<string> 
            {
                "This wizard will allow you create a Zome which contain Holon's. Zome's are like modules and Holon's are the basic building blocks of The OASIS and one of their functions is to act as data objects, everything in The OASIS is dervived from a Holon, Holon's can be made of other Holon's also.",
                "The wizard will create an empty folder with a ZomeDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the zome into this folder.",
                "Finally you run the sub-command 'zome publish' to convert the folder containing the zome (can contain any number of files and sub-folders) into a OASIS Zome file (.ozome) as well as optionally upload to STARNET.",
                "You can then share the .ozome file with others across any platform or OS, who can then install the Zome from the file using the sub-command 'zome install'.",
                "You can also optionally choose to upload the .ozome file to the STARNET store so others can search, download and install the zome."
            },
            STAR.STARDNA.DefaultZomesSourcePath, "DefaultZomesSourcePath",
            STAR.STARDNA.DefaultZomesPublishedPath, "DefaultZomesPublishedPath",
            STAR.STARDNA.DefaultZomesDownloadedPath, "DefaultZomesDownloadedPath",
            STAR.STARDNA.DefaultZomesInstalledPath, "DefaultZomesInstalledPath")
        { }

        public async Task<OASISResult<CoronalEjection>> GenerateZomesAndHolonsAsync(string OAPPName, string OAPPDesc, OAPPType OAPPType, OAPPTemplateType OAPPTemplateType, Guid OAPPTemplateId, int OAPPTemplateVersion, string zomesAndHolonsyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", ProviderType providerType = ProviderType.Default)
        {
            // Create (OApp) by generating dynamic template/scaffolding code.
            CLIEngine.ShowWorkingMessage($"Generating Zomes & Holons...");

            //OASISResult<CoronalEjection> lightResult = STAR.LightAsync(oAPPName, OAPPType, zomesAndHolonsyDNAFolder, genesisFolder, genesisNameSpace).Result;
            OASISResult<CoronalEjection> lightResult = STAR.LightAsync(OAPPName, OAPPDesc, OAPPType, OAPPTemplateType, OAPPTemplateId, OAPPTemplateVersion, zomesAndHolonsyDNAFolder, genesisFolder, genesisNameSpace).Result;

            //Will use settings in the STARDNA.json file.
            //OASISResult<CoronalEjection> lightResult = STAR.LightAsync(oAPPName, OAPPType).Result;

            if (lightResult.IsError)
                CLIEngine.ShowErrorMessage(string.Concat(" ERROR OCCURED. Error Message: ", lightResult.Message));
            else
            {
                int iNoHolons = 0;
                foreach (IZome zome in lightResult.Result.Zomes)
                    iNoHolons += zome.Children.Count();

                CLIEngine.ShowSuccessMessage($"{lightResult.Result.Zomes.Count()} Zomes & {iNoHolons} Holons Generated.");

                Console.WriteLine("");
                STARCLI.Zomes.ShowZomesAndHolons(lightResult.Result.Zomes);
            }

            return lightResult;
        }

        public void ShowZomesAndHolons(IEnumerable<IZome> zomes, string customHeader = null, string indentBuffer = " ")
        {
            if (string.IsNullOrEmpty(customHeader))
                //Console.WriteLine($" {zomes.Count()} Zome(s) Found", zomes.Count() > 0 ? ":" : "");
                Console.WriteLine($" {zomes.Count()} Zome(s) Found:");
            else
                Console.WriteLine(customHeader);

            Console.WriteLine("");

            foreach (IZome zome in zomes)
            {
                //Console.WriteLine(string.Concat("  | ZOME | Name: ", zome.Name.PadRight(20), " | Id: ", zome.Id, " | Containing ", zome.Children.Count(), " Holon(s)", zome.Children.Count > 0 ? ":" : ""));
                string tree = string.Concat(" |", indentBuffer, "ZOME").PadRight(16);
                string children = string.Concat(" | Containing ", zome.Children != null ? zome.Children.Count() : 0, " Child Holon(s)");

                //Console.WriteLine(string.Concat(tree, " | Name: ", zome.Name.PadRight(20), " | Id: ", zome.Id, " | Type: ", "Zome".PadRight(6), children.PadRight(30), " |".PadRight(27), "|"));
                //Console.WriteLine(string.Concat(tree, " | Name: ", zome.Name.PadRight(20), " | Id: ", zome.Id, " | Type: ", "Zome".PadRight(6)));
                CLIEngine.ShowMessage(string.Concat(tree, " | Name: ", zome.Name.PadRight(20), " | Id: ", zome.Id, " | Type: ", "Zome".PadRight(6)), false);
                STARCLI.Holons.ShowHolons(zome.Children, false);
            }
        }
    }
}