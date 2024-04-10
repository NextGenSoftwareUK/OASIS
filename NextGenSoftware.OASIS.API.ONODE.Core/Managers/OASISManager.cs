using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONode.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using System;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.ONode.Core.Managers
{
    public abstract class OASISManager : IOASISManager
    {
        public HolonManager Data { get; set; }
        public OASISDNA OASISDNA {get; set; }
        public Guid AvatarId { get; set; }

        public OASISManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null)
        {
            AvatarId = avatarId;
            Data = new HolonManager(OASISStorageProvider, OASISDNA);
            Task.Run(async () => await HandleDNAAsync(OASISDNA)).Wait();
        }

        public OASISManager(Guid avatarId, OASISDNA OASISDNA = null)
        {
            AvatarId = avatarId;
            Task.Run(async () => await HandleDNAAsync(OASISDNA)).Wait();
            OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;

            if (!result.IsError && result.Result != null)
                Data = new HolonManager(result.Result);
            else
                OASISErrorHandling.HandleError(ref result, string.Concat("Error calling OASISDNAManager.GetAndActivateDefaultProvider(). Error details: ", result.Message));
        }

        private async Task HandleDNAAsync(OASISDNA dna)
        {
            if (dna == null)
            {
                if (OASISDNAManager.OASISDNA == null)
                    await OASISDNAManager.LoadDNAAsync();

                this.OASISDNA = OASISDNAManager.OASISDNA;
            }
            else
                this.OASISDNA = dna;
        }
    }
}