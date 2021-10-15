using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.DbModel.Interfaces;
using NextGenSoftware.OASIS.API.Providers.OnionProtocol.MongoDb.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.OnionProtocol.MongoDb.Provider
{
    public class MongoDbProvider<TEntity> : IEntityProvider<TEntity> where TEntity : IEntity
    {
        private IMongoClient _client;
        private IMongoDatabase _database;
        private IMongoCollection<TEntity> _collection;

        public MongoDbProvider(string databaseUrl, string databaseName)
        {
            this._client = new MongoClient(databaseUrl);
            this._database = _client.GetDatabase(databaseName);
            this._collection = _database.GetCollection<TEntity>(typeof(TEntity).Name);
        }

        public long Count()
        {
            var filter = Builders<TEntity>.Filter.Empty;
            var count = this._collection.CountDocuments(filter);
            return count;
        }

        public async Task<long> CountAsync()
        {
            var filter = Builders<TEntity>.Filter.Empty;
            var count = await this._collection.CountDocumentsAsync(filter);
            return count;
        }

        public TEntity Get(Guid objectId)
        {
            var filter = Builders<TEntity>.Filter.Eq("_id", objectId);
            var list = this._collection.Find(filter);
            return list.SingleOrDefault();
        }

        public async Task<TEntity> GetAsync(Guid objectId)
        {
            var filter = Builders<TEntity>.Filter.Eq("_id", objectId);
            var list = await this._collection.FindAsync(filter);
            return list.SingleOrDefault();
        }

        public TEntity Get(FilterDefinition<TEntity> filters)
        {
            var list = this._collection.Find(filters);
            return list.ToList().FirstOrDefault();
        }

        public async Task<TEntity> GetAsync(FilterDefinition<TEntity> filters)
        {
            var list = await this._collection.FindAsync(filters);
            return list.ToList().FirstOrDefault();
        }

        public IEnumerable<TEntity> Get(FilterDefinition<TEntity> filters, FindOptions<TEntity> options)
        {
            var list = this._collection.Find(filters);
            return list.ToList();
        }

        public async Task<IEnumerable<TEntity>> GetAsync(FilterDefinition<TEntity> filters, FindOptions<TEntity> options)
        {
            var list = await this._collection.FindAsync(filters, options);
            return list.ToList();
        }

        public IList<TEntity> GetAll()
        {
            var filter = Builders<TEntity>.Filter.Empty;
            var list = this._collection.Find(filter);
            return list.ToList();
        }

        public async Task<IList<TEntity>> GetAllAsync()
        {
            var filter = Builders<TEntity>.Filter.Empty;
            var list = await this._collection.FindAsync(filter);
            return list.ToList();
        }

        public TEntity Insert(TEntity entityObject)
        {
            this._collection.InsertOne(entityObject);
            return entityObject;
        }

        public async Task<TEntity> InsertAsync(TEntity entityObject)
        {
            await this._collection.InsertOneAsync(entityObject);
            return entityObject;
        }

        public DeleteResult Remove(Guid objectId)
        {
            var filter = Builders<TEntity>.Filter.Eq("_id", objectId);
            var list = this._collection.DeleteOne(filter);
            return list;
        }

        public async Task<DeleteResult> RemoveAsync(Guid objectId)
        {
            var filter = Builders<TEntity>.Filter.Eq("_id", objectId);
            var list = await this._collection.DeleteOneAsync(filter);
            return list;
        }

        public DeleteResult Remove(TEntity objectId)
        {
            var filter = Builders<TEntity>.Filter.And(
           Builders<TEntity>.Filter.Eq("_id", objectId.Id));
            var count = this._collection.DeleteOne(filter);
            return count;
        }

        public async Task<DeleteResult> RemoveAsync(TEntity objectId)
        {
            var filter = Builders<TEntity>.Filter.And(
               Builders<TEntity>.Filter.Eq("_id", objectId.Id));
            var count = await this._collection.DeleteOneAsync(filter);
            return count;
        }

        public void RemoveCollection()
        {
            this._database.DropCollection(typeof(TEntity).FullName);
        }

        public TEntity Update(TEntity entityObject)
        {
            var filter = Builders<TEntity>.Filter.And(
                Builders<TEntity>.Filter.Eq("_id", entityObject.Id));

            // Concurrency check
            var previousInstance = this._collection.FindOneAndReplace(filter, entityObject);
            if (previousInstance == null)
            {
                throw new Exception();
            }
            return entityObject;
        }

        public async Task<TEntity> UpdateAsync(TEntity entityObject)
        {
            var filter = Builders<TEntity>.Filter.And(
                Builders<TEntity>.Filter.Eq("_id", entityObject.Id));

            // Concurrency check
            var previousInstance = await this._collection.FindOneAndReplaceAsync(filter, entityObject);
            if (previousInstance == null)
            {
                throw new Exception();
            }
            return entityObject;
        }

        public TEntity Upsert(TEntity entityObject)
        {
            this._collection.ReplaceOne(doc => doc.Id.Equals(entityObject.Id), entityObject,
            new UpdateOptions { IsUpsert = true });
            return entityObject;
        }

        public async Task<TEntity> UpsertAsync(TEntity entityObject)
        {
            await this._collection.ReplaceOneAsync(doc => doc.Id.Equals(entityObject.Id), entityObject,
                new UpdateOptions { IsUpsert = true });
            return entityObject;
        }

        public async Task<IList<TEntity>> GetAllAsync(FilterDefinition<TEntity> filters)
        {
            var list = await this._collection.FindAsync(filters);
            return list.ToList();
        }

        public IList<TEntity> GetAll(FilterDefinition<TEntity> filters)
        {
            var list = this._collection.Find(filters);
            return list.ToList();
        }

        public async Task<DeleteResult> RemoveAsync(FilterDefinition<TEntity> filters)
        {
            var list = await this._collection.FindAsync(filters);
            var filter = Builders<TEntity>.Filter.And(
           Builders<TEntity>.Filter.Eq("_id", list.FirstOrDefault().Id));
            var count = this._collection.DeleteOne(filter);
            return count;
        }

        public DeleteResult Remove(FilterDefinition<TEntity> filters)
        {
            var list = this._collection.Find(filters);
            var filter = Builders<TEntity>.Filter.And(
            Builders<TEntity>.Filter.Eq("_id", list.FirstOrDefault().Id));
            var count = this._collection.DeleteOne(filter);
            return count;
        }
    }
}