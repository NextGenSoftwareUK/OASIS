using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>The single versioned persisted recovery codec used by native outbox stores.</summary>
public static class HolonRecoveryEvidenceJson
{
    public static string Write(HolonDeliveryRecoveryEvidence evidence)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        return new JObject{["format"]=1,["requiresNativeReconciliation"]=evidence.RequiresNativeReconciliation,
            ["originalTransactionIds"]=new JArray(evidence.OriginalTransactionIds)}.ToString(Formatting.None);
    }
    public static string Digest(string json)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    public static HolonDeliveryRecoveryEvidence ReadStored(string json,string digest,HolonDeliveryState state)
    {
        if(!Enum.IsDefined(typeof(HolonDeliveryState),state))throw new InvalidDataException("Unknown outbox delivery state.");
        if((json==null)!=(digest==null))throw new InvalidDataException("Incomplete stored recovery evidence.");
        if(json==null)return null;
        if(Digest(json)!=digest)throw new InvalidDataException("Stored recovery evidence checksum differs.");
        try
        {
            var document=JObject.Parse(json,new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error});
            if(document.Properties().Count()!=3 || document["format"]?.Type!=JTokenType.Integer || document["format"].ToString(Formatting.None)!="1"
                || document["requiresNativeReconciliation"]?.Type!=JTokenType.Boolean || document["originalTransactionIds"] is not JArray ids
                || ids.Any(id=>id.Type!=JTokenType.String))throw new InvalidDataException("Unsupported or invalid outbox recovery evidence.");
            var evidence=new HolonDeliveryRecoveryEvidence(document.Value<bool>("requiresNativeReconciliation"),ids.Values<string>());
            if(evidence.RequiresNativeReconciliation && state!=HolonDeliveryState.ReconciliationRequired)
                throw new InvalidDataException("Native recovery evidence disagrees with the delivery state.");
            return evidence;
        }
        catch(Exception error)when(error is JsonException || error is ArgumentException)
        {throw new InvalidDataException("Invalid stored outbox recovery evidence.",error);}
    }
}
