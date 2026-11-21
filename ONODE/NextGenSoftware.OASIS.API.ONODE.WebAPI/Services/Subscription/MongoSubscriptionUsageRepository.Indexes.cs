using System.Threading;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    public sealed partial class MongoSubscriptionUsageRepository
    {
        private static void EnsureSubscriptionIndexes<T>(IMongoCollection<T> collection, params CreateIndexModel<T>[] models)
            => VerifiedMongoIndexes.EnsureAsync(collection, models, CancellationToken.None).GetAwaiter().GetResult();
    }
}
