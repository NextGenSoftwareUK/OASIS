using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
using NextGenSoftware.OASIS.API.Providers.TelosOASIS;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS;
using NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;
using NextGenSoftware.Holochain.HoloNET.Client.Core;


namespace NextGenSoftware.OASIS.OASISBootLoader
{
    public static class OASISBootLoader
    {
        public static string OASISDNAFileName { get; set; } = "OASIS_DNA.json";
        public static OASISDNA OASISDNA;
        public static bool IsOASISBooted { get; private set; } = false;
        public static bool IsOASISBooting { get; private set; } = false;

        public static OASISResult<bool> BootOASIS(string OASISDNAFileName)
        {
            LoadOASISDNA(OASISDNAFileName);
            return BootOASIS(OASISDNA);
        }

        public static OASISResult<bool> BootOASIS(OASISDNA OASISDNA)
        {
            OASISResult<bool> result = new OASISResult<bool>(false);
            object OASISProviderBootTypeObject = null;

            if (!IsOASISBooting)
            {
                IsOASISBooting = true;

                OASISDNAManager.OASISDNA = OASISDNA;
                LoggingManager.CurrentLoggingFramework = (LoggingFramework) Enum.Parse(typeof(LoggingFramework), OASISDNA.OASIS.Logging.LoggingFramework);
                ErrorHandling.LogAllErrors = OASISDNA.OASIS.ErrorHandling.LogAllErrors;
                ErrorHandling.LogAllWarnings = OASISDNA.OASIS.ErrorHandling.LogAllWarnings;
                ErrorHandling.ShowStackTrace = OASISDNA.OASIS.ErrorHandling.ShowStackTrace;
                ErrorHandling.ThrowExceptionsOnErrors = OASISDNA.OASIS.ErrorHandling.ThrowExceptionsOnErrors;
                ErrorHandling.ThrowExceptionsOnWarnings = OASISDNA.OASIS.ErrorHandling.ThrowExceptionsOnWarnings;

                ProviderManager.IsAutoFailOverEnabled = OASISDNA.OASIS.StorageProviders.AutoFailOverEnabled;
                ProviderManager.IsAutoLoadBalanceEnabled = OASISDNA.OASIS.StorageProviders.AutoLoadBalanceEnabled;
                ProviderManager.IsAutoReplicationEnabled = OASISDNA.OASIS.StorageProviders.AutoReplicationEnabled;

                LoadProviderLists();

                //TODO: Need to apply this logic to rest of methods in this DNAManager such as RegisterProvider(s) etc... (Actually dont think we need to because this is our the OASIS is booted so it only applies here (the other methods override it).
                if (Enum.TryParse(typeof(OASISProviderBootType), OASISDNA.OASIS.StorageProviders.OASISProviderBootType,
                    out OASISProviderBootTypeObject))
                {
                    ProviderManager.OASISProviderBootType = (OASISProviderBootType) OASISProviderBootTypeObject;

                    if (ProviderManager.OASISProviderBootType == OASISProviderBootType.Warm ||
                        ProviderManager.OASISProviderBootType == OASISProviderBootType.Hot)
                        result = RegisterProvidersInAllLists();
                    else
                    {
                        IsOASISBooted = true;
                        result.Result = true;
                        result.Message =
                            "OASIS Booted but OASISProviderBootType is set to Cold so no providers have been registered or activated.";
                    }
                }
                else
                {
                    result.IsError = true;
                    result.Message = string.Concat("OASISProviderBootType '",
                        OASISDNA.OASIS.StorageProviders.OASISProviderBootType,
                        "' defined in OASISDNA is invalid. Valid values are: ",
                        EnumHelper.GetEnumValues(typeof(OASISProviderBootType),
                            EnumHelperListType.ItemsSeperatedByComma));
                }

                if (result.Result && !result.IsError)
                    IsOASISBooted = true;

                IsOASISBooting = false;
            }
            else
            {
                result.Result = false;
                result.Message = "Already Initializing...";
            }

            return result;
        }

