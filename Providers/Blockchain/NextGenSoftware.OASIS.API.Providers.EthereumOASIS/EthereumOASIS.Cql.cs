using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.RPC.Eth.DTOs;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

public partial class EthereumOASIS : ICosmicQueryStorageProvider
{
    public ProviderType QueryProviderType => Core.Enums.ProviderType.EthereumOASIS;
    public CosmicQueryCapabilities QueryCapabilities => EthereumCqlCompiler.Capabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(
        CosmicQueryPlan<T> plan, int maximumResults = 100,
        CancellationToken cancellationToken = default) where T : IHolon
    {
        OASISResult<IReadOnlyList<IHolon>> Error(string code, string message, Exception cause = null)
            => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message, Exception = cause };

        try
        {
            // Admit the complete expression before activation or any RPC. Unsupported
            // business predicates must never become an in-memory scan of the chain.
            if (plan == null || plan.Targets.Count != 1 || plan.Targets[0].Provider != QueryProviderType)
                return Error("CQL_PLAN_TARGET_UNAVAILABLE", "One explicit Ethereum target is required.");
            if (maximumResults < 1 || maximumResults > 10000)
                return Error("CQL_QUERY_LIMIT", "A result budget between 1 and 10,000 is required.");
            var admitted = QueryCapabilities.AdmitPlan(plan);
            if (admitted.IsError) return Error(admitted.ErrorCode, admitted.Message);
            var selection = EthereumCqlCompiler.Compile(plan.Query, QueryProviderType);
            if (selection.IsError) return Error(selection.ErrorCode, selection.Message);
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsProviderActivated)
                return Error("CQL_PROVIDER_INACTIVE", "Activate the configured Ethereum provider before querying.");

            var rows = new List<IHolon>();
            if (selection.Result.Count != 0)
            {
                // Pin every lookup to one observed block number. This does not claim
                // finality, reorg immunity or a snapshot shared with another provider.
                var block = await Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync()
                    .WaitAsync(cancellationToken).ConfigureAwait(false);
                var atBlock = new BlockParameter(block);
                foreach (var id in selection.Result)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var response = await _nextGenSoftwareOasisService.GetHolonByIdQueryAsync(
                        HashUtility.GetNumericHash(id.ToString()), atBlock)
                        .WaitAsync(cancellationToken).ConfigureAwait(false);
                    var native = response?.ReturnValue1
                        ?? throw new InvalidDataException("Native Ethereum lookup returned no acknowledgement.");
                    // A zero native tuple is the contract's explicit absent-record value.
                    if (native.EntityId == 0 && string.IsNullOrEmpty(native.Info)) continue;
                    var row = ReadPrivateStorageVersion<Holon>("holon", native.EntityId, native.Info, id, 0);
                    if (row == null || !plan.Query.ReadAccess.CanRead(row))
                        throw new InvalidDataException("Native Ethereum record differs from the authorized identity.");
                    if (row.IsDeleted) continue;
                    row.Children = Array.Empty<IHolon>();
                    rows.Add(row);
                }
            }

            var page = rows.Skip(plan.Query.Offset).Take(plan.Query.Limit ?? rows.Count).ToArray();
            if (page.Length > maximumResults)
                return Error("CQL_QUERY_LIMIT", "The complete page exceeds the result budget.");
            cancellationToken.ThrowIfCancellationRequested();
            return new(Array.AsReadOnly<IHolon>(page)) { IsLoaded = true, LoadedCount = page.Length };
        }
        catch (OperationCanceledException cause)
        { return Error("CQL_QUERY_CANCELLED", "Ethereum query cancelled; no partial result published.", cause); }
        catch (Exception cause)
        { return Error("CQL_QUERY_FAILED", "Native Ethereum query failed; no partial result published.", cause); }
    }
}
