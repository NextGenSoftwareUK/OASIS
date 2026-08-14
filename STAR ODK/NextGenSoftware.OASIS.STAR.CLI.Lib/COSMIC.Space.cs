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
                    var findResult = await FindAsync("create child for", "", HolonType.All, false);
                    
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

                    // Check if user owns the parent (must be within user's multiverse)
                    var ownershipCheck = await _cosmicManager.IsUserOwnedAsync(parent);
                    if (ownershipCheck.IsError || !ownershipCheck.Result)
                    {
                        CLIEngine.ShowErrorMessage("You can only create celestial spaces within your own multiverse.");
                        CLIEngine.ShowErrorMessage("System multiverses (MagicVerse, The Grand Simulation) are read-only.");
                        return;
                    }
                }
                else
                {
                    // Omniverse creation is disabled - only system can create it
                    CLIEngine.ShowErrorMessage("Omniverse creation is restricted. Only the system can create the Omniverse.");
                    return;
                }

                // Prevent creating Multiverse (users get one automatically)
                if (selectedType == HolonType.Multiverse)
                {
                    CLIEngine.ShowErrorMessage("Multiverse creation is restricted. Each user gets one multiverse automatically.");
                    CLIEngine.ShowMessage("If you don't have a multiverse yet, it will be created automatically when you log in.", ConsoleColor.Yellow);
                    return;
                }

                string name = CLIEngine.GetValidInput("Enter the name of the celestial space:");
                if (name.ToLower() == "exit")
                    return;

                string description = CLIEngine.GetValidInput("Enter a description (optional, press Enter to skip):");
                if (string.IsNullOrWhiteSpace(description))
                    description = "";

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
                var findResult = await FindAsync("read", "", HolonType.All, false);

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
                var findResult = await FindAsync("update", "", HolonType.All, false);

                if (findResult.IsError || findResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error loading celestial space: {findResult.Message}");
                    return;
                }

                var space = findResult.Result;
                ShowHolonDetails(space);

                // Check if user owns this celestial space (must be within user's multiverse)
                var ownershipCheck = await _cosmicManager.IsUserOwnedAsync(space);
                if (ownershipCheck.IsError || !ownershipCheck.Result)
                {
                    CLIEngine.ShowErrorMessage("You can only update celestial spaces within your own multiverse.");
                    CLIEngine.ShowErrorMessage("System multiverses (MagicVerse, The Grand Simulation) are read-only.");
                    return;
                }

                Console.WriteLine("");
                CLIEngine.ShowMessage("Enter new values (press Enter to keep current value):", ConsoleColor.Green);

                string newName = CLIEngine.GetValidInput($"Name [{space.Name}]:");
                if (string.IsNullOrWhiteSpace(newName))
                    newName = space.Name;
                else
                    space.Name = newName;

                string newDescription = CLIEngine.GetValidInput($"Description [{space.Description}]:");
                if (string.IsNullOrWhiteSpace(newDescription))
                    newDescription = space.Description;
                else
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
                var findResult = await FindAsync("delete", "", HolonType.All, false);

                if (findResult.IsError || findResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error loading celestial space: {findResult.Message}");
                    return;
                }

                ShowHolonDetails(findResult.Result);

                // Check if user owns this celestial space (must be within user's multiverse)
                var ownershipCheck = await _cosmicManager.IsUserOwnedAsync(findResult.Result);
                if (ownershipCheck.IsError || !ownershipCheck.Result)
                {
                    CLIEngine.ShowErrorMessage("You can only delete celestial spaces within your own multiverse.");
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
                    var findResult = await FindAsync("list children for", "", HolonType.All, false);
                    
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
                    // List all celestial spaces
                    bool showAll = CLIEngine.GetConfirmation("Do you want to list all celestial spaces (not just yours)?");
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


    }
}