        public static OASISResult<bool> BootOASIS()
        {
            return BootOASIS(OASISDNAFileName);
        }

        public static OASISResult<bool> ShutdownOASIS()
        {
            OASISResult<bool> result = new OASISResult<bool>(true);

            //TODO: Add OASISResult to ActivateProvider and DeActivateProvider so more detailed data can be returned... 
            foreach (IOASISStorageProvider provider in ProviderManager.GetStorageProviders())
                provider.DeActivateProvider();

            return result;
        }

        public static OASISResult<IOASISStorageProvider> GetAndActivateDefaultProvider()
        {
            OASISResult<IOASISStorageProvider> result = new OASISResult<IOASISStorageProvider>();

            if (ProviderManager.CurrentStorageProvider == null)
            {
                if (!IsOASISBooted)
                {
                    OASISResult<bool> initResult = BootOASIS(OASISDNAFileName);

                    if (initResult.IsError)
                    {
                        result.IsError = true;
                        result.Message = initResult.Message;
                        return result;
                    }
                }

                //TODO: Double check this code when I am not half asleep! ;-) lol
                foreach (EnumValue<ProviderType> providerType in ProviderManager.GetProviderAutoFailOverList())
                {
                    OASISResult<IOASISStorageProvider> providerManagerResult = GetAndActivateProvider(providerType.Value);

                    if ((providerManagerResult.IsError || providerManagerResult.Result == null))
                    {
                        ErrorHandling.HandleError(ref result, providerManagerResult.Message);
                        result.InnerMessages.Add(providerManagerResult.Message);
                        result.IsWarning = true;
                        result.IsError = false;

                        if (!ProviderManager.IsAutoFailOverEnabled)
                            break;
                    }
                    else
                        break;
                }

                if (ProviderManager.CurrentStorageProvider == null)
                {
                    result.IsError = true;

                    if (ProviderManager.IsAutoFailOverEnabled)
                        result.Message = $"CRITCAL ERROR: None of the OASIS Providers listed in the AutoFailOver List managed to start. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}Check logs or InnerMessages for more details. Providers in AutoFailOverList are {ProviderManager.GetProviderAutoFailOverListAsString()}.";
                    else
                        result.Message = $"CRITCAL ERROR: AutoFailOver is DISABLED and the first provider in the list failed to start. Reason: {result.InnerMessages[0]}";
                }
                else if (result.InnerMessages.Count > 0)
                {
                    result.IsWarning = true;
                    result.Message = $"WARNING: The {ProviderManager.CurrentStorageProviderType.Name} Provider started but others failed to start. Reason: {OASISResultHelper.BuildInnerMessageError(result.InnerMessages)}Please check the logs or InnerMessages for more details. Providers in AutoFailOverList are {ProviderManager.GetProviderAutoFailOverListAsString()}.";
                }
            }
            else
                result.Result = ProviderManager.CurrentStorageProvider;

            return result;
        }

        public static OASISResult<IOASISStorageProvider> GetAndActivateProvider(ProviderType providerType,
            string customConnectionString = null, bool forceRegister = false, bool setGlobally = false)
        {
            OASISResult<IOASISStorageProvider> result = new OASISResult<IOASISStorageProvider>();

            if (!IsOASISBooted && !IsOASISBooting)
            {
                OASISResult<bool> bootResult = BootOASIS(OASISDNAFileName);

                if (bootResult.IsError)
                {
                    result.IsError = true;
                    result.Message = string.Concat("Error booting OASIS. Reason: ", bootResult.Message);
                    return result;
                }
            }

            //TODO: Think we can have this in ProviderManger and have default connection strings/settings for each provider.
            if (providerType != ProviderManager.CurrentStorageProviderType.Value)
            {
                RegisterProvider(providerType, customConnectionString, forceRegister);

                result = ProviderManager.SetAndActivateCurrentStorageProvider(providerType, setGlobally);
            }

            if (result.IsError != true)
            {
                if (setGlobally && ProviderManager.CurrentStorageProvider !=
                    ProviderManager.DefaultGlobalStorageProvider)
                    ProviderManager.DefaultGlobalStorageProvider = ProviderManager.CurrentStorageProvider;

                ProviderManager.OverrideProviderType = true;
                result.Result = ProviderManager.CurrentStorageProvider;
            }

            return result;
        }

