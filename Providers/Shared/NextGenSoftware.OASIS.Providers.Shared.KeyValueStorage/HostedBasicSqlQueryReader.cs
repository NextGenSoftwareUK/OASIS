using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Credentials and origin validation for fixed HTTPS SQL read adapters.</summary>
public static class HostedBasicSqlQueryReader
{
    public static bool IsValid(Uri origin, Guid probeId, string username, string password) =>
        origin is { IsAbsoluteUri: true, Scheme: "https", HostNameType: UriHostNameType.Dns, IsDefaultPort: true, IsLoopback: false } &&
        origin.AbsolutePath == "/" && origin.UserInfo.Length == 0 && origin.Query.Length == 0 && origin.Fragment.Length == 0 &&
        origin.Host.Length <= 253 && origin.Host.Contains('.') && origin.OriginalString == origin.GetLeftPart(UriPartial.Authority) &&
        origin.Host.Split('.').All(label => label.Length is >= 1 and <= 63 && label[0] != '-' && label[^1] != '-' &&
            label.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-')) &&
        char.IsAsciiLetterLower(origin.Host.Split('.')[^1][0]) &&
        probeId != Guid.Empty && username is { Length: >= 1 and <= 63 } && char.IsAsciiLetterLower(username[0]) &&
        username.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c is '_' or '-') &&
        password is { Length: >= 1 and <= 1024 } && password.All(c => c is >= '!' and <= '~');

    public static AuthenticationHeaderValue CreateAuthorization(Uri origin, Guid probeId, string username, string password)
    {
        if (!IsValid(origin, probeId, username, password))
            throw new ArgumentException("A reviewed SQL HTTPS origin, restricted reader and public probe are required.");
        return new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password)));
    }
}
