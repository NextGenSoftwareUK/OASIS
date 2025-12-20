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

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    /// <summary>
    /// COSMIC CLI module providing wizards and UI for the full COSMIC API
    /// Exposes CRUD, list, search operations for all celestial bodies and spaces
    /// </summary>
    public class COSMIC
    {
        private COSMICManager _cosmicManager;
        private Guid _avatarId;

        public COSMIC(Guid avatarId, OASISDNA oasisDNA = null)
        {
            _avatarId = avatarId;
            _cosmicManager = new COSMICManager(avatarId, oasisDNA);
        }

        #region Celestial Bodies - CRUD Wizards

        public async Task CreateCelestialBodyWizardAsync()
        {
            ShowHeader("COSMIC - Create Celestial Body Wizard");
            ShowIntro(new List<string>
            {
                "This wizard will guide you through creating a new Celestial Body.",
                "You can create various types of celestial bodies such as:",
                "  - Stars",
                "  - Planets",
                "  - Moons",
                "  - Asteroids",
                "  - Comets",
                "  - Nebulas",
                "  - Black Holes",
                "  - And many more..."
            });

            try
            {
                // Get celestial body type
                Console.WriteLine("");
                CLIEngine.ShowMessage("Available Celestial Body Types:", ConsoleColor.Green);
                var bodyTypes = Enum.GetValues(typeof(HolonType))
                    .Cast<HolonType>()
                    .Where(t => t.ToString().Contains("Star") || 
                               t.ToString().Contains("Planet") || 
                               t.ToString().Contains("Moon") ||
                               t.ToString().Contains("Asteroid") ||
                               t.ToString().Contains("Comet") ||
                               t.ToString().Contains("Nebula") ||
                               t.ToString().Contains("BlackHole") ||
                               t.ToString().Contains("Galaxy") ||
                               t.ToString().Contains("SolarSystem") ||
                               t.ToString().Contains("Universe") ||
                               t.ToString().Contains("Multiverse") ||
                               t.ToString().Contains("Omniverse"))
                    .ToList();

                for (int i = 0; i < bodyTypes.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. {bodyTypes[i]}");
                }

                string typeInput = CLIEngine.GetValidInput("\nEnter the number of the celestial body type (or type 'exit' to cancel):");
                if (typeInput.ToLower() == "exit")
                    return;

                if (!int.TryParse(typeInput, out int typeIndex) || typeIndex < 1 || typeIndex > bodyTypes.Count)
                {
                    CLIEngine.ShowErrorMessage("Invalid selection.");
                    return;
                }

                HolonType selectedType = bodyTypes[typeIndex - 1];

                // Get name
                string name = CLIEngine.GetValidInput("Enter the name of the celestial body:");
                if (name.ToLower() == "exit")
                    return;

                // Get description (optional)
                string description = CLIEngine.GetValidInput("Enter a description (optional, press Enter to skip):", allowEmpty: true);

                CLIEngine.ShowWorkingMessage($"Creating {selectedType} '{name}'...");

                // Create the celestial body based on type
                OASISResult<ICelestialBody> result = await CreateCelestialBodyByTypeAsync(selectedType, name, description);

                if (result.IsError || result.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error creating celestial body: {result.Message}");
                }
                else
                {
                    CLIEngine.ShowSuccessMessage($"Celestial body '{name}' created successfully!");
                    ShowCelestialBodyDetails(result.Result);
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in wizard: {ex.Message}");
            }
        }

        private async Task<OASISResult<ICelestialBody>> CreateCelestialBodyByTypeAsync(HolonType type, string name, string description)
        {
            // This is a simplified version - in reality, you'd need to create the appropriate concrete type
            // For now, we'll use a generic approach
            var result = new OASISResult<ICelestialBody>();

            try
            {
                // Note: This would need to be implemented based on the actual COSMICManager methods
                // For now, this is a placeholder structure
                CLIEngine.ShowMessage($"Creating {type} is not yet fully implemented. Please use the specific manager methods.", ConsoleColor.Yellow);
                result.Message = "Not yet implemented - use specific manager methods";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating {type}: {ex.Message}", ex);
            }

            return result;
        }

        public async Task ReadCelestialBodyWizardAsync()
        {
            ShowHeader("COSMIC - Read Celestial Body Wizard");

            try
            {
                string idInput = CLIEngine.GetValidInput("Enter the ID of the celestial body to read (or type 'exit' to cancel):");
                if (idInput.ToLower() == "exit")
                    return;

                if (!Guid.TryParse(idInput, out Guid id))
                {
                    CLIEngine.ShowErrorMessage("Invalid GUID format.");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Loading celestial body...");

                // Use COSMICManager to load - this would need to be implemented based on actual methods
                var result = await _cosmicManager.Data.LoadHolonAsync(id);

                if (result.IsError || result.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error loading celestial body: {result.Message}");
                }
                else
                {
                    CLIEngine.ShowSuccessMessage("Celestial body loaded successfully!");
                    ShowHolonDetails(result.Result);
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
                string idInput = CLIEngine.GetValidInput("Enter the ID of the celestial body to update (or type 'exit' to cancel):");
                if (idInput.ToLower() == "exit")
                    return;

                if (!Guid.TryParse(idInput, out Guid id))
                {
                    CLIEngine.ShowErrorMessage("Invalid GUID format.");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Loading celestial body...");
                var loadResult = await _cosmicManager.Data.LoadHolonAsync(id);

                if (loadResult.IsError || loadResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error loading celestial body: {loadResult.Message}");
                    return;
                }

                var celestialBody = loadResult.Result;
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
                string idInput = CLIEngine.GetValidInput("Enter the ID of the celestial body to delete (or type 'exit' to cancel):");
                if (idInput.ToLower() == "exit")
                    return;

                if (!Guid.TryParse(idInput, out Guid id))
                {
                    CLIEngine.ShowErrorMessage("Invalid GUID format.");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Loading celestial body...");
                var loadResult = await _cosmicManager.Data.LoadHolonAsync(id);

                if (loadResult.IsError || loadResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error loading celestial body: {loadResult.Message}");
                    return;
                }

                ShowHolonDetails(loadResult.Result);

                bool softDelete = CLIEngine.GetConfirmation("Do you want to soft delete (recommended)?");
                bool confirm = CLIEngine.GetConfirmation($"Are you sure you want to delete '{loadResult.Result.Name}'?");

                if (!confirm)
                {
                    CLIEngine.ShowMessage("Deletion cancelled.");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Deleting celestial body...");
                var deleteResult = await loadResult.Result.DeleteAsync(_avatarId, softDelete);

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
                bool showAll = CLIEngine.GetConfirmation("Do you want to list all celestial bodies (not just yours)?");
                bool showDetailed = CLIEngine.GetConfirmation("Do you want to see detailed information?");

                CLIEngine.ShowWorkingMessage("Loading celestial bodies...");

                // This would use COSMICManager search/list methods
                // For now, showing structure
                CLIEngine.ShowMessage("List functionality will be implemented with COSMICManager search methods.", ConsoleColor.Yellow);
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
                string searchTerm = CLIEngine.GetValidInput("Enter search term (name or description):");
                if (string.IsNullOrEmpty(searchTerm))
                {
                    CLIEngine.ShowErrorMessage("Search term cannot be empty.");
                    return;
                }

                CLIEngine.ShowMessage("\nAvailable celestial body types to search:", ConsoleColor.Green);
                var bodyTypes = Enum.GetValues(typeof(HolonType))
                    .Cast<HolonType>()
                    .Where(t => t.ToString().Contains("Star") || 
                               t.ToString().Contains("Planet") || 
                               t.ToString().Contains("Moon"))
                    .ToList();

                for (int i = 0; i < bodyTypes.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. {bodyTypes[i]}");
                }
                Console.WriteLine($"  {bodyTypes.Count + 1}. All Types");

                string typeInput = CLIEngine.GetValidInput("\nSelect type to search (or press Enter for All):", allowEmpty: true);
                HolonType? selectedType = null;

                if (!string.IsNullOrEmpty(typeInput))
                {
                    if (int.TryParse(typeInput, out int typeIndex) && typeIndex >= 1 && typeIndex <= bodyTypes.Count)
                    {
                        selectedType = bodyTypes[typeIndex - 1];
                    }
                }

                CLIEngine.ShowWorkingMessage("Searching...");

                // Use COSMICManager.SearchHolonsForParentAsync
                var result = await _cosmicManager.SearchHolonsForParentAsync<IHolon>(
                    searchTerm,
                    _avatarId,
                    default(Guid),
                    false, // searchOnlyForCurrentAvatar
                    selectedType ?? HolonType.All
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
                    CLIEngine.ShowSuccessMessage($"Found {result.Result.Count()} celestial body(ies):");
                    Console.WriteLine("");
                    foreach (var body in result.Result)
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
                "Celestial Spaces include:",
                "  - Omniverse",
                "  - Multiverse",
                "  - Universe",
                "  - Galaxy Cluster",
                "  - Galaxy",
                "  - Solar System"
            });

            try
            {
                string name = CLIEngine.GetValidInput("Enter the name of the celestial space:");
                if (name.ToLower() == "exit")
                    return;

                string description = CLIEngine.GetValidInput("Enter a description (optional, press Enter to skip):", allowEmpty: true);

                CLIEngine.ShowMessage("Creating celestial space functionality will use COSMICManager methods.", ConsoleColor.Yellow);
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
                string idInput = CLIEngine.GetValidInput("Enter the ID of the celestial space to read (or type 'exit' to cancel):");
                if (idInput.ToLower() == "exit")
                    return;

                if (!Guid.TryParse(idInput, out Guid id))
                {
                    CLIEngine.ShowErrorMessage("Invalid GUID format.");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Loading celestial space...");
                var result = await _cosmicManager.Data.LoadHolonAsync(id);

                if (result.IsError || result.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error loading celestial space: {result.Message}");
                }
                else
                {
                    CLIEngine.ShowSuccessMessage("Celestial space loaded successfully!");
                    ShowHolonDetails(result.Result);
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
                string idInput = CLIEngine.GetValidInput("Enter the ID of the celestial space to update (or type 'exit' to cancel):");
                if (idInput.ToLower() == "exit")
                    return;

                if (!Guid.TryParse(idInput, out Guid id))
                {
                    CLIEngine.ShowErrorMessage("Invalid GUID format.");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Loading celestial space...");
                var loadResult = await _cosmicManager.Data.LoadHolonAsync(id);

                if (loadResult.IsError || loadResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error loading celestial space: {loadResult.Message}");
                    return;
                }

                var space = loadResult.Result;
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
                string idInput = CLIEngine.GetValidInput("Enter the ID of the celestial space to delete (or type 'exit' to cancel):");
                if (idInput.ToLower() == "exit")
                    return;

                if (!Guid.TryParse(idInput, out Guid id))
                {
                    CLIEngine.ShowErrorMessage("Invalid GUID format.");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Loading celestial space...");
                var loadResult = await _cosmicManager.Data.LoadHolonAsync(id);

                if (loadResult.IsError || loadResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error loading celestial space: {loadResult.Message}");
                    return;
                }

                ShowHolonDetails(loadResult.Result);

                bool softDelete = CLIEngine.GetConfirmation("Do you want to soft delete (recommended)?");
                bool confirm = CLIEngine.GetConfirmation($"Are you sure you want to delete '{loadResult.Result.Name}'?");

                if (!confirm)
                {
                    CLIEngine.ShowMessage("Deletion cancelled.");
                    return;
                }

                CLIEngine.ShowWorkingMessage("Deleting celestial space...");
                var deleteResult = await loadResult.Result.DeleteAsync(_avatarId, softDelete);

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
                bool showAll = CLIEngine.GetConfirmation("Do you want to list all celestial spaces (not just yours)?");
                bool showDetailed = CLIEngine.GetConfirmation("Do you want to see detailed information?");

                CLIEngine.ShowWorkingMessage("Loading celestial spaces...");
                CLIEngine.ShowMessage("List functionality will be implemented with COSMICManager search methods.", ConsoleColor.Yellow);
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
                string searchTerm = CLIEngine.GetValidInput("Enter search term (name or description):");
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
                    HolonType.CelestialSpace
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
                    CLIEngine.ShowSuccessMessage($"Found {result.Result.Count()} celestial space(s):");
                    Console.WriteLine("");
                    foreach (var space in result.Result)
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

        private void ShowCelestialBodyDetails(ICelestialBody body)
        {
            if (body == null)
                return;

            Console.WriteLine("");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.WriteLine("  CELESTIAL BODY DETAILS");
            Console.WriteLine("═══════════════════════════════════════════════════════════════");
            Console.ResetColor();
            Console.WriteLine($"  ID: {body.Id}");
            Console.WriteLine($"  Name: {body.Name}");
            Console.WriteLine($"  Description: {body.Description ?? "N/A"}");
            Console.WriteLine($"  Created By: {body.CreatedByAvatarId}");
            Console.WriteLine($"  Created Date: {body.CreatedDate}");
            Console.WriteLine($"  Modified Date: {body.ModifiedDate}");
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

        #endregion
    }
}