        public static IOASISStorageProvider RegisterProvider(ProviderType providerType, string overrideConnectionString = null,
            bool forceRegister = false)
        {
            IOASISStorageProvider registeredProvider = null;

            if (!IsOASISBooted && !IsOASISBooting)
                BootOASIS(OASISDNAFileName);

            // If they wish to forceRegister then if it is already registered then unregister it first (when connectionstring changes for example).
            if (forceRegister && ProviderManager.IsProviderRegistered(providerType))
                ProviderManager.UnRegisterProvider(providerType);

            if (!ProviderManager.IsProviderRegistered(providerType))
            {
                switch (providerType)
                {
                    case ProviderType.HoloOASIS:
                        {
                            object hcVersion = null;

                            if (Enum.TryParse(typeof(HolochainVersion),
                                OASISDNA.OASIS.StorageProviders.HoloOASIS.HolochainVersion, out hcVersion))
                            {
                                HoloOASIS holoOASIS =
                                    new HoloOASIS(
                                        overrideConnectionString == null
                                            ? OASISDNA.OASIS.StorageProviders.HoloOASIS.ConnectionString
                                            : overrideConnectionString, (HolochainVersion)hcVersion);
                                holoOASIS.OnHoloOASISError += HoloOASIS_OnHoloOASISError;
                                holoOASIS.StorageProviderError += HoloOASIS_StorageProviderError;
                                registeredProvider = holoOASIS;
                            }
                            else
                                throw new ArgumentOutOfRangeException(
                                    "OASISDNA.OASIS.StorageProviders.HoloOASIS.HolochainVersion",
                                    OASISDNA.OASIS.StorageProviders.HoloOASIS.HolochainVersion,
                                    "The HolochainVersion needs to be either RSM or Redux.");
                        }
                        break;

                    case ProviderType.SQLLiteDBOASIS:
                    {
                            //TODO: need to fix or re-write SQLLiteDBOASIS Provider ASAP!

                            SQLLiteDBOASIS SQLLiteDBOASIS = new SQLLiteDBOASIS(overrideConnectionString == null
                                ? OASISDNA.OASIS.StorageProviders.SQLLiteDBOASIS.ConnectionString
                                : overrideConnectionString);
                            SQLLiteDBOASIS.StorageProviderError += SQLLiteDBOASIS_StorageProviderError;
                            registeredProvider = SQLLiteDBOASIS;
                        }
                        break;

                    case ProviderType.MongoDBOASIS:
                    {
                        MongoDBOASIS mongoOASIS =
                            new MongoDBOASIS(
                                overrideConnectionString == null
                                    ? OASISDNA.OASIS.StorageProviders.MongoDBOASIS.ConnectionString
                                    : overrideConnectionString, OASISDNA.OASIS.StorageProviders.MongoDBOASIS.DBName);
                        mongoOASIS.StorageProviderError += MongoOASIS_StorageProviderError;
                        registeredProvider = mongoOASIS;
                    }
                        break;

                    case ProviderType.SolanaOASIS:
                    {
                        SolanaOASIS solanaOasis = new SolanaOASIS(OASISDNA.OASIS.StorageProviders.SolanaOASIS.WalletMnemonicWords);
                        solanaOasis.StorageProviderError += SolanaOASIS_StorageProviderError;
                        registeredProvider = solanaOasis;
                    }
                    break;

                    case ProviderType.EOSIOOASIS:
                    {
                        EOSIOOASIS EOSIOOASIS = new EOSIOOASIS(overrideConnectionString == null
                            ? OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ConnectionString
                            : overrideConnectionString);
                        EOSIOOASIS.StorageProviderError += EOSIOOASIS_StorageProviderError;
                        registeredProvider = EOSIOOASIS;
                    }
                        break;

                    case ProviderType.TelosOASIS:
                    {
                        TelosOASIS TelosOASIS = new TelosOASIS(overrideConnectionString == null
                            ? OASISDNA.OASIS.StorageProviders.TelosOASIS.ConnectionString
                            : overrideConnectionString);
                        TelosOASIS.StorageProviderError += TelosOASIS_StorageProviderError;
                        registeredProvider = TelosOASIS;
                    }
                        break;

                    //case ProviderType.SEEDSOASIS:
                    //    {
                    //        SEEDSOASIS SEEDSOASIS = new SEEDSOASIS(new EOSIOOASIS(OASISDNA.OASIS.StorageProviders.SEEDSOASIS.ConnectionString));
                    //        ProviderManager.RegisterProvider(SEEDSOASIS);
                    //        registeredProvider = SEEDSOASIS;
                    //    }
                    //    break;

                    case ProviderType.Neo4jOASIS:
                    {
                        Neo4jOASIS Neo4jOASIS = new Neo4jOASIS(
                            overrideConnectionString == null
                                ? OASISDNA.OASIS.StorageProviders.Neo4jOASIS.ConnectionString
                                : overrideConnectionString, OASISDNA.OASIS.StorageProviders.Neo4jOASIS.Username,
                            OASISDNA.OASIS.StorageProviders.Neo4jOASIS.Password);
                        Neo4jOASIS.StorageProviderError += Neo4jOASIS_StorageProviderError;
                        registeredProvider = Neo4jOASIS;
                    }
                        break;

                    case ProviderType.IPFSOASIS:
                    {
                        //IPFSOASIS IPFSOASIS = new IPFSOASIS(overrideConnectionString == null ? OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString : overrideConnectionString, OASISDNA.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress);
                        //IPFSOASIS IPFSOASIS = new IPFSOASIS(overrideConnectionString == null ? OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString : overrideConnectionString, OASISDNAFileName);
                        IPFSOASIS IPFSOASIS = null;

                        //Example of how to pass in OASISDNA if the Provider needs to update the DNA.
                        if (overrideConnectionString != null)
                        {
                            OASISDNA overrideDNA = OASISDNA;
                            overrideDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString = overrideConnectionString;
                            IPFSOASIS = new IPFSOASIS(overrideDNA, OASISDNAFileName);
                        }
                        else
                            IPFSOASIS = new IPFSOASIS(OASISDNA, OASISDNAFileName);

                        //IPFSOASIS IPFSOASIS = new IPFSOASIS(overrideConnectionString == null ? OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString : overrideConnectionString, OASISDNAFileName);
                        IPFSOASIS.StorageProviderError += IPFSOASIS_StorageProviderError;
                        registeredProvider = IPFSOASIS;
                    }
                        break;

                    case ProviderType.EthereumOASIS:
                    {
                        EthereumOASIS EthereumOASIS = new EthereumOASIS(overrideConnectionString == null
                            ? OASISDNA.OASIS.StorageProviders.EthereumOASIS.ConnectionString
                            : overrideConnectionString,
                            OASISDNA.OASIS.StorageProviders.EthereumOASIS.SenderAddress);
                        EthereumOASIS.StorageProviderError += EthereumOASIS_StorageProviderError;
                        registeredProvider = EthereumOASIS;
                    }
                        break;

                    case ProviderType.ThreeFoldOASIS:
                    {
                        ThreeFoldOASIS ThreeFoldOASIS = new ThreeFoldOASIS(overrideConnectionString == null
                            ? OASISDNA.OASIS.StorageProviders.ThreeFoldOASIS.ConnectionString
                            : overrideConnectionString);
                        ThreeFoldOASIS.StorageProviderError += ThreeFoldOASIS_StorageProviderError;
                        registeredProvider = ThreeFoldOASIS;
                    }
                        break;
                }

                if (registeredProvider != null)
                    ProviderManager.RegisterProvider(registeredProvider);
                ;
            }
            else
                registeredProvider = (IOASISStorageProvider) ProviderManager.GetProvider(providerType);

            if (ProviderManager.OASISProviderBootType == OASISProviderBootType.Hot)
                ProviderManager.ActivateProvider(registeredProvider);

            return registeredProvider;
        }

