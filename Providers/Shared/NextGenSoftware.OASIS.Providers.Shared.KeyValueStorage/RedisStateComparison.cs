using System;
using System.Text;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Wire-independent Redis state comparisons. Tokens bind exact bytes to a key;
/// they are not monotonic revisions and do not detect ABA or provide multi-key transactions.</summary>
public static class RedisStateComparison
{
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private const string RevisionPrefix = "redis-state-v1:";
    public const string ReplaceScript = "if redis.call('GET',KEYS[1]) ~= ARGV[1] then return 0 end redis.call('SET',KEYS[1],ARGV[2]) return 1";
    public const string DeleteScript = "if redis.call('GET',KEYS[1]) ~= ARGV[1] then return 0 end return redis.call('DEL',KEYS[1])";

    public static string Observe(string key, byte[] value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        return Header(key) + Convert.ToBase64String(value);
    }

    public static byte[] Expected(string key, string revision)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(revision);
        var header = Header(key);
        if (!revision.StartsWith(header, StringComparison.Ordinal))
            throw new ArgumentException("Redis observation belongs to another key or format.", nameof(revision));
        var encoded = revision.Substring(header.Length);
        var bytes = Convert.FromBase64String(encoded);
        if (Convert.ToBase64String(bytes) != encoded)
            throw new ArgumentException("Redis observation is not canonical.", nameof(revision));
        return bytes;
    }

    public static string PrefixPattern(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        var pattern = new StringBuilder();
        foreach (var character in prefix)
        {
            if (character is '\\' or '*' or '?' or '[' or ']') pattern.Append('\\');
            pattern.Append(character);
        }
        return pattern.Append('*').ToString();
    }

    private static string Header(string key) => RevisionPrefix + Convert.ToBase64String(Utf8.GetBytes(key)) + ":";
}
