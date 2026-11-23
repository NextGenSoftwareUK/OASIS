using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : ICosmicQueryKeyedEntityStorageProvider
{
    public CosmicProjectionCapabilities KeyedEntityCapabilities => CqlProjectionCapabilities;

    public async Task<OASISResult<CosmicKeyedEntityBatch>> ExecuteCosmicKeyedEntitiesAsync<T>(
        CosmicQueryPlan<T> plan,IReadOnlyList<CosmicQueryPath> keys,int maximumRows=1000,
        int maximumPayloadBytes=CosmicProjectionResults.MaximumPayloadLimit,
        CancellationToken cancellationToken=default) where T:IHolon
    {
        NeoCqlReadBudget readBudget=null;
        try
        {
            var admitted=CosmicKeyedEntitySource.Admit(plan,keys,Core.Enums.ProviderType.Neo4jOASIS,
                CqlCapabilities,CqlProjectionCapabilities,maximumRows,maximumPayloadBytes);
            if(admitted.IsError)
                return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<bool,CosmicKeyedEntityBatch>(admitted);
            var parameters=new Dictionary<string,object>();
            var source=CompileCqlDetachedSource(plan.Query,parameters,keys,Array.Empty<CosmicQueryPath>(),
                maximumRows,maximumPayloadBytes,includeEntityLocator:true);
            parameters["keyedSnapshotBytes"]=maximumPayloadBytes;
            var text="CALL { "+source+" } WITH collect(input) AS inputs "
                +"UNWIND CASE WHEN size(inputs)=0 THEN [null] ELSE range(0,size(inputs)-1) END AS position "
                +"WITH size(inputs) AS nativeCount,position,CASE WHEN position IS NULL THEN null ELSE inputs[position] END AS input "
                +"OPTIONAL MATCH (entity) WHERE elementId(entity)=input.entityLocator "
                +"AND coalesce(entity.ProviderRevisionId,'')=input.entityRevision "
                +"RETURN nativeCount,input,CASE WHEN entity IS NULL THEN null ELSE "
                +"{id:entity.Id,revision:entity.ProviderRevisionId,snapshot:CASE WHEN size(entity.SnapshotJson)<=$keyedSnapshotBytes "
                +"THEN entity.SnapshotJson ELSE null END} END AS entity ORDER BY position";
            using var budget=readBudget=new NeoCqlReadBudget(maximumPayloadBytes,cancellationToken);
            budget.Check();
            await using var readOwner=await LeaseCqlReadAsync(budget.Token).ConfigureAwait(false);
            using var session=readOwner.Driver.AsyncSession(config=>config.WithDefaultAccessMode(AccessMode.Read).WithFetchSize(1));
            using var transaction=await session.BeginTransactionAsync(config=>config.WithTimeout(TimeSpan.FromSeconds(30))).ConfigureAwait(false);
            budget.Check();
            var cursor=await transaction.RunAsync(text,parameters).ConfigureAwait(false);
            var decoder=new NeoCqlKeyedEntityDecoder<T>(plan.Query,keys,maximumRows,maximumPayloadBytes,budget.Check);
            while(true)
            {
                budget.Check();
                if(!await cursor.FetchAsync().ConfigureAwait(false))break;
                budget.Check();decoder.Add(cursor.Current.Values);
            }
            budget.Check();var batch=decoder.Complete();
            await transaction.CommitAsync().ConfigureAwait(false);budget.Check();
            return new OASISResult<CosmicKeyedEntityBatch>(batch){IsLoaded=true};
        }
        catch(Exception cause) when(cancellationToken.IsCancellationRequested)
        {return KeyedCqlFailure("CQL_QUERY_CANCELLED","Neo4j keyed entity read was cancelled.",cause);}
        catch(Exception cause) when(readBudget?.IsTimedOut==true)
        {return KeyedCqlFailure("CQL_QUERY_TIMEOUT","Neo4j keyed entity read exceeded its cumulative deadline.",cause);}
        catch(NeoCqlNotReadyException cause){return KeyedCqlFailure("CQL_PROVIDER_NOT_READY",cause.Message,cause);}
        catch(NeoCqlLimitException cause){return KeyedCqlFailure("CQL_QUERY_LIMIT",cause.Message,cause);}
        catch(NeoCqlTimeoutException cause){return KeyedCqlFailure("CQL_QUERY_TIMEOUT",cause.Message,cause);}
        catch(Neo4jException cause) when(cause.Code=="Neo.ClientError.Transaction.TransactionTimedOut"
            || cause.Code=="Neo.TransientError.Transaction.TransactionTimedOut"
            || cause.Code=="Neo.ClientError.Transaction.TransactionTimedOutClientConfiguration")
        {return KeyedCqlFailure("CQL_QUERY_TIMEOUT",cause.Message,cause);}
        catch(OperationCanceledException cause){return KeyedCqlFailure("CQL_QUERY_CANCELLED",cause.Message,cause);}
        catch(NotSupportedException cause){return KeyedCqlFailure("CQL_CAPABILITY_UNSUPPORTED",cause.Message,cause);}
        catch(OverflowException cause){return KeyedCqlFailure("CQL_DATA_INVALID",cause.Message,cause);}
        catch(InvalidDataException cause){return KeyedCqlFailure("CQL_DATA_INVALID",cause.Message,cause);}
        catch(Exception cause){return KeyedCqlFailure("CQL_PROVIDER_ERROR",cause.Message,cause);}
    }

    private static OASISResult<CosmicKeyedEntityBatch> KeyedCqlFailure(string code,string message,Exception cause)
        =>new(){IsError=true,ErrorCode=code,Message=message,Exception=cause};

    private sealed class NeoCqlKeyedEntityDecoder<T>(CosmicQuery<T> query,IReadOnlyList<CosmicQueryPath> keys,
        int maximumRows,int maximumBytes,Action check):NeoCqlDetachedSourceDecoder(check) where T:IHolon
    {
        private readonly List<CosmicKeyedEntityRow> rows=new();
        private long observed=-1;
        private long bytes;
        private bool emptyHeader;

        internal void Add(IReadOnlyDictionary<string,object> record)
        {
            check();
            if(record.Count!=3 || !record.TryGetValue("input",out var raw)
                || !record.TryGetValue("entity",out var rawEntity))
                throw Invalid("Native keyed entity stream shape is invalid.");
            var count=Number(record,"nativeCount");
            if(count<0 || observed>=0 && count!=observed)
                throw Invalid("Native keyed entity count changed within the stream.");
            if(count>maximumRows)throw new NeoCqlLimitException("Complete keyed entity source exceeds its row bound.");
            observed=count;
            if(count==0)
            {
                if(emptyHeader || rows.Count!=0 || raw!=null || rawEntity!=null)
                    throw Invalid("Native empty keyed entity acknowledgement is invalid.");
                emptyHeader=true;return;
            }
            if(emptyHeader || rows.Count>=count || raw is not IDictionary<string,object> source)
                throw Invalid("Native keyed entity source is incomplete or duplicated.");
            var proofFields=new Dictionary<string,object>(source);
            proofFields.Remove("entityLocator");proofFields.Remove("entityRevision");
            var proof=Source(proofFields,query.ReadAccess,keys.Select(key=>key.ValueType).ToArray(),
                Array.Empty<Type>(),query.Orderings.Select(order=>order.Path.ValueType).ToArray(),
                ref bytes,maximumBytes,includeNativeByteFloor:true);
            if(source.Count!=14 || !source.TryGetValue("entityLocator",out var locator)
                || locator is not string locatorText || string.IsNullOrEmpty(locatorText)
                || !source.TryGetValue("entityRevision",out var revision) || revision is not string)
                throw Invalid("Native keyed entity locator/revision proof is missing.");
            if(rawEntity is not IReadOnlyDictionary<string,object> entity || entity.Count!=3
                || !entity.TryGetValue("snapshot",out var snapshot) || !entity.ContainsKey("id")
                || !entity.ContainsKey("revision"))
                throw Invalid("Native keyed source lost its corresponding snapshot.");
            if(snapshot is not string json)throw new NeoCqlLimitException("Native keyed entity exceeds its snapshot bound.");
            bytes=checked(bytes+Encoding.UTF8.GetByteCount(json));
            if(bytes>maximumBytes)throw new NeoCqlLimitException("Complete native keyed source exceeds its payload bound.");
            var holon=ReadCqlCanonicalSnapshot(entity,json);
            if(holon.Id!=proof.Id || holon.IsPublic!=proof.IsPublic || holon.CreatedByAvatarId!=proof.CreatedByAvatarId)
                throw Invalid("Native keyed entity snapshot differs from its identity/access proof.");
            rows.Add(new CosmicKeyedEntityRow(proof,holon));
        }

        internal CosmicKeyedEntityBatch Complete()
        {
            check();
            if(observed<0 || rows.Count!=observed || observed==0 && !emptyHeader)
                throw Invalid("Native keyed entity stream did not acknowledge the complete source.");
            return new CosmicKeyedEntityBatch(observed,rows);
        }
    }
}
