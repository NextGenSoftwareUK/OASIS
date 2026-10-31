using System.Text.Json;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public class NftStandardCompatibilityTests
{
    [Fact]
    public void TronStandardIsAppendedWithoutRenumberingExistingContracts()
    {
        var original=new[]{"ERC721","ERC1155","SPL","SuiObject","HederaHTS","OASISCosmWasm"};
        for(var i=0;i<original.Length;i++)Assert.Equal(i,(int)Enum.Parse<NFTStandardType>(original[i]));
        Assert.True(Enum.TryParse<NFTStandardType>("TRC721",out var tron));
        Assert.Equal(6,(int)tron);
        Assert.Equal("6",JsonSerializer.Serialize(tron));
        var options=new JsonSerializerOptions();options.Converters.Add(new JsonStringEnumConverter());
        Assert.Equal("\"TRC721\"",JsonSerializer.Serialize(tron,options));
        Assert.Equal(tron,JsonSerializer.Deserialize<NFTStandardType>("\"TRC721\"",options));
    }
}
