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
            throw new InvalidOperationException("Solana SDK-built legacy/v0 transaction exceeds the native 1232-byte packet limit; no oversized packet submitted.");
    }

    private async Task<string> WriteEncryptedRecordAsync(string ciphertext)
    {
        var blockHash=await GetWritableBlockHashAsync();
        // These are explicitly versioned wire formats, not a retry after a failed write.
        if(ciphertext.Length<=SolanaMemoFrames.MaximumMemoBytes)
        {
            var packet=BuildStorageMemo(ciphertext,blockHash);
            RequirePacketSize(packet);
            return await SubmitAndObserveStorageMemoAsync(packet, ciphertext);
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
                var memo=frames.ChunkMemo(index,index==0?null:committed[^1]);
                var packet=BuildStorageMemo(memo,blockHash);
                RequirePacketSize(packet);
                committed.Add(await SubmitAndObserveStorageMemoAsync(packet, memo));
            }
            var commitMemo=frames.CommitMemo(committed[^1]);
            var commitPacket=BuildStorageMemo(commitMemo,await GetWritableBlockHashAsync());
            RequirePacketSize(commitPacket);
            return await SubmitAndObserveStorageMemoAsync(commitPacket, commitMemo);
        }
        catch(Exception cause) when(committed.Count>0)
        {
            // Chunk transactions are real mutations, but not logical saved records.
            // Preserve all finalized references plus any attempted-signature inner error.
            throw new SolanaPartialMutationException(committed,cause);
        }
    }

    private async Task<string> SubmitAndObserveStorageMemoAsync(byte[] packet, string memo)
    {
        var signature = await SubmitAndConfirmAsync(packet);
        try
        {
            var transaction = await transactionReader(signature);
            var stored = ValidateStoredTransaction(transaction, signature, oasisAccount.PublicKey.Key);
            if (!stored.AsSpan().SequenceEqual(Encoding.UTF8.GetBytes(memo)))
                throw new InvalidOperationException("Finalized Solana storage memo differs from the submitted payload.");
            return signature;
        }
        catch (Exception error)
        {
            throw new SolanaTransactionReconciliationException(signature, error);
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
            var response=await transactionReader(signature);
            if(response==null)
                throw new SolanaMemoChunkUnavailableException(rootSignature, signature, index, commit.Count, commit.WriteId);
            var part=new UTF8Encoding(false,true).GetString(ValidateStoredTransaction(response,signature,oasisAccount.PublicKey.Key));
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
