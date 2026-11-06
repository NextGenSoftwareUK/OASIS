using System;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.ScyllaDBOASIS;

/// <summary>Bounded reads from an approved public ScyllaDB TLS node and caller-owned cluster CA.</summary>
public sealed class ScyllaDbQuerySource : ScyllaDBOASIS, ICosmicReadOnlyQueryActivation, IDisposable
{
    private readonly IPAddress _address;
    private readonly string _host;
    private readonly string _password;
    private readonly X509Certificate2 _root;
    private readonly Guid _probe;
    private readonly SemaphoreSlim _lifecycle = new(1);

    public ScyllaDbQuerySource(IPAddress address, string host, string password, X509Certificate2 root, Guid probe)
        : base(string.Empty)
    {
        if (address == null || !NativeConnectionGuard.IsPublicAddress(address))
            throw new ArgumentException("Use an approved public ScyllaDB address.", nameof(address));
        if (Uri.CheckHostName(host) != UriHostNameType.Dns)
            throw new ArgumentException("Use the DNS name in the ScyllaDB certificate.", nameof(host));
        if (string.IsNullOrWhiteSpace(password) || password.Length > 1024 || password.Any(char.IsControl))
            throw new ArgumentException("Use the dedicated ScyllaDB reader password.", nameof(password));
        if (root == null || root.HasPrivateKey ||
            !root.Extensions.OfType<X509BasicConstraintsExtension>().Any(constraints => constraints.CertificateAuthority))
            throw new ArgumentException("Use the public cluster CA certificate.", nameof(root));
        if (probe == Guid.Empty) throw new ArgumentException("An existing teaching probe is required.", nameof(probe));
        _address = address;
        _host = host;
        _password = password;
        _root = root;
        _probe = probe;
    }

    protected override bool RequiresPublicQueryRecords => true;

    public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync(default);

    public async Task<OASISResult<bool>> ActivateQuerySourceAsync(CancellationToken cancellationToken)
    {
        try { await _lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException cause)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "CQL_QUERY_CANCELLED", Exception = cause };
        }
        try { return await ActivatePublicQuerySourceAsync(BuildCluster, _probe, cancellationToken).ConfigureAwait(false); }
        finally { _lifecycle.Release(); }
    }

    public override async Task<OASISResult<bool>> DeActivateProviderAsync()
    {
        await _lifecycle.WaitAsync().ConfigureAwait(false);
        try { return await base.DeActivateProviderAsync().ConfigureAwait(false); }
        finally { _lifecycle.Release(); }
    }

    public void Dispose() => DeActivateProviderAsync().GetAwaiter().GetResult();

    private ICluster BuildCluster()
    {
        var endpoint = new IPEndPoint(_address, 9142);
        var tls = new SSLOptions(SslProtocols.Tls12 | SslProtocols.Tls13, false,
            (_, certificate, chain, errors) => VerifyServer(certificate, chain, errors))
            .SetHostNameResolver(address => address.Equals(_address) ? _host :
                throw new InvalidOperationException("ScyllaDB attempted to use an unreviewed TLS address."));

        return Cluster.Builder()
            .AddContactPoint(endpoint)
            .WithAddressTranslator(new ExactNode(endpoint))
            .WithMetadataSyncOptions(new MetadataSyncOptions().SetMetadataSyncEnabled(false))
            .WithSSL(tls)
            .WithCredentials("cosmic_reader", _password)
            .WithSocketOptions(new SocketOptions().SetConnectTimeoutMillis(8000).SetReadTimeoutMillis(15000))
            .WithRetryPolicy(FallthroughRetryPolicy.Instance)
            .Build();
    }

    private bool VerifyServer(X509Certificate? certificate, X509Chain? suppliedChain, SslPolicyErrors errors)
    {
        if (certificate == null || (errors & ~SslPolicyErrors.RemoteCertificateChainErrors) != SslPolicyErrors.None)
            return false;
        using var leaf = X509CertificateLoader.LoadCertificate(certificate.GetRawCertData());
        using var chain = new X509Chain();
        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        chain.ChainPolicy.CustomTrustStore.Add(_root);
        chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.1"));
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
        chain.ChainPolicy.DisableCertificateDownloads = true;
        if (suppliedChain != null)
            foreach (var element in suppliedChain.ChainElements)
                chain.ChainPolicy.ExtraStore.Add(element.Certificate);
        return chain.Build(leaf);
    }

    private sealed class ExactNode(IPEndPoint approved) : IAddressTranslator
    {
        public IPEndPoint Translate(IPEndPoint endpoint) =>
            endpoint.Address.Equals(approved.Address) ? approved :
                throw new InvalidOperationException("ScyllaDB advertised an unreviewed node.");
    }
}
