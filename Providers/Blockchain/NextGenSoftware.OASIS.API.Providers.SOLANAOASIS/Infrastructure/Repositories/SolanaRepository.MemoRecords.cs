namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;

public partial class SolanaRepository
{
    private byte[] BuildStorageMemo(string memo,string blockHash) => new TransactionBuilder()
        .SetRecentBlockHash(blockHash).SetFeePayer(oasisAccount)
        .AddInstruction(SetComputeUnitLimit(1_400_000))
        .AddInstruction(MemoProgram.NewMemoV2(memo)).Build(oasisAccount);

    private static void RequirePacketSize(byte[] packet)
    {
        if(packet.Length>SolanaMemoFrames.TransactionBytesLimit)
            throw new InvalidOperationException("Solana storage transaction exceeds the native 1232-byte packet limit; no oversized packet submitted.");
    }

    private async Task<string> WriteEncryptedRecordAsync(string ciphertext)
    {
        var blockHash=await GetWritableBlockHashAsync();
        // These are explicitly versioned wire formats, not a retry after a failed write.
        if(ciphertext.Length<=SolanaMemoFrames.MaximumMemoBytes)
        {
            var packet=BuildStorageMemo(ciphertext,blockHash);
            RequirePacketSize(packet);
            return await SubmitAndConfirmAsync(packet);
        }
        var frames=new SolanaMemoFrames(ciphertext);
        // Admit every possible packet size before the first mutation. Native signatures
        // are at most 88 Base58 characters; FF bytes attain that maximum length.
        var maximumSignature=Encoders.Base58.EncodeData(Enumerable.Repeat((byte)255,64).ToArray());
        for(var index=0;index<frames.Count;index++)
            RequirePacketSize(BuildStorageMemo(frames.ChunkMemo(index,index==0?null:maximumSignature),blockHash));
        RequirePacketSize(BuildStorageMemo(frames.CommitMemo(maximumSignature),blockHash));
        var committed=new List<string>();
        try
        {
            for(var index=0;index<frames.Count;index++)
            {
                if(index>0)blockHash=await GetWritableBlockHashAsync();
                var packet=BuildStorageMemo(frames.ChunkMemo(index,index==0?null:committed[^1]),blockHash);
                RequirePacketSize(packet);
                committed.Add(await SubmitAndConfirmAsync(packet));
            }
            var commitPacket=BuildStorageMemo(frames.CommitMemo(committed[^1]),await GetWritableBlockHashAsync());
            RequirePacketSize(commitPacket);
            return await SubmitAndConfirmAsync(commitPacket);
        }
        catch(Exception cause) when(committed.Count>0)
        {
            // Chunk transactions are real mutations, but not logical saved records.
            // Preserve all finalized references plus any attempted-signature inner error.
            throw new SolanaPartialMutationException(committed,cause);
        }
    }

    private async Task<string> DecodeStoredMemoAsync(string rootSignature,byte[] memo)
    {
        var text=new UTF8Encoding(false,true).GetString(memo);
        if(text.StartsWith(SolanaMemoFrames.CipherPrefix,StringComparison.Ordinal))return DecodeEntity(memo);
        var commit=SolanaMemoFrames.ParseCommit(text); // Bounds checked before following references.
        var visited=new HashSet<string>(StringComparer.Ordinal){rootSignature};
        var reverse=new List<(string Signature,string Memo)>();
        var signature=commit.LastSignature;
        for(var index=commit.Count-1;index>=0;index--)
        {
            if(!visited.Add(signature))throw new System.IO.InvalidDataException("Solana memo chain contains a repeated transaction reference.");
            var response=await rpcClient.GetTransactionAsync(signature,Commitment.Finalized);
            if(response==null || !response.WasSuccessful || response.HttpStatusCode!=HttpStatusCode.OK)
                throw new InvalidOperationException("Finalized Solana memo chunk is unavailable; no partial record returned.");
            var part=new UTF8Encoding(false,true).GetString(ValidateStoredTransaction(response.Result,signature,oasisAccount.PublicKey.Key));
            var frame=SolanaMemoFrames.ParseChunk(part);
            if(frame.WriteId!=commit.WriteId || frame.Index!=index || frame.Count!=commit.Count
                || frame.Length!=commit.Length || frame.Hash!=commit.Hash)
                throw new System.IO.InvalidDataException("Solana memo chunk differs from its commit binding.");
            reverse.Add((signature,part));signature=frame.PreviousSignature;
        }
        reverse.Reverse();
        return DecodeEntity(Encoding.ASCII.GetBytes(SolanaMemoFrames.Join(commit,reverse)));
    }
}
