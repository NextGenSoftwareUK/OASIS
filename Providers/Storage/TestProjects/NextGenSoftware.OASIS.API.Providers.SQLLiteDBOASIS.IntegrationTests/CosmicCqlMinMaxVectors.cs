using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    public static IEnumerable<object[]> MinMaxReferenceVectors()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "CqlMinMaxVectors.json")));
        return document.RootElement.GetProperty("vectors").EnumerateArray()
            .Select(vector => new object[] { vector.GetProperty("name").GetString()!, vector.GetRawText() }).ToArray();
    }

    [Theory]
    [MemberData(nameof(MinMaxReferenceVectors))]
    public async Task NativeMinMaxConsumesReferenceVector(string name, string json)
    {
        using var document = JsonDocument.Parse(json);
        var vector = document.RootElement; var domain = vector.GetProperty("domain").GetString()!;
        var optional = vector.GetProperty("nullable").GetBoolean();
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var ids = new List<Guid>();
        foreach (var input in vector.GetProperty("inputs").EnumerateArray())
        {
            var row = new Holon { Id = Guid.NewGuid(), Name = "owned minmax " + name };
            var state = input.GetProperty("state").GetString();
            if (state != "missing") row.MetaData["minmaxValue"] = state == "null" ? null!
                : state == "wrong-domain" ? input.GetProperty("value").GetString()!
                : VectorStoredValue(domain, input.GetProperty("value"));
            (await provider.SaveHolonAsync(row, saveChildren: false)).IsError.Should().BeFalse(); ids.Add(row.Id);
        }
        var keys = ids.ToArray();
        var source = CosmicQuery<Holon>.Create().Where(h => keys.Contains(h.Id)).Result;
        var expected = vector.GetProperty("expected");
        switch (domain)
        {
            case "Int64": if (optional) await CheckVector<long?>(); else await CheckVector<long>(); break;
            case "Int32": if (optional) await CheckVector<int?>(); else await CheckVector<int>(); break;
            case "Byte": if (optional) await CheckVector<byte?>(); else await CheckVector<byte>(); break;
            case "Boolean": if (optional) await CheckVector<bool?>(); else await CheckVector<bool>(); break;
            case "String": await CheckVector<string>(); break;
            case "Guid": if (optional) await CheckVector<Guid?>(); else await CheckVector<Guid>(); break;
            case "DateTime": if (optional) await CheckVector<DateTime?>(); else await CheckVector<DateTime>(); break;
            default: throw new InvalidOperationException("Unregistered test vector domain.");
        }

        async Task CheckVector<TValue>()
        {
            foreach (var minimum in new[] { true, false })
            {
                var intent = minimum ? source.Min(h => CosmicQueryMetadata.Value<TValue>(h, "minmaxValue"))
                    : source.Max(h => CosmicQueryMetadata.Value<TValue>(h, "minmaxValue"));
                intent.IsError.Should().BeFalse(intent.Message);
                var result = await provider.ExecuteCosmicScalarAsync(MinMaxPlan(provider, intent.Result));
                if (expected.TryGetProperty("error", out var error))
                {
                    result.ErrorCode.Should().Be(error.GetString()); result.IsLoaded.Should().BeFalse();
                }
                else
                {
                    result.IsError.Should().BeFalse(result.Message + result.Exception); result.IsLoaded.Should().BeTrue();
                    var value = expected.GetProperty(minimum ? "min" : "max");
                    if (value.ValueKind == JsonValueKind.Null) Assert.Null((object?)result.Result);
                    else
                    {
                        var native = VectorStoredValue(domain, value);
                        var type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
                        if (native is long integer && type != typeof(long)) native = Convert.ChangeType(integer, type, CultureInfo.InvariantCulture);
                        Assert.Equal((TValue)native, result.Result);
                    }
                }
            }
        }
    }

    private static object VectorStoredValue(string domain, JsonElement value) => domain switch
    {
        "Int64" or "Int32" or "Byte" => long.Parse(value.GetString()!, CultureInfo.InvariantCulture),
        "Boolean" => value.GetBoolean(), "String" => value.GetString()!,
        "Guid" => Guid.ParseExact(value.GetString()!, "D"),
        "DateTime" => DateTime.ParseExact(value.GetString()!, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
        _ => throw new InvalidOperationException("Unsupported test storage domain.")
    };
}
