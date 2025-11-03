using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Exceptions;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public abstract class CosmosDbRepository<T> : IRepository<T>, IDocumentCollectionContext<T> where T : IHolonBase //Entity
    {
        private readonly ICosmosDbClientFactory _cosmosDbClientFactory;
        //private readonly Microsoft.Azure.Cosmos.Container _cosmosDbContainer;

        protected CosmosDbRepository(ICosmosDbClientFactory cosmosDbClientFactory)
        {
            _cosmosDbClientFactory = cosmosDbClientFactory;                      
        }

        public async Task<T> GetByIdAsync(string id)
        {
            try
            {
                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
                
                var document = await cosmosDbClient.ReadDocumentAsync(id, new RequestOptions
                {
                    PartitionKey = ResolvePartitionKey(id)
                    //PartitionKey = new PartitionKey(id)
                });
                //var document = await cosmosDbClient.ReadDocumentAsync(id);
                return JsonConvert.DeserializeObject<T>(document.ToString());
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new EntityNotFoundException();
                }

                throw;
            }
        }

        public T GetByField(string fieldName, string fieldValue, int version = 0)
        {
            try
            {
                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);

                //var document = await cosmosDbClient.ReadDocumentByField(fieldName, fieldValue, new RequestOptions
                //{
                //    PartitionKey = ResolvePartitionKey(id)
                //});

                var document = cosmosDbClient.ReadDocumentByField(fieldName, fieldValue, version);
                return JsonConvert.DeserializeObject<T>(document.ToString());
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new EntityNotFoundException();
                }

                throw;
            }
        }

        public List<T> GetList()
        {
            try
            {
                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);

                var av = cosmosDbClient.ReadAllDocuments();
                var objList = av.ToList();
                List<T> avatars = new List<T>();

                foreach (var item in objList)
                    avatars.Add(JsonConvert.DeserializeObject<T>(item.ToString()));

                return avatars;
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new EntityNotFoundException();
                }

                throw;
            }
        }

        public async Task<T> AddAsync(T entity)
        {
            try
            {
                entity.Id = GenerateId(entity);

                //Normally the providerKey is different to the Id but in this case they are the same since Azure uses GUID's the same as the OASIS does for ID.
                entity.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureCosmosDBOASIS] = entity.Id.ToString();

                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
                var document = await cosmosDbClient.CreateDocumentAsync(entity);
                return JsonConvert.DeserializeObject<T>(document.ToString());
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == HttpStatusCode.Conflict)
                {
                    throw new EntityAlreadyExistsException();
                }

                throw;
            }
        }

        public async Task UpdateAsync(T entity)
        {
            try
            {
                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
                await cosmosDbClient.ReplaceDocumentAsync(entity.Id.ToString(), entity);
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new EntityNotFoundException();
                }

                throw;
            }
        }

        public async Task DeleteAsync(T entity)
        {
            //await DeleteAsync(entity.Id.ToString());
            await DeleteAsync(entity.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureCosmosDBOASIS]);
        }

        public async Task DeleteAsync(Guid id)
        {
            //Because ProviderKey and Id are both the same (GUID) we can just do this... :)
            await DeleteAsync(id.ToString());
        }

        public async Task DeleteAsync(string providerKey)
        {
            try
            {
                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
                await cosmosDbClient.DeleteDocumentAsync(providerKey, new RequestOptions
                {
                    PartitionKey = ResolvePartitionKey(providerKey)
                });
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new EntityNotFoundException();
                }

                throw;
            }
        }

        public abstract string CollectionName { get; }
        //public virtual string GenerateId(T entity) => Guid.NewGuid().ToString();
        public virtual Guid GenerateId(T entity) => Guid.NewGuid();
        public virtual PartitionKey ResolvePartitionKey(string entityId) => null;
    }
}
