using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public static class ExtensionMethods
    {
        public static string ToSnakeCase(this string str)
        {
            return string.Concat(str.Select((x, i) => i > 0 && char.IsUpper(x) ? "_" + x.ToString() : x.ToString())).ToLower();
        }

        public static string ToCamelCase(this string str)
        {
            return Char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        public static string ToPascalCase(this string str)
        {
            Regex invalidCharsRgx = new Regex("[^_a-zA-Z0-9]");
            Regex whiteSpace = new Regex(@"(?<=\s)");
            Regex startsWithLowerCaseChar = new Regex("^[a-z]");
            Regex firstCharFollowedByUpperCasesOnly = new Regex("(?<=[A-Z])[A-Z0-9]+$");
            Regex lowerCaseNextToNumber = new Regex("(?<=[0-9])[a-z]");
            Regex upperCaseInside = new Regex("(?<=[A-Z])[A-Z]+?((?=[A-Z][a-z])|(?=[0-9]))");

            // replace white spaces with undescore, then replace all invalid chars with empty string
            var pascalCase = invalidCharsRgx.Replace(whiteSpace.Replace(str, "_"), string.Empty)
                // split by underscores
                .Split(new char[] { '_' }, StringSplitOptions.RemoveEmptyEntries)
                // set first letter to uppercase
                .Select(w => startsWithLowerCaseChar.Replace(w, m => m.Value.ToUpper()))
                // replace second and all following upper case letters to lower if there is no next lower (ABC -> Abc)
                .Select(w => firstCharFollowedByUpperCasesOnly.Replace(w, m => m.Value.ToLower()))
                // set upper case the first lower case following a number (Ab9cd -> Ab9Cd)
                .Select(w => lowerCaseNextToNumber.Replace(w, m => m.Value.ToUpper()))
                // lower second and next upper case letters except the last if it follows by any lower (ABcDEf -> AbcDef)
                .Select(w => upperCaseInside.Replace(w, m => m.Value.ToLower()));

            return string.Concat(pascalCase);
        }
    }

    public class NETHDK
    {
        public void Build(string classFolder)
        {
            ConfigSettings settings;

            if (!string.IsNullOrEmpty(classFolder) && !Directory.Exists(classFolder))
                throw new ArgumentOutOfRangeException("classFolder", classFolder, "The folder is not valid, please double check and try again.");

            if (File.Exists("NETHDKSettings.json"))
                settings = LoadConfig();
            else
            {
                settings = new ConfigSettings();
                SaveConfig(settings);
            }            

            if (settings != null)
            {
                if (!Directory.Exists(settings.ProxyClassesToGenerateFromFolder))
                    throw new ArgumentOutOfRangeException("ProxyClassesToGenerateFromFolder", settings.ProxyClassesToGenerateFromFolder, "The ProxyClassesToGenerateFromFolder folder is not valid, please double check and try again.");

                if (!Directory.Exists(settings.GeneratedCSharpFilesFolder))
                    throw new ArgumentOutOfRangeException("GeneratedCSharpFilesFolder", settings.GeneratedCSharpFilesFolder, "The GeneratedCSharpFilesFolder folder is not valid, please double check and try again.");

                if (!Directory.Exists(settings.GeneratedRustFilesFolder))
                    throw new ArgumentOutOfRangeException("GeneratedRustFilesFolder", settings.GeneratedCSharpFilesFolder, "The GeneratedRustFilesFolder folder is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateCreate)))
                    throw new ArgumentOutOfRangeException("RustTemplateCreate", string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateCreate), "The RustTemplateCreate file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateDelete)))
                    throw new ArgumentOutOfRangeException("RustTemplateDelete", string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateDelete), "The RustTemplateDelete file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateLib)))
                    throw new ArgumentOutOfRangeException("RustTemplateLib", string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateLib), "The RustTemplateLib file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateRead)))
                    throw new ArgumentOutOfRangeException("RustTemplateRead", string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateRead), "The RustTemplateRead file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateUpdate)))
                    throw new ArgumentOutOfRangeException("RustTemplateUpdate", string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateUpdate), "The RustTemplateUpdate file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateList)))
                    throw new ArgumentOutOfRangeException("RustTemplateList", string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateList), "The RustTemplateList file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateValidation)))
                    throw new ArgumentOutOfRangeException("RustTemplateValidation", string.Concat(settings.RustTemplateValidation, "\\", settings.RustTemplateList), "The RustTemplateValidation file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(settings.CSharpTemplateFolder, "\\", settings.CSharpTemplateMyZomeEventArgs)))
                    throw new ArgumentOutOfRangeException("CSharpTemplateEventArgs", string.Concat(settings.CSharpTemplateMyZomeEventArgs, "\\", settings.CSharpTemplateMyZomeEventArgs), "The CSharpTemplateEventArgs file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(settings.CSharpTemplateFolder, "\\", settings.CSharpTemplateIMyClass)))
                    throw new ArgumentOutOfRangeException("CSharpTemplateIMyClass", string.Concat(settings.CSharpTemplateFolder, "\\", settings.CSharpTemplateIMyClass), "The CSharpTemplateIMyClass file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(settings.CSharpTemplateFolder, "\\", settings.CSharpTemplateMyClass)))
                    throw new ArgumentOutOfRangeException("CSharpTemplateMyClass", string.Concat(settings.CSharpTemplateFolder, "\\", settings.CSharpTemplateMyClass), "The CSharpTemplateMyClass file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(settings.CSharpTemplateFolder, "\\", settings.CSharpTemplateMyZome)))
                    throw new ArgumentOutOfRangeException("CSharpTemplateMyZome", string.Concat(settings.CSharpTemplateFolder, "\\", settings.CSharpTemplateMyZome), "The CSharpTemplateMyZome file is not valid, please double check and try again.");
            }

            string libTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateLib)).OpenText().ReadToEnd();
            string createTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateCreate)).OpenText().ReadToEnd();
            string readTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateRead)).OpenText().ReadToEnd();
            string updateTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateUpdate)).OpenText().ReadToEnd();
            string deleteTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateDelete)).OpenText().ReadToEnd();
            string listTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateList)).OpenText().ReadToEnd();
            string validationTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateValidation)).OpenText().ReadToEnd();
            string structTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateStruct)).OpenText().ReadToEnd();
            string intTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateInt)).OpenText().ReadToEnd();
            string stringTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateString)).OpenText().ReadToEnd();
            string boolTemplate = new FileInfo(string.Concat(settings.RustTemplateFolder, "\\", settings.RustTemplateBool)).OpenText().ReadToEnd();
            string myZomeEventArgsTemplate = new FileInfo(string.Concat(settings.CSharpTemplateFolder, "\\", settings.CSharpTemplateMyZomeEventArgs)).OpenText().ReadToEnd();
            string iMyClassTemplate = new FileInfo(string.Concat(settings.CSharpTemplateFolder, "\\", settings.CSharpTemplateIMyClass)).OpenText().ReadToEnd();
            string myClassTemplate = new FileInfo(string.Concat(settings.CSharpTemplateFolder, "\\", settings.CSharpTemplateMyClass)).OpenText().ReadToEnd();
            string myZomeTemplate = new FileInfo(string.Concat(settings.CSharpTemplateFolder, "\\", settings.CSharpTemplateMyZome)).OpenText().ReadToEnd();

            //If folder is not passed in via command line args then use default in config file.
            if (string.IsNullOrEmpty(classFolder))
                classFolder = settings.ProxyClassesToGenerateFromFolder;

            DirectoryInfo dirInfo = new DirectoryInfo(classFolder);
            FileInfo[] files = dirInfo.GetFiles();

            //bool classLineReached = false;
            bool classHolochainDataObjectReached = false;
            bool classHolochainZometReached = false;
            string classBuffer = "";
            string libBuffer = "";
            string zomeBuffer = "";
            string className = "";
            string zomeName = "";
            string classFieldsClone = "";
            int nextLineToWrite = 0;
            bool firstField = true;
            string myZomeEventArgsBuffer = "";
            string iMyClassBuffer = "";
            string myClassBuffer = "";
            string myZomeBuffer = "";
            string classList = "";
            string namespaceName = "";

            foreach (FileInfo file in files)
            {
                if (file != null)
                {
                    StreamReader reader = file.OpenText();

                    while (!reader.EndOfStream)
                    {
                        string buffer = reader.ReadLine();

                        //if (buffer.Concat(classHolochainZometReached))


                        if (buffer.Contains("namespace"))
                        {
                            string[] parts = buffer.Split(' ');
                            myZomeBuffer = myZomeTemplate.Replace("NextGenSoftware.Holochain.HoloNET.HDK.Core.CSharpTemplates", parts[1]);
                        }

                        if (buffer.Contains("HolochainBaseZome"))
                        //if (buffer.Contains("HolochainBaseProxyZome"))
                        {
                            string[] parts = buffer.Split(' ');

                            myZomeEventArgsBuffer = myZomeEventArgsTemplate.Replace("MyZome", parts[6]);
                            libBuffer = libTemplate.Replace("zome_name", parts[6].ToSnakeCase());

                            myZomeBuffer = myZomeBuffer.Replace("MyZome", parts[6].ToPascalCase());
                            myZomeBuffer = myZomeBuffer.Replace("MYZOME", parts[6].ToUpper());
                            myZomeBuffer = myZomeBuffer.Replace("my_zome", parts[6].ToSnakeCase());
                            zomeName = parts[6].ToPascalCase();
                        }

                        if (classHolochainDataObjectReached && buffer.Contains("string") || buffer.Contains("int") || buffer.Contains("bool"))
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

                                        //int index = classBuffer.IndexOf("//#CopyFields//");
                                        classBuffer = string.Concat(classBuffer, stringTemplate.Replace("variableName", fieldName), ",", Environment.NewLine);

                                       // classBuffer = classBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, createTemplate.Replace("MyEntry", className.ToPascalCase()).Replace("my_entry", className), Environment.NewLine));
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
                        if (classHolochainDataObjectReached && buffer.Length > 1 && buffer.Substring(buffer.Length-1 ,1) == "}" && !buffer.Contains("get;"))
                        {
                            if (classBuffer.Length >2)
                                classBuffer = classBuffer.Remove(classBuffer.Length - 3);

                            classBuffer = string.Concat(Environment.NewLine, classBuffer, Environment.NewLine, structTemplate.Substring(structTemplate.Length - 1, 1), Environment.NewLine);

                            //int zomeNameIndex = libTemplate.IndexOf("zome_name");
                            int zomeIndex = libTemplate.IndexOf("#[zome]");
                            int zomeBodyStartIndex = libTemplate.IndexOf("{", zomeIndex);
                            
                            libBuffer = libBuffer.Insert(zomeIndex - 2, classBuffer);
                            // libBuffer = libBuffer.Insert(zomeBodyStartIndex + 2, classBuffer);

                            if (nextLineToWrite == 0)
                                nextLineToWrite = zomeBodyStartIndex + classBuffer.Length;
                            else
                                nextLineToWrite += classBuffer.Length;

                            //Now insert the CRUD methods for each class.
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, createTemplate.Replace("MyEntry", className.ToPascalCase()).Replace("my_entry", className), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, readTemplate.Replace("MyEntry", className.ToPascalCase()).Replace("my_entry", className), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, updateTemplate.Replace("MyEntry", className.ToPascalCase()).Replace("my_entry", className).Replace("//#CopyFields//", classFieldsClone), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, deleteTemplate.Replace("MyEntry", className.ToPascalCase()).Replace("my_entry", className), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, validationTemplate.Replace("MyEntry", className.ToPascalCase()).Replace("my_entry", className), Environment.NewLine));
                            //libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, listTemplate, Environment.NewLine));

                            classBuffer = "";
                            classFieldsClone = "";
                            classHolochainDataObjectReached = false;
                            classHolochainZometReached = false;
                            firstField = true;
                            className = className.ToPascalCase();

                            File.WriteAllText(string.Concat(settings.GeneratedCSharpFilesFolder, "\\", className, ".cs"), myClassBuffer);
                            File.WriteAllText(string.Concat(settings.GeneratedCSharpFilesFolder, "\\I", className, ".cs"), iMyClassBuffer);

                            className = "";
                        }

                        if (buffer.Contains("HolochainBaseDataObject"))
                        {
                            string[] parts = buffer.Split(' ');
                            className = parts[10].ToPascalCase();
                            classList = string.Concat(classList, className, ", ");

                            classBuffer = structTemplate.Replace("MyEntry", className).Replace("my_entry", className.ToSnakeCase());
                            classBuffer = classBuffer.Substring(0, classBuffer.Length - 1);

                            //Process the CSharp Templates.
                            myZomeEventArgsBuffer = myZomeEventArgsBuffer.Replace("MyClass", parts[10]);
                            myClassBuffer = myClassTemplate.Replace("MyClass", parts[10]);
                            iMyClassBuffer = iMyClassTemplate.Replace("MyClass", parts[10]);

                            myZomeBuffer = myZomeBuffer.Replace("MyClass", parts[10].ToPascalCase());
                            myZomeBuffer = myZomeBuffer.Replace("MYCLASS", parts[10].ToUpper());
                            myZomeBuffer = myZomeBuffer.Replace("myClass", parts[10].ToCamelCase());
                            myZomeBuffer = myZomeBuffer.Replace("my_class", parts[10].ToSnakeCase());

                            className = className.ToSnakeCase();
                            classHolochainDataObjectReached = true;
                        }
                    }

                    reader.Close();
                    nextLineToWrite = 0;

                    File.WriteAllText(string.Concat(settings.GeneratedRustFilesFolder, "\\lib.rs"), libBuffer);
                    //  File.WriteAllText(string.Concat(settings.GeneratedRustFilesFolder, "\\", className.ToSnakeCase(), ".rs"), libBuffer);

                    classList = classList.Substring(0, classList.Length - 2);
                    myZomeBuffer = myZomeBuffer.Replace("class_list", classList);
                    File.WriteAllText(string.Concat(settings.GeneratedCSharpFilesFolder, "\\", zomeName, ".cs"), myZomeBuffer);
                    File.WriteAllText(string.Concat(settings.GeneratedCSharpFilesFolder, "\\", zomeName, "EventArgs.cs"), myZomeEventArgsBuffer);
                    //File.WriteAllText(string.Concat(settings.GeneratedCSharpFilesFolder, "\\", className, ".cs"), myClassBuffer);
                    //File.WriteAllText(string.Concat(settings.GeneratedCSharpFilesFolder, "\\I", className, ".cs"), iMyClassBuffer);
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
            //TODO: Make the paths relative!
            public string RustTemplateFolder = @"C:\Users\david\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.Holochain.HoloNET.HDK.Core\RustTemplates";
            public string CSharpTemplateFolder = @"C:\Users\david\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.Holochain.HoloNET.HDK.Core\CSharpTemplates";
            public string ProxyClassesToGenerateFromFolder = @"C:\Users\david\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.Holochain.HoloNET.HDK.CLI\ProxyClasses";
            public string GeneratedRustFilesFolder = @"C:\Users\david\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.Holochain.HoloNET.HDK.CLI\GeneratedCode\Rust";
            public string GeneratedCSharpFilesFolder = @"C:\Users\david\source\repos\Our-World-OASIS-API-HoloNET-HoloUnity-And-.NET-HDK\NextGenSoftware.Holochain.HoloNET.HDK.CLI\GeneratedCode\CSharp";
            public string RustTemplateLib = "lib.rs";
            public string RustTemplateCreate = "create.rs";
            public string RustTemplateRead = "read.rs";
            public string RustTemplateUpdate = "update.rs";
            public string RustTemplateDelete = "delete.rs";
            public string RustTemplateList = "list.rs";
            public string RustTemplateInt = "int.rs";
            public string RustTemplateString = "string.rs";
            public string RustTemplateBool = "bool.rs";
            public string RustTemplateStruct = "struct.rs";
            public string RustTemplateValidation = "validation.rs";
            public string CSharpTemplateMyZomeEventArgs = "MyZomeEventArgs.cs";
            public string CSharpTemplateIMyClass = "IMyClass.cs";
            public string CSharpTemplateMyClass = "MyClass.cs";
            public string CSharpTemplateMyZome = "MyZome.cs";
        }
    }
}
