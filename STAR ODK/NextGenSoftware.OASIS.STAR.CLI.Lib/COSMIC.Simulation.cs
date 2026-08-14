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
        /// </summary>
        public async Task SimulationProposeWizardAsync()
        {
            ShowHeader("COSMIC - Simulation Proposal Wizard");
            ShowIntro(new List<string>
            {
                "This wizard will guide you through creating a proposal for The Grand Simulation.",
                "Proposals allow you to suggest new bodies/spaces to be added to The Grand Simulation.",
                "Top level for proposals is Universe (not Multiverse).",
                "Once submitted, others can view and vote on your proposal."
            });

            try
            {
                // Get the parent Universe (required, top level for proposals)
                Console.WriteLine("");
                CLIEngine.ShowMessage("Finding parent Universe (required for proposals)...", ConsoleColor.Green);
                var findResult = await FindAsync("create proposal for", "", HolonType.Universe, false);
                
                if (findResult.IsError || findResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error finding parent Universe: {findResult.Message}");
                    return;
                }

                var parentUniverse = findResult.Result;
                if (parentUniverse.HolonType != HolonType.Universe)
                {
                    CLIEngine.ShowErrorMessage($"Parent must be a Universe. Found: {parentUniverse.HolonType}");
                    return;
                }

                CLIEngine.ShowSuccessMessage($"Found parent Universe: {parentUniverse.Name}");

                // Ask if creating a body or space
                Console.WriteLine("");
                CLIEngine.ShowMessage("What would you like to propose?", ConsoleColor.Green);
                Console.WriteLine("  1. Celestial Body");
                Console.WriteLine("  2. Celestial Space");
                string choice = CLIEngine.GetValidInput("Enter your choice (1 or 2):");
                
                bool isBody = choice == "1";
                List<HolonType> types = isBody ? CelestialBodyTypes : CelestialSpaceTypes;

                // Get type
                Console.WriteLine("");
                CLIEngine.ShowMessage($"Available {(isBody ? "Celestial Body" : "Celestial Space")} Types:", ConsoleColor.Green);
                for (int i = 0; i < types.Count; i++)
                {
                    Console.WriteLine($"  {i + 1}. {types[i]}");
                }

                string typeInput = CLIEngine.GetValidInput("\nEnter the number of the type (or type 'exit' to cancel):");
                if (typeInput.ToLower() == "exit")
                    return;

                if (!int.TryParse(typeInput, out int typeIndex) || typeIndex < 1 || typeIndex > types.Count)
                {
                    CLIEngine.ShowErrorMessage("Invalid selection.");
                    return;
                }

                HolonType selectedType = types[typeIndex - 1];

                // Get name and description
                string name = CLIEngine.GetValidInput("Enter the name:");
                if (name.ToLower() == "exit")
                    return;

                string description = CLIEngine.GetValidInput("Enter a description (optional, press Enter to skip):");
                if (string.IsNullOrWhiteSpace(description))
                    description = "";

                // Create the proposed holon (simplified - would need proper STAR factory)
                CLIEngine.ShowWorkingMessage($"Creating proposal for {selectedType} '{name}' in Universe '{parentUniverse.Name}'...");
                
                // Using base Holon type; swap for concrete STAR factory type when available.
                var proposedHolon = new NextGenSoftware.OASIS.API.Core.Holons.Holon
                {
                    Name = name,
                    Description = description,
                    HolonType = selectedType
                };
                
                var proposalResult = await _cosmicManager.CreateSimulationProposalAsync(
                    proposedHolon,
                    parentUniverse.Id
                );

                if (proposalResult.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Error creating proposal: {proposalResult.Message}");
                }
                else
                {
                    CLIEngine.ShowSuccessMessage("Proposal created successfully!");
                    CLIEngine.ShowMessage($"Proposal ID: {proposalResult.Result.Id}", ConsoleColor.Green);
                    CLIEngine.ShowMessage("Others can now view and vote on your proposal.", ConsoleColor.Yellow);
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in wizard: {ex.Message}");
            }
        }

        /// <summary>
        /// List simulation proposals
        /// </summary>
        public async Task SimulationListProposalsWizardAsync(bool onlyMine = false)
        {
            ShowHeader($"COSMIC - Simulation Proposals{(onlyMine ? " (My Proposals)" : "")}");

            try
            {
                CLIEngine.ShowWorkingMessage("Loading proposals...");
                var result = await _cosmicManager.ListSimulationProposalsAsync(onlyMine);

                if (result.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Error loading proposals: {result.Message}");
                }
                else if (result.Result == null || !result.Result.Any())
                {
                    CLIEngine.ShowMessage(onlyMine ? "You have no proposals." : "No proposals found.", ConsoleColor.Yellow);
                }
                else
                {
                    CLIEngine.ShowSuccessMessage($"Found {result.Result.Count()} proposal(s):");
                    Console.WriteLine("");

                    foreach (var proposal in result.Result)
                    {
                        // Get user's vote if they voted
                        var userVoteResult = await _cosmicManager.GetUserVoteOnProposalAsync(proposal.Id);
                        bool? userVote = userVoteResult.Result;

                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"  Proposal: {proposal.Name} ({proposal.HolonType})");
                        Console.ResetColor();
                        Console.WriteLine($"    ID: {proposal.Id}");
                        Console.WriteLine($"    Created by: {proposal.CreatedByAvatarName ?? proposal.CreatedByAvatarId.ToString()}");
                        Console.WriteLine($"    Created: {proposal.CreatedDate:yyyy-MM-dd HH:mm:ss}");
                        Console.WriteLine($"    Description: {proposal.Description ?? "(none)"}");
                        Console.WriteLine($"    Accept Votes: {proposal.AcceptVotes} | Reject Votes: {proposal.RejectVotes}");

                        if (userVote.HasValue)
                        {
                            Console.ForegroundColor = userVote.Value ? ConsoleColor.Green : ConsoleColor.Red;
                            Console.WriteLine($"    Your Vote: {(userVote.Value ? "✓ ACCEPT" : "✗ REJECT")}");
                            Console.ResetColor();
                        }
                        else
                        {
                            Console.WriteLine($"    Your Vote: (not voted yet)");
                        }

                        Console.WriteLine("");
                    }
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in wizard: {ex.Message}");
            }
        }

        /// <summary>
        /// List content of The Grand Simulation (like other list commands)
        /// </summary>
        public async Task SimulationListWizardAsync()
        {
            ShowHeader("COSMIC - List The Grand Simulation");

            try
            {
                // Get The Grand Simulation multiverse
                var grandSimResult = await _cosmicManager.GetGrandSimulationAsync();
                if (grandSimResult.IsError || grandSimResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error loading The Grand Simulation: {grandSimResult.Message}");
                    return;
                }

                var grandSim = grandSimResult.Result;
                CLIEngine.ShowMessage($"The Grand Simulation: {grandSim.Name}", ConsoleColor.Green);
                Console.WriteLine("");

                // Ask if they want to list for a specific parent
                bool listForParent = CLIEngine.GetConfirmation("Do you want to list for a specific parent?");

                IHolon parent = grandSim;
                Guid parentId = grandSim.Id;

                if (listForParent)
                {
                    var findResult = await FindAsync("list children for", "", HolonType.All, false);
                    if (findResult.IsError || findResult.Result == null)
                    {
                        CLIEngine.ShowErrorMessage($"Error finding parent: {findResult.Message}");
                        return;
                    }
                    parent = findResult.Result;
                    parentId = parent.Id;
                    CLIEngine.ShowSuccessMessage($"Found parent: {parent.Name} ({parent.HolonType})");
                }

                // Ask space/body/both
                Console.WriteLine("");
                CLIEngine.ShowMessage("What would you like to list?", ConsoleColor.Green);
                Console.WriteLine("  1. Celestial Spaces");
                Console.WriteLine("  2. Celestial Bodies");
                Console.WriteLine("  3. Both");
                string choice = CLIEngine.GetValidInput("Enter your choice (1, 2, or 3):");

                bool listSpaces = choice == "1" || choice == "3";
                bool listBodies = choice == "2" || choice == "3";

                // Ask which type (or all)
                HolonType? selectedType = null;
                if (listSpaces && listBodies)
                {
                    // Both - ask for specific type or all
                    Console.WriteLine("");
                    CLIEngine.ShowMessage("Which type? (0 for all)", ConsoleColor.Green);
                    // Simplified - would show all types
                }
                else if (listSpaces)
                {
                    // Only spaces
                    Console.WriteLine("");
                    CLIEngine.ShowMessage("Which space type? (0 for all)", ConsoleColor.Green);
                    for (int i = 0; i < CelestialSpaceTypes.Count; i++)
                    {
                        Console.WriteLine($"  {i + 1}. {CelestialSpaceTypes[i]}");
                    }
                    string typeInput = CLIEngine.GetValidInput("Enter number:");
                    if (int.TryParse(typeInput, out int typeIndex) && typeIndex > 0 && typeIndex <= CelestialSpaceTypes.Count)
                    {
                        selectedType = CelestialSpaceTypes[typeIndex - 1];
                    }
                }
                else
                {
                    // Only bodies
                    Console.WriteLine("");
                    CLIEngine.ShowMessage("Which body type? (0 for all)", ConsoleColor.Green);
                    for (int i = 0; i < CelestialBodyTypes.Count; i++)
                    {
                        Console.WriteLine($"  {i + 1}. {CelestialBodyTypes[i]}");
                    }
                    string typeInput = CLIEngine.GetValidInput("Enter number:");
                    if (int.TryParse(typeInput, out int typeIndex) && typeIndex > 0 && typeIndex <= CelestialBodyTypes.Count)
                    {
                        selectedType = CelestialBodyTypes[typeIndex - 1];
                    }
                }

                CLIEngine.ShowWorkingMessage("Loading content...");
                var childrenResult = await _cosmicManager.GetChildrenForParentAsync<IHolon>(parent, selectedType ?? HolonType.All);

                if (childrenResult.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Error loading content: {childrenResult.Message}");
                }
                else if (childrenResult.Result == null || !childrenResult.Result.Any())
                {
                    CLIEngine.ShowMessage("No content found.", ConsoleColor.Yellow);
                }
                else
                {
                    var filtered = childrenResult.Result;
                    if (listSpaces && !listBodies)
                        filtered = filtered.Where(h => CelestialSpaceTypes.Contains(h.HolonType));
                    else if (listBodies && !listSpaces)
                        filtered = filtered.Where(h => CelestialBodyTypes.Contains(h.HolonType));

                    CLIEngine.ShowSuccessMessage($"Found {filtered.Count()} item(s):");
                    Console.WriteLine("");
                    foreach (var item in filtered)
                    {
                        ShowHolonSummary(item);
                    }
                }
            }
            catch (Exception ex)
            {
                CLIEngine.ShowErrorMessage($"Error in wizard: {ex.Message}");
            }
        }

        /// <summary>
        /// List content of MagicVerse (like other list commands)
        /// </summary>
        public async Task ListMagicVerseWizardAsync()
        {
            ShowHeader("COSMIC - List MagicVerse");

            try
            {
                // Get MagicVerse multiverse
                var magicVerseResult = await _cosmicManager.GetMagicVerseAsync();
                if (magicVerseResult.IsError || magicVerseResult.Result == null)
                {
                    CLIEngine.ShowErrorMessage($"Error loading MagicVerse: {magicVerseResult.Message}");
                    return;
                }

                var magicVerse = magicVerseResult.Result;
                CLIEngine.ShowMessage($"MagicVerse: {magicVerse.Name}", ConsoleColor.Green);
                Console.WriteLine("");

                // Similar to SimulationListWizardAsync but for MagicVerse
                // Ask if they want to list for a specific parent
                bool listForParent = CLIEngine.GetConfirmation("Do you want to list for a specific parent?");

                IHolon parent = magicVerse;
                if (listForParent)
                {
                    var findResult = await FindAsync("list children for", "", HolonType.All, false);
                    if (findResult.IsError || findResult.Result == null)
                    {
                        CLIEngine.ShowErrorMessage($"Error finding parent: {findResult.Message}");
                        return;
                    }
                    parent = findResult.Result;
                    CLIEngine.ShowSuccessMessage($"Found parent: {parent.Name} ({parent.HolonType})");
                }

                // Ask space/body/both and type (similar to SimulationListWizardAsync)
                // Simplified for now
                CLIEngine.ShowWorkingMessage("Loading content...");
                var childrenResult = await _cosmicManager.GetChildrenForParentAsync<IHolon>(parent, HolonType.All);

                if (childrenResult.IsError)
                {
                    CLIEngine.ShowErrorMessage($"Error loading content: {childrenResult.Message}");
                }
                else if (childrenResult.Result == null || !childrenResult.Result.Any())
                {
                    CLIEngine.ShowMessage("No content found.", ConsoleColor.Yellow);
                }
                else
                {
                    CLIEngine.ShowSuccessMessage($"Found {childrenResult.Result.Count()} item(s):");
                    Console.WriteLine("");
                    foreach (var item in childrenResult.Result)
                    {
                        ShowHolonSummary(item);
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
