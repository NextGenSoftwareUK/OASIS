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
        public static async Task<OASISResult<IOmiverse>> IgniteStarAsync(string userName = "", string password = "", string STARDNAPath = STAR_DNA_DEFAULT_PATH, string OASISDNAPath = OASIS_DNA_DEFAULT_PATH, string starId = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();
            Status = StarStatus.Igniting;

            // If you wish to change the logging framework from the default (NLog) then set it below (or just change in OASIS_DNA - prefered way)
            //LoggingManager.CurrentLoggingFramework = LoggingFramework.NLog;

            /*
            var config = new MapperConfiguration(cfg => {
                //cfg.AddProfile<AppProfile>();
                cfg.CreateMap<IHolon, CelestialBody>();
                cfg.CreateMap<IHolon, Zome>();
            });

            Mapper = config.CreateMapper();
            */

            ApplyPlatformSpecificDnaFiles(STARDNAPath);

            if (File.Exists(AppPathHelper.ResolvePathFromAppRoot(STARDNAPath)))
                await STARDNAManager.LoadDNAAsync(STARDNAPath);
            else
            {
                STARDNA = new STARDNA();
                await STARDNAManager.SaveDNAAsync(STARDNAPath, STARDNA);
                STARDNAManager.ResolveRuntimeBasePaths(STARDNA);
            }
            STARDNAPath = STARDNAManager.STARDNAPath;

            ValidateSTARDNA(STARDNA);
            Status = StarStatus.BootingOASIS;
            OASISResult<bool> oasisResult = await BootOASISAsync(userName, password, OASISDNAPath);

            if (oasisResult.IsError)
            {
                string errorMessage = string.Concat("Error whilst booting OASIS. Reason: ", oasisResult.Message);
                OnOASISBootError?.Invoke(null, new OASISBootErrorEventArgs() { ErrorReason = errorMessage });
                OnStarError?.Invoke(null, new StarErrorEventArgs() { Reason = errorMessage });
                result.IsError = true;
                result.Message = errorMessage;
                return result;
            }
            else
                OnOASISBooted?.Invoke(null, new OASISBootedEventArgs() { Message = result.Message });

            OASISDNAPath = OASISBootLoader.OASISBootLoader.OASISDNAPath;
            Status = StarStatus.OASISBooted;

            // If the starId is passed in and is valid then convert to Guid, otherwise get it from the STARDNA file.
            if (!string.IsNullOrEmpty(starId) && !string.IsNullOrWhiteSpace(starId))
            {
                if (!Guid.TryParse(starId, out _starId))
                {
                    //TODO: Need to apply this error handling across the entire OASIS eventually...
                    HandleErrorMessage(ref result, "StarID passed in is invalid. It needs to be a valid Guid.");
                    return result;
                }
            }
            else if (!string.IsNullOrEmpty(STARDNA.DefaultStarId) && !string.IsNullOrWhiteSpace(STARDNA.DefaultStarId) && !Guid.TryParse(STARDNA.DefaultStarId, out _starId))
            {
                HandleErrorMessage(ref result, "StarID defined in the STARDNA file in is invalid. It needs to be a valid Guid.");
                return result;
            }

            result = await IgniteInnerStarAsync(result);

            if (result.IsError)
                Status = StarStatus.Error;
            else
            {
                Status = StarStatus.Ignited;
                IsStarIgnited = true;
                OnStarIgnited.Invoke(null, new StarIgnitedEventArgs() { Message = result.Message });
            }

            return result;
        }

        public static OASISResult<IOmiverse> IgniteStar(string userName = "", string password = "", string STARDNAPath = STAR_DNA_DEFAULT_PATH, string OASISDNAPath = OASIS_DNA_DEFAULT_PATH, string starId = null, ProviderType providerType = ProviderType.Default)
        {
            OASISResult<IOmiverse> result = new OASISResult<IOmiverse>();
            Status = StarStatus.Igniting;

            // If you wish to change the logging framework from the default (NLog) then set it below (or just change in OASIS_DNA - prefered way)
            //LoggingManager.CurrentLoggingFramework = LoggingFramework.NLog;

            /*
            var config = new MapperConfiguration(cfg => {
                //cfg.AddProfile<AppProfile>();
                cfg.CreateMap<IHolon, CelestialBody>();
                cfg.CreateMap<IHolon, Zome>();
            });

            Mapper = config.CreateMapper();
            */

            ApplyPlatformSpecificDnaFiles(STARDNAPath);

            if (File.Exists(AppPathHelper.ResolvePathFromAppRoot(STARDNAPath)))
                STARDNAManager.LoadDNA(STARDNAPath);
            else
            {
                STARDNA = new STARDNA();
                STARDNAManager.SaveDNA(STARDNAPath, STARDNA);
                STARDNAManager.ResolveRuntimeBasePaths(STARDNA);
            }
            STARDNAPath = STARDNAManager.STARDNAPath;

            ValidateSTARDNA(STARDNA);

            IsDetailedCOSMICOutputsEnabled = STARDNA.DetailedCOSMICOutputEnabled;
            IsDetailedStatusUpdatesEnabled = STARDNA.DetailedSTARStatusOutputEnabled;

            Status = StarStatus.BootingOASIS;
            OASISResult<bool> oasisResult = BootOASIS(userName, password, OASISDNAPath);

            if (oasisResult.IsError)
            {
                string errorMessage = string.Concat("Error whilst booting OASIS. Reason: ", oasisResult.Message);
                OnOASISBootError?.Invoke(null, new OASISBootErrorEventArgs() { ErrorReason = errorMessage });
                OnStarError?.Invoke(null, new StarErrorEventArgs() { Reason = errorMessage });
                result.IsError = true;
                result.Message = errorMessage;
                return result;
            }
            else
                OnOASISBooted?.Invoke(null, new OASISBootedEventArgs() { Message = result.Message });

            OASISDNAPath = OASISBootLoader.OASISBootLoader.OASISDNAPath;
            Status = StarStatus.OASISBooted;
            BeamedInAvatar = AvatarManager.LoggedInAvatar;

            // If the starId is passed in and is valid then convert to Guid, otherwise get it from the STARDNA file.
            if (!string.IsNullOrEmpty(starId) && !string.IsNullOrWhiteSpace(starId))
            {
                if (!Guid.TryParse(starId, out _starId))
                {
                    //TODO: Need to apply this error handling across the entire OASIS eventually...
                    HandleErrorMessage(ref result, "StarID passed in is invalid. It needs to be a valid Guid.");
                    return result;
                }
            }
            else if (!string.IsNullOrEmpty(STARDNA.DefaultStarId) && !string.IsNullOrWhiteSpace(STARDNA.DefaultStarId) && !Guid.TryParse(STARDNA.DefaultStarId, out _starId))
            {
                HandleErrorMessage(ref result, "StarID defined in the STARDNA file in is invalid. It needs to be a valid Guid.");
                return result;
            }

            result = IgniteInnerStar(result);

            if (result.IsError)
                Status = StarStatus.Error;
            else
            {
                Status = StarStatus.Ignited;
                IsStarIgnited = true;
                OnStarIgnited?.Invoke(null, new StarIgnitedEventArgs() { Message = result.Message });
            }

            return result;
        }

        public static OASISResult<bool> ExtinguishStar()
        {
            return OASISAPI.ShutdownOASIS();
        }

        public static async Task<OASISResult<bool>> ExtinguishStarAsync()
        {
            return await OASISAPI.ShutdownOASISAsync();
        }

        private static void WireUpEvents()
        {
            if (DefaultStar != null)
            {
                DefaultStar.OnHolonLoaded += InnerStar_OnHolonLoaded;
                DefaultStar.OnHolonSaved += InnerStar_OnHolonSaved;
                DefaultStar.OnHolonsLoaded += InnerStar_OnHolonsLoaded;
                DefaultStar.OnZomeError += InnerStar_OnZomeError;
                DefaultStar.OnInitialized += InnerStar_OnInitialized;
            }
        }

        private static void InnerStar_OnInitialized(object sender, System.EventArgs e)
        {
            OnStarCoreIgnited?.Invoke(sender, e);
        }

        private static void InnerStar_OnZomeError(object sender, ZomeErrorEventArgs e)
        {
            OnZomeError?.Invoke(sender, e);
        }

        private static void InnerStar_OnHolonLoaded(object sender, HolonLoadedEventArgs e)
        {
            OnHolonLoaded?.Invoke(sender, e);
        }

        private static void InnerStar_OnHolonSaved(object sender, HolonSavedEventArgs e)
        {
            OnHolonSaved?.Invoke(sender, e);
        }

        private static void InnerStar_OnHolonsLoaded(object sender, HolonsLoadedEventArgs e)
        {
            OnHolonsLoaded?.Invoke(sender, e);
        }

        public static OASISResult<IAvatar> CreateAvatar(string title, string firstName, string lastName, string email, string username, string password, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green, ProviderType providerType = ProviderType.Default)
        {
            if (!IsStarIgnited)
                IgniteStar();

            return OASISAPI.Avatars.Register(title, firstName, lastName, email, password, username, AvatarType.User, OASISType.STARCLI, cliColour, favColour);
        }

        public static async Task<OASISResult<IAvatar>> CreateAvatarAsync(string title, string firstName, string lastName, string email, string username, string password, ConsoleColor cliColour = ConsoleColor.Green, ConsoleColor favColour = ConsoleColor.Green, ProviderType providerType = ProviderType.Default)
        {
            if (!IsStarIgnited)
                await IgniteStarAsync();

            return await OASISAPI.Avatars.RegisterAsync(title, firstName, lastName, email, password, username, AvatarType.User, OASISType.STARCLI, cliColour, favColour);
        }

        public static async Task<OASISResult<IAvatar>> BeamInAsync(string username, string password, ProviderType providerType = ProviderType.Default)
        {
            string hostName = Dns.GetHostName();
            string IPAddress = Dns.GetHostEntry(hostName).AddressList[0].ToString();

            if (!IsStarIgnited)
                await IgniteStarAsync();

            OASISResult<IAvatar> result = await OASISAPI.Avatars.AuthenticateAsync(username, password, IPAddress);

            if (!result.IsError)
            {
                BeamedInAvatar = (Avatar)result.Result;
                //OASISAPI.LogAvatarIntoOASISManagers(); //TODO: Is there a better way of doing this?

                //BeamedInAvatarDetail = new AvatarDetail()
                //{
                //    Karma = 777
                //};

                //TODO: Fix later! Gifts property de-serialiazed issue in MongoDBOASIS
                OASISResult<IAvatarDetail> loggedInAvatarDetailResult = await OASISAPI.Avatars.LoadAvatarDetailAsync(BeamedInAvatar.Id);

                if (!loggedInAvatarDetailResult.IsError && loggedInAvatarDetailResult.Result != null)
                    BeamedInAvatarDetail = loggedInAvatarDetailResult.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error Occured In BeamInAsync Calling LoadAvatarDetailAsync. Reason: {loggedInAvatarDetailResult.Message}");

                //TODO: NEED TO FIX LATER!
                //await STARAPI.BootSTARAPIAsync(username, password);
                //await STARAPI.InitManagers(username, password);
            }

            return result;
        }

        public static OASISResult<IAvatar> BeamIn(string username, string password, ProviderType providerType = ProviderType.Default)
        {
            string IPAddress = "";
            string hostName = Dns.GetHostName();
            IPHostEntry entry = Dns.GetHostEntry(hostName);

            if (entry != null && entry.AddressList.Length > 1)
                IPAddress = Dns.GetHostEntry(hostName).AddressList[1].ToString();

            if (!IsStarIgnited)
                IgniteStar();

            OASISResult<IAvatar> result = OASISAPI.Avatars.Authenticate(username, password, IPAddress);

            if (!result.IsError)
            {
                BeamedInAvatar = (Avatar)result.Result;

                OASISResult<IAvatarDetail> loggedInAvatarDetailResult = OASISAPI.Avatars.LoadAvatarDetail(BeamedInAvatar.Id);

                if (!loggedInAvatarDetailResult.IsError && loggedInAvatarDetailResult.Result != null)
                    BeamedInAvatarDetail = loggedInAvatarDetailResult.Result;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error Occured In BeamIn Calling LoadAvatarDetail. Reason: {loggedInAvatarDetailResult.Message}");
            }
            return result;
        }

        public static OASISResult<CoronalEjection> Light(string OAPPName, string OAPPDescription, OAPPType OAPPType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, ProviderType providerType = ProviderType.Default)
        {
            return Light(OAPPName, OAPPDescription, OAPPType, OAPPTemplateId, OAPPTemplateVersion, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, metaHolonTagMappings, metaTagMappings, null, providerType);
        }

        //public static OASISResult<CoronalEjection> Light(string OAPPName, string OAPPDescription, OAPPType OAPPType, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", IStar starToAddPlanetTo = null, ProviderType providerType = ProviderType.Default)
        //{
        //    return Light(OAPPName, OAPPDescription, OAPPType, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, (ICelestialBody)starToAddPlanetTo, providerType);
        //}

        //public static OASISResult<CoronalEjection> Light(string OAPPName, string OAPPDescription, OAPPType OAPPType, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", IPlanet planetToAddMoonTo = null, ProviderType providerType = ProviderType.Default)
        //{
        //    return Light(OAPPName, OAPPDescription, OAPPType, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, (ICelestialBody)planetToAddMoonTo, providerType);
        //}

        public static OASISResult<CoronalEjection> Light(string OAPPName, string OAPPDescription, OAPPType OAPPType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, ICelestialBody celestialBodyParent = null, ProviderType providerType = ProviderType.Default)
        {
            return LightAsync(OAPPName, OAPPDescription, OAPPType, OAPPTemplateId, OAPPTemplateVersion, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, metaHolonTagMappings, metaTagMappings, celestialBodyParent, providerType).Result;
        }

        public static OASISResult<CoronalEjection> Light(string OAPPName, string OAPPDescription, OAPPType OAPPType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, Guid celestialBodyParentId = new Guid(), ProviderType providerType = ProviderType.Default)
        {
            return LightInternalAsync(OAPPName, OAPPDescription, OAPPType, OAPPTemplateId, OAPPTemplateVersion, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, metaHolonTagMappings, metaTagMappings, null, celestialBodyParentId, providerType).Result;
        }

        public static async Task<OASISResult<CoronalEjection>> LightAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, ProviderType providerType = ProviderType.Default)
        {
            return await LightAsync(OAPPName, OAPPDescription, OAPPType, OAPPTemplateId, OAPPTemplateVersion, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, metaHolonTagMappings, metaTagMappings, (ICelestialBody)null, providerType);
        }

        //public static async Task<OASISResult<CoronalEjection>> LightAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", IStar starToAddPlanetTo = null, ProviderType providerType = ProviderType.Default)
        //{
        //    return await LightAsync(OAPPName, OAPPDescription, OAPPType, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, (ICelestialBody)starToAddPlanetTo, providerType);
        //}

        //public static async Task<OASISResult<CoronalEjection>> LightAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, GenesisType genesisType,  string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", IPlanet planetToAddMoonTo = null, ProviderType providerType = ProviderType.Default)
        //{
        //    return await LightAsync(OAPPName, OAPPDescription, OAPPType, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, (ICelestialBody)planetToAddMoonTo, providerType);
        //}

        public static async Task<OASISResult<CoronalEjection>> LightAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, Guid OAPPTemplateId, int OAPPTemplateVersion, string zomeAndHolonDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, ProviderType providerType = ProviderType.Default)
        {
            return await LightAsync(OAPPName, OAPPDescription, OAPPType, OAPPTemplateId, OAPPTemplateVersion, GenesisType.ZomesAndHolonsOnly, zomeAndHolonDNAFolder, genesisFolder, genesisNameSpace, metaHolonTagMappings, metaTagMappings, providerType);
        }

        public static async Task<OASISResult<CoronalEjection>> LightAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, Guid celestialBodyParentId = new Guid(), ProviderType providerType = ProviderType.Default)
        {
            return await LightInternalAsync(OAPPName, OAPPDescription, OAPPType, OAPPTemplateId, OAPPTemplateVersion, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, metaHolonTagMappings, metaTagMappings, null, celestialBodyParentId, providerType);
        }

        //public static async Task<OASISResult<CoronalEjection>> LightAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, OAPPTemplateType OAPPTemplateType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", ICelestialBody celestialBodyParent = null, ProviderType providerType = ProviderType.Default)
        public static async Task<OASISResult<CoronalEjection>> LightAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "", string genesisNameSpace = "", List<MetaHolonTag> metaHolonTagMappings = null, Dictionary<string, string> metaTagMappings = null, ICelestialBody celestialBodyParent = null, ProviderType providerType = ProviderType.Default)
        {
            return await LightInternalAsync(OAPPName, OAPPDescription, OAPPType, OAPPTemplateId, OAPPTemplateVersion, genesisType, celestialBodyDNAFolder, genesisFolder, genesisNameSpace, metaHolonTagMappings, metaTagMappings, celestialBodyParent, Guid.Empty, providerType);
        }

        //private static async Task<OASISResult<CoronalEjection>> LightInternalAsync(string OAPPName, string OAPPDescription, OAPPType OAPPType, OAPPTemplateType OAPPTemplateType, Guid OAPPTemplateId, int OAPPTemplateVersion, GenesisType genesisType, string celestialBodyDNAFolder = "", string genesisFolder = "",  string genesisNameSpace = "", ICelestialBody celestialBodyParent = null, Guid celestialBodyParentId = new Guid(), ProviderType providerType = ProviderType.Default)
    }
}