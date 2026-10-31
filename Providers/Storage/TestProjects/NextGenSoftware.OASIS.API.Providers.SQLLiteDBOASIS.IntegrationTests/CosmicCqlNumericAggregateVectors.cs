using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    public static IEnumerable<object[]> NumericReferenceVectors()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CqlNumericAggregateVectors.json")));
        return document.RootElement.GetProperty("vectors").EnumerateArray()
            .Select(v => new object[] { v.GetProperty("name").GetString()!, v.GetRawText() }).ToArray();
    }

    [Theory]
    [MemberData(nameof(NumericReferenceVectors))]
    public async Task NativeNumericConsumesSharedReferenceVector(string name, string json)
    {
        using var document = JsonDocument.Parse(json); var vector = document.RootElement;
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var domain = vector.GetProperty("domain").GetString(); var optional = vector.GetProperty("nullable").GetBoolean();
        var values = vector.GetProperty("inputs").EnumerateArray().Select(v => (object)long.Parse(v.GetString()!, CultureInfo.InvariantCulture)).ToArray();
        var keys = await SeedNumeric(provider, values);
        var source = CosmicQuery<Holon>.Create().Where(h => keys.Contains(h.Id)).Result;
        var average = vector.GetProperty("operation").GetString() == "Average";
        if (domain == "Int32")
        {
            if (optional) { if (average) await Check(source.Average(h => CosmicQueryMetadata.Value<int?>(h, "value")).Result); else await Check(source.Sum(h => CosmicQueryMetadata.Value<int?>(h, "value")).Result); }
            else { if (average) await Check(source.Average(h => CosmicQueryMetadata.Value<int>(h, "value")).Result); else await Check(source.Sum(h => CosmicQueryMetadata.Value<int>(h, "value")).Result); }
        }
        else
        {
            if (optional) { if (average) await Check(source.Average(h => CosmicQueryMetadata.Value<long?>(h, "value")).Result); else await Check(source.Sum(h => CosmicQueryMetadata.Value<long?>(h, "value")).Result); }
            else { if (average) await Check(source.Average(h => CosmicQueryMetadata.Value<long>(h, "value")).Result); else await Check(source.Sum(h => CosmicQueryMetadata.Value<long>(h, "value")).Result); }
        }

        async Task Check<TResult>(CosmicScalarQuery<Holon, TResult> intent)
        {
            var result = await provider.ExecuteCosmicScalarAsync(NumericPlan(provider, intent));
            var expected = vector.GetProperty("expected");
            if (expected.TryGetProperty("error", out var error))
            { result.ErrorCode.Should().Be(error.GetString()); result.IsLoaded.Should().BeFalse(); return; }
            result.IsError.Should().BeFalse(result.Message + result.Exception); result.IsLoaded.Should().BeTrue();
            var value = expected.GetProperty("value");
            if (value.ValueKind == JsonValueKind.Null) Assert.Null((object?)result.Result);
            else
            {
                var type = Nullable.GetUnderlyingType(typeof(TResult)) ?? typeof(TResult);
                var decoded = Convert.ChangeType(value.GetString(), type, CultureInfo.InvariantCulture);
                Assert.Equal((TResult)decoded!, result.Result);
            }
        }
    }
}
