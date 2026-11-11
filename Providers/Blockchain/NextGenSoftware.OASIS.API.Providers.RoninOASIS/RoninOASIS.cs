using System;
using System.Numerics;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.RoninOASIS;

public class RoninOASIS : Web3CoreOASISBaseProvider
{
    private const string MainnetRpc = "https://api.roninchain.com/rpc";
    private const int MainnetChainId = 2020;
    private const int SaigonChainId = 202601;

    public RoninOASIS(string rpcUrl = MainnetRpc)
        : this(rpcUrl ?? MainnetRpc, "", "", MainnetChainId)
    {
    }

    public RoninOASIS(string rpcUrl, string chainPrivateKey, string contractAddress, BigInteger chainId)
        : base(rpcUrl, chainPrivateKey, contractAddress, ValidateChainId(chainId))
    {
        ProviderName = "RoninOASIS";
        ProviderDescription = "Ronin storage and wallet provider using Web3Core";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.RoninOASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain);
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
        ProviderCapabilities.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));
    }

    public new OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long lat, long lon, int radius, HolonType holonType = HolonType.All)
        => base.GetHolonsNearMe(lat, lon, radius, holonType);

    private static BigInteger ValidateChainId(BigInteger chainId)
    {
        if (chainId != MainnetChainId && chainId != SaigonChainId)
            throw new ArgumentOutOfRangeException(nameof(chainId), "Use Ronin mainnet or Saigon testnet.");

        return chainId;
    }
}
