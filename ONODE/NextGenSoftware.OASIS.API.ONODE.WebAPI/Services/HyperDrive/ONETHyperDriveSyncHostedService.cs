using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.ONET;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive
{
    /// <summary>Owns the Full ONODE composition of the authenticated ONET HyperDrive sync host.</summary>
    public sealed class ONETHyperDriveSyncHostedService : IHostedService
    {
        private readonly ILogger<ONETHyperDriveSyncHostedService> _logger;
        private Core.Managers.ONETManager _manager;
        private IDisposable _endpoint;
        private bool _startedNetwork;
        private ONETCapabilityRegistry _capabilityRegistry;
        private CancellationTokenSource _capabilityRenewalCancellation;
        private Task _capabilityRenewalTask;
        private Task _capabilityReconciliationTask;

        public ONETHyperDriveSyncHostedService(ILogger<ONETHyperDriveSyncHostedService> logger) =>
            _logger = logger;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (OASISBootLoader.OASISBootLoader.OASISDNA == null)
            {
                var boot = await OASISBootLoader.OASISBootLoader.BootOASISASync(false).ConfigureAwait(false);
                if (boot == null || boot.IsError || !boot.Result)
                    throw new InvalidOperationException(boot?.Message ?? "OASIS DNA could not be loaded for ONET synchronization composition.");
            }
            var dna = OASISBootLoader.OASISBootLoader.OASISDNA;
            if (dna?.OASIS?.ONET?.EnableHyperDriveSyncHost != true)
            {
                _logger.LogInformation("ONET HyperDrive synchronization host is disabled in OASIS DNA.");
                return;
            }

            var providerResult = await OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync()
                .ConfigureAwait(false);
            if (providerResult == null || providerResult.IsError || providerResult.Result == null)
                throw new InvalidOperationException(providerResult?.Message ?? "The default provider could not be activated for ONET synchronization.");
            if (!(providerResult.Result is IHostedHyperDriveSyncStore syncStore) ||
                !(providerResult.Result is IHostedHyperDrivePeerBindingStore bindingStore))
                throw new InvalidOperationException($"Provider '{providerResult.Result.ProviderName}' must implement hosted sync and durable peer bindings.");

            _manager = await ONETController.GetOnetManagerStaticAsync().ConfigureAwait(false);
            var start = await _manager.StartNetworkAsync().ConfigureAwait(false);
            if (start == null || start.IsError || !start.Result)
                throw new InvalidOperationException(start?.Message ?? "ONET failed to start for HyperDrive synchronization.");
            _startedNetwork = true;

            var endpoint = new ONETRequestResponseEndpoint(_manager.CreateApplicationMessageChannel());
            _endpoint = endpoint;
            _ = new ONETHyperDriveSyncHost(endpoint, new HostedHyperDriveSyncProcessor(syncStore),
                new HostedPeerBindingAuthorizationResolver(bindingStore));
            _capabilityRegistry = new ONETCapabilityRegistry(endpoint);
            await PublishFullCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
            _capabilityRenewalCancellation = new CancellationTokenSource();
            _capabilityRenewalTask = RenewFullCapabilitiesAsync(_capabilityRenewalCancellation.Token);
            var peerRegistryIds = dna.OASIS.ONET.CapabilityRegistryNodeIds
                .Where(x => !string.Equals(x, dna.OASIS.ONET.NodeId, StringComparison.Ordinal)).ToArray();
            if (peerRegistryIds.Length > 0)
            {
                var peerDirectories = peerRegistryIds.Select(x => (IONETCapabilityDirectory)
                    new ONETCapabilityDirectoryClient(endpoint, x)).ToArray();
                var reconciler = new ONETCapabilityRegistryReconciler(_capabilityRegistry, peerDirectories,
                    dna.OASIS.ONET.CapabilityRegistryQuorum);
                _capabilityReconciliationTask = ReconcileCapabilityRegistriesAsync(reconciler,
                    TimeSpan.FromSeconds(dna.OASIS.ONET.CapabilityRegistryReconciliationSeconds),
                    _capabilityRenewalCancellation.Token);
            }
            _logger.LogInformation("Authenticated ONET HyperDrive synchronization host started.");
        }

        private async Task PublishFullCapabilitiesAsync(CancellationToken cancellationToken)
        {
            var onet = OASISBootLoader.OASISBootLoader.OASISDNA.OASIS.ONET;
            using var signingKey = ECDsa.Create();
            try { signingKey.ImportPkcs8PrivateKey(Convert.FromBase64String(onet.NodePrivateKey), out _); }
            catch (Exception ex) when (ex is FormatException || ex is CryptographicException)
            { throw new InvalidOperationException("The Full ONODE capability advertisement requires its configured ECDSA private key.", ex); }
            var providers = new ONETProviderCapabilitySource().GetEligibleCapabilities(onet.RemotelyAdvertisedProviderTypes);
            var advertisement = await ONETCapabilityProof.CreateAsync(onet.NodeId, onet.NodePublicKey,
                ONETNodeProfile.Full, new[] { "hyperdrive-sync-v3", "hosted-onode" }, providers,
                DateTime.UtcNow, TimeSpan.FromMinutes(10), (message, _) => Task.FromResult(
                    new OASISResult<string>(Convert.ToBase64String(signingKey.SignData(
                        Encoding.UTF8.GetBytes(message), HashAlgorithmName.SHA256)))), cancellationToken)
                .ConfigureAwait(false);
            if (advertisement == null || advertisement.IsError || advertisement.Result == null)
                throw new InvalidOperationException(advertisement?.Message ?? "The Full ONODE capability advertisement could not be signed.");
            var registered = _capabilityRegistry.RegisterLocal(advertisement.Result, onet.NodeId);
            if (registered == null || registered.IsError || !registered.Result)
                throw new InvalidOperationException(registered?.Message ?? "The Full ONODE capability advertisement could not be registered.");
        }

        private async Task RenewFullCapabilitiesAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(false);
                    try { await PublishFullCapabilitiesAsync(cancellationToken).ConfigureAwait(false); }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                    catch (Exception ex) { _logger.LogError(ex, "Full ONODE capability renewal failed; the current lease will expire unless a later renewal succeeds."); }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        }

        private async Task ReconcileCapabilityRegistriesAsync(ONETCapabilityRegistryReconciler reconciler,
            TimeSpan interval, CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    var reconciled = await reconciler.ReconcileAsync(cancellationToken).ConfigureAwait(false);
                    if (reconciled == null || reconciled.IsError)
                        _logger.LogError("ONET capability registry reconciliation failed: {Message}", reconciled?.Message);
                    else if (reconciled.IsWarning)
                        _logger.LogWarning("ONET capability registry reconciliation completed with reduced registry availability: {Message}", reconciled.Message);
                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_capabilityRenewalCancellation != null)
            {
                _capabilityRenewalCancellation.Cancel();
                if (_capabilityRenewalTask != null)
                    await _capabilityRenewalTask.ConfigureAwait(false);
                if (_capabilityReconciliationTask != null)
                    await _capabilityReconciliationTask.ConfigureAwait(false);
                _capabilityRenewalCancellation.Dispose();
            }
            _endpoint?.Dispose();
            if (_startedNetwork && _manager != null)
            {
                var stop = await _manager.StopNetworkAsync().ConfigureAwait(false);
                if (stop == null || stop.IsError)
                    _logger.LogError("ONET HyperDrive synchronization host did not stop cleanly: {Message}", stop?.Message);
            }
        }
    }
}
