using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using System.Threading.Tasks;
using static NextGenSoftware.OASIS.API.Core.Events.EventDelegates;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public abstract class OASISManager
    {
        public OASISDNA OASISDNA { get; set; }

        //Events
        //public delegate void OASISManagerError(object sender, OASISErrorEventArgs e);
        public event OASISManagerError OnOASISManagerError;

        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public OASISManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null)
        {
            if (OASISStorageProvider != null)
            {
                //If it wasn't abstract we could also use this pattern.
                //https://blog.stephencleary.com/2013/01/async-oop-2-constructors.html
                Task.Run(async () => await ProviderManager.Instance.SetAndActivateCurrentStorageProviderAsync(OASISStorageProvider)).Wait(5000);
                OASISStorageProvider.OnStorageProviderError += OASISStorageProvider_StorageProviderError;
            }

            if (OASISDNA == null)
            {
                if (OASISDNAManager.OASISDNA == null)
                    OASISDNAManager.LoadDNA();

                this.OASISDNA = OASISDNAManager.OASISDNA;
            }
            else
                this.OASISDNA = OASISDNA;

            //TODO: Need to unsubscribe events to stop memory leaks...
        }

        private void OASISStorageProvider_StorageProviderError(object sender, OASISErrorEventArgs e)
        {
            OnOASISManagerError?.Invoke(this, new OASISErrorEventArgs() { Exception = e.Exception, Reason = e.Reason });
        }
    }
}
