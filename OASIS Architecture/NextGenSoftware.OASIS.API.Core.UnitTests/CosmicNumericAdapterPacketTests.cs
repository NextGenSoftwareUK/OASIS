using System.Globalization;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class CosmicNumericAdapterPacketTests
{
    public static IEnumerable<object[]> Vectors()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CqlNumericAggregateVectors.json")));
        return document.RootElement.GetProperty("vectors").EnumerateArray()
            .Select(v => new object[] { v.GetProperty("name").GetString()!, v.GetRawText() }).ToArray();
    }

    [Theory]
    [MemberData(nameof(Vectors))]
    public void AdapterPacketsUseTheSameExactReferenceContract(string name, string json)
    {
        using var document = JsonDocument.Parse(json); var vector = document.RootElement;
        var inputs = vector.GetProperty("inputs").EnumerateArray().Select(v => long.Parse(v.GetString()!, CultureInfo.InvariantCulture)).ToArray();
        var domain = vector.GetProperty("domain").GetString() == "Int32" ? typeof(int) : typeof(long);
        if (vector.GetProperty("nullable").GetBoolean()) domain = typeof(Nullable<>).MakeGenericType(domain);
        var operation = System.Enum.Parse<CosmicScalarOperation>(vector.GetProperty("operation").GetString()!);
        var high = inputs.Sum(value => value / CosmicNumericScalarResults.LimbBase);
        var low = inputs.Sum(value => value % CosmicNumericScalarResults.LimbBase);
        var result = CosmicNumericScalarResults.FromParts(operation, domain, high, low, inputs.Length);
        var expected = vector.GetProperty("expected");
        if (expected.TryGetProperty("error", out var error))
        {
            Assert.Equal(error.GetString(), result.ErrorCode);
            Assert.False(result.IsLoaded);
        }
        else
        {
            Assert.False(result.IsError, result.Message);
            Assert.True(result.IsLoaded);
            var value = expected.GetProperty("value");
            if (value.ValueKind == JsonValueKind.Null) Assert.Null(result.Result);
            else
            {
                var output = operation == CosmicScalarOperation.Average ? typeof(decimal) : Nullable.GetUnderlyingType(domain) ?? domain;
                Assert.Equal(output, result.Result.GetType());
                Assert.Equal(Convert.ChangeType(value.GetString(), output, CultureInfo.InvariantCulture), result.Result);
            }
        }
    }
}
