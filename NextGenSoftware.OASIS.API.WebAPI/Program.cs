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

namespace NextGenSoftware.OASIS.API.ORIAServices
{
    public class Program
    {
        public static AvatarManager AvatarManager
        {
            get
            {
                //if (AvatarManager.Instance.CurrentOASISStorageProvider == null)
                if (ProviderManager.CurrentStorageProvider == null)
                {
                    HoloOASIS holoOASISProvider = new HoloOASIS("ws://localhost:8888");
                    ProviderManager.SwitchCurrentStorageProvider(holoOASISProvider); //Default to HoloOASIS Provider.

                    holoOASISProvider.OnHoloOASISError += HoloOASISProvider_OnHoloOASISError;
                    AvatarManager.OnOASISManagerError += AvatarManager_OnOASISManagerError;

                    //AvatarManager.OnAvatarManagerError += AvatarManager_OnAvatarManagerError;

                }
                                                                                                          //AvatarManager.Instance.SetOASISStorageProvider(new HoloOASIS("ws://localhost:8888")); //Default to HoloOASIS Provider.

                return AvatarManager.Instance;
            }
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
