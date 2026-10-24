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
        if(capacities!=null&&capacities.Count!=0&&capacities.Count!=requestedTables.Count)
            throw new InvalidDataException("Transaction capacity must preserve the original action ordering and count.");
        var rows=new List<NativeResourceUsageObservation>();
        for(var index=0;index<requestedTables.Count;index++){
            var table=requestedTables[index];ArgumentException.ThrowIfNullOrWhiteSpace(table);
            var capacity=capacities==null||capacities.Count==0?null:capacities[index];
            if(capacities?.Count>0&&(capacity==null||capacity.TableName!=table))
                throw new InvalidDataException("Transaction capacity table differs from its original ordered action.");
            foreach(var row in Capture(provider,logicalRequestId,nativeRequestId,region,table,capacity,observedAtUtc))
                rows.Add(row with {ResourceScope=row.ResourceScope+"/transaction-action/"+index.ToString(CultureInfo.InvariantCulture)});
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
