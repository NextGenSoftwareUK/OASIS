using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task ArchivedVersionPreservesExactDecimalMetadata(bool synchronous)
    {
        const decimal before = 1.000000000000000000000000001m;
        using var provider = new SQLLiteDBOASIS(TestConnectionString) { IsVersionControlEnabled = true };
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = new Holon { Version = 1, MetaData = new() { ["amount"] = before, ["wide"] = double.MaxValue } };
        (await provider.SaveHolonAsync(source, saveChildren: false)).IsError.Should().BeFalse();
        source.Version = 2; source.MetaData["amount"] = 1.000000000000000000000000002m;
        (await provider.SaveHolonAsync(source, saveChildren: false)).IsError.Should().BeFalse();
        var old = synchronous ? provider.LoadHolon(source.Id, version: 1, loadChildren: false)
            : await provider.LoadHolonAsync(source.Id, version: 1, loadChildren: false);
        old.IsError.Should().BeFalse(old.Message + old.Exception);
        StoredRecordJson.Serialize(old.Result.MetaData).Should().Contain(before.ToString(System.Globalization.CultureInfo.InvariantCulture))
            .And.Contain(double.MaxValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        old.Result.Version.Should().Be(1);
    }

    [Theory]
    [InlineData(false, false)] [InlineData(false, true)] [InlineData(true, false)] [InlineData(true, true)]
    public async Task HistoricalMergeRejectsTinyDecimalConflictUnderOnePhysicalRevision(bool collection, bool conflict)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString) { IsVersionControlEnabled = true };
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source = new Holon { Version = 1, MetaData = new() { ["amount"] = 1.000000000000000000000000001m } };
        (await provider.SaveHolonAsync(source, saveChildren: false)).IsError.Should().BeFalse();
        var observed = (await provider.LoadHolonAsync(source.Id, loadChildren: false)).Result;
        observed.MetaData["amount"] = conflict ? 1.000000000000000000000000002m : 1.0000000000000000000000000010m;
        var json = StoredRecordJson.Serialize<IHolon>(observed);
        using var control = new DataContext(TestConnectionString);
        control.Database.ExecuteSqlRaw("INSERT INTO CosmicHolonRevision(HolonId,ProviderRevisionId,Version,VersionId,RecordJson,RecordDigest,ArchivedAtUtc) VALUES({0},{1},1,{2},{3},{4},{5})",
            observed.Id.ToString("D"), CosmicConcurrency.GetProviderRevisionId(observed, ProviderType.SQLLiteDBOASIS)!.Value.ToString("D"),
            observed.VersionId.ToString("D"), json, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))), DateTime.UtcNow.ToString("O"));
        if (collection)
        {
            var result = await provider.LoadAllHolonsAsync(version: 1, loadChildren: false);
            result.IsError.Should().Be(conflict, result.Message + result.Exception);
            if (conflict) result.Exception.Should().BeOfType<InvalidDataException>();
            else result.Result.Should().ContainSingle().Which.Id.Should().Be(source.Id);
        }
        else
        {
            var result = await provider.LoadHolonAsync(source.Id, version: 1, loadChildren: false);
            result.IsError.Should().Be(conflict, result.Message + result.Exception);
            if (conflict) result.Exception.Should().BeOfType<InvalidDataException>();
            else result.Result.Id.Should().Be(source.Id);
        }
    }
}
