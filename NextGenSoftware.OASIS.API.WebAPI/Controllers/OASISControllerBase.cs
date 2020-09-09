
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using System;

namespace NextGenSoftware.OASIS.API.WebAPI.Controllers
{
    // TODO: Not sure if should move this into OASIS.API.Core.ProviderManager? But then Core will have refs to all the providers and 
    // the providers already have a ref to Core so then you will get circular refs so maybe not a good idea?

    //[Route("api/[controller]")]
    //[ApiController]
    public class OASISControllerBase : ControllerBase
    {
        public static IOptions<OASISSettings> _OASISSettings;
        private static OASISControllerBase _instance;

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
                                ProviderManager.RegisterProvider(new MongoDBOASIS(_OASISSettings.Value.StorageProviders.MongoDBOASIS.ConnectionString, _OASISSettings.Value.StorageProviders.MongoDBOASIS.DBName));

                            }
                            break;

                        case ProviderType.EOSOASIS:
                            {
                                EOSIOOASIS EOSIOOASIS = new EOSIOOASIS();
                                EOSIOOASIS.StorageProviderError += EOSIOOASIS_StorageProviderError;
                                ProviderManager.RegisterProvider(new EOSIOOASIS()); //TODO: Need to pass connection string in.
                            }

                            break;
                    }
                }

                ProviderManager.SetAndActivateCurrentStorageProvider(providerType);
            }

            return ProviderManager.CurrentStorageProvider;
        }

        private void EOSIOOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            
        }

        private void MongoOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {

        }

        private void HoloOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {

        }

        private void HoloOASIS_OnHoloOASISError(object sender, Providers.HoloOASIS.Core.HoloOASISErrorEventArgs e)
        {
          
        }
    }
}
