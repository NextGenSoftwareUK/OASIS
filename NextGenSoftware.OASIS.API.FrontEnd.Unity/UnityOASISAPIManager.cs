using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA.Enums;

//using UnityEngine;
//using UnityEngine.UI;

namespace NextGenSoftware.OASIS.API.FrontEnd.Unity
{
    public class UnityOASISAPIManager
    {
        //public GameObject AvatarName;

        public UnityOASISAPIManager()
        {
            // Inject in the HoloOASIS Storage Provider (this could be moved to a config file later so the 
            // providers can be sweapped without having to re-compile.
            AvatarManager = new AvatarManager(new HoloOASIS("ws://localhost:8888", HolochainVersion.Redux));
            //StorageProvider = new HoloOASIS("ws://localhost:8888");
        }

        AvatarManager AvatarManager { get; set; }  //If the AvatarManager is going to contain additional business logic not contained in the providers then use this.
        //IOASISStorage StorageProvider { get; set; }

        public async Task LoadAvatarAsync(string username, string password)
        {
            IAvatar Avatar = await AvatarManager.LoadAvatarAsync(username, password);

            if (Avatar != null)
            {
                //TODO: Bind Avatar info to Unity Avatar UI here.
            }

            //return await StorageProvider.LoadAvatarAsync(username, password);
        }
    }
}
