namespace ScGen.Lib.Shared.Extensions;

public static class UriExtensions
{
    public static string GetHost(this string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return uri.Host;
        }

        throw new ArgumentException($"Invalid URL: {url}");
    }

    public static int GetPort(this string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            if (!uri.IsDefaultPort)
                return uri.Port;
            return 0;
        }

        throw new ArgumentException($"Invalid URL: {url}");
    }
}