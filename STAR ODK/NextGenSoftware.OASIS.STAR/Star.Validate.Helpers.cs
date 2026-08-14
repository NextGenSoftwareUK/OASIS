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
    }
}
