using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(-2)]
    [InlineData(-1)]
    [InlineData(1)]
    public async Task NativeGroupedNumericRejectsContradictoryParticipationAndAllowsFreshRead(int countAdjustment)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = await SeedGroups(provider, ("coffee", 5L), ("coffee", 7L));
        var query = CosmicQuery<Holon>.Create().Where(h => ids.Contains(h.Id))
            .GroupBy(h => CosmicQueryMetadata.Value<string>(h, "category"))
            .Select(g => new
            {
                Sum = g.Sum(h => CosmicQueryMetadata.Value<long>(h, "stock")),
                Mean = g.Average(h => CosmicQueryMetadata.Value<long>(h, "stock"))
            }).Result;
        var plan = GroupPlan(provider, query);

        using (var context = new DataContext(TestConnectionString))
        {
            await context.Database.OpenConnectionAsync();
            var connection = (SqliteConnection)context.Database.GetDbConnection();
            var altered = 0;

            // Corrupt the containing native COUNT(*) while leaving SUM/Average
            // limbs and their participating-value counts intact. The decoder must
            // reject missing/excess required coverage before publishing any values.
            connection.CreateAggregate<long, long>(
                "count", 0L, count => count + 1,
                count =>
                {
                    altered++;
                    return count + countAdjustment;
                });

            var rejected = await new HolonRepository(context, () => false).ExecuteCosmicGroupedAsync(plan);

            altered.Should().BeGreaterThan(0, "the real native group-count aggregate must be reached");
            rejected.ErrorCode.Should().Be("CQL_DATA_INVALID", rejected.Message);
            rejected.IsError.Should().BeTrue();
            rejected.IsLoaded.Should().BeFalse();
            rejected.Result.Should().BeNull();
            context.Database.CurrentTransaction.Should().BeNull();
        }

        // The override belongs to the disposed connection. A new native read
        // must publish the exact values and retain no failed-query transaction.
        var fresh = await CosmicGroupedExecutor.ExecuteAsync(plan, new[] { (ICosmicQueryStorageProvider)provider });
        fresh.IsError.Should().BeFalse(fresh.Message + fresh.Exception);
        var row = fresh.Result.Providers.Single().Rows.Single();
        row.Sum.Should().Be(12L);
        row.Mean.Should().Be(6m);
    }
}
