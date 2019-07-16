using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Desktop;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Core.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("NextGenSoftware.OASIS.API.Core Test Harness v1.0");
            Console.WriteLine("");
            
            ProfileManager profileManager = new ProfileManager(new HoloOASIS("ws://localhost:8888"));
            profileManager.OnProfileManagerError += ProfileManager_OnProfileManagerError;
            profileManager.OASISStorageProvider.OnStorageProviderError += OASISStorageProvider_OnStorageProviderError;



            Console.WriteLine("\nSaving Profile...");
            Profile newProfile = new Profile { Username = "dellams", Email = "david@nextgensoftware.co.uk", Password = "1234", FirstName = "David", LastName = "Ellams", DOB = "11/04/1980", Id = Guid.NewGuid(), Title = "Mr", PlayerAddress = "blahahahaha" };
            newProfile.AddKarma(999);
            Providers.HoloOASIS.Core.Profile savedProfile = (Providers.HoloOASIS.Core.Profile)await profileManager.SaveProfileAsync(newProfile);
            //IProfile savedProfile = await profileManager.SaveProfileAsync(newProfile);

            if (savedProfile != null)
            {
                Console.WriteLine("Profile Saved.\n");
                Console.WriteLine(string.Concat("Id: ", savedProfile.Id));
                Console.WriteLine(string.Concat("Provider Key: ", savedProfile.ProviderKey));
                Console.WriteLine(string.Concat("HC Address Hash: ", savedProfile.HcAddressHash)); //But we can still view the HC Hash if we wish by casting to the provider profile object as we have above.
                Console.WriteLine(string.Concat("Name: ", savedProfile.Title, " ", savedProfile.FirstName, " ", savedProfile.LastName));
                Console.WriteLine(string.Concat("Username: ", savedProfile.Username));
                Console.WriteLine(string.Concat("Password: ", savedProfile.Password));
                Console.WriteLine(string.Concat("Email: ", savedProfile.Email));
                Console.WriteLine(string.Concat("DOB: ", savedProfile.DOB));
                Console.WriteLine(string.Concat("Address: ", savedProfile.PlayerAddress));
                Console.WriteLine(string.Concat("Karma: ", savedProfile.Karma));
                Console.WriteLine(string.Concat("Level: ", savedProfile.Level));
            }

            Console.WriteLine("\nLoading Profile...");
            //IProfile profile = await profileManager.LoadProfileAsync("dellams", "1234");
            IProfile profile = await profileManager.LoadProfileAsync("QmR6A1gkSmCsxnbDF7V9Eswnd4Kw9SWhuf8r4R643eDshg");

            if (profile != null)
            {
                Console.WriteLine("Profile Loaded.\n");
                Console.WriteLine(string.Concat("Id: ", profile.Id));
                Console.WriteLine(string.Concat("Provider Key: ", savedProfile.ProviderKey));
                //Console.WriteLine(string.Concat("HC Address Hash: ", profile.HcAddressHash)); //ProfileManager is independent of provider implementation so it should not know about HC Hash.
                Console.WriteLine(string.Concat("Name: ", profile.Title, " ", profile.FirstName, " ", profile.LastName));
                Console.WriteLine(string.Concat("Username: ", profile.Username));
                Console.WriteLine(string.Concat("Password: ", profile.Password));
                Console.WriteLine(string.Concat("Email: ", profile.Email));
                Console.WriteLine(string.Concat("DOB: ", profile.DOB));
                Console.WriteLine(string.Concat("Address: ", profile.PlayerAddress));
                Console.WriteLine(string.Concat("Karma: ", profile.Karma));
                Console.WriteLine(string.Concat("Level: ", profile.Level));
            }

            

            Console.ReadKey();
        }

        private static void ProfileManager_OnProfileManagerError(object sender, ProfileManagerErrorEventArgs e)
        {
            Console.WriteLine(string.Concat("\nProfileManager Error. EndPoint: ", e.EndPoint, ", Reason: ", e.Reason, ", Error Details: ", e.ErrorDetails.ToString()));
        }

        private static void OASISStorageProvider_OnStorageProviderError(object sender, ProfileManagerErrorEventArgs e)
        {
            Console.WriteLine(string.Concat("\nOASIS Storage Provider Error. EndPoint: ", e.EndPoint, ", Reason: ", e.Reason, ", Error Details: ", e.ErrorDetails.ToString()));
        }
    }
}
