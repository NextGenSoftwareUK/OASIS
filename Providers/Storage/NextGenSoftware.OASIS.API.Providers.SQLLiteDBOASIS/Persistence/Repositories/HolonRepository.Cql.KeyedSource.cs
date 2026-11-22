using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private async Task<(Dictionary<long,CosmicJoinInputRow> Rows,long Count,long Bytes)> ReadNativeCqlKeyedSourceAsync<T>(
        CosmicQuery<T> query,string sql,IReadOnlyList<CosmicQueryPath> paths,IReadOnlyList<CosmicQueryPath> valuePaths,
        int maximumRows,int maximumBytes,Dictionary<Guid,string> entityStorageKeys,
        Func<string,SqliteCommand> commandFactory,Action check,CancellationToken cancellationToken) where T:IHolon
                    {
                        var result = new Dictionary<long, CosmicJoinInputRow>();
                        long measured = 0, count = 0;
                        using var command = commandFactory("SELECT *,count(*) OVER () AS nativeCount FROM (" + sql + ") ORDER BY position");
                        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            check();
                            count = reader.GetInt64(reader.FieldCount - 1);
                            if (count > maximumRows) throw new SqliteCqlResourceException("Native join source exceeds its remaining row budget.");
                            var position = reader.GetInt64(0);
                            var storageKey = reader.GetString(1);
                            var id = (Guid)DecodeProjectionCell(storageKey, typeof(Guid));
                            if (entityStorageKeys != null && !entityStorageKeys.TryAdd(id,storageKey))
                                throw new InvalidDataException("Native selected source has duplicate logical identities.");
                            var visible = (bool)DecodeProjectionCell(reader.GetValue(2), typeof(bool));
                            var creator = reader.IsDBNull(3) ? Guid.Empty : (Guid)DecodeProjectionCell(reader.GetValue(3), typeof(Guid));
                            measured = checked(measured + 48);
                            var offset = 4;
                            var keys = paths.Select(path => Cell(path.ValueType)).ToArray();
                            var values = valuePaths.Select(path => Cell(path.ValueType)).ToArray();
                            var states = new int[query.Orderings.Count];
                            var order = new object[states.Length];
                            for (var index = 0; index < states.Length; index++)
                            {
                                states[index] = checked((int)reader.GetInt64(offset));
                                order[index] = Cell(query.Orderings[index].Path.ValueType, allowAbsent: true);
                            }
                            if (measured > maximumBytes) throw new SqliteCqlResourceException("Native join source exceeds its remaining byte budget.");
                            result.Add(position, new CosmicJoinInputRow(id, visible, creator, keys, values, order, states));

                            object Cell(Type declared, bool allowAbsent = false)
                            {
                                check();
                                var state = reader.GetInt64(offset++);
                                var raw = reader.GetValue(offset++);
                                if (state is < 0 or > 2) throw new InvalidDataException("Join source scalar path is ambiguous or invalid.");
                                var value = state == 2 ? DecodeProjectionCell(raw, declared) : null;
                                if (value == null && !allowAbsent && declared.IsValueType && Nullable.GetUnderlyingType(declared) == null)
                                    throw new InvalidDataException("A required join source scalar is null or missing.");
                                Reserve(value, ref measured, maximumBytes);
                                return value;
                            }
                        }
                        check();
                        if(count!=result.Count)throw new InvalidDataException("Native keyed source count differs from its complete stream.");
                        return (result,count,measured);
                    }
}
