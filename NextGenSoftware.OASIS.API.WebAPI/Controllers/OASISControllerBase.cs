
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop;
using NextGenSoftware.OASIS.API.Providers.MongoOASIS;
using System;

namespace NextGenSoftware.OASIS.API.WebAPI.Controllers
{
    //[Route("api/[controller]")]
    //[ApiController]
    public class OASISControllerBase : ControllerBase
    {
        protected readonly IOptions<OASISSettings> OASISSettings;

        public OASISControllerBase(IOptions<OASISSettings> OASISSettings)
        {
            this.OASISSettings = OASISSettings;
        }

        protected IOASISStorage GetAndActivateProvider()
        {
            return GetAndActivateProvider((ProviderType)Enum.Parse(typeof(ProviderType), OASISSettings.Value.StorageProviders.DefaultProvider));
        }

        public static IOASISStorage GetAndActivateProvider(ProviderType providerType)
        {
            //GetAndActivateProvider()
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
                                HoloOASIS holoOASIS = new HoloOASIS(OASISSettings.Value.StorageProviders.HoloOASIS.ConnectionString);
                                holoOASIS.OnHoloOASISError += HoloOASIS_OnHoloOASISError;
                                holoOASIS.StorageProviderError += HoloOASIS_StorageProviderError;

                                ProviderManager.RegisterProvider(holoOASIS);
                                
                            }
                            break;

                        case ProviderType.MongoDBOASIS:
                            {
                                MongoOASIS mongoOASIS = new MongoOASIS(OASISSettings.Value.StorageProviders.MongoOASIS.ConnectionString, OASISSettings.Value.StorageProviders.MongoOASIS.DBName));
                                mongoOASIS.StorageProviderError += MongoOASIS_StorageProviderError;
                                ProviderManager.RegisterProvider(new MongoOASIS(OASISSettings.Value.StorageProviders.MongoOASIS.ConnectionString, OASISSettings.Value.StorageProviders.MongoOASIS.DBName));

                            }
                            break;

                        case ProviderType.EOSOASIS:
                            ProviderManager.RegisterProvider(new EOSIOOASIS()); //TODO: Need to pass connection string in.
                            break;
                    }
                }

                ProviderManager.SwitchCurrentStorageProvider(providerType);
            }

            return ProviderManager.CurrentStorageProvider;
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
