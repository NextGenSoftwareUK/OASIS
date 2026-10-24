using System.IO;
using System.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics.CodeAnalysis;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

public partial class DynamoDBOASIS
{
    // One SDK acknowledgement boundary for point reads, mutation snapshots and lifecycle admission.
    // An acknowledged empty Item is absence; a missing response/collection is not.
    private static void RequireItemRead([NotNull] GetItemResponse? response)
        =>RequireReadAcknowledgement(response,response?.Item!=null);

    private static void RequireReadAcknowledgement([NotNull] AmazonWebServiceResponse? response,bool payloadPresent)
    {
        if(response?.HttpStatusCode!=HttpStatusCode.OK||!payloadPresent)
            throw new InvalidDataException("DynamoDB snapshot lacks an authoritative read acknowledgement.");
    }

    private static Dictionary<string,AttributeValue>? CopyReadCursor(Dictionary<string,AttributeValue>? cursor)
    {
        if(cursor==null||cursor.Count==0)return null;
        var copy=new Dictionary<string,AttributeValue>(StringComparer.Ordinal);
        foreach(var pair in cursor){
            // Every OASIS native table/index key is a nonempty string; never coerce/guess a different key type.
            if(string.IsNullOrEmpty(pair.Key)||string.IsNullOrEmpty(pair.Value?.S))throw new InvalidDataException("DynamoDB continuation key is invalid.");
            copy.Add(pair.Key,new AttributeValue{S=pair.Value.S});
        }
        return copy;
    }

    private static string ReadCursorIdentity(Dictionary<string,AttributeValue> cursor)
        =>System.Text.Json.JsonSerializer.Serialize(new SortedDictionary<string,string>(cursor.ToDictionary(pair=>pair.Key,pair=>pair.Value.S),StringComparer.Ordinal));

    private static Guid ReadNativeItemIdentity(Dictionary<string,AttributeValue> item)
    {
        if(item==null || !item.TryGetValue("id",out var key) || !Guid.TryParse(key?.S,out var id))
            throw new InvalidDataException("DynamoDB native record identity is invalid.");
        return id;
    }

    private static async IAsyncEnumerable<Dictionary<string,AttributeValue>> ReadNativePagesAsync(
        Func<Dictionary<string,AttributeValue>?,Task<(List<Dictionary<string,AttributeValue>> Items,Dictionary<string,AttributeValue>? Cursor)>> read,
        Dictionary<string,AttributeValue>? initialCursor=null)
    {
        var cursor=CopyReadCursor(initialCursor);var seen=new HashSet<string>(StringComparer.Ordinal);
        var identities=new HashSet<Guid>();
        if(cursor!=null)seen.Add(ReadCursorIdentity(cursor));
        do{
            var (items,next)=await read(cursor);
            if(items==null||items.Any(item=>item==null))throw new InvalidDataException("DynamoDB page has no valid item collection.");
            var following=CopyReadCursor(next);
            if(following!=null&&!seen.Add(ReadCursorIdentity(following)))throw new InvalidDataException("DynamoDB pagination repeated its continuation key.");
            foreach(var item in items){
                if(!identities.Add(ReadNativeItemIdentity(item)))
                    throw new InvalidDataException("DynamoDB pagination repeated its record identity.");
                yield return item;
            }
            cursor=following;
        }while(cursor!=null);
    }

    private static IAsyncEnumerable<Dictionary<string,AttributeValue>> ReadScanRowsAsync(Func<ScanRequest,Task<ScanResponse>> read,string table)
        =>ReadNativePagesAsync(async cursor=>{
            var response=await read(new ScanRequest{TableName=table,ConsistentRead=true,ExclusiveStartKey=cursor});
            RequireReadAcknowledgement(response,response?.Items!=null);
            return(response.Items,response.LastEvaluatedKey);
        });

    private static async Task<List<Dictionary<string,AttributeValue>>> ReadScanItemsAsync(Func<ScanRequest,Task<ScanResponse>> read,string table)
    {
        var items=new List<Dictionary<string,AttributeValue>>();
        await foreach(var item in ReadScanRowsAsync(read,table))items.Add(item);
        return items;
    }
}
