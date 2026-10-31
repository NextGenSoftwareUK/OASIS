using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)] [InlineData(6)] [InlineData(7)]
    public async Task ConditionalFingerprintBindsExactNumbersAndForeignObservations(int selection)
    {
        const decimal before = 1.000000000000000000000000001m;
        const decimal after = 1.000000000000000000000000002m;
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = new Holon { Id = Guid.NewGuid(), MetaData = new() { ["amount"] = before,
            ["nested"] = new Dictionary<string, object> { ["amount"] = before } },
            ProviderUniqueStorageKey = new() { [ProviderType.MongoDBOASIS] = "foreign-before" },
            PreviousVersionProviderUniqueStorageKey = new() { [ProviderType.MongoDBOASIS] = "previous-before" } };
        CosmicConcurrency.SetProviderRevisionId(source, ProviderType.MongoDBOASIS, Guid.NewGuid());
        var request = ConditionalRequest(source, null, Guid.NewGuid(), Guid.NewGuid());
        var first = provider.ExecuteConditionalHolonWrite(request); first.IsError.Should().BeFalse(first.Message + first.Exception);
        var revision = Revision(first.Result.Single());
        switch (selection)
        {
            case 0: source.MetaData["amount"] = after; break;
            case 1: ((Dictionary<string, object>)source.MetaData["nested"])["amount"] = after; break;
            case 2: source.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = "foreign-after"; break;
            case 3: CosmicConcurrency.SetProviderRevisionId(source, ProviderType.MongoDBOASIS, Guid.NewGuid()); break;
            case 4: source.PreviousVersionProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = "previous-after"; break;
            case 5: source.MetaData["amount"] = 1.0000000000000000000000000010m; break;
            case 6:
                using (var document = System.Text.Json.JsonDocument.Parse("1.000000000000000000000000001")) source.MetaData["amount"] = document.RootElement.Clone();
                break;
            default:
                source.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "local-observation";
                CosmicConcurrency.SetProviderRevisionId(source, ProviderType.SQLLiteDBOASIS, Guid.NewGuid());
                source.Version = 999; source.VersionId = Guid.NewGuid(); break;
        }
        var repeated = provider.ExecuteConditionalHolonWrite(request);
        if (selection < 5)
        {
            repeated.IsError.Should().BeTrue(); repeated.ErrorCode.Should().Be("COSMIC_OPERATION_ID_REUSED"); repeated.Result.Should().BeNull();
        }
        else
        {
            repeated.IsError.Should().BeFalse(repeated.Message + repeated.Exception); Revision(repeated.Result.Single()).Should().Be(revision);
        }
        var stored = (await provider.LoadHolonAsync(source.Id, loadChildren: false)).Result;
        Revision(stored).Should().Be(revision);
        StoredRecordJson.Serialize(stored.MetaData).Should().Contain(before.ToString(System.Globalization.CultureInfo.InvariantCulture));
        stored.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS].Should().Be("foreign-before");
    }

    [Fact]
    public async Task HistoricalFingerprintReceiptRequiresReconciliationWithoutAnotherWrite()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = new Holon { Id = Guid.NewGuid(), Name = "retained historical receipt" };
        var op = Guid.NewGuid(); var request = ConditionalRequest(source, null, op, Guid.NewGuid());
        var first = provider.ExecuteConditionalHolonWrite(request); first.IsError.Should().BeFalse(first.Message + first.Exception);
        var revision = Revision(first.Result.Single());
        using (var context = new DataContext(TestConnectionString))
            context.Database.ExecuteSqlRaw("UPDATE CosmicWriteReceipt SET ResultJson=json_remove(ResultJson,'$.fingerprintFormat') WHERE OperationId={0}", op.ToString("D"));
        var replay = provider.ExecuteConditionalHolonWrite(request);
        replay.IsError.Should().BeTrue(); replay.ErrorCode.Should().Be("COSMIC_RECEIPT_FINGERPRINT_RECONCILIATION_REQUIRED"); replay.Result.Should().BeNull();
        var stored = (await provider.LoadHolonAsync(source.Id, loadChildren: false)).Result;
        Revision(stored).Should().Be(revision); stored.Name.Should().Be(source.Name);
    }
}
