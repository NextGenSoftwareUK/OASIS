using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition;

public abstract class CheckedStorageFunction : FunctionMessage
{
    [Parameter("uint256", "entityId", 1)] public BigInteger EntityId { get; set; }
    [Parameter("string", "identity", 2)] public string Identity { get; set; }
    [Parameter("bool", "expectedPresent", 3)] public bool ExpectedPresent { get; set; }
    [Parameter("bytes32", "expectedInfoHash", 4)] public byte[] ExpectedInfoHash { get; set; }
    [Parameter("string", "info", 5)] public string Info { get; set; }
}

[Function("CreateAvatarChecked", "uint256")]
public sealed class CreateAvatarCheckedFunction : CheckedStorageFunction { }
[Function("CreateAvatarDetailChecked", "uint256")]
public sealed class CreateAvatarDetailCheckedFunction : CheckedStorageFunction { }
[Function("CreateHolonChecked", "uint256")]
public sealed class CreateHolonCheckedFunction : CheckedStorageFunction { }
