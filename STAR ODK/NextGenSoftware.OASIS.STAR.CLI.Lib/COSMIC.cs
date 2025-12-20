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

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    /// <summary>
    /// COSMIC CLI module providing wizards and UI for the full COSMIC API
    /// Exposes CRUD, list, search, Find, and GetXForY operations for all celestial bodies and spaces
    /// </summary>
    public class COSMIC
    {
        private COSMICManager _cosmicManager;
        private Guid _avatarId;

        // All Celestial Body Types (excluding spaces)
        private static readonly List<HolonType> CelestialBodyTypes = new List<HolonType>
        {
            HolonType.Star,
            HolonType.Planet,
            HolonType.Moon,
            HolonType.Asteroid,
            HolonType.Comet,
            HolonType.Meteroid,
            HolonType.Nebula,
            HolonType.SuperVerse,
            HolonType.WormHole,
            HolonType.BlackHole,
            HolonType.Portal,
            HolonType.StarGate,
            HolonType.SpaceTimeDistortion,
            HolonType.SpaceTimeAbnormally,
            HolonType.TemporalRift,
            HolonType.StarDust,
            HolonType.CosmicWave,
            HolonType.CosmicRay,
            HolonType.GravitationalWave
        };

        // All Celestial Space Types
        private static readonly List<HolonType> CelestialSpaceTypes = new List<HolonType>
        {
            HolonType.Omniverse,
            HolonType.Multiverse,
            HolonType.Universe,
            HolonType.GalaxyCluster,
            HolonType.Galaxy,
            HolonType.SolarSystem
        };

        public COSMIC(Guid avatarId, OASISDNA oasisDNA = null)
        {
            _avatarId = avatarId;
            _cosmicManager = new COSMICManager(avatarId, oasisDNA);
        }

        #region Find Function (like other modules - search by ID or name/title)

        /// <summary>
        /// Find a celestial body or space by ID or name/title (similar to FindAsync in other CLI modules)
        /// </summary>
        public async Task<OASISResult<IHolon>> FindAsync(string operationName, string idOrName = "", HolonType? holonType = null, bool showOnlyForCurrentAvatar = false, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IHolon> result = new OASISResult<IHolon>();
            Guid id = Guid.Empty;

            do
            {
                if (string.IsNullOrEmpty(idOrName))
                {
                    bool cont = true;
                    OASISResult<IEnumerable<IHolon>> searchResults = null;

                    if (!CLIEngine.GetConfirmation($"Do you know the GUID/ID or Name of the celestial body/space you wish to {operationName}? Press 'Y' for Yes or 'N' for No."))
                    {
                        Console.WriteLine("");
                        CLIEngine.ShowWorkingMessage("Loading celestial bodies/spaces...");

                        // Search for all if no type specified
                        HolonType searchType = holonType ?? HolonType.All;
                        searchResults = await _cosmicManager.SearchHolonsForParentAsync<IHolon>(
                            "",
                            _avatarId,
                            default(Guid),
                            showOnlyForCurrentAvatar,
                            searchType,
                            providerType
                        );

                        if (searchResults != null && searchResults.Result != null && !searchResults.IsError && searchResults.Result.Any())
                        {
                            ListHolons(searchResults.Result);
                        }
                        else
                        {
                            CLIEngine.ShowMessage("No celestial bodies/spaces found.", ConsoleColor.Yellow);
                            cont = false;
                        }
                    }
                    else
                        Console.WriteLine("");

                    if (cont)
                        idOrName = CLIEngine.GetValidInput($"What is the GUID/ID or Name of the celestial body/space you wish to {operationName}?");
                    else
                    {
                        idOrName = "nonefound";
                        break;
                    }

                    if (idOrName == "exit")
                        break;
                }

                Console.WriteLine("");

                if (Guid.TryParse(idOrName, out id))
                {
                    CLIEngine.ShowWorkingMessage("Loading celestial body/space...");
                    var loadResult = await _cosmicManager.Data.LoadHolonAsync(id);

                    if (loadResult != null && loadResult.Result != null && !loadResult.IsError)
                    {
                        if (showOnlyForCurrentAvatar && loadResult.Result.CreatedByAvatarId != _avatarId)
                        {
                            CLIEngine.ShowErrorMessage($"You do not have permission to {operationName} this celestial body/space. It was created by another avatar.");
                            result.Result = null;
                        }
                        else
                        {
                            result.Result = loadResult.Result;
                        }
                    }
                    else
                    {
                        CLIEngine.ShowErrorMessage($"Error loading celestial body/space: {loadResult?.Message ?? "Unknown error"}");
                        idOrName = "";
                    }
                }
                else
                {
                    CLIEngine.ShowWorkingMessage("Searching celestial bodies/spaces...");
                    HolonType searchType = holonType ?? HolonType.All;
                    var searchResults = await _cosmicManager.SearchHolonsForParentAsync<IHolon>(
                        idOrName,
                        _avatarId,
                        default(Guid),
                        showOnlyForCurrentAvatar,
                        searchType,
                        providerType
                    );

                    if (searchResults != null && searchResults.Result != null && !searchResults.IsError)
                    {
                        if (searchResults.Result.Count() > 1)
                        {
                            ListHolons(searchResults.Result, true);

                            if (CLIEngine.GetConfirmation("Are any of these correct?"))
                            {
                                Console.WriteLine("");

                                do
                                {
                                    int number = CLIEngine.GetValidInputForInt($"What is the number of the celestial body/space you wish to {operationName}?");

                                    if (number > 0 && number <= searchResults.Result.Count())
                                    {
                                        result.Result = searchResults.Result.ElementAt(number - 1);
                                        break;
                                    }
                                    else
                                    {
                                        CLIEngine.ShowErrorMessage("Invalid number entered. Please try again.");
                                    }
                                } while (true);
                            }
                            else
                            {
                                Console.WriteLine("");
                                idOrName = "";
                            }
                        }
                        else if (searchResults.Result.Count() == 1)
                        {
                            result.Result = searchResults.Result.FirstOrDefault();
                        }
                        else
                        {
                            CLIEngine.ShowErrorMessage("No celestial bodies/spaces found matching your search.");
                            idOrName = "";
                        }
                    }
                    else
                    {
                        CLIEngine.ShowErrorMessage($"Error searching: {searchResults?.Message ?? "Unknown error"}");
                        idOrName = "";
                    }
                }
            } while (result.Result == null && !string.IsNullOrEmpty(idOrName) && idOrName != "exit" && idOrName != "nonefound");

            return result;
        }

        #endregion

        #region Celestial Bodies - CRUD Wizards

        public async Task CreateCelestialBodyWizardAsync()
        {
            ShowHeader("COSMIC - Create Celestial Body Wizard");
            ShowIntro(new List<string>
            {
                "This wizard will guide you through creating a new Celestial Body.",
                "In cyberspace, everything must have a parent (except Omniverse).",
                "You must select a parent (celestial space or planet) for this celestial body."
            });

            try
            {
                // Find the parent (can be any space or a planet) - REQUIRED
                Console.WriteLine("");
                CLIEngine.ShowMessage("Finding parent (can be any celestial space or a planet)...", ConsoleColor.Green);
                var findResult = await FindAsync("create child for", "", HolonType.All, false);
                
                if (findResult.IsError || findResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error finding parent: {findResult.Message}");
                    CLIEngine.ShowErrorMessage("A parent is required to create a celestial body. Cannot create orphan children in cyberspace.");
                    return;
                }

                IHolon parent = findResult.Result;

                // Validate that parent is a space or planet
                if (!CelestialSpaceTypes.Contains(parent.HolonType) && parent.HolonType != HolonType.Planet)
                {
                    CLIEngine.ShowErrorMessage($"Parent must be a celestial space or a planet. Found: {parent.HolonType}");
                    return;
                }

                CLIEngine.ShowSuccessMessage($"Found parent: {parent.Name} ({parent.HolonType})");

                // Get celestial body type
                Console.WriteLine("");
                CLIEngine.ShowMessage("Available Celestial Body Types:", ConsoleColor.Green);
                for (int i = 0; i < CelestialBodyTypes.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. {CelestialBodyTypes[i]}");
                }

                string typeInput = CLIEngine.GetValidInput("\nEnter the number of the celestial body type (or type 'exit' to cancel):");
                if (typeInput.ToLower() == "exit")
                    return;

                if (!int.TryParse(typeInput, out int typeIndex) || typeIndex < 1 || typeIndex > CelestialBodyTypes.Count)
                {
                    CLIEngine.ShowErrorMessage("Invalid selection.");
                    return;
                }

                HolonType selectedType = CelestialBodyTypes[typeIndex - 1];

                // Get name
                string name = CLIEngine.GetValidInput("Enter the name of the celestial body:");
                if (name.ToLower() == "exit")
                    return;

                // Get description (optional)
                string description = CLIEngine.GetValidInput("Enter a description (optional, press Enter to skip):", allowEmpty: true);

                CLIEngine.ShowWorkingMessage($"Creating {selectedType} '{name}' for parent '{parent.Name}'...");

                // Note: Actual creation would need to use STAR.LightAsync or appropriate manager methods
                CLIEngine.ShowMessage($"Creating {selectedType} is not yet fully implemented. Please use the specific manager methods or STAR.LightAsync.", ConsoleColor.Yellow);
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in wizard: {ex.Message}");
            }
        }

        public async Task ReadCelestialBodyWizardAsync()
        {
            ShowHeader("COSMIC - Read Celestial Body Wizard");

            try
            {
                var findResult = await FindAsync("read", "", HolonType.All, false);

                if (findResult.IsError || findResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error loading celestial body: {findResult.Message}");
                }
                else
                {
                    CLIEngine.ShowSuccessMessage("Celestial body loaded successfully!");
                    ShowHolonDetails(findResult.Result);
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in wizard: {ex.Message}");
            }
        }

        public async Task UpdateCelestialBodyWizardAsync()
        {
            ShowHeader("COSMIC - Update Celestial Body Wizard");

            try
            {
                var findResult = await FindAsync("update", "", HolonType.All, false);

                if (findResult.IsError || findResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error loading celestial body: {findResult.Message}");
                    return;
                }

                var celestialBody = findResult.Result;
                ShowHolonDetails(celestialBody);

                Console.WriteLine("");
                CLIEngine.ShowMessage("Enter new values (press Enter to keep current value):", ConsoleColor.Green);

                string newName = CLIEngine.GetValidInput($"Name [{celestialBody.Name}]:", allowEmpty: true);
                if (!string.IsNullOrEmpty(newName))
                    celestialBody.Name = newName;

                string newDescription = CLIEngine.GetValidInput($"Description [{celestialBody.Description}]:", allowEmpty: true);
                if (!string.IsNullOrEmpty(newDescription))
                    celestialBody.Description = newDescription;

                CLIEngine.ShowWorkingMessage("Saving changes...");
                var saveResult = await celestialBody.SaveAsync();

                if (saveResult.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Error updating celestial body: {saveResult.Message}");
                }
                else
                {
                    CLIEngine.ShowSuccessMessage("Celestial body updated successfully!");
                    ShowHolonDetails(celestialBody);
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in wizard: {ex.Message}");
            }
        }

        public async Task DeleteCelestialBodyWizardAsync()
        {
            ShowHeader("COSMIC - Delete Celestial Body Wizard");

            try
            {
                var findResult = await FindAsync("delete", "", HolonType.All, false);

                if (findResult.IsError || findResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error loading celestial body: {findResult.Message}");
                    return;
                }

                ShowHolonDetails(findResult.Result);

                bool softDelete = CLIEngine.GetConfirmation("Do you want to soft delete (recommended)?");
                bool confirm = CLIEngine.GetConfirmation($"Are you sure you want to delete '{findResult.Result.Name}'?");

                if (!confirm)
                {
                    CLIEngine.ShowMessage("Deletion cancelled.");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Deleting celestial body...");
                var deleteResult = await findResult.Result.DeleteAsync(_avatarId, softDelete);

                if (deleteResult.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Error deleting celestial body: {deleteResult.Message}");
                }
                else
                {
                    CLIEngine.ShowSuccessMessage("Celestial body deleted successfully!");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in wizard: {ex.Message}");
            }
        }

        public async Task ListCelestialBodiesWizardAsync()
        {
            ShowHeader("COSMIC - List Celestial Bodies Wizard");

            try
            {
                // Ask if they want to list for a specific parent
                bool listForParent = CLIEngine.GetConfirmation("Do you want to list celestial bodies for a specific parent (space or planet)?");
                
                IHolon parent = null;
                Guid parentId = default(Guid);

                if (listForParent)
                {
                    // Find the parent (can be any space or a planet)
                    Console.WriteLine("");
                    CLIEngine.ShowMessage("Finding parent (can be any celestial space or a planet)...", ConsoleColor.Green);
                    var findResult = await FindAsync("list children for", "", HolonType.All, false);
                    
                    if (findResult.IsError || findResult.Result == null)
                    {
                        CLIEngine.ShowErrorMessage($"Error finding parent: {findResult.Message}");
                        return;
                    }

                    parent = findResult.Result;
                    parentId = parent.Id;

                    // Validate that parent is a space or planet
                    if (!CelestialSpaceTypes.Contains(parent.HolonType) && parent.HolonType != HolonType.Planet)
                    {
                        CLIEngine.ShowErrorMessage($"Parent must be a celestial space or a planet. Found: {parent.HolonType}");
                        return;
                    }

                    CLIEngine.ShowSuccessMessage($"Found parent: {parent.Name} ({parent.HolonType})");
                }

                // Ask which type to list (or all)
                Console.WriteLine("");
                CLIEngine.ShowMessage("Which type of celestial body do you want to list?", ConsoleColor.Green);
                Console.WriteLine("  0. All types");
                for (int i = 0; i < CelestialBodyTypes.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. {CelestialBodyTypes[i]}");
                }

                string typeInput = CLIEngine.GetValidInput("\nEnter the number (or type 'exit' to cancel):");
                if (typeInput.ToLower() == "exit")
                    return;

                if (!int.TryParse(typeInput, out int typeIndex) || typeIndex < 0 || typeIndex > CelestialBodyTypes.Count)
                {
                    CLIEngine.ShowErrorMessage("Invalid selection.");
                    return;
                }

                HolonType? selectedType = typeIndex == 0 ? null : CelestialBodyTypes[typeIndex - 1];
                bool showDetailed = CLIEngine.GetConfirmation("Do you want to see detailed information?");

                CLIEngine.ShowWorkingMessage("Loading celestial bodies...");

                OASISResult<IEnumerable<IHolon>> result;

                if (listForParent && parent != null)
                {
                    // List children of the parent
                    HolonType childType = selectedType ?? HolonType.All;
                    var childrenResult = await _cosmicManager.GetChildrenForParentAsync<IHolon>(parent, childType);
                    
                    if (childrenResult.IsError)
                    {
                        CLIEngine.ShowErrorMessage($"Error loading children: {childrenResult.Message}");
                        return;
                    }

                    result = new OASISResult<IEnumerable<IHolon>>();
                    result.Result = childrenResult.Result;
                }
                else
                {
                    // List all celestial bodies
                    bool showAll = CLIEngine.GetConfirmation("Do you want to list all celestial bodies (not just yours)?");
                    result = await _cosmicManager.SearchHolonsForParentAsync<IHolon>(
                        "",
                        _avatarId,
                        default(Guid),
                        !showAll,
                        selectedType ?? HolonType.All,
                        ProviderType.Default
                    );
                }

                if (result.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Error loading: {result.Message}");
                }
                else if (result.Result == null || !result.Result.Any())
                {
                    CLIEngine.ShowMessage("No celestial bodies found.", ConsoleColor.Yellow);
                }
                else
                {
                    // Filter to only celestial body types
                    var bodies = result.Result.Where(h => CelestialBodyTypes.Contains(h.HolonType));
                    CLIEngine.ShowSuccessMessage($"Found {bodies.Count()} celestial body(ies):");
                    Console.WriteLine("");
                    foreach (var body in bodies)
                    {
                        if (showDetailed)
                            ShowHolonDetails(body);
                        else
                            ShowHolonSummary(body);
                    }
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in wizard: {ex.Message}");
            }
        }

        public async Task SearchCelestialBodiesWizardAsync()
        {
            ShowHeader("COSMIC - Search Celestial Bodies Wizard");

            try
            {
                string searchTerm = CLIEngine.GetValidInput("Enter search term (ID, name or description):");
                if (string.IsNullOrEmpty(searchTerm))
                {
                    CLIEngine.ShowErrorMessage("Search term cannot be empty.");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Searching...");

                var result = await _cosmicManager.SearchHolonsForParentAsync<IHolon>(
                    searchTerm,
                    _avatarId,
                    default(Guid),
                    false,
                    HolonType.All
                );

                if (result.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Error searching: {result.Message}");
                }
                else if (result.Result == null || !result.Result.Any())
                {
                    CLIEngine.ShowMessage("No celestial bodies found matching your search.", ConsoleColor.Yellow);
                }
                else
                {
                    // Filter to only celestial body types
                    var bodies = result.Result.Where(h => CelestialBodyTypes.Contains(h.HolonType));
                    CLIEngine.ShowSuccessMessage($"Found {bodies.Count()} celestial body(ies):");
                    Console.WriteLine("");
                    foreach (var body in bodies)
                    {
                        ShowHolonSummary(body);
                    }
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in wizard: {ex.Message}");
            }
        }

        #endregion

        #region Celestial Spaces - CRUD Wizards

        public async Task CreateCelestialSpaceWizardAsync()
        {
            ShowHeader("COSMIC - Create Celestial Space Wizard");
            ShowIntro(new List<string>
            {
                "This wizard will guide you through creating a new Celestial Space.",
                "Celestial Spaces include: Omniverse, Multiverse, Universe, Galaxy Cluster, Galaxy, and Solar System.",
                "In cyberspace, everything must have a parent (except Omniverse).",
                "You must select a parent (higher-level space) for this celestial space, unless creating an Omniverse."
            });

            try
            {
                // Get celestial space type first to check if it's Omniverse
                Console.WriteLine("");
                CLIEngine.ShowMessage("Available Celestial Space Types:", ConsoleColor.Green);
                for (int i = 0; i < CelestialSpaceTypes.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. {CelestialSpaceTypes[i]}");
                }

                string typeInput = CLIEngine.GetValidInput("\nEnter the number of the celestial space type (or type 'exit' to cancel):");
                if (typeInput.ToLower() == "exit")
                    return;

                if (!int.TryParse(typeInput, out int typeIndex) || typeIndex < 1 || typeIndex > CelestialSpaceTypes.Count)
                {
                    CLIEngine.ShowErrorMessage("Invalid selection.");
                    return;
                }

                HolonType selectedType = CelestialSpaceTypes[typeIndex - 1];

                // Omniverse is the only space that doesn't need a parent
                IHolon parent = null;
                if (selectedType != HolonType.Omniverse)
                {
                    // Find the parent (must be a higher-level space) - REQUIRED (except for Omniverse)
                    Console.WriteLine("");
                    CLIEngine.ShowMessage("Finding parent (must be a celestial space)...", ConsoleColor.Green);
                    var findResult = await FindAsync("create child for", "", HolonType.CelestialSpace, false);
                    
                    if (findResult.IsError || findResult.Result == null)
                    {
                        CLIEngine.ShowErrorMessage($"Error finding parent: {findResult.Message}");
                        CLIEngine.ShowErrorMessage("A parent is required to create a celestial space (except for Omniverse). Cannot create orphan children in cyberspace.");
                        return;
                    }

                    parent = findResult.Result;

                    // Validate that parent is a space
                    if (!CelestialSpaceTypes.Contains(parent.HolonType))
                    {
                        CLIEngine.ShowErrorMessage($"Parent must be a celestial space. Found: {parent.HolonType}");
                        return;
                    }

                    CLIEngine.ShowSuccessMessage($"Found parent: {parent.Name} ({parent.HolonType})");
                }
                else
                {
                    CLIEngine.ShowMessage("Omniverse is the top-level object and does not require a parent.", ConsoleColor.Green);
                }

                string name = CLIEngine.GetValidInput("Enter the name of the celestial space:");
                if (name.ToLower() == "exit")
                    return;

                string description = CLIEngine.GetValidInput("Enter a description (optional, press Enter to skip):", allowEmpty: true);

                CLIEngine.ShowWorkingMessage($"Creating {selectedType} '{name}'{(parent != null ? $" for parent '{parent.Name}'" : " (Omniverse - no parent required)")}...");

                CLIEngine.ShowMessage($"Creating {selectedType} is not yet fully implemented. Please use the specific manager methods or STAR.LightAsync.", ConsoleColor.Yellow);
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in wizard: {ex.Message}");
            }
        }

        public async Task ReadCelestialSpaceWizardAsync()
        {
            ShowHeader("COSMIC - Read Celestial Space Wizard");

            try
            {
                var findResult = await FindAsync("read", "", HolonType.CelestialSpace, false);

                if (findResult.IsError || findResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error loading celestial space: {findResult.Message}");
                }
                else
                {
                    CLIEngine.ShowSuccessMessage("Celestial space loaded successfully!");
                    ShowHolonDetails(findResult.Result);
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in wizard: {ex.Message}");
            }
        }

        public async Task UpdateCelestialSpaceWizardAsync()
        {
            ShowHeader("COSMIC - Update Celestial Space Wizard");

            try
            {
                var findResult = await FindAsync("update", "", HolonType.CelestialSpace, false);

                if (findResult.IsError || findResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error loading celestial space: {findResult.Message}");
                    return;
                }

                var space = findResult.Result;
                ShowHolonDetails(space);

                Console.WriteLine("");
                CLIEngine.ShowMessage("Enter new values (press Enter to keep current value):", ConsoleColor.Green);

                string newName = CLIEngine.GetValidInput($"Name [{space.Name}]:", allowEmpty: true);
                if (!string.IsNullOrEmpty(newName))
                    space.Name = newName;

                string newDescription = CLIEngine.GetValidInput($"Description [{space.Description}]:", allowEmpty: true);
                if (!string.IsNullOrEmpty(newDescription))
                    space.Description = newDescription;

                CLIEngine.ShowWorkingMessage("Saving changes...");
                var saveResult = await space.SaveAsync();

                if (saveResult.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Error updating celestial space: {saveResult.Message}");
                }
                else
                {
                    CLIEngine.ShowSuccessMessage("Celestial space updated successfully!");
                    ShowHolonDetails(space);
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in wizard: {ex.Message}");
            }
        }

        public async Task DeleteCelestialSpaceWizardAsync()
        {
            ShowHeader("COSMIC - Delete Celestial Space Wizard");

            try
            {
                var findResult = await FindAsync("delete", "", HolonType.CelestialSpace, false);

                if (findResult.IsError || findResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error loading celestial space: {findResult.Message}");
                    return;
                }

                ShowHolonDetails(findResult.Result);

                bool softDelete = CLIEngine.GetConfirmation("Do you want to soft delete (recommended)?");
                bool confirm = CLIEngine.GetConfirmation($"Are you sure you want to delete '{findResult.Result.Name}'?");

                if (!confirm)
                {
                    CLIEngine.ShowMessage("Deletion cancelled.");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Deleting celestial space...");
                var deleteResult = await findResult.Result.DeleteAsync(_avatarId, softDelete);

                if (deleteResult.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Error deleting celestial space: {deleteResult.Message}");
                }
                else
                {
                    CLIEngine.ShowSuccessMessage("Celestial space deleted successfully!");
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in wizard: {ex.Message}");
            }
        }

        public async Task ListCelestialSpacesWizardAsync()
        {
            ShowHeader("COSMIC - List Celestial Spaces Wizard");

            try
            {
                // Ask if they want to list for a specific parent
                bool listForParent = CLIEngine.GetConfirmation("Do you want to list celestial spaces for a specific parent (higher-level space)?");
                
                IHolon parent = null;
                Guid parentId = default(Guid);

                if (listForParent)
                {
                    // Find the parent (must be a higher-level space)
                    Console.WriteLine("");
                    CLIEngine.ShowMessage("Finding parent (must be a celestial space)...", ConsoleColor.Green);
                    var findResult = await FindAsync("list children for", "", HolonType.CelestialSpace, false);
                    
                    if (findResult.IsError || findResult.Result == null)
                    {
                        CLIEngine.ShowErrorMessage($"Error finding parent: {findResult.Message}");
                        return;
                    }

                    parent = findResult.Result;
                    parentId = parent.Id;

                    // Validate that parent is a space
                    if (!CelestialSpaceTypes.Contains(parent.HolonType))
                    {
                        CLIEngine.ShowErrorMessage($"Parent must be a celestial space. Found: {parent.HolonType}");
                        return;
                    }

                    CLIEngine.ShowSuccessMessage($"Found parent: {parent.Name} ({parent.HolonType})");
                }

                // Ask which type to list (or all)
                Console.WriteLine("");
                CLIEngine.ShowMessage("Which type of celestial space do you want to list?", ConsoleColor.Green);
                Console.WriteLine("  0. All types");
                for (int i = 0; i < CelestialSpaceTypes.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. {CelestialSpaceTypes[i]}");
                }

                string typeInput = CLIEngine.GetValidInput("\nEnter the number (or type 'exit' to cancel):");
                if (typeInput.ToLower() == "exit")
                    return;

                if (!int.TryParse(typeInput, out int typeIndex) || typeIndex < 0 || typeIndex > CelestialSpaceTypes.Count)
                {
                    CLIEngine.ShowErrorMessage("Invalid selection.");
                    return;
                }

                HolonType? selectedType = typeIndex == 0 ? null : CelestialSpaceTypes[typeIndex - 1];
                bool showDetailed = CLIEngine.GetConfirmation("Do you want to see detailed information?");

                CLIEngine.ShowWorkingMessage("Loading celestial spaces...");

                OASISResult<IEnumerable<IHolon>> result;

                if (listForParent && parent != null)
                {
                    // List children of the parent
                    HolonType childType = selectedType ?? HolonType.All;
                    var childrenResult = await _cosmicManager.GetChildrenForParentAsync<IHolon>(parent, childType);
                    
                    if (childrenResult.IsError)
                    {
                        CLIEngine.ShowErrorMessage($"Error loading children: {childrenResult.Message}");
                        return;
                    }

                    result = new OASISResult<IEnumerable<IHolon>>();
                    result.Result = childrenResult.Result;
                }
                else
                {
                    // List all celestial spaces
                    bool showAll = CLIEngine.GetConfirmation("Do you want to list all celestial spaces (not just yours)?");
                    result = await _cosmicManager.SearchHolonsForParentAsync<IHolon>(
                        "",
                        _avatarId,
                        default(Guid),
                        !showAll,
                        selectedType ?? HolonType.All
                    );
                }

                if (result.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Error loading: {result.Message}");
                }
                else if (result.Result == null || !result.Result.Any())
                {
                    CLIEngine.ShowMessage("No celestial spaces found.", ConsoleColor.Yellow);
                }
                else
                {
                    // Filter to only celestial space types
                    var spaces = result.Result.Where(h => CelestialSpaceTypes.Contains(h.HolonType));
                    CLIEngine.ShowSuccessMessage($"Found {spaces.Count()} celestial space(s):");
                    Console.WriteLine("");
                    foreach (var space in spaces)
                    {
                        if (showDetailed)
                            ShowHolonDetails(space);
                        else
                            ShowHolonSummary(space);
                    }
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in wizard: {ex.Message}");
            }
        }

        public async Task SearchCelestialSpacesWizardAsync()
        {
            ShowHeader("COSMIC - Search Celestial Spaces Wizard");

            try
            {
                string searchTerm = CLIEngine.GetValidInput("Enter search term (ID, name or description):");
                if (string.IsNullOrEmpty(searchTerm))
                {
                    CLIEngine.ShowErrorMessage("Search term cannot be empty.");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Searching...");

                var result = await _cosmicManager.SearchHolonsForParentAsync<IHolon>(
                    searchTerm,
                    _avatarId,
                    default(Guid),
                    false,
                    HolonType.All
                );

                if (result.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Error searching: {result.Message}");
                }
                else if (result.Result == null || !result.Result.Any())
                {
                    CLIEngine.ShowMessage("No celestial spaces found matching your search.", ConsoleColor.Yellow);
                }
                else
                {
                    // Filter to only celestial space types
                    var spaces = result.Result.Where(h => CelestialSpaceTypes.Contains(h.HolonType));
                    CLIEngine.ShowSuccessMessage($"Found {spaces.Count()} celestial space(s):");
                    Console.WriteLine("");
                    foreach (var space in spaces)
                    {
                        ShowHolonSummary(space);
                    }
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in wizard: {ex.Message}");
            }
        }

        #endregion

        #region Common Use Case Scenarios

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

                string description = CLIEngine.GetValidInput("Enter a description (optional, press Enter to skip):", allowEmpty: true);

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
                // TODO: Implement using STAR.LightAsync or create Universe instance and call _cosmicManager.CreateUniverseWithChildrenAsync
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
                    string moonsInput = CLIEngine.GetValidInput("How many moons? (default: 1):", allowEmpty: true);
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
                    string planetsInput = CLIEngine.GetValidInput("How many planets? (default: 1):", allowEmpty: true);
                    if (!int.TryParse(planetsInput, out numberOfPlanets) || numberOfPlanets < 1)
                        numberOfPlanets = 1;

                    bool createMoon = CLIEngine.GetConfirmation("Do you want to create Moon(s) for each planet?");
                    int numberOfMoonsPerPlanet = 0;
                    if (createMoon)
                    {
                        string moonsInput = CLIEngine.GetValidInput("How many moons per planet? (default: 1):", allowEmpty: true);
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

        #endregion

        #region Helper Methods

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

        #endregion
    }
}
