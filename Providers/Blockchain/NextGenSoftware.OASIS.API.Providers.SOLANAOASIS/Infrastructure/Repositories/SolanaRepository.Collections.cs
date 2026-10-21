namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

public partial class SolanaRepository
{
    // Explicit resource boundary: never return a silently truncated collection.
    private const int HistoryPagesLimit=100;
    private const ulong HistoryPageSize=100;

    public async Task<IReadOnlyList<(T Record,string Signature)>> GetAllAsync<T>(int version=0) where T:SolanaBaseDto,new()
    {
        if(version<0) throw new ArgumentOutOfRangeException(nameof(version));
        var writer=oasisAccount.PublicKey.Key;
        var expectedKind=new T().RecordKind;
        var selected=new Dictionary<Guid,(T Record,string Signature)>();
        var lastVersions=new Dictionary<Guid,int>();
        var seenSignatures=new HashSet<string>(StringComparer.Ordinal);
        string before=null;
        for(var page=0;page<HistoryPagesLimit;page++)
        {
            var response=await rpcClient.GetSignaturesForAddressAsync(writer,HistoryPageSize,before,null,Commitment.Finalized);
            if(response == null || !response.WasSuccessful || response.HttpStatusCode != HttpStatusCode.OK || response.Result == null)
                throw new InvalidOperationException("Solana finalized history query failed; no partial collection returned.");
            if(response.Result.Count==0)
                return selected.Values.Where(entry=>!entry.Record.IsDeleted).ToList();
            if(response.Result.Count>(int)HistoryPageSize)
                throw new InvalidOperationException("Solana history exceeded the requested page size.");
            foreach(var entry in response.Result)
            {
                if(entry == null || string.IsNullOrWhiteSpace(entry.Signature) || !seenSignatures.Add(entry.Signature))
                    throw new InvalidOperationException("Solana history contains invalid or repeated signatures.");
                before=entry.Signature;
                if(entry.Error != null) continue; // Native failed transactions never committed storage records.
                var transaction=await rpcClient.GetTransactionAsync(entry.Signature,Commitment.Finalized);
                if(transaction == null || !transaction.WasSuccessful || transaction.HttpStatusCode != HttpStatusCode.OK || transaction.Result?.Transaction?.Message == null)
                    throw new InvalidOperationException("Solana finalized history transaction is unavailable; no partial collection returned.");
                var message=transaction.Result.Transaction.Message;
                if(message.AccountKeys == null || message.AccountKeys.Length==0)
                    throw new InvalidOperationException("Solana history transaction has no account identities.");
                if(message.AccountKeys[0] != writer) continue; // Recipient activity is not storage written by this provider.
                if(message.Instructions == null) throw new InvalidOperationException("Solana history transaction has no instructions.");
                foreach(var instruction in message.Instructions)
                    if(instruction == null || instruction.ProgramIdIndex>=message.AccountKeys.Length)
                        throw new InvalidOperationException("Solana history instruction has an invalid program index.");
                var memos=message.Instructions.Where(instruction=>message.AccountKeys[instruction.ProgramIdIndex]=="MemoSq4gqABAXKb96qnH8TysNcWxMyWCqXgDLGmfcHr").ToList();
                if(memos.Count==0) continue;
                var ownedStorage=false;
                foreach(var memo in memos)
                {
                    var text=new UTF8Encoding(false,true).GetString(Encoders.Base58.DecodeData(memo.Data));
                    if(text.StartsWith(SolanaMemoFrames.CipherPrefix,StringComparison.Ordinal)
                        || text.StartsWith("oasis-solana-commit-",StringComparison.Ordinal)) ownedStorage=true;
                }
                if(!ownedStorage) continue; // An explicitly non-storage memo, not a guessed avatar.
                var payload=await DecodeStoredMemoAsync(entry.Signature,ValidateStoredTransaction(transaction.Result,entry.Signature,writer));
                var document=Newtonsoft.Json.Linq.JObject.Parse(payload);
                var kind=document.Value<string>("RecordKind");
                if(kind is not ("oasis.avatar.v1" or "oasis.avatar-detail.v1" or "oasis.holon.v1"))
                    throw new InvalidOperationException("Solana storage history contains an untyped or unknown record.");
                if(kind != expectedKind) continue;
                var record=DeserializeStoredRecord<T>(payload);
                if(lastVersions.TryGetValue(record.Id,out var newerVersion) && record.Version>=newerVersion)
                    throw new InvalidOperationException("Solana storage history contains conflicting or non-monotonic versions.");
                lastVersions[record.Id]=record.Version;
                if((version==0 || record.Version==version) && !selected.ContainsKey(record.Id))
                    selected.Add(record.Id,(record,entry.Signature));
            }
        }
        throw new InvalidOperationException("Solana storage history exceeds the bounded scan; no partial collection returned. A durable indexed history is required.");
    }
}
