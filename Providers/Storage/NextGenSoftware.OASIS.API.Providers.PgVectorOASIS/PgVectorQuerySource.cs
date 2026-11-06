using System;
using System.Linq;
using Npgsql;

namespace NextGenSoftware.OASIS.API.Providers.PgVectorOASIS;

/// <summary>Exact public reads from one reviewed pgvector database over verified TLS.</summary>
public sealed class PgVectorQuerySource : PgVectorOASIS
{
    public PgVectorQuerySource(string host, string database, string username, string password, Guid probeId)
        : base(PrepareConnection(host, database, username, password, probeId), probeId) { }

    public static bool IsValidConfiguration(string? host, string? database, string? username, string? password, Guid probeId) =>
        host is { Length: > 0 and <= 253 } && Uri.CheckHostName(host) == UriHostNameType.Dns && host.Contains('.') &&
        host.Split('.').All(label => label.Length is >= 1 and <= 63 && label[0] != '-' && label[^1] != '-' &&
            label.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-')) &&
        char.IsAsciiLetterLower(host.Split('.')[^1][0]) && IsIdentifier(database) && IsIdentifier(username) &&
        password is { Length: > 0 and <= 1024 } && password.All(c => c > ' ' && c <= '~') &&
        probeId != Guid.Empty;

    private static bool IsIdentifier(string? value) =>
        value is { Length: > 0 and <= 63 } && char.IsAsciiLetterLower(value[0]) &&
        value.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '_');

    private static string PrepareConnection(string host, string database, string username, string password, Guid probeId)
    {
        if (!IsValidConfiguration(host, database, username, password, probeId))
            throw new ArgumentException("A reviewed pgvector host, database, restricted reader and public probe are required.");
        return new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = 5432,
            Database = database,
            Username = username,
            Password = password,
            SslMode = SslMode.VerifyFull,
            Timeout = 8,
            CommandTimeout = 15,
            SearchPath = "public"
        }.ConnectionString;
    }
}
