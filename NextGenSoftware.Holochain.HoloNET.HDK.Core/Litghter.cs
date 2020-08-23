using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public class Litghter
    {
        //TODO: Is Light a better name?
        public void Spark(string proxyClassesToGenerateFromFolder = "", string generatedCSharpCodeFolder = "", string generatedRustCodeFolder = "", string generatedCodeNameSpace = "")
        {
            LighterDNA lighterDNA = null;
            //bool classLineReached = false;
            bool classHolochainDataObjectReached = false;
            bool classHolochainZometReached = false;
            string classBuffer = "";
            string libBuffer = "";
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
            string loadClassMethodBuffer = "";
            int loadClassMethodBufferReadLine = 0;

            if (File.Exists("LighterDNA.json"))
                lighterDNA = LoadConfig();
            else
            {
                lighterDNA = new LighterDNA();
                SaveConfig(lighterDNA);
            }

            ValidateDNA(lighterDNA, proxyClassesToGenerateFromFolder, generatedCSharpCodeFolder, generatedRustCodeFolder);

            string libTemplate = new FileInfo(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateLib)).OpenText().ReadToEnd();
            string createTemplate = new FileInfo(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateCreate)).OpenText().ReadToEnd();
            string readTemplate = new FileInfo(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateRead)).OpenText().ReadToEnd();
            string updateTemplate = new FileInfo(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateUpdate)).OpenText().ReadToEnd();
            string deleteTemplate = new FileInfo(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateDelete)).OpenText().ReadToEnd();
            string listTemplate = new FileInfo(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateList)).OpenText().ReadToEnd();
            string validationTemplate = new FileInfo(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateValidation)).OpenText().ReadToEnd();
            string holonTemplate = new FileInfo(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateHolon)).OpenText().ReadToEnd();
            string intTemplate = new FileInfo(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateInt)).OpenText().ReadToEnd();
            string stringTemplate = new FileInfo(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateString)).OpenText().ReadToEnd();
            string boolTemplate = new FileInfo(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateBool)).OpenText().ReadToEnd();
           // string myZomeEventArgsTemplate = new FileInfo(string.Concat(lighterDNA.CSharpTemplateFolder, "\\", lighterDNA.CSharpTemplateMyZomeEventArgs)).OpenText().ReadToEnd();
           // string iMyClassTemplate = new FileInfo(string.Concat(lighterDNA.CSharpTemplateFolder, "\\", lighterDNA.CSharpTemplateIMyClass)).OpenText().ReadToEnd();
            string myClassTemplate = new FileInfo(string.Concat(lighterDNA.CSharpTemplateFolder, "\\", lighterDNA.CSharpTemplateHolonDNA)).OpenText().ReadToEnd();
            string myZomeTemplate = new FileInfo(string.Concat(lighterDNA.CSharpTemplateFolder, "\\", lighterDNA.CSharpTemplateZomeDNA)).OpenText().ReadToEnd();

            //If folder is not passed in via command line args then use default in config file.
            if (string.IsNullOrEmpty(proxyClassesToGenerateFromFolder))
                proxyClassesToGenerateFromFolder = lighterDNA.ProxyClassesToGenerateFromFolder;

            if (string.IsNullOrEmpty(generatedCSharpCodeFolder))
                generatedCSharpCodeFolder = lighterDNA.GeneratedCSharpFilesFolder;

            if (string.IsNullOrEmpty(generatedRustCodeFolder))
                generatedRustCodeFolder = lighterDNA.GeneratedRustFilesFolder;
            

            DirectoryInfo dirInfo = new DirectoryInfo(proxyClassesToGenerateFromFolder);
            FileInfo[] files = dirInfo.GetFiles();

            foreach (FileInfo file in files)
            {
                if (file != null)
                {
                    StreamReader reader = file.OpenText();

                    while (!reader.EndOfStream)
                    {
                        string buffer = reader.ReadLine();

                        if (buffer.Contains("namespace"))
                        {
                            string[] parts = buffer.Split(' ');

                            //If the new namespace name has not been passed in then default it to the proxy class namespace.
                            if (string.IsNullOrEmpty(generatedCodeNameSpace))
                                generatedCodeNameSpace = parts[1];

                            myZomeBuffer = myZomeTemplate.Replace("NextGenSoftware.Holochain.HoloNET.HDK.Core.CSharpTemplates", generatedCodeNameSpace);
                        }

                        if (buffer.Contains("ZomeBase"))
                        //if (buffer.Contains("HolochainBaseProxyZome"))
                        {
                            string[] parts = buffer.Split(' ');

                         //   myZomeEventArgsBuffer = myZomeEventArgsTemplate.Replace("MyZome", parts[6]);
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

                            classBuffer = string.Concat(Environment.NewLine, classBuffer, Environment.NewLine, holonTemplate.Substring(holonTemplate.Length - 1, 1), Environment.NewLine);

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
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, createTemplate.Replace("MyHolon", className.ToPascalCase()).Replace("my_holon", className), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, readTemplate.Replace("MyHolon", className.ToPascalCase()).Replace("my_holon", className), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, updateTemplate.Replace("MyHolon", className.ToPascalCase()).Replace("my_holon", className).Replace("//#CopyFields//", classFieldsClone), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, deleteTemplate.Replace("MyHolon", className.ToPascalCase()).Replace("my_holon", className), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, validationTemplate.Replace("MyHolon", className.ToPascalCase()).Replace("my_holon", className), Environment.NewLine));
                            //libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, listTemplate, Environment.NewLine));

                            //int loadMyClassIndex = myZomeBuffer.IndexOf("LoadMyClassAsync");
                            //int loadMyClassIndex = myZomeBuffer.IndexOf("Load");
                            //char[] data = new char[1];

                            //StringReader myZomeReader = new StringReader(myZomeBuffer);
                            //myZomeReader.Read(data, loadMyClassIndex, 1);

                            //string data2 = myZomeReader.ReadLine();
                            //data2 = string.Concat(data2, myZomeReader.ReadLine());
                            //data2 = string.Concat(data2, myZomeReader.ReadLine());
                            //myZomeBuffer.Insert(myZomeBuffer.Length - 3, data2);

                            //if (myZomeBuffer.Contains("LoadMyClassAsync"))
                            //{
                            //    loadClassMethodBuffer = string.Concat(loadClassMethodBuffer, buffer.Replace();
                            //    loadClassMethodBufferReadLine++;

                            //    if (loadClassMethodBufferReadLine == 3)
                            //        myZomeBuffer.Insert(myZomeBuffer.Length - 3, loadClassMethodBuffer);
                            //}

                            classBuffer = "";
                            classFieldsClone = "";
                            classHolochainDataObjectReached = false;
                            classHolochainZometReached = false;
                            firstField = true;
                            className = className.ToPascalCase();

                            File.WriteAllText(string.Concat(generatedCSharpCodeFolder, "\\", className, ".cs"), myClassBuffer);
                            File.WriteAllText(string.Concat(generatedCSharpCodeFolder, "\\I", className, ".cs"), iMyClassBuffer);
                            File.WriteAllText(string.Concat(generatedCSharpCodeFolder, "\\", zomeName, ".cs"), myZomeBuffer);

                            className = "";
                        }

                        if (buffer.Contains("HolonBase"))
                        {
                            string[] parts = buffer.Split(' ');
                            className = parts[10].ToPascalCase();
                            classList = string.Concat(classList, className, ", ");

                            classBuffer = holonTemplate.Replace("MyHolon", className).Replace("my_holon", className.ToSnakeCase());
                            classBuffer = classBuffer.Substring(0, classBuffer.Length - 1);

                            //Process the CSharp Templates.
                            myZomeEventArgsBuffer = myZomeEventArgsBuffer.Replace("ZomeDNA", parts[10]);
                            myClassBuffer = myClassTemplate.Replace("ZomeDNA", parts[10]);
                           // iMyClassBuffer = iMyClassTemplate.Replace("ZomeDNA", parts[10]);

                            myZomeBuffer = myZomeBuffer.Replace("MyHolon", parts[10].ToPascalCase());
                            myZomeBuffer = myZomeBuffer.Replace("MYHOLON", parts[10].ToUpper());
                            myZomeBuffer = myZomeBuffer.Replace("myHolon", parts[10].ToCamelCase());
                            myZomeBuffer = myZomeBuffer.Replace("my_holon", parts[10].ToSnakeCase());

                            className = className.ToSnakeCase();
                            classHolochainDataObjectReached = true;
                        }

                        //if (buffer.Contains("LoadMyClassAsync"))
                        //{
                        //    loadClassMethodBuffer = string.Concat(loadClassMethodBuffer, buffer.Replace();
                        //    loadClassMethodBufferReadLine++;
                            
                        //    if (loadClassMethodBufferReadLine == 3)
                        //        myZomeBuffer.Insert(myZomeBuffer.Length - 3, loadClassMethodBuffer);
                        //}
                    }

                    reader.Close();
                    nextLineToWrite = 0;

                    File.WriteAllText(string.Concat(lighterDNA.GeneratedRustFilesFolder, "\\lib.rs"), libBuffer);

                    classList = classList.Substring(0, classList.Length - 2);
                    myZomeBuffer = myZomeBuffer.Replace("holon_list", classList);
                    //File.WriteAllText(string.Concat(generatedCSharpCodeFolder, "\\", zomeName, ".cs"), myZomeBuffer);
                    //File.WriteAllText(string.Concat(generatedRustCodeFolder, "\\", zomeName, "EventArgs.cs"), myZomeEventArgsBuffer);
                }
            }
        }

        private void ValidateDNA(LighterDNA lighterDNA, string proxyClassesToGenerateFromFolder, string generatedCSharpCodeFolder, string generatedRustCodeFolder)
        {
            if (!string.IsNullOrEmpty(proxyClassesToGenerateFromFolder) && !Directory.Exists(proxyClassesToGenerateFromFolder))
                throw new ArgumentOutOfRangeException("proxyClassesToGenerateFromFolder", proxyClassesToGenerateFromFolder, "The folder is not valid, please double check and try again.");

            if (!string.IsNullOrEmpty(generatedCSharpCodeFolder) && !Directory.Exists(generatedCSharpCodeFolder))
                throw new ArgumentOutOfRangeException("generatedCSharpCodeFolder", generatedCSharpCodeFolder, "The folder is not valid, please double check and try again.");

            if (!string.IsNullOrEmpty(generatedRustCodeFolder) && !Directory.Exists(generatedRustCodeFolder))
                throw new ArgumentOutOfRangeException("generatedRustCodeFolder", generatedRustCodeFolder, "The folder is not valid, please double check and try again.");

            if (lighterDNA != null)
            {
                if (!Directory.Exists(lighterDNA.ProxyClassesToGenerateFromFolder))
                    throw new ArgumentOutOfRangeException("ProxyClassesToGenerateFromFolder", lighterDNA.ProxyClassesToGenerateFromFolder, "The ProxyClassesToGenerateFromFolder folder is not valid, please double check and try again.");

                if (!Directory.Exists(lighterDNA.GeneratedCSharpFilesFolder))
                    throw new ArgumentOutOfRangeException("GeneratedCSharpFilesFolder", lighterDNA.GeneratedCSharpFilesFolder, "The GeneratedCSharpFilesFolder folder is not valid, please double check and try again.");

                if (!Directory.Exists(lighterDNA.GeneratedRustFilesFolder))
                    throw new ArgumentOutOfRangeException("GeneratedRustFilesFolder", lighterDNA.GeneratedCSharpFilesFolder, "The GeneratedRustFilesFolder folder is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateCreate)))
                    throw new ArgumentOutOfRangeException("RustTemplateCreate", string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateCreate), "The RustTemplateCreate file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateDelete)))
                    throw new ArgumentOutOfRangeException("RustTemplateDelete", string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateDelete), "The RustTemplateDelete file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateLib)))
                    throw new ArgumentOutOfRangeException("RustTemplateLib", string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateLib), "The RustTemplateLib file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateRead)))
                    throw new ArgumentOutOfRangeException("RustTemplateRead", string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateRead), "The RustTemplateRead file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateUpdate)))
                    throw new ArgumentOutOfRangeException("RustTemplateUpdate", string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateUpdate), "The RustTemplateUpdate file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateList)))
                    throw new ArgumentOutOfRangeException("RustTemplateList", string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateList), "The RustTemplateList file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(lighterDNA.RustTemplateFolder, "\\", lighterDNA.RustTemplateValidation)))
                    throw new ArgumentOutOfRangeException("RustTemplateValidation", string.Concat(lighterDNA.RustTemplateValidation, "\\", lighterDNA.RustTemplateList), "The RustTemplateValidation file is not valid, please double check and try again.");

                //if (!File.Exists(string.Concat(lighterDNA.CSharpTemplateFolder, "\\", lighterDNA.CSharpTemplateMyZomeEventArgs)))
                //    throw new ArgumentOutOfRangeException("CSharpTemplateEventArgs", string.Concat(lighterDNA.CSharpTemplateMyZomeEventArgs, "\\", lighterDNA.CSharpTemplateMyZomeEventArgs), "The CSharpTemplateEventArgs file is not valid, please double check and try again.");

                //if (!File.Exists(string.Concat(lighterDNA.CSharpTemplateFolder, "\\", lighterDNA.CSharpTemplateIMyClass)))
                //    throw new ArgumentOutOfRangeException("CSharpTemplateIMyClass", string.Concat(lighterDNA.CSharpTemplateFolder, "\\", lighterDNA.CSharpTemplateIMyClass), "The CSharpTemplateIMyClass file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(lighterDNA.CSharpTemplateFolder, "\\", lighterDNA.CSharpTemplateHolonDNA)))
                    throw new ArgumentOutOfRangeException("CSharpTemplateHolonDNA", string.Concat(lighterDNA.CSharpTemplateFolder, "\\", lighterDNA.CSharpTemplateHolonDNA), "The CSharpTemplateMyClass file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(lighterDNA.CSharpTemplateFolder, "\\", lighterDNA.CSharpTemplateZomeDNA)))
                    throw new ArgumentOutOfRangeException("CSharpTemplateZomeDNA", string.Concat(lighterDNA.CSharpTemplateFolder, "\\", lighterDNA.CSharpTemplateZomeDNA), "The CSharpTemplateMyZome file is not valid, please double check and try again.");
            }
        }

        //private void ProcessField(string fieldNameRaw, out string classFieldsClone, out string classBuffer, string template, string className)
        //{
        //    string fieldName = template.Replace("variableName", fieldNameRaw.ToSnakeCase());
        //    classFieldsClone = string.Concat(classFieldsClone, className, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);
        //    classBuffer = string.Concat(classBuffer, fieldName, ",", Environment.NewLine);
        //}

        private LighterDNA LoadConfig()
        {
            using (StreamReader r = new StreamReader("LighterDNA.json"))
            {
                string json = r.ReadToEnd();
                LighterDNA lighterDNA = JsonConvert.DeserializeObject<LighterDNA> (json);
                return lighterDNA;
            }
        }

        private bool SaveConfig(LighterDNA lighterDNA)
        {
            string json = JsonConvert.SerializeObject(lighterDNA);
            StreamWriter writer = new StreamWriter("LighterDNA.json");
            writer.Write(json);
            writer.Close();
            
            return true;
        }
    }
}
