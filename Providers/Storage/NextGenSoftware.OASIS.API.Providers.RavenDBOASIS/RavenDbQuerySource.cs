using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.RavenDBOASIS;

/// <summary>One HTTPS teaching database with a caller-owned read-only certificate.</summary>
public sealed class RavenDbQuerySource : RavenDBOASIS, IDisposable
{
    public RavenDbQuerySource(Uri origin, Guid probe, X509Certificate2 certificate)
        : base(new[] { ValidateOrigin(origin) }, "oasis", probe,
            certificate ?? throw new ArgumentNullException(nameof(certificate)))
    {
    }

    protected override bool RequiresPublicQueryRecords => true;

    public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

    public override Task<OASISResult<bool>> ActivateProviderAsync() => ActivateQuerySourceAsync(default);

    public void Dispose() => DeActivateProviderAsync().GetAwaiter().GetResult();

    private static string ValidateOrigin(Uri origin)
    {
        if (origin is not { IsAbsoluteUri: true, Scheme: "https", AbsolutePath: "/", IsLoopback: false } ||
            origin.UserInfo.Length != 0 || origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new ArgumentException("Use a single credential-free HTTPS RavenDB origin.", nameof(origin));
        return origin.GetLeftPart(UriPartial.Authority);
    }
}
