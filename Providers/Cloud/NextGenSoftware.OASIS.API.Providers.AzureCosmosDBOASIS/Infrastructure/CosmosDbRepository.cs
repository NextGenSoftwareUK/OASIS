using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public abstract class CosmosDbRepository<T> : IRepository<T>, IDocumentCollectionContext<T> where T : IHolonBase
    {
        private readonly ICosmosDbClientFactory _cosmosDbClientFactory;

        protected CosmosDbRepository(ICosmosDbClientFactory cosmosDbClientFactory)
        {
            _cosmosDbClientFactory = cosmosDbClientFactory;
        }

        public async Task<T> GetByIdAsync(string id)
        {
            try
            {
                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
                var partitionKey = ResolvePartitionKey(id);
                var json = await cosmosDbClient.ReadDocumentAsync(id, partitionKey);
                return json == null ? default : JsonConvert.DeserializeObject<T>(json);
            }
            catch (CosmosException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                    throw new EntityNotFoundException();
                throw;
            }
        }

        public T GetByField(string fieldName, string fieldValue, int version = 0)
        {
            try
            {
                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
                var json = cosmosDbClient.ReadDocumentByField(fieldName, fieldValue, version);
                return json == null ? default : JsonConvert.DeserializeObject<T>(json);
            }
            catch (CosmosException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                    throw new EntityNotFoundException();
                throw;
            }
        }

        public List<T> GetList()
        {
            try
            {
                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
                var jsonList = cosmosDbClient.ReadAllDocuments();
                var list = new List<T>();
                foreach (var json in jsonList)
                    list.Add(JsonConvert.DeserializeObject<T>(json));
                return list;
            }
            catch (CosmosException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                    throw new EntityNotFoundException();
                throw;
            }
        }

        public async Task<T> AddAsync(T entity)
        {
            try
            {
                entity.Id = GenerateId(entity);
                entity.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureCosmosDBOASIS] = entity.Id.ToString();

                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
                var partitionKey = ResolvePartitionKey(entity.Id.ToString());
                var json = await cosmosDbClient.CreateDocumentAsync(entity, partitionKey);
                return json == null ? entity : JsonConvert.DeserializeObject<T>(json);
            }
            catch (CosmosException e)
            {
                if (e.StatusCode == HttpStatusCode.Conflict)
                    throw new EntityAlreadyExistsException();
                throw;
            }
        }

        public async Task UpdateAsync(T entity)
        {
            try
            {
                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
                var partitionKey = ResolvePartitionKey(entity.Id.ToString());
                await cosmosDbClient.ReplaceDocumentAsync(entity.Id.ToString(), entity, partitionKey);
            }
            catch (CosmosException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                    throw new EntityNotFoundException();
                throw;
            }
        }

        public async Task DeleteAsync(T entity)
        {
            await DeleteAsync(entity.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureCosmosDBOASIS]);
        }

        public async Task DeleteAsync(Guid id)
        {
            await DeleteAsync(id.ToString());
        }

        public async Task DeleteAsync(string providerKey)
        {
            try
            {
                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
                var partitionKey = ResolvePartitionKey(providerKey);
                await cosmosDbClient.DeleteDocumentAsync(providerKey, partitionKey);
            }
            catch (CosmosException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                    throw new EntityNotFoundException();
                throw;
            }
        }

        public abstract string CollectionName { get; }
        public virtual Guid GenerateId(T entity) => Guid.NewGuid();
        public virtual PartitionKey? ResolvePartitionKey(string entityId) => null;
    }
}
