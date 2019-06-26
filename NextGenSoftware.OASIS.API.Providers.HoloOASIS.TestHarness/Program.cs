using NextGenSoftware.OASIS.API.Core;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.TestHarness
{
    class Program
    {
        static HoloOASIS _holoOASIS = new HoloOASIS("ws://localhost:8888");
        static Profile _savedProfile;

        static async Task Main(string[] args)
        {
            Console.WriteLine("NEXTGEN SOFTWARE HOLOOASIS TEST HARNESS V1.O");
            Console.WriteLine("");

            _holoOASIS.HoloNETClient.OnConnected += HoloNETClient_OnConnected;
            _holoOASIS.OnInitialized += _holoOASIS_OnInitialized;
            _holoOASIS.OnPlayerProfileLoaded += _holoOASIS_OnPlayerProfileLoaded;
            _holoOASIS.OnPlayerProfileSaved += _holoOASIS_OnPlayerProfileSaved;
            _holoOASIS.OnHoloOASISError += _holoOASIS_OnHoloOASISError;

           // await _holoOASIS.Initialize();

            Task.Delay(10000);
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

        private static void _holoOASIS_OnHoloOASISError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine("Error Occured. Reason: " + e.Reason + ", HoloNET Reason: " + e.HoloNETErrorDetails.Reason + ", HoloNET Details: " + e.HoloNETErrorDetails.ErrorDetails.ToString());
        }

        private static void HoloNETClient_OnConnected(object sender, Holochain.HoloNET.Client.Core.ConnectedEventArgs e)
        {
            Console.WriteLine("Connected");
        }

        private static void _holoOASIS_OnPlayerProfileSaved(object sender, ProfileSavedEventArgs e)
        {
            Console.WriteLine("Profile Saved.");
            Console.WriteLine("Profile Entry Hash: " + e.ProfileEntryHash);
            Console.WriteLine("Loading Profile...");
            _holoOASIS.LoadProfileAsync(e.ProfileEntryHash);
        }

        private static void _holoOASIS_OnPlayerProfileLoaded(object sender, ProfileLoadedEventArgs e)
        {
            Console.WriteLine("Profile Loaded.");
            Console.WriteLine(string.Concat("Name: ", e.Profile.Title, " ", e.Profile.FirstName, " ", e.Profile.LastName));
            Console.WriteLine(string.Concat("DOB: ", e.Profile.DOB));
            Console.WriteLine(string.Concat("Address: ", e.Profile.PlayerAddress));
            Console.WriteLine(string.Concat("Karma: ", e.Profile.Karma));
            Console.WriteLine(string.Concat("Level: ", e.Profile.Level));
        }

        private static void _holoOASIS_OnInitialized(object sender, EventArgs e)
        {
            Console.WriteLine("Initialized.");
            Console.WriteLine("Saving Profile...");

            //_holoOASIS.HoloNETClient.OnConnected += HoloNETClient_OnConnected;
            _savedProfile = new Profile { FirstName = "David", LastName = "Ellams", DOB = "11/04/1980", Id = Guid.NewGuid(), Title = "Mr", PlayerAddress = "blahahahaha", Karma = "999" };
            _holoOASIS.SaveProfileAsync(_savedProfile);
        }
    }
}
