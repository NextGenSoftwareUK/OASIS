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
                        var searchResult = await _cosmicManager.SearchHolonsForParentAsync<Holon>(
                            "",
                            _avatarId,
                            default(Guid),
                            null,
                            MetaKeyValuePairMatchMode.All,
                            showOnlyForCurrentAvatar,
                            searchType,
                            providerType
                        );
                        searchResults = new OASISResult<IEnumerable<IHolon>>();
                        searchResults.Result = searchResult.Result?.Cast<IHolon>();
                        searchResults.IsError = searchResult.IsError;
                        searchResults.Message = searchResult.Message;

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
                    var searchResult = await _cosmicManager.SearchHolonsForParentAsync<Holon>(
                        idOrName,
                        _avatarId,
                        default(Guid),
                        null,
                        MetaKeyValuePairMatchMode.All,
                        showOnlyForCurrentAvatar,
                        searchType,
                        providerType
                    );
                    var searchResults = new OASISResult<IEnumerable<IHolon>>();
                    searchResults.Result = searchResult.Result?.Cast<IHolon>();
                    searchResults.IsError = searchResult.IsError;
                    searchResults.Message = searchResult.Message;

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

                // Check if user owns the parent (must be within user's multiverse)
                var ownershipCheck = await _cosmicManager.IsUserOwnedAsync(parent);
                if (ownershipCheck.IsError || !ownershipCheck.Result)
                {
                    CLIEngine.ShowErrorMessage("You can only create celestial bodies within your own multiverse.");
                    CLIEngine.ShowErrorMessage("System multiverses (MagicVerse, The Grand Simulation) are read-only.");
                    return;
                }

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
                string description = CLIEngine.GetValidInput("Enter a description (optional, press Enter to skip):");
                if (string.IsNullOrWhiteSpace(description))
                    description = "";

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

                // Check if user owns this celestial body (must be within user's multiverse)
                var ownershipCheck = await _cosmicManager.IsUserOwnedAsync(celestialBody);
                if (ownershipCheck.IsError || !ownershipCheck.Result)
                {
                    CLIEngine.ShowErrorMessage("You can only update celestial bodies within your own multiverse.");
                    CLIEngine.ShowErrorMessage("System multiverses (MagicVerse, The Grand Simulation) are read-only.");
                    return;
                }

                Console.WriteLine("");
                CLIEngine.ShowMessage("Enter new values (press Enter to keep current value):", ConsoleColor.Green);

                string newName = CLIEngine.GetValidInput($"Name [{celestialBody.Name}]:");
                if (string.IsNullOrWhiteSpace(newName))
                    newName = celestialBody.Name;
                if (!string.IsNullOrEmpty(newName))
                    celestialBody.Name = newName;

                string newDescription = CLIEngine.GetValidInput($"Description [{celestialBody.Description}]:");
                if (string.IsNullOrWhiteSpace(newDescription))
                    newDescription = celestialBody.Description;
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

                // Check if user owns this celestial body (must be within user's multiverse)
                var ownershipCheck = await _cosmicManager.IsUserOwnedAsync(findResult.Result);
                if (ownershipCheck.IsError || !ownershipCheck.Result)
                {
                    CLIEngine.ShowErrorMessage("You can only delete celestial bodies within your own multiverse.");
                    CLIEngine.ShowErrorMessage("System multiverses (MagicVerse, The Grand Simulation) are read-only.");
                    return;
                }

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
                    var childrenResult = await _cosmicManager.GetChildrenForParentAsync<Holon>(parent, childType);
                    
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
                    var searchResult = await _cosmicManager.SearchHolonsForParentAsync<Holon>(
                        "",
                        _avatarId,
                        default(Guid),
                        null,
                        MetaKeyValuePairMatchMode.All,
                        !showAll,
                        selectedType ?? HolonType.All,
                        ProviderType.Default
                    );
                    result = new OASISResult<IEnumerable<IHolon>>();
                    result.Result = searchResult.Result?.Cast<IHolon>();
                    result.IsError = searchResult.IsError;
                    result.Message = searchResult.Message;
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

                var searchResult = await _cosmicManager.SearchHolonsForParentAsync<Holon>(
                    searchTerm,
                    _avatarId,
                    default(Guid),
                    null,
                    MetaKeyValuePairMatchMode.All,
                    false,
                    HolonType.All,
                    ProviderType.Default
                );
                var result = new OASISResult<IEnumerable<IHolon>>();
                result.Result = searchResult.Result?.Cast<IHolon>();
                result.IsError = searchResult.IsError;
                result.Message = searchResult.Message;

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



    }
}
