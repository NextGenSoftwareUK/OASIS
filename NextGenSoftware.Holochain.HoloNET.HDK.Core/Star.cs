using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace NextGenSoftware.Holochain.HoloNET.HDK.Core
{
    public static class Star
    {
        public static IPlanet Genesis(string planetName, string dnaFolder = "", string genesisCSharpFolder = "", string genesisRustFolder = "", string genesisNameSpace = "")
        {
            //TODO: Make this dynamic by reading from top of template files!
            const string TEMPLATE_NAMESPACE = "NextGenSoftware.Holochain.HoloNET.HDK.Core.CSharpTemplates";
            StarDNA starDNA;
            //bool holonLineReached = false;
            bool holonReached = false;
            //  bool holonHolochainZometReached = false;
            string holonBufferRust = "";
            string holonBufferCsharp = "";
            string libBuffer = "";
            string holonName = "";
            string zomeName = "";
            string holonFieldsClone = "";
            int nextLineToWrite = 0;
            bool firstField = true;
         //   string myZomeEventArgsBuffer = "";
            string iholonBuffer = "";
            string zomeBufferCsharp = "";
           // string holonList = "";
            bool firstHolon = true;

            //PlanetBase newPlanet = new PlanetBase() { Name = planetName };

            //  string loadholonMethodBuffer = "";
            //   int loadholonMethodBufferReadLine = 0;

            if (File.Exists("starDNA.json"))
                starDNA = LoadDNA();
            else
            {
                starDNA = new StarDNA();
                SaveDNA(starDNA);
            }

            ValidateDNA(starDNA, dnaFolder, genesisCSharpFolder, genesisRustFolder);

            string libTemplate = new FileInfo(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateLib)).OpenText().ReadToEnd();
            string createTemplate = new FileInfo(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateCreate)).OpenText().ReadToEnd();
            string readTemplate = new FileInfo(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateRead)).OpenText().ReadToEnd();
            string updateTemplate = new FileInfo(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateUpdate)).OpenText().ReadToEnd();
            string deleteTemplate = new FileInfo(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateDelete)).OpenText().ReadToEnd();
            string listTemplate = new FileInfo(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateList)).OpenText().ReadToEnd();
            string validationTemplate = new FileInfo(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateValidation)).OpenText().ReadToEnd();
            string holonTemplateRust = new FileInfo(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateHolon)).OpenText().ReadToEnd();
            string intTemplate = new FileInfo(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateInt)).OpenText().ReadToEnd();
            string stringTemplate = new FileInfo(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateString)).OpenText().ReadToEnd();
            string boolTemplate = new FileInfo(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateBool)).OpenText().ReadToEnd();
           // string myZomeEventArgsTemplate = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateMyZomeEventArgs)).OpenText().ReadToEnd();
            string iHolonTemplate = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateIHolonDNA)).OpenText().ReadToEnd();
            string holonTemplateCsharp = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateHolonDNA)).OpenText().ReadToEnd();
            string zomeTemplateCsharp = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateZomeDNA)).OpenText().ReadToEnd();
            string iZomeTemplate = new FileInfo(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateIZomeDNA)).OpenText().ReadToEnd();

            //If folder is not passed in via command line args then use default in config file.
            if (string.IsNullOrEmpty(dnaFolder))
                dnaFolder = starDNA.DNAFolder;

            if (string.IsNullOrEmpty(genesisCSharpFolder))
                genesisCSharpFolder = starDNA.GenesisCSharpFolder;

            if (string.IsNullOrEmpty(genesisRustFolder))
                genesisRustFolder = starDNA.GenesisRustFolder;

            if (string.IsNullOrEmpty(genesisNameSpace))
                genesisNameSpace = starDNA.GenesisNameSpace;
            

            DirectoryInfo dirInfo = new DirectoryInfo(dnaFolder);
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

                            //If the new namespace name has not been passed in then default it to the proxy holon namespace.
                            if (string.IsNullOrEmpty(genesisNameSpace))
                                genesisNameSpace = parts[1];

                            zomeBufferCsharp = zomeTemplateCsharp.Replace(TEMPLATE_NAMESPACE, genesisNameSpace);
                            holonBufferCsharp = holonTemplateCsharp.Replace(TEMPLATE_NAMESPACE, genesisNameSpace);
                        }

                        if (buffer.Contains("ZomeDNA"))
                        //if (buffer.Contains("HolochainBaseProxyZome"))
                        {
                            string[] parts = buffer.Split(' ');

                         //   myZomeEventArgsBuffer = myZomeEventArgsTemplate.Replace("MyZome", parts[6]);
                            libBuffer = libTemplate.Replace("zome_name", parts[6].ToSnakeCase());

                            //zomeBufferCsharp = zomeBufferCsharp.Replace("MyZome", parts[6].ToPascalCase());
                            //zomeBufferCsharp = zomeBufferCsharp.Replace("MYZOME", parts[6].ToUpper());
                            zomeBufferCsharp = zomeBufferCsharp.Replace("ZomeDNATemplate", parts[6].ToPascalCase());
                            zomeBufferCsharp = zomeBufferCsharp.Replace("{zome}", parts[6].ToSnakeCase());
                            zomeName = parts[6].ToPascalCase();
                        }

                        if (holonReached && buffer.Contains("string") || buffer.Contains("int") || buffer.Contains("bool"))
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
                                            holonFieldsClone = string.Concat(holonFieldsClone, "\t");

                                        fieldName = parts[14].ToSnakeCase();
                                        holonFieldsClone = string.Concat(holonFieldsClone, holonName, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);

                                        //int index = holonBuffer.IndexOf("//#CopyFields//");
                                        holonBufferRust = string.Concat(holonBufferRust, stringTemplate.Replace("variableName", fieldName), ",", Environment.NewLine);

                                       // holonBuffer = holonBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, createTemplate.Replace("MyEntry", holonName.ToPascalCase()).Replace("my_entry", holonName), Environment.NewLine));
                                    }
                                    break;

                                case "int":
                                    {
                                        if (firstField)
                                            firstField = false;
                                        else
                                            holonFieldsClone = string.Concat(holonFieldsClone, "\t");

                                        fieldName = parts[14].ToSnakeCase();
                                        holonFieldsClone = string.Concat(holonFieldsClone, holonName, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);
                                        holonBufferRust = string.Concat(holonBufferRust, intTemplate.Replace("variableName", fieldName), ",", Environment.NewLine);
                                    }
                                    break;

                                case "bool":
                                    {
                                        if (firstField)
                                            firstField = false;
                                        else
                                            holonFieldsClone = string.Concat(holonFieldsClone, "\t");

                                        fieldName = parts[14].ToSnakeCase();
                                        holonFieldsClone = string.Concat(holonFieldsClone, holonName, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);
                                        holonBufferRust = string.Concat(holonBufferRust, boolTemplate.Replace("variableName", fieldName), ",", Environment.NewLine);
                                    }
                                    break;
                            }
                        }

                        // Write the holon out to the rust lib template. 
                        if (holonReached && buffer.Length > 1 && buffer.Substring(buffer.Length-1 ,1) == "}" && !buffer.Contains("get;"))
                        {
                            if (holonBufferRust.Length >2)
                                holonBufferRust = holonBufferRust.Remove(holonBufferRust.Length - 3);

                            holonBufferRust = string.Concat(Environment.NewLine, holonBufferRust, Environment.NewLine, holonTemplateRust.Substring(holonTemplateRust.Length - 1, 1), Environment.NewLine);

                            //int zomeNameIndex = libTemplate.IndexOf("zome_name");
                            int zomeIndex = libTemplate.IndexOf("#[zome]");
                            int zomeBodyStartIndex = libTemplate.IndexOf("{", zomeIndex);
                            
                            libBuffer = libBuffer.Insert(zomeIndex - 2, holonBufferRust);
                            // libBuffer = libBuffer.Insert(zomeBodyStartIndex + 2, holonBuffer);

                            if (nextLineToWrite == 0)
                                nextLineToWrite = zomeBodyStartIndex + holonBufferRust.Length;
                            else
                                nextLineToWrite += holonBufferRust.Length;

                            //Now insert the CRUD methods for each holon.
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, createTemplate.Replace("Holon", holonName.ToPascalCase()).Replace("{holon}", holonName), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, readTemplate.Replace("Holon", holonName.ToPascalCase()).Replace("{holon}", holonName), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, updateTemplate.Replace("Holon", holonName.ToPascalCase()).Replace("{holon}", holonName).Replace("//#CopyFields//", holonFieldsClone), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, deleteTemplate.Replace("Holon", holonName.ToPascalCase()).Replace("{holon}", holonName), Environment.NewLine));
                            libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, validationTemplate.Replace("Holon", holonName.ToPascalCase()).Replace("{holon}", holonName), Environment.NewLine));
                            //libBuffer = libBuffer.Insert(nextLineToWrite + 2, string.Concat(Environment.NewLine, listTemplate, Environment.NewLine));

                            if (!firstHolon)
                            {
                                //TODO: Need to make dynamic so no need to pass length in (had issues before but will try again later... :) )
                                zomeBufferCsharp = GenerateDynamicZomeFunc("Load", zomeTemplateCsharp, holonName, zomeBufferCsharp, 170);
                                zomeBufferCsharp = GenerateDynamicZomeFunc("Save", zomeTemplateCsharp, holonName, zomeBufferCsharp, 147);
                            }

                            holonName = holonName.ToPascalCase();

                            File.WriteAllText(string.Concat(genesisCSharpFolder, "\\I", holonName, ".cs"), iholonBuffer);
                            File.WriteAllText(string.Concat(genesisCSharpFolder, "\\", holonName, ".cs"), holonBufferCsharp);

                            //TDOD: Finish putting in IZomeBuffer etc
                            //   File.WriteAllText(string.Concat(genesisCSharpFolder, "\\I", holonName, ".cs"), izomeBuffer);
                           // File.WriteAllText(string.Concat(genesisCSharpFolder, "\\", zomeName, ".cs"), zomeBufferCsharp);
   
                            holonBufferRust = "";
                            holonBufferCsharp = "";
                            holonFieldsClone = "";
                            holonReached = false;
                            firstField = true;
                            firstHolon = false;
                            holonName = "";
                        }

                        if (buffer.Contains("HolonDNA"))
                        {
                            string[] parts = buffer.Split(' ');
                            holonName = parts[10].ToPascalCase();
                           // holonList = string.Concat(holonList, holonName, ", ");

                            holonBufferRust = holonTemplateRust.Replace("Holon", holonName).Replace("{holon}", holonName.ToSnakeCase());
                            holonBufferRust = holonBufferRust.Substring(0, holonBufferRust.Length - 1);

                            //Process the CSharp Templates.
                            if (string.IsNullOrEmpty(holonBufferCsharp))
                                holonBufferCsharp = holonTemplateCsharp;

                            holonBufferCsharp = holonBufferCsharp.Replace("HolonDNATemplate", parts[10]);
                            iholonBuffer = iHolonTemplate.Replace("IHolonDNATemplate", string.Concat("I", parts[10]));

                            //zomeBufferCsharp = zomeBufferCsharp.Replace("MYHOLON", parts[10].ToUpper());
                            //zomeBufferCsharp = zomeBufferCsharp.Replace("myHolon", parts[10].ToCamelCase());
                            zomeBufferCsharp = zomeBufferCsharp.Replace("HOLON", parts[10].ToPascalCase());
                            zomeBufferCsharp = zomeBufferCsharp.Replace("{holon}", parts[10].ToSnakeCase());

                            zomeBufferCsharp = zomeBufferCsharp.Replace(TEMPLATE_NAMESPACE, genesisNameSpace);
                            holonBufferCsharp = holonBufferCsharp.Replace(TEMPLATE_NAMESPACE, genesisNameSpace);
                            iholonBuffer = iholonBuffer.Replace(TEMPLATE_NAMESPACE, genesisNameSpace);
                            //izomeBufferCsharp = izomeBufferCsharp.Replace(TEMPLATE_NAMESPACE, genesisNameSpace);

                            holonName = holonName.ToSnakeCase();
                            holonReached = true;
                        }

                        //if (buffer.Contains("LoadMyholonAsync"))
                        //{
                        //    loadholonMethodBuffer = string.Concat(loadholonMethodBuffer, buffer.Replace();
                        //    loadholonMethodBufferReadLine++;
                            
                        //    if (loadholonMethodBufferReadLine == 3)
                        //        zomeBufferCsharp.Insert(zomeBufferCsharp.Length - 3, loadholonMethodBuffer);
                        //}
                    }

                    reader.Close();
                    nextLineToWrite = 0;

                    File.WriteAllText(string.Concat(genesisRustFolder, "\\lib.rs"), libBuffer);

                    //holonList = holonList.Substring(0, holonList.Length - 2);
                    //zomeBufferCsharp = zomeBufferCsharp.Replace("holon_list", holonList);
                    File.WriteAllText(string.Concat(genesisCSharpFolder, "\\", zomeName, ".cs"), zomeBufferCsharp);
                }
            }

            //PlanetBase newPlanet = new PlanetBase()
            //newPlanet.Id = Guid.NewGuid();


           // return newPlanet;
            return null;
        }


        private static string GenerateDynamicZomeFunc(string funcName, string zomeTemplateCsharp, string holonName, string zomeBufferCsharp, int funcLength)
        {
            int funcHolonIndex = zomeTemplateCsharp.IndexOf(funcName);
            string funct = zomeTemplateCsharp.Substring(funcHolonIndex - 26, funcLength); //170
            funct = funct.Replace("{holon}", holonName.ToSnakeCase()).Replace("HOLON", holonName.ToPascalCase());
            zomeBufferCsharp = zomeBufferCsharp.Insert(zomeBufferCsharp.Length - 6, funct);
            return zomeBufferCsharp;
        }

        // Build
        public static void Light(string planetName)
        {

        }

        public static void Light(PlanetBase planet)
        {

        }

        //Activate & Launch - Launch & activate a planet (OAPP) by shining the star's light upon it...
        public static void Shine(PlanetBase planet)
        {

        }

        public static void Shine(string planetName)
        {

        }

        //Dractivate
        public static void Dim(PlanetBase planet)
        {

        }

        public static void Dim(string planetName)
        {

        }

        //Deploy
        public static void Seed(PlanetBase planet)
        {

        }

        public static void Seed(string planetName)
        {

        }

        // Run Tests
        public static void Twinkle(PlanetBase planet)
        {

        }

        public static void Twinkle(string planetName)
        {

        }

        // Delete Planet (OAPP)
        public static void Dust(PlanetBase planet)
        {

        }

        public static void Dust(string planetName)
        {

        }

        // Delete Planet (OAPP)
        public static void Evolve(PlanetBase planet)
        {

        }

        public static void Evolve(string planetName)
        {

        }

        // Delete Planet (OAPP)
        public static void Mutate(PlanetBase planet)
        {

        }

        public static void Mutate(string planetName)
        {

        }

        // Highlight the Planet (OAPP) in the OAPP Store (StarNET)
        public static void Radiate(PlanetBase planet)
        {

        }

        public static void Radiate(string planetName)
        {

        }

        // Show how much light the planet (OAPP) is emitting into the solar system (StarNET/HoloNET)
        public static void Emit(PlanetBase planet)
        {

        }

        public static void Emit(string planetName)
        {

        }

        // Show stats of the Planet (OAPP)
        public static void Reflect(PlanetBase planet)
        {

        }

        public static void Reflect(string planetName)
        {

        }

        // Send/Receive Love
        public static void Love(PlanetBase planet)
        {

        }

        public static void Love(string planetName)
        {

        }

        // Reserved For Future Use...
        public static void Super(PlanetBase planet)
        {

        }

        public static void Super(string planetName)
        {

        }

        private static void ValidateDNA(StarDNA starDNA, string dnaFolder, string genesisCSharpFolder, string genesisRustFolder)
        {
            if (!string.IsNullOrEmpty(dnaFolder) && !Directory.Exists(dnaFolder))
                throw new ArgumentOutOfRangeException("dnaFolder", dnaFolder, "The folder is not valid, please double check and try again.");

            if (!string.IsNullOrEmpty(genesisCSharpFolder) && !Directory.Exists(genesisCSharpFolder))
                throw new ArgumentOutOfRangeException("genesisCSharpFolder", genesisCSharpFolder, "The folder is not valid, please double check and try again.");

            if (!string.IsNullOrEmpty(genesisRustFolder) && !Directory.Exists(genesisRustFolder))
                throw new ArgumentOutOfRangeException("genesisRustFolder", genesisRustFolder, "The folder is not valid, please double check and try again.");

            if (starDNA != null)
            {
                if (!Directory.Exists(starDNA.DNAFolder))
                    throw new ArgumentOutOfRangeException("DNAFolder", starDNA.DNAFolder, "The DNAFolder is not valid, please double check and try again.");

                if (!Directory.Exists(starDNA.GenesisCSharpFolder))
                    throw new ArgumentOutOfRangeException("GenesisCSharpFolder", starDNA.GenesisCSharpFolder, "The GenesisCSharpFolder is not valid, please double check and try again.");

                if (!Directory.Exists(starDNA.GenesisRustFolder))
                    throw new ArgumentOutOfRangeException("GenesisRustFolder", starDNA.GenesisCSharpFolder, "The GenesisRustFolder is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateCreate)))
                    throw new ArgumentOutOfRangeException("RustTemplateCreate", string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateCreate), "The RustTemplateCreate file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateDelete)))
                    throw new ArgumentOutOfRangeException("RustTemplateDelete", string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateDelete), "The RustTemplateDelete file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateLib)))
                    throw new ArgumentOutOfRangeException("RustTemplateLib", string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateLib), "The RustTemplateLib file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateRead)))
                    throw new ArgumentOutOfRangeException("RustTemplateRead", string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateRead), "The RustTemplateRead file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateUpdate)))
                    throw new ArgumentOutOfRangeException("RustTemplateUpdate", string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateUpdate), "The RustTemplateUpdate file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateList)))
                    throw new ArgumentOutOfRangeException("RustTemplateList", string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateList), "The RustTemplateList file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(starDNA.RustDNATemplateFolder, "\\", starDNA.RustTemplateValidation)))
                    throw new ArgumentOutOfRangeException("RustTemplateValidation", string.Concat(starDNA.RustTemplateValidation, "\\", starDNA.RustTemplateList), "The RustTemplateValidation file is not valid, please double check and try again.");

                //if (!File.Exists(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateMyZomeEventArgs)))
                //    throw new ArgumentOutOfRangeException("CSharpTemplateEventArgs", string.Concat(starDNA.CSharpTemplateMyZomeEventArgs, "\\", starDNA.CSharpTemplateMyZomeEventArgs), "The CSharpTemplateEventArgs file is not valid, please double check and try again.");

                //if (!File.Exists(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateIMyholon)))
                //    throw new ArgumentOutOfRangeException("CSharpTemplateIMyholon", string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateIMyholon), "The CSharpTemplateIMyholon file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateHolonDNA)))
                    throw new ArgumentOutOfRangeException("CSharpTemplateHolonDNA", string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateHolonDNA), "The CSharpTemplateMyholon file is not valid, please double check and try again.");

                if (!File.Exists(string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateZomeDNA)))
                    throw new ArgumentOutOfRangeException("CSharpTemplateZomeDNA", string.Concat(starDNA.CSharpDNATemplateFolder, "\\", starDNA.CSharpTemplateZomeDNA), "The CSharpTemplateMyZome file is not valid, please double check and try again.");
            }
        }

        //private void ProcessField(string fieldNameRaw, out string holonFieldsClone, out string holonBuffer, string template, string holonName)
        //{
        //    string fieldName = template.Replace("variableName", fieldNameRaw.ToSnakeCase());
        //    holonFieldsClone = string.Concat(holonFieldsClone, holonName, ".", fieldName, "=updated_entry.", fieldName, ";", Environment.NewLine);
        //    holonBuffer = string.Concat(holonBuffer, fieldName, ",", Environment.NewLine);
        //}

        private static StarDNA LoadDNA()
        {
            using (StreamReader r = new StreamReader("starDNA.json"))
            {
                string json = r.ReadToEnd();
                StarDNA starDNA = JsonConvert.DeserializeObject<StarDNA> (json);
                return starDNA;
            }
        }

        private static bool SaveDNA(StarDNA starDNA)
        {
            string json = JsonConvert.SerializeObject(starDNA);
            StreamWriter writer = new StreamWriter("starDNA.json");
            writer.Write(json);
            writer.Close();
            
            return true;
        }
    }
}