        private static void ThreeFoldOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void EthereumOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void TelosOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        public static OASISResult<bool> RegisterProvidersInAutoFailOverList(
            bool abortIfOneProviderFailsToRegister = false)
        {
            return RegisterProviders(ProviderManager.GetProviderAutoFailOverList(), abortIfOneProviderFailsToRegister);
        }

        public static OASISResult<bool> RegisterProvidersInAutoLoadBalanceList(
            bool abortIfOneProviderFailsToRegister = false)
        {
            return RegisterProviders(ProviderManager.GetProviderAutoLoadBalanceList(),
                abortIfOneProviderFailsToRegister);
        }

        public static OASISResult<bool> RegisterProvidersInAutoReplicatingList(
            bool abortIfOneProviderFailsToRegister = false)
        {
            return RegisterProviders(ProviderManager.GetProvidersThatAreAutoReplicating(),
                abortIfOneProviderFailsToRegister);
        }

        public static OASISResult<bool> RegisterProvidersInAllLists(bool abortIfOneProviderFailsToRegister = false)
        {
            OASISResult<bool> result = new OASISResult<bool>(true);
            result = ProcessResult("AutoFailOverList",
                RegisterProvidersInAutoFailOverList(abortIfOneProviderFailsToRegister), result);

            if (result.IsError && abortIfOneProviderFailsToRegister)
                return result;

            result = ProcessResult("AutoLoadBalanceList",
                RegisterProvidersInAutoLoadBalanceList(abortIfOneProviderFailsToRegister), result);

            if (result.IsError && abortIfOneProviderFailsToRegister)
                return result;

            result = ProcessResult("AutoReplicatingList",
                RegisterProvidersInAutoReplicatingList(abortIfOneProviderFailsToRegister), result);

            return result;
        }

