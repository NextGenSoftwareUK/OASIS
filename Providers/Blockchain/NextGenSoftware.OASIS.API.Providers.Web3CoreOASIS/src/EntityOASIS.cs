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

[FunctionOutput]
public class EntityOASISPageOutput : IFunctionOutputDTO
{
    [Parameter("tuple[]", "", 1)]
    public System.Collections.Generic.List<EntityOASIS> Entities {get;set;} = new();
}

[Struct("AvatarLifecycleRequest")]
public class AvatarLifecycleRequest
{
    [Parameter("uint256","entityId",1)] public BigInteger EntityId {get;set;}
    [Parameter("bytes32","externalId",2)] public byte[] ExternalId {get;set;} = [];
    [Parameter("bytes32","expectedAvatarHash",3)] public byte[] ExpectedAvatarHash {get;set;} = [];
    [Parameter("bytes32","expectedDetailHash",4)] public byte[] ExpectedDetailHash {get;set;} = [];
    [Parameter("bool","hasDetail",5)] public bool HasDetail {get;set;}
    [Parameter("bool","hardDelete",6)] public bool HardDelete {get;set;}
    [Parameter("bytes","avatarInfo",7)] public byte[] AvatarInfo {get;set;} = [];
    [Parameter("bytes","detailInfo",8)] public byte[] DetailInfo {get;set;} = [];
}
