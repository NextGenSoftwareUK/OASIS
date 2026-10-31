using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS;

public partial class PolkadotOASIS
{
    /// <summary>Builds the native conditional-cell adapter using this provider's verified
    /// network and existing SDK/owned-submission runner. OASIS records must additionally
    /// use the shared private-record layer. Configured public storage uses that layer
    /// through the canonical shared storage engine.</summary>
    public PolkadotRecordBackend CreateRecordBackend(string contractAccountId, string codeHash, string ownerAccountId)
    {
        EnsureRecordActivation();
        return new PolkadotRecordBackend(_rpcEndpoint, ReportedGenesisHash, contractAccountId,
            codeHash, ownerAccountId, _privateKey, InvokePolkadotSdkAsync);
    }

    public Task<T> WithRecordStoragePreparationAsync<T>(Func<Task<T>> operation,
        Func<string,string,PreparedSubmissionServer> factory, CancellationToken token=default)
        => _recordStorage.WithPreparationAsync(operation,factory,token);

    private void EnsureRecordActivation()
    {
        ObjectDisposedException.ThrowIf(_isDisposed,this);
        if (!_isActivated || !IsGenesisHash(ReportedGenesisHash))
            throw new InvalidOperationException("Activate the verified network before configuring record storage.");
    }
}
