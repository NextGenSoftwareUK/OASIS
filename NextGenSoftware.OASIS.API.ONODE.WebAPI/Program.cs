
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.Config;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Managers;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class Program
    {
        public static bool IsDevEnviroment;
        public static string LIVE_OASISAPI = "http://oasisplatform.world/api";
        public static string DEV_OASISAPI = "http://localhost:5000/api";
        
        public static string CURRENT_OASISAPI
        {
            get
            {
                if (IsDevEnviroment)
                    return DEV_OASISAPI;
                else
                    return LIVE_OASISAPI;
            }
        }

        private static AvatarManager _avatarManager;
        //public static ProviderType CurrentStorageProviderType = ProviderType.Default;

        /*
        private static MongoOASIS _mongoOASISProvider = null;
        private static HoloOASIS _holoOASISProvider = null;
        
        public static string HoloOASISConnectionString = "";
        public static string MongoOASISConnectionString = "";
        public static string MongoOASISDBName = "";
        public static ProviderType DefaultStorageProviderType = ProviderType.MongoDBOASIS;

        public static MongoOASIS MongoOASISProvider
        {
            get
            {
                if (_mongoOASISProvider == null)
                {


                    _mongoOASISProvider = new MongoOASIS(MongoOASISConnectionString, MongoOASISDBName);
                    _mongoOASISProvider.StorageProviderError += MongoOASISProvider_StorageProviderError;
                }

                return _mongoOASISProvider;
            }
        }

        public static HoloOASIS HoloOASISProvider
        {
            get
            {
                if (_holoOASISProvider == null)
                {
                    _holoOASISProvider = new HoloOASIS(HoloOASISConnectionString);
                    _holoOASISProvider.OnHoloOASISError += _holoOASISProvider_OnHoloOASISError;
                    _holoOASISProvider.StorageProviderError += _holoOASISProvider_StorageProviderError;
                }

                return _holoOASISProvider;
            }
        }*/

        /*
        private static void _holoOASISProvider_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            
        }

        private static void _holoOASISProvider_OnHoloOASISError(object sender, Providers.HoloOASIS.Core.HoloOASISErrorEventArgs e)
        {
            
        }*/


        public static AvatarManager AvatarManager
        {
            get
            {
                // TODO: This code is only needed because we have a singelton pattern for AvatarManager (only 1 instance instanitated through a static property).
                // Normally code is simpler if you just pass the provider into the manager constructor like SearchManager does, it is just one instance that is disposed of again once the request has been serviced...
                //if (ProviderManager.CurrentStorageProvider == null)

                //if (AvatarManager.Instance == null || ProviderManager.CurrentStorageProvider == null)
                if (_avatarManager == null)
                {
                    //_avatarManager = new AvatarManager(OASISControllerBase.GetAndActivateProviderStatic(CurrentStorageProviderType));
                    //_avatarManager = new AvatarManager();

                    _avatarManager = new AvatarManager(OASISDNAManager.GetAndActivateProvider());

                    /*
                    if (ProviderManager.CurrentStorageProvider == null)
                        _avatarManager = new AvatarManager(OASISProviderManager.GetAndActivateProvider());
                    else
                        _avatarManager = new AvatarManager(ProviderManager.CurrentStorageProvider);
                    */

                    //ProviderManager.OverrideProviderType = false; //TODO: Check if this is still needed?

                    _avatarManager.OnOASISManagerError += _avatarManager_OnOASISManagerError;

                    //ProviderManager.SwitchCurrentStorageProvider(MongoOASISProvider);
                    // ProviderManager.SwitchCurrentStorageProvider(DefaultStorageProviderType);

                    //HoloOASIS holoOASISProvider = new HoloOASIS("ws://localhost:8888");
                    //MongoOASIS mongoOASISProvider = new MongoOASIS("mongodb+srv://dbadmin:PlRuNP9u4rG2nRdN@oasisapi-oipck.mongodb.net/OASISAPI?retryWrites=true&w=majority", "OASISAPI");

                    //ProviderManager.SwitchCurrentStorageProvider(mongoOASISProvider);

                    //  holoOASISProvider.OnHoloOASISError += HoloOASISProvider_OnHoloOASISError;
                    //mongoOASISProvider.StorageProviderError += MongoOASISProvider_StorageProviderError;
                    // AvatarManager.Instance.OnOASISManagerError += AvatarManager_OnOASISManagerError;
                }
                else
                {

                }

                return _avatarManager;
                // return AvatarManager.Instance;
            }
        }

        //private static void AvatarManager_OnOASISManagerError(object sender, OASISErrorEventArgs e)
        //{
        //    throw new Exception(string.Concat("ERROR: AvatarManager_OnOASISManagerError. Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails);
        //}

        private static void _avatarManager_OnOASISManagerError(object sender, OASISErrorEventArgs e)
        {
            throw new Exception(string.Concat("ERROR: AvatarManager_OnOASISManagerError. Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
        }

        /*
        private static void MongoOASISProvider_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
           
        }

        private static void AvatarManager_OnOASISManagerError(object sender, OASISErrorEventArgs e)
        {

        }

        private static void HoloOASISProvider_OnHoloOASISError(object sender, Providers.HoloOASIS.Core.HoloOASISErrorEventArgs e)
        {
            
        }
        */

        private static void AvatarManager_OnAvatarManagerError(object sender, AvatarManagerErrorEventArgs e)
        {

        }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}




//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using Microsoft.AspNetCore;
//using Microsoft.AspNetCore.Hosting;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
//{
//    public class Program
//    {
//        public static void Main(string[] args)
//        {
//            CreateWebHostBuilder(args).Build().Run();
//        }

//        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
//            WebHost.CreateDefaultBuilder(args)
//                .UseStartup<Startup>();
//    }
//}
