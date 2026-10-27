using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS;

/// <summary>Current private-catalogue CID lookup. No general IPFS query/index claim.</summary>
public partial class IPFSOASIS : ICosmicQueryStorageProvider
{
    public const string CqlRecordCidKey = "IPFSRecordCid";
    private const int CqlMaximumEnvelopeBytes = 4 * 1024 * 1024;
    private static readonly CosmicQueryCapabilities CidCapabilities =
        CosmicQueryCapabilities.ForStringMetadataPointLookup(CqlRecordCidKey,128);

    public ProviderType QueryProviderType => Core.Enums.ProviderType.IPFSOASIS;
    public CosmicQueryCapabilities QueryCapabilities => CidCapabilities;

    public async Task<OASISResult<IReadOnlyList<IHolon>>> ExecuteCosmicQueryAsync<T>(CosmicQueryPlan<T> plan,
        int maximumResults=100,CancellationToken cancellationToken=default) where T:IHolon
    {
        try
        {
            if(plan==null || plan.Targets.Count!=1 || plan.Targets[0].Provider!=QueryProviderType)
                return CqlFailure("CQL_PLAN_TARGET_UNAVAILABLE","One admitted IPFS target is required.");
            if(maximumResults<1 || maximumResults>10000)
                return CqlFailure("CQL_QUERY_LIMIT","The result budget must be between one and 10,000.");
            var admitted=QueryCapabilities.AdmitPlan(plan);
            if(admitted.IsError)return CqlFailure(admitted.ErrorCode,admitted.Message);
            var cid=QueryCapabilities.StringPointLookup.SelectKey(plan.Query).Result;
            // Reject paths, gateways and malformed CIDs before contacting the daemon.
            try { _=(Ipfs.Cid)cid; }
            catch(Exception error){return CqlFailure("CQL_INVALID_SOURCE_KEY","A valid record CID is required.",error);}
            cancellationToken.ThrowIfCancellationRequested();
            if(!IsProviderActivated || IPFSClient==null)
                return CqlFailure("CQL_PROVIDER_INACTIVE","Activate the configured IPFS provider before querying.");
            if(plan.Query.Offset>0 || plan.Query.Limit==0)return CqlRows();

            var root=_OASISDNA.OASIS.StorageProviders.IPFSOASIS.LookUpIPFSAddress;
            if(string.IsNullOrWhiteSpace(root))return CqlRows();
            _=(Ipfs.Cid)root;
            // One immutable catalogue snapshot and dictionary lookup, not a managed predicate scan.
            var catalogue=DeserializeLookup(UnprotectIndex(await ReadCqlEnvelopeAsync(root,cancellationToken)));
            if(!catalogue.TryGetValue(cid,out var entry) || entry.RecordKind!="holon")return CqlRows();
            var holon=DecodePrivateRecord<Holon>(cid,"holon",entry.Id,
                await ReadCqlEnvelopeAsync(cid,cancellationToken));
            cancellationToken.ThrowIfCancellationRequested();
            if(holon.IsDeleted || (plan.Query.ReadAccess!=null && !plan.Query.ReadAccess.CanRead(holon)))return CqlRows();
            holon.Children=Array.Empty<IHolon>();
            holon.MetaData ??= new Dictionary<string,object>();
            holon.MetaData[CqlRecordCidKey]=cid;
            return CqlRows(holon);
        }
        catch(OperationCanceledException error){return CqlFailure("CQL_QUERY_CANCELLED","The IPFS query was cancelled.",error);}
        catch(Exception error){return CqlFailure("CQL_QUERY_FAILED","The IPFS query failed; no partial rows returned.",error);}
    }

    private async Task<string> ReadCqlEnvelopeAsync(string cid,CancellationToken cancellationToken)
    {
        using var stream=await IPFSClient.FileSystem.ReadFileAsync(cid,cancellationToken);
        using var body=new MemoryStream();
        var buffer=new byte[16384];int count;
        while((count=await stream.ReadAsync(buffer,0,buffer.Length,cancellationToken))!=0){
            if(body.Length+count>CqlMaximumEnvelopeBytes)throw new InvalidDataException("IPFS CQL envelope exceeds its byte budget.");
            body.Write(buffer,0,count);
        }
        return new UTF8Encoding(false,true).GetString(body.ToArray());
    }

    private static OASISResult<IReadOnlyList<IHolon>> CqlRows(params IHolon[] rows)
        => new(Array.AsReadOnly(rows)){IsLoaded=true,LoadedCount=rows.Length};
    private static OASISResult<IReadOnlyList<IHolon>> CqlFailure(string code,string message,Exception error=null)
        => new(){IsError=true,ErrorCount=1,ErrorCode=code,Message=message,Exception=error};
}
