using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.QuestDBOASIS;

public partial class QuestDBOASIS
{
    private async Task EnsureTablesAsync()
    {
        using var connection = await OpenConnectionAsync();
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        foreach (var table in new[] { "oasis_avatars", "oasis_avatar_details", "oasis_holons" })
        {
            // Metadata only: never turn missing storage or an arbitrary database error into DDL/retry.
            using (var metadata = Command(connection,
                "SELECT id, table_name, designatedTimestamp, partitionBy, walEnabled, dedup, ttlValue, matView " +
                "FROM tables() WHERE table_name = @name LIMIT 2;"))
            {
                TextParameter(metadata, "name", table);
                using var reader = await metadata.ExecuteReaderAsync(deadline.Token);
                if (!await reader.ReadAsync(deadline.Token) || reader.GetInt32(0) <= 0 || reader.GetString(1) != table ||
                    reader.GetString(2) != "ts" || reader.GetString(3) != "DAY" || !reader.GetBoolean(4) ||
                    !reader.GetBoolean(5) || reader.GetInt32(6) != 0 || reader.GetBoolean(7) || await reader.ReadAsync(deadline.Token))
                    throw new InvalidDataException("QuestDB requires one explicit DAY/WAL/DEDUP table with designated ts and no TTL/materialized view.");
            }
            // Names come solely from the fixed private provider table list, not external SQL input.
            using var columns = Command(connection,
                $"SELECT \"column\", type, designated, upsertKey FROM table_columns('{table}') LIMIT 4;");
            using var schema = await columns.ExecuteReaderAsync(deadline.Token);
            var expectedNames = new[] { "id", "data", "ts" };
            var expectedTypes = new[] { "SYMBOL", "VARCHAR", "TIMESTAMP" };
            var index = 0;
            while (await schema.ReadAsync(deadline.Token))
            {
                if (index >= 3 || schema.GetString(0) != expectedNames[index] || schema.GetString(1) != expectedTypes[index] ||
                    schema.GetBoolean(2) != (index == 2) || schema.GetBoolean(3) != (index != 1))
                    throw new InvalidDataException("QuestDB schema or DEDUP(ts,id) key differs from the storage contract.");
                index++;
            }
            if (index != 3) throw new InvalidDataException("QuestDB requires exactly id/data/ts columns.");
        }
    }
}
