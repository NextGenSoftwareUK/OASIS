using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>
/// Explicit operator repair of one reviewed held baseline. Host owns the protected
/// journal path, configured target and fenced source completion callback. No default
/// authorization, automatic cutover or general-purpose queue reset is introduced.
/// </summary>
public sealed class HolonReplicaInitializationApplication
{
    private readonly HolonReplicaInitializationPreparation _preparation;
    private readonly ICommittedHolonReplicaInitializationRecoveryStorageProvider _target;
    private readonly Func<HolonReplicaInitializationIntent,HolonReplicaInitializationOutcome,CancellationToken,Task<OASISResult<bool>>> _complete;

    public HolonReplicaInitializationApplication(HolonReplicaInitializationPreparation preparation,
        ICommittedHolonReplicaInitializationRecoveryStorageProvider configuredTarget,
        Func<HolonReplicaInitializationIntent,HolonReplicaInitializationOutcome,CancellationToken,Task<OASISResult<bool>>> fencedSourceCompletion)
    {
        _preparation=preparation ?? throw new ArgumentNullException(nameof(preparation));
        _target=configuredTarget ?? throw new ArgumentNullException(nameof(configuredTarget));
        _complete=fencedSourceCompletion ?? throw new ArgumentNullException(nameof(fencedSourceCompletion));
    }

    public async Task<OASISResult<bool>> ExecuteAsync(Guid operatorId,long sequence,Guid initializationOperationId,
        string protectedJournalPath,CancellationToken token=default)
    {
        try
        {
            token.ThrowIfCancellationRequested();
            var path=Path.GetFullPath(protectedJournalPath);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using var lease=new FileStream(path+".lock",FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
            if(File.Exists(path+".tmp"))return Failed("COSMIC_REPLICA_INITIALIZATION_JOURNAL_UNRESOLVED","Preserve the interrupted journal publication before any submission.");
            var recovering=File.Exists(path);
            var reviewed=await _preparation.ReviewApplicationAsync(operatorId,sequence,initializationOperationId,recovering,token).ConfigureAwait(false);
            if(!Clean(reviewed) || reviewed.Result==null)return Failed(reviewed?.ErrorCode ?? "COSMIC_REPLICA_INITIALIZATION_REVIEW_FAILED",reviewed?.Message,reviewed?.Exception);
            var intent=reviewed.Result;
            var record=recovering ? JObject.Parse(await File.ReadAllTextAsync(path,token).ConfigureAwait(false),
                new JsonLoadSettings{DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error})
                :new JObject{["format"]=1,["intentFingerprint"]=intent.Fingerprint,["intent"]=JObject.FromObject(intent),["phase"]="Prepared",["receiptDigest"]=JValue.CreateNull()};
            if(recovering)
            {
                var checksum=record.Value<string>("checksum");record.Remove("checksum");
                if(checksum!=Digest(record) || record.Properties().Count()!=5 || record.Value<int>("format")!=1
                    || record.Value<string>("intentFingerprint")!=intent.Fingerprint
                    || record["intent"]?.ToString(Formatting.None)!=JObject.FromObject(intent).ToString(Formatting.None))
                    return Failed("COSMIC_REPLICA_INITIALIZATION_JOURNAL_MISMATCH","The durable journal does not bind this exact authorized intent.");
            }
            var phase=record.Value<string>("phase");
            if(phase is not ("Prepared" or "Submitted" or "TargetCommitted" or "SourceCompleted"))
                return Failed("COSMIC_REPLICA_INITIALIZATION_JOURNAL_INVALID","Unknown initialization journal phase.");
            void Save(string next,string receipt=null)
            {
                record["phase"]=next;record["receiptDigest"]=receipt==null ? JValue.CreateNull():new JValue(receipt);
                var envelope=(JObject)record.DeepClone();envelope["checksum"]=Digest(record);
                using(var file=new FileStream(path+".tmp",FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {var bytes=Encoding.UTF8.GetBytes(envelope.ToString(Formatting.None));file.Write(bytes);file.Flush(true);}
                File.Move(path+".tmp",path,true);
            }
            if(!recovering)Save("Prepared");
            var authorized=await _preparation.ReauthorizeApplicationAsync(intent,token).ConfigureAwait(false);
            if(!Clean(authorized) || authorized.Result==null)return Failed(authorized?.ErrorCode,authorized?.Message,authorized?.Exception);
            var mutation=authorized.Result;
            var observed=await _target.ReadReplicaInitializationOutcomeAsync(mutation,token).ConfigureAwait(false);
            if(!Clean(observed) || observed.Result==null)return Failed("COSMIC_REPLICA_INITIALIZATION_RECEIPT_UNAVAILABLE","An authoritative target outcome is required.",observed?.Exception);
            if(observed.Result.ReceiptState==ConditionalHolonReceiptState.Absent)
            {
                if(phase!="Prepared")return Failed("COSMIC_REPLICA_INITIALIZATION_RECONCILIATION_REQUIRED","A submitted initialization has no receipt yet; absence never permits resubmission.");
                Save("Submitted");
                var applied=await _target.InitializeCommittedHolonReplicaAsync(mutation,token).ConfigureAwait(false);
                if(!Clean(applied))return Failed("COSMIC_REPLICA_INITIALIZATION_RECONCILIATION_REQUIRED","Submission did not finish cleanly; recover this same journal and operation.",applied?.Exception);
                observed=await _target.ReadReplicaInitializationOutcomeAsync(mutation,token).ConfigureAwait(false);
            }
            if(!Clean(observed) || observed.Result==null || observed.Result.ReceiptState!=ConditionalHolonReceiptState.Committed
                || observed.Result.OperationId!=intent.InitializationOperationId || observed.Result.BaselineFingerprint!=intent.BaselineFingerprint
                || record.Value<string>("receiptDigest") is string savedDigest && savedDigest!=observed.Result.ReceiptDigest)
                return Failed("COSMIC_REPLICA_INITIALIZATION_RECEIPT_MISMATCH","The exact committed target initialization receipt is required.",observed?.Exception);
            if(phase!="SourceCompleted")Save("TargetCommitted",observed.Result.ReceiptDigest);

            // Authorization is current again at source completion, not merely when
            // the operator originally prepared or submitted the target baseline.
            authorized=await _preparation.ReauthorizeApplicationAsync(intent,token).ConfigureAwait(false);
            if(!Clean(authorized))return Failed(authorized?.ErrorCode,authorized?.Message,authorized?.Exception);
            var completed=await _complete(intent,observed.Result,token).ConfigureAwait(false);
            if(!Clean(completed) || !completed.Result)return Failed("COSMIC_REPLICA_INITIALIZATION_SOURCE_COMPLETION_UNRESOLVED","Recover the original fenced completion; do not reset or replay the source write.",completed?.Exception);
            if(phase!="SourceCompleted")Save("SourceCompleted",observed.Result.ReceiptDigest);
            return new(true);
        }
        catch(OperationCanceledException)when(token.IsCancellationRequested){throw;}
        catch(Exception cause){return Failed("COSMIC_REPLICA_INITIALIZATION_APPLICATION_FAILED","Initialization application stopped; preserve its journal and inspect original outcomes before continuing.",cause);}
    }

    private static bool Clean<T>(OASISResult<T> result)=>result!=null && !result.IsError && !result.IsWarning && result.Exception==null;
    private static string Digest(JObject record)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(record.ToString(Formatting.None))));
    private static OASISResult<bool> Failed(string code,string message,Exception cause=null)
        =>new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=cause};
}
