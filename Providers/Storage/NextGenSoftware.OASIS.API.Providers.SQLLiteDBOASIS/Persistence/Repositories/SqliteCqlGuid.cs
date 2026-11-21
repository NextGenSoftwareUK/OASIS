using System.Linq;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

internal static class SqliteCqlGuid
{
    private static readonly string Pattern = string.Join("-", new[] { 8, 4, 4, 4, 12 }
        .Select(count => string.Concat(Enumerable.Repeat("[0-9a-fA-F]", count))));

    internal static string TextDomain(string value)
        => "typeof(" + value + ") = 'text' AND length(CAST(" + value + " AS BLOB)) = 36 AND "
            + value + " GLOB '" + Pattern + "'";
}
