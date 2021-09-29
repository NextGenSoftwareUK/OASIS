//using System;
//using System.Threading.Tasks;
//using NextGenSoftware.OASIS.API.Manager;
//using NextGenSoftware.OASIS.API.Core.Events;
//using NextGenSoftware.OASIS.API.Core.Interfaces;
//using NextGenSoftware.OASIS.API.Core.Holons;
//using NextGenSoftware.OASIS.API.Core.Enums;

//namespace NextGenSoftware.OASIS.API.Core.TestHarness
//{
//    class Program
//    {
//        static async Task Main(string[] args)
//        {
//            Console.WriteLine("NextGenSoftware.OASIS.API.Core Test Harness v1.1");
//            Console.WriteLine("");

//            //By default it will load the settings from OASIS_DNA.json in the current working dir but you can override using below:
//            //OASISAPI.Initialize("OASIS_DNA_Override.json");
//            OASISAPI.Initialize();

//            //Init with the Holochain Provider.
//            OASISBootLoader.OASISBootLoader.GetAndActivateProvider(ProviderType.HoloOASIS, null, false, true);
//            //ProviderManager.ActivateProvider(ProviderType.HoloOASIS); // Can also do it this way.

//            //OASISAPI.Init(new List<IOASISProvider> { new HoloOASIS("ws://localhost:8888", Holochain.HoloNET.Client.Core.HolochainVersion.Redux) }, OASISConfigManager.OASISDNA);
//            //OASISAPI.Init(InitOptions.InitWithAllProviders, OASISConfigManager.OASISDNA);
            
//            //AvatarManager AvatarManager = new AvatarManager(new HoloOASIS("ws://localhost:8888"));
//            //AvatarManager.OnAvatarManagerError += AvatarManager_OnAvatarManagerError;
//            //AvatarManager.OASISStorageProvider.OnStorageProviderError += OASISStorageProvider_OnStorageProviderError;



//            Console.WriteLine("\nSaving Avatar...");
//            Avatar newAvatar = new Avatar { Username = "dellams", Email = "david@nextgensoftware.co.uk", Password = "1234", FirstName = "David", LastName = "Ellams", Id = Guid.NewGuid(), Title = "Mr" };
            
            
//<<<<<<< Updated upstream
//         //   await newAvatar.KarmaEarntAsync(KarmaTypePositive.HelpingTheEnvironment, KarmaSourceType.hApp, "Our World", "XR Educational Game To Make The World A Better Place");
//=======
//>>>>>>> Stashed changes
//            Avatar savedAvatar = (Avatar)await OASISAPI.Avatar.SaveAvatarAsync(newAvatar);
//            //IAvatar savedAvatar = await AvatarManager.SaveAvatarAsync(newAvatar);

//            if (savedAvatar != null)
//            {
//                Console.WriteLine("Avatar Saved.\n");
//                Console.WriteLine(string.Concat("Id: ", savedAvatar.Id));
//                Console.WriteLine(string.Concat("Provider Key: ", savedAvatar.ProviderKey));
//               // Console.WriteLine(string.Concat("HC Address Hash: ", savedAvatar.HcAddressHash)); //But we can still view the HC Hash if we wish by casting to the provider Avatar object as we have above. - UPDATE: We do not need this, the ProviderKey shows the same info (hash in this case).
//                Console.WriteLine(string.Concat("Name: ", savedAvatar.Title, " ", savedAvatar.FirstName, " ", savedAvatar.LastName));
//                Console.WriteLine(string.Concat("Username: ", savedAvatar.Username));
//                Console.WriteLine(string.Concat("Password: ", savedAvatar.Password));
//                Console.WriteLine(string.Concat("Email: ", savedAvatar.Email));
//<<<<<<< Updated upstream
//               // Console.WriteLine(string.Concat("DOB: ", savedAvatar.DOB));
//                //Console.WriteLine(string.Concat("Address: ", savedAvatar.Address));
//                Console.WriteLine(string.Concat("Karma: ", savedAvatar.Karma));
//                Console.WriteLine(string.Concat("Level: ", savedAvatar.Level));
//=======
//>>>>>>> Stashed changes
//            }

//            Console.WriteLine("\nLoading Avatar...");
//            //IAvatar Avatar = await AvatarManager.LoadAvatarAsync("dellams", "1234");
//            IAvatar Avatar = await OASISAPI.Avatar.LoadAvatarAsync("QmR6A1gkSmCsxnbDF7V9Eswnd4Kw9SWhuf8r4R643eDshg");

//            if (Avatar != null)
//            {
//                Console.WriteLine("Avatar Loaded.\n");
//                Console.WriteLine(string.Concat("Id: ", Avatar.Id));
//                Console.WriteLine(string.Concat("Provider Key: ", savedAvatar.ProviderKey));
//                //Console.WriteLine(string.Concat("HC Address Hash: ", Avatar.HcAddressHash)); //AvatarManager is independent of provider implementation so it should not know about HC Hash.
//                Console.WriteLine(string.Concat("Name: ", Avatar.Title, " ", Avatar.FirstName, " ", Avatar.LastName));
//                Console.WriteLine(string.Concat("Username: ", Avatar.Username));
//                Console.WriteLine(string.Concat("Password: ", Avatar.Password));
//                Console.WriteLine(string.Concat("Email: ", Avatar.Email));
//<<<<<<< Updated upstream
//              //  Console.WriteLine(string.Concat("DOB: ", Avatar.DOB));
//              //  Console.WriteLine(string.Concat("Address: ", Avatar.Address));
//                Console.WriteLine(string.Concat("Karma: ", Avatar.Karma));
//                Console.WriteLine(string.Concat("Level: ", Avatar.Level));
//=======
//>>>>>>> Stashed changes
//            }

            

//            Console.ReadKey();
//        }

//        private static void AvatarManager_OnAvatarManagerError(object sender, AvatarManagerErrorEventArgs e)
//        {
//            Console.WriteLine(string.Concat("\nAvatarManager Error. EndPoint: ", e.EndPoint, ", Reason: ", e.Reason, ", Error Details: ", e.ErrorDetails.ToString()));
//        }

//        private static void OASISStorageProvider_OnStorageProviderError(object sender, AvatarManagerErrorEventArgs e)
//        {
//            Console.WriteLine(string.Concat("\nOASIS Storage Provider Error. EndPoint: ", e.EndPoint, ", Reason: ", e.Reason, ", Error Details: ", e.ErrorDetails.ToString()));
//        }
//    }
//}