        public static OASISResult<bool> RegisterProviders(List<EnumValue<ProviderType>> providerTypes,
            bool abortIfOneProviderFailsToRegister = false)
        {
            List<ProviderType> providerTypesList = new List<ProviderType>();

            foreach (EnumValue<ProviderType> providerType in providerTypes)
                providerTypesList.Add(providerType.Value);

            return RegisterProviders(providerTypesList, abortIfOneProviderFailsToRegister);
        }

        public static OASISResult<bool> RegisterProviders(List<ProviderType> providerTypes,
            bool abortIfOneProviderFailsToRegister = false)
        {
            OASISResult<bool> result = new OASISResult<bool>(true);

            foreach (ProviderType providerType in providerTypes)
            {
                // If a provider fails to register then log it and add to returned error message but continue onto the next provider in the list...
                if (RegisterProvider(providerType) == null)
                {
                    result.Result = false;
                    result.IsError = true;

                    string errorMessage = string.Concat("OASIS Provider ",
                        Enum.GetName(typeof(ProviderType), providerType), " failed to register.\n");
                    result.Message = string.Concat(result.Message, errorMessage);
                    LoggingManager.Log(errorMessage, API.Core.Enums.LogType.Error);

                    if (abortIfOneProviderFailsToRegister)
                        break;
                }
            }

            return result;
        }

