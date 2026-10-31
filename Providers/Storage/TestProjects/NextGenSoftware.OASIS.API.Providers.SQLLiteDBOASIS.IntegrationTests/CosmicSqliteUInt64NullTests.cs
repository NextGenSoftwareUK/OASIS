using Microsoft.Data.Sqlite;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public sealed class CosmicSqliteUInt64NullTests
{
    [Theory]
    [InlineData("null", null)]
    [InlineData("0", "00000000000000000000")]
    [InlineData("18446744073709551615", "18446744073709551615")]
    public void CanonicalUnsignedKeysPreserveJsonNullAndExactNumbers(string token, string? expected)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT " + SqliteCqlUInt64.Column("cell") + " FROM json_each($json) AS cell";
        command.Parameters.AddWithValue("$json", "[" + token + "]");
        var value = command.ExecuteScalar();
        Assert.Equal(expected, value is DBNull ? null : value);
    }
}
