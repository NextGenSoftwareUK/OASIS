using System.Security.Cryptography;
using System.Threading;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

public sealed record SolanaChainObservedHolon(CosmicChainQueryRecordReceipt Receipt,string CanonicalJson);
public sealed record SolanaChainObservedRange(CosmicChainQueryCheckpoint Checkpoint,
    CosmicChainQueryFinalizedHead Head,IReadOnlyList<SolanaChainObservedHolon> Records);

public partial class SolanaRepository
{
    /// <summary>Exact configured writer/network/holon catalogue, not an authorization grant.</summary>
    public static string ChainHolonCatalogueHash(string writer,string genesis)
        =>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("oasis-solana-holon-range/v1\n"+writer+"\n"+genesis)));

    private void RequireChainIndexSource(CosmicChainQuerySource source)
    {
        if(source==null||source.SourceProvider!=ProviderType.SolanaOASIS||source.ExecutionProvider!=ProviderType.SQLLiteDBOASIS
            ||source.CatalogueHash!=ChainHolonCatalogueHash(oasisAccount.PublicKey.Key,source.NetworkIdentity))
            throw new InvalidOperationException("The explicit source must bind this Solana writer/network holon catalogue and SQLite execution owner.");
    }

    private async Task<CosmicChainQueryPosition> ReadFinalizedPositionAsync(ulong slot,CancellationToken token)
    {
        var block=await rpcClient.GetBlockAsync(slot,Commitment.Finalized,TransactionDetailsFilterType.None,false).WaitAsync(token);
        if(block==null||!block.WasSuccessful||block.HttpStatusCode!=HttpStatusCode.OK||block.Result==null
            ||string.IsNullOrWhiteSpace(block.Result.Blockhash)||Encoders.Base58.DecodeData(block.Result.Blockhash).Length!=32)
            throw new InvalidOperationException("The finalized block identity is unavailable or invalid.");
        return new CosmicChainQueryPosition(checked((long)slot),block.Result.Blockhash);
    }

    /// <summary>Independent exact finalized-source re-read, for comparison with an indexed receipt.</summary>
    public async Task<OASISResult<SolanaChainObservedHolon>> ObserveFinalizedHolonAsync(CosmicChainQuerySource source,
        string signature,CancellationToken cancellationToken=default)
    {
        try{
            RequireChainIndexSource(source);cancellationToken.ThrowIfCancellationRequested();
            var genesis=await rpcClient.GetGenesisHashAsync().WaitAsync(cancellationToken);
            if(genesis==null||!genesis.WasSuccessful||genesis.HttpStatusCode!=HttpStatusCode.OK||genesis.Result!=source.NetworkIdentity)
                throw new InvalidOperationException("Solana source network differs or is unavailable.");
            var transaction=await transactionReader(signature).WaitAsync(cancellationToken);
            if(transaction==null)throw new InvalidOperationException("Original finalized source transaction is unavailable.");
            var position=await ReadFinalizedPositionAsync(transaction.Slot,cancellationToken);
            var payload=await DecodeStoredMemoAsync(signature,ValidateStoredTransaction(transaction,signature,oasisAccount.PublicKey.Key)).WaitAsync(cancellationToken);
            var observed=MakeChainObservation(source,position,signature,payload);
            return new OASISResult<SolanaChainObservedHolon>(observed){IsLoaded=true,LoadedCount=1};
        }catch(Exception error){return ChainObservationError<SolanaChainObservedHolon>(error);}
    }

    private static SolanaChainObservedHolon MakeChainObservation(CosmicChainQuerySource source,CosmicChainQueryPosition position,string signature,string payload)
    {
        var dto=DeserializeStoredRecord<SolanaHolonDto>(payload);
        var holon=dto.GetHolon();
        if(dto.IsDeleted!=holon.IsDeleted)throw new InvalidOperationException("Canonical Solana deletion fields disagree.");
        // Never invent version identity or silently normalize historical version-zero records.
        var canonical=OasisJson.Serialize(holon);
        var identity=new CosmicChainQueryRecordIdentity(holon.Id,holon.Version,holon.VersionId,holon.IsDeleted,
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))));
        return new(new CosmicChainQueryRecordReceipt(source,position,signature,identity),canonical);
    }

    /// <summary>
    /// Observe complete configured coverage through one captured finalized head, with all holon revisions.
    /// RPC observations are not cryptographic inclusion proofs. This method never signs/submits/publishes.
    /// </summary>
    public async Task<OASISResult<SolanaChainObservedRange>> ObserveFinalizedHolonRangeAsync(CosmicChainQuerySource source,
        CancellationToken cancellationToken=default)
    {
        try{
            RequireChainIndexSource(source);cancellationToken.ThrowIfCancellationRequested();
            var genesis=await rpcClient.GetGenesisHashAsync().WaitAsync(cancellationToken);
            if(genesis==null||!genesis.WasSuccessful||genesis.HttpStatusCode!=HttpStatusCode.OK||genesis.Result!=source.NetworkIdentity)
                throw new InvalidOperationException("Solana source network differs or is unavailable.");
            async Task Retention(){
                var first=await rpcClient.GetFirstAvailableBlockAsync().WaitAsync(cancellationToken);
                if(first==null||!first.WasSuccessful||first.HttpStatusCode!=HttpStatusCode.OK||first.Result>(ulong)source.CoverageStartSlot)
                    throw new InvalidOperationException("Node retention cannot prove the configured source range; no complete checkpoint returned.");
            }
            await Retention();
            var headReply=await rpcClient.GetSlotAsync(Commitment.Finalized).WaitAsync(cancellationToken);
            if(headReply==null||!headReply.WasSuccessful||headReply.HttpStatusCode!=HttpStatusCode.OK||headReply.Result<(ulong)source.CoverageStartSlot)
                throw new InvalidOperationException("Finalized head does not cover the configured range.");
            var head=await ReadFinalizedPositionAsync(headReply.Result,cancellationToken);
            var observedAt=DateTimeOffset.UtcNow;
            var blocks=new Dictionary<ulong,CosmicChainQueryPosition>{{headReply.Result,head}};
            var records=new List<SolanaChainObservedHolon>();var signatures=new HashSet<string>(StringComparer.Ordinal);
            string before=null;var priorSlot=ulong.MaxValue;long payloadBytes=0;var complete=false;
            for(var page=0;page<HistoryPagesLimit&&!complete;page++){
                var reply=await rpcClient.GetSignaturesForAddressAsync(oasisAccount.PublicKey.Key,HistoryPageSize,before,null,Commitment.Finalized).WaitAsync(cancellationToken);
                if(reply==null||!reply.WasSuccessful||reply.HttpStatusCode!=HttpStatusCode.OK||reply.Result==null||reply.Result.Count>(int)HistoryPageSize)
                    throw new InvalidOperationException("Finalized writer history is unavailable or exceeds its requested bound.");
                if(reply.Result.Count==0){complete=true;break;}
                foreach(var entry in reply.Result){
                    cancellationToken.ThrowIfCancellationRequested();
                    if(entry==null||string.IsNullOrWhiteSpace(entry.Signature)||!signatures.Add(entry.Signature)||entry.Slot>priorSlot)
                        throw new InvalidOperationException("History signatures are repeated, invalid or not descending by slot.");
                    priorSlot=entry.Slot;before=entry.Signature;
                    if(entry.Slot<(ulong)source.CoverageStartSlot){complete=true;break;}
                    if(entry.Slot>headReply.Result||entry.Error!=null)continue;
                    var transaction=await transactionReader(entry.Signature).WaitAsync(cancellationToken);
                    if(transaction?.Transaction?.Message==null||transaction.Slot!=entry.Slot)
                        throw new InvalidOperationException("Finalized transaction is unavailable or differs from its history slot.");
                    var message=transaction.Transaction.Message;
                    if(message.AccountKeys==null||message.AccountKeys.Length==0||message.Instructions==null)
                        throw new InvalidOperationException("Finalized transaction lacks canonical accounts/instructions.");
                    if(message.AccountKeys[0]!=oasisAccount.PublicKey.Key)continue;
                    var storage=false;
                    foreach(var instruction in message.Instructions){
                        if(instruction==null||instruction.ProgramIdIndex>=message.AccountKeys.Length)throw new InvalidOperationException("Invalid native program index.");
                        if(message.AccountKeys[instruction.ProgramIdIndex]!="MemoSq4gqABAXKb96qnH8TysNcWxMyWCqXgDLGmfcHr")continue;
                        var text=new UTF8Encoding(false,true).GetString(Encoders.Base58.DecodeData(instruction.Data));
                        if(text.StartsWith(SolanaMemoFrames.CipherPrefix,StringComparison.Ordinal)||text.StartsWith("oasis-solana-commit-",StringComparison.Ordinal))storage=true;
                    }
                    if(!storage)continue;
                    var payload=await DecodeStoredMemoAsync(entry.Signature,ValidateStoredTransaction(transaction,entry.Signature,oasisAccount.PublicKey.Key)).WaitAsync(cancellationToken);
                    var document=OasisJson.ParseStoredObject(payload);var kind=document.Value<string>("RecordKind");
                    if(kind is not ("oasis.holon.v1" or "oasis.avatar.v1" or "oasis.avatar-detail.v1"))throw new InvalidOperationException("Unknown canonical storage kind.");
                    if(kind!="oasis.holon.v1")continue;
                    if(!blocks.TryGetValue(entry.Slot,out var position))blocks[entry.Slot]=position=await ReadFinalizedPositionAsync(entry.Slot,cancellationToken);
                    var row=MakeChainObservation(source,position,entry.Signature,payload);payloadBytes+=Encoding.UTF8.GetByteCount(row.CanonicalJson);
                    if(records.Count==1000||payloadBytes>16*1024*1024)throw new InvalidOperationException("Complete source batch exceeds its index publication budget.");
                    records.Add(row);
                }
            }
            if(!complete)throw new InvalidOperationException("Writer history exceeds the bounded range scan; no partial checkpoint returned.");
            foreach(var history in records.GroupBy(row=>row.Receipt.Record.Id)){
                var revisions=history.OrderBy(row=>row.Receipt.Record.Version).ToArray();
                if(revisions.Select(row=>row.Receipt.Record.Version).Distinct().Count()!=revisions.Length
                    ||revisions.Select(row=>row.Receipt.Record.VersionId).Distinct().Count()!=revisions.Length)
                    throw new InvalidOperationException("Source history repeats a logical revision/version identity.");
                for(var index=1;index<revisions.Length;index++)
                    if(revisions[index].Receipt.Position.Slot<revisions[index-1].Receipt.Position.Slot)
                        throw new InvalidOperationException("Source version advancement contradicts finalized slot order.");
            }
            await Retention(); // Pruning during observation cannot become successful empty/partial coverage.
            cancellationToken.ThrowIfCancellationRequested();
            var checkpoint=new CosmicChainQueryCheckpoint(source,head,source.CoverageStartSlot);
            return new OASISResult<SolanaChainObservedRange>(new(checkpoint,new(source.SourceProvider,source.NetworkIdentity,head,observedAt),records.AsReadOnly())){IsLoaded=true,LoadedCount=records.Count};
        }catch(Exception error){return ChainObservationError<SolanaChainObservedRange>(error);}
    }

    private static OASISResult<T> ChainObservationError<T>(Exception error)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=error is OperationCanceledException?"CQL_SOURCE_OBSERVATION_CANCELLED":"CQL_SOURCE_OBSERVATION_FAILED",
            Message="Canonical source observation failed; no partial records/checkpoint returned.",Exception=error};
}
