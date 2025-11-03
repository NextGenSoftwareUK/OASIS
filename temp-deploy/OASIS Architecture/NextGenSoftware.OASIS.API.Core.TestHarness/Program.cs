using System;
using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Manager;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.CLI.Engine;

namespace NextGenSoftware.OASIS.API.Core.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("NextGenSoftware.OASIS.API.Core Test Harness v1.1");
            Console.WriteLine("");

            Console.WriteLine($"For Karma Score 1 I would be Level {LevelManager.GetLevelFromKarma(1)}");
            Console.WriteLine($"For Karma Score 777 I would be Level {LevelManager.GetLevelFromKarma(777)}");
            Console.WriteLine($"For Karma Score 2222 I would be Level {LevelManager.GetLevelFromKarma(2222)}");
            Console.WriteLine($"For Karma Score 8888888 I would be Level {LevelManager.GetLevelFromKarma(8888888)}");
            Console.WriteLine($"For Karma Score 1004100756606 I would be Level {LevelManager.GetLevelFromKarma(1004100756606)}");
            Console.WriteLine($"For Karma Score 1004100756607 I would be Level {LevelManager.GetLevelFromKarma(1004100756607)}");
            Console.WriteLine($"For Karma Score 1004100756608 I would be Level {LevelManager.GetLevelFromKarma(1004100756608)}");
            Console.WriteLine($"For Karma Score 1255125945857 I would be Level {LevelManager.GetLevelFromKarma(1255125945857)}");
            Console.WriteLine($"For Karma Score 1255125945858 I would be Level {LevelManager.GetLevelFromKarma(1255125945858)}");
            Console.WriteLine($"For Karma Score 1255125945859 I would be Level {LevelManager.GetLevelFromKarma(1255125945859)}");
            Console.WriteLine($"For Karma Score 777777777777777777 I would be Level {LevelManager.GetLevelFromKarma(777777777777777777)}");
            Console.WriteLine($"For Karma Score 8888888888888888888888888888 I would be Level {LevelManager.GetLevelFromKarma(999999999999999999)}"); //Max supported karma is 9 Quintillion but current max level is 99 (1255125945858 karma, around 1.2 trillion)

            //By default it will load the settings from OASIS_DNA.json in the current working dir but you can override using below:
            //OASISAPI.Initialize("OASIS_DNA_Override.json");
            //OASISAPI.BootOASIS();

            OASISAPI OASISAPI = new OASISAPI();
            await OASISAPI.BootOASISAsync();

            TestHolon testHolon = new TestHolon();
            testHolon.Description = "test!";
            OASISResult<IHolon> saveResult = await testHolon.SaveAsync();

            if (saveResult != null && saveResult.Result != null && !saveResult.IsError)
                CLIEngine.ShowSuccessMessage($"TestHolon saved successfully! Id: {saveResult.Result.Id}");
            else
                CLIEngine.ShowErrorMessage("Error saving TestHolon: " + saveResult.Message);

            return;

            //Init with the Holochain Provider.
            await OASISBootLoader.OASISBootLoader.GetAndActivateStorageProviderAsync(ProviderType.HoloOASIS, null, false, true);
            //ProviderManager.ActivateProvider(ProviderType.HoloOASIS); // Can also do it this way.

            //OASISAPI.Init(new List<IOASISProvider> { new HoloOASIS("ws://localhost:8888", Holochain.HoloNET.Client.Core.HolochainVersion.Redux) }, OASISConfigManager.OASISDNA);
            //OASISAPI.Init(InitOptions.InitWithAllProviders, OASISConfigManager.OASISDNA);

            //AvatarManager AvatarManager = new AvatarManager(new HoloOASIS("ws://localhost:8888"));
            //AvatarManager.OnAvatarManagerError += AvatarManager_OnAvatarManagerError;
            //AvatarManager.OASISStorageProvider.OnStorageProviderError += OASISStorageProvider_OnStorageProviderError;



            Console.WriteLine("\nSaving Avatar...");
            Avatar newAvatar = new Avatar { Username = "dellams", Email = "david@nextgensoftware.co.uk", Password = "1234", FirstName = "David", LastName = "Ellams", Id = Guid.NewGuid(), Title = "Mr" };


            //   await newAvatar.KarmaEarntAsync(KarmaTypePositive.HelpingTheEnvironment, KarmaSourceType.hApp, "Our World", "XR Educational Game To Make The World A Better Place");
            OASISResult<IAvatar> savedAvatar = await OASISAPI.Avatars.SaveAvatarAsync(newAvatar);
            //IAvatar savedAvatar = await AvatarManager.SaveAvatarAsync(newAvatar);

            if (!savedAvatar.IsError && savedAvatar.Result != null)
            {
                Console.WriteLine("Avatar Saved.\n");
                Console.WriteLine(string.Concat("Id: ", savedAvatar.Result.Id));
                Console.WriteLine(string.Concat("Provider Key: ", savedAvatar.Result.ProviderUniqueStorageKey));
                // Console.WriteLine(string.Concat("HC Address Hash: ", savedAvatar.HcAddressHash)); //But we can still view the HC Hash if we wish by casting to the provider Avatar object as we have above. - UPDATE: We do not need this, the ProviderUniqueStorageKey shows the same info (hash in this case).
                Console.WriteLine(string.Concat("Name: ", savedAvatar.Result.Title, " ", savedAvatar.Result.FirstName, " ", savedAvatar.Result.LastName));
                Console.WriteLine(string.Concat("Username: ", savedAvatar.Result.Username));
                Console.WriteLine(string.Concat("Password: ", savedAvatar.Result.Password));
                Console.WriteLine(string.Concat("Email: ", savedAvatar.Result.Email));
                // Console.WriteLine(string.Concat("DOB: ", savedAvatar.DOB));
                //Console.WriteLine(string.Concat("Address: ", savedAvatar.Address));
                //Console.WriteLine(string.Concat("Karma: ", savedAvatar.Karma));
                //Console.WriteLine(string.Concat("Level: ", savedAvatar.Level));
            }

            Console.WriteLine("\nLoading Avatar...");
            //IAvatar Avatar = await AvatarManager.LoadAvatarAsync("dellams", "1234");
            OASISResult<IAvatar> avatarResult = await OASISAPI.Avatars.LoadAvatarAsync("QmR6A1gkSmCsxnbDF7V9Eswnd4Kw9SWhuf8r4R643eDshg");

            if (!avatarResult.IsError && avatarResult.Result != null)
            {
                Console.WriteLine("Avatar Loaded.\n");
                Console.WriteLine(string.Concat("Id: ", avatarResult.Result.Id));
                Console.WriteLine(string.Concat("Provider Key: ", savedAvatar.Result.ProviderUniqueStorageKey));
                //Console.WriteLine(string.Concat("HC Address Hash: ", Avatar.HcAddressHash)); //AvatarManager is independent of provider implementation so it should not know about HC Hash.
                Console.WriteLine(string.Concat("Name: ", avatarResult.Result.Title, " ", avatarResult.Result.FirstName, " ", avatarResult.Result.LastName));
                Console.WriteLine(string.Concat("Username: ", avatarResult.Result.Username));
                Console.WriteLine(string.Concat("Password: ", avatarResult.Result.Password));
                Console.WriteLine(string.Concat("Email: ", avatarResult.Result.Email));
                //  Console.WriteLine(string.Concat("DOB: ", Avatar.DOB));
                //  Console.WriteLine(string.Concat("Address: ", Avatar.Address));
                //Console.WriteLine(string.Concat("Karma: ", avatarResult.Result.Karma));
                //Console.WriteLine(string.Concat("Level: ", avatarResult.Result.Level));
            }

            Console.ReadKey();
        }

        private static void AvatarManager_OnAvatarManagerError(object sender, AvatarManagerErrorEventArgs e)
        {
            Console.WriteLine(string.Concat("\nAvatarManager Error. EndPoint: ", e.EndPoint, ", Reason: ", e.Reason, ", Error Details: ", e.Reason.ToString()));
        }

        private static void OASISStorageProvider_OnStorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            Console.WriteLine(string.Concat("\nOASIS Storage Provider Error. EndPoint: ", e.EndPoint, ", Reason: ", e.Reason, ", Error Details: ", e.Reason.ToString()));
        }
    }
}
