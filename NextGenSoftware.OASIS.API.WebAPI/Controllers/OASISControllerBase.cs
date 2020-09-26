
using System;
using System.Diagnostics.Eventing.Reader;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.EOSIOOASIS;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    // TODO: Not sure if should move this into OASIS.API.Core.ProviderManager? But then Core will have refs to all the providers and 
    // the providers already have a ref to Core so then you will get circular refs so maybe not a good idea?

    //[Route("api/[controller]")]
    //[ApiController]
    public class OASISControllerBase : ControllerBase
    {
        public IOptions<OASISSettings> OASISSettings;
        public static IOptions<OASISSettings> OASISSettingsStatic;
        private static OASISControllerBase _instance;
        //public static ProviderType CurrentStorageProviderType = ProviderType.Default;

        // returns the current authenticated account (null if not logged in)
        //public IAvatar Avatar => (IAvatar)HttpContext.Items["Avatar"];

        public IAvatar Avatar
        {
            get
            {
                if (HttpContext.Items.ContainsKey("Avatar") && HttpContext.Items["Avatar"] != null)
                    return (IAvatar)HttpContext.Items["Avatar"];

                if (HttpContext.Session.GetString("Avatar") != null)
                    return JsonSerializer.Deserialize<IAvatar>(HttpContext.Session.GetString("Avatar"));

                return null;
            }
            set
            {
                HttpContext.Items["Avatar"] = value;
                HttpContext.Session.SetString("Avatar", JsonSerializer.Serialize(value));
            }
        }

        public static OASISControllerBase Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new OASISControllerBase(OASISSettingsStatic);

                return _instance;
            }
        }

        public OASISControllerBase(IOptions<OASISSettings> settings)
        {
            OASISSettings = settings;
            OASISSettingsStatic = settings;
        }

        public static IOASISStorage GetAndActivateProviderStatic()
        {
            ProviderManager.DefaultProviderTypes = Instance.OASISSettings.Value.StorageProviders.DefaultProviders.Split(",");

            //TODO: Need to add additional logic later for when the first provider and others fail or are too laggy and so need to switch to a faster provider, etc...
            return Instance.GetAndActivateProvider((ProviderType)Enum.Parse(typeof(ProviderType), ProviderManager.DefaultProviderTypes[0]));
        }

        public static IOASISStorage GetAndActivateProviderStatic(ProviderType providerType)
        {
            return Instance.GetAndActivateProvider(providerType);
        }

        protected IOASISStorage GetAndActivateProvider()
        {
            ProviderManager.DefaultProviderTypes = OASISSettings.Value.StorageProviders.DefaultProviders.Split(",");

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
                                HoloOASIS holoOASIS = new HoloOASIS(OASISSettings.Value.StorageProviders.HoloOASIS.ConnectionString);
                                holoOASIS.OnHoloOASISError += HoloOASIS_OnHoloOASISError;
                                holoOASIS.StorageProviderError += HoloOASIS_StorageProviderError;
                                ProviderManager.RegisterProvider(holoOASIS);
                            }
                            break;

                        case ProviderType.SQLLiteDBOASIS:
                        {
                                SQLLiteDBOASIS SQLLiteDBOASIS = new SQLLiteDBOASIS(OASISSettings.Value.StorageProviders.SQLLiteDBOASIS.ConnectionString);
                                SQLLiteDBOASIS.StorageProviderError += SQLLiteDBOASIS_StorageProviderError;
                                ProviderManager.RegisterProvider(SQLLiteDBOASIS);

                        }
                        break;

                        case ProviderType.MongoDBOASIS:
                            {
                                MongoDBOASIS mongoOASIS = new MongoDBOASIS(OASISSettings.Value.StorageProviders.MongoDBOASIS.ConnectionString, OASISSettings.Value.StorageProviders.MongoDBOASIS.DBName);
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

        private void SQLLiteDBOASIS_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
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
