using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class Program
    {
        private static AvatarManager _avatarManager;
        private static SettingsManager _settingsManager;
        private static StatsManager _statsManager;

        //public static bool IsDevEnviroment = false;
        //public static string LIVE_OASISAPI = "https://oasisweb4.one"; //"https://api.oasisweb4.one/api";
        //public static string DEV_OASISAPI = "https://localhost:3000"; //"http://localhost:5000/api";

        //public static string CURRENT_OASISAPI
        //{
        //    get
        //    {
        //        if (IsDevEnviroment)
        //            return DEV_OASISAPI;
        //        else
        //            return LIVE_OASISAPI;
        //    }
        //}

        //TODO: MOVE THE CODE OUT OF HERE ASAP!
        public static AvatarManager AvatarManager
        {
            get
            {
                // TODO: This code is only needed because we have a singelton pattern for AvatarManager (only 1 instance instanitated through a static property).
                // Normally code is simpler if you just pass the provider into the manager constructor like SearchManager does, it is just one instance that is disposed of again once the request has been serviced...
                if (_avatarManager == null)
                {
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;

                    //TODO: Eventually want to replace all exceptions with OASISResult throughout the OASIS because then it makes sure errors are handled properly and friendly messages are shown (plus less overhead of throwing an entire stack trace!)
                    if (result.IsError)
                        OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISDNAManager.GetAndActivateDefaultStorageProvider(). Error details: ", result.Message));

                    _avatarManager = new AvatarManager(result.Result, OASISBootLoader.OASISBootLoader.OASISDNA);
                    _avatarManager.OnOASISManagerError += _avatarManager_OnOASISManagerError;
                }

                return _avatarManager;
            }
        }

        public static SettingsManager SettingsManager
        {
            get
            {
                if (_settingsManager == null)
                {
                    _settingsManager = SettingsManager.Instance;
                }
                return _settingsManager;
            }
        }

        public static StatsManager StatsManager
        {
            get
            {
                if (_statsManager == null)
                {
                    _statsManager = StatsManager.Instance;
                }
                return _statsManager;
            }
        }

        private static void _avatarManager_OnOASISManagerError(object sender, OASISErrorEventArgs e)
        {
            throw new Exception(string.Concat("ERROR: AvatarManager_OnOASISManagerError. Reason: ", e.Reason, ". Error Details: ", e.Reason));
        }

        private static void AvatarManager_OnAvatarManagerError(object sender, AvatarManagerErrorEventArgs e)
        {

        }

        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    // Load OASIS_DNA.json so SubscriptionStore can fall back to MongoDBOASIS connection
                    config.AddJsonFile("OASIS_DNA.json", optional: true, reloadOnChange: false);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}