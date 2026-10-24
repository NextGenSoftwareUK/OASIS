using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Amazon.DynamoDBv2.Model;
using Amazon.Util;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

/// <summary>Pure projection of official SDK capacity evidence. Does not dispatch, price,
/// aggregate alternative views, authorize spending or invent missing measurements.</summary>
internal static class DynamoCapacityUsage
{
    private static readonly UTF8Encoding StrictUtf8=new(false,true);
    internal static IReadOnlyList<NativeResourceUsageObservation> CaptureTransaction(string provider,string logicalRequestId,string? nativeRequestId,
        string region,IReadOnlyList<string> requestedTables,IReadOnlyList<ConsumedCapacity>? capacities,DateTime observedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(requestedTables);
        if(requestedTables.Count==0)throw new ArgumentException("Original transaction tables are required.",nameof(requestedTables));
        var tables=new Dictionary<string,int>(StringComparer.Ordinal);
        foreach(var table in requestedTables){ArgumentException.ThrowIfNullOrWhiteSpace(table);tables.TryGetValue(table,out var count);tables[table]=count+1;}
        var returned=new Dictionary<string,List<(int Index,ConsumedCapacity Capacity)>>(StringComparer.Ordinal);
        if(capacities!=null)for(var index=0;index<capacities.Count;index++){
            var capacity=capacities[index];
            if(capacity==null||string.IsNullOrWhiteSpace(capacity.TableName)||!tables.TryGetValue(capacity.TableName,out var actions))
                throw new InvalidDataException("Transaction capacity must identify original requested tables.");
            if(!returned.TryGetValue(capacity.TableName,out var entries))returned[capacity.TableName]=entries=new();
            if(entries.Count>=actions)throw new InvalidDataException("Transaction capacity exceeds the original table's action count.");
            entries.Add((index,capacity));
        }
        var rows=new List<NativeResourceUsageObservation>();
        foreach(var table in tables.Keys){
            if(!returned.TryGetValue(table,out var entries)){
                rows.AddRange(Capture(provider,logicalRequestId,nativeRequestId,region,table,null,observedAtUtc));continue;
            }
            foreach(var entry in entries)foreach(var row in Capture(provider,logicalRequestId,nativeRequestId,region,table,entry.Capacity,observedAtUtc)){
                // Preserve response entry identity; do not guess an action mapping or sum alternative views.
                var observation=entries.Count==1?row:row with{ResourceScope=row.ResourceScope+"/capacity-entry/"+entry.Index.ToString(CultureInfo.InvariantCulture)};
                observation.Validate(provider,logicalRequestId);rows.Add(observation);
            }
        }
        return rows.AsReadOnly();
    }
    internal static IReadOnlyList<NativeResourceUsageObservation> Capture(string provider,string logicalRequestId,string? nativeRequestId,
        string region,string table,ConsumedCapacity? capacity,DateTime observedAtUtc)
    {
        static string Name(string value){ArgumentException.ThrowIfNullOrWhiteSpace(value);StrictUtf8.GetByteCount(value);return Uri.EscapeDataString(value);}
        var scope=Name(region)+"/table/"+Name(table);
        if(capacity?.TableName!=null&&capacity.TableName!=table)throw new InvalidDataException("DynamoDB capacity table differs from its original request.");
        var rows=new List<NativeResourceUsageObservation>();
        void Add(string resource,string unit,string view,double? quantity){
            decimal? exact=null;
            if(quantity.HasValue){
                var value=quantity.Value;
                if(!double.IsFinite(value)||value<0||!decimal.TryParse(value.ToString("R",CultureInfo.InvariantCulture),NumberStyles.Float,CultureInfo.InvariantCulture,out var parsed)
                    ||(double)parsed!=value)throw new InvalidDataException("Native capacity quantity cannot be represented without loss.");
                exact=parsed;
            }
            var observation=new NativeResourceUsageObservation(provider,logicalRequestId,nativeRequestId!,resource,unit,view,exact,observedAtUtc);
            observation.Validate(provider,logicalRequestId);rows.Add(observation);
        }
        void Dimensions(object model,string resource,string view,Func<double> total,Func<double> read,Func<double> write){
            var before=rows.Count;
            // Public SDK utility preserves explicit zero vs an omitted non-nullable double.
            if(AWSSDKUtils.IsPropertySet(model,"CapacityUnits"))Add(resource,"capacity-units",view,total());
            if(AWSSDKUtils.IsPropertySet(model,"ReadCapacityUnits"))Add(resource,"read-capacity-units",view,read());
            if(AWSSDKUtils.IsPropertySet(model,"WriteCapacityUnits"))Add(resource,"write-capacity-units",view,write());
            if(rows.Count==before)Add(resource,"capacity-units",view,null);
        }
        void Breakdown(Capacity value,string resource){
            if(value==null)throw new InvalidDataException("Native capacity breakdown is missing.");
            Dimensions(value,resource,"Breakdown",()=>value.CapacityUnits,()=>value.ReadCapacityUnits,()=>value.WriteCapacityUnits);
        }
        if(capacity==null)Add(scope,"capacity-units","Total",null);
        else{
            Dimensions(capacity,scope,"Total",()=>capacity.CapacityUnits,()=>capacity.ReadCapacityUnits,()=>capacity.WriteCapacityUnits);
            if(capacity.Table!=null)Breakdown(capacity.Table,scope);
            if(capacity.GlobalSecondaryIndexes!=null)foreach(var pair in capacity.GlobalSecondaryIndexes)Breakdown(pair.Value,scope+"/gsi/"+Name(pair.Key));
            if(capacity.LocalSecondaryIndexes!=null)foreach(var pair in capacity.LocalSecondaryIndexes)Breakdown(pair.Value,scope+"/lsi/"+Name(pair.Key));
        }
        return rows.AsReadOnly();
    }
}
