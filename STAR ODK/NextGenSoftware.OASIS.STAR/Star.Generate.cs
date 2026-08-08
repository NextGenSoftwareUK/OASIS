using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.Enums;
using NextGenSoftware.OASIS.STAR.Zomes;
using NextGenSoftware.OASIS.STAR.EventArgs;
using NextGenSoftware.OASIS.STAR.ErrorEventArgs;
using NextGenSoftware.OASIS.STAR.CelestialSpace;
using NextGenSoftware.OASIS.STAR.CelestialBodies;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;
using NextGenSoftware.OASIS.STAR.Interfaces;
using SevenZip.Buffer;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace NextGenSoftware.OASIS.STAR
{
    public static partial class STAR
    {
        private static void HandleErrorMessage<T>(ref OASISResult<T> result, string errorMessage)
        {
            OnStarError?.Invoke(null, new StarErrorEventArgs() { Reason = errorMessage });
            OASISErrorHandling.HandleError(ref result, errorMessage);
        }

        private static void CopyFolder(string OAPPNameSpace, DirectoryInfo source, DirectoryInfo target)
        {
            foreach (FileInfo file in source.GetFiles())
            {
                if (!File.Exists(Path.Combine(target.FullName, file.Name)))
                {
                    if (file.Extension == ".csproj")
                        file.CopyTo(Path.Combine(target.FullName, string.Concat(OAPPNameSpace, ".csproj")));
                    else
                        file.CopyTo(Path.Combine(target.FullName, file.Name));
                }
            }

            foreach (DirectoryInfo dir in source.GetDirectories())
            {
                if (dir.Name != "bin" && dir.Name != "obj")
                {
                    if (!Directory.Exists(Path.Combine(target.FullName, dir.Name)))
                        CopyFolder(OAPPNameSpace, dir, target.CreateSubdirectory(dir.Name));
                }
            }
        }

        private static void ApplyOAPPTemplate(GenesisType genesisType, string OAPPFolder, string oAppNameSpace, string oAppName, string celestialBodyName, string zomeName, string holonName, string firstStringProperty, List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, bool root = true)
        {
            // Generate library references and stubs if GeneratedProxies folder exists
            string generatedProxiesPath = Path.Combine(OAPPFolder, "GeneratedProxies");
            string libraryUsingStatements = "";
            string libraryMethodStubs = "";
            
            if (Directory.Exists(generatedProxiesPath))
            {
                var proxyFiles = Directory.GetFiles(generatedProxiesPath, "*Proxy.cs", SearchOption.TopDirectoryOnly);
                
                if (proxyFiles.Length > 0)
                {
                    // Generate using statement
                    libraryUsingStatements = "using GeneratedProxies;\nusing NextGenSoftware.OASIS.API.ONODE.Core.Managers.Interop;\n";
                    
                    // Generate method stubs for each library
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("\n        // Library Integration Methods");
                    sb.AppendLine("        // These methods demonstrate how to use the imported libraries");
                    
                    foreach (var proxyFile in proxyFiles)
                    {
                        string proxyClassName = Path.GetFileNameWithoutExtension(proxyFile);
                        string libraryName = proxyClassName.Replace("Proxy", "");
                        
                        // Example 1: Simple usage (default constructor - just works!)
                        sb.AppendLine($"        // Example 1: Simple usage - proxy handles everything automatically");
                        sb.AppendLine($"        private static async Task Use{libraryName}LibrarySimple()");
                        sb.AppendLine("        {");
                        sb.AppendLine($"            // Create proxy instance - it automatically loads the library on first use");
                        sb.AppendLine($"            var proxy = new {proxyClassName}();");
                        sb.AppendLine($"            ");
                        sb.AppendLine($"            // Call library methods - library is loaded automatically if needed");
                        sb.AppendLine($"            // var result = await proxy.SomeMethodAsync(param1, param2);");
                        sb.AppendLine($"            // if (!result.IsError && result.Result != null)");
                        sb.AppendLine($"            //     Console.WriteLine($\"Result: {{result.Result}}\");");
                        sb.AppendLine("        }");
                        sb.AppendLine("");
                        
                        // Example 2: Advanced usage (only needed for testing or custom setup)
                        sb.AppendLine($"        // Example 2: Advanced usage - only needed for unit testing or custom provider setup");
                        sb.AppendLine($"        // Note: The default constructor is usually sufficient. Only use this for:");
                        sb.AppendLine($"        // - Unit testing with a mock interop manager");
                        sb.AppendLine($"        // - Custom provider configuration");
                        sb.AppendLine($"        // - Reusing a library that was already loaded elsewhere");
                        sb.AppendLine($"        private static async Task Use{libraryName}LibraryAdvanced()");
                        sb.AppendLine("        {");
                        sb.AppendLine($"            // Get shared interop manager (same instance used by all proxies)");
                        sb.AppendLine($"            var interopManager = await LibraryInteropFactory.CreateDefaultManagerAsync();");
                        sb.AppendLine($"            if (interopManager.IsError || interopManager.Result == null)");
                        sb.AppendLine($"            {{");
                        sb.AppendLine($"                Console.WriteLine($\"Error: {{interopManager.Message}}\");");
                        sb.AppendLine($"                return;");
                        sb.AppendLine($"            }}");
                        sb.AppendLine($"            ");
                        sb.AppendLine($"            // If library was already loaded, you can reuse it");
                        sb.AppendLine($"            // var loadResult = await interopManager.Result.LoadLibraryAsync(\"path/to/{libraryName}.dll\");");
                        sb.AppendLine($"            // if (!loadResult.IsError && loadResult.Result != null)");
                        sb.AppendLine($"            // {{");
                        sb.AppendLine($"            //     // Create proxy with pre-loaded library");
                        sb.AppendLine($"            //     var proxy = new {proxyClassName}(interopManager.Result, loadResult.Result.LibraryId);");
                        sb.AppendLine($"            //     // Use proxy methods");
                        sb.AppendLine($"            // }}");
                        sb.AppendLine("        }");
                        sb.AppendLine("");
                    }
                    
                    libraryMethodStubs = sb.ToString();
                }
            }
            
            foreach (DirectoryInfo dir in new DirectoryInfo(OAPPFolder).GetDirectories())
            {
                if (dir.Name != "bin" && dir.Name != "obj")
                    ApplyOAPPTemplate(genesisType, dir.FullName, oAppNameSpace, oAppName, celestialBodyName, zomeName, holonName, firstStringProperty, metaHolonTagMappings, metaTagMappings, false);
            }
            
            if (!OAPPFolder.Contains(STARDNA.OAPPGeneratedCodeFolder))
            {                
                foreach (FileInfo file in new DirectoryInfo(OAPPFolder).GetFiles("*.csproj"))
                {
                    int lineNumber = 1;
                    string line = null;

                    using (TextReader tr = File.OpenText(file.FullName))
                    using (TextWriter tw = File.CreateText(string.Concat(file.FullName, ".temp")))
                    {
                        while ((line = tr.ReadLine()) != null)
                        {
                            line = line.Replace("<Compile Remove=\"Program.cs\" />", "");
                           
                            tw.WriteLine(line);
                            lineNumber++;
                        }
                    }



                    File.Delete(file.FullName);
                    File.Move(string.Concat(file.FullName, ".temp"), file.FullName);
                }

                //TODO: use multiple file extention wildcards so only need one file loop...
                bool foundOASISDNA = false;
                foreach (FileInfo file in new DirectoryInfo(OAPPFolder).GetFiles("*.cs"))
                {
                    int lineNumber = 1;
                    string line = null;

                    if (file.FullName.Contains("OASIS_DNA.json"))
                        foundOASISDNA = true;

                    using (TextReader tr = File.OpenText(file.FullName))
                    using (TextWriter tw = File.CreateText(string.Concat(file.FullName, ".temp")))
                    {
                        bool celestialBodyBlock = false;

                        while ((line = tr.ReadLine()) != null)
                        {
                            if (metaHolonTagMappings != null && metaHolonTagMappings.Count > 0)
                            {
                                string initHolons = "";
                                foreach (MetaHolonTag metaHolonTag in metaHolonTagMappings)
                                {
                                    if (!string.IsNullOrEmpty(initHolons))
                                        initHolons = string.Concat(initHolons, "\n");

                                    initHolons = string.Concat(initHolons, metaHolonTag.HolonName.ToPascalCase(), " ", metaHolonTag.HolonName.ToCamelCase(), " = new ", metaHolonTag.HolonName.ToPascalCase(), "();");

                                    if (line.Contains(metaHolonTag.MetaTag))
                                        line = line.Replace(string.Concat("{{", metaHolonTag.MetaTag, "}}"), string.Concat(metaHolonTag.HolonName.ToPascalCase(), ".Instance.", metaHolonTag.NodeName));
                                        //line = line.Replace(string.Concat("{{", metaHolonTag.MetaTag, "}}"), string.Concat(metaHolonTag.HolonName.ToCamelCase(), ".", metaHolonTag.NodeName));
                                }

                                if (!string.IsNullOrEmpty(initHolons) && line.Contains("{INITCUSTOMTAGHOLONS}"))
                                    line = line.Replace("{INITCUSTOMTAGHOLONS}", initHolons);
                            }

                            if (metaTagMappings != null && metaTagMappings.Count > 0)
                            {
                                string initHolons = "";
                                foreach (string key in metaTagMappings.Keys)
                                {
                                    if (line.Contains(key))
                                        line = line.Replace(string.Concat("[[", key, "]]"), metaTagMappings[key]);
                                }
                            }

                            celestialBodyName = celestialBodyName.Replace(" ", "");
                            line = line.Replace("{OAPPNAMESPACE}", oAppNameSpace);
                            line = line.Replace("{OAPPNAME}", oAppName);

                            if (line.Contains("CelestialBodyOnly:BEGIN"))
                            {
                                celestialBodyBlock = true;
                                continue;

                            }
                            else if (line.Contains("CelestialBodyOnly:END"))
                            {
                                celestialBodyBlock = false;
                                continue;
                            }
                            else if (celestialBodyBlock && genesisType == GenesisType.ZomesAndHolonsOnly)
                                continue;

                            else
                            {
                                if (genesisType == GenesisType.ZomesAndHolonsOnly)
                                {
                                    line = line.Replace("//ZomesAndHolonsOnly:", "");

                                    if (line.Contains("CelestialBodyOnly"))
                                        continue;
                                }
                                else
                                {
                                    //line = line.Replace("{CELESTIALBODY}", string.Concat(oAppNameSpace.ToPascalCase() , ".", celestialBodyName.ToPascalCase())).Replace("//CelestialBodyOnly:", "");
                                    //line = line.Replace("{CELESTIALBODY}", string.Concat(oAppNameSpace, ".", celestialBodyName.ToPascalCase())).Replace("//CelestialBodyOnly:", "");
                                    line = line.Replace("{CELESTIALBODY}", string.Concat(celestialBodyName.ToPascalCase())).Replace("//CelestialBodyOnly:", "");
                                    line = line.Replace("{CELESTIALBODYVAR}", celestialBodyName.ToCamelCase()).Replace("//CelestialBodyOnly:", "");

                                    if (line.Contains("ZomesAndHolonsOnly"))
                                        continue;
                                }

                                line = line.Replace("{ZOME1}", zomeName.ToPascalCase());
                                line = line.Replace("{HOLON1}", holonName.ToPascalCase());
                                line = line.Replace("{HOLON1_STRINGPROPERTY1}", firstStringProperty.ToPascalCase());
                                //TODO: Add rest of the props, holons, zomes, etc...
                            }

                            // Replace library reference tags
                            if (!string.IsNullOrEmpty(libraryUsingStatements) && line.Contains("{LIBRARYUSINGSTATEMENTS}"))
                                line = line.Replace("{LIBRARYUSINGSTATEMENTS}", libraryUsingStatements);
                            
                            if (!string.IsNullOrEmpty(libraryMethodStubs) && line.Contains("{LIBRARYMETHODSTUBS}"))
                                line = line.Replace("{LIBRARYMETHODSTUBS}", libraryMethodStubs);

                            tw.WriteLine(line);
                            lineNumber++;
                        }
                    }

                    File.Delete(file.FullName);
                    File.Move(string.Concat(file.FullName, ".temp"), file.FullName);
                }

                if (!foundOASISDNA && root)
                {
                    string oappDna = Path.Combine(OAPPFolder, "DNA");
                    if (File.Exists(Path.Combine(oappDna, "OASIS_DNA.json")))
                        File.Delete(Path.Combine(oappDna, "OASIS_DNA.json"));

                    if (File.Exists(Path.Combine(oappDna, "STAR_DNA.json")))
                        File.Delete(Path.Combine(oappDna, "STAR_DNA.json"));

                    if (!Directory.Exists(oappDna))
                        Directory.CreateDirectory(oappDna);

                    File.Copy(OASISDNAPath, Path.Combine(oappDna, "OASIS_DNA.json"));
                    File.Copy(STARDNAPath, Path.Combine(oappDna, "STAR_DNA.json"));
                    //File.Copy(OASISDNAPath, Path.Combine(OAPPFolder, "OASIS_DNA.json"));
                }
            }
        }

        //private void ReplaceInTemplate(string OAPPFolder, string fileExtention)
        //{
        //    foreach (FileInfo file in new DirectoryInfo(OAPPFolder).GetFiles($"*.{fileExtention}"))
        //    {
        //        int lineNumber = 1;
        //        string line = null;

        //        using (TextReader tr = File.OpenText(file.FullName))
        //        using (TextWriter tw = File.CreateText(string.Concat(file.FullName, ".temp")))
        //        {
        //            while ((line = tr.ReadLine()) != null)
        //            {
        //                line = line.Replace("<Compile Remove=\"Program.cs\" />", "");

        //                tw.WriteLine(line);
        //                lineNumber++;
        //            }
        //        }

        //        File.Delete(file.FullName);
        //        File.Move(string.Concat(file.FullName, ".temp"), file.FullName);
        //    }
        //}

        private static async Task<OASISResult<string>> InitOAPPFolderAsync(OAPPType OAPPType, string OAPPName, string genesisFolder, string genesisNameSpace, Guid OAPPTemplateId, int OAPPTemplateVersion, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<string> result = new OASISResult<string>();
            string errorMessage = "An error occured in InitOAPPFolderAsync. Reason:";

            try
            {
                string OAPPFolder = Path.Combine(genesisFolder, OAPPName);

                if (Directory.Exists(OAPPFolder))
                    Directory.Delete(OAPPFolder, true);

                Directory.CreateDirectory(OAPPFolder);

                if (OAPPTemplateId != Guid.Empty)
                {
                    OASISResult<InstalledOAPPTemplate> installedOAPPTemplateResult = await STARAPI.OAPPTemplates.LoadInstalledAsync(BeamedInAvatar.Id, OAPPTemplateId, true, OAPPTemplateVersion, providerType);

                    if (installedOAPPTemplateResult != null && installedOAPPTemplateResult.Result != null && !installedOAPPTemplateResult.IsError)
                        CopyFolder(genesisNameSpace, new DirectoryInfo(installedOAPPTemplateResult.Result.InstalledPath), new DirectoryInfo(OAPPFolder));
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling STARAPI.OAPPTemplates.LoadInstalledOAPPTemplateAsync. Reason: {installedOAPPTemplateResult.Message}");
                        return result;
                    }
                }

                genesisFolder = Path.Combine(OAPPFolder, STARDNA.OAPPGeneratedCodeFolder ?? string.Empty);

                if (!Directory.Exists(genesisFolder))
                    Directory.CreateDirectory(genesisFolder);

                result.Result = OAPPFolder;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured: Reason: {ex}");
            }

            return result;
        }
    }
}