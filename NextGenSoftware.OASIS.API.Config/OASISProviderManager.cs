
using System;
using System.IO;
using Newtonsoft.Json;

using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.IPFSOASIS;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Config
{
    // TODO: Not sure if should move this into OASIS.API.Core.ProviderManager? But then Core will have refs to all the providers and 
    // the providers already have a ref to Core so then you will get circular refs so maybe not a good idea?

    public static class OASISProviderManager 
    {
        public static string OASISDNAFileName { get; set; } = "appsettings.json";
        public static OASISSettings OASISSettings;
       // private static OASISProviderManager _instance;
        //public static ProviderType CurrentStorageProviderType = ProviderType.Default;

        //public static OASISProviderManager Instance
        //{
        //    get
        //    {
        //        if (_instance == null)
        //            _instance = new OASISProviderManager(_OASISSettings);

        //        return _instance;
        //    }
        //}



        //public static void SetOASISSettings(OASISSettings settings)
        //{
        //    OASISSettings = settings;
        //}

        public static void LoadOASISSettings(string OASISSettingsFileName)
        {
            if (File.Exists(OASISSettingsFileName))
            {
                using (StreamReader r = new StreamReader(OASISSettingsFileName))
                {
                    string json = r.ReadToEnd();
                    OASISSettings = JsonConvert.DeserializeObject<OASISSettings>(json);
                }
            }
        }

        public static IOASISStorage GetAndActivateProvider()
        {
            if (ProviderManager.CurrentStorageProvider == null)
            {
                if (OASISSettings == null)
                    LoadOASISSettings(OASISDNAFileName);

                ProviderManager.DefaultProviderTypes = OASISSettings.OASIS.StorageProviders.DefaultProviders.Split(",");

                //TODO: Need to add additional logic later for when the first provider and others fail or are too laggy and so need to switch to a faster provider, etc...
                return GetAndActivateProvider((ProviderType)Enum.Parse(typeof(ProviderType), ProviderManager.DefaultProviderTypes[0]));
            }
            else
                return ProviderManager.CurrentStorageProvider;
        }

        public static IOASISStorage GetAndActivateProvider(ProviderType providerType, bool setGlobally = false)
        {
            if (OASISSettings == null)
                LoadOASISSettings(OASISDNAFileName);

            //TODO: Think we can have this in ProviderManger and have default connection strings/settings for each provider.
            if (providerType != ProviderManager.CurrentStorageProviderType)
            {
                if (!ProviderManager.IsProviderRegistered(providerType))
                {
                    switch (providerType)
                    {
                        case ProviderType.HoloOASIS:
                            {
                                HoloOASIS holoOASIS = new HoloOASIS(OASISSettings.OASIS.StorageProviders.HoloOASIS.ConnectionString, HolochainVersion.Redux); //TODO: Move hc version to config.
                                holoOASIS.OnHoloOASISError += HoloOASIS_OnHoloOASISError;
                                holoOASIS.StorageProviderError += HoloOASIS_StorageProviderError;
                                ProviderManager.RegisterProvider(holoOASIS);
                            }
                            break;

                        case ProviderType.SQLLiteDBOASIS:
                            {
                                SQLLiteDBOASIS SQLLiteDBOASIS = new SQLLiteDBOASIS(OASISSettings.OASIS.StorageProviders.SQLLiteDBOASIS.ConnectionString);
                                SQLLiteDBOASIS.StorageProviderError += SQLLiteDBOASIS_StorageProviderError;
                                ProviderManager.RegisterProvider(SQLLiteDBOASIS);

                            }
                            break;

                        case ProviderType.MongoDBOASIS:
                            {
                                MongoDBOASIS mongoOASIS = new MongoDBOASIS(OASISSettings.OASIS.StorageProviders.MongoDBOASIS.ConnectionString, OASISSettings.OASIS.StorageProviders.MongoDBOASIS.DBName);
                                mongoOASIS.StorageProviderError += MongoOASIS_StorageProviderError;
                                ProviderManager.RegisterProvider(mongoOASIS);

                            }
                            break;

                        case ProviderType.EOSOASIS:
                            {
                                EOSIOOASIS EOSIOOASIS = new EOSIOOASIS();
                                EOSIOOASIS.StorageProviderError += EOSIOOASIS_StorageProviderError;
                                ProviderManager.RegisterProvider(EOSIOOASIS); //TODO: Need to pass connection string in.
                            }
                            break;

                        case ProviderType.Neo4jOASIS:
                            {
                                Neo4jOASIS Neo4jOASIS = new Neo4jOASIS();
                                Neo4jOASIS.StorageProviderError += Neo4jOASIS_StorageProviderError;
                                ProviderManager.RegisterProvider(Neo4jOASIS); //TODO: Need to pass connection string in.
                            }
                            break;

                        case ProviderType.IPFSOASIS:
                            {
                                IPFSOASIS IPFSOASIS = new IPFSOASIS();
                                IPFSOASIS.StorageProviderError += IPFSOASIS_StorageProviderError;
                                ProviderManager.RegisterProvider(IPFSOASIS); //TODO: Need to pass connection string in.
                            }
                            break;
                    }
                }

                ProviderManager.SetAndActivateCurrentStorageProvider(providerType, setGlobally);

               // if (setGlobally)
                  //  ProviderManager.IgnoreDefaultProviderTypes = true;
            }

            if (setGlobally && ProviderManager.CurrentStorageProvider != ProviderManager.DefaultGlobalStorageProvider)
                ProviderManager.DefaultGlobalStorageProvider = ProviderManager.CurrentStorageProvider;

            ProviderManager.OverrideProviderType = true;
            return ProviderManager.CurrentStorageProvider; 
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
