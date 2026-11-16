using System;
using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed partial class MongoSubscriptionUsageRepository
    {
        private static void EnsureSubscriptionIndexes<T>(IMongoCollection<T> collection, params CreateIndexModel<T>[] models)
        {
            List<BsonDocument> catalog;
            try
            {
                using var cursor = collection.Indexes.List();
                catalog = cursor.ToList();
            }
            catch (MongoCommandException error) when (error.Code == 26)
            {
                // NamespaceNotFound means this collection has no indexes to verify.
                catalog = new List<BsonDocument>();
            }

            var render = new RenderArgs<T>(collection.DocumentSerializer, BsonSerializer.SerializerRegistry);
            var missing = new List<CreateIndexModel<T>>();
            foreach (var model in models)
            {
                var options = model.Options;
                var current = catalog.SingleOrDefault(index => index["name"] == options.Name);
                if (current == null)
                {
                    missing.Add(model);
                    continue;
                }

                var partial = (options as CreateIndexOptions<T>)?.PartialFilterExpression?.Render(render);
                if (!current["key"].AsBsonDocument.Equals(model.Keys.Render(render))
                    || current.GetValue("unique", false).ToBoolean() != (options.Unique ?? false)
                    || !current.GetValue("partialFilterExpression", BsonNull.Value).Equals((BsonValue)partial ?? BsonNull.Value)
                    || current.GetValue("sparse", false).ToBoolean()
                    || current.GetValue("hidden", false).ToBoolean()
                    || current.Contains("expireAfterSeconds") || current.Contains("collation"))
                    throw new InvalidOperationException($"Subscription index {options.Name} differs from its required definition.");
            }

            if (missing.Count != 0)
                collection.Indexes.CreateMany(missing);
        }
    }
}
