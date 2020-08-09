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

            //Directory.GetFiles(classFolder)

            string libTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateLib)).OpenText().ReadToEnd();
            string structTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateStruct)).OpenText().ReadToEnd();
            string intTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateInt)).OpenText().ReadToEnd();
            string stringTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateString)).OpenText().ReadToEnd();
            string boolTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateBool)).OpenText().ReadToEnd();

            DirectoryInfo dirInfo = new DirectoryInfo(classFolder);
            FileInfo[] files = dirInfo.GetFiles();
            
            bool classLineReached = false;
            string classBuffer = "";
            string libBuffer = "";

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

                        //if (classLineReached && !buffer.Contains("{") && !buffer.Contains("}"))
                        if (classLineReached && buffer.Contains("string") || buffer.Contains("int") || buffer.Contains("bool"))
                        {
                            string[] parts = buffer.Split(' ');

                            switch (parts[13].ToLower())
                            {
                                case "string":
                                    classBuffer = string.Concat(classBuffer, stringTemplate.Replace("variableName", parts[14].ToSnakeCase()), Environment.NewLine);
                                    break;

                                case "int":
                                    classBuffer = string.Concat(classBuffer, intTemplate.Replace("variableName", parts[14].ToSnakeCase()), Environment.NewLine);
                                    break;

                                case "bool":
                                    classBuffer = string.Concat(classBuffer, boolTemplate.Replace("variableName", parts[14].ToSnakeCase()), Environment.NewLine);
                                    break;
                            }
                        }

                        // Write the class out to the rust lib template. 
                        if (classLineReached && buffer.Length > 1 && buffer.Substring(buffer.Length-1 ,1) == "}" && !buffer.Contains("get;"))
                        {
                            classBuffer = string.Concat(Environment.NewLine, classBuffer, structTemplate.Substring(structTemplate.Length - 1, 1), Environment.NewLine);

                            int zomeNameIndex = libTemplate.IndexOf("zome_name");
                            int zomeBodyStartIndex = libTemplate.IndexOf("{", zomeNameIndex);
                            libBuffer = libBuffer.Insert(zomeBodyStartIndex + 2, classBuffer);
                            classBuffer = "";
                        }

                        if (buffer.Contains("HolochainBaseDataObject"))
                        {
                            string[] parts = buffer.Split(' ');
                            // int length = 

                            //classBuffer = structTemplate.Replace("StructName", parts[10]).Substring(0, structTemplate.Length - 1);
                            classBuffer = structTemplate.Replace("StructName", parts[10].ToSnakeCase());
                            classBuffer = classBuffer.Substring(0, classBuffer.Length - 1);

                            classLineReached = true;
                        }
                    }

                    reader.Close();
                    File.WriteAllText("lib.rs", libBuffer);
                }
            }
        }


       

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
            
            //public string ClassFolder = "RustTemplates\\list.rs"; classFolder
        }
    }
}
