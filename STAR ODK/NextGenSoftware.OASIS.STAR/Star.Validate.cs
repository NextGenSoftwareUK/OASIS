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
        private static void ValidateLightDNA(string celestialBodyDNAFolder, string genesisFolder)
        {
            ValidateFolder("", celestialBodyDNAFolder, "celestialBodyDNAFolder");
            ValidateFolder("", genesisFolder, "genesisFolder", false, true);
            //ValidateFolder("", genesisRustFolder, "genesisRustFolder", false, true);
        }

        private static void ValidateFolder(string basePath, string folder, string folderParam, bool checkIfContainsFilesOrFolder = false, bool createIfDoesNotExist = false)
        {
            //string path = string.IsNullOrEmpty(basePath) ? folder : $"{basePath}\\{folder}";
            string path = string.IsNullOrEmpty(basePath) ? folder : Path.Combine(basePath, folder);

            if (Path.IsPathRooted(folder))
                path = folder; //If the folder is rooted, use it as is.

            if (string.IsNullOrEmpty(folder))
                throw new ArgumentNullException(folderParam, string.Concat("The ", folderParam, " param in the STARDNA is null, please double check and try again."));

            if (checkIfContainsFilesOrFolder && Directory.GetFiles(path).Length == 0 && Directory.GetDirectories(path).Length == 0)
                throw new InvalidOperationException(string.Concat("The ", folderParam, " folder (", path, ") in the STARDNA is empty."));

            if (!Directory.Exists(path))
            {
                if (createIfDoesNotExist)
                    Directory.CreateDirectory(path);
                else
                    throw new InvalidOperationException(string.Concat("The ", folderParam, " was not found (", path, "), please double check and try again."));
            }
        }

        private static void ValidateFile(string basePath, string folder, string file, string fileParam)
        {
            //string path = $"{basePath}\\{folder}";
            string path = Path.Combine(basePath, folder);

            if (string.IsNullOrEmpty(file))
                throw new ArgumentNullException(fileParam, string.Concat("The ", fileParam, " param in the STARDNA is null, please double check and try again."));

            //if (!File.Exists(string.Concat(path, "\\", file)))
            if (!File.Exists(Path.Combine(path, file)))
                throw new FileNotFoundException(string.Concat("The ", fileParam, " file is not valid, the file does not exist, please double check and try again."), Path.Combine(path, file));
        }

        //private static STARDNA LoadDNA()
        //{
        //    using (StreamReader r = new StreamReader(STARDNAPath))
        //    {
        //        string json = r.ReadToEnd();
        //        STARDNA = JsonConvert.DeserializeObject<STARDNA> (json);
        //        return STARDNA;
        //    }
        //}
        //private static bool SaveDNA()
        //{
        //    try
        //    {
        //        string json = JsonConvert.SerializeObject(STARDNA);

        //        if (!Directory.Exists(Path.GetDirectoryName(STARDNAPath)))
        //            Directory.CreateDirectory(Path.GetDirectoryName(STARDNAPath));

        //        StreamWriter writer = new StreamWriter(STARDNAPath);
        //        writer.Write(json);
        //        writer.Close();
        //    }
        //    catch (Exception e)
        //    {
                
        //    }

        //    return true;
        //}

        private static void NewBody_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            //OnZomeError?.Invoke(sender, new ZomeErrorEventArgs() { EndPoint = StarBody.HoloNETClient.EndPoint, Reason = e.Reason, ErrorDetails = e.ErrorDetails, HoloNETErrorDetails = e.HoloNETErrorDetails });
            // OnStarError?.Invoke(sender, new StarErrorEventArgs() { EndPoint = StarBody.HoloNETClient.EndPoint, Reason = e.Reason, ErrorDetails = e.ErrorDetails, HoloNETErrorDetails = e.HoloNETErrorDetails });
        }

        //TODO: Get this working... :) // Is this working now?! lol hmmmm... need to check...
        private static string GenerateDynamicZomeFunc(string funcName, string zomeTemplateCsharp, string holonName, string zomeBufferCsharp, int funcLength)
        {
            int funcHolonIndex = zomeTemplateCsharp.IndexOf(funcName);
            string funct = zomeTemplateCsharp.Substring(funcHolonIndex - 26, funcLength); //170
            funct = funct.Replace("{holon}", holonName.ToSnakeCase()).Replace("HOLON", holonName.ToPascalCase());
            zomeBufferCsharp = zomeBufferCsharp.Insert(zomeBufferCsharp.Length - 6, funct);
            return zomeBufferCsharp;
        }

        // GenerateRustField method moved to HoloOASIS.NativeCodeGenesis

        private static void GenerateCSharpField(string fieldName, string fieldTemplate, ref string holonBufferCsharp, ref string iHolonBufferCsharp, ref bool firstField, ref bool secondField)
        {
            int fieldsEnd = holonBufferCsharp.LastIndexOf("}") - 7;
            holonBufferCsharp = holonBufferCsharp.Insert(fieldsEnd, string.Concat("\n", fieldTemplate.Replace("variableName", fieldName), "\n"));

            //fieldsEnd = iHolonBufferCsharp.LastIndexOf("}") - 7;

            if (firstField)
            {
                fieldsEnd = iHolonBufferCsharp.LastIndexOf("}") - 10;
                iHolonBufferCsharp = iHolonBufferCsharp.Insert(fieldsEnd, string.Concat(fieldTemplate.Replace("variableName", fieldName)));
                secondField = true;
            }
            else if (secondField)
            {
                secondField = false;
                fieldsEnd = iHolonBufferCsharp.LastIndexOf("}") - 7;
                //iHolonBufferCsharp = iHolonBufferCsharp.Insert(fieldsEnd, string.Concat("\n", fieldTemplate.Replace("variableName", fieldName), "\n"));
                iHolonBufferCsharp = iHolonBufferCsharp.Insert(fieldsEnd, string.Concat(fieldTemplate.Replace("variableName", fieldName), "\n"));
            }
            else
            {
                fieldsEnd = iHolonBufferCsharp.LastIndexOf("}") - 7;
                iHolonBufferCsharp = iHolonBufferCsharp.Insert(fieldsEnd, string.Concat("\n", fieldTemplate.Replace("variableName", fieldName), "\n"));
            }
        }

        private static OASISResult<bool> BootOASIS(string userName = "", string password = "", string OASISDNAPath = OASIS_DNA_DEFAULT_PATH)
        {
            STAR.OASISDNAPath = OASISDNAPath;

            if (!OASISAPI.IsOASISBooted)
                //return OASISAPI.BootOASIS(userName, password, STAR.OASISDNAPath);
                return STARAPI.BootOASISAPI(userName, password, STAR.OASISDNAPath);
            else
                return new OASISResult<bool>() { Message = "OASIS Already Booted" };
        }

        private static async Task<OASISResult<bool>> BootOASISAsync(string userName = "", string password = "", string OASISDNAPath = OASIS_DNA_DEFAULT_PATH)
        {
            STAR.OASISDNAPath = OASISDNAPath;

            if (!OASISAPI.IsOASISBooted)
                //return await OASISAPI.BootOASISAsync(userName, password, STAR.OASISDNAPath);
                return await STARAPI.BootOASISAsync(userName, password, STAR.OASISDNAPath);
            else
                return new OASISResult<bool>() { Message = "OASIS Already Booted" };
        }

        private static OASISResult<IOmiverse> IgniteInnerStar(OASISResult<IOmiverse> result)
        {
            //  _starId = Guid.Empty; //TODO:Temp, remove after!

            ShowStatusMessage(StarStatusMessageType.Processing, "IGNITING INNER STAR...");
            ShowStatusMessage(StarStatusMessageType.Processing, "Checking If OASIS Omniverse Already Created...");

            if (_starId == Guid.Empty)
                result = OASISOmniverseGenesisAsync().Result;
            else
            {
                result = InitDefaultCelestialBodies(result);
            }

            WireUpEvents();
            return result;
        }

        private static async Task<OASISResult<IOmiverse>> IgniteInnerStarAsync(OASISResult<IOmiverse> result)
        {
            // _starId = Guid.Empty; //TODO:Temp, remove after!

            ShowStatusMessage(StarStatusMessageType.Processing, "IGNITING INNER STAR...");
            ShowStatusMessage(StarStatusMessageType.Processing, "Checking If OASIS Omniverse Already Created...");

            if (_starId == Guid.Empty)
                result = await OASISOmniverseGenesisAsync();
            else
                result = await InitDefaultCelestialBodiesAsync(result);

            WireUpEvents();
            return result;
        }

        private static OASISResult<IOmiverse> InitDefaultCelestialBodies(OASISResult<IOmiverse> result)
        {
            ShowStatusMessage(StarStatusMessageType.Success, "OASIS Omniverse Already Created.");
            ShowStatusMessage(StarStatusMessageType.Processing, "Initializing Default Celestial Bodies...");

            (result, DefaultPlanet) = InitCelestialBody<Planet>(STARDNA.DefaultPlanetId, "Default Planet", result);

            if (result.IsError || DefaultPlanet == null)
                return result;

            (result, DefaultStar) = InitCelestialBody<Star>(STARDNA.DefaultStarId, "Default Star", result);

            if (result.IsError || DefaultStar == null)
                return result;

            (result, DefaultSuperStar) = InitCelestialBody<SuperStar>(STARDNA.DefaultSuperStarId, "Default Super Star", result);

            if (result.IsError || DefaultSuperStar == null)
                return result;

            (result, DefaultGrandSuperStar) = InitCelestialBody<GrandSuperStar>(STARDNA.DefaultGrandSuperStarId, "Default Grand Super Star", result);

            if (result.IsError || DefaultGrandSuperStar == null)
                return result;

            (result, DefaultGreatGrandSuperStar) = InitCelestialBody<GreatGrandSuperStar>(STARDNA.DefaultGreatGrandSuperStarId, "Default Great Grand Super Star", result);

            if (result.IsError || DefaultGreatGrandSuperStar == null)
                return result;

            ShowStatusMessage(StarStatusMessageType.Success, "Default Celestial Bodies Initialized.");
            return result;
        }

        private static async Task<OASISResult<IOmiverse>> InitDefaultCelestialBodiesAsync(OASISResult<IOmiverse> result)
        {
            ShowStatusMessage(StarStatusMessageType.Success, "OASIS Omniverse Already Created.");
            ShowStatusMessage(StarStatusMessageType.Processing, "Initializing Default Celestial Bodies...");

            (result, DefaultPlanet) = await InitCelestialBodyAsync<Planet>(STARDNA.DefaultPlanetId, "Default Planet", result);

            if (result.IsError || DefaultPlanet == null)
                return result;

            (result, DefaultStar) = await InitCelestialBodyAsync<Star>(STARDNA.DefaultStarId, "Default Star", result);

            if (result.IsError || DefaultStar == null)
                return result;

            (result, DefaultSuperStar) = await InitCelestialBodyAsync<SuperStar>(STARDNA.DefaultSuperStarId, "Default Super Star", result);

            if (result.IsError || DefaultSuperStar == null)
                return result;

            (result, DefaultGrandSuperStar) = await InitCelestialBodyAsync<GrandSuperStar>(STARDNA.DefaultGrandSuperStarId, "Default Grand Super Star", result);

            if (result.IsError || DefaultGrandSuperStar == null)
                return result;

            (result, DefaultGreatGrandSuperStar) = await InitCelestialBodyAsync<GreatGrandSuperStar>(STARDNA.DefaultGreatGrandSuperStarId, "Default Great Grand Super Star", result);

            if (result.IsError || DefaultGreatGrandSuperStar == null)
                return result;

            ShowStatusMessage(StarStatusMessageType.Success, "Default Celestial Bodies Initialized.");

            return result;
        }

        private static (OASISResult<IOmiverse>, T) InitCelestialBody<T>(string id, string longName, OASISResult<IOmiverse> result) where T : ICelestialBody, new()
        {
            Guid guidId;
            ICelestialBody celestialBody = null;
            string name = longName.Replace(" ", "");

            ShowStatusMessage(StarStatusMessageType.Processing, $"Initializing {longName}...");

            if (!string.IsNullOrEmpty(id))
            {
                if (Guid.TryParse(id, out guidId))
                {
                    //Normally you would leave autoLoad set to true but if you need to process the result in-line then you need to manually call Load as we do here (otherwise you would process the result from the OnCelestialBodyLoaded or OnCelestialBodyError event handlers).
                    //ICelestialBody celestialBody = new T(guidId, false);
                    celestialBody = new T() {  Id = guidId};
                    OASISResult<T> celestialBodyResult = celestialBody.Load<T>();

                    if (celestialBodyResult.IsError || celestialBodyResult.Result == null)
                    {
                        ShowStatusMessage(StarStatusMessageType.Error, $"Error Initializing {longName}.");
                        HandleCelesitalBodyInitError(result, name, id, celestialBodyResult);
                    }
                    else
                    {
                        ShowStatusMessage(StarStatusMessageType.Success, $"{longName} Initialized.");
                        OnDefaultCeletialBodyInit?.Invoke(null, new DefaultCelestialBodyInitEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(celestialBodyResult) });
                    }
                }
                else
                    HandleCelesitalBodyInitError<T>(result, name, id, $"The {name}Id value in STARDNA.json is not a valid Guid.");
            }
            else
                HandleCelesitalBodyInitError<T>(result, name, id, $"The {name}Id value in STARDNA.json is missing.");

            return (result, (T)celestialBody);
        }

        private static async Task<(OASISResult<IOmiverse>, T)> InitCelestialBodyAsync<T>(string id, string longName, OASISResult<IOmiverse> result) where T : ICelestialBody, new()
        {
            Guid guidId;
            ICelestialBody celestialBody = null;
            string name = longName.Replace(" ", "");

            ShowStatusMessage(StarStatusMessageType.Processing, $"Initializing {longName}..");

            if (!string.IsNullOrEmpty(id))
            {
                if (Guid.TryParse(id, out guidId))
                {
                    //Normally you would leave autoLoad set to true but if you need to process the result in-line then you need to manually call Load as we do here (otherwise you would process the result from the OnCelestialBodyLoaded or OnCelestialBodyError event handlers).
                    //ICelestialBody celestialBody = new T(guidId, false);
                    celestialBody = new T() { Id = guidId };
                    OASISResult<T> celestialBodyResult = await celestialBody.LoadAsync<T>();

                    if (celestialBodyResult.IsError || celestialBodyResult.Result == null)
                    {
                        ShowStatusMessage(StarStatusMessageType.Error, $"Error Initializing {longName}.");
                        HandleCelesitalBodyInitError(result, name, id, celestialBodyResult);
                    }
                    else
                    {
                        ShowStatusMessage(StarStatusMessageType.Success, $"{longName} Initialized.");
                        OnDefaultCeletialBodyInit?.Invoke(null, new DefaultCelestialBodyInitEventArgs() { Result = OASISResultHelper.CopyResultToICelestialBody(celestialBodyResult) });
                    }
                }
                else
                    HandleCelesitalBodyInitError<T>(result, name, id, $"The {name}Id value in STARDNA.json is not a valid Guid.");
            }
            else
                HandleCelesitalBodyInitError<T>(result, name, id, $"The {name}Id value in STARDNA.json is missing.");

            return (result, (T)celestialBody);
        }

        //private static void HandleCelesitalBodyInitError(OASISResult<IOmiverse> result, string name, string id, string errorMessage, OASISResult<ICelestialBody> celstialBodyResult = null)
        //{
        //    string msg = $"Error occured in IgniteInnerStar initializing {name} with Id {id}. {errorMessage} Please correct or delete STARDNA to reset STAR ODK to then auto-generate new defaults.";

        //    if (celstialBodyResult != null)
        //        msg = string.Concat(msg, " Reason: ", celstialBodyResult.Message);

        //    OASISErrorHandling.HandleError(ref result, msg, celstialBodyResult != null ? celstialBodyResult.DetailedMessage : null);
        //}

        //private static void HandleCelesitalBodyInitError(OASISResult<IOmiverse> result, string name, string id, OASISResult<ICelestialBody> celstialBodyResult)
        //{
        //    HandleCelesitalBodyInitError(result, name, id, "Likely reason is that the id does not exist.", celstialBodyResult);
        //    //OASISErrorHandling.HandleError(ref result, $"Error occured in IgniteInnerStar initializing {name} with Id {id}. Likely reason is that the id does not exist. Please correct or delete STARDNA to reset STAR ODK to then auto-generate new defaults. Reason: {celstialBodyResult.Message}", celstialBodyResult.DetailedMessage);
        //    //OASISErrorHandling.HandleError(ref result, $"Error occured in IgniteInnerStar initializing {name} with Id {id}. Likely reason is that the id does not exist, in this case remove the {name}Id from STARDNA.json and then try again. Reason: {celstialBodyResult.Message}", celstialBodyResult.DetailedMessage);
        //}

        private static void HandleCelesitalBodyInitError<T>(OASISResult<IOmiverse> result, string name, string id, string errorMessage, OASISResult<T> celstialBodyResult = null) where T : ICelestialBody
        {
            string msg = $"Error occured in IgniteInnerStar initializing {name} with Id {id}. {errorMessage} Please correct or delete STARDNA to reset STAR ODK to then auto-generate new defaults.";

            if (celstialBodyResult != null)
                msg = string.Concat(msg, " Reason: ", celstialBodyResult.Message);

            OASISErrorHandling.HandleError(ref result, msg, celstialBodyResult != null ? celstialBodyResult.DetailedMessage : null);
        }

        private static void HandleCelesitalBodyInitError<T>(OASISResult<IOmiverse> result, string name, string id, OASISResult<T> celstialBodyResult) where T : ICelestialBody
        {
            HandleCelesitalBodyInitError(result, name, id, "Likely reason is that the id does not exist.", celstialBodyResult);
        }


        /// <summary>
        /// Create's the OASIS Omniverse along with a new default Multiverse (with it's GrandSuperStar) containing the ThirdDimension containing UniversePrime (simulation) and the MagicVerse (contains OApp's), which itself contains a default GalaxyCluster containing a default Galaxy (along with it's SuperStar) containing a default SolarSystem (along wth it's Star) containing a default planet (Our World).
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static async Task<OASISResult<IOmiverse>> OASISOmniverseGenesisAsync()
        {
            OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();
            OASISResult<ICelestialSpace> celestialSpaceResult = new OASISResult<ICelestialSpace>();
            ShowStatusMessage(StarStatusMessageType.Processing, "OASIS Omniverse not found. Initiating Omniverse Genesis Process...");

            //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Omniverse..." });

            //Will create the Omniverse with all the omniverse dimensions (8 - 12) along with one default Multiverse and it's dimensions (1-7), each containing a Universe. 
            //The 3rd Dimension will contain the UniversePrime and MagicVerse.
            //It will also create the GreatGrandCentralStar in the centre of the Omniverse and also a GrandCentralStar at the centre of the Multiverse.
            Omniverse omniverse = new Omniverse();
            celestialSpaceResult = await omniverse.SaveAsync();
            OASISResultHelper.CopyResult(celestialSpaceResult, result);
            result.Result = (IOmiverse)celestialSpaceResult.Result;

            if (!result.IsError && result.Result != null)
            {
                //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "CelestialSpace Omniverse Created." });
                STARDNA.DefaultGreatGrandSuperStarId = omniverse.GreatGrandSuperStar.Id.ToString();
                STARDNA.DefaultGrandSuperStarId = omniverse.Multiverses[0].GrandSuperStar.Id.ToString();


                //TODO: May not need any of the code below because the Omniverse Save method will recursively save all it's child CelestialBodies & CelesitalSpaces...
                //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Multiverse..." });
                //Multiverse multiverse = new Multiverse();
                //celestialSpaceResult = await multiverse.SaveAsync(); //TODO: Check tomorrow if this is better way than using old below method (On the STAR Core).
                ////OASISResult<IMultiverse> multiverseResult = await ((GreatGrandSuperStarCore)result.Result.GreatGrandSuperStar.CelestialBodyCore).AddMultiverseAsync(multiverse);

                //if (!celestialSpaceResult.IsError && celestialSpaceResult.Result != null)
                //{
                //    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Multiverse Created." });
                //    multiverse = (Multiverse)celestialSpaceResult.Result;
                //    STARDNA.DefaultGrandSuperStarId = multiverse.GrandSuperStar.Id.ToString();

                //GalaxyCluster galaxyCluster = new GalaxyCluster();
                //galaxyCluster.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                //galaxyCluster.Name = "Our Milky Way Galaxy Cluster.";
                //galaxyCluster.Description = "Our Galaxy Cluster that our Milky Way Galaxy belongs to, the default Galaxy Cluster.";
                //Mapper<IMultiverse, GalaxyCluster>.MapParentCelestialBodyProperties(multiverse, galaxyCluster);
                //galaxyCluster.ParentMultiverse = multiverse;
                //galaxyCluster.ParentMultiverseId = multiverse.Id;
                //galaxyCluster.ParentDimension = multiverse.Dimensions.ThirdDimension;
                //galaxyCluster.ParentDimensionId = multiverse.Dimensions.ThirdDimension.Id;
                //galaxyCluster.ParentUniverseId = multiverse.Dimensions.ThirdDimension.MagicVerse.Id;
                //galaxyCluster.ParentUniverse = multiverse.Dimensions.ThirdDimension.MagicVerse;

                //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Galaxy Cluster..." });
                //OASISResult<IGalaxyCluster> galaxyClusterResult = await ((GrandSuperStarCore)multiverse.GrandSuperStar.CelestialBodyCore).AddGalaxyClusterToUniverseAsync(multiverse.Dimensions.ThirdDimension.MagicVerse, galaxyCluster);

                GalaxyCluster galaxyCluster = new GalaxyCluster();
                galaxyCluster.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                galaxyCluster.Name = "Our Milky Way Galaxy Cluster (Default Galaxy Cluster).";
                galaxyCluster.Description = "Our Galaxy Cluster that our Milky Way Galaxy belongs to, the default Galaxy Cluster.";
                Mapper<IMultiverse, GalaxyCluster>.MapParentCelestialBodyProperties(omniverse.Multiverses[0], galaxyCluster);
                galaxyCluster.ParentMultiverse = omniverse.Multiverses[0];
                galaxyCluster.ParentMultiverseId = omniverse.Multiverses[0].Id;
                galaxyCluster.ParentHolon = omniverse.Multiverses[0];
                galaxyCluster.ParentHolonId = omniverse.Multiverses[0].Id;
                galaxyCluster.ParentCelestialSpace = omniverse.Multiverses[0];
                galaxyCluster.ParentCelestialSpaceId = omniverse.Multiverses[0].Id;
                galaxyCluster.ParentDimension = omniverse.Multiverses[0].Dimensions.ThirdDimension;
                galaxyCluster.ParentDimensionId = omniverse.Multiverses[0].Dimensions.ThirdDimension.Id;
                galaxyCluster.ParentUniverseId = omniverse.Multiverses[0].Dimensions.ThirdDimension.MagicVerse.Id;
                galaxyCluster.ParentUniverse = omniverse.Multiverses[0].Dimensions.ThirdDimension.MagicVerse;

                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = $"Creating CelestialSpace {galaxyCluster.Name}..." });
                OASISResult<IGalaxyCluster> galaxyClusterResult = await ((GrandSuperStarCore)omniverse.Multiverses[0].GrandSuperStar.CelestialBodyCore).AddGalaxyClusterToUniverseAsync(omniverse.Multiverses[0].Dimensions.ThirdDimension.MagicVerse, galaxyCluster);

                if (!galaxyClusterResult.IsError && galaxyClusterResult.Result != null)
                {
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"CelestialSpace {galaxyCluster.Name} Created." }); ;
                    galaxyCluster = (GalaxyCluster)galaxyClusterResult.Result;

                    Galaxy galaxy = new Galaxy();
                    galaxy.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                    galaxy.Name = "Our Milky Way Galaxy (Default Galaxy)";
                    galaxy.Description = "Our Milky Way Galaxy, which is the default Galaxy.";
                    Mapper<IGalaxyCluster, Galaxy>.MapParentCelestialBodyProperties(galaxyCluster, galaxy);
                    galaxy.ParentGalaxyCluster = galaxyCluster;
                    galaxy.ParentGalaxyClusterId = galaxyCluster.Id;
                    galaxy.ParentHolon = galaxyCluster;
                    galaxy.ParentHolonId = galaxyCluster.Id;
                    galaxy.ParentCelestialSpace = galaxyCluster;
                    galaxy.ParentCelestialSpaceId = galaxyCluster.Id;

                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = $"Creating CelestialSpace {galaxy.Name}..." });
                    //OASISResult<IGalaxy> galaxyResult = await ((GrandSuperStarCore)multiverse.GrandSuperStar.CelestialBodyCore).AddGalaxyToGalaxyClusterAsync(galaxyCluster, galaxy);
                    OASISResult<IGalaxy> galaxyResult = await ((GrandSuperStarCore)omniverse.Multiverses[0].GrandSuperStar.CelestialBodyCore).AddGalaxyToGalaxyClusterAsync(galaxyCluster, galaxy);

                    if (!galaxyResult.IsError && galaxyResult.Result != null)
                    {
                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"CelestialSpace {galaxy.Name} Created." });
                        galaxy = (Galaxy)galaxyResult.Result;
                        STARDNA.DefaultSuperStarId = galaxy.SuperStar.Id.ToString();

                        SolarSystem solarSystem = new SolarSystem();
                        solarSystem.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                        solarSystem.Name = "Our Solar System (Default Solar System)";
                        solarSystem.Description = "Our Solar System, which is the default Solar System.";
                        solarSystem.Id = Guid.NewGuid();
                        solarSystem.IsNewHolon = true;

                        Mapper<IGalaxy, Star>.MapParentCelestialBodyProperties(galaxy, (Star)solarSystem.Star);
                        solarSystem.Star.Name = "Our Sun (Sol) (Default Star)";
                        solarSystem.Star.Description = "The Sun at the centre of our Solar System";
                        solarSystem.Star.ParentGalaxy = galaxy;
                        solarSystem.Star.ParentGalaxyId = galaxy.Id;
                        solarSystem.Star.ParentHolon = galaxy;
                        solarSystem.Star.ParentHolonId = galaxy.Id;
                        solarSystem.Star.ParentCelestialSpace = galaxy;
                        solarSystem.Star.ParentCelestialSpaceId = galaxy.Id;
                        solarSystem.Star.ParentSolarSystem = solarSystem;
                        solarSystem.Star.ParentSolarSystemId = solarSystem.Id;

                        //Star star = new Star();
                        //star.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                        //Mapper<IGalaxy, Star>.MapParentCelestialBodyProperties(galaxy, star);
                        //star.Name = "Our Sun (Sol)";
                        //star.Description = "The Sun at the centre of our Solar System";
                        //star.ParentGalaxy = galaxy;
                        //star.ParentGalaxyId = galaxy.Id;
                        //star.ParentSolarSystem = solarSystem;
                        //star.ParentSolarSystemId = solarSystem.Id;

                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = $"Creating CelestialBody {solarSystem.Star.Name}..." });
                        OASISResult<IStar> starResult = await ((SuperStarCore)galaxy.SuperStar.CelestialBodyCore).AddStarAsync(solarSystem.Star);

                        if (!starResult.IsError && starResult.Result != null)
                        {
                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"CelestialBody {solarSystem.Star.Name} Created." });
                            solarSystem.Star = (Star)starResult.Result;
                            DefaultStar = solarSystem.Star; //TODO: TEMP: For now the default Star in STAR ODK will be our Sun (this will be more dynamic later on).
                            STARDNA.DefaultStarId = DefaultStar.Id.ToString();

                            Mapper<IStar, SolarSystem>.MapParentCelestialBodyProperties(solarSystem.Star, solarSystem);
                            solarSystem.ParentStar = solarSystem.Star;
                            solarSystem.ParentStarId = solarSystem.Star.Id;
                            solarSystem.ParentHolon = solarSystem;
                            solarSystem.ParentHolonId = solarSystem.Id;
                            solarSystem.ParentCelestialSpace = solarSystem;
                            solarSystem.ParentCelestialSpaceId = solarSystem.Id;
                            solarSystem.ParentSolarSystem = null;
                            solarSystem.ParentSolarSystemId = Guid.Empty;

                            //TODO: Not sure if this method should also automatically create a Star inside it like the methods above do for Galaxy, Universe etc?
                            // I like how a Star creates its own Solar System from its StarDust, which is how it works in real life I am pretty sure? So I think this is best... :)
                            //TODO: For some reason I could not get Galaxy and Universe to work the same way? Need to come back to this so they all work in the same consistent manner...

                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = $"Creating CelestialSpace {solarSystem.Name}..." });
                            OASISResult<ISolarSystem> solarSystemResult = await ((StarCore)solarSystem.Star.CelestialBodyCore).AddSolarSystemAsync(solarSystem);

                            if (!solarSystemResult.IsError && solarSystemResult.Result != null)
                            {
                                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = $"CelestialSpace {solarSystem.Name} Created." });
                                solarSystem = (SolarSystem)solarSystemResult.Result;

                                Planet ourWorld = new Planet();
                                ourWorld.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                                ourWorld.Name = "Our World (Default Planet)";
                                ourWorld.Description = "The digital twin of our planet and the default planet.";
                                Mapper<ISolarSystem, Planet>.MapParentCelestialBodyProperties(solarSystem, ourWorld);
                                ourWorld.ParentSolarSystem = solarSystem;
                                ourWorld.ParentSolarSystemId = solarSystem.Id;
                                ourWorld.ParentHolon = solarSystem;
                                ourWorld.ParentHolonId = solarSystem.Id;
                                ourWorld.ParentCelestialSpace = solarSystem;
                                ourWorld.ParentCelestialSpaceId = solarSystem.Id;
                                // await ourWorld.InitializeAsync();

                                //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Planet (Our World)..." });
                                OASISResult<IPlanet> ourWorldResult = await ((StarCore)solarSystem.Star.CelestialBodyCore).AddPlanetAsync(ourWorld);

                                if (!ourWorldResult.IsError && ourWorldResult.Result != null)
                                {
                                    //OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Our World Created." });
                                    ourWorld = (Planet)ourWorldResult.Result;
                                    STARDNA.DefaultPlanetId = ourWorld.Id.ToString();
                                }
                                else
                                {
                                    OASISResultHelper.CopyResult(ourWorldResult, result);
                                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Our World. Reason: {ourWorldResult.Message}." });
                                }
                            }
                            else
                                OASISResultHelper.CopyResult(solarSystemResult, result);
                        }
                        else
                        {
                            OASISResultHelper.CopyResult(starResult, result);
                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Star. Reason: {starResult.Message}." });
                        }
                    }
                    else
                    {
                        OASISResultHelper.CopyResult(galaxyResult, result);
                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Galaxy. Reason: {galaxyResult.Message}." });
                    }
                }
                else
                {
                    OASISResultHelper.CopyResult(galaxyClusterResult, result);
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Galaxy Cluster. Reason: {galaxyClusterResult.Message}." });
                }
                //}
                //else
                //{
                //    OASISResultHelper<IMultiverse, ICelestialBody>.CopyResult(multiverseResult, result);
                //    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Multiverse. Reason: {multiverseResult.Message}." });
                //}
            }
            else
                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Omniverse. Reason: {result.Message}." });

            STARDNAManager.SaveDNA(STARDNAPath, STARDNA);

            if (!result.IsError)
            {
                result.Message = "STAR Ignited and The OASIS Omniverse Created.";
                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Omniverse Genesis Process Complete." });
            }

            return result;
        }

        /*
        /// <summary>
        /// Create's the OASIS Omniverse along with a new default Multiverse (with it's GrandSuperStar) containing the ThirdDimension containing UniversePrime (simulation) and the MagicVerse (contains OApp's), which itself contains a default GalaxyCluster containing a default Galaxy (along with it's SuperStar) containing a default SolarSystem (along wth it's Star) containing a default planet (Our World).
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static async Task<OASISResult<ICelestialBody>> OASISOmniverseGenesisAsync()
        {
            OASISResult<ICelestialBody> result = new OASISResult<ICelestialBody>();

            //StarStatus = StarStatus.
            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Omniverse not found. Initiating Omniverse Genesis Process..." });

            Omniverse omniverse = new Omniverse();
            //omniverse.Name = "The OASIS Omniverse";
            //omniverse.Description = "The OASIS Omniverse that contains everything else.";
            //omniverse.IsNewHolon = true;
            //omniverse.Id = Guid.NewGuid();
            //omniverse.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);

            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Great Grand Super Star..." });

            //GreatGrandSuperStar greatGrandSuperStar = new GreatGrandSuperStar(); //GODHEAD ;-)
            //greatGrandSuperStar.IsNewHolon = true;
            //greatGrandSuperStar.Name = "GreatGrandSuperStar";
            //greatGrandSuperStar.Description = "GreatGrandSuperStar at the centre of the Omniverse (The OASIS). Can create Multiverses, Universes, Galaxies, SolarSystems, Stars, Planets (Super OAPPS) and moons (OAPPS)";
            //greatGrandSuperStar.ParentOmniverse = omniverse;
            //greatGrandSuperStar.ParentOmniverseId = omniverse.Id;
            //greatGrandSuperStar.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);

            //omniverse.GreatGrandSuperStar.IsNewHolon = true;
            //omniverse.GreatGrandSuperStar.Name = "GreatGrandSuperStar";
            //omniverse.GreatGrandSuperStar.Description = "";
            //omniverse.GreatGrandSuperStar.ParentOmniverse = omniverse;
            //omniverse.GreatGrandSuperStar.ParentOmniverseId = omniverse.Id;
            //omniverse.ParentGreatGrandSuperStar = omniverse.GreatGrandSuperStar;
            //omniverse.ParentGreatGrandSuperStarId = omniverse.GreatGrandSuperStar.Id;
            //omniverse.GreatGrandSuperStar.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
            //result = await omniverse.GreatGrandSuperStar.SaveAsync(false, false, true); //This would normally save all it's children including the Omniverse but we are creating it seperatley below so no need for that part.
            result = await omniverse.GreatGrandSuperStar.SaveAsync();

            if (!result.IsError && result.Result != null)
            {
                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Great Grand Super Star Created." });
                STARDNA.DefaultGreatGrandSuperStarId = omniverse.GreatGrandSuperStar.Id.ToString();

                //omniverse.Name = "The OASIS Omniverse";
                //omniverse.Description = "The OASIS Omniverse that contains everything else.";
                //omniverse.ParentGreatGrandSuperStar = omniverse.GreatGrandSuperStar;
                //omniverse.ParentGreatGrandSuperStarId = omniverse.GreatGrandSuperStar.Id;

                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Omniverse..." });
                OASISResult<IOmiverse> omiverseResult = await ((GreatGrandSuperStarCore)omniverse.GreatGrandSuperStar.CelestialBodyCore).AddOmiverseAsync(omniverse);

                if (!omiverseResult.IsError && omiverseResult.Result != null)
                {
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Omniverse Created." });
                    Multiverse multiverse = new Multiverse();
                    multiverse.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                    multiverse.Name = "Our Multiverse.";
                    multiverse.Description = "Our Multiverse that our Milky Way Galaxy belongs to, the default Multiverse.";
                    multiverse.ParentOmniverse = omiverseResult.Result;
                    multiverse.ParentOmniverseId = omiverseResult.Result.Id;
                    multiverse.ParentGreatGrandSuperStar = omiverseResult.Result.GreatGrandSuperStar;
                    multiverse.ParentGreatGrandSuperStarId = omiverseResult.Result.GreatGrandSuperStar.Id;
                    multiverse.GrandSuperStar.Name = "The GrandSuperStar at the centre of our Multiverse/Universe.";

                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Multiverse..." });
                    OASISResult<IMultiverse> multiverseResult = await ((GreatGrandSuperStarCore)omiverseResult.Result.GreatGrandSuperStar.CelestialBodyCore).AddMultiverseAsync(multiverse);

                    if (!multiverseResult.IsError && multiverseResult.Result != null)
                    {
                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Multiverse Created." });
                        multiverse = (Multiverse)multiverseResult.Result;
                        STARDNA.DefaultGrandSuperStarId = multiverse.GrandSuperStar.Id.ToString();

                        GalaxyCluster galaxyCluster = new GalaxyCluster();
                        galaxyCluster.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                        galaxyCluster.Name = "Our Milky Way Galaxy Cluster.";
                        galaxyCluster.Description = "Our Galaxy Cluster that our Milky Way Galaxy belongs to, the default Galaxy Cluster.";
                        Mapper<IMultiverse, GalaxyCluster>.MapParentCelestialBodyProperties(multiverse, galaxyCluster);
                        galaxyCluster.ParentMultiverse = multiverse;
                        galaxyCluster.ParentMultiverseId = multiverse.Id;
                        galaxyCluster.ParentDimension = multiverse.Dimensions.ThirdDimension;
                        galaxyCluster.ParentDimensionId = multiverse.Dimensions.ThirdDimension.Id;
                        galaxyCluster.ParentUniverseId = multiverse.Dimensions.ThirdDimension.MagicVerse.Id;
                        galaxyCluster.ParentUniverse = multiverse.Dimensions.ThirdDimension.MagicVerse;

                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Galaxy Cluster..." });
                        OASISResult<IGalaxyCluster> galaxyClusterResult = await ((GrandSuperStarCore)multiverse.GrandSuperStar.CelestialBodyCore).AddGalaxyClusterToUniverseAsync(multiverse.Dimensions.ThirdDimension.MagicVerse, galaxyCluster);

                        if (!galaxyClusterResult.IsError && galaxyClusterResult.Result != null)
                        {
                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Galaxy Cluster Created." }); ;
                            galaxyCluster = (GalaxyCluster)galaxyClusterResult.Result;

                            Galaxy galaxy = new Galaxy();
                            galaxy.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                            galaxy.Name = "Our Milky Way Galaxy";
                            galaxy.Description = "Our Milky Way Galaxy, which is the default Galaxy.";
                            Mapper<IGalaxyCluster, Galaxy>.MapParentCelestialBodyProperties(galaxyCluster, galaxy);
                            galaxy.ParentGalaxyCluster = galaxyCluster;
                            galaxy.ParentGalaxyClusterId = galaxyCluster.Id;

                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Galaxy (Milky Way)..." });
                            OASISResult<IGalaxy> galaxyResult = await ((GrandSuperStarCore)multiverse.GrandSuperStar.CelestialBodyCore).AddGalaxyToGalaxyClusterAsync(galaxyCluster, galaxy);

                            if (!galaxyResult.IsError && galaxyResult.Result != null)
                            {
                                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Galaxy Created." });
                                galaxy = (Galaxy)galaxyResult.Result;
                                STARDNA.DefaultSuperStarId = galaxy.SuperStar.Id.ToString();

                                SolarSystem solarSystem = new SolarSystem();
                                solarSystem.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                                solarSystem.Name = "Our Solar System";
                                solarSystem.Description = "Our Solar System, which is the default Solar System.";
                                solarSystem.Id = Guid.NewGuid();
                                solarSystem.IsNewHolon = true;

                                Mapper<IGalaxy, Star>.MapParentCelestialBodyProperties(galaxy, (Star)solarSystem.Star);
                                solarSystem.Star.Name = "Our Sun (Sol)";
                                solarSystem.Star.Description = "The Sun at the centre of our Solar System";
                                solarSystem.Star.ParentGalaxy = galaxy;
                                solarSystem.Star.ParentGalaxyId = galaxy.Id;
                                solarSystem.Star.ParentSolarSystem = solarSystem;
                                solarSystem.Star.ParentSolarSystemId = solarSystem.Id;

                                //Star star = new Star();
                                //star.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                                //Mapper<IGalaxy, Star>.MapParentCelestialBodyProperties(galaxy, star);
                                //star.Name = "Our Sun (Sol)";
                                //star.Description = "The Sun at the centre of our Solar System";
                                //star.ParentGalaxy = galaxy;
                                //star.ParentGalaxyId = galaxy.Id;
                                //star.ParentSolarSystem = solarSystem;
                                //star.ParentSolarSystemId = solarSystem.Id;

                                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Star (Our Sun)..." });
                                OASISResult<IStar> starResult = await ((SuperStarCore)galaxy.SuperStar.CelestialBodyCore).AddStarAsync(solarSystem.Star);

                                if (!starResult.IsError && starResult.Result != null)
                                {
                                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Star Created." });
                                    solarSystem.Star = (Star)starResult.Result;
                                    DefaultStar = solarSystem.Star; //TODO: TEMP: For now the default Star in STAR ODK will be our Sun (this will be more dynamic later on).
                                    STARDNA.DefaultStarId = DefaultStar.Id.ToString();

                                    Mapper<IStar, SolarSystem>.MapParentCelestialBodyProperties(solarSystem.Star, solarSystem);
                                    solarSystem.ParentStar = solarSystem.Star;
                                    solarSystem.ParentStarId = solarSystem.Star.Id;
                                    solarSystem.ParentSolarSystem = null;
                                    solarSystem.ParentSolarSystemId = Guid.Empty;

                                    //TODO: Not sure if this method should also automatically create a Star inside it like the methods above do for Galaxy, Universe etc?
                                    // I like how a Star creates its own Solar System from its StarDust, which is how it works in real life I am pretty sure? So I think this is best... :)
                                    //TODO: For some reason I could not get Galaxy and Universe to work the same way? Need to come back to this so they all work in the same consistent manner...

                                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Solar System..." });
                                    OASISResult<ISolarSystem> solarSystemResult = await ((StarCore)solarSystem.Star.CelestialBodyCore).AddSolarSystemAsync(solarSystem);

                                    if (!solarSystemResult.IsError && solarSystemResult.Result != null)
                                    {
                                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Solar System Created." });
                                        solarSystem = (SolarSystem)solarSystemResult.Result;

                                        Planet ourWorld = new Planet();
                                        ourWorld.CreatedOASISType = new EnumValue<OASISType>(OASISType.STARCLI);
                                        ourWorld.Name = "Our World";
                                        ourWorld.Description = "The digital twin of our planet and the default planet.";
                                        Mapper<ISolarSystem, Planet>.MapParentCelestialBodyProperties(solarSystem, ourWorld);
                                        ourWorld.ParentSolarSystem = solarSystem;
                                        ourWorld.ParentSolarSystemId = solarSystem.Id;
                                        // await ourWorld.InitializeAsync();

                                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Processing, Message = "Creating Default Planet (Our World)..." });
                                        OASISResult<IPlanet> ourWorldResult = await ((StarCore)solarSystem.Star.CelestialBodyCore).AddPlanetAsync(ourWorld);

                                        if (!ourWorldResult.IsError && ourWorldResult.Result != null)
                                        {
                                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Our World Created." });
                                            ourWorld = (Planet)ourWorldResult.Result;
                                            STARDNA.DefaultPlanetId = ourWorld.Id.ToString();
                                        }
                                        else
                                        {
                                            OASISResultHelper<IPlanet, ICelestialBody>.CopyResult(ourWorldResult, result);
                                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Our World. Reason: {ourWorldResult.Message}." });
                                        }
                                    }
                                    else
                                        OASISResultHelper<ISolarSystem, ICelestialBody>.CopyResult(solarSystemResult, result);
                                }
                                else
                                {
                                    OASISResultHelper<IStar, ICelestialBody>.CopyResult(starResult, result);
                                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Star. Reason: {starResult.Message}." });
                                }
                            }
                            else
                            {
                                OASISResultHelper<IGalaxy, ICelestialBody>.CopyResult(galaxyResult, result);
                                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Galaxy. Reason: {galaxyResult.Message}." });
                            }
                        }
                        else
                        {
                            OASISResultHelper<IGalaxyCluster, ICelestialBody>.CopyResult(galaxyClusterResult, result);
                            OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Galaxy Cluster. Reason: {galaxyClusterResult.Message}." });
                        }
                    }
                    else
                    {
                        OASISResultHelper<IMultiverse, ICelestialBody>.CopyResult(multiverseResult, result);
                        OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Multiverse. Reason: {multiverseResult.Message}." });
                    }
                }
                else
                {
                    OASISResultHelper<IOmiverse, ICelestialBody>.CopyResult(omiverseResult, result);
                    OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Error, Message = $"Error Creating Omniverse. Reason: {omiverseResult.Message}." });
                }
            }

            SaveDNA();

            if (!result.IsError)
            {
                result.Message = "STAR Ignited and The OASIS Omniverse Created.";
                OnStarStatusChanged?.Invoke(null, new StarStatusChangedEventArgs() { MessageType = StarStatusMessageType.Success, Message = "Omniverse Genesis Process Complete." });
            }

            return result;
        }*/

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
