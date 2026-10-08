using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public class CosmosDbClientFactory : ICosmosDbClientFactory, IDisposable
    {
        private readonly CosmosClient _cosmosClient;
        private readonly string _databaseName;
        private readonly List<string> _collectionNames;

        public CosmosDbClientFactory(CosmosClient cosmosClient, string databaseName, List<string> collectionNames)
        {
            _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
            _databaseName = databaseName ?? throw new ArgumentNullException(nameof(databaseName));
            _collectionNames = collectionNames ?? throw new ArgumentNullException(nameof(collectionNames));
        }

        public ICosmosDbClient GetClient(string collectionName)
        {
            if (!_collectionNames.Contains(collectionName))
                throw new ArgumentException($"Unable to find collection: {collectionName}");

            var container = _cosmosClient.GetContainer(_databaseName, collectionName);
            return new CosmosDbClient(container);
        }

        public async Task<OASISResult<bool>> EnsureDbSetupAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                DatabaseResponse databaseResponse = await _cosmosClient.CreateDatabaseIfNotExistsAsync(_databaseName);
                Database database = databaseResponse.Database;

                foreach (var collectionName in _collectionNames)
                    await database.CreateContainerIfNotExistsAsync(
                        new ContainerProperties(collectionName, "/id"));

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in EnsureDbSetupAsync in AzureCosmosDBOASIS Provider. Reason: {ex}");
            }

            return result;
        }

        public void Dispose() => _cosmosClient.Dispose();
    }
}
