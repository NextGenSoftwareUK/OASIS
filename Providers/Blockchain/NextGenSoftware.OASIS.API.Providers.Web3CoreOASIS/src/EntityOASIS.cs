using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

[Struct("EntityOASIS")]
public class EntityOASIS
{
    [Parameter("uint256", "EntityId", 1)]
    public BigInteger EntityId { get; set; }

    [Parameter("bytes32", "ExternalId", 2)]
    public byte[] ExternalId { get; set; } = [];

    [Parameter("bytes", "Info", 3)]
    public byte[] Info { get; set; } = [];
}

[FunctionOutput]
public class EntityOASISOutput : IFunctionOutputDTO
{
    [Parameter("tuple", "", 1)]
    public EntityOASIS Entity { get; set; } = new();
}
