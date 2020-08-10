using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace NextGenSoftware.Holochain.NETHDK.Core
{
    public static class ExtensionMethods
    {
        public static string ToSnakeCase(this string str)
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }
    }

    public class NETHDK
    {
        public void Build(string classFolder)
        {
            if (!Directory.Exists(classFolder))
            {
                throw new ArgumentOutOfRangeException("classFolder", classFolder, "The folder is not valid, please double check and try again.");
            }

          // ConfigSettings settings2 = new ConfigSettings();
         //   SaveConfig(settings2);

            ConfigSettings settings = LoadConfig();

            if (settings != null)
            {
                if (!File.Exists(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateCreate)))
                {
                    throw new ArgumentOutOfRangeException("RustTemplateCreate", settings.RustTemplateCreate, "The folder is not valid, please double check and try again.");
                }

                if (!File.Exists(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateDelete)))
                {
                    throw new ArgumentOutOfRangeException("RustTemplateDelete", settings.RustTemplateDelete, "The folder is not valid, please double check and try again.");
                }

                if (!File.Exists(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateLib)))
                {
                    throw new ArgumentOutOfRangeException("RustTemplateLib", settings.RustTemplateLib, "The folder is not valid, please double check and try again.");
                }

                if (!File.Exists(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateRead)))
                {
                    throw new ArgumentOutOfRangeException("RustTemplateRead", settings.RustTemplateRead, "The folder is not valid, please double check and try again.");
                }

                if (!File.Exists(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateUpdate)))
                {
                    throw new ArgumentOutOfRangeException("RustTemplateUpdate", settings.RustTemplateUpdate, "The folder is not valid, please double check and try again.");
                }

                if (!File.Exists(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateList)))
                {
                    throw new ArgumentOutOfRangeException("RustTemplateList", settings.RustTemplateList, "The folder is not valid, please double check and try again.");
                }
            }

            string libTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateLib)).OpenText().ReadToEnd();
            string createTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateCreate)).OpenText().ReadToEnd();
            string readTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateRead)).OpenText().ReadToEnd();
            string updateTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateUpdate)).OpenText().ReadToEnd();
            string deleteTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateDelete)).OpenText().ReadToEnd();
            string listTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateList)).OpenText().ReadToEnd();
            string structTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateStruct)).OpenText().ReadToEnd();
            string intTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateInt)).OpenText().ReadToEnd();
            string stringTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateString)).OpenText().ReadToEnd();
            string boolTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateBool)).OpenText().ReadToEnd();

            DirectoryInfo dirInfo = new DirectoryInfo(classFolder);
            FileInfo[] files = dirInfo.GetFiles();
            
            bool classLineReached = false;
            string classBuffer = "";
            string libBuffer = "";
            string className = "";
            string classFields = "";
            string classFieldsClone = "";
            int nextLineToWrite = 0;
            bool firstField = true;
            
            foreach (FileInfo file in files)
            {
                if (file != null)
                {
                    StreamReader reader = file.OpenText();

                    while (!reader.EndOfStream)
                    {
                        string buffer = reader.ReadLine();

                        if (buffer.Contains("HolochainBaseZome"))
                        {
                            string[] parts = buffer.Split(' ');
                            libBuffer = libTemplate.Replace("zome_name", parts[6].ToSnakeCase());
                        }

                        if (classLineReached && buffer.Contains("string") || buffer.Contains("int") || buffer.Contains("bool"))
                        {
                            string[] parts = buffer.Split(' ');
                            string fieldName = string.Empty;

                            switch (parts[13].ToLower())
                            {
                                case "string":
                                    {
                                        if (firstField)
                                            firstField = false;
                                        else
                                            classFieldsClone = string.Concat(classFieldsClone, "\t");

                                        fieldName = parts[14].ToSnakeCase();
                                        classFieldsClone = string.Concat(classFieldsClone, className, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);
                                        classBuffer = string.Concat(classBuffer, stringTemplate.Replace("variableName", fieldName), ",", Environment.NewLine);

                                    }
                                    break;

                                case "int":
                                    {
                                        if (firstField)
                                            firstField = false;
                                        else
                                            classFieldsClone = string.Concat(classFieldsClone, "\t");

                                        fieldName = parts[14].ToSnakeCase();
                                        classFieldsClone = string.Concat(classFieldsClone, className, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);
                                        classBuffer = string.Concat(classBuffer, intTemplate.Replace("variableName", fieldName), ",", Environment.NewLine);
                                    }
                                    break;

                                case "bool":
                                    {
                                        if (firstField)
                                            firstField = false;
                                        else
                                            classFieldsClone = string.Concat(classFieldsClone, "\t");

                                        fieldName = parts[14].ToSnakeCase();
                                        classFieldsClone = string.Concat(classFieldsClone, className, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);
                                        classBuffer = string.Concat(classBuffer, boolTemplate.Replace("variableName", fieldName), ",", Environment.NewLine);
                                    }
                                    break;
                            }
                        }

                        // Write the class out to the rust lib template. 
                        if (classLineReached && buffer.Length > 1 && buffer.Substring(buffer.Length-1 ,1) == "}" && !buffer.Contains("get;"))
                        {
                            if (classBuffer.Length >2)
                                classBuffer = classBuffer.Remove(classBuffer.Length - 3);

                            classBuffer = string.Concat(Environment.NewLine, classBuffer, Environment.NewLine, structTemplate.Substring(structTemplate.Length - 1, 1), Environment.NewLine);

                            int zomeNameIndex = libTemplate.IndexOf("zome_name");
                            int zomeBodyStartIndex = libTemplate.IndexOf("{", zomeNameIndex);
                            libBuffer = libBuffer.Insert(zomeBodyStartIndex + 2, classBuffer);
                            
                            if (nextLineToWrite == 0)
                                nextLineToWrite = zomeBodyStartIndex + classBuffer.Length;
                            else
                                nextLineToWrite += classBuffer.Length;

                            //Now insert the CRUD methods for each class.
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, createTemplate.Replace("MyEntry", className).Replace("my_entry", className), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, readTemplate.Replace("MyEntry", className).Replace("my_entry", className), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, updateTemplate.Replace("MyEntry", className).Replace("my_entry", className).Replace("//#CopyFields//", classFieldsClone), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, deleteTemplate.Replace("MyEntry", className).Replace("my_entry", className), Environment.NewLine));
                            //libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, listTemplate, Environment.NewLine));

                            classBuffer = "";
                            classFieldsClone = "";
                            className = "";
                            classLineReached = false;
                            firstField = true;
                        }

                        if (buffer.Contains("HolochainBaseDataObject"))
                        {
                            string[] parts = buffer.Split(' ');
                            className = parts[10].ToSnakeCase();  
                            classBuffer = structTemplate.Replace("StructName", className); 
                            classBuffer = classBuffer.Substring(0, classBuffer.Length - 1);
                            classLineReached = true;
                        }
                    }

                    reader.Close();
                    File.WriteAllText("lib.rs", libBuffer);
                }
            }
        }


        //private void ProcessField(string fieldNameRaw, out string classFieldsClone, out string classBuffer, string template, string className)
        //{
        //    string fieldName = template.Replace("variableName", fieldNameRaw.ToSnakeCase());
        //    classFieldsClone = string.Concat(classFieldsClone, className, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);
        //    classBuffer = string.Concat(classBuffer, fieldName, ",", Environment.NewLine);
        //}

        private ConfigSettings LoadConfig()
        {
            using (StreamReader r = new StreamReader("NETHDKSettings.json"))
            {
                string json = r.ReadToEnd();
                ConfigSettings settings = JsonConvert.DeserializeObject<ConfigSettings> (json);
                return settings;
            }
        }

        private bool SaveConfig(ConfigSettings settings)
        {
            string json = JsonConvert.SerializeObject(settings);
            StreamWriter writer = new StreamWriter("NETHDKSettings.json");
            writer.Write(json);
            writer.Close();
            
            return true;
        }

        class ConfigSettings
        {
            public string RustTemplateFolder = @"C:\Users\david\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.Holochain.HoloNET.HDK\RustTemplates";
            public string RustTemplateLib = "lib.rs";
            public string RustTemplateCreate = "create.rs";
            public string RustTemplateRead = "read.rs";
            public string RustTemplateUpdate = "update.rs";
            public string RustTemplateDelete = "RustTemplates\\delete.rs";
            public string RustTemplateList = "list.rs";
            public string RustTemplateInt = "int.rs";
            public string RustTemplateString = "string.rs";
            public string RustTemplateBool = "bool.rs";
            public string RustTemplateStruct = "struct.rs";
        }
    }
}
