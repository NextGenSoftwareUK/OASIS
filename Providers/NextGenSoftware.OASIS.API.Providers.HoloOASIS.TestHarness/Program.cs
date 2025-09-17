using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.WebSocket;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.TestHarness
{
    class Program
    {
        static HoloOASIS _holoOASIS = new HoloOASIS("ws://localhost:8888");
        //static Core.HcAvatar _savedAvatar;
        static Avatar _savedAvatar;

        static async Task Main(string[] args)
        {
            Console.WriteLine("NEXTGEN SOFTWARE HOLOOASIS TEST HARNESS V1.O");
            Console.WriteLine("");

            _holoOASIS.HoloNETClient.OnConnected += HoloNETClient_OnConnected;
            _holoOASIS.OnInitialized += _holoOASIS_OnInitialized;
            _holoOASIS.OnPlayerAvatarLoaded += _holoOASIS_OnPlayerAvatarLoaded;
            _holoOASIS.OnPlayerAvatarSaved += _holoOASIS_OnPlayerAvatarSaved;
            _holoOASIS.OnHoloOASISError += _holoOASIS_OnHoloOASISError;

            //  await _holoOASIS.Initialize();

            _holoOASIS.ActivateProvider();

           // Task.Delay(10000);
            Console.ReadKey();

            /*
            IAvatar Avatar = await _holoOASIS.GetAvatarAsync(Guid.NewGuid());

            if (Avatar != null)
            {
                Console.WriteLine("Avatar Received.");
                Console.WriteLine(string.Concat("Name: ", Avatar.Title, " ", Avatar.FirstName, " ", Avatar.LastName));
                Console.WriteLine(string.Concat("DOB: ", Avatar.DOB));
                Console.WriteLine(string.Concat("Address: ", Avatar.PlayerAddress));
                Console.WriteLine(string.Concat("Karma: ", Avatar.Karma));
                Console.WriteLine(string.Concat("Level: ", Avatar.Level));
            }*/
        }

        private static void _holoOASIS_OnHoloOASISError(object sender, HoloOASISErrorEventArgs e)
        {
            Console.WriteLine(string.Concat("Error Occured. Reason: ", e.Reason, (e.HoloNETErrorDetails != null ? string.Concat(", HoloNET Reason: ", e.HoloNETErrorDetails.Reason) : ""), (e.HoloNETErrorDetails != null ? string.Concat(", HoloNET Details: ", e.HoloNETErrorDetails.ErrorDetails.ToString()) : ""), "\n"));
        }

        private static void HoloNETClient_OnConnected(object sender, ConnectedEventArgs e)
        {
            Console.WriteLine("Connected");
        }

        private static void _holoOASIS_OnPlayerAvatarSaved(object sender, AvatarSavedEventArgs e)
        {
            Console.WriteLine("Avatar Saved.");
            Console.WriteLine("Avatar Entry Hash: " + e.HcAvatar.hc_address_hash);
            Console.WriteLine("Loading Avatar...");
            //_savedAvatar.Id = new Guid(e.AvatarEntryHash);
            _holoOASIS.LoadAvatarAsync(e.HcAvatar.hc_address_hash);
        }

        private static void _holoOASIS_OnPlayerAvatarLoaded(object sender, AvatarLoadedEventArgs e)
        {
            Console.WriteLine("Avatar Loaded.");
            Console.WriteLine(string.Concat("Id: ", e.Avatar.Id));
            Console.WriteLine(string.Concat("HC Address Hash: ", e.HcAvatar.hc_address_hash));
            Console.WriteLine(string.Concat("Name: ", e.Avatar.Title, " ", e.Avatar.FirstName, " ", e.Avatar.LastName));
            Console.WriteLine(string.Concat("Username: ", e.Avatar.Username));
            Console.WriteLine(string.Concat("Password: ", e.Avatar.Password));
            Console.WriteLine(string.Concat("Email: ", e.Avatar.Email));
            //Console.WriteLine(string.Concat("DOB: ", e.Avatar.DOB));
          //  Console.WriteLine(string.Concat("Address: ", e.Avatar.Address));
            //Console.WriteLine(string.Concat("Karma: ", e.Avatar.Karma));
            //Console.WriteLine(string.Concat("Level: ", e.Avatar.Level));
        }

        private static async void _holoOASIS_OnInitialized(object sender, EventArgs e)
        {
            Console.WriteLine("Initialized.");
            Console.WriteLine("Saving Avatar...");

            _savedAvatar = new Avatar { Username = "dellams", Email = "david@nextgensoftware.co.uk", Password = "1234", FirstName = "David", LastName = "Ellams", Id = Guid.NewGuid(), Title = "Mr" };
            await _holoOASIS.SaveAvatarAsync(_savedAvatar);
        }
    }
}
