
using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    // TODO: Not sure if should move this into OASIS.API.Core.ProviderManager? But then Core will have refs to all the providers and 
    // the providers already have a ref to Core so then you will get circular refs so maybe not a good idea?

    //[Route("api/[controller]")]
    //[ApiController]
    public class OASISControllerBase : ControllerBase
    {
        public static IOptions<OASISSettings> _OASISSettings;
        private static OASISControllerBase _instance;
        //public static ProviderType CurrentStorageProviderType = ProviderType.Default;

        public static OASISControllerBase Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new OASISControllerBase(_OASISSettings);

                return _instance;
            }
        }

        public OASISControllerBase(IOptions<OASISSettings> OASISSettings)
        {
            _OASISSettings = OASISSettings;
        }

        public static IOASISStorage GetAndActivateProviderStatic()
        {
            ProviderManager.DefaultProviderTypes = _OASISSettings.Value.StorageProviders.DefaultProviders.Split(",");

            //TODO: Need to add additional logic later for when the first provider and others fail or are too laggy and so need to switch to a faster provider, etc...
            return Instance.GetAndActivateProvider((ProviderType)Enum.Parse(typeof(ProviderType), ProviderManager.DefaultProviderTypes[0]));
        }

        public static IOASISStorage GetAndActivateProviderStatic(ProviderType providerType)
        {
            return Instance.GetAndActivateProvider(providerType);
        }

        protected IOASISStorage GetAndActivateProvider()
        {
            ProviderManager.DefaultProviderTypes = _OASISSettings.Value.StorageProviders.DefaultProviders.Split(",");

            //TODO: Need to add additional logic later for when the first provider and others fail or are too laggy and so need to switch to a faster provider, etc...
            return GetAndActivateProvider((ProviderType)Enum.Parse(typeof(ProviderType), ProviderManager.DefaultProviderTypes[0]));
        }

        protected IOASISStorage GetAndActivateProvider(ProviderType providerType)
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
                                HoloOASIS holoOASIS = new HoloOASIS(_OASISSettings.Value.StorageProviders.HoloOASIS.ConnectionString);
                                holoOASIS.OnHoloOASISError += HoloOASIS_OnHoloOASISError;
                                holoOASIS.StorageProviderError += HoloOASIS_StorageProviderError;
                                ProviderManager.RegisterProvider(holoOASIS);
                            }
                            break;

                        case ProviderType.MongoDBOASIS:
                            {
                                MongoDBOASIS mongoOASIS = new MongoDBOASIS(_OASISSettings.Value.StorageProviders.MongoDBOASIS.ConnectionString, _OASISSettings.Value.StorageProviders.MongoDBOASIS.DBName);
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
                    }
                }

                ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
            }

          //  CurrentStorageProviderType = providerType;
            return ProviderManager.CurrentStorageProvider;
        }

        private void EOSIOOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            // throw new Exception(string.Concat("ERROR: EOSIOOASIS_StorageProviderError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private void MongoOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            //  throw new Exception(string.Concat("ERROR: MongoOASIS_StorageProviderError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private void HoloOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            //  throw new Exception(string.Concat("ERROR: HoloOASIS_StorageProviderError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        private void HoloOASIS_OnHoloOASISError(object sender, Providers.HoloOASIS.Core.HoloOASISErrorEventArgs e)
        {
            //TODO: {URGENT} Handle Errors properly here (log, etc)
            //  throw new Exception(string.Concat("ERROR: HoloOASIS_OnHoloOASISError. EndPoint: ", e.EndPoint, "Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails, "HoloNET.Reason: ", e.HoloNETErrorDetails.Reason, "HoloNET.ErrorDetails: ", e.HoloNETErrorDetails.ErrorDetails));
        }
    }
}
