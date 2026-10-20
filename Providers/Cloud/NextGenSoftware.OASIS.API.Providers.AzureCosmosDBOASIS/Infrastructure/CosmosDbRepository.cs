using System;
using System.Collections.Generic;
using System.Linq;
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
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure
{
    public abstract class CosmosDbRepository<T> : IRepository<T>, IDocumentCollectionContext<T> where T : IHolonBase
    {
        private readonly ICosmosDbClientFactory _cosmosDbClientFactory;

        protected CosmosDbRepository(ICosmosDbClientFactory cosmosDbClientFactory)
        {
            _cosmosDbClientFactory = cosmosDbClientFactory;
        }

        public Task<T> GetByIdAsync(string id) => GetByIdAsync(id,0);

        public async Task<T> GetByIdAsync(string id,int version)
        {
            try
            {
                if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
                if(!Guid.TryParse(id,out var identity) || identity==Guid.Empty)
                    throw new ArgumentException("A nonempty Cosmos OASIS GUID identity is required.",nameof(id));
                id=identity.ToString();
                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
                var partitionKey = ResolvePartitionKey(id);
                var json = await cosmosDbClient.ReadDocumentAsync(id, partitionKey);
                return json == null ? default : DeserializeEntity(json,id,version);
            }
            catch (CosmosException) { throw; }
        }

        public T GetByField(string fieldName, string fieldValue, int version = 0)
        {
            try
            {
                if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
                CosmosDbClient.ValidateFieldPath(fieldName);
                if(version>0)
                {
                    var matches=GetList(version).Where(record=> {
                        var value=JObject.Parse(OasisJson.Serialize(record)).SelectToken(fieldName);
                        return value?.Type==JTokenType.String && value.Value<string>()==fieldValue;
                    }).Take(2).ToList();
                    if(matches.Count>1)throw new StorageIdentityAmbiguousException();
                    return matches.Count==0 ? default : matches[0];
                }
                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
                var json = cosmosDbClient.ReadDocumentByField(fieldName, fieldValue, version);
                return json == null ? default : DeserializeEntity(json);
            }
            catch (CosmosException) { throw; }
        }

        public List<T> GetList() => GetList(0);

        public List<T> GetList(int version)
        {
            try
            {
                if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
                var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
                var jsonList = cosmosDbClient.ReadAllDocuments();
                var list = new List<T>();
                foreach (var json in jsonList)
                {
                    var selected=DeserializeEntity(json,version:version);
                    if(selected!=null)list.Add(selected);
                }
                return list;
            }
            catch (CosmosException) { throw; }
        }

        public Task<T> AddAsync(T entity) => WriteSnapshotAsync(entity,true);

        public Task<T> UpsertAsync(T entity) => WriteSnapshotAsync(entity,false);

        private async Task<T> WriteSnapshotAsync(T entity,bool create)
        {
            ArgumentNullException.ThrowIfNull(entity);
            var originalId=entity.Id;
            var snapshot=ProjectInput(entity);
            if(snapshot.Id==Guid.Empty) snapshot.Id=GenerateId(entity);
            if(snapshot.Id==Guid.Empty) throw new InvalidDataException("Cosmos generated an empty identity.");
            snapshot.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
            var expectedId=snapshot.Id.ToString();
            snapshot.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureCosmosDBOASIS]=expectedId;
            var client=_cosmosDbClientFactory.GetClient(CollectionName);
            var partition=ResolvePartitionKey(expectedId);
            var json=create ? await client.CreateDocumentAsync(snapshot,partition):await client.UpsertDocumentAsync(snapshot,partition);
            var acknowledged=DecodeAcknowledgedEntity(json,snapshot,expectedId);
            if(entity.Id!=originalId) throw new CosmosMutationReconciliationException(expectedId,new InvalidDataException("Caller identity changed during acknowledged Cosmos write."),acknowledged:true);
            entity.Id=snapshot.Id;
            entity.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
            entity.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureCosmosDBOASIS]=expectedId;
            entity.Version=acknowledged.Version;entity.VersionId=acknowledged.VersionId;entity.PreviousVersionId=acknowledged.PreviousVersionId;
            return acknowledged;
        }

        public async Task<T> ReplaceIfUnchangedAsync(T expected,T replacement)
        {
            ArgumentNullException.ThrowIfNull(expected);ArgumentNullException.ThrowIfNull(replacement);
            if(expected.Id==Guid.Empty || expected.Id!=replacement.Id)throw new ArgumentException("Conditional Cosmos replacement requires matching nonempty identities.");
            var original=ProjectInput(expected);
            var staged=ProjectInput(replacement);
            var key=staged.Id.ToString();
            staged.ProviderUniqueStorageKey ??=new Dictionary<Core.Enums.ProviderType,string>();
            staged.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureCosmosDBOASIS]=key;
            var client=_cosmosDbClientFactory.GetClient(CollectionName);
            var json=await client.ReplaceUnchangedDocumentAsync(key,original,staged,ResolvePartitionKey(key));
            return DecodeAcknowledgedEntity(json,staged,key);
        }

        public async Task UpdateAsync(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            var originalId=entity.Id;
            if(originalId==Guid.Empty) throw new ArgumentException("A nonempty Cosmos identity is required for replacement.",nameof(entity));
            var snapshot=ProjectInput(entity);
            var key=originalId.ToString();
            snapshot.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
            snapshot.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureCosmosDBOASIS]=key;
            var client=_cosmosDbClientFactory.GetClient(CollectionName);
            var json=await client.ReplaceDocumentAsync(key,snapshot,ResolvePartitionKey(key));
            var acknowledged=DecodeAcknowledgedEntity(json,snapshot,key);
            if(entity.Id!=originalId) throw new CosmosMutationReconciliationException(key,new InvalidDataException("Caller identity changed during acknowledged Cosmos replacement."),acknowledged:true);
            entity.ProviderUniqueStorageKey ??= new Dictionary<Core.Enums.ProviderType,string>();
            entity.ProviderUniqueStorageKey[Core.Enums.ProviderType.AzureCosmosDBOASIS]=key;
            entity.Version=acknowledged.Version;entity.VersionId=acknowledged.VersionId;entity.PreviousVersionId=acknowledged.PreviousVersionId;
        }

        public async Task DeleteAsync(T entity)
        {
            ArgumentNullException.ThrowIfNull(entity);
            await DeleteAsync(entity.Id);
        }

        public Task DeleteIfUnchangedAsync(T expected)
        {
            ArgumentNullException.ThrowIfNull(expected);
            if(expected.Id==Guid.Empty)throw new ArgumentException("A nonempty Cosmos identity is required.");
            var snapshot=ProjectInput(expected);var key=snapshot.Id.ToString();
            return _cosmosDbClientFactory.GetClient(CollectionName).DeleteUnchangedDocumentAsync(key,snapshot,ResolvePartitionKey(key));
        }

        public async Task DeleteAsync(Guid id)
        {
            await DeleteAsync(id.ToString());
        }

        public async Task DeleteAsync(string providerKey)
        {
            if(!Guid.TryParse(providerKey,out var id) || id==Guid.Empty) throw new ArgumentException("A nonempty Cosmos GUID key is required.",nameof(providerKey));
            var key=id.ToString();
            var cosmosDbClient = _cosmosDbClientFactory.GetClient(CollectionName);
            await cosmosDbClient.DeleteDocumentAsync(key, ResolvePartitionKey(key));
        }

        public abstract string CollectionName { get; }
        protected abstract Type EntityType { get; }
        // Trusted caller objects project into canonical DTOs; stored JSON still uses EntityType and the restricted binder.
        protected abstract T ProjectInput(T entity);
        public virtual Guid GenerateId(T entity) => Guid.NewGuid();
        public virtual PartitionKey? ResolvePartitionKey(string entityId) => null;

        private T DecodeAcknowledgedEntity(string json,T entity,string expectedId)
        {
            try
            {
                if(string.IsNullOrWhiteSpace(json))throw new InvalidDataException("Acknowledged Cosmos write lacks its canonical response record.");
                var acknowledged=DeserializeEntity(json,expectedId);
                if(JObject.Parse(json)[CosmosEmbeddedHistory.PropertyName]!=null)
                {
                    // SDK already checked the exact stamped document including its archive.
                    // Compare application fields against that ACK while accepting only SDK-owned version changes.
                    entity=ProjectInput(entity);
                    entity.Version=acknowledged.Version;entity.VersionId=acknowledged.VersionId;entity.PreviousVersionId=acknowledged.PreviousVersionId;
                }
                // Compare OASIS-owned fields after canonical decoding; Cosmos system fields
                // (id/_etag/_rid/_ts) are not part of the submitted domain payload.
                if(!StoredRecordPayloadEquality.Equivalent(JObject.Parse(OasisJson.Serialize(entity)),JObject.Parse(OasisJson.Serialize(acknowledged))))
                    throw new InvalidDataException("Acknowledged Cosmos record differs from the submitted canonical payload.");
                return acknowledged;
            }
            catch(Exception cause) {throw new CosmosMutationReconciliationException(expectedId,cause,acknowledged:true);}
        }

        private T DeserializeEntity(string json,string expectedId=null,int version=0)
        {
            try
            {
                var document=JObject.Parse(json);
                if(document["Id"]?.Type!=JTokenType.String || document["id"]?.Type!=JTokenType.String
                    || !Guid.TryParse(document["Id"].Value<string>(),out var logical) || logical==Guid.Empty
                    || document["id"].Value<string>()!=logical.ToString()
                    || expectedId!=null && expectedId!=logical.ToString())
                    throw new InvalidDataException("Cosmos document key, OASIS payload identity and requested identity must match.");
                var entity=(T)OasisJson.Deserialize(json,EntityType);
                if(entity==null || entity.Id!=logical) throw new InvalidDataException("Deserialized Cosmos identity differs from its document.");
                if(version>0 || document[CosmosEmbeddedHistory.PropertyName]!=null)
                    entity=(T)CosmosEmbeddedHistory.ReadAsync<IHolonBase>(logical,document,version,
                        text=>(IHolonBase)OasisJson.Deserialize(text,EntityType)).GetAwaiter().GetResult();
                return entity;
            }
            catch(JsonException cause)
            {
                throw new InvalidDataException("Stored Cosmos canonical record JSON is invalid.",cause);
            }
        }
    }
}
