using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS
{
    public partial class AzureCosmosDBOASIS : OASISStorageProviderBase, IOASISStorageProvider, IOASISNETProvider
    {
        private readonly Uri serviceEndpoint;
        private readonly string authKey;
        private readonly string databaseName;
        private readonly List<string> collectionNames;
        private CosmosDbClientFactory dbClientFactory;
        private IAvatarRepository avatarRepository;
        private IAvatarDetailRepository avatarDetailRepository;
        private IHolonRepository holonRepository;

        public AzureCosmosDBOASIS(Uri serviceEndpoint, string authKey, string databaseName, List<string> collectionNames)
        {
            this.ProviderName = "AzureCosmosDBOASIS";
            this.ProviderDescription = "Microsoft Azure Cosmos DB Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.AzureCosmosDBOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.serviceEndpoint = serviceEndpoint;
            this.authKey = authKey;
            this.databaseName = databaseName;
            this.collectionNames = collectionNames;
        }

    }
}
