
using System;

using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.StarknetOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    // TODO: Not sure if should move this into OASIS.API.Core.ProviderManager? But then Core will have refs to all the providers and 
    // the providers already have a ref to Core so then you will get circular refs so maybe not a good idea?

    public static class OASISProviderManager 
    {
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

        public static IOASISStorage GetAndActivateProvider()
        {
            ProviderManager.DefaultProviderTypes = OASISSettings.StorageProviders.DefaultProviders.Split(",");

            //TODO: Need to add additional logic later for when the first provider and others fail or are too laggy and so need to switch to a faster provider, etc...
            return GetAndActivateProvider((ProviderType)Enum.Parse(typeof(ProviderType), ProviderManager.DefaultProviderTypes[0]));
        }

        public static IOASISStorage GetAndActivateProvider(ProviderType providerType, bool setGlobally = false)
        {
            //TODO: Think we can have this in ProviderManger and have default connection strings/settings for each provider.
            if (providerType != ProviderManager.CurrentStorageProviderType)
            {
                if (!ProviderManager.IsProviderRegistered(providerType))
                {
                    switch (providerType)
                    {
                        case ProviderType.HoloOASIS:
                            {
                                HoloOASIS holoOASIS = new HoloOASIS(OASISSettings.StorageProviders.HoloOASIS.ConnectionString);
                                holoOASIS.OnHoloOASISError += HoloOASIS_OnHoloOASISError;
                                holoOASIS.StorageProviderError += HoloOASIS_StorageProviderError;
                                ProviderManager.RegisterProvider(holoOASIS);
                            }
                            break;

                        case ProviderType.SQLLiteDBOASIS:
                            {
                                SQLLiteDBOASIS SQLLiteDBOASIS = new SQLLiteDBOASIS(OASISSettings.StorageProviders.SQLLiteDBOASIS.ConnectionString);
                                SQLLiteDBOASIS.StorageProviderError += SQLLiteDBOASIS_StorageProviderError;
                                ProviderManager.RegisterProvider(SQLLiteDBOASIS);

                            }
                            break;

                        case ProviderType.MongoDBOASIS:
                            {
                                MongoDBOASIS mongoOASIS = new MongoDBOASIS(OASISSettings.StorageProviders.MongoDBOASIS.ConnectionString, OASISSettings.StorageProviders.MongoDBOASIS.DBName);
                                mongoOASIS.StorageProviderError += MongoOASIS_StorageProviderError;
                                ProviderManager.RegisterProvider(mongoOASIS);

                            }
                            break;

                        case ProviderType.StarknetOASIS:
                            {
                                var starknetSettings = OASISSettings.StorageProviders.StarknetOASIS ?? new StarknetOASISProviderSettings
                                {
                                    Network = "alpha-goerli",
                                    RpcUrl = "https://alpha4.starknet.io"
                                };
                                var starknetOASIS = new StarknetOASIS(starknetSettings.Network, starknetSettings.RpcUrl);
                                starknetOASIS.StorageProviderError += StarknetOASIS_StorageProviderError;
                                ProviderManager.RegisterProvider(starknetOASIS);
                            }
                            break;

                        case ProviderType.EOSOASIS:
                            {
                                EOSIOOASIS EOSIOOASIS = new EOSIOOASIS();
                                EOSIOOASIS.StorageProviderError += EOSIOOASIS_StorageProviderError;
                                ProviderManager.RegisterProvider(EOSIOOASIS); //TODO: Need to pass connection string in.
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

        private static void StarknetOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            //  throw new Exception(string.Concat("ERROR: StarknetOASIS_StorageProviderError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }
    }
}
