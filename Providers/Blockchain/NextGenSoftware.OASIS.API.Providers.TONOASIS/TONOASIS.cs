using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS;

/// <summary>Native local TVM storage with mandatory shared private records.
/// Public-network transport, NFT/wallet and HyperDrive parity are not implied.</summary>
public sealed partial class TONOASIS : KeyValueStorageProviderBase, IDisposable
{
    private readonly TonProviderBackend _owner;
    private bool _disposed;

    public TONOASIS(TonSandboxStorageOptions options, Func<byte[]> encryptionKeyResolver)
        : this(new TonProviderBackend(PersistedPrivateKeyValueBackend.Create(
            new TonSandboxStorageBackend(options), encryptionKeyResolver))) { }

    private TONOASIS(TonProviderBackend owner) : base(owner)
    {
        _owner = owner;
        owner.IsActive = () => IsProviderActivated && !_disposed;
        ProviderName = "TONOASIS";
        ProviderDescription = "Native TON local TVM storage (explicit emulator source)";
        ProviderType = new(Core.Enums.ProviderType.TONOASIS);
        ProviderCategory = new(Core.Enums.ProviderCategory.Storage);
    }

    [Obsolete("Use explicit native TVM options and separately persisted storage encryption key.")]
    public TONOASIS(string hostUri, string chainPrivateKey, string contractAddress)
        : this(RejectEvmConfiguration(), () => throw new NotSupportedException()) { }

    private static TonSandboxStorageOptions RejectEvmConfiguration()
        => throw new NotSupportedException("EVM configuration cannot execute native TON. Supply explicit native TVM source options.");

    public override async Task<OASISResult<bool>> ActivateProviderAsync()
    {
        await _owner.Gate.WaitAsync().ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            await _owner.Private.VerifyAsync().ConfigureAwait(false);
            IsProviderActivated = true;
            return new OASISResult<bool>(true) { Message = "Native local TON TVM source verified." };
        }
        catch (Exception error)
        {
            IsProviderActivated = false;
            return new OASISResult<bool> { IsError = true, ErrorCount = 1, Result = false,
                ErrorCode = error is TonStorageException native ? native.Code : "TON_ACTIVATION_FAILED",
                Message = "Native TON source or private-record validation failed.", Exception = error };
        }
        finally { _owner.Gate.Release(); }
    }
    public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();
    public override async Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        await _owner.Gate.WaitAsync().ConfigureAwait(false);
        try { IsProviderActivated = false; return new OASISResult<bool>(true); }
        finally { _owner.Gate.Release(); }
    }
    public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();
    public void Dispose()
    {
        _owner.Gate.Wait();
        try { if (_disposed) return; IsProviderActivated = false; _disposed = true; _owner.Private.Dispose(); }
        finally { _owner.Gate.Release(); }
    }
}
