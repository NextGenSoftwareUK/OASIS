using System;
using System.Numerics;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ImmutableZkEVMOASIS;

public class ImmutableZkEVMOASIS : Web3CoreOASISBaseProvider
{
    private const string MainnetRpc = "https://rpc.immutable.com";
    private const int MainnetChainId = 13371;
    private const int TestnetChainId = 13473;

    public ImmutableZkEVMOASIS(string rpcUrl = MainnetRpc)
        : this(rpcUrl ?? MainnetRpc, "", "", MainnetChainId)
    {
    }

    public ImmutableZkEVMOASIS(string rpcUrl, string chainPrivateKey, string contractAddress, BigInteger chainId)
        : base(rpcUrl, chainPrivateKey, contractAddress, ValidateChainId(chainId))
    {
        ProviderName = "ImmutableZkEVMOASIS";
        ProviderDescription = "Immutable zkEVM storage and wallet provider using Web3Core";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ImmutableZkEVMOASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain);
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
    }

    public new OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long lat, long lon, int radius, HolonType holonType = HolonType.All)
        => base.GetHolonsNearMe(lat, lon, radius, holonType);

    private static BigInteger ValidateChainId(BigInteger chainId)
    {
        if (chainId != MainnetChainId && chainId != TestnetChainId)
            throw new ArgumentOutOfRangeException(nameof(chainId), "Use Immutable zkEVM mainnet or testnet.");

        return chainId;
    }
}
