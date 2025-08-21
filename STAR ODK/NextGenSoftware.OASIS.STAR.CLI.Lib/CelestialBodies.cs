using System.Diagnostics;
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
    //public class CelestialBodies : STARNETUIBase<STARCelestialBody, DownloadedCelestialBody, InstalledCelestialBody, CelestialBodyDNA>
    public class CelestialBodies : STARNETUIBase<STARCelestialBody, DownloadedCelestialBody, InstalledCelestialBody, STARNETDNA>
    {
        public CelestialBodies(Guid avatarId, STARDNA STARDNA) : base(new API.ONODE.Core.Managers.CelestialBodyManager(avatarId, STARDNA),
            "Welcome to the Celestial Bodies Wizard", new List<string> 
            {
                "This wizard will allow you create a CelestialBody which contain Zomes's & Holon's.",
                "CelestialBody's can be anything from a Moon, Planet or Star (lot's more too!).",
                "The wizard will create an empty folder with a CelestialBodyDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the mission into this folder.",
                "Finally you run the sub-command 'celestialbody publish' to convert the folder containing the celestialbody (can contain any number of files and sub-folders) into a OASIS CelestialBody file (.ocelestialbody) as well as optionally upload to STARNET.",
                "You can then share the .ocelestialbody file with others across any platform or OS, who can then install the CelestialBody from the file using the sub-command 'celestialbody install'.",
                "You can also optionally choose to upload the .ocelestialbody file to the STARNET store so others can search, download and install the celestialbody."
            },
            STAR.STARDNA.DefaultCelestialBodiesSourcePath, "DefaultCelestialBodiesSourcePath",
            STAR.STARDNA.DefaultCelestialBodiesPublishedPath, "DefaultCelestialBodiesPublishedPath",
            STAR.STARDNA.DefaultCelestialBodiesDownloadedPath, "DefaultCelestialBodiesDownloadedPath",
            STAR.STARDNA.DefaultCelestialBodiesInstalledPath, "DefaultCelestialBodiesInstalledPath")
        { }

        public async Task LoadCelestialBodyAsync<T>(T celestialBody, string name, ProviderType providerType = ProviderType.Default) where T : ICelestialBody, new()
        {
            CLIEngine.ShowWorkingMessage($"Loading {name}...");
            OASISResult<T> celestialBodyResult = await celestialBody.LoadAsync<T>();

            if (celestialBodyResult != null && !celestialBodyResult.IsError && celestialBodyResult.Result != null)
            {
                CLIEngine.ShowSuccessMessage($"{name} Loaded Successfully.");
                STARCLI.Holons.ShowHolonProperties(celestialBodyResult.Result);
                Console.WriteLine("");
                STARCLI.Zomes.ShowZomesAndHolons(celestialBodyResult.Result.CelestialBodyCore.Zomes, string.Concat(" ", name, " Contains ", celestialBodyResult.Result.CelestialBodyCore.Zomes.Count(), " Zome(s)", celestialBodyResult.Result.CelestialBodyCore.Zomes.Count > 0 ? ":" : ""));
            }
        }

        public async Task<OASISResult<CoronalEjection>> GenerateCelestialBodyAsync(string OAPPName, string OAPPDesc, ICelestialBody parentCelestialBody, OAPPType OAPPType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", ProviderType providerType = ProviderType.Default)
        {
            // Create (OApp) by generating dynamic template/scaffolding code.
            string message = $"Generating {Enum.GetName(typeof(GenesisType), genesisType)} '{OAPPName}' (OApp)";

            if (genesisType == GenesisType.Moon && parentCelestialBody != null)
                message = $"{message} For Planet '{parentCelestialBody.Name}'";

            message = $"{message} ...";

            CLIEngine.ShowWorkingMessage(message);

            //Allows the celestialBodyDNAFolder, genesisFolder & genesisNameSpace params to be passed in overridng what is in the STARDNA.json file.
            OASISResult<CoronalEjection> lightResult = STAR.LightAsync(OAPPName, OAPPDesc, OAPPType, OAPPTemplateId, OAPPTemplateVersion, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, parentCelestialBody, providerType).Result;

            //Will use settings in the STARDNA.json file.
            //OASISResult<CoronalEjection> lightResult = STAR.LightAsync(OAPPType, genesisType, name, parentCelestialBody).Result;

            if (lightResult.IsError)
                CLIEngine.ShowErrorMessage(string.Concat(" ERROR OCCURED. Error Message: ", lightResult.Message));
            else
            {
                CLIEngine.ShowSuccessMessage($"{Enum.GetName(typeof(GenesisType), genesisType)} Generated.");

                Console.WriteLine("");
                Console.WriteLine(string.Concat(" Id: ", lightResult.Result.CelestialBody.Id));
                Console.WriteLine(string.Concat(" CreatedByAvatarId: ", lightResult.Result.CelestialBody.CreatedByAvatarId));
                Console.WriteLine(string.Concat(" CreatedDate: ", lightResult.Result.CelestialBody.CreatedDate));
                Console.WriteLine("");
                STARCLI.Zomes.ShowZomesAndHolons(lightResult.Result.CelestialBody.CelestialBodyCore.Zomes, string.Concat($" {Enum.GetName(typeof(GenesisType), genesisType)} contains ", lightResult.Result.CelestialBody.CelestialBodyCore.Zomes.Count(), " Zome(s): "));
            }

            return lightResult;
        }
    }
}