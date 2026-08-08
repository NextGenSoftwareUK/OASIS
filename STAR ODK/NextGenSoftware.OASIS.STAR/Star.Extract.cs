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

    }
}