using System;
using System.IO;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

/// <summary>Source-owned physical binding approval,using the caller's native transaction.</summary>
internal static class SqliteDestinationBindings
{
    internal static HolonReplicationDestinationBinding Read(SqliteConnection connection,SqliteTransaction transaction,
        CosmicStorageIdentity source,ProviderType target,out bool recorded)
    {
        recorded=false;
        using(var query=connection.CreateCommand())
        {
            query.Transaction=transaction;
            query.CommandText="SELECT RecordJson,RecordDigest FROM CosmicDestinationBinding WHERE SourceBinding=$source AND TargetAuthority=$target";
            query.Parameters.AddWithValue("$source",source.BindingFingerprint);
            query.Parameters.AddWithValue("$target",target.ToString());
            using var reader=query.ExecuteReader();
            if(reader.Read())
            {
                var binding=HolonDestinationBindingRecord.Read(reader.GetString(0),reader.GetString(1),source,target);
                if(reader.Read())throw new InvalidDataException("Duplicate native destination approval records.");
                recorded=true;
                return binding;
            }
        }
        // Upgrade bootstrap is read-only. The latest complete committed native
        // descriptor is the last verified identity; older transport cannot be
        // guessed from a target hash. Bound the scan and fail rather than guess.
        using var history=connection.CreateCommand();history.Transaction=transaction;
        history.CommandText="SELECT OperationId,TargetId,SourceId,SourceAuthority,ActorId,PayloadJson,PayloadDigest FROM CosmicHolonOutbox "
            +"WHERE SourceId=$source AND substr(TargetId,1,length($prefix))=$prefix COLLATE BINARY ORDER BY Sequence DESC LIMIT 257";
        history.Parameters.AddWithValue("$source",HolonReplicationRouteBinding.NativeSourceId(source));
        history.Parameters.AddWithValue("$prefix","hyperdrive-target-v2:"+target+":");
        using var rows=history.ExecuteReader();var count=0;
        while(rows.Read())
        {
            if(++count>256)throw new InvalidDataException("Destination binding history exceeds the bootstrap budget;explicit catalogue migration is required.");
            var operation=Guid.ParseExact(rows.GetString(0),"D");
            var route=SqliteHolonReplicationOutbox.ValidateFrozenPayload(rows.GetString(5),rows.GetString(6),operation,
                rows.GetString(2),rows.GetString(3),rows.GetString(4),rows.GetString(1));
            if(route==null)continue; // Explicit older transport has no durable descriptor.
            if(route.SourceStorage.BindingFingerprint!=source.BindingFingerprint || route.TargetStorage.Authority!=target)
                throw new InvalidDataException("Historical destination descriptor is outside this native source scope.");
            return new(route,operation,operation);
        }
        return null;
    }

    internal static void Approve(SqliteConnection connection,SqliteTransaction transaction,
        HolonReplicationEnrollment enrollment,Guid operation)
    {
        if(enrollment==null)return;
        foreach(var proposed in enrollment.NativeRoutes)
        {
            var known=Read(connection,transaction,proposed.SourceStorage,proposed.TargetStorage.Authority,out var recorded);
            if(known!=null && known.Route.TargetStorage.BindingFingerprint!=proposed.TargetStorage.BindingFingerprint)
                throw new HolonDestinationRebindRequiredException();
            if(recorded)continue;
            var binding=known ?? new HolonReplicationDestinationBinding(proposed,operation,operation);
            var json=HolonDestinationBindingRecord.Capture(binding);
            using var insert=connection.CreateCommand();insert.Transaction=transaction;
            insert.CommandText="INSERT INTO CosmicDestinationBinding(SourceBinding,TargetAuthority,RecordJson,RecordDigest) VALUES($source,$target,$json,$digest)";
            insert.Parameters.AddWithValue("$source",proposed.SourceStorage.BindingFingerprint);
            insert.Parameters.AddWithValue("$target",proposed.TargetStorage.Authority.ToString());
            insert.Parameters.AddWithValue("$json",json);
            insert.Parameters.AddWithValue("$digest",HolonDestinationBindingRecord.Digest(json));
            if(insert.ExecuteNonQuery()!=1)throw new InvalidDataException("Native destination approval was not recorded.");
        }
    }
}
