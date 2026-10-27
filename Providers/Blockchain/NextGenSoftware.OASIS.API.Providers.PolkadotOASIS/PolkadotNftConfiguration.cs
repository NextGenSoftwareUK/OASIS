using System;
using System.Text.RegularExpressions;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

/// <summary>Reviewed PSP34 implementation identity and explicit read-only call origin.</summary>
public sealed class PolkadotNftConfiguration
{
    public string CodeHash { get; }
    public string QueryOrigin { get; }
    public PolkadotNftConfiguration(string codeHash,string queryOrigin)
    {
        foreach(var identity in new[]{codeHash,queryOrigin})
            if(identity==null||!Regex.IsMatch(identity,"^0x[0-9a-fA-F]{64}$",RegexOptions.CultureInvariant))
                throw new ArgumentException("PSP34 requires an explicit canonical32byte code hash and query origin.");
        CodeHash=codeHash.ToLowerInvariant();QueryOrigin=queryOrigin.ToLowerInvariant();
    }
    internal static PolkadotNftConfiguration FromEnvironment()
    {
        var code=Environment.GetEnvironmentVariable("OASIS_POLKADOT_NFT_CODE_HASH");
        var origin=Environment.GetEnvironmentVariable("OASIS_POLKADOT_NFT_QUERY_ORIGIN");
        return code==null&&origin==null?null:new PolkadotNftConfiguration(code,origin);
    }
}
