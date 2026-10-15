using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Exceptions;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

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
                if(!Guid.TryParse(id,out var identity) || identity==Guid.Empty)
                    throw new ArgumentException("A nonempty Cosmos OASIS GUID identity is required.",nameof(id));
                id=identity.ToString();
                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
                var partitionKey = ResolvePartitionKey(id);
                var json = await cosmosDbClient.ReadDocumentAsync(id, partitionKey);
                return json == null ? default : DeserializeEntity(json,id);
            }
            catch (CosmosException) { throw; }
        }

        public T GetByField(string fieldName, string fieldValue, int version = 0)
        {
            try
            {
                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
                var json = cosmosDbClient.ReadDocumentByField(fieldName, fieldValue, version);
                return json == null ? default : DeserializeEntity(json);
            }
            catch (CosmosException) { throw; }
        }

        public List<T> GetList()
        {
            try
            {
                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
                var jsonList = cosmosDbClient.ReadAllDocuments();
                var list = new List<T>();
                foreach (var json in jsonList)
                    list.Add(DeserializeEntity(json));
                return list;
            }
            catch (CosmosException) { throw; }
        }

        public async Task<T> AddAsync(T entity)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(entity);
                if (entity.Id == Guid.Empty)
                    entity.Id = GenerateId(entity);
                entity.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
                entity.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureCosmosDBOASIS] = entity.Id.ToString();

                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
                var expectedId=entity.Id.ToString();
                var partitionKey = ResolvePartitionKey(expectedId);
                var json = await cosmosDbClient.CreateDocumentAsync(entity, partitionKey);
                return DecodeAcknowledgedEntity(json,entity,expectedId);
            }
            catch (CosmosException e)
            {
                if (e.StatusCode == HttpStatusCode.Conflict)
                    throw new EntityAlreadyExistsException();
                throw;
            }
        }

        public async Task<T> UpsertAsync(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            if (entity.Id == Guid.Empty)
                entity.Id = GenerateId(entity);

            entity.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
            entity.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureCosmosDBOASIS] = entity.Id.ToString();
            var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
            var expectedId=entity.Id.ToString();
            var json = await cosmosDbClient.UpsertDocumentAsync(entity, ResolvePartitionKey(expectedId));
            return DecodeAcknowledgedEntity(json,entity,expectedId);
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
        protected abstract Type EntityType { get; }
        public virtual Guid GenerateId(T entity) => Guid.NewGuid();
        public virtual PartitionKey? ResolvePartitionKey(string entityId) => null;

        private T DecodeAcknowledgedEntity(string json,T entity,string expectedId)
        {
            try
            {
                if(json!=null) return DeserializeEntity(json,expectedId);
                if(entity.Id.ToString()!=expectedId) throw new InvalidDataException("Caller identity changed after acknowledged Cosmos write.");
                return entity;
            }
            catch(Exception cause) {throw new CosmosMutationReconciliationException(expectedId,cause,acknowledged:true);}
        }

        private T DeserializeEntity(string json,string expectedId=null)
        {
            var document=JObject.Parse(json);
            if(document["Id"]?.Type!=JTokenType.String || document["id"]?.Type!=JTokenType.String
                || !Guid.TryParse(document["Id"].Value<string>(),out var logical) || logical==Guid.Empty
                || document["id"].Value<string>()!=logical.ToString()
                || expectedId!=null && expectedId!=logical.ToString())
                throw new InvalidDataException("Cosmos document key, OASIS payload identity and requested identity must match.");
            var entity=(T)OasisJson.Deserialize(json,EntityType);
            if(entity==null || entity.Id!=logical) throw new InvalidDataException("Deserialized Cosmos identity differs from its document.");
            return entity;
        }
    }
}
