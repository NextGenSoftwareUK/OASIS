using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.CosmosOASIS.Infrastructure;
using NextGenSoftware.OASIS.API.Providers.CosmosOASIS.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.CosmosOASIS
{
    public class CosmosOASIS : OASISStorageBase, IOASISStorage, IOASISNET
    {
        private readonly Uri serviceEndpoint;
        private readonly string authKey;
        private readonly string databaseName;
        private readonly List<string> collectionNames;
        private CosmosDbClientFactory dbClientFactory;
        private IAvatarRepository avatarRepository;

        public CosmosOASIS(Uri serviceEndpoint, string authKey, string databaseName, List<string> collectionNames)
        {
            this.ProviderName = "CosmosOASIS";
            this.ProviderDescription = "Microsoft Cosmos DB Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AzureCosmosDBOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.serviceEndpoint = serviceEndpoint;
            this.authKey = authKey;
            this.databaseName = databaseName;
            this.collectionNames = collectionNames;
        }

        public override void ActivateProvider()
        {
            if (dbClientFactory == null)
            {
                var documentClient = new DocumentClient(serviceEndpoint, authKey, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Ignore,
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                });
                documentClient.OpenAsync().Wait();

                dbClientFactory = new CosmosDbClientFactory(databaseName, collectionNames, documentClient);
                dbClientFactory.EnsureDbSetupAsync().Wait();

                avatarRepository = new AvatarRepository(dbClientFactory);
            }
            base.ActivateProvider();
        }

        public override void DeActivateProvider()
        {
            dbClientFactory = null;
            avatarRepository = null;
            base.DeActivateProvider();
        }

        #region -- Avatar --

        public override bool DeleteAvatar(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteAvatar(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        #endregion

        public override bool DeleteHolon(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteHolon(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteHolonAsync(Guid id, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public override Task<bool> DeleteHolonAsync(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IHolon> GetHolonsNearMe(HolonType Type)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IPlayer> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IAvatarDetail> LoadAllAvatarDetails()
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IAvatarDetail>> LoadAllAvatarDetailsAsync()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IAvatar> LoadAllAvatars()
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IAvatar>> LoadAllAvatarsAsync()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadAllHolons(HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IHolon>> LoadAllHolonsAsync(HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(Guid Id)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(string username, string password)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatar(string username)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarAsync(Guid Id)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarAsync(string username, string password)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarAsync(string username)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatarByEmail(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarByEmailAsync(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatarByUsername(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarByUsernameAsync(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override IAvatarDetail LoadAvatarDetail(Guid id)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatarDetail> LoadAvatarDetailAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public override IAvatarDetail LoadAvatarDetailByEmail(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatarDetail> LoadAvatarDetailByEmailAsync(string avatarEmail)
        {
            throw new NotImplementedException();
        }

        public override IAvatarDetail LoadAvatarDetailByUsername(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatarDetail> LoadAvatarDetailByUsernameAsync(string avatarUsername)
        {
            throw new NotImplementedException();
        }

        public override IAvatar LoadAvatarForProviderKey(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> LoadAvatarForProviderKeyAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override IHolon LoadHolon(Guid id)
        {
            throw new NotImplementedException();
        }

        public override IHolon LoadHolon(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> LoadHolonAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        public override Task<IHolon> LoadHolonAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(Guid id, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<IHolon> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override Task<IEnumerable<IHolon>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All)
        {
            throw new NotImplementedException();
        }

        public override IAvatar SaveAvatar(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatar> SaveAvatarAsync(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }

        public override IAvatarDetail SaveAvatarDetail(IAvatarDetail Avatar)
        {
            throw new NotImplementedException();
        }

        public override Task<IAvatarDetail> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildrenRecursive = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildrenRecursive = true)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildrenRecursive = true)
        {
            throw new NotImplementedException();
        }

        public override Task<ISearchResults> SearchAsync(ISearchParams searchParams)
        {
            throw new NotImplementedException();
        }
    }
}
