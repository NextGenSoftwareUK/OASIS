using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.STAR.DNA;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    //public class Holons : STARNETUIBase<STARHolon, DownloadedHolon, InstalledHolon, HolonDNA>
    public class Holons : STARNETUIBase<STARHolon, DownloadedHolon, InstalledHolon, STARNETDNA>
    {
        public Holons(Guid avatarId, STARDNA STARDNA) : base(new API.ONODE.Core.Managers.STARHolonManager(avatarId, STARDNA),
            "Welcome to the Holon Wizard", new List<string> 
            {
                "This wizard will allow you create a Holon. Holon's are the basic building blocks of The OASIS and one of their functions is to act as data objects, everything in The OASIS is dervived from a Holon, Holon's can be made of other Holon's also.",
                "The wizard will create an empty folder with a HolonDNA.json file in it. You then simply place any files/folders you need for the assets (optional) for the mission into this folder.",
                "Finally you run the sub-command 'holon publish' to convert the folder containing the holon (can contain any number of files and sub-folders) into a OASIS Holon file (.oholon) as well as optionally upload to STARNET.",
                "You can then share the .oholon file with others across any platform or OS, who can then install the Holon from the file using the sub-command 'holon install'.",
                "You can also optionally choose to upload the .oholon file to the STARNET store so others can search, download and install the holon."
            },
            STAR.STARDNA.DefaultHolonsSourcePath, "DefaultHolonsSourcePath",
            STAR.STARDNA.DefaultHolonsPublishedPath, "DefaultHolonsPublishedPath",
            STAR.STARDNA.DefaultHolonsDownloadedPath, "DefaultHolonsDownloadedPath",
            STAR.STARDNA.DefaultHolonsInstalledPath, "DefaultHolonsInstalledPath")
        { }

        public async Task ShowHolonAsync(string idOrName = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = await LoadHolonAsync(idOrName, "view", providerType);

            if (result != null && !result.IsError && result.Result != null)
                ShowHolonProperties(result.Result);
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the holon. Reason: {result.Message}");
        }

        public async Task ListAllHolonsForForBeamedInAvatar(bool showAllVersions = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IHolon>> holonsResult = await STAR.OASISAPI.Data.LoadHolonsForParentAsync(STAR.BeamedInAvatar.Id, HolonType.All, true, true, 0, true, false, 0, HolonType.All, 0, providerType);

            if (holonsResult != null && holonsResult.Result != null && !holonsResult.IsError)
                ShowHolons(holonsResult.Result);
            else
                CLIEngine.ShowErrorMessage($"Error occured loading holons. Reason: {holonsResult.Message}");
        }

        public async Task ListAllHolonsAsync(bool showAllVersions = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IEnumerable<IHolon>> holonsResult = await STAR.OASISAPI.Data.LoadAllHolonsAsync(HolonType.All, true, true, 0, true, false, HolonType.All, 0, providerType);

            if (holonsResult != null && holonsResult.Result != null && !holonsResult.IsError)
                ShowHolons(holonsResult.Result);
            else
                CLIEngine.ShowErrorMessage($"Error occured loading holons. Reason: {holonsResult.Message}");
        }

        public void ShowHolons(IEnumerable<IHolon> holons, bool showHeader = true, string customHeader = null, int indentBy = 2, int level = 0)
        {
            //Console.WriteLine("");

            if (showHeader)
            {
                if (string.IsNullOrEmpty(customHeader))
                    CLIEngine.ShowMessage(string.Concat(holons.Count(), " Child Holons(s) Found:", false));
                    //CLIEngine.ShowMessage(string.Concat(holons.Count(), " Child Holons(s) Found", holons.Count() > 0 ? ":" : ""), false);
                    //Console.WriteLine(string.Concat(" ", holons.Count(), " Child Holons(s) Found", holons.Count() > 0 ? ":" : ""));
                else
                    CLIEngine.ShowMessage(customHeader, false);
                //Console.WriteLine(customHeader);
            }

            //Console.WriteLine("");
            string indentPadding = "";

            for (int i = 0; i <= indentBy; i++)
                indentPadding = indentPadding.Insert(0, " ");

            //  int parentIndent = indentBy;
            foreach (IHolon holon in holons)
            {
                // indentBy = parentIndent;
                //Console.WriteLine("");
                //CLIEngine.ShowMessage("", false);
                ShowHolonBasicProperties(holon, "", indentPadding, true);
                //Console.WriteLine(string.Concat("   Holon Name: ", holon.Name, " Holon Id: ", holon.Id, ", Holon Type: ", Enum.GetName(typeof(HolonType), holon.HolonType), " containing ", holon.Nodes != null ? holon.Nodes.Count() : 0, " node(s): "));

                if (holon.Nodes != null)
                {
                    foreach (INode node in holon.Nodes)
                    {
                        //Console.WriteLine("");
                        //CLIEngine.ShowMessage("", false);
                        string tree = string.Concat(" |", indentPadding, "  NODE").PadRight(17);
                        //Console.WriteLine(string.Concat(indentPadding, "  | NODE | Name: ", node.NodeName.PadRight(20), " | Id: ", node.Id, " | Type: ", Enum.GetName(node.NodeType).PadRight(10)));
                        //Console.WriteLine(string.Concat(tree, " | Name: ", node.NodeName.PadRight(40), " | Id: ", node.Id, " | Type: ", Enum.GetName(node.NodeType).PadRight(15), " | ".PadRight(30), " | ".PadRight(30), "|"));
                        //CLIEngine.ShowMessage(string.Concat(tree, "| Name: ", node.NodeName.PadRight(20), " | Id: ", node.Id, " | Type: ", Enum.GetName(node.NodeType).PadRight(6), " | ".PadRight(30), " | ".PadRight(24), "|"), false);
                        CLIEngine.ShowMessage(string.Concat(tree, "| Name: ", node.NodeName.PadRight(20), " | Id: ", node.Id, " | Type: ", Enum.GetName(node.NodeType).PadRight(6)), false);
                    }
                }

                if (holon.Children != null && holon.Children.Count > 0)
                {
                    //indentBy += 2;
                    //ShowHolons(holon.Children, showHeader, $"{indentPadding}{holon.Children.Count} Child Sub-Holon(s) Found:", indentBy);
                    ShowHolons(holon.Children, false, "", indentBy + 2, level + 1);
                }
            }

            if (level == 0)
                //Console.WriteLine("");
                CLIEngine.ShowMessage("", false);
        }

        public void ShowHolonBasicProperties(IHolon holon, string prefix = "", string indentBuffer = " ", bool showChildren = true, bool showNodes = true)
        {
            string children = "";
            string nodes = "";

            if (showChildren)
                children = string.Concat(" | Containing ", holon.Children != null ? holon.Children.Count() : 0, " Child Holon(s)");
            else
                children = " |";

            if (showNodes)
                nodes = string.Concat(" | Containing ", holon.Nodes != null ? holon.Nodes.Count() : 0, " Node(s)");
            else
                nodes = " |";

            string tree = string.Concat(" |", indentBuffer, "HOLON").PadRight(17);

            //Console.WriteLine(string.Concat(tree, " | Name: ", holon.Name != null ? holon.Name.PadRight(40) : "".PadRight(40), prefix, " | Id: ", holon.Id, prefix, " | Type: ", Enum.GetName(typeof(HolonType), holon.HolonType).PadRight(15), children.PadRight(30), nodes.PadRight(30), "|"));
            //CLIEngine.ShowMessage(string.Concat(tree, "| Name: ", holon.Name != null ? holon.Name.PadRight(20) : "".PadRight(20), prefix, " | Id: ", holon.Id, prefix, " | Type: ", Enum.GetName(typeof(HolonType), holon.HolonType).PadRight(6), children.PadRight(30), nodes.PadRight(24), "|"), false);
            CLIEngine.ShowMessage(string.Concat(tree, "| Name: ", holon.Name != null ? holon.Name.PadRight(20) : "".PadRight(20), prefix, " | Id: ", holon.Id, prefix, " | Type: ", Enum.GetName(typeof(HolonType), holon.HolonType)), false);
        }

        public void ShowHolonProperties(IHolon holon, bool showChildren = true)
        {
            Console.WriteLine("");
            Console.WriteLine(string.Concat(" Id: ", holon.Id));
            Console.WriteLine(string.Concat(" Holon Type: ", Enum.GetName(typeof(HolonType), holon.HolonType)));
            Console.WriteLine(string.Concat(" Created By Avatar Id: ", holon.CreatedByAvatarId));
            Console.WriteLine(string.Concat(" Created Date: ", holon.CreatedDate));
            Console.WriteLine(string.Concat(" Modifed By Avatar Id: ", holon.ModifiedByAvatarId));
            Console.WriteLine(string.Concat(" Modifed Date: ", holon.ModifiedDate));
            Console.WriteLine(string.Concat(" Name: ", holon.Name));
            Console.WriteLine(string.Concat(" Description: ", holon.Description));
            Console.WriteLine(string.Concat(" Created OASIS Type: ", holon.CreatedOASISType != null ? holon.CreatedOASISType.Name : ""));
            Console.WriteLine(string.Concat(" Created On Provider Type: ", holon.CreatedProviderType != null ? holon.CreatedProviderType.Name : ""));
            Console.WriteLine(string.Concat(" Instance Saved On Provider Type: ", holon.InstanceSavedOnProviderType != null ? holon.InstanceSavedOnProviderType.Name : ""));
            Console.WriteLine(string.Concat(" Active: ", holon.IsActive ? "True" : "False"));
            Console.WriteLine(string.Concat(" Version: ", holon.Version));
            Console.WriteLine(string.Concat(" Version Id: ", holon.VersionId));
            //Console.WriteLine(string.Concat(" Custom Key: ", holon.CustomKey));
            Console.WriteLine(string.Concat(" Dimension Level: ", Enum.GetName(typeof(DimensionLevel), holon.DimensionLevel)));
            Console.WriteLine(string.Concat(" Sub-Dimension Level: ", Enum.GetName(typeof(SubDimensionLevel), holon.SubDimensionLevel)));

            ICelestialHolon celestialHolon = holon as ICelestialHolon;

            if (celestialHolon != null)
            {
                Console.WriteLine(string.Concat(" Age: ", celestialHolon.Age));
                Console.WriteLine(string.Concat(" Colour: ", celestialHolon.Colour));
                Console.WriteLine(string.Concat(" Ecliptic Latitute: ", celestialHolon.EclipticLatitute));
                Console.WriteLine(string.Concat(" Ecliptic Longitute: ", celestialHolon.EclipticLongitute));
                Console.WriteLine(string.Concat(" Equatorial Latitute: ", celestialHolon.EquatorialLatitute));
                Console.WriteLine(string.Concat(" Equatorial Longitute: ", celestialHolon.EquatorialLongitute));
                Console.WriteLine(string.Concat(" Galactic Latitute: ", celestialHolon.GalacticLatitute));
                Console.WriteLine(string.Concat(" Galactic Longitute: ", celestialHolon.GalacticLongitute));
                Console.WriteLine(string.Concat(" Horizontal Latitute: ", celestialHolon.HorizontalLatitute));
                Console.WriteLine(string.Concat(" Horizontal Longitute: ", celestialHolon.HorizontalLongitute));
                Console.WriteLine(string.Concat(" Radius: ", celestialHolon.Radius));
                Console.WriteLine(string.Concat(" Size: ", celestialHolon.Size));
                Console.WriteLine(string.Concat(" Space Quadrant: ", Enum.GetName(typeof(SpaceQuadrantType), celestialHolon.SpaceQuadrant)));
                Console.WriteLine(string.Concat(" Space Sector: ", celestialHolon.SpaceSector));
                Console.WriteLine(string.Concat(" Super Galactic Latitute: ", celestialHolon.SuperGalacticLatitute));
                Console.WriteLine(string.Concat(" Super Galactic Longitute: ", celestialHolon.SuperGalacticLongitute));
                Console.WriteLine(string.Concat(" Temperature: ", celestialHolon.Temperature));
            }

            ICelestialBody celestialBody = holon as ICelestialBody;

            if (celestialBody != null)
            {
                Console.WriteLine(string.Concat(" Current Orbit Angle Of Parent Star: ", celestialBody.Age));
                Console.WriteLine(string.Concat(" Density: ", celestialBody.Age));
                Console.WriteLine(string.Concat(" Distance From Parent Star In Metres: ", celestialBody.Age));
                Console.WriteLine(string.Concat(" Gravitaional Pull: ", celestialBody.Age));
                Console.WriteLine(string.Concat(" Mass: ", celestialBody.Age));
                Console.WriteLine(string.Concat(" Number Active Avatars: ", celestialBody.Age));
                Console.WriteLine(string.Concat(" Number Registered Avatars: ", celestialBody.Age));
                Console.WriteLine(string.Concat(" Orbit Position From Parent Star: ", celestialBody.Age));
                Console.WriteLine(string.Concat(" Rotation Period: ", celestialBody.Age));
                Console.WriteLine(string.Concat(" Rotation Speed: ", celestialBody.Age));
                Console.WriteLine(string.Concat(" Tilt Angle: ", celestialBody.Age));
                Console.WriteLine(string.Concat(" Weight: ", celestialBody.Age));
            }

            if (holon.ParentHolon != null)
                ShowHolonBasicProperties(holon.ParentHolon, "Parent Holon");
            else
                Console.WriteLine(string.Concat(" Parent Holon Id: ", holon.ParentHolonId));


            if (holon.ParentCelestialBody != null)
                ShowHolonBasicProperties(holon.ParentHolon, "Parent Celestial Body");
            else
                Console.WriteLine(string.Concat(" Parent Celestial Body Id: ", holon.ParentCelestialBodyId));


            if (holon.ParentCelestialSpace != null)
                ShowHolonBasicProperties(holon.ParentHolon, "Parent Celestial Space");
            else
                Console.WriteLine(string.Concat(" Parent Celestial Space Id: ", holon.ParentCelestialSpaceId));


            if (holon.ParentGreatGrandSuperStar != null)
                ShowHolonBasicProperties(holon.ParentHolon, "Parent Great Grand Super Star");
            else
                Console.WriteLine(string.Concat(" Parent Great Grand Super Star Id: ", holon.ParentGreatGrandSuperStarId));


            if (holon.ParentGrandSuperStar != null)
                ShowHolonBasicProperties(holon.ParentHolon, "Parent Grand Super Star");
            else
                Console.WriteLine(string.Concat(" Parent Grand Super Star Id: ", holon.ParentGrandSuperStarId));


            if (holon.ParentSuperStar != null)
                ShowHolonBasicProperties(holon.ParentHolon, "Parent Super Star");
            else
                Console.WriteLine(string.Concat(" Parent Super Star Id: ", holon.ParentSuperStarId));


            if (holon.ParentStar != null)
                ShowHolonBasicProperties(holon.ParentHolon, "Parent Star");
            else
                Console.WriteLine(string.Concat(" Parent Star Id: ", holon.ParentStarId));


            if (holon.ParentPlanet != null)
                ShowHolonBasicProperties(holon.ParentHolon, "Parent Planet");
            else
                Console.WriteLine(string.Concat(" Parent Planet Id: ", holon.ParentPlanetId));


            if (holon.ParentMoon != null)
                ShowHolonBasicProperties(holon.ParentHolon, "Parent Moon");
            else
                Console.WriteLine(string.Concat(" Parent Moon Id: ", holon.ParentMoonId));

            if (holon.ParentOmniverse != null)
                ShowHolonBasicProperties(holon.ParentHolon, "Parent Omniverse");
            else
                Console.WriteLine(string.Concat(" Parent Omniverse Id: ", holon.ParentOmniverseId));


            if (holon.ParentMultiverse != null)
                ShowHolonBasicProperties(holon.ParentHolon, "Parent Multiverse");
            else
                Console.WriteLine(string.Concat(" Parent Multiverse Id: ", holon.ParentMultiverseId));


            if (holon.ParentDimension != null)
                ShowHolonBasicProperties(holon.ParentHolon, "Parent Dimension");
            else
                Console.WriteLine(string.Concat(" Parent Dimension Id: ", holon.ParentDimensionId));


            if (holon.ParentUniverse != null)
                ShowHolonBasicProperties(holon.ParentHolon, "Parent Universe");
            else
                Console.WriteLine(string.Concat(" Parent Universe Id: ", holon.ParentUniverseId));


            if (holon.ParentGalaxyCluster != null)
                ShowHolonBasicProperties(holon.ParentHolon, "Parent Galaxy Cluster");
            else
                Console.WriteLine(string.Concat(" Parent Galaxy Cluster Id: ", holon.ParentGalaxyClusterId));


            if (holon.ParentGalaxy != null)
                ShowHolonBasicProperties(holon.ParentHolon, "Parent Galaxy");
            else
                Console.WriteLine(string.Concat(" Parent Galaxy Id: ", holon.ParentGalaxyId));


            if (holon.ParentSolarSystem != null)
                ShowHolonBasicProperties(holon.ParentHolon, "Parent Solar System");
            else
                Console.WriteLine(string.Concat(" Parent Solar System Id: ", holon.ParentSolarSystemId));


            Console.WriteLine(string.Concat(" Children: ", holon.Children.Count));
            Console.WriteLine(string.Concat(" All Children: ", holon.AllChildren.Count));

            if (showChildren)
            {
                ShowHolons(holon.Children);
                //Console.WriteLine("");
            }

            if (holon.MetaData != null && holon.MetaData.Keys.Count > 0)
            {
                Console.WriteLine(string.Concat(" Meta Data: ", holon.MetaData.Keys.Count, " Key(s) Found:"));
                foreach (string key in holon.MetaData.Keys)
                    Console.WriteLine(string.Concat("   ", key, " = ", holon.MetaData[key]));
            }
            else
                Console.WriteLine(string.Concat(" Meta Data: None"));

            if (holon.ProviderMetaData != null && holon.ProviderMetaData.Keys.Count > 0)
            {
                Console.WriteLine(string.Concat(" Provider Meta Data: "));

                foreach (ProviderType providerType in holon.ProviderMetaData.Keys)
                {
                    Console.WriteLine(string.Concat(" Provider: ", Enum.GetName(typeof(ProviderType), providerType)));

                    foreach (string key in holon.ProviderMetaData[providerType].Keys)
                        Console.WriteLine(string.Concat("Key: ", key, "Value: ", holon.ProviderMetaData[providerType][key]));
                }
            }
            else
                Console.WriteLine(string.Concat(" Provider Meta Data: None"));

            Console.WriteLine("");
            Console.WriteLine(string.Concat(" Provider Unique Storage Keys: "));

            foreach (ProviderType providerType in holon.ProviderUniqueStorageKey.Keys)
                Console.WriteLine(string.Concat("   Provider: ", Enum.GetName(typeof(ProviderType), providerType), " = ", holon.ProviderUniqueStorageKey[providerType]));

        }

        public async Task DeleteHolonAsync(string idOrName = "", bool softDelete = true, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = await LoadHolonAsync(idOrName, "delete", providerType);

            if (result != null && !result.IsError && result.Result != null)
            {
                if (CLIEngine.GetConfirmation($"Are you sure you wish to delete the holon with name {result.Result.Name}, id {result.Result.Id} and type {Enum.GetName(typeof(HolonType), result.Result.HolonType)}?"))
                {
                    OASISResult<IHolon> holonResult = await STAR.OASISAPI.Data.DeleteHolonAsync(STAR.BeamedInAvatar.Id, result.Result.Id, true, providerType);

                    if (holonResult != null && !holonResult.IsError && holonResult.Result != null)
                        ShowHolonProperties(holonResult.Result);
                    else
                        CLIEngine.ShowErrorMessage($"Error Occured: {holonResult.Message}");
                }
            }
            else
                CLIEngine.ShowErrorMessage($"An error occured loading the holon. Reason: {result.Message}");
        }

        public async Task<OASISResult<IHolon>> LoadHolonAsync(string idOrName, string operationName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            Guid id = Guid.Empty;

            if (string.IsNullOrEmpty(idOrName))
                idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name of the Holon you wish to {operationName}?");

            CLIEngine.ShowWorkingMessage("Loading Holon...");

            if (Guid.TryParse(idOrName, out id))
                result = await STAR.OASISAPI.Data.LoadHolonAsync(id, true, true, 0, true, false, HolonType.All, 0, providerType);
            else
            {
                OASISResult<IEnumerable<IHolon>> allHolonsResult = await STAR.OASISAPI.Data.LoadAllHolonsAsync();

                if (allHolonsResult != null && allHolonsResult.Result != null && !allHolonsResult.IsError)
                {
                    result.Result = allHolonsResult.Result.FirstOrDefault(x => x.Name == idOrName); //TODO: In future will use Where instead so user can select which Holon they want... (if more than one matches the given name).

                    if (result.Result == null)
                    {
                        result.IsError = true;
                        result.Message = "No Holon Was Found!";
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured calling STAR.OASISAPI.Data.LoadAllHolonsAsync. Reason: {allHolonsResult.Message}");
            }

            return result;
        }

        public async Task<OASISResult<IHolon>> LoadHolon(string idOrName, string operationName, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            Guid id = Guid.Empty;

            if (string.IsNullOrEmpty(idOrName))
                idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name of the Holon you wish to {operationName}?");

            CLIEngine.ShowWorkingMessage("Loading Holon...");

            if (Guid.TryParse(idOrName, out id))
                result = await STAR.OASISAPI.Data.LoadHolonAsync(id, true, true, 0, true, false, HolonType.All, 0, providerType);
            else
            {
                OASISResult<IEnumerable<IHolon>> allHolonsResult = STAR.OASISAPI.Data.LoadAllHolons();

                if (allHolonsResult != null && allHolonsResult.Result != null && !allHolonsResult.IsError)
                {
                    result.Result = allHolonsResult.Result.FirstOrDefault(x => x.Name == idOrName); //TODO: In future will use Where instead so user can select which Holon they want... (if more than one matches the given name).

                    if (result.Result == null)
                    {
                        result.IsError = true;
                        result.Message = "No Holon Was Found!";
                    }
                }
                else
                    CLIEngine.ShowErrorMessage($"An error occured calling STAR.OASISAPI.Data.LoadAllHolons. Reason: {allHolonsResult.Message}");
            }

            return result;
        }

        private void ListHolons<T>(OASISResult<IEnumerable<T>> holons, string holonTypeName, Action<IHolon> showHolonDelicate) where T : IHolon
        {
            if (holons != null)
            {
                if (!holons.IsError)
                {
                    if (holons.Result != null && holons.Result.Count() > 0)
                    {
                        Console.WriteLine();

                        if (holons.Result.Count() == 1)
                            CLIEngine.ShowMessage($"{holons.Result.Count()} {holonTypeName} Found:");
                        else
                            CLIEngine.ShowMessage($"{holons.Result.Count()} {holonTypeName}'s Found:");

                        CLIEngine.ShowDivider();

                        foreach (IOAPP oapp in holons.Result)
                            showHolonDelicate(oapp);
                        //ShowOAPP(oapp);
                    }
                    else
                        CLIEngine.ShowWarningMessage($"No {holonTypeName}'s Found.");
                }
                else
                    CLIEngine.ShowErrorMessage($"Error occured loading {holonTypeName}'s. Reason: {holons.Message}");
            }
            else
                CLIEngine.ShowErrorMessage($"Unknown error occured loading {holonTypeName}'s.");
        }
    }
}