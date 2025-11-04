using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NextGenSoftware.CLI.Engine;
using Org.BouncyCastle.Asn1.Ocsp;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{ 
    public static class TagHelper
    {
        public static void ShowTags(List<string> tags, int displayFieldLength)
        {
            if (tags != null && tags.Count > 0)
            {
                string tagsString = "";

                foreach (string tag in tags)
                    tagsString = string.Concat(tagsString, tag, ", ");

                tagsString = tagsString.Substring(0, tagsString.Length - 2);
                CLIEngine.ShowMessage(string.Concat("Tags:".PadRight(displayFieldLength), tagsString), false);
            }
            else
                CLIEngine.ShowMessage(string.Concat("Tags:".PadRight(displayFieldLength), "None"), false);
        }

        public static List<string> ManageTags(List<string> tags)
        {
            // Allow tags to be added/removed/edited rather than replaced.
            //if (CLIEngine.GetConfirmation("Do you wish to edit the Tags?"))
            //{
                // Start with existing tags if present so user can edit previous values
                //List<string> tags = collectionResult.Result.Tags != null ? new List<string>(collectionResult.Result.Tags) : new List<string>();
                if (tags == null)
                    tags = new List<string>();

                bool done = false;
                while (!done)
                {
                    Console.WriteLine("");
                    CLIEngine.ShowMessage("Current Tags:");
                    if (tags.Count == 0)
                        CLIEngine.ShowMessage("  (none)");
                    else
                    {
                        for (int i = 0; i < tags.Count; i++)
                            CLIEngine.ShowMessage($"  {i + 1}. {tags[i]}");
                    }

                    Console.WriteLine("");
                    CLIEngine.ShowMessage("Choose action: (A)dd, (R)emove, (E)dit, (D)one");
                    string action = CLIEngine.GetValidInput("Enter A, R, E or D:").Trim().ToLower();

                    switch (action)
                    {
                        case "a":
                        case "add":
                            Console.WriteLine("");
                            string newTag = CLIEngine.GetValidInput("Enter Tag to add (or 'cancel' to cancel):");
                            if (!string.IsNullOrWhiteSpace(newTag) && !newTag.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                                tags.Add(newTag.Trim());
                            break;

                        case "r":
                        case "remove":
                            if (tags.Count == 0)
                            {
                                CLIEngine.ShowWarningMessage("No tags to remove.");
                                break;
                            }
                            Console.WriteLine("");
                            string removeInput = CLIEngine.GetValidInput("Enter the number of the tag to remove (or 'cancel'):");
                            if (removeInput.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                                break;
                            if (int.TryParse(removeInput, out int removeIndex) && removeIndex >= 1 && removeIndex <= tags.Count)
                                tags.RemoveAt(removeIndex - 1);
                            else
                                CLIEngine.ShowErrorMessage("Invalid selection. Please try again.");
                            break;

                        case "e":
                        case "edit":
                            if (tags.Count == 0)
                            {
                                CLIEngine.ShowWarningMessage("No tags to edit.");
                                break;
                            }
                            Console.WriteLine("");
                            string editInput = CLIEngine.GetValidInput("Enter the number of the tag to edit (or 'cancel'):");
                            if (editInput.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                                break;
                            if (int.TryParse(editInput, out int editIndex) && editIndex >= 1 && editIndex <= tags.Count)
                            {
                                string editedTag = CLIEngine.GetValidInput($"Enter new value for tag '{tags[editIndex - 1]}' (or 'cancel'):");
                                if (!string.IsNullOrWhiteSpace(editedTag) && !editedTag.Equals("cancel", StringComparison.OrdinalIgnoreCase))
                                    tags[editIndex - 1] = editedTag.Trim();
                            }
                            else
                                CLIEngine.ShowErrorMessage("Invalid selection. Please try again.");
                            break;

                        case "d":
                        case "done":
                            done = true;
                            break;

                        default:
                            CLIEngine.ShowErrorMessage("Unknown action. Please enter A, R, E or D.");
                            break;
                    }
                }
            //}
            //else
            //    Console.WriteLine("");

            return tags;
        }
    }
}
