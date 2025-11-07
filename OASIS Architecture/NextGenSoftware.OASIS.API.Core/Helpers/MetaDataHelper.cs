using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NextGenSoftware.CLI.Engine;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{ 
    public static class MetaDataHelper
    {
        public static void ShowMetaData(Dictionary<string, object> metaData, int displayFieldLength)
        {
            if (metaData != null)
            {
                CLIEngine.ShowMessage($"MetaData:", false);

                foreach (string key in metaData.Keys)
                    CLIEngine.ShowMessage(string.Concat("".PadRight(displayFieldLength), key, " = ", GetMetaValue(metaData[key])), false);
                    //CLIEngine.ShowMessage(string.Concat("          ", key, " = ", GetMetaValue(metaData[key])), false);
            }
            else
                CLIEngine.ShowMessage(string.Concat("MetaData:".PadRight(displayFieldLength), "None"), false);
        }

        public static string GetMetaData(Dictionary<string, object> metaData)
        {
            string metaDataString = "";

            foreach (string key in metaData.Keys)
                string.Concat(metaDataString, key, " = ", GetMetaValue(metaData[key]), ",");

            metaDataString = metaDataString.Substring(0, metaDataString.Length - 2);
            return metaDataString;
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
                CLIEngine.ShowMessage($"Current {itemName} metadata:");

                if (metaData.Count == 0)
                    CLIEngine.ShowMessage("  (none)");
                else
                {
                    int i = 1;
                    foreach (var kv in metaData)
                    {
                        CLIEngine.ShowMessage($"  {i}. {kv.Key} = {GetMetaValue(kv.Value)}");
                        i++;
                    }
                }

                Console.WriteLine("");
                CLIEngine.ShowMessage("Choose an action: (A)dd, (E)dit, (R)emove, (D)one", false);
                string choice = CLIEngine.GetValidInput("Enter A, E, R or D:").ToUpper();

                switch (choice)
                {
                    case "ADD":
                    case "A":
                        metaData = AddItemToMetaData(metaData);
                        break;

                    case "EDIT":
                    case "E":
                        if (metaData.Count == 0)
                        {
                            CLIEngine.ShowErrorMessage("No metadata to edit.");
                            break;
                        }

                        int editIndex = CLIEngine.GetValidInputForInt("Enter the number of the metadata entry to edit:", true, 1, metaData.Count);
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
                                Console.WriteLine();
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

                    case "REMOVE":
                    case "R":
                        if (metaData.Count == 0)
                        {
                            CLIEngine.ShowErrorMessage("No metadata to remove.");
                            break;
                        }

                        int delIndex = CLIEngine.GetValidInputForInt("Enter the number of the metadata entry to remove:", true, 1, metaData.Count);
                        string delKey = metaData.Keys.ElementAt(delIndex - 1);

                        if (CLIEngine.GetConfirmation($"Are you sure you want to remove metadata '{delKey}'?"))
                        {
                            metaData.Remove(delKey);
                            Console.WriteLine();
                            CLIEngine.ShowSuccessMessage($"Metadata '{delKey}' removed.", lineSpace: true);
                        }
                        else
                            Console.WriteLine("");

                        break;

                    case "DONE":
                    case "D":
                        done = true;
                        break;

                    default:
                        CLIEngine.ShowErrorMessage("Invalid choice. Please enter A, E, R or D.");
                        break;
                }
            }

            return metaData;
        }
    }
}
