using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NextGenSoftware.CLI.Engine;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{ 
    public static class MetaDataHelper
    {
        public static void ShowMetaData(Dictionary<string, object> metaData)
        {
            if (metaData != null)
            {
                CLIEngine.ShowMessage($"MetaData:");

                foreach (string key in metaData.Keys)
                    CLIEngine.ShowMessage(string.Concat("          ", key, " = ", GetMetaValue(metaData[key])), false);
            }
            else
                CLIEngine.ShowMessage($"MetaData: None");
        }

        public static string GetMetaValue(object value)
        {
            return value != null ? IsBinary(value) ? "<binary>" : value.ToString() : "None";
        }

        public static bool IsBinary(object data)
        {
            if (data == null)
                return false;

            if (data is byte[])
                return true;

            try
            {
                byte[] binaryData = Convert.FromBase64String(data.ToString());

                for (int i = 0; i < binaryData.Length; i++)
                {
                    if (binaryData[i] > 127)
                        return true;
                }
            }
            catch { }

            return false;
        }

        public static Dictionary<string, object> AddMetaData(string holonName)
        {
            Dictionary<string, object> metaData = new Dictionary<string, object>();

            if (CLIEngine.GetConfirmation($"Do you wish to add any metadata to this {holonName}?"))
            {
                metaData = AddItemToMetaData(metaData);
                bool metaDataDone = false;

                do
                {
                    if (CLIEngine.GetConfirmation("Do you wish to add more metadata?"))
                        metaData = AddItemToMetaData(metaData);
                    else
                        metaDataDone = true;
                }
                while (!metaDataDone);
            }

            return metaData;
        }

        public static Dictionary<string, object> AddItemToMetaData(Dictionary<string, object> metaData)
        {
            Console.WriteLine("");
            string key = CLIEngine.GetValidInput("What is the key?");
            string value = "";
            byte[] metaFile = null;

            if (CLIEngine.GetConfirmation("Is the value a file?"))
            {
                Console.WriteLine("");
                string metaPath = CLIEngine.GetValidFile("What is the full path to the file?");
                metaFile = File.ReadAllBytes(metaPath);
            }
            else
            {
                Console.WriteLine("");
                value = CLIEngine.GetValidInput("What is the value?");
            }

            if (metaFile != null)
                metaData[key] = metaFile;
            else
                metaData[key] = value;

            return metaData;
        }

        public static Dictionary<string, object> ManageMetaData(Dictionary<string, object> metaData, string itemName)
        {
            if (metaData == null)
                metaData = new Dictionary<string, object>();

            bool done = false;

            while (!done)
            {
                Console.WriteLine("");
                CLIEngine.ShowMessage($"Current {itemName} metadata:", false);

                if (metaData.Count == 0)
                    CLIEngine.ShowMessage("  None", false);
                else
                {
                    int i = 1;
                    foreach (var kv in metaData)
                    {
                        CLIEngine.ShowMessage($"  {i}. {kv.Key} = {GetMetaValue(kv.Value)}", false);
                        i++;
                    }
                }

                Console.WriteLine("");
                CLIEngine.ShowMessage("Choose an action: (A)dd, (E)dit, (D)elete, (Q)uit", false);
                string choice = CLIEngine.GetValidInput("Enter A, E, D or Q:").ToUpper();

                switch (choice)
                {
                    case "A":
                        metaData = AddItemToMetaData(metaData);
                        break;

                    case "E":
                        if (metaData.Count == 0)
                        {
                            CLIEngine.ShowErrorMessage("No metadata to edit.");
                            break;
                        }

                        int editIndex = CLIEngine.GetValidInputForInt($"Enter the number of the metadata entry to edit (1-{metaData.Count}):");
                        if (editIndex < 1 || editIndex > metaData.Count)
                        {
                            CLIEngine.ShowErrorMessage("Invalid index.");
                            break;
                        }
                        string editKey = metaData.Keys.ElementAt(editIndex - 1);
                        object currentValue = metaData[editKey];

                        if (currentValue is byte[])
                        {
                            if (CLIEngine.GetConfirmation("This value is binary. Do you want to replace it with a file? (Y) or replace with text (N)?"))
                            {
                                string metaPath = CLIEngine.GetValidFile("What is the full path to the file?");
                                metaData[editKey] = File.ReadAllBytes(metaPath);
                            }
                            else
                            {
                                string newValue = CLIEngine.GetValidInput("Enter the new text value (or type 'clear' to remove):");
                                if (newValue.ToLower() == "clear")
                                    metaData.Remove(editKey);
                                else
                                    metaData[editKey] = newValue;
                            }
                        }
                        else
                        {
                            if (CLIEngine.GetConfirmation("Do you want to set this value from a file? (Y) or enter text value (N)?"))
                            {
                                string metaPath = CLIEngine.GetValidFile("What is the full path to the file?");
                                metaData[editKey] = File.ReadAllBytes(metaPath);
                            }
                            else
                            {
                                string newValue = CLIEngine.GetValidInput("Enter the new text value (or type 'clear' to remove):");
                                if (newValue.ToLower() == "clear")
                                    metaData.Remove(editKey);
                                else
                                    metaData[editKey] = newValue;
                            }
                        }

                        break;

                    case "D":
                        if (metaData.Count == 0)
                        {
                            CLIEngine.ShowErrorMessage("No metadata to delete.");
                            break;
                        }

                        int delIndex = CLIEngine.GetValidInputForInt($"Enter the number of the metadata entry to delete (1-{metaData.Count}):");
                        if (delIndex < 1 || delIndex > metaData.Count)
                        {
                            CLIEngine.ShowErrorMessage("Invalid index.");
                            break;
                        }
                        string delKey = metaData.Keys.ElementAt(delIndex - 1);

                        if (CLIEngine.GetConfirmation($"Are you sure you want to delete metadata '{delKey}'?"))
                        {
                            metaData.Remove(delKey);
                            CLIEngine.ShowSuccessMessage($"Metadata '{delKey}' deleted.", lineSpace: true);
                        }
                        else
                            Console.WriteLine("");

                        break;

                    case "Q":
                        done = true;
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Invalid choice. Please enter A, E, D or Q.");
                        break;
                }
            }

            return metaData;
        }
    }
}
