using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

public sealed partial class SqliteHolonReplicationOutbox
{
    private const string InspectionColumns="Sequence,OperationId,TargetId,DeliveryState,AttemptCount,LastErrorCode,LastErrorMessage,RecoveryEvidenceJson,RecoveryEvidenceDigest,PayloadJson,PayloadDigest,SourceId,SourceAuthority,ActorId,LeaseUntilUtcTicks,NextAttemptUtcTicks,CompletedAtUtcTicks,LeaseToken,LeaseWorkerId";
    public Task<OASISResult<HolonDeliveryInspection>> InspectAsync(long sequence,CancellationToken cancellationToken=default)
        =>Execute(()=>{
            if(sequence<=0)throw new ArgumentOutOfRangeException(nameof(sequence));
            using var connection=Open();using var query=connection.CreateCommand();
            query.CommandText="SELECT "+InspectionColumns+" FROM CosmicHolonOutbox WHERE Sequence=$id";query.Parameters.AddWithValue("$id",sequence);
            using var reader=query.ExecuteReader();return ReadInspection(reader,false);
        },cancellationToken);
    public Task<OASISResult<HolonDeliveryInspection>> PeekNextAsync(string targetId,CancellationToken cancellationToken=default)
        =>Execute(()=>{
            ValidateIdentity(targetId);using var connection=Open();using var query=connection.CreateCommand();
            query.CommandText="SELECT "+InspectionColumns+" FROM CosmicHolonOutbox WHERE TargetId=$target AND DeliveryState<>'Completed' ORDER BY Sequence LIMIT 1";
            query.Parameters.AddWithValue("$target",targetId);using var reader=query.ExecuteReader();return ReadInspection(reader,true);
        },cancellationToken);
    private static OASISResult<HolonDeliveryInspection> ReadInspection(SqliteDataReader reader,bool verifyPayload)
    {
        if(!reader.Read())return new OASISResult<HolonDeliveryInspection>{Message="No matching delivery obligation exists."};
        var sequence=reader.GetInt64(0);var target=reader.GetString(2);
        if(sequence<=0 || !Guid.TryParseExact(reader.GetString(1),"D",out var operation) || operation==Guid.Empty
            || !Enum.TryParse<HolonDeliveryState>(reader.GetString(3),out var state) || !Enum.IsDefined(typeof(HolonDeliveryState),state)
            || state.ToString()!=reader.GetString(3) || reader.GetInt32(4)<0)
            throw new InvalidDataException("Invalid outbox delivery identity or state.");
        try{ValidateIdentity(target);}catch(ArgumentException cause){throw new InvalidDataException("Invalid outbox target.",cause);}
        var evidence=HolonRecoveryEvidenceJson.ReadStored(reader.IsDBNull(7) ? null:reader.GetString(7),reader.IsDBNull(8) ? null:reader.GetString(8),state);
        if(verifyPayload)
        {
            for(var index=14;index<=16;index++)if(reader.GetInt64(index)<0 || reader.GetInt64(index)>DateTime.MaxValue.Ticks)
                throw new InvalidDataException("Invalid outbox lifecycle timestamp.");
            if(state==HolonDeliveryState.InFlight && (reader.GetInt64(14)==0 || reader.GetInt32(4)==0
                || !Guid.TryParseExact(reader.GetString(17),"D",out var lease) || lease==Guid.Empty || string.IsNullOrWhiteSpace(reader.GetString(18)))
                || state==HolonDeliveryState.Completed && reader.GetInt64(16)==0 || state!=HolonDeliveryState.Completed && reader.GetInt64(16)!=0)
                throw new InvalidDataException("Outbox lifecycle disagrees with its state.");
        }
        if(verifyPayload)ValidateFrozenPayload(reader.GetString(9),reader.GetString(10),operation,reader.GetString(11),reader.GetString(12),reader.GetString(13));
        return new OASISResult<HolonDeliveryInspection>(new HolonDeliveryInspection(sequence,operation,target,state,reader.GetInt32(4),
            reader.IsDBNull(5) ? null:reader.GetString(5),reader.IsDBNull(6) ? null:reader.GetString(6),evidence));
    }
}
