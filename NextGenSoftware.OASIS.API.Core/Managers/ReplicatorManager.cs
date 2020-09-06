using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static NextGenSoftware.OASIS.API.Core.AvatarManager;

namespace NextGenSoftware.OASIS.API.Core
{
    public class ReplicatorManager : OASISManager
    {
       // private AvatarManagerConfig _config;

        public List<IOASISStorage> OASISStorageProviders { get; set; }

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
        public AvatarManager(List<IOASISStorage> OASISStorageProviders)
        {
            this.OASISStorageProviders = OASISStorageProviders;

            foreach (IOASISStorage provider in OASISStorageProviders)
            {
                provider.OnStorageProviderError += OASISStorageProvider_OnStorageProviderError;
                provider.ActivateProvider();
            }
        }*/

       //TODO: In future more than one storage provider can be active at a time where each call can specify which provider to use.
        public ReplicatorManager(IOASISStorage OASISStorageProvider) : base(OASISStorageProvider)
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

        public async Task<IAvatar> LoadAvatarAsync(string providerKey, ProviderType provider = ProviderType.Default)
        {
            if (provider != ProviderType.Default)
                return await ((IOASISStorage)ProviderManager.GetAndActivateProvider(provider)).LoadAvatarAsync(providerKey);

            return await ProviderManager.CurrentStorageProvider.LoadAvatarAsync(providerKey);
        }

        public async Task<IAvatar> LoadAvatarAsync(Guid id, ProviderType provider = ProviderType.Default)
        {
            if (provider != ProviderType.Default)
                return await ((IOASISStorage)ProviderManager.GetAndActivateProvider(provider)).LoadAvatarAsync(id);

            return await ProviderManager.CurrentStorageProvider.LoadAvatarAsync(id);
        }

        public async Task<IAvatar> LoadAvatarAsync(string username, string password, ProviderType provider = ProviderType.Default)
        {
            if (provider != ProviderType.Default)
                return await ((IOASISStorage)ProviderManager.GetAndActivateProvider(provider)).LoadAvatarAsync(username, password);

            return await ProviderManager.CurrentStorageProvider.LoadAvatarAsync(username, password);
        }

        public async Task<IAvatar> SaveAvatarAsync(IAvatar Avatar, ProviderType provider = ProviderType.Default)
        {
            if (provider != ProviderType.Default)
                return await ((IOASISStorage)ProviderManager.GetAndActivateProvider(provider)).SaveAvatarAsync(Avatar);

            return await ProviderManager.CurrentStorageProvider.SaveAvatarAsync(Avatar);
        }

        public async Task<KarmaAkashicRecord> AddKarmaToAvatarAsync(IAvatar Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType provider = ProviderType.Default)
        {
            if (provider != ProviderType.Default)
                return await ((IOASISStorage)ProviderManager.GetAndActivateProvider(provider)).AddKarmaToAvatarAsync(Avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);

            return await ProviderManager.CurrentStorageProvider.AddKarmaToAvatarAsync(Avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }

        public async Task<KarmaAkashicRecord> RemoveKarmaFromAvatarAsync(IAvatar Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, ProviderType provider = ProviderType.Default)
        {
            if (provider != ProviderType.Default)
                return await ((IOASISStorage)ProviderManager.GetAndActivateProvider(provider)).SubtractKarmaFromAvatarAsync(Avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);

            return await ProviderManager.CurrentStorageProvider.SubtractKarmaFromAvatarAsync(Avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc);
        }
    }
}
