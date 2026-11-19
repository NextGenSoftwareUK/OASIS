using System.Security.Cryptography;
using System.Text;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class AvatarProfileCommitStampNativeTests
{
    [Fact]
    public Task ProfileHydrationUsesCurrentSdkBearerClientAndActualNativeController() =>
        RunSubscriptionHttp(true, usage: true, profileHydration: true);

    private static async Task<string> ProfileHydrationNativeStateAsync(IMongoDatabase db)
    {
        var state = await SubscriptionAdminFinancialStateAsync(db);
        foreach (var name in new[] { "Avatar", "AvatarDetail" })
            state += new BsonArray(await db.GetCollection<BsonDocument>(name)
                .Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).ToListAsync()).ToJson();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(state)));
    }
}
