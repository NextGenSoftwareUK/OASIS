using System;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS;

/// <summary>One construction contract for DNA-based host registration and native demos.</summary>
public static class ActivityPubProviderConfiguration
{
    public static ActivityPubOASIS Create(ActivityPubOASISProviderSettings settings,
        string overrideConnectionString = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var configured = Origin(settings.BaseUrl);
        // A connection override must not move server credentials to another instance.
        if (overrideConnectionString != null && Origin(overrideConnectionString) != configured)
            throw new ArgumentException("ActivityPub override cannot change the configured instance origin.", nameof(overrideConnectionString));

        var token = string.Empty;
        if (!string.IsNullOrWhiteSpace(settings.AccessTokenEnvironmentVariable))
        {
            token = Environment.GetEnvironmentVariable(settings.AccessTokenEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(token))
                throw new ArgumentException("The configured ActivityPub access-token environment variable is missing.");
        }

        // Construction does not contact the instance. Explicit activation verifies its contract.
        return new ActivityPubOASIS(configured.GetLeftPart(UriPartial.Authority), token)
        {
            EnableCqlStatusKeySets = settings.EnableCqlStatusKeySets
        };
    }

    private static Uri Origin(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var origin)
            || (origin.Scheme != Uri.UriSchemeHttps && !(origin.Scheme == Uri.UriSchemeHttp && origin.IsLoopback))
            || origin.AbsolutePath != "/" || origin.UserInfo.Length != 0
            || origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new ArgumentException("ActivityPub requires an HTTPS instance origin (or loopback HTTP), without API path, credentials, query or fragment.");
        return origin;
    }
}
