using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    internal sealed class NeoCqlCompositionDecoder<T, TResult> : NeoCqlDetachedSourceDecoder where T : IHolon
    {
        private readonly CosmicProjectionQuery<T, TResult> query;
        private readonly CosmicProjectionCompositionExecutionLimits limits;
        private readonly List<CosmicProjectionInputRow> input = new();
        private readonly HashSet<Guid> sourceIds = new();
        private readonly List<List<Guid>> stages = new();
        private readonly List<long> counts = new();
        private readonly HashSet<Guid> stageIds = new();
        private long sourceCount, work, inputBytes, proofBytes;
        private bool header;
        private readonly bool terminal;
        internal long? Counterexamples { get; private set; }

        internal NeoCqlCompositionDecoder(CosmicProjectionQuery<T, TResult> query,
            CosmicProjectionCompositionExecutionLimits limits, Action check, bool terminal = false) : base(check)
        { this.query = query; this.limits = limits; this.terminal = terminal; }

        internal void Add(IReadOnlyDictionary<string, object> record)
        {
            check();
            if (record.Count != 6 || !record.TryGetValue("proof", out var proof))
                throw Invalid("Native composition envelope is invalid.");
            var phase = Number(record, "phase");
            var position = Number(record, "position");
            var sourcePosition = Number(record, "sourcePosition");
            var count = Number(record, "nativeCount");
            var reportedWork = Number(record, "stageWork");
            if (!header)
            {
                if (phase != -2 || position != -1 || sourcePosition != -1 || proof != null || count < 0 || reportedWork < 0)
                    throw Invalid("Native composition requires one leading count/work header.");
                if (count > limits.MaximumInputRows || reportedWork > limits.MaximumStageRows)
                    throw new NeoCqlLimitException("Composition exceeds remaining input/stage work.");
                sourceCount = count; work = reportedWork; header = true;
                return;
            }
            if (reportedWork != work || Counterexamples.HasValue) throw Invalid("Native selected count/work proof changed.");
            if (terminal && phase == query.ResultStages.Count)
            {
                if (count < 0) throw new NeoCqlLimitException("All reduction exceeds remaining native work.");
                if (position != -1 || sourcePosition != -1 || proof != null || input.Count != sourceCount
                    || stages.Count != query.ResultStages.Count || stages.Count > 0 && stages.Last().Count != counts.Last()
                    || count > (stages.Count == 0 ? input.Count : stages.Last().Count))
                    throw Invalid("Native All reduction is outside its complete selected sequence.");
                Counterexamples = count; return;
            }
            if (count < 0) throw Invalid("Native composition count is negative.");
            if (phase == -1)
            {
                if (stages.Count != 0 || position != input.Count || sourcePosition != position || count != sourceCount || input.Count >= sourceCount)
                    throw Invalid("Native composition source proof position/count is invalid.");
                var row = Source(proof, query.Source.ReadAccess, Array.Empty<Type>(),
                    query.Columns.Select(column => column.Path.ValueType).ToArray(),
                    query.Source.Orderings.Select(item => item.Path.ValueType).ToArray(), ref inputBytes, limits.MaximumInputPayloadBytes);
                if (!sourceIds.Add(row.Id)) throw Invalid("Native composition source identity is duplicated.");
                input.Add(new CosmicProjectionInputRow(row.Id, row.IsPublic, row.CreatedByAvatarId,
                    row.Values.ToArray(), row.OrderingValues.ToArray(), row.OrderingStates.ToArray()));
                return;
            }
            if (phase < 0 || phase >= query.ResultStages.Count || proof != null || input.Count != sourceCount)
                throw Invalid("Native composition stage is outside its complete captured source.");
            if (position == -1)
            {
                if (phase != stages.Count || sourcePosition != -1 || count > input.Count
                    || stages.Count > 0 && stages.Last().Count != counts.Last())
                    throw Invalid("Native composition stage header/order is invalid.");
                if (phase == query.ResultStages.Count - 1 && count > limits.MaximumResults)
                    throw new NeoCqlLimitException("Composition exceeds remaining output rows.");
                stages.Add(new List<Guid>()); counts.Add(count); stageIds.Clear();
                return;
            }
            if (phase != stages.Count - 1 || position != stages.Last().Count || position >= counts.Last()
                || count != counts.Last() || sourcePosition < 0 || sourcePosition >= input.Count)
                throw Invalid("Native composition stage position/count is invalid.");
            var id = input[(int)sourcePosition].Id;
            if (!stageIds.Add(id)) throw Invalid("Native composition stage repeats an identity.");
            proofBytes = checked(proofBytes + 16);
            if (proofBytes > limits.MaximumProofPayloadBytes) throw new NeoCqlLimitException("Composition exceeds remaining proof bytes.");
            stages.Last().Add(id);
        }

        internal CosmicProjectionCompositionPacket Complete()
        {
            check();
            if (!header || input.Count != sourceCount || stages.Count != query.ResultStages.Count
                || stages.Count == 0 && !terminal || stages.Count > 0 && stages.Last().Count != counts.Last()
                || terminal && !Counterexamples.HasValue)
                throw Invalid("Native composition stream is incomplete.");
            long measuredWork = stages.Count == 0 ? 0 : input.Count;
            for (var index = 0; index < stages.Count - 1; index++) measuredWork = checked(measuredWork + stages[index].Count);
            if (measuredWork != work) throw Invalid("Native composition cumulative work disagrees with its complete proof.");
            var rows = input.ToDictionary(row => row.Id);
            var output = new List<CosmicProjectionCompositionRow>();
            long outputBytes = 0;
            var final = stages.Count == 0 ? input.Select(row => row.Id).ToList() : stages.Last();
            if (final.Count > limits.MaximumResults || terminal && checked(work + final.Count) > limits.MaximumStageRows)
                throw new NeoCqlLimitException("All evidence or terminal scan exceeds its remaining work.");
            foreach (var id in final)
            {
                check();
                if (terminal) outputBytes = checked(outputBytes + 16);
                var values = rows[id].Values.ToArray();
                foreach (var value in values) Reserve(value, ref outputBytes, limits.MaximumPayloadBytes);
                output.Add(new CosmicProjectionCompositionRow(id, values));
            }
            if (terminal && checked(proofBytes + outputBytes) > limits.MaximumProofPayloadBytes)
                throw new NeoCqlLimitException("All evidence exceeds its remaining proof-byte budget.");
            check();
            return new CosmicProjectionCompositionPacket(input, stages.Select(ids => new CosmicProjectionStageProof(ids)).ToArray(),
                output, sourceCount, work);
        }
    }
}
