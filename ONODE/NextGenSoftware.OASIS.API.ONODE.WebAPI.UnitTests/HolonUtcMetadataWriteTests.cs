using System;
using System.Text.Json;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class HolonUtcMetadataWriteTests
{
    [Fact]
    public void ConvertsOnlyExplicitKeysAndPreservesExactUtcTicks()
    {
        var request = JsonSerializer.Deserialize<SaveHolonRequest>(
            "{\"UtcMetadataKeys\":[\"stamp\",\"empty\"],\"Holon\":{\"MetaData\":{\"stamp\":\"2021-01-01T00:00:00.0000001Z\",\"empty\":null,\"text\":\"2021-01-01T00:00:00.0000001Z\"}}}");

        Assert.False(HolonWriteEndpoint.BindUtcMetadata(request).IsError);
        var timestamp = Assert.IsType<DateTime>(request.Holon.MetaData["stamp"]);
        Assert.Equal(DateTimeKind.Utc, timestamp.Kind);
        Assert.Equal(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(1), timestamp);
        Assert.Null(request.Holon.MetaData["empty"]);
        Assert.Equal(JsonValueKind.String, Assert.IsType<JsonElement>(request.Holon.MetaData["text"]).ValueKind);
    }

    [Theory]
    [InlineData("2021-01-01T00:00:00.00000001Z")]
    [InlineData("2021-01-01T00:00:00+00:00")]
    public void RejectsAnInvalidSecondKeyWithoutPartiallyChangingMetadata(string invalid)
    {
        var request = JsonSerializer.Deserialize<SaveHolonRequest>(
            "{\"UtcMetadataKeys\":[\"valid\",\"invalid\"],\"Holon\":{\"MetaData\":{\"valid\":\"2021-01-01T00:00:00Z\",\"invalid\":\"" + invalid + "\"}}}");

        Assert.Equal("COSMIC_WRITE_INTENT_INVALID", HolonWriteEndpoint.BindUtcMetadata(request).ErrorCode);
        Assert.IsType<JsonElement>(request.Holon.MetaData["valid"]);
    }
}
