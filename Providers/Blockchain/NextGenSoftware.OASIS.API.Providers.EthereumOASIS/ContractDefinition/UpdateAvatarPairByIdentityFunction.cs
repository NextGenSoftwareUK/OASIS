using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition
{
    [Function("UpdateAvatarPairByIdentity", "bool")]
    public sealed class UpdateAvatarPairByIdentityFunction : FunctionMessage
    {
        [Parameter("uint256","entityId",1)] public BigInteger EntityId { get; set; }
        [Parameter("string","avatarId",2)] public string AvatarId { get; set; }
        [Parameter("bytes32","expectedAvatarHash",3)] public byte[] ExpectedAvatarHash { get; set; }
        [Parameter("string","avatarInfo",4)] public string AvatarInfo { get; set; }
        [Parameter("bytes32","expectedDetailHash",5)] public byte[] ExpectedDetailHash { get; set; }
        [Parameter("string","detailInfo",6)] public string DetailInfo { get; set; }
    }
}
