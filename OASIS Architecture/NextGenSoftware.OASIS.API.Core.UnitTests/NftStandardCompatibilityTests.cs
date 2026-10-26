using NextGenSoftware.OASIS.API.Core.Enums;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class NftStandardCompatibilityTests
{
    [Theory]
    [InlineData(NFTStandardType.ERC721, 0)]
    [InlineData(NFTStandardType.ERC1155, 1)]
    [InlineData(NFTStandardType.SPL, 2)]
    [InlineData(NFTStandardType.SuiObject, 3)]
    [InlineData(NFTStandardType.HederaHTS, 4)]
    [InlineData(NFTStandardType.OASISCosmWasm, 5)]
    [InlineData(NFTStandardType.TRC721, 6)]
    [InlineData(NFTStandardType.PSP34, 7)]
    public void PersistedStandardNumbersRemainStable(NFTStandardType standard, int serializedValue)
        => Assert.Equal(serializedValue, (int)standard);
}
