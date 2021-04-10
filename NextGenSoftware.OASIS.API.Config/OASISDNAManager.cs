using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
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

namespace NextGenSoftware.OASIS.API.DNA
{
    public static class OASISDNAManager 
    {
        public static string OASISDNAFileName { get; set; } = "OASIS_DNA.json";
        public static OASISDNA OASISDNA;

        public static bool IsInitialized
        {
            get
            {
                return OASISDNA != null;
            }
        }

        public static void Initialize(string OASISDNAFileName)
        {
            LoadOASISDNA(OASISDNAFileName);
            LoadProviderLists();
            RegisterProvidersInAllLists();
        }

        public static void Initialize()
        {
            Initialize(OASISDNAFileName);
        }

        public static IOASISStorage GetAndActivateDefaultProvider()
        {
            if (ProviderManager.CurrentStorageProvider == null)
            {
                if (!IsInitialized)
                    Initialize(OASISDNAFileName);

                //TODO: Need to add additional logic later for when the first provider and others fail or are too laggy and so need to switch to a faster provider, etc... DONE! :)
                //return GetAndActivateProvider((ProviderType)Enum.Parse(typeof(ProviderType), ProviderManager.DefaultProviderTypes[0]));
                return GetAndActivateProvider(ProviderManager.GetProviderAutoFailOverList()[0].Value);
            }
            else
                return ProviderManager.CurrentStorageProvider;
        }

        public static IOASISStorage GetAndActivateProvider(ProviderType providerType, bool setGlobally = false)
        {
            if (!IsInitialized)
                Initialize(OASISDNAFileName);

            //TODO: Think we can have this in ProviderManger and have default connection strings/settings for each provider.
            if (providerType != ProviderManager.CurrentStorageProviderType.Value)
            {
                RegisterProvider(providerType);
                ProviderManager.SetAndActivateCurrentStorageProvider(providerType, setGlobally);
            }

            if (setGlobally && ProviderManager.CurrentStorageProvider != ProviderManager.DefaultGlobalStorageProvider)
                ProviderManager.DefaultGlobalStorageProvider = ProviderManager.CurrentStorageProvider;

            ProviderManager.OverrideProviderType = true;
            return ProviderManager.CurrentStorageProvider; 
        }

        public static IOASISStorage RegisterProvider(ProviderType providerType)
        {
            IOASISStorage registeredProvider = null;

            if (!IsInitialized)
                Initialize(OASISDNAFileName);

            if (!ProviderManager.IsProviderRegistered(providerType))
            {
                switch (providerType)
                {
                    case ProviderType.HoloOASIS:
                        {
                            HoloOASIS holoOASIS = new HoloOASIS(OASISDNA.OASIS.StorageProviders.HoloOASIS.ConnectionString, OASISDNA.OASIS.StorageProviders.HoloOASIS.HolochainVersion);
                            holoOASIS.OnHoloOASISError += HoloOASIS_OnHoloOASISError;
                            holoOASIS.StorageProviderError += HoloOASIS_StorageProviderError;
                            ProviderManager.RegisterProvider(holoOASIS);
                            registeredProvider = holoOASIS;
                        }
                        break;

                    case ProviderType.SQLLiteDBOASIS:
                        {
                            SQLLiteDBOASIS SQLLiteDBOASIS = new SQLLiteDBOASIS(OASISDNA.OASIS.StorageProviders.SQLLiteDBOASIS.ConnectionString);
                            SQLLiteDBOASIS.StorageProviderError += SQLLiteDBOASIS_StorageProviderError;
                            ProviderManager.RegisterProvider(SQLLiteDBOASIS);
                            registeredProvider = SQLLiteDBOASIS;
                        }
                        break;

                    case ProviderType.MongoDBOASIS:
                        {
                            MongoDBOASIS mongoOASIS = new MongoDBOASIS(OASISDNA.OASIS.StorageProviders.MongoDBOASIS.ConnectionString, OASISDNA.OASIS.StorageProviders.MongoDBOASIS.DBName);
                            mongoOASIS.StorageProviderError += MongoOASIS_StorageProviderError;
                            ProviderManager.RegisterProvider(mongoOASIS);
                            registeredProvider = mongoOASIS;
                        }
                        break;

                    case ProviderType.EOSIOOASIS:
                        {
                            EOSIOOASIS EOSIOOASIS = new EOSIOOASIS(OASISDNA.OASIS.StorageProviders.EOSIOOASIS.ConnectionString);
                            EOSIOOASIS.StorageProviderError += EOSIOOASIS_StorageProviderError;
                            ProviderManager.RegisterProvider(EOSIOOASIS); 
                            registeredProvider = EOSIOOASIS;
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
                            Neo4jOASIS Neo4jOASIS = new Neo4jOASIS(OASISDNA.OASIS.StorageProviders.Neo4jOASIS.ConnectionString, OASISDNA.OASIS.StorageProviders.Neo4jOASIS.Username, OASISDNA.OASIS.StorageProviders.Neo4jOASIS.Password);
                            Neo4jOASIS.StorageProviderError += Neo4jOASIS_StorageProviderError;
                            ProviderManager.RegisterProvider(Neo4jOASIS); 
                            registeredProvider = Neo4jOASIS;
                        }
                        break;

                    case ProviderType.IPFSOASIS:
                        {
                            IPFSOASIS IPFSOASIS = new IPFSOASIS(OASISDNA.OASIS.StorageProviders.IPFSOASIS.ConnectionString);
                            IPFSOASIS.StorageProviderError += IPFSOASIS_StorageProviderError;
                            ProviderManager.RegisterProvider(IPFSOASIS); 
                            registeredProvider = IPFSOASIS;
                        }
                        break;
                }
            }
            else
                registeredProvider = (IOASISStorage)ProviderManager.GetProvider(providerType);

            return registeredProvider;
        }

