using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using TronNet.Protocol;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

/// <summary>Read-only SDK observations for fee-model review, NEVER an all-fee maximum.
/// Equal surrounding heads do not prove an atomic or future governance snapshot.</summary>
public sealed class TronFeeScheduleObservation
{
    public string GenesisBlockId { get; }
    public string BeforeBlockId { get; }
    public string AfterBlockId { get; }
    public long BeforeHeight { get; }
    public long AfterHeight { get; }
    public long BeforeTimestampMilliseconds { get; }
    public long AfterTimestampMilliseconds { get; }
    public long NextMaintenanceMilliseconds { get; }
    public DateTimeOffset ObservedAtUtc { get; }
    public IReadOnlyDictionary<string,long> Parameters { get; }
    public bool HeadUnchanged => BeforeBlockId==AfterBlockId;
    public bool IsFeeAdmissionProof => false;

    internal TronFeeScheduleObservation(string genesis,BlockExtention before,BlockExtention after,
        ChainParameters parameters,long maintenance,DateTimeOffset observed)
    {
        static void ValidateHead(BlockExtention head){
            if(head?.Blockid.Length!=32||head.BlockHeader?.RawData==null||head.BlockHeader.RawData.Number<0||head.BlockHeader.RawData.Timestamp<=0)
                throw new InvalidDataException("Complete TRON block identity, height and timestamp required for fee observations.");
            // Block IDs embed their height in their first eight bytes, big endian.
            if(System.Buffers.Binary.BinaryPrimitives.ReadInt64BigEndian(head.Blockid.Span[..8])!=head.BlockHeader.RawData.Number)
                throw new InvalidDataException("TRON observed block identity and height disagree.");
            var digest=System.Security.Cryptography.SHA256.HashData(head.BlockHeader.RawData.ToByteArray());
            System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(digest.AsSpan(0,8),head.BlockHeader.RawData.Number);
            if(!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(digest,head.Blockid.Span))
                throw new InvalidDataException("TRON observed block header does not match its block ID.");
        }
        ValidateHead(before);ValidateHead(after);
        if(after.BlockHeader.RawData.Number<before.BlockHeader.RawData.Number||after.BlockHeader.RawData.Timestamp<before.BlockHeader.RawData.Timestamp
            ||before.BlockHeader.RawData.Number==after.BlockHeader.RawData.Number&&!before.Blockid.Equals(after.Blockid))
            throw new InvalidDataException("TRON head changed incompatibly during fee observations; inspect chain state.");
        if(parameters==null||parameters.ChainParameter.Count is <1 or >256||maintenance<=after.BlockHeader.RawData.Timestamp)
            throw new InvalidDataException("Bounded chain parameters and future maintenance observation required.");
        var values=new Dictionary<string,long>(StringComparer.Ordinal);
        foreach(var entry in parameters.ChainParameter){
            if(string.IsNullOrWhiteSpace(entry.Key)||entry.Key.Length>128||System.Linq.Enumerable.Any(entry.Key,char.IsControl)||!values.TryAdd(entry.Key,entry.Value))
                throw new InvalidDataException("Chain parameter names must be unique and bounded; missing values are never inferred.");
        }
        GenesisBlockId=genesis;BeforeBlockId=Convert.ToHexString(before.Blockid.Span).ToLowerInvariant();AfterBlockId=Convert.ToHexString(after.Blockid.Span).ToLowerInvariant();
        BeforeHeight=before.BlockHeader.RawData.Number;AfterHeight=after.BlockHeader.RawData.Number;
        BeforeTimestampMilliseconds=before.BlockHeader.RawData.Timestamp;AfterTimestampMilliseconds=after.BlockHeader.RawData.Timestamp;
        NextMaintenanceMilliseconds=maintenance;ObservedAtUtc=observed;Parameters=new ReadOnlyDictionary<string,long>(values);
    }
}

public sealed partial class TronNativeTransferClient
{
    /// <summary>Reads official SDK network parameters without storage activation, signing or broadcast.
    /// A successful observation does not authorize fees or guarantee their future values.</summary>
    public async Task<TronFeeScheduleObservation> ReadFeeScheduleAsync(CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();await VerifyNetworkAsync(token);
        var rpc=_wallet.GetProtocol();var headers=_wallet.GetHeaders();
        var before=await rpc.GetNowBlock2Async(new EmptyMessage(),headers,DateTime.UtcNow.AddSeconds(15),token);
        var parameters=await rpc.GetChainParametersAsync(new EmptyMessage(),headers,DateTime.UtcNow.AddSeconds(15),token);
        var maintenance=await rpc.GetNextMaintenanceTimeAsync(new EmptyMessage(),headers,DateTime.UtcNow.AddSeconds(15),token);
        var after=await rpc.GetNowBlock2Async(new EmptyMessage(),headers,DateTime.UtcNow.AddSeconds(15),token);
        token.ThrowIfCancellationRequested();
        return new TronFeeScheduleObservation(_genesisBlockId,before,after,parameters,maintenance.Num,DateTimeOffset.UtcNow);
    }
}
