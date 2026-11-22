using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

public partial class HolonRepository
{
    private async Task<(Dictionary<Guid,CosmicJoinInputRow> Rows,long Count,long Bytes)> ReadNativeCqlKeyedSourceAsync<T>(
        MongoCqlProjectionCommand source,CosmicQuery<T> intent,IReadOnlyList<CosmicQueryPath> paths,
        IReadOnlyList<CosmicQueryPath> valuePaths,int maximumRows,int maximumBytes,
        IClientSessionHandle session,Func<AggregateOptions> options,CancellationToken cancellationToken) where T:IHolon
            {
                var result = new Dictionary<Guid, CosmicJoinInputRow>(); long measured = 0, count = 0;
                using var cursor = await _dbContext.Holon.AggregateAsync<BsonDocument>(session, source.Pipeline,
                    options(), cancellationToken).ConfigureAwait(false);
                while (await cursor.MoveNextAsync(cancellationToken).ConfigureAwait(false))
                    foreach (var document in cursor.Current)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var data = UnwrapNativeSelectedScalar(document, source.HasTraversalEnvelope);
                        var reported = JoinCount(data);
                        if (reported > maximumRows || result.Count >= maximumRows) throw new MongoCqlLimitException();
                        if (result.Count > 0 && count != reported) throw new InvalidDataException("Native join source count changed.");
                        count = reported;
                        if (data.ElementCount != 7 + paths.Count + intent.Orderings.Count
                            || !data.TryGetValue("id", out var idCell) || !data.TryGetValue("visible", out var visibleCell)
                            || !data.TryGetValue("creator", out var creatorCell))
                            throw new InvalidDataException("Native join source shape is invalid.");
                        var id = (Guid)DecodeNativeProjectionCell(idCell, typeof(Guid));
                        var visible = (bool)DecodeNativeProjectionCell(visibleCell, typeof(bool));
                        var creator = (Guid)DecodeNativeProjectionCell(creatorCell, typeof(Guid));
                        if (id == Guid.Empty || result.ContainsKey(id)
                            || intent.ReadAccess != null && !intent.ReadAccess.CanRead(id,visible,creator))
                            throw new InvalidDataException("Native join source identity/access proof is invalid.");
                        measured = checked(measured + 48);
                        var keys = Cells("keys", paths.Select(path => path.ValueType).ToArray(), false, out _);
                        var values = Cells("values", valuePaths.Select(path => path.ValueType).ToArray(), false, out _);
                        var order = Cells("ordering", intent.Orderings.Select(item => item.Path.ValueType).ToArray(), true, out var states);
                        if (measured > maximumBytes) throw new MongoCqlLimitException();
                        result.Add(id, new CosmicJoinInputRow(id, visible, creator, keys, values, order, states));

                        object[] Cells(string name, Type[] domains, bool allowAbsent, out int[] states)
                        {
                            if (!data.TryGetValue(name, out var cells) || !cells.IsBsonArray || cells.AsBsonArray.Count != domains.Length)
                                throw new InvalidDataException("Native join source cells do not match their selected shape.");
                            var decoded = new object[domains.Length]; states = new int[domains.Length];
                            for (var index = 0; index < decoded.Length; index++)
                            {
                                cancellationToken.ThrowIfCancellationRequested();
                                if (!cells[index].IsBsonDocument) throw new InvalidDataException("Native join source cell is invalid.");
                                var cell = cells[index].AsBsonDocument;
                                if (cell.ElementCount != 2 || !cell.TryGetValue("s", out var state) || !state.IsInt32
                                    || state.AsInt32 < 0 || state.AsInt32 > 2 || !cell.TryGetValue("v", out var value))
                                    throw new InvalidDataException("Native join source cell is ambiguous or has an invalid domain.");
                                states[index] = state.AsInt32;
                                var declared = domains[index];
                                if (state.AsInt32 < 2)
                                {
                                    if (!value.IsBsonNull || !allowAbsent && declared.IsValueType && Nullable.GetUnderlyingType(declared) == null)
                                        throw new InvalidDataException("Required native join source value is null or missing.");
                                }
                                else decoded[index] = DecodeNativeProjectionCell(value, Nullable.GetUnderlyingType(declared) ?? declared);
                                ReserveJoinCell(decoded[index], ref measured, maximumBytes);
                            }
                            return decoded;
                        }
                    }
                cancellationToken.ThrowIfCancellationRequested();
                if (count != result.Count) throw new InvalidDataException("Native join source count disagrees with its complete stream.");
                return (result, count, measured);
            }
}