        public static bool RegisterProvidersInAutoFailOverList()
        {
            foreach (EnumValue<ProviderType> providerType in ProviderManager.GetProviderAutoFailOverList())
                RegisterProvider(providerType.Value);

            return true;
        }

        public static bool RegisterProvidersInAutoLoadBalanceList()
        {
            foreach (EnumValue<ProviderType> providerType in ProviderManager.GetProviderAutoLoadBalanceList())
                RegisterProvider(providerType.Value);

            return true;
        }

        public static bool RegisterProvidersInAutoReplicatingList()
        {
            foreach (EnumValue<ProviderType> providerType in ProviderManager.GetProvidersThatAreAutoReplicating())
                RegisterProvider(providerType.Value);

            return true;
        }

        public static bool RegisterProvidersInAllLists()
        {
            RegisterProvidersInAutoFailOverList();
            RegisterProvidersInAutoLoadBalanceList();
            RegisterProvidersInAutoReplicatingList();
            return true;
        }

        private static List<ProviderType> GetProviderTypesFromDNA(string providerListName, string providerList)
        {
            List<ProviderType> providerTypes = new List<ProviderType>();
            string[] providers = providerList.Split(",");
            object providerTypeObject = null;

            foreach (string provider in providers)
            {
                if (Enum.TryParse(typeof(ProviderType), provider.Trim(), out providerTypeObject))
                    providerTypes.Add((ProviderType)providerTypeObject);
                else
                    throw new ArgumentOutOfRangeException(providerListName, string.Concat("ERROR: The OASIS DNA file ", OASISDNAFileName, " contains an invalid entry in the ", providerListName, " comma delimited list. Entry found was ", provider.Trim(), ". Valid entries are:\n\n", EnumHelper.GetEnumValues(typeof(ProviderType))));
            }

            return providerTypes;
        }

        private static void LoadOASISDNA(string OASISDNAFileName)
        {
            OASISDNAManager.OASISDNAFileName = OASISDNAFileName;

            if (File.Exists(OASISDNAFileName))
            {
                using (StreamReader r = new StreamReader(OASISDNAFileName))
                {
                    string json = r.ReadToEnd();
                    OASISDNA = JsonConvert.DeserializeObject<OASISDNA>(json);
                }
            }
            else
                throw new ArgumentNullException("OASISDNAFileName", string.Concat("ERROR: OASIS DNA file not found. Path: ", OASISDNAFileName));
        }

        private static void LoadProviderLists()
        {
            // ProviderManager.DefaultProviderTypes = OASISDNA.OASIS.StorageProviders.DefaultProviders.Split(",");
            ProviderManager.SetAutoFailOverForProviders(true, GetProviderTypesFromDNA("AutoFailOverProviders", OASISDNA.OASIS.StorageProviders.AutoFailOverProviders));
            ProviderManager.SetAutoLoadBalanceForProviders(true, GetProviderTypesFromDNA("AutoLoadBalanceProviders", OASISDNA.OASIS.StorageProviders.AutoLoadBalanceProviders));
            ProviderManager.SetAutoReplicationForProviders(true, GetProviderTypesFromDNA("AutoReplicationProviders", OASISDNA.OASIS.StorageProviders.AutoReplicationProviders));
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

        private static void HoloOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            //  throw new Exception(string.Concat("ERROR: HoloOASIS_StorageProviderError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private static void HoloOASIS_OnHoloOASISError(object sender, Providers.HoloOASIS.Core.HoloOASISErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            //  throw new Exception(string.Concat("ERROR: HoloOASIS_OnHoloOASISError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails, "HoloNET.Reason: ", e.HoloNETErrorDetails.Reason, "HoloNET.ErrorDetails: ", e.HoloNETErrorDetails.ErrorDetails));
        }
    }
}
