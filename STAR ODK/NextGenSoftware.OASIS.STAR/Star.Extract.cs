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
        public static OASISResult<Dictionary<string, IList<INode>>> ExtractNodesFromCelestialBodyMetaDataDNA(string celestialBodyDNAFolder)
        {
            OASISResult<Dictionary<string, IList<INode>>> result = new OASISResult<Dictionary<string, IList<INode>>>(new Dictionary<string, IList<INode>>());

            try
            {
                DirectoryInfo dirInfo = new DirectoryInfo(celestialBodyDNAFolder);
                FileInfo[] files = dirInfo.GetFiles();
                bool holonReached = false;
                string holonName = "";

                foreach (FileInfo file in files)
                {
                    if (file != null)
                    {
                        using (StreamReader reader = file.OpenText())
                        {
                            while (!reader.EndOfStream)
                            {
                                string buffer = reader.ReadLine();

                                if (holonReached && buffer.Length > 1 && buffer.Substring(buffer.Length - 1, 1) == "}" && !buffer.Contains("get;"))
                                {
                                    holonReached = false;
                                    holonName = "";
                                }

                                if (buffer.Contains("HolonDNA"))
                                {
                                    string[] parts = buffer.Split(' ');
                                    holonName = parts[10].ToPascalCase();
                                    holonReached = true;
                                }

                                if (buffer.Contains("string") || buffer.Contains("int") || buffer.Contains("bool"))
                                {
                                    string[] parts = buffer.Split(' ');
                                    string fieldName = parts[14].ToPascalCase();

                                    if (!result.Result.ContainsKey(holonName))
                                        result.Result[holonName] = new List<INode>();

                                    result.Result[holonName].Add(new Node()
                                    {
                                        NodeName = fieldName,
                                        NodeType = parts[13].ToLower() switch
                                        {
                                            "string" => NodeType.String,
                                            "int" => NodeType.Int,
                                            "bool" => NodeType.Bool,
                                            _ => NodeType.Unknown
                                        }
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Invalid CelesitalBodyMetaDataDNA! Please correct or use another!");
            }

            return result;
        }

        ////public static async Task<OASISResult<IGenerateMetaDataDNAResult>> GenerateMetaDataDNAAsync(List<IZome> zomes, string CelestialBodyMetaDataDNAName, string CelestialBodyMetaDataDNADesc, CelestialBodyType celestialBodyType, string ZomeMetaDataDNAName, string ZomeMetaDataDNADesc, ZomeType zomeType, string HolonMetaDataDNAName, string HolonMetaDataDNADesc, HolonType holonType, string fullPathToCelestialBodySourcePath = "", string fullPathToZomeSourcePath = "", string fullPathToHolonSourcePath = "", ProviderType providerType = ProviderType.Default)
        ////public static async Task<OASISResult<IGenerateMetaDataDNAResult>> GenerateMetaDataDNAAsync(List<IZome> zomes, string CelestialBodyMetaDataDNAName, string CelestialBodyMetaDataDNADesc, CelestialBodyType celestialBodyType, string fullPathToCelestialBodySourcePath = "", string fullPathToZomeSourcePath = "", string fullPathToHolonSourcePath = "", ProviderType providerType = ProviderType.Default)
        //public static async Task<OASISResult<IGenerateMetaDataDNAResult>> GenerateMetaDataDNAAsync(List<IZome> zomes, string OAPPName, string OAPPMetaDataDNAPath = "", ProviderType providerType = ProviderType.Default)
        //{
        //    OASISResult<IGenerateMetaDataDNAResult> result = new OASISResult<IGenerateMetaDataDNAResult>();
        //    string errorMessage = "Error occured in STAR.GenerateMetaDataDNAAsync. Reason:";

        //    if (string.IsNullOrEmpty(OAPPMetaDataDNAPath))
        //    {
        //        if (Path.IsPathRooted(STARDNA.OAPPMetaDataDNA))
        //            OAPPMetaDataDNAPath = STARDNA.OAPPMetaDataDNA;
        //        else
        //            OAPPMetaDataDNAPath = Path.Combine(STARDNA.STARBasePath, STARDNA.OAPPMetaDataDNA);
        //    }

        //    //if (string.IsNullOrEmpty(OAPPMetaDataDNAPath))
        //    //{
        //    //    if (Path.IsPathRooted(STARDNA.DefaultCelestialBodiesMetaDataDNASourcePath))
        //    //        fullPathToCelestialBodySourcePath = STARDNA.DefaultCelestialBodiesMetaDataDNASourcePath;

        //    //    else if (Path.IsPathRooted(STARDNA.STARNETBasePath))
        //    //        fullPathToCelestialBodySourcePath = Path.Combine(STARDNA.STARNETBasePath, STARDNA.DefaultCelestialBodiesMetaDataDNASourcePath);

        //    //    else
        //    //        fullPathToCelestialBodySourcePath = Path.Combine(STARDNA.STARBasePath, STARDNA.STARNETBasePath, STARDNA.DefaultCelestialBodiesMetaDataDNASourcePath);
        //    //}

        //    //if (string.IsNullOrEmpty(fullPathToZomeSourcePath))
        //    //{
        //    //    if (Path.IsPathRooted(STARDNA.DefaultZomesMetaDataDNASourcePath))
        //    //        fullPathToZomeSourcePath = STARDNA.DefaultZomesMetaDataDNASourcePath;

        //    //    else if (Path.IsPathRooted(STARDNA.STARNETBasePath))
        //    //        fullPathToZomeSourcePath = Path.Combine(STARDNA.STARNETBasePath, STARDNA.DefaultZomesMetaDataDNASourcePath);

        //    //    else
        //    //        fullPathToZomeSourcePath = Path.Combine(STARDNA.STARBasePath, STARDNA.STARNETBasePath, STARDNA.DefaultZomesMetaDataDNASourcePath);
        //    //}

        //    //if (string.IsNullOrEmpty(fullPathToHolonSourcePath))
        //    //{
        //    //    if (Path.IsPathRooted(STARDNA.DefaultHolonsMetaDataDNASourcePath))
        //    //        fullPathToHolonSourcePath = STARDNA.DefaultHolonsMetaDataDNASourcePath;

        //    //    else if (Path.IsPathRooted(STARDNA.STARNETBasePath))
        //    //        fullPathToHolonSourcePath = Path.Combine(STARDNA.STARNETBasePath, STARDNA.DefaultHolonsMetaDataDNASourcePath);

        //    //    else
        //    //        fullPathToHolonSourcePath = Path.Combine(STARDNA.STARBasePath, STARDNA.STARNETBasePath, STARDNA.DefaultHolonsMetaDataDNASourcePath);
        //    //}


        //OASISResult<STARNETHolon> createResult = await STARAPI.CelestialBodiesMetaDataDNA.CreateAsync(BeamedInAvatar.Id, CelestialBodyMetaDataDNAName, CelestialBodyMetaDataDNADesc, celestialBodyType, fullPathToCelestialBodySourcePath, providerType: providerType);

        //if (createResult != null && createResult.Result != null && !createResult.IsError)
        //    result.Result.CelestialBodyMetaDataDNA = createResult.Result;
        //else
        //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling STARAPI.CelestialBodiesMetaDataDNA.CreateAsync. Reason: {createResult.Message}");


        //createResult = await STARAPI.ZomesMetaDataDNA.CreateAsync(BeamedInAvatar.Id, ZomeMetaDataDNAName, ZomeMetaDataDNADesc, zomeType, fullPathToZomeSourcePath, providerType: providerType);

        //if (createResult != null && createResult.Result != null && !createResult.IsError)
        //    result.Result.ZomeMetaDataDNA = createResult.Result;
        //else
        //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling STARAPI.ZomesMetaDataDNA.CreateAsync. Reason: {createResult.Message}");


        //createResult = await STARAPI.HolonsMetaDataDNA.CreateAsync(BeamedInAvatar.Id, HolonMetaDataDNAName, HolonMetaDataDNADesc, holonType, fullPathToHolonSourcePath, providerType: providerType);

        //if (createResult != null && createResult.Result != null && !createResult.IsError)
        //    result.Result.HolonMetaDataDNA = createResult.Result;
        //else
        //    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling STARAPI.HolonsMetaDataDNA.CreateAsync. Reason: {createResult.Message}");


        //    OASISResult<bool> generateResult = GenerateMetaDataDNA(zomes, fullPathToCelestialBodySourcePath, fullPathToZomeSourcePath, fullPathToHolonSourcePath);

        //    if (!(generateResult != null && generateResult.Result != null && !generateResult.IsError))
        //        OASISErrorHandling.HandleError(ref result, $"{errorMessage} An error occured calling STAR.GenerateMetaDataDNA. Reason: {generateResult.Message}");

        //    return result;
        //}

        //public static OASISResult<bool> GenerateMetaDataDNA(List<IZome> zomes, string generatedCelstialBodyMetaDataDNAPath, string generatedZomeMetaDataDNAPath, string generatedHolonMetaDataDNAPath, ProviderType providerType = ProviderType.Default)
        public static OASISResult<IGenerateMetaDataDNAResult> GenerateMetaDataDNA(List<IZome> zomes, string OAPPName, string OAPPMetaDataDNAPath = "", ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IGenerateMetaDataDNAResult> result = new OASISResult<IGenerateMetaDataDNAResult>();
            string propBuffer = "";
            string holonBuffer = "";
            string holonsBuffer = "";
            string zomeBuffer = "";
            string zomeDNAPath = "";
            string holonDNAPath = "";
            //string propTemplate = "            public {TYPE} {PROPERTYNAME} {get; set;}";
            string propTemplate = "public {TYPE} {PROPERTYNAME} {get; set;}";
            bool firstProp = true;
            int iHolon = 0;
            int iProp = 0;

            try
            {
                if (string.IsNullOrEmpty(OAPPMetaDataDNAPath))
                {
                    if (Path.IsPathRooted(STARDNA.OAPPMetaDataDNAFolder))
                        OAPPMetaDataDNAPath = STARDNA.OAPPMetaDataDNAFolder;
                    else
                        OAPPMetaDataDNAPath = Path.Combine(STARDNA.STARBasePath, STARDNA.OAPPMetaDataDNAFolder);
                }

                result.Result = new GenerateMetaDataDNAResult()
                {
                    CelestialBodyMetaDataDNAPath = Path.Combine(OAPPMetaDataDNAPath, OAPPName, "CelestialBodyDNA"),
                    ZomeMetaDataDNAPath = Path.Combine(OAPPMetaDataDNAPath, OAPPName, "ZomeDNA"),
                    HolonMetaDataDNAPath = Path.Combine(OAPPMetaDataDNAPath, OAPPName, "HolonDNA")
                };

                if (Directory.Exists(result.Result.CelestialBodyMetaDataDNAPath))
                    Directory.Delete(result.Result.CelestialBodyMetaDataDNAPath, true);

                if (Directory.Exists(result.Result.ZomeMetaDataDNAPath))
                    Directory.Delete(result.Result.ZomeMetaDataDNAPath, true);

                if (Directory.Exists(result.Result.HolonMetaDataDNAPath))
                    Directory.Delete(result.Result.HolonMetaDataDNAPath, true);

                Directory.CreateDirectory(result.Result.CelestialBodyMetaDataDNAPath);
                Directory.CreateDirectory(result.Result.ZomeMetaDataDNAPath);
                Directory.CreateDirectory(result.Result.HolonMetaDataDNAPath);

                //TODO: Apply this pathing logic to ALL of STARDNA paths! ;-)
                if (!Path.IsPathRooted(STARDNA.ZomeMetaDataDNA))
                {
                    if (Path.IsPathRooted(STARDNA.MetaDataDNATemplateFolder))
                        zomeDNAPath = Path.Combine(STARDNA.MetaDataDNATemplateFolder, STARDNA.ZomeMetaDataDNA);
                    else
                        zomeDNAPath = Path.Combine(STARDNA.STARBasePath, STARDNA.MetaDataDNATemplateFolder, STARDNA.ZomeMetaDataDNA);
                }
                else
                    zomeDNAPath = STARDNA.ZomeMetaDataDNA;


                if (!Path.IsPathRooted(STARDNA.HolonMetaDataDNA))
                {
                    if (Path.IsPathRooted(STARDNA.MetaDataDNATemplateFolder))
                        holonDNAPath = Path.Combine(STARDNA.MetaDataDNATemplateFolder, STARDNA.HolonMetaDataDNA);
                    else
                        holonDNAPath = Path.Combine(STARDNA.STARBasePath, STARDNA.MetaDataDNATemplateFolder, STARDNA.HolonMetaDataDNA);
                }
                else
                    holonDNAPath = STARDNA.HolonMetaDataDNA;

                string zomeMetaDataDNA = File.ReadAllText(zomeDNAPath);
                string holonMetaDataDNA = File.ReadAllText(holonDNAPath);
                //string[] lines = File.ReadAllLines(holonDNAPath);

                //string holonMetaDataDNA = "";
                //for (int i = 0; i < lines.Length; i++)
                //{
                //    if (!lines[i].Contains("//"))
                //    {
                //        holonMetaDataDNA = string.Concat(holonMetaDataDNA, lines[i]);

                //        if (i < lines.Length - 1)
                //            holonMetaDataDNA = string.Concat(holonMetaDataDNA, "\n");
                //    }
                //}

                foreach (IZome zome in zomes)
                {
                    holonBuffer = "";
                    holonsBuffer = "";
                    iHolon = 0;

                    foreach (IHolon holon in zome.Children)
                    {
                        iHolon++;
                        propBuffer = "";
                        firstProp = true;
                        iProp = 0;

                        foreach (INode node in holon.Nodes)
                        {
                            iProp++;
                            if (!firstProp)
                                propBuffer = string.Concat(propBuffer, "".PadRight(12));
                                    
                            switch (node.NodeType)
                            {
                                case NodeType.Bool:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "bool").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;

                                case NodeType.String:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "string").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;

                                case NodeType.Int:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "int").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;

                                case NodeType.Double:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "double").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;

                                case NodeType.Float:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "float").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;

                                case NodeType.Long:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "long").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;

                                case NodeType.DateTime:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "DateTime").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;

                                case NodeType.ByteArray:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "byte[]").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;

                                case NodeType.Object:
                                    propBuffer = string.Concat(propBuffer, propTemplate.Replace("{TYPE}", "object").Replace("{PROPERTYNAME}", node.NodeName));
                                    break;
                            }

                            if (iProp != holon.Nodes.Count)
                                propBuffer = string.Concat(propBuffer, "\n");

                            firstProp = false;
                        }

                        holonBuffer = string.Concat(holonMetaDataDNA.Replace("{HOLONNAME}", holon.Name).Replace("{PROPERTIES}", propBuffer));

                        if (iHolon != zome.Children.Count)
                            holonBuffer = string.Concat(holonBuffer, "\n\n");

                        holonsBuffer = string.Concat(holonsBuffer, holonBuffer);
                        File.WriteAllText(Path.Combine(result.Result.HolonMetaDataDNAPath, string.Concat(holon.Name, ".cs")), holonBuffer);
                    }

                    zomeBuffer = zomeMetaDataDNA.Replace("{ZOMENAME}", zome.Name).Replace("{HOLONS}", holonsBuffer);
                    File.WriteAllText(Path.Combine(result.Result.ZomeMetaDataDNAPath, string.Concat(zome.Name, ".cs")), zomeBuffer);
                    File.WriteAllText(Path.Combine(result.Result.CelestialBodyMetaDataDNAPath, string.Concat(zome.Name, ".cs")), zomeBuffer);
                }
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in STAR.GenerateMetaDataDNA generating the CelestialBody, Zome & Holon MetaData DNA. Reason: {e}");
            }

            return result;
        }

        public static void ShowStatusMessage(StarStatusMessageType messageType, string message)
        {
            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = messageType, Message = message });
        }

        //public static void ShowStatusMessage(StarStatusChangedEventArgs eventArgs)
        //{
        //    OnStarStatusChanged?.Invoke(null, eventArgs);
        //}

        public static void ShowStatusMessage<T>(OASISEventArgs<T> eventArgs)
        {
            if (eventArgs.Result != null && eventArgs.Result.Result != null)
            {
                if (!eventArgs.Result.IsError)
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"{((IHolon)eventArgs.Result.Result).Name} Created." });
                else
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating {((IHolon)eventArgs.Result.Result)}. Reason: {eventArgs.Result.Message}" });
            }
        }

        private static void NewBody_OnCelestialBodySaved(object sender, CelestialBodySavedEventArgs e)
        {
            if (IsDetailedStatusUpdatesEnabled && e.Result != null && e.Result.Result != null)
            {
                if (!e.Result.IsError)
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"{e.Result.Result.Name} Saved." });
                else
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating {e.Result.Result.Name}. Reason: {e.Result.Message}" });
            }

            /*
            switch (e.Result.Result.HolonType)
            {
                case HolonType.GreatGrandSuperStar:
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "GreatGrandSuperStar Created." });
                    break;

                case HolonType.GrandSuperStar:
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "GrandSuperStar Created." });
                    break;

                case HolonType.Multiverse:
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Default Multiverse Created." });
                    break;

                case HolonType.Dimension:
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"{e.Result.Result.Name} Created." });
                    break;

                case HolonType.Universe:
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Default Universe Created." });
                    break;
            }*/

            //switch (e.Result.Result.Name)
            //{
            //    case "ThirdDimenson"
            //}

            OnCelestialBodySaved?.Invoke(null, e);
        }

        private static void NewBody_OnCelestialBodyError(object sender, CelestialBodyErrorEventArgs e)
        {
            OnCelestialBodyError?.Invoke(null, e);
        }

        private static void NewBody_OnZomeSaved(object sender, ZomeSavedEventArgs e)
        {
            OnZomeSaved?.Invoke(null, e);
        }

        private static void NewBody_OnZomesSaved(object sender, ZomesSavedEventArgs e)
        {
            OnZomesSaved?.Invoke(null, e);
        }

        private static void NewBody_OnZomesError(object sender, ZomesErrorEventArgs e)
        {
            OnZomesError?.Invoke(null, e);
        }

        private static void NewBody_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(null, e);
        }

        private static void NewBody_OnHolonError(object sender, HolonErrorEventArgs e)
        {
            OnHolonError?.Invoke(null, e);
        }

        private static void NewBody_OnHolonsSaved(object sender, HolonsSavedEventArgs e)
        {
            OnHolonsSaved?.Invoke(null, e);
        }

        private static void NewBody_OnHolonsError(object sender, HolonsErrorEventArgs e)
        {
            OnHolonsError?.Invoke(null, e);
        }


        // Build
        public static CoronalEjection Flare(string bodyName)
        {
            //TODO: Build rust code using hc conductor and .net code using dotnet compiler.
            return new CoronalEjection();
        }

        public static CoronalEjection Flare(ICelestialBody body)
        {
            //TODO: Build rust code using hc conductor and .net code using dotnet compiler.
            return new CoronalEjection();
        }

        //Activate & Launch - Launch & activate a planet (OApp) by shining the star's light upon it...
        public static void Shine(ICelestialBody body)
        {

        }

        public static void Shine(string bodyName)
        {

        }

        //Dractivate
        public static void Dim(ICelestialBody body)
        {

        }

        public static void Dim(string bodyName)
        {

        }

        //Publish
        public static async Task<OASISResult<IOAPP>> SeedAsync(string fullPathToOAPP, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool dotnetPublish = true, bool generateOAPPSource = true, bool uploadOAPPSourceToSTARNET = true, bool makeOAPPSourcePublic = false, bool generateOAPPBinary = true, bool generateOAPPSelfContainedBinary = false, bool generateOAPPSelfContainedFullBinary = false, bool uploadOAPPToCloud = false, bool uploadOAPPSelfContainedToCloud = false, bool uploadOAPPSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS, ProviderType oappSelfContainedBinaryProviderType = ProviderType.None, ProviderType oappSelfContainedFullBinaryProviderType = ProviderType.None)
        {
            return await STARAPI.OAPPs.PublishOAPPAsync(BeamedInAvatar.AvatarId, fullPathToOAPP, launchTarget, fullPathToPublishTo, false, registerOnSTARNET, dotnetPublish, generateOAPPSource, uploadOAPPSourceToSTARNET, makeOAPPSourcePublic, generateOAPPBinary, generateOAPPSelfContainedBinary, generateOAPPSelfContainedFullBinary, uploadOAPPToCloud, uploadOAPPSelfContainedToCloud, uploadOAPPSelfContainedFullToCloud, providerType, oappBinaryProviderType, oappSelfContainedBinaryProviderType, oappSelfContainedFullBinaryProviderType);
        }

        public static OASISResult<IOAPP> Seed(string fullPathToOAPP, string launchTarget, string fullPathToPublishTo = "", bool registerOnSTARNET = true, bool dotnetPublish = true, bool generateOAPPSource = true, bool uploadOAPPSourceToSTARNET = true, bool makeOAPPSourcePublic = false, bool generateOAPPBinary = true, bool generateOAPPSelfContainedBinary = false, bool generateOAPPSelfContainedFullBinary = false, bool uploadOAPPToCloud = false, bool uploadOAPPSelfContainedToCloud = false, bool uploadOAPPSelfContainedFullToCloud = false, ProviderType providerType = ProviderType.Default, ProviderType oappBinaryProviderType = ProviderType.IPFSOASIS, ProviderType oappSelfContainedBinaryProviderType = ProviderType.None, ProviderType oappSelfContainedFullBinaryProviderType = ProviderType.None)
        {
            return STARAPI.OAPPs.PublishOAPP(BeamedInAvatar.AvatarId, fullPathToOAPP, launchTarget, fullPathToPublishTo, false, registerOnSTARNET, dotnetPublish, generateOAPPSource, uploadOAPPSourceToSTARNET, makeOAPPSourcePublic, generateOAPPBinary, generateOAPPSelfContainedBinary, generateOAPPSelfContainedFullBinary, uploadOAPPToCloud, uploadOAPPSelfContainedToCloud, uploadOAPPSelfContainedFullToCloud, providerType, oappBinaryProviderType, oappSelfContainedBinaryProviderType, oappSelfContainedFullBinaryProviderType);
        }

        public static async Task<OASISResult<OAPP>> UnSeedAsync(Guid OAPPId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return await STARAPI.OAPPs.UnpublishAsync(BeamedInAvatar.Id, OAPPId, version, providerType);
        }

        public static OASISResult<OAPP> UnSeed(Guid OAPPId, int version = 0, ProviderType providerType = ProviderType.Default)
        {
            return STARAPI.OAPPs.Unpublish(BeamedInAvatar.Id, OAPPId, version, providerType);
        }

        // Run Tests
        public static void Twinkle(ICelestialBody body)
        {

        }

        public static void Twinkle(string bodyName)
        {

        }

        // Delete Planet (OApp)
        public static void Dust(ICelestialBody body)
        {

        }

        // Delete Planet (OApp)
        public static void Dust(string bodyName)
        {

        }

        
        public static void Evolve(ICelestialBody body)
        {

        }

        public static void Evolve(string bodyName)
        {

        }

        public static void Mutate(ICelestialBody body)
        {

        }

        public static void Mutate(string bodyName)
        {

        }

        // Highlight the Planet (OApp) in the OApp Store (StarNET)
        public static void Radiate(ICelestialBody body)
        {

        }

        public static void Radiate(string bodyName)
        {

        }

        // Show how much light the planet (OApp) is emitting into the solar system (StarNET/HoloNET)
        public static void Emit(ICelestialBody body)
        {

        }

        public static void Emit(string bodyName)
        {

        }

        // Show stats of the Planet (OApp)
        public static void Reflect(ICelestialBody body)
        {

        }

        public static void Reflect(string bodyName)
        {

        }

        // Send/Receive Love
        public static void Love(ICelestialBody body)
        {

        }

        public static void Love(string body)
        {

        }

        // Show network stats/management/settings
        public static void Burst(ICelestialBody body)
        {

        }

        public static void Burst(string body)
        {

        }

        // ????
        public static void Pulse(ICelestialBody body)
        {

        }

        public static void Pulse(string body)
        {

        }

        // Reserved For Future Use...
        public static void Super(ICelestialBody body)
        {

        }

        public static void Super(string planetName)
        {

        }

        private static void ValidateSTARDNA(STARDNA starDNA)
        {
            if (starDNA != null)
            {
                STARDNAManager.ResolveRuntimeBasePaths(starDNA);

                ValidateFolder("", starDNA.STARBasePath, "STARDNA.STARBasePath");
                ValidateFolder(starDNA.STARBasePath, starDNA.MetaDataDNATemplateFolder, "STARDNA.MetaDataDNATemplateFolder");
                ValidateFolder(starDNA.STARBasePath, starDNA.OAPPMetaDataDNAFolder, "STARDNA.OAPPMetaDataDNAFolder", false, true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.GenesisFolder, "STARDNA.GenesisFolder", false, true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.GenesisRustFolder, "STARDNA.GenesisRustFolder", false, true);
                ValidateFolder(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, "STARDNA.CSharpDNATemplateFolder");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateHolonDNA, "STARDNA.CSharpTemplateHolonDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateZomeDNA, "STARDNA.CSharpTemplateZomeDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateCelestialBodyDNA, "STARDNA.CSharpTemplateCelestialBodyDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateLoadHolonDNA, "STARDNA.CSharpTemplateLoadHolonDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateSaveHolonDNA, "STARDNA.CSharpTemplateSaveHolonDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateILoadHolonDNA, "STARDNA.CSharpTemplateILoadHolonDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateISaveHolonDNA, "STARDNA.CSharpTemplateISaveHolonDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateInt, "STARDNA.CSharpTemplateInt");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateString, "STARDNA.CSharpTemplateString");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateBool, "STARDNA.CSharpTemplateBool");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateIHolonDNA, "STARDNA.CSharpTemplateIHolonDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateIZomeDNA, "STARDNA.CSharpTemplateIZomeDNA");
                ValidateFile(starDNA.STARBasePath, starDNA.CSharpDNATemplateFolder, starDNA.CSharpTemplateICelestialBodyDNA, "STARDNA.CSharpTemplateICelestialBodyDNA");

                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPBlazorTemplateDNA, "STARDNA.OAPPBlazorTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPConsoleTemplateDNA, "STARDNA.OAPPConsoleTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPCustomTemplateDNA, "STARDNA.OAPPCustomTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPGraphQLServiceTemplateDNA, "STARDNA.OAPPGraphQLServiceTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPgRPCServiceTemplateDNA, "STARDNA.OAPPgRPCServiceTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPMAUITemplateDNA, "STARDNA.OAPPMAUITemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPRESTServiceTemplateDNA, "STARDNA.OAPPRESTServiceTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPUnityTemplateDNA, "STARDNA.OAPPUnityTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPWebMVCTemplateDNA, "STARDNA.OAPPWebMVCTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPWindowsServiceTemplateDNA, "STARDNA.OAPPWindowsServiceTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPWinFormsTemplateDNA, "STARDNA.OAPPWinFormsTemplateDNA", true);
                //ValidateFolder(starDNA.STARBasePath, starDNA.OAPPWPFTemplateDNA, "STARDNA.OAPPWPFTemplateDNA", true);


                // Rust template validation moved to HoloOASIS - commented out for rollback purposes:
                //ValidateFolder(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, "STARDNA.RustDNARSMTemplateFolder");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateLib, "STARDNA.RustTemplateLib");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateCreate, "STARDNA.RustTemplateCreate");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateDelete, "STARDNA.RustTemplateDelete");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateRead, "STARDNA.RustTemplateRead");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateUpdate, "STARDNA.RustTemplateUpdate");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateList, "STARDNA.RustTemplateList");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateValidation, "STARDNA.RustTemplateValidation");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateInt, "STARDNA.RustTemplateInt");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateString, "STARDNA.RustTemplateString");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateBool, "STARDNA.RustTemplateBool");
                //ValidateFile(starDNA.STARBasePath, starDNA.RustDNARSMTemplateFolder, starDNA.RustTemplateHolon, "STARDNA.RustTemplateHolon");

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPsSourcePath))
                    starDNA.DefaultOAPPsSourcePath = Path.Combine("OAPPs", "Source");

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPsPublishedPath))
                    starDNA.DefaultOAPPsPublishedPath = Path.Combine("OAPPs", "Published");

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPsDownloadedPath))
                    starDNA.DefaultOAPPsDownloadedPath = Path.Combine("OAPPs", "Downloaded");

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPsInstalledPath))
                    starDNA.DefaultOAPPsInstalledPath = Path.Combine("OAPPs", "Installed");


                if (string.IsNullOrEmpty(starDNA.DefaultOAPPTemplatesSourcePath))
                    starDNA.DefaultOAPPTemplatesSourcePath = Path.Combine("OAPPTemplates", "Source");

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPTemplatesPublishedPath))
                    starDNA.DefaultOAPPTemplatesPublishedPath = Path.Combine("OAPPTemplates", "Published");

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPTemplatesDownloadedPath))
                    starDNA.DefaultOAPPTemplatesDownloadedPath = Path.Combine("OAPPTemplates", "Downloaded");

                if (string.IsNullOrEmpty(starDNA.DefaultOAPPTemplatesInstalledPath))
                    starDNA.DefaultOAPPTemplatesInstalledPath = Path.Combine("OAPPTemplates", "Installed");


                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesSourcePath))
                    starDNA.DefaultRuntimesSourcePath = Path.Combine("Runtimes", "Source");

                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesPublishedPath))
                    starDNA.DefaultRuntimesPublishedPath = Path.Combine("Runtimes", "Published");

                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesDownloadedPath))
                    starDNA.DefaultRuntimesDownloadedPath = Path.Combine("Runtimes", "Downloaded");

                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesInstalledPath))
                    starDNA.DefaultRuntimesInstalledPath = Path.Combine("Runtimes", "Installed", "Other");

                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesInstalledOASISPath))
                    starDNA.DefaultRuntimesInstalledOASISPath = Path.Combine("Runtimes", "Installed", "OASIS");

                if (string.IsNullOrEmpty(starDNA.DefaultRuntimesInstalledSTARPath))
                    starDNA.DefaultRuntimesInstalledSTARPath = Path.Combine("Runtimes", "Installed", "STAR");

                if (string.IsNullOrEmpty(starDNA.DefaultLibsSourcePath))
                    starDNA.DefaultLibsSourcePath = Path.Combine("Libs", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultLibsPublishedPath))
                    starDNA.DefaultLibsPublishedPath = Path.Combine("Libs", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultLibsDownloadedPath))
                    starDNA.DefaultLibsDownloadedPath = Path.Combine("Libs", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultLibsInstalledPath))
                    starDNA.DefaultLibsInstalledPath = Path.Combine("Libs", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultChaptersSourcePath))
                    starDNA.DefaultChaptersSourcePath = Path.Combine("Chapters", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultChaptersPublishedPath))
                    starDNA.DefaultChaptersPublishedPath = Path.Combine("Chapters", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultChaptersDownloadedPath))
                    starDNA.DefaultChaptersDownloadedPath = Path.Combine("Chapters", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultChaptersInstalledPath))
                    starDNA.DefaultChaptersInstalledPath = Path.Combine("Chapters", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultMissionsSourcePath))
                    starDNA.DefaultMissionsSourcePath = Path.Combine("Missions", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultMissionsPublishedPath))
                    starDNA.DefaultMissionsPublishedPath = Path.Combine("Missions", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultMissionsDownloadedPath))
                    starDNA.DefaultMissionsDownloadedPath = Path.Combine("Missions", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultMissionsInstalledPath))
                    starDNA.DefaultMissionsInstalledPath = Path.Combine("Missions", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultQuestsSourcePath))
                    starDNA.DefaultQuestsSourcePath = Path.Combine("Quests", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultQuestsPublishedPath))
                    starDNA.DefaultQuestsPublishedPath = Path.Combine("Quests", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultQuestsDownloadedPath))
                    starDNA.DefaultQuestsDownloadedPath = Path.Combine("Quests", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultQuestsInstalledPath))
                    starDNA.DefaultQuestsInstalledPath = Path.Combine("Quests", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultGamesSourcePath))
                    starDNA.DefaultGamesSourcePath = Path.Combine("Games", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultGamesPublishedPath))
                    starDNA.DefaultGamesPublishedPath = Path.Combine("Games", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultGamesDownloadedPath))
                    starDNA.DefaultGamesDownloadedPath = Path.Combine("Games", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultGamesInstalledPath))
                    starDNA.DefaultGamesInstalledPath = Path.Combine("Games", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultNFTsSourcePath))
                    starDNA.DefaultNFTsSourcePath = Path.Combine("NFTs", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultNFTsPublishedPath))
                    starDNA.DefaultNFTsPublishedPath = Path.Combine("NFTs", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultNFTsDownloadedPath))
                    starDNA.DefaultNFTsDownloadedPath = Path.Combine("NFTs", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultNFTsInstalledPath))
                    starDNA.DefaultNFTsInstalledPath = Path.Combine("NFTs", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultGeoNFTsSourcePath))
                    starDNA.DefaultGeoNFTsSourcePath = Path.Combine("GeoNFTs", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoNFTsPublishedPath))
                    starDNA.DefaultGeoNFTsPublishedPath = Path.Combine("GeoNFTs", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoNFTsDownloadedPath))
                    starDNA.DefaultGeoNFTsDownloadedPath = Path.Combine("GeoNFTs", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoNFTsInstalledPath))
                    starDNA.DefaultGeoNFTsInstalledPath = Path.Combine("GeoNFTs", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultNFTCollectionsSourcePath))
                    starDNA.DefaultNFTCollectionsSourcePath = Path.Combine("NFTCollections", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultNFTCollectionsPublishedPath))
                    starDNA.DefaultNFTCollectionsPublishedPath = Path.Combine("NFTCollections", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultNFTCollectionsDownloadedPath))
                    starDNA.DefaultNFTCollectionsDownloadedPath = Path.Combine("NFTCollections", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultNFTCollectionsInstalledPath))
                    starDNA.DefaultNFTCollectionsInstalledPath = Path.Combine("NFTCollections", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultGeoNFTCollectionsSourcePath))
                    starDNA.DefaultGeoNFTCollectionsSourcePath = Path.Combine("GeoNFTCollections", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoNFTCollectionsPublishedPath))
                    starDNA.DefaultGeoNFTCollectionsPublishedPath = Path.Combine("GeoNFTCollections", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoNFTCollectionsDownloadedPath))
                    starDNA.DefaultGeoNFTCollectionsDownloadedPath = Path.Combine("GeoNFTCollections", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoNFTCollectionsInstalledPath))
                    starDNA.DefaultGeoNFTCollectionsInstalledPath = Path.Combine("GeoNFTCollections", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultGeoHotSpotsSourcePath))
                    starDNA.DefaultGeoHotSpotsSourcePath = Path.Combine("GeoHotSpots", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoHotSpotsPublishedPath))
                    starDNA.DefaultGeoHotSpotsPublishedPath = Path.Combine("GeoHotSpots", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoHotSpotsDownloadedPath))
                    starDNA.DefaultGeoHotSpotsDownloadedPath = Path.Combine("GeoHotSpots", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultGeoHotSpotsInstalledPath))
                    starDNA.DefaultGeoHotSpotsInstalledPath = Path.Combine("GeoHotSpots", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultInventoryItemsSourcePath))
                    starDNA.DefaultInventoryItemsSourcePath = Path.Combine("InventoryItems", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultInventoryItemsPublishedPath))
                    starDNA.DefaultInventoryItemsPublishedPath = Path.Combine("InventoryItems", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultInventoryItemsDownloadedPath))
                    starDNA.DefaultInventoryItemsDownloadedPath = Path.Combine("InventoryItems", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultInventoryItemsInstalledPath))
                    starDNA.DefaultInventoryItemsInstalledPath = Path.Combine("InventoryItems", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultCelestialSpacesSourcePath))
                    starDNA.DefaultCelestialSpacesSourcePath = Path.Combine("CelestialSpaces", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialSpacesPublishedPath))
                    starDNA.DefaultCelestialSpacesPublishedPath = Path.Combine("CelestialSpaces", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialSpacesDownloadedPath))
                    starDNA.DefaultCelestialSpacesDownloadedPath = Path.Combine("CelestialSpaces", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialSpacesInstalledPath))
                    starDNA.DefaultCelestialSpacesInstalledPath = Path.Combine("CelestialSpaces", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultCelestialBodiesSourcePath))
                    starDNA.DefaultCelestialBodiesSourcePath = Path.Combine("CelestialBodies", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialBodiesPublishedPath))
                    starDNA.DefaultCelestialBodiesPublishedPath = Path.Combine("CelestialBodies", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialBodiesDownloadedPath))
                    starDNA.DefaultCelestialBodiesDownloadedPath = Path.Combine("CelestialBodies", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialBodiesInstalledPath))
                    starDNA.DefaultCelestialBodiesInstalledPath = Path.Combine("CelestialBodies", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultZomesSourcePath))
                    starDNA.DefaultZomesSourcePath = Path.Combine("Zomes", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultZomesPublishedPath))
                    starDNA.DefaultZomesPublishedPath = Path.Combine("Zomes", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultZomesDownloadedPath))
                    starDNA.DefaultZomesDownloadedPath = Path.Combine("Zomes", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultZomesInstalledPath))
                    starDNA.DefaultZomesInstalledPath = Path.Combine("Zomes", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultHolonsSourcePath))
                    starDNA.DefaultHolonsSourcePath = Path.Combine("Holons", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultHolonsPublishedPath))
                    starDNA.DefaultHolonsPublishedPath = Path.Combine("Holons", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultHolonsDownloadedPath))
                    starDNA.DefaultHolonsDownloadedPath = Path.Combine("Holons", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultHolonsInstalledPath))
                    starDNA.DefaultHolonsInstalledPath = Path.Combine("Holons", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultCelestialBodiesMetaDataDNASourcePath))
                    starDNA.DefaultCelestialBodiesMetaDataDNASourcePath = Path.Combine("CelestialBodies", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialBodiesMetaDataDNAPublishedPath))
                    starDNA.DefaultCelestialBodiesMetaDataDNAPublishedPath = Path.Combine("CelestialBodies", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialBodiesMetaDataDNADownloadedPath))
                    starDNA.DefaultCelestialBodiesMetaDataDNADownloadedPath = Path.Combine("CelestialBodies", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultCelestialBodiesMetaDataDNAInstalledPath))
                    starDNA.DefaultCelestialBodiesMetaDataDNAInstalledPath = Path.Combine("CelestialBodies", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultZomesMetaDataDNASourcePath))
                    starDNA.DefaultZomesMetaDataDNASourcePath = Path.Combine("Zomes", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultZomesMetaDataDNAPublishedPath))
                    starDNA.DefaultZomesMetaDataDNAPublishedPath = Path.Combine("Zomes", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultZomesMetaDataDNADownloadedPath))
                    starDNA.DefaultZomesMetaDataDNADownloadedPath = Path.Combine("Zomes", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultZomesMetaDataDNAInstalledPath))
                    starDNA.DefaultZomesMetaDataDNAInstalledPath = Path.Combine("Zomes", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultHolonsMetaDataDNASourcePath))
                    starDNA.DefaultHolonsMetaDataDNASourcePath = Path.Combine("Holons", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultHolonsMetaDataDNAPublishedPath))
                    starDNA.DefaultHolonsMetaDataDNAPublishedPath = Path.Combine("Holons", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultHolonsMetaDataDNADownloadedPath))
                    starDNA.DefaultHolonsMetaDataDNADownloadedPath = Path.Combine("Holons", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultHolonsMetaDataDNAInstalledPath))
                    starDNA.DefaultHolonsMetaDataDNAInstalledPath = Path.Combine("Holons", "Installed");

                if (string.IsNullOrEmpty(starDNA.DefaultPluginsSourcePath))
                    starDNA.DefaultPluginsSourcePath = Path.Combine("Plugins", "Source");
                if (string.IsNullOrEmpty(starDNA.DefaultPluginsPublishedPath))
                    starDNA.DefaultPluginsPublishedPath = Path.Combine("Plugins", "Published");
                if (string.IsNullOrEmpty(starDNA.DefaultPluginsDownloadedPath))
                    starDNA.DefaultPluginsDownloadedPath = Path.Combine("Plugins", "Downloaded");
                if (string.IsNullOrEmpty(starDNA.DefaultPluginsInstalledPath))
                    starDNA.DefaultPluginsInstalledPath = Path.Combine("Plugins", "Installed");

                STARDNAManager.SaveDNA(STARDNAPath, STARDNA);

                ValidateFolder("", starDNA.STARNETBasePath, "STARDNA.STARNETBasePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultOAPPsSourcePath, "STARDNA.DefaultOAPPsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultOAPPsPublishedPath, "STARDNA.DefaultOAPPsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultOAPPsDownloadedPath, "STARDNA.DefaultOAPPsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultOAPPsInstalledPath, "STARDNA.DefaultOAPPsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultOAPPTemplatesSourcePath, "STARDNA.DefaultOAPPTemplatesSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultOAPPTemplatesPublishedPath, "STARDNA.DefaultOAPPTemplatesPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultOAPPTemplatesDownloadedPath, "STARDNA.DefaultOAPPTemplatesDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultOAPPTemplatesInstalledPath, "STARDNA.DefaultOAPPTemplatesInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultRuntimesSourcePath, "STARDNA.DefaultRuntimesSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultRuntimesPublishedPath, "STARDNA.DefaultRuntimesPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultRuntimesDownloadedPath, "STARDNA.DefaultRuntimesDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultRuntimesInstalledPath, "STARDNA.DefaultRuntimesInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultRuntimesInstalledOASISPath, "STARDNA.DefaultRuntimesInstalledOASISPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultRuntimesInstalledSTARPath, "STARDNA.DefaultRuntimesInstalledSTARPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultLibsSourcePath, "STARDNA.DefaultLibsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultLibsPublishedPath, "STARDNA.DefaultLibsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultLibsDownloadedPath, "STARDNA.DefaultLibsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultLibsInstalledPath, "STARDNA.DefaultLibsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultChaptersSourcePath, "STARDNA.DefaultChaptersSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultChaptersPublishedPath, "STARDNA.DefaultChaptersPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultChaptersDownloadedPath, "STARDNA.DefaultChaptersDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultChaptersInstalledPath, "STARDNA.DefaultChaptersInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultMissionsSourcePath, "STARDNA.DefaultMissionsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultMissionsPublishedPath, "STARDNA.DefaultMissionsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultMissionsDownloadedPath, "STARDNA.DefaultMissionsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultMissionsInstalledPath, "STARDNA.DefaultMissionsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultQuestsSourcePath, "STARDNA.DefaultQuestsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultQuestsPublishedPath, "STARDNA.DefaultQuestsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultQuestsDownloadedPath, "STARDNA.DefaultQuestsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultQuestsInstalledPath, "STARDNA.DefaultQuestsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGamesSourcePath, "STARDNA.DefaultGamesSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGamesPublishedPath, "STARDNA.DefaultGamesPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGamesDownloadedPath, "STARDNA.DefaultGamesDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGamesInstalledPath, "STARDNA.DefaultGamesInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultNFTsSourcePath, "STARDNA.DefaultNFTsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultNFTsPublishedPath, "STARDNA.DefaultNFTsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultNFTsDownloadedPath, "STARDNA.DefaultNFTsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultNFTsInstalledPath, "STARDNA.DefaultNFTsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoNFTsSourcePath, "STARDNA.DefaultGeoNFTsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoNFTsPublishedPath, "STARDNA.DefaultGeoNFTsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoNFTsDownloadedPath, "STARDNA.DefaultGeoNFTsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoNFTsInstalledPath, "STARDNA.DefaultGeoNFTsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultNFTCollectionsSourcePath, "STARDNA.DefaultNFTCollectionsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultNFTCollectionsPublishedPath, "STARDNA.DefaultNFTCollectionsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultNFTCollectionsDownloadedPath, "STARDNA.DefaultNFTCollectionsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultNFTCollectionsInstalledPath, "STARDNA.DefaultNFTCollectionsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoNFTCollectionsSourcePath, "STARDNA.DefaultGeoNFTCollectionsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoNFTCollectionsPublishedPath, "STARDNA.DefaultGeoNFTCollectionsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoNFTCollectionsDownloadedPath, "STARDNA.DefaultGeoNFTCollectionsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoNFTCollectionsInstalledPath, "STARDNA.DefaultGeoNFTCollectionsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoHotSpotsSourcePath, "STARDNA.DefaultGeoHotSpotsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoHotSpotsPublishedPath, "STARDNA.DefaultGeoHotSpotsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoHotSpotsDownloadedPath, "STARDNA.DefaultGeoHotSpotsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultGeoHotSpotsInstalledPath, "STARDNA.DefaultGeoHotSpotsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultInventoryItemsSourcePath, "STARDNA.DefaultInventoryItemsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultInventoryItemsPublishedPath, "STARDNA.DefaultInventoryItemsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultInventoryItemsDownloadedPath, "STARDNA.DefaultInventoryItemsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultInventoryItemsInstalledPath, "STARDNA.DefaultInventoryItemsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialSpacesSourcePath, "STARDNA.DefaultCelestialSpacesSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialSpacesPublishedPath, "STARDNA.DefaultCelestialSpacesPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialSpacesDownloadedPath, "STARDNA.DefaultCelestialSpacesDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialSpacesInstalledPath, "STARDNA.DefaultCelestialSpacesInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialBodiesSourcePath, "STARDNA.DefaultCelestialBodiesSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialBodiesPublishedPath, "STARDNA.DefaultCelestialBodiesPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialBodiesDownloadedPath, "STARDNA.DefaultCelestialBodiesDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialBodiesInstalledPath, "STARDNA.DefaultCelestialBodiesInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultZomesSourcePath, "STARDNA.DefaultZomesSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultZomesPublishedPath, "STARDNA.DefaultZomesPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultZomesDownloadedPath, "STARDNA.DefaultZomesDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultZomesInstalledPath, "STARDNA.DefaultZomesInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultHolonsSourcePath, "STARDNA.DefaultHolonsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultHolonsPublishedPath, "STARDNA.DefaultHolonsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultHolonsDownloadedPath, "STARDNA.DefaultHolonsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultHolonsInstalledPath, "STARDNA.DefaultHolonsInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialBodiesMetaDataDNASourcePath, "STARDNA.DefaultCelestialBodiesMetaDataDNASourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialBodiesMetaDataDNAPublishedPath, "STARDNA.DefaultCelestialBodiesMetaDataDNAPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialBodiesMetaDataDNADownloadedPath, "STARDNA.DefaultCelestialBodiesMetaDataDNADownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultCelestialBodiesMetaDataDNAInstalledPath, "STARDNA.DefaultCelestialBodiesMetaDataDNAInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultZomesMetaDataDNASourcePath, "STARDNA.DefaultZomesMetaDataDNASourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultZomesMetaDataDNAPublishedPath, "STARDNA.DefaultZomesMetaDataDNAPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultZomesMetaDataDNADownloadedPath, "STARDNA.DefaultZomesMetaDataDNADownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultZomesMetaDataDNAInstalledPath, "STARDNA.DefaultZomesMetaDataDNAInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultHolonsMetaDataDNASourcePath, "STARDNA.DefaultHolonsMetaDataDNASourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultHolonsMetaDataDNAPublishedPath, "STARDNA.DefaultHolonsMetaDataDNAPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultHolonsMetaDataDNADownloadedPath, "STARDNA.DefaultHolonsMetaDataDNADownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultHolonsMetaDataDNAInstalledPath, "STARDNA.DefaultHolonsMetaDataDNAInstalledPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultPluginsSourcePath, "STARDNA.DefaultPluginsSourcePath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultPluginsPublishedPath, "STARDNA.DefaultPluginsPublishedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultPluginsDownloadedPath, "STARDNA.DefaultPluginsDownloadedPath", false, true);
                ValidateFolder(starDNA.STARNETBasePath, starDNA.DefaultPluginsInstalledPath, "STARDNA.DefaultPluginsInstalledPath", false, true);
            }
            else
                throw new ArgumentNullException("STARDNA is null, please check and try again.");
        }

    }
}
