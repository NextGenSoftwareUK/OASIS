using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("NextGenSoftware.OASIS.API.Core Test Harness v1.0");
            Console.WriteLine("");

            OASISAPIManager OASISAPIManager = new OASISAPIManager(new List<IOASISProvider> { new HoloOASIS("ws://localhost:8888") });
            
            //AvatarManager AvatarManager = new AvatarManager(new HoloOASIS("ws://localhost:8888"));
            //AvatarManager.OnAvatarManagerError += AvatarManager_OnAvatarManagerError;
            //AvatarManager.OASISStorageProvider.OnStorageProviderError += OASISStorageProvider_OnStorageProviderError;



            Console.WriteLine("\nSaving Avatar...");
            Avatar newAvatar = new Avatar { Username = "dellams", Email = "david@nextgensoftware.co.uk", Password = "1234", FirstName = "David", LastName = "Ellams", DOB = "11/04/1980", Id = Guid.NewGuid(), Title = "Mr", PlayerAddress = "blahahahaha" };
            
            
            await newAvatar.KarmaEarnt(KarmaTypePositive.HelpingTheEnvironment, KarmaSourceType.hApp, "Our World", "XR Educational Game To Make The World A Better Place");
            Avatar savedAvatar = (Avatar)await OASISAPIManager.AvatarManager.SaveAvatarAsync(newAvatar);
            //IAvatar savedAvatar = await AvatarManager.SaveAvatarAsync(newAvatar);

            if (savedAvatar != null)
            {
                Console.WriteLine("Avatar Saved.\n");
                Console.WriteLine(string.Concat("Id: ", savedAvatar.Id));
                Console.WriteLine(string.Concat("Provider Key: ", savedAvatar.ProviderKey));
               // Console.WriteLine(string.Concat("HC Address Hash: ", savedAvatar.HcAddressHash)); //But we can still view the HC Hash if we wish by casting to the provider Avatar object as we have above. - UPDATE: We do not need this, the ProviderKey shows the same info (hash in this case).
                Console.WriteLine(string.Concat("Name: ", savedAvatar.Title, " ", savedAvatar.FirstName, " ", savedAvatar.LastName));
                Console.WriteLine(string.Concat("Username: ", savedAvatar.Username));
                Console.WriteLine(string.Concat("Password: ", savedAvatar.Password));
                Console.WriteLine(string.Concat("Email: ", savedAvatar.Email));
                Console.WriteLine(string.Concat("DOB: ", savedAvatar.DOB));
                Console.WriteLine(string.Concat("Address: ", savedAvatar.PlayerAddress));
                Console.WriteLine(string.Concat("Karma: ", savedAvatar.Karma));
                Console.WriteLine(string.Concat("Level: ", savedAvatar.Level));
            }

            Console.WriteLine("\nLoading Avatar...");
            //IAvatar Avatar = await AvatarManager.LoadAvatarAsync("dellams", "1234");
            IAvatar Avatar = await OASISAPIManager.AvatarManager.LoadAvatarAsync("QmR6A1gkSmCsxnbDF7V9Eswnd4Kw9SWhuf8r4R643eDshg");

            if (Avatar != null)
            {
                Console.WriteLine("Avatar Loaded.\n");
                Console.WriteLine(string.Concat("Id: ", Avatar.Id));
                Console.WriteLine(string.Concat("Provider Key: ", savedAvatar.ProviderKey));
                //Console.WriteLine(string.Concat("HC Address Hash: ", Avatar.HcAddressHash)); //AvatarManager is independent of provider implementation so it should not know about HC Hash.
                Console.WriteLine(string.Concat("Name: ", Avatar.Title, " ", Avatar.FirstName, " ", Avatar.LastName));
                Console.WriteLine(string.Concat("Username: ", Avatar.Username));
                Console.WriteLine(string.Concat("Password: ", Avatar.Password));
                Console.WriteLine(string.Concat("Email: ", Avatar.Email));
                Console.WriteLine(string.Concat("DOB: ", Avatar.DOB));
                Console.WriteLine(string.Concat("Address: ", Avatar.PlayerAddress));
                Console.WriteLine(string.Concat("Karma: ", Avatar.Karma));
                Console.WriteLine(string.Concat("Level: ", Avatar.Level));
            }

            

            Console.ReadKey();
        }

        private static void AvatarManager_OnAvatarManagerError(object sender, AvatarManagerErrorEventArgs e)
        {
            Console.WriteLine(string.Concat("\nAvatarManager Error. EndPoint: ", e.EndPoint, ", Reason: ", e.Reason, ", Error Details: ", e.ErrorDetails.ToString()));
        }

        private static void OASISStorageProvider_OnStorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            Console.WriteLine(string.Concat("\nOASIS Storage Provider Error. EndPoint: ", e.EndPoint, ", Reason: ", e.Reason, ", Error Details: ", e.ErrorDetails.ToString()));
        }
    }
}