        private static OASISResult<bool> ProcessResult(string listName, OASISResult<bool> listResult,
            OASISResult<bool> allListResult)
        {
            if (listResult.IsError)
            {
                string errorMessage = string.Concat("Error registering providers in ", listName, ". Error Details: \n",
                    listResult.Message);
                allListResult.IsError = true;
                allListResult.Message = string.Concat(allListResult.Message, errorMessage);
                LoggingManager.Log(errorMessage, API.Core.Enums.LogType.Error);
            }

            return allListResult;
        }

        private static List<ProviderType> GetProviderTypesFromDNA(string providerListName, string providerList)
        {
            List<ProviderType> providerTypes = new List<ProviderType>();
            string[] providers = providerList.Split(",");
            object providerTypeObject = null;

            foreach (string provider in providers)
            {
                if (Enum.TryParse(typeof(ProviderType), provider.Trim(), out providerTypeObject))
                    providerTypes.Add((ProviderType) providerTypeObject);
                else
                    throw new ArgumentOutOfRangeException(providerListName,
                        string.Concat("ERROR: The OASIS DNA file ", OASISDNAFileName,
                            " contains an invalid entry in the ", providerListName,
                            " comma delimited list. Entry found was ", provider.Trim(), ". Valid entries are:\n\n",
                            EnumHelper.GetEnumValues(typeof(ProviderType))));
            }

            return providerTypes;
        }

        private static void LoadOASISDNA(string OASISDNAFileName)
        {
            OASISBootLoader.OASISDNAFileName = OASISDNAFileName;

            if (File.Exists(OASISDNAFileName))
            {
                using (StreamReader r = new StreamReader(OASISDNAFileName))
                {
                    string json = r.ReadToEnd();
                    OASISDNA = JsonConvert.DeserializeObject<OASISDNA>(json);
                }
            }
            else
                throw new ArgumentNullException("OASISDNAFileName",
                    string.Concat("ERROR: OASIS DNA file not found. Path: ", OASISDNAFileName));
        }

        private static void LoadProviderLists()
        {
            // ProviderManager.DefaultProviderTypes = OASISDNA.OASIS.StorageProviders.DefaultProviders.Split(",");
            ProviderManager.SetAutoFailOverForProviders(true,
                GetProviderTypesFromDNA("AutoFailOverProviders",
                    OASISDNA.OASIS.StorageProviders.AutoFailOverProviders));
            ProviderManager.SetAutoLoadBalanceForProviders(true,
                GetProviderTypesFromDNA("AutoLoadBalanceProviders",
                    OASISDNA.OASIS.StorageProviders.AutoLoadBalanceProviders));
            ProviderManager.SetAutoReplicationForProviders(true,
                GetProviderTypesFromDNA("AutoReplicationProviders",
                    OASISDNA.OASIS.StorageProviders.AutoReplicationProviders));
        }

        private static void IPFSOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void Neo4jOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            throw new NotImplementedException();
        }

        private static void SQLLiteDBOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            //  throw new Exception(string.Concat("ERROR: MongoOASIS_StorageProviderError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private static void EOSIOOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            // throw new Exception(string.Concat("ERROR: EOSIOOASIS_StorageProviderError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private static void MongoOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            //  throw new Exception(string.Concat("ERROR: MongoOASIS_StorageProviderError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private static void SolanaOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            //  throw new Exception(string.Concat("ERROR: MongoOASIS_StorageProviderError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private static void HoloOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            //  throw new Exception(string.Concat("ERROR: HoloOASIS_StorageProviderError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private static void HoloOASIS_OnHoloOASISError(object sender,
            API.Providers.HoloOASIS.Core.HoloOASISErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            //  throw new Exception(string.Concat("ERROR: HoloOASIS_OnHoloOASISError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails, "HoloNET.Reason: ", e.HoloNETErrorDetails.Reason, "HoloNET.ErrorDetails: ", e.HoloNETErrorDetails.ErrorDetails));
        }
    }
}