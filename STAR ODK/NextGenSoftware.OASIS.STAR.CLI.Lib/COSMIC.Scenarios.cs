using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class COSMIC
    {

        public async Task ShowScenariosMenuAsync()
        {
            ShowHeader("COSMIC - Common Use Case Scenarios");
            ShowIntro(new List<string>
            {
                "This menu provides common use case scenarios for creating celestial bodies and spaces",
                "with their full child hierarchies while preserving the cyberspace ontology.",
                "",
                "Available scenarios:",
                "  1. Create Universe with Children (Universe -> GalaxyCluster -> Galaxy -> SolarSystem -> Star -> Planet -> Moon)",
                "  2. Create Multiverse with Children (Multiverse -> Universe -> ... -> Moon)",
                "  3. Create Galaxy with Children (Galaxy -> SolarSystem -> Star -> Planet -> Moon)",
                "  4. Create Solar System with Children (SolarSystem -> Star -> Planet -> Moon)",
                "  5. Create Planet with Children (Planet -> Moon(s))",
                "  6. Create Star with Children (Star -> Planet(s) -> Moon(s))"
            });

            try
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage("Select a scenario (or type 'exit' to cancel):", ConsoleColor.Green);
                Console.WriteLine("  1. Create Universe with Children");
                Console.WriteLine("  2. Create Multiverse with Children");
                Console.WriteLine("  3. Create Galaxy with Children");
                Console.WriteLine("  4. Create Solar System with Children");
                Console.WriteLine("  5. Create Planet with Children");
                Console.WriteLine("  6. Create Star with Children");

                string choice = CLIEngine.GetValidInput("\nEnter the number:");
                if (choice.ToLower() == "exit")
                    return;

                if (!int.TryParse(choice, out int scenarioIndex) || scenarioIndex < 1 || scenarioIndex > 6)
                {
                    CLIEngine.ShowErrorMessage("Invalid selection.");
                    return;
                }

                switch (scenarioIndex)
                {
                    case 1:
                        await CreateUniverseWithChildrenScenarioAsync();
                        break;
                    case 2:
                        await CreateMultiverseWithChildrenScenarioAsync();
                        break;
                    case 3:
                        await CreateGalaxyWithChildrenScenarioAsync();
                        break;
                    case 4:
                        await CreateSolarSystemWithChildrenScenarioAsync();
                        break;
                    case 5:
                        await CreatePlanetWithChildrenScenarioAsync();
                        break;
                    case 6:
                        await CreateStarWithChildrenScenarioAsync();
                        break;
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in scenarios menu: {ex.Message}");
            }
        }

        public async Task CreateUniverseWithChildrenScenarioAsync()
        {
            ShowHeader("COSMIC - Create Universe with Children Scenario");

            try
            {
                // Find parent Multiverse
                Console.WriteLine("");
                CLIEngine.ShowMessage("Finding parent Multiverse...", ConsoleColor.Green);
                var multiverseResult = await FindAsync("create universe for", "", HolonType.Multiverse, false);
                
                if (multiverseResult.IsError || multiverseResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error finding Multiverse: {multiverseResult.Message}");
                    return;
                }

                var multiverse = multiverseResult.Result as IMultiverse;
                if (multiverse == null)
                {
                    CLIEngine.ShowErrorMessage("Parent must be a Multiverse.");
                    return;
                }

                CLIEngine.ShowSuccessMessage($"Found Multiverse: {multiverse.Name}");

                // Get Universe details
                string universeName = CLIEngine.GetValidInput("Enter the name of the Universe:");
                if (universeName.ToLower() == "exit")
                    return;

                string description = CLIEngine.GetValidInput("Enter a description (optional, press Enter to skip):");
                if (string.IsNullOrWhiteSpace(description))
                    description = "";

                // Ask which children to create
                bool createGalaxyCluster = CLIEngine.GetConfirmation("Do you want to create GalaxyCluster(s)?");
                bool createGalaxy = createGalaxyCluster && CLIEngine.GetConfirmation("Do you want to create Galaxy(ies)?");
                bool createSolarSystem = createGalaxy && CLIEngine.GetConfirmation("Do you want to create SolarSystem(s)?");
                bool createStar = createSolarSystem && CLIEngine.GetConfirmation("Do you want to create Star(s)?");
                bool createPlanet = createStar && CLIEngine.GetConfirmation("Do you want to create Planet(s)?");
                bool createMoon = createPlanet && CLIEngine.GetConfirmation("Do you want to create Moon(s)?");

                CLIEngine.ShowWorkingMessage($"Creating Universe '{universeName}' with children...");

                // Note: This would need to create concrete Universe instance and call COSMICManager method
                CLIEngine.ShowMessage("Creating Universe with children is not yet fully implemented. Please use STAR.LightAsync or specific manager methods.", ConsoleColor.Yellow);
                // Full implementation requires concrete Universe type from STAR SDK; use STAR.LightAsync or specific manager methods.
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in scenario: {ex.Message}");
            }
        }

        public async Task CreateMultiverseWithChildrenScenarioAsync()
        {
            ShowHeader("COSMIC - Create Multiverse with Children Scenario");

            try
            {
                // Find parent Omniverse
                Console.WriteLine("");
                CLIEngine.ShowMessage("Finding parent Omniverse...", ConsoleColor.Green);
                var omniverseResult = await FindAsync("create multiverse for", "", HolonType.Omniverse, false);
                
                if (omniverseResult.IsError || omniverseResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error finding Omniverse: {omniverseResult.Message}");
                    return;
                }

                var omniverse = omniverseResult.Result as IOmiverse;
                if (omniverse == null)
                {
                    CLIEngine.ShowErrorMessage("Parent must be an Omniverse.");
                    return;
                }

                CLIEngine.ShowSuccessMessage($"Found Omniverse: {omniverse.Name}");

                string multiverseName = CLIEngine.GetValidInput("Enter the name of the Multiverse:");
                if (multiverseName.ToLower() == "exit")
                    return;

                CLIEngine.ShowMessage("Creating Multiverse with children is not yet fully implemented. Please use STAR.LightAsync or specific manager methods.", ConsoleColor.Yellow);
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in scenario: {ex.Message}");
            }
        }

        public async Task CreateGalaxyWithChildrenScenarioAsync()
        {
            ShowHeader("COSMIC - Create Galaxy with Children Scenario");

            try
            {
                // Find parent GalaxyCluster
                Console.WriteLine("");
                CLIEngine.ShowMessage("Finding parent GalaxyCluster...", ConsoleColor.Green);
                var clusterResult = await FindAsync("create galaxy for", "", HolonType.GalaxyCluster, false);
                
                if (clusterResult.IsError || clusterResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error finding GalaxyCluster: {clusterResult.Message}");
                    return;
                }

                var cluster = clusterResult.Result as IGalaxyCluster;
                if (cluster == null)
                {
                    CLIEngine.ShowErrorMessage("Parent must be a GalaxyCluster.");
                    return;
                }

                CLIEngine.ShowSuccessMessage($"Found GalaxyCluster: {cluster.Name}");

                string galaxyName = CLIEngine.GetValidInput("Enter the name of the Galaxy:");
                if (galaxyName.ToLower() == "exit")
                    return;

                CLIEngine.ShowMessage("Creating Galaxy with children is not yet fully implemented. Please use STAR.LightAsync or specific manager methods.", ConsoleColor.Yellow);
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in scenario: {ex.Message}");
            }
        }

        public async Task CreateSolarSystemWithChildrenScenarioAsync()
        {
            ShowHeader("COSMIC - Create Solar System with Children Scenario");

            try
            {
                // Find parent Galaxy
                Console.WriteLine("");
                CLIEngine.ShowMessage("Finding parent Galaxy...", ConsoleColor.Green);
                var galaxyResult = await FindAsync("create solar system for", "", HolonType.Galaxy, false);
                
                if (galaxyResult.IsError || galaxyResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error finding Galaxy: {galaxyResult.Message}");
                    return;
                }

                var galaxy = galaxyResult.Result as IGalaxy;
                if (galaxy == null)
                {
                    CLIEngine.ShowErrorMessage("Parent must be a Galaxy.");
                    return;
                }

                CLIEngine.ShowSuccessMessage($"Found Galaxy: {galaxy.Name}");

                string solarSystemName = CLIEngine.GetValidInput("Enter the name of the Solar System:");
                if (solarSystemName.ToLower() == "exit")
                    return;

                string starName = CLIEngine.GetValidInput("Enter the name of the Star:");
                if (starName.ToLower() == "exit")
                    return;

                CLIEngine.ShowMessage("Creating Solar System with children is not yet fully implemented. Please use STAR.LightAsync or specific manager methods.", ConsoleColor.Yellow);
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in scenario: {ex.Message}");
            }
        }

        public async Task CreatePlanetWithChildrenScenarioAsync()
        {
            ShowHeader("COSMIC - Create Planet with Children Scenario");

            try
            {
                // Find parent SolarSystem
                Console.WriteLine("");
                CLIEngine.ShowMessage("Finding parent Solar System...", ConsoleColor.Green);
                var solarSystemResult = await FindAsync("create planet for", "", HolonType.SolarSystem, false);
                
                if (solarSystemResult.IsError || solarSystemResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error finding Solar System: {solarSystemResult.Message}");
                    return;
                }

                var solarSystem = solarSystemResult.Result as ISolarSystem;
                if (solarSystem == null)
                {
                    CLIEngine.ShowErrorMessage("Parent must be a Solar System.");
                    return;
                }

                CLIEngine.ShowSuccessMessage($"Found Solar System: {solarSystem.Name}");

                string planetName = CLIEngine.GetValidInput("Enter the name of the Planet:");
                if (planetName.ToLower() == "exit")
                    return;

                bool createMoon = CLIEngine.GetConfirmation("Do you want to create Moon(s)?");
                int numberOfMoons = 0;
                if (createMoon)
                {
                    string moonsInput = CLIEngine.GetValidInput("How many moons? (default: 1):");
                    if (string.IsNullOrWhiteSpace(moonsInput))
                        moonsInput = "1";
                    if (!int.TryParse(moonsInput, out numberOfMoons) || numberOfMoons < 1)
                        numberOfMoons = 1;
                }

                CLIEngine.ShowMessage("Creating Planet with children is not yet fully implemented. Please use STAR.LightAsync or specific manager methods.", ConsoleColor.Yellow);
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in scenario: {ex.Message}");
            }
        }

        public async Task CreateStarWithChildrenScenarioAsync()
        {
            ShowHeader("COSMIC - Create Star with Children Scenario");

            try
            {
                // Find parent SolarSystem
                Console.WriteLine("");
                CLIEngine.ShowMessage("Finding parent Solar System...", ConsoleColor.Green);
                var solarSystemResult = await FindAsync("create star for", "", HolonType.SolarSystem, false);
                
                if (solarSystemResult.IsError || solarSystemResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error finding Solar System: {solarSystemResult.Message}");
                    return;
                }

                var solarSystem = solarSystemResult.Result as ISolarSystem;
                if (solarSystem == null)
                {
                    CLIEngine.ShowErrorMessage("Parent must be a Solar System.");
                    return;
                }

                CLIEngine.ShowSuccessMessage($"Found Solar System: {solarSystem.Name}");

                string starName = CLIEngine.GetValidInput("Enter the name of the Star:");
                if (starName.ToLower() == "exit")
                    return;

                bool createPlanet = CLIEngine.GetConfirmation("Do you want to create Planet(s)?");
                int numberOfPlanets = 0;
                if (createPlanet)
                {
                    string planetsInput = CLIEngine.GetValidInput("How many planets? (default: 1):");
                    if (string.IsNullOrWhiteSpace(planetsInput))
                        planetsInput = "1";
                    if (!int.TryParse(planetsInput, out numberOfPlanets) || numberOfPlanets < 1)
                        numberOfPlanets = 1;

                    bool createMoon = CLIEngine.GetConfirmation("Do you want to create Moon(s) for each planet?");
                    int numberOfMoonsPerPlanet = 0;
                    if (createMoon)
                    {
                        string moonsInput = CLIEngine.GetValidInput("How many moons per planet? (default: 1):");
                        if (string.IsNullOrWhiteSpace(moonsInput))
                            moonsInput = "1";
                        if (!int.TryParse(moonsInput, out numberOfMoonsPerPlanet) || numberOfMoonsPerPlanet < 1)
                            numberOfMoonsPerPlanet = 1;
                    }
                }

                CLIEngine.ShowMessage("Creating Star with children is not yet fully implemented. Please use STAR.LightAsync or specific manager methods.", ConsoleColor.Yellow);
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in scenario: {ex.Message}");
            }
        }



        private void ShowHeader(string title)
        {
            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine($"  {title}");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.ResetColor();
            Console.WriteLine("");
        }

        private void ShowIntro(List<string> paragraphs)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            foreach (var paragraph in paragraphs)
            {
                Console.WriteLine($"  {paragraph}");
            }
            Console.ResetColor();
            Console.WriteLine("");
        }

        private void ShowHolonDetails(IHolon holon)
        {
            if (holon == null)
                return;

            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("  HOLON DETAILS");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.ResetColor();
            Console.WriteLine($"  ID: {holon.Id}");
            Console.WriteLine($"  Name: {holon.Name}");
            Console.WriteLine($"  Description: {holon.Description ?? "N/A"}");
            Console.WriteLine($"  Holon Type: {holon.HolonType}");
            Console.WriteLine($"  Created By: {holon.CreatedByAvatarId}");
            Console.WriteLine($"  Created Date: {holon.CreatedDate}");
            Console.WriteLine($"  Modified Date: {holon.ModifiedDate}");
            Console.WriteLine("");
        }

        private void ShowHolonSummary(IHolon holon)
        {
            if (holon == null)
                return;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"  • {holon.Name} ({holon.HolonType})");
            Console.ResetColor();
            Console.WriteLine($"    ID: {holon.Id}");
            if (!string.IsNullOrEmpty(holon.Description))
                Console.WriteLine($"    Description: {holon.Description}");
            Console.WriteLine("");
        }

        private void ListHolons(IEnumerable<IHolon> holons, bool numbered = false)
        {
            if (holons == null || !holons.Any())
            {
                CLIEngine.ShowMessage("No holons found.", ConsoleColor.Yellow);
                return;
            }

            Console.WriteLine("");
            CLIEngine.ShowMessage($"Found {holons.Count()} holon(s):", ConsoleColor.Green);
            Console.WriteLine("");

            int index = 1;
            foreach (var holon in holons)
            {
                if (numbered)
                    Console.Write($"{index}. ");
                ShowHolonSummary(holon);
                index++;
            }
        }



        /// <summary>
        /// Create a proposal for The Grand Simulation
    }
}
