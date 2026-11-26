using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.FlowOASIS
{
    /// <summary>Key-value records in the OasisKv Cadence contract. Reads are scripts; writes are transactions signed by the
    /// account holding the contract's Admin resource and acknowledged only once sealed successfully.</summary>
    public sealed class FlowLedgerKeyValueBackend : IKeyValueBackend
    {
        private readonly IFlowSdkBridge _sdk;
        private readonly string _endpoint;
        private readonly string _contract;
        private readonly object _signer;

        public FlowLedgerKeyValueBackend(IFlowSdkBridge sdk, string accessNodeRestUrl, string contractAddress, string signerAddress,
            string signerPrivateKeyHex, int signerKeyIndex = 0, string signatureAlgorithm = "ECDSA_P256")
        {
            _sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
            if (!Uri.TryCreate(accessNodeRestUrl, UriKind.Absolute, out var endpoint) || endpoint.Scheme is not ("http" or "https"))
                throw new ArgumentException("An http(s) Flow access node REST endpoint is required.", nameof(accessNodeRestUrl));
            _endpoint = endpoint.AbsoluteUri.TrimEnd('/');
            _contract = Address(contractAddress, nameof(contractAddress));
            if (string.IsNullOrWhiteSpace(signerPrivateKeyHex) || signerPrivateKeyHex.Length != 64 || !signerPrivateKeyHex.All(Uri.IsHexDigit))
                throw new ArgumentException("A 32-byte hex Flow signer key is required.", nameof(signerPrivateKeyHex));
            if (signatureAlgorithm is not ("ECDSA_P256" or "ECDSA_secp256k1"))
                throw new ArgumentException("Only ECDSA_P256 and ECDSA_secp256k1 signer keys are supported.", nameof(signatureAlgorithm));
            if (signerKeyIndex < 0) throw new ArgumentOutOfRangeException(nameof(signerKeyIndex));
            _signer = new { address = Address(signerAddress, nameof(signerAddress)), privateKey = signerPrivateKeyHex, keyId = signerKeyIndex, algorithm = signatureAlgorithm };
        }

        internal static string Address(string value, string name)
        {
            var text = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (text.StartsWith("0x", StringComparison.Ordinal)) text = text[2..];
            if (text.Length != 16 || !text.All(Uri.IsHexDigit)) throw new ArgumentException("A 16-hex-digit Flow address is required.", name);
            return "0x" + text;
        }

        /// <summary>Deploys OasisKv to the signer's account when absent (the signer then holds the Admin resource).</summary>
        public async Task<bool> EnsureDeployedAsync(CancellationToken cancellationToken = default)
        {
            var result = await _sdk.InvokeAsync(new { operation = "deploy", endpoint = _endpoint, signer = _signer }, cancellationToken).ConfigureAwait(false);
            return result.GetProperty("deployed").GetBoolean();
        }

        public async Task VerifyAsync(CancellationToken cancellationToken = default)
            => await _sdk.InvokeAsync(new { operation = "kvKeys", endpoint = _endpoint, contract = _contract, prefix = "\u0001" }, cancellationToken).ConfigureAwait(false);

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(key);
            var result = await _sdk.InvokeAsync(new { operation = "kvGet", endpoint = _endpoint, contract = _contract, key }, cancellationToken).ConfigureAwait(false);
            var value = result.GetProperty("value");
            return value.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.String => value.GetString(),
                _ => throw new InvalidOperationException("Flow returned a non-string OasisKv value.")
            };
        }

        public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            var result = await _sdk.InvokeAsync(new { operation = "kvKeys", endpoint = _endpoint, contract = _contract, prefix = prefix ?? string.Empty }, cancellationToken).ConfigureAwait(false);
            return result.GetProperty("keys").EnumerateArray()
                .Select(key => key.GetString() ?? throw new InvalidOperationException("Flow returned a null OasisKv key."))
                .OrderBy(key => key, StringComparer.Ordinal)
                .ToList();
        }

        public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);
            await _sdk.InvokeAsync(new { operation = "kvPut", endpoint = _endpoint, contract = _contract, signer = _signer, key, value }, cancellationToken).ConfigureAwait(false);
        }

        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(key);
            await _sdk.InvokeAsync(new { operation = "kvRemove", endpoint = _endpoint, contract = _contract, signer = _signer, key }, cancellationToken).ConfigureAwait(false);
        }
    }
}
