using System;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    /// <summary>Builds the native conditional-cell adapter using this provider's verified
    /// network and existing SDK/owned-submission runner. OASIS records must additionally
    /// use the shared private-record layer; this factory alone does not enable legacy storage.</summary>
    public PolkadotRecordBackend CreateRecordBackend(string contractAccountId, string codeHash, string ownerAccountId)
    {
        if (!_isActivated || !IsGenesisHash(ReportedGenesisHash))
            throw new InvalidOperationException("Activate the verified network before configuring record storage.");
        return new PolkadotRecordBackend(_rpcEndpoint, ReportedGenesisHash, contractAccountId,
            codeHash, ownerAccountId, _privateKey, InvokePolkadotSdkAsync);
    }
}
