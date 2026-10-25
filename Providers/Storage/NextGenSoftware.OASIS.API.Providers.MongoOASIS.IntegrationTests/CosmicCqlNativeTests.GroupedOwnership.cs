using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Fact]
    public Task NativeGroupedMembershipKeysAndValuesShareSnapshotAcrossReparenting()
        => WithMongoGraph(async fixture =>
        {
            var query = DescendantPlan(fixture, 2, 3).Query.GroupBy(h => h.Name)
                .Select(g => g.Sum(h => h.Version)).Result;
            var plan = GroupedPlan(query);
            var oldName = fixture.Root.Name;
            var oldVersion = fixture.Root.Version;
            var child = await fixture.Provider.LoadHolonAsync(fixture.Root.Children.First().Id, loadChildren: false);
            Assert.False(child.IsError, child.Message);

            var admin = fixture.Client.GetDatabase("admin");
            var configured = await admin.RunCommandAsync<BsonDocument>(new BsonDocument
            {
                { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("skip", 1) },
                { "data", new BsonDocument
                    {
                        { "failCommands", new BsonArray { "aggregate" } }, { "appName", "CosmicCqlGraph" },
                        { "blockConnection", true }, { "blockTimeMS", 5000 }
                    }
                }
            });
            Task<OASISResult<IReadOnlyList<CosmicGroupedRow>>>? pending = null;
            try
            {
                // The first native scope command establishes the snapshot. Hold
                // the later aggregate while changing both membership and values.
                pending = fixture.Provider.ExecuteCosmicGroupedAsync(plan);
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument
                {
                    { "waitForFailPoint", "failCommand" },
                    { "timesEntered", configured["count"].ToInt64() + 1 }, { "maxTimeMS", 10000 }
                });
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument
                    { { "configureFailPoint", "failCommand" }, { "mode", "off" } });

                fixture.KnownOutcome = false;
                child.Result.ParentHolonId = Guid.NewGuid();
                var moved = await fixture.Provider.SaveHolonAsync(child.Result, saveChildren: false);
                fixture.KnownOutcome = !moved.IsError;
                Assert.False(moved.IsError, moved.Message + moved.Exception);

                fixture.KnownOutcome = false;
                var changed = await fixture.Records.UpdateOneAsync(
                    new BsonDocument("HolonId", fixture.Root.Id.ToString("D")),
                    new BsonDocument("$set", new BsonDocument { { "Name", "after snapshot" }, { "Version", 71 } }));
                fixture.KnownOutcome = changed.IsAcknowledged;
                Assert.True(changed.IsAcknowledged);
                Assert.Equal(1, changed.ModifiedCount);

                var observed = await pending;
                Assert.False(observed.IsError, observed.Message + observed.Exception);
                var group = Assert.Single(observed.Result);
                Assert.Equal(oldName, Assert.Single(group.Keys));
                Assert.Equal(oldVersion, Assert.Single(group.Values));

                // The same provider must observe the committed reparent on a
                // new read, rather than retaining the old snapshot or its scope.
                var fresh = await fixture.Provider.ExecuteCosmicGroupedAsync(plan);
                Assert.False(fresh.IsError, fresh.Message + fresh.Exception);
                Assert.Empty(fresh.Result);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument
                    { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                if (pending != null) await pending;
            }
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task NativeGroupedInterruptsOutstandingIoWithinOneDeadlineAndAllowsReuse(bool expire)
        => WithMongoGraph(async fixture =>
        {
            var query = DescendantPlan(fixture, 2, 3).Query.GroupBy(h => h.Name)
                .Select(g => g.Sum(h => h.Version)).Result;
            var plan = GroupedPlan(query);
            var budgets = new ConcurrentQueue<long>();
            var settings = MongoClientSettings.FromConnectionString(fixture.Provider.ConnectionString);
            settings.MaxConnectionPoolSize = 30;
            settings.ClusterConfigurator = cluster => cluster.Subscribe<CommandStartedEvent>(command =>
            {
                if (command.CommandName == "aggregate") budgets.Enqueue(command.Command["maxTimeMS"].ToInt64());
            });
            var monitored = new MongoClient(settings);
            fixture.Provider.Database.MongoClient = monitored;
            fixture.Provider.Database.MongoDB = monitored.GetDatabase(fixture.Provider.DBName);

            var admin = fixture.Client.GetDatabase("admin");
            var configured = await admin.RunCommandAsync<BsonDocument>(new BsonDocument
            {
                { "configureFailPoint", "failCommand" }, { "mode", "alwaysOn" },
                { "data", new BsonDocument
                    {
                        { "failCommands", new BsonArray { "aggregate" } }, { "appName", "CosmicCqlGraph" },
                        { "blockConnection", true }, { "blockTimeMS", expire ? 11000 : 3000 }
                    }
                }
            });
            using var cancellation = new CancellationTokenSource();
            Task<OASISResult<IReadOnlyList<CosmicGroupedRow>>>? pending = null;
            try
            {
                pending = fixture.Provider.ExecuteCosmicGroupedAsync(plan, cancellationToken: cancellation.Token);
                if (!expire)
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument
                    {
                        { "waitForFailPoint", "failCommand" },
                        { "timesEntered", configured["count"].ToInt64() + 1 }, { "maxTimeMS", 10000 }
                    });
                    cancellation.Cancel();
                }

                var rejected = await pending;
                Assert.Equal(expire ? "CQL_QUERY_TIMEOUT" : "CQL_QUERY_CANCELLED", rejected.ErrorCode);
                Assert.Null(rejected.Result);
                Assert.False(rejected.IsLoaded);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument
                    { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                if (pending != null) await pending;
            }

            if (expire)
            {
                // Assert the actual wire budgets: scope and reduction cannot
                // each restart the provider's 30-second allowance.
                var observed = budgets.ToArray();
                Assert.True(observed.Length >= 2);
                Assert.All(observed, value => Assert.InRange(value, 1L, 30000L));
                for (var index = 1; index < observed.Length; index++)
                    Assert.True(observed[index] < observed[index - 1]);
            }

            var fresh = await fixture.Provider.ExecuteCosmicGroupedAsync(plan);
            Assert.False(fresh.IsError, fresh.Message + fresh.Exception);
            Assert.Equal(fixture.Root.Version, Assert.Single(Assert.Single(fresh.Result).Values));
        });
}
