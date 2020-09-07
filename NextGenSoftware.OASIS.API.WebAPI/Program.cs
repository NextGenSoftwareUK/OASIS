using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop;
using NextGenSoftware.OASIS.API.Providers.MongoOASIS;

namespace NextGenSoftware.OASIS.API.ORIAServices
{
    public class Program
    {
        public static AvatarManager AvatarManager
        {
            get
            {
                // TODO: This code is only needed because we have a singelton pattern for AvatarManager (only 1 instance instanitated through a static property).
                // Normally code is simpler if you just pass the provider into the manager constructor like SearchManager does, it is just one instance that is disposed of again once the request has been serviced...
                if (ProviderManager.CurrentStorageProvider == null)
                {
                    //HoloOASIS holoOASISProvider = new HoloOASIS("ws://localhost:8888");
                    MongoOASIS mongoOASISProvider = new MongoOASIS("mongodb+srv://dbadmin:PlRuNP9u4rG2nRdN@oasisapi-oipck.mongodb.net/OASISAPI?retryWrites=true&w=majority", "OASISAPI");
                    
                    ProviderManager.SwitchCurrentStorageProvider(mongoOASISProvider);

                    //  holoOASISProvider.OnHoloOASISError += HoloOASISProvider_OnHoloOASISError;
                    mongoOASISProvider.StorageProviderError += MongoOASISProvider_StorageProviderError;
                    AvatarManager.Instance.OnOASISManagerError += AvatarManager_OnOASISManagerError;
                }
                                                                                                       
                return AvatarManager.Instance;
            }
        }

        private static void MongoOASISProvider_StorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
           
        }

        private static void AvatarManager_OnOASISManagerError(object sender, OASISErrorEventArgs e)
        {

        }

        private static void HoloOASISProvider_OnHoloOASISError(object sender, Providers.HoloOASIS.Core.HoloOASISErrorEventArgs e)
        {
            
        }

        //private static void AvatarManager_OnAvatarManagerError(object sender, AvatarManagerErrorEventArgs e)
        //{
           
        //}

        public static void Main(string[] args)
        {

            Guid temp = Guid.NewGuid();
            Guid temp2 = Guid.NewGuid();

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

//namespace NextGenSoftware.OASIS.API.ORIAServices
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
