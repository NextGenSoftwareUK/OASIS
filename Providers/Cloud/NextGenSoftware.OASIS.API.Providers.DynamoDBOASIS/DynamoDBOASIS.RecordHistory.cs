using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

public partial class DynamoDBOASIS
{
    private static readonly EmbeddedRecordHistory RecordHistory=new();

    // Disabling creation of new histories must never erase an already committed archive.
    // The owner persists this detached document under its observed native condition.
    private Task<JObject> StageRecordAsync<T>(T proposed,JObject previousDocument) where T:class,IHolonBase
        => StageRecordDocumentAsync(proposed,previousDocument,IsVersionControlEnabled);

    private async Task<JObject> StageRecordDocumentAsync<T>(T proposed,JObject previousDocument,bool createHistory) where T:class,IHolonBase
    {
        if(createHistory || previousDocument?.Property(EmbeddedRecordHistory.PropertyName)!=null)
        {
            var previous=previousDocument==null?null:(JObject)previousDocument.DeepClone();
            if(previous!=null)
            {
                if(previous["id"]!=null && previous["id"].Value<string>()!=proposed.Id.ToString())
                    throw new InvalidDataException("Embedded native identity differs from its item key.");
                previous["id"]=proposed.Id.ToString();
            }
            return await RecordHistory.StageAsync(proposed,previous,OasisJson.DeserializeStored<T>);
        }
        return JObject.Parse(OasisJson.Serialize(proposed));
    }

    private static T ReadStoredVersion<T>(Dictionary<string,AttributeValue> item,Guid identity,int version) where T:class,IHolonBase
    {
        if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
        var current=ReadCanonicalIdentity<T>(item,identity);
        var document=JObject.Parse(item["data_json"].S);
        if(document.Property(EmbeddedRecordHistory.PropertyName)==null)return version==0||current.Version==version?current:null;
        if(document["id"]!=null&&document["id"].Value<string>()!=identity.ToString())throw new InvalidDataException("Embedded record identity conflicts with native key.");
        document["id"]=identity.ToString();
        // Pure codec, no native I/O; one canonical JSON/version implementation.
        return RecordHistory.ReadAsync(identity,document,version,OasisJson.DeserializeStored<T>).GetAwaiter().GetResult();
    }

    // One conditional writer for ordinary and versioned records. No caller-state publication here.
    private async Task<T> WriteRecordAsync<T>(PutItemRequest proposal,bool createHistory) where T:class,IHolonBase
    {
        var client=_client;
        if(client.Config.MaxErrorRetry!=0)throw new InvalidOperationException("Conditional history writes require single-attempt SDK configuration; unknown acknowledgements must not become a retry's condition failure.");
        ArgumentNullException.ThrowIfNull(proposal);var table=proposal.TableName;
        if(table is not ("OasisAvatars" or "OasisAvatarDetails" or "OasisHolons"))throw new ArgumentException("Canonical OASIS table required.");
        var expectedType=table=="OasisAvatars"?typeof(Core.Holons.Avatar):table=="OasisAvatarDetails"?typeof(Core.Holons.AvatarDetail):typeof(Core.Holons.Holon);
        if(typeof(T)!=expectedType)throw new ArgumentException("Canonical record kind differs from its native table.");
        // Freeze all native attributes before the first await; supported record projections use S/N/BOOL.
        var item=new Dictionary<string,AttributeValue>();
        foreach(var pair in proposal.Item){
            if(pair.Value==null)throw new InvalidDataException("Missing proposed attribute value.");
            if(pair.Value.S!=null)item.Add(pair.Key,new AttributeValue{S=pair.Value.S});
            else if(pair.Value.N!=null)item.Add(pair.Key,new AttributeValue{N=pair.Value.N});
            else if(pair.Key=="is_deleted")item.Add(pair.Key,new AttributeValue{BOOL=pair.Value.BOOL});
            else throw new ArgumentException("Unsupported proposed record attribute.");
        }
        var proposed=ReadStoredIdentity<T>(item);var id=proposed.Id;
        var observed=await client.GetItemAsync(new GetItemRequest{TableName=table,Key=new(){{"id",new AttributeValue{S=id.ToString()}}},ConsistentRead=true});
        RequireItemRead(observed);
        var previous=observed.Item.Count==0?null:observed.Item;
        string previousJson=null;
        JObject previousDocument=null;
        if(previous!=null){
            _=ReadStoredIdentity<T>(previous,id);
            previousJson=previous["data_json"].S;
            previousDocument=JObject.Parse(previousJson);
            if(previousDocument["id"]!=null&&previousDocument["id"].Value<string>()!=id.ToString())throw new InvalidDataException("Embedded native identity differs from its item key.");
            previousDocument["id"]=id.ToString();
        }
        var staged=await StageRecordDocumentAsync(proposed,previousDocument,createHistory);
        item["data_json"]=new AttributeValue{S=staged.ToString(Newtonsoft.Json.Formatting.None)};
        var request=new PutItemRequest{TableName=table,Item=item,ReturnValues=ReturnValue.ALL_OLD,
            ExpressionAttributeNames=new(){{"#id","id"},{"#data","data_json"}}};
        if(previous==null){request.ConditionExpression="attribute_not_exists(#id)";request.ExpressionAttributeNames.Remove("#data");}
        else{
            request.ConditionExpression="#data = :observed";request.ExpressionAttributeNames.Remove("#id");
            request.ExpressionAttributeValues=new(){{":observed",new AttributeValue{S=previousJson}}};
        }
        return await ObserveMutationAsync(async()=>{
            var ack=await client.PutItemAsync(request);
            if(ack?.HttpStatusCode!=HttpStatusCode.OK)throw new InvalidDataException("Conditional history write lacks authoritative acknowledgement.");
            if(previous==null){if(ack.Attributes?.Count>0)throw new InvalidDataException("Create-only write unexpectedly replaced a record.");}
            else if(ack.Attributes==null||!ack.Attributes.TryGetValue("id",out var oldId)||oldId.S!=id.ToString()
                ||!ack.Attributes.TryGetValue("data_json",out var oldJson)||oldJson.S!=previousJson)
                throw new InvalidDataException("Conditional replacement acknowledgement differs from the observed original.");
            return OasisJson.DeserializeStored<T>(staged.ToString(Newtonsoft.Json.Formatting.None));
        },table,id.ToString());
    }
}
