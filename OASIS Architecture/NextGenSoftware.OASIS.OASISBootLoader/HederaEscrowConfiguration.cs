using System;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

namespace NextGenSoftware.OASIS.OASISBootLoader;

internal static class HederaEscrowConfiguration
{
    internal static HederaTokenEscrowConfiguration Create(HashgraphOASISProviderSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new()
        {
            Protocol=settings.TokenEscrowProtocol switch {
                "aggregate-v1"=>HederaTokenEscrowProtocol.AggregateV1,
                "id-bound-v1"=>HederaTokenEscrowProtocol.IdBoundV1,
                _=>throw new ArgumentException("Explicit supported Hedera TokenEscrowProtocol is required; unknown settings cannot select lock-all.")},
            ContractId=settings.TokenEscrowContractId,RuntimeCodeHash=settings.TokenEscrowRuntimeCodeHash,
            GasLimit=settings.TokenEscrowGasLimit,MaxFeeTinybars=settings.TokenEscrowMaxFeeTinybars
        };
    }
}
