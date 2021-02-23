//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace NextGenSoftware.OASIS.API.Core
//{
//    public class SeedsManager : OASISManager
//    {
//       // public List<IOASISStorage> OASISStorageProviders { get; set; }
        
//        public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

//        //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
//        public SeedsManager(IOASISStorage OASISStorageProvider) : base(OASISStorageProvider)
//        {

//        }

//        public Task<bool> SEEDSManager(string providerKey, bool softDelete = true, ProviderType provider = ProviderType.Default)
//        {
//            return  (SEEDSOASIS)ProviderManager.GetProvider(ProviderType.SEEDSOASIS);
//        }
//    }
//}
