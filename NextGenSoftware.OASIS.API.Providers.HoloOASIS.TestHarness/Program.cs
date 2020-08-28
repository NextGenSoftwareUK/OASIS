using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core;

using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.TestHarness
{
    class Program
    {
        static Desktop.HoloOASIS _holoOASIS = new Desktop.HoloOASIS("ws://localhost:8888");
        //static Core.HcProfile _savedProfile;
        static Avatar _savedProfile;

        static async Task Main(string[] args)
        {
            Console.WriteLine("NEXTGEN SOFTWARE HOLOOASIS TEST HARNESS V1.O");
            Console.WriteLine("");

            _holoOASIS.HoloNETClient.OnConnected += HoloNETClient_OnConnected;
            _holoOASIS.OnInitialized += _holoOASIS_OnInitialized;
            _holoOASIS.OnPlayerProfileLoaded += _holoOASIS_OnPlayerProfileLoaded;
            _holoOASIS.OnPlayerProfileSaved += _holoOASIS_OnPlayerProfileSaved;
            _holoOASIS.OnHoloOASISError += _holoOASIS_OnHoloOASISError;

            //  await _holoOASIS.Initialize();

            _holoOASIS.ActivateProvider();

           // Task.Delay(10000);
            Console.ReadKey();

            /*
            IProfile profile = await _holoOASIS.GetProfileAsync(Guid.NewGuid());

            if (profile != null)
            {
                Console.WriteLine("Profile Received.");
                Console.WriteLine(string.Concat("Name: ", profile.Title, " ", profile.FirstName, " ", profile.LastName));
                Console.WriteLine(string.Concat("DOB: ", profile.DOB));
                Console.WriteLine(string.Concat("Address: ", profile.PlayerAddress));
                Console.WriteLine(string.Concat("Karma: ", profile.Karma));
                Console.WriteLine(string.Concat("Level: ", profile.Level));
            }*/
        }

        private static void _holoOASIS_OnHoloOASISError(object sender, HoloOASISErrorEventArgs e)
        {
            Console.WriteLine(string.Concat("Error Occured. Reason: ", e.Reason, (e.HoloNETErrorDetails != null ? string.Concat(", HoloNET Reason: ", e.HoloNETErrorDetails.Reason) : ""), (e.HoloNETErrorDetails != null ? string.Concat(", HoloNET Details: ", e.HoloNETErrorDetails.ErrorDetails.ToString()) : ""), "\n"));
        }

        private static void HoloNETClient_OnConnected(object sender, Holochain.HoloNET.Client.Core.ConnectedEventArgs e)
        {
            Console.WriteLine("Connected");
        }

        private static void _holoOASIS_OnPlayerProfileSaved(object sender, ProfileSavedEventArgs e)
        {
            Console.WriteLine("Profile Saved.");
            Console.WriteLine("Profile Entry Hash: " + e.HcProfile.hc_address_hash);
            Console.WriteLine("Loading Profile...");
            //_savedProfile.Id = new Guid(e.ProfileEntryHash);
            _holoOASIS.LoadProfileAsync(e.HcProfile.hc_address_hash);
        }

        private static void _holoOASIS_OnPlayerProfileLoaded(object sender, ProfileLoadedEventArgs e)
        {
            Console.WriteLine("Profile Loaded.");
            Console.WriteLine(string.Concat("Id: ", e.Profile.Id));
            Console.WriteLine(string.Concat("HC Address Hash: ", e.HcProfile.hc_address_hash));
            Console.WriteLine(string.Concat("Name: ", e.Profile.Title, " ", e.Profile.FirstName, " ", e.Profile.LastName));
            Console.WriteLine(string.Concat("Username: ", e.Profile.Username));
            Console.WriteLine(string.Concat("Password: ", e.Profile.Password));
            Console.WriteLine(string.Concat("Email: ", e.Profile.Email));
            Console.WriteLine(string.Concat("DOB: ", e.Profile.DOB));
            Console.WriteLine(string.Concat("Address: ", e.Profile.PlayerAddress));
            Console.WriteLine(string.Concat("Karma: ", e.Profile.Karma));
            Console.WriteLine(string.Concat("Level: ", e.Profile.Level));
        }

        private static async void _holoOASIS_OnInitialized(object sender, EventArgs e)
        {
            Console.WriteLine("Initialized.");
            Console.WriteLine("Saving Profile...");

            _savedProfile = new Avatar { Username = "dellams", Email = "david@nextgensoftware.co.uk", Password = "1234", FirstName = "David", LastName = "Ellams", DOB = "11/04/1980", Id = Guid.NewGuid(), Title = "Mr", PlayerAddress = "blahahahaha" };
            await _savedProfile.KarmaEarnt(KarmaTypePositive.HelpingTheEnvironment, KarmaSourceType.hApp, "Our World", "XR Educational Game To Make The World A Better Place", false);
            await _holoOASIS.SaveProfileAsync(_savedProfile);
        }
    }
}
