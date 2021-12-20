
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public class ReplicatorManager : OASISManager
    {
       // private AvatarManagerConfig _config;

        public List<IOASISStorageProvider> OASISStorageProviders { get; set; }

        //public Task<IAvatar> LoadAvatarAsync(Guid id)
        //{
        //    throw new NotImplementedException();
        //}

        //public AvatarManagerConfig Config
        //{
        //    get
        //    {
        //        if (_config == null)
        //        {
        //            _config = new AvatarManagerConfig();
        //        }

        //        return _config;
        //    }
        //}




        //RegisterDatabaseForReplication(string connectionString, string dbName);
        //RegisterTableForReplication(string connectionString, string dbName, string tableName);
        //RegisterTablesForReplication(string connectionString, string dbName, string<> tables);
        //RegisterQueryForReplication(string connectionString, string dbName, string query);

        //Events
        public delegate void ReplicatorManagerError(object sender, AvatarManagerErrorEventArgs e);
      //  public event AvatarManagerError OnAvatarManagerError;

        public delegate void StorageProviderError(object sender, AvatarManagerErrorEventArgs e);

        /*
        public AvatarManager(List<IOASISStorageProvider> OASISStorageProviders)
        {
            this.OASISStorageProviders = OASISStorageProviders;

            foreach (IOASISStorageProvider provider in OASISStorageProviders)
            {
                provider.OnStorageProviderError += OASISStorageProvider_OnStorageProviderError;
                provider.ActivateProvider();
            }
        }*/

       //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public ReplicatorManager(IOASISStorageProvider OASISStorageProvider) : base(OASISStorageProvider)
        {
            //if (!ProviderManager.IsProviderRegistered(OASISStorageProvider))
            //    ProviderManager.RegisterProvider(OASISStorageProvider);

            //ProviderManager.SwitchCurrentStorageProvider(OASISStorageProvider.ProviderType);

         //   return null;
        }

        private void OASISStorageProvider_OnStorageProviderError(object sender, AvatarManagerErrorEventArgs e)
        {
            //TODO: Not sure if we need to have a OnAvatarManagerError as well as the StorageProvider Error Event?
            
            //TODO: (URGENT) FIX THIS! BOTTOM LINE USE TO BE IN....
            //OnOASISManagerError?.Invoke(this, new OASISErrorEventArgs() { Reason = e.Reason, ErrorDetails = e.ErrorDetails });
            //OnAvatarManagerError?.Invoke(this, e);
        }
    }
}
