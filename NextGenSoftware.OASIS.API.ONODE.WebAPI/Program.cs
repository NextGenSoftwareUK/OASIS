﻿using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using NextGenSoftware.OASIS.API.DNA.Manager;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class Program
    {
        private static AvatarManager _avatarManager;

        public static bool IsDevEnviroment;
        public static string LIVE_OASISAPI = "https://api.oasisplatform.world/api";
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

        public static AvatarManager AvatarManager
        {
            get
            {
                // TODO: This code is only needed because we have a singelton pattern for AvatarManager (only 1 instance instanitated through a static property).
                // Normally code is simpler if you just pass the provider into the manager constructor like SearchManager does, it is just one instance that is disposed of again once the request has been serviced...
                if (_avatarManager == null)
                {
                    OASISResult<IOASISStorage> result = OASISDNAManager.GetAndActivateDefaultProvider();

                    //TODO: Eventually want to replace all exceptions with OASISResult throughout the OASIS because then it makes sure errors are handled properly and friendly messages are shown (plus less overhead of throwing an entire stack trace!)
                    if (result.IsError)
                        ErrorHandling.HandleError(ref result, string.Concat("Error calling OASISDNAManager.GetAndActivateDefaultProvider(). Error details: ", result.Message), true, false, true);

                    _avatarManager = new AvatarManager(result.Result, OASISDNAManager.OASISDNA);
                    _avatarManager.OnOASISManagerError += _avatarManager_OnOASISManagerError;
                }

                return _avatarManager;
            }
        }

        private static void _avatarManager_OnOASISManagerError(object sender, OASISErrorEventArgs e)
        {
            throw new Exception(string.Concat("ERROR: AvatarManager_OnOASISManagerError. Reason: ", e.Reason, ". Error Details: ", e.ErrorDetails));
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
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}