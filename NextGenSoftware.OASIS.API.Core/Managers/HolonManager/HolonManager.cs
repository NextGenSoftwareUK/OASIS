using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class HolonManager : OASISManager
    {
        private static HolonManager _instance = null;
        private OASISResult<IEnumerable<IHolon>> _allHolonsCache = null;

        //public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

        public static HolonManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new HolonManager(ProviderManager.Instance.CurrentStorageProvider);

                return _instance;
            }
        }

        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public HolonManager(IOASISStorageProvider OASISStorageProvider, OASISDNA OASISDNA = null) : base(OASISStorageProvider, OASISDNA)
        {

        }

        public void ClearCache()
        {
            _allHolonsCache.Result = null;
            _allHolonsCache = null;
        }

        /// <summary>
        /// Send's a given holon from one provider to another. 
        /// This method is only really needed if auto-replication is disabled or there is a use case for sending from one provider to another.
        /// By default this will NOT auto-replicate to any other provider (set autoReplicate to true if you wish it to). This param overrides the global auto-replication setting.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id"></param>
        /// <param name="sourceProviderType"></param>
        /// <param name="destinationProviderType"></param>
        /// <param name="autoReplicate"></param>
        /// <returns></returns>
        public OASISResult<T> SendHolon<T>(Guid id, ProviderType sourceProviderType, ProviderType destinationProviderType, bool autoReplicate = false) where T : IHolon, new()
        {
            // TODO: Finish Implementing ASAP...
            // Needs to load the holon from the source provider and then save to the destination provider.


            return new OASISResult<T>();
        }
    }
} 