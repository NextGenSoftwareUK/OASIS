using System;
using System.Text.RegularExpressions;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

/// <summary>Explicit reviewed per-deposit PSP22 escrow; never a service wallet or pallet Assets ID.</summary>
public sealed class PolkadotTokenEscrowConfiguration
{
    public string ContractAccountId { get; }
    public string CodeHash { get; }
    public PolkadotTokenEscrowConfiguration(string contractAccountId,string codeHash)
    {
        foreach(var value in new[]{contractAccountId,codeHash})
            if(value==null||!Regex.IsMatch(value,"^0x[0-9a-fA-F]{64}$",RegexOptions.CultureInvariant))
                throw new ArgumentException("Token escrow requires explicit AccountId32 and reviewed code hash.");
        ContractAccountId=contractAccountId.ToLowerInvariant();CodeHash=codeHash.ToLowerInvariant();
    }
    internal static PolkadotTokenEscrowConfiguration FromEnvironment()
    {
        var account=Environment.GetEnvironmentVariable("OASIS_POLKADOT_TOKEN_ESCROW_CONTRACT");
        var code=Environment.GetEnvironmentVariable("OASIS_POLKADOT_TOKEN_ESCROW_CODE_HASH");
        return account==null&&code==null?null:new PolkadotTokenEscrowConfiguration(account,code);
    }
}
