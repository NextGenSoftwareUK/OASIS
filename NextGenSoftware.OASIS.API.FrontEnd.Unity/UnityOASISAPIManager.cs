using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity;
using System.Threading.Tasks;

//using UnityEngine;
//using UnityEngine.UI;

namespace NextGenSoftware.OASIS.API.FrontEnd.Unity
{
    public class UnityOASISAPIManager
    {
        //public GameObject ProfileName;

        public UnityOASISAPIManager()
        {
            // Inject in the HoloOASIS Storage Provider (this could be moved to a config file later so the 
            // providers can be sweapped without having to re-compile.
            ProfileManager = new ProfileManager(new HoloOASIS("ws://localhost:8888"));
            //StorageProvider = new HoloOASIS("ws://localhost:8888");
        }

        ProfileManager ProfileManager { get; set; }  //If the ProfileManager is going to contain additional business logic not contained in the providers then use this.
        //IOASISStorage StorageProvider { get; set; }

        public async Task LoadProfileAsync(string username, string password)
        {
            IProfile profile = await ProfileManager.LoadProfileAsync(username, password);

            if (profile != null)
            {
                //TODO: Bind profile info to Unity Avatar UI here.
            }

            //return await StorageProvider.LoadProfileAsync(username, password);
        }
    }
}
