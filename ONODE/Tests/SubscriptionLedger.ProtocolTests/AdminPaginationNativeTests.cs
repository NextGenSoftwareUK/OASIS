using System.Globalization;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace SubscriptionLedger.ProtocolTests;

public sealed class AdminPaginationNativeTests
{
    [Fact]
    public async Task AdministrativePagesRetainEveryEqualTimestampRecord()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
            throw new InvalidOperationException("The owned loopback MongoDB runner is required.");
        var name = "admin_pages_" + Guid.NewGuid().ToString("N");
        var client = new MongoClient(uri);
        Assert.DoesNotContain(name, await (await client.ListDatabaseNamesAsync()).ToListAsync());
        try
        {
            var ledger = new MongoSubscriptionUsageRepository(client, name, TimeProvider.System);
            var db = client.GetDatabase(name);
            var at = new DateTime(2026, 11, 18, 0, 0, 0, DateTimeKind.Utc);
            var ids = new[] { "d", "c", "b", "a", "old" };
            foreach (var id in ids)
            {
                var time = id == "old" ? at.AddMilliseconds(-1) : at;
                await db.GetCollection<OrderRecord>("subscription_orders").InsertOneAsync(new OrderRecord
                    { Id = id, StripeInvoiceId = "invoice-" + id, CreatedAt = time });
                await db.GetCollection<BsonDocument>("subscription_usage_events").InsertOneAsync(new BsonDocument
                    { { "_id", ObjectId.GenerateNewId() }, { "OperationId", id }, { "AuthorizedAtUtc", time } });
                await db.GetCollection<SubscriptionUsageAuditEntry>("subscription_usage_audit").InsertOneAsync(
                    new SubscriptionUsageAuditEntry { Id = id, OperationId = id, OccurredAtUtc = time });
            }
            Assert.Equal(ids, await Collect((before, id) => ledger.GetAdminInvoicesAsync(before, 2, default, id), x => x.Id));
            Assert.Equal(ids, await Collect((before, id) => ledger.GetAdminEventsAsync(before, 2, default, id), x => x.OperationId));
            Assert.Equal(ids, await Collect((before, id) => ledger.GetAdminAuditAsync(before, 2, default, id), x => x.Id));
            await Assert.ThrowsAsync<ArgumentException>(() => ledger.GetAdminInvoicesAsync(null, 2, default, "missing-date"));
            await Assert.ThrowsAsync<ArgumentException>(() => ledger.GetAdminEventsAsync(at, 2, default, " "));
            await Assert.ThrowsAsync<ArgumentException>(() => ledger.GetAdminAuditAsync(null, 2, default, "missing-date"));
        }
        finally
        {
            await client.DropDatabaseAsync(name);
            Assert.DoesNotContain(name, await (await client.ListDatabaseNamesAsync()).ToListAsync());
        }
    }

    private static async Task<List<string>> Collect<T>(Func<DateTime?, string, Task<SubscriptionAdminPage<T>>> read, Func<T, string> id)
    {
        DateTime? before = null;
        string beforeId = null;
        var seen = new List<string>();
        for (var pageNumber = 0; pageNumber < 10; pageNumber++)
        {
            var page = await read(before, beforeId);
            Assert.InRange(page.Items.Count, 1, 2);
            seen.AddRange(page.Items.Select(id));
            if (page.NextCursor == null) { Assert.Null(page.NextId); return seen; }
            Assert.Equal(id(page.Items.Last()), page.NextId);
            before = DateTime.Parse(page.NextCursor, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            beforeId = page.NextId;
        }
        throw new InvalidOperationException("Native continuation did not terminate.");
    }
}
