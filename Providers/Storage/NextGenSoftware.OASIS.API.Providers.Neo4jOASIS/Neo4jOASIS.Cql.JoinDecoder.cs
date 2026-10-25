using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    internal sealed class NeoCqlJoinDecoder<TLeft,TRight,TKey,TResult> : NeoCqlDetachedSourceDecoder where TLeft:IHolon where TRight:IHolon
    {
        private readonly CosmicJoinSelection<TLeft,TRight,TKey,TResult> query;
        private readonly CosmicJoinExecutionLimits limits;
        private readonly List<CosmicJoinInputRow> left=new(),right=new();
        private readonly List<CosmicJoinRow> pairs=new();
        private long leftCount,rightCount,pairCount,leftBytes,rightBytes,outputBytes;
        private long previousKind=-1;

        internal NeoCqlJoinDecoder(CosmicJoinSelection<TLeft,TRight,TKey,TResult> query,CosmicJoinExecutionLimits limits,Action check) : base(check)
        { this.query=query;this.limits=limits; }

        internal void Add(IReadOnlyDictionary<string,object> record)
        {
            check();
            if(record.Count!=5 || !record.TryGetValue("proof",out var proof))throw Invalid("Native join envelope is invalid.");
            var kind=Number(record,"kind");var lp=Number(record,"lp");var rp=Number(record,"rp");var count=Number(record,"nativeCount");
            if(kind<previousKind || kind is <0 or >2 || count<1)throw Invalid("Native join stream order/count is invalid.");
            previousKind=kind;
            if(kind==0)
            {
                if(lp!=left.Count || rp!=-1)throw Invalid("Native left proof position is invalid.");
                Observe(count,left.Count,limits.MaximumLeftRows,ref leftCount);
                left.Add(Source(proof,query.Join.Left,true,ref leftBytes,limits.MaximumLeftPayloadBytes));
            }
            else if(kind==1)
            {
                if(rp!=right.Count || lp!=-1)throw Invalid("Native right proof position is invalid.");
                Observe(count,right.Count,limits.MaximumRightRows,ref rightCount);
                right.Add(Source(proof,query.Join.Right,false,ref rightBytes,limits.MaximumRightPayloadBytes));
            }
            else
            {
                if(proof!=null || lp<0 || lp>=left.Count || rp< -1 || rp>=right.Count)
                    throw Invalid("Native join pair refers outside its captured source.");
                if(count>limits.MaximumPairs || count>limits.MaximumResults || pairs.Count>=limits.MaximumPairs || pairs.Count>=limits.MaximumResults)
                    throw new NeoCqlLimitException("Native join exceeds remaining pair/output work.");
                if(pairs.Count>0 && pairCount!=count)throw Invalid("Native join pair count changed.");
                pairCount=count;
                var l=left[(int)lp];var r=rp<0?null:right[(int)rp];var li=0;var ri=0;
                var values=new object[query.Columns.Count];outputBytes=checked(outputBytes+48);
                for(var index=0;index<values.Length;index++)
                {
                    var column=query.Columns[index];
                    if(column.Source==CosmicJoinColumnSource.Left)values[index]=l.Values[li++];
                    else if(column.Source==CosmicJoinColumnSource.Right){values[index]=r==null?null:r.Values[ri];ri++;}
                    else values[index]=column.PresenceWhenMissing?r==null:r!=null;
                    Reserve(values[index],ref outputBytes,limits.MaximumPayloadBytes);
                }
                pairs.Add(new CosmicJoinRow(l.Id,r?.Id,values));
            }
        }

        internal CosmicJoinPacket Complete()
        {
            check();
            if(left.Count!=leftCount || right.Count!=rightCount || pairs.Count!=pairCount)
                throw Invalid("Native join stream is incomplete.");
            return new CosmicJoinPacket(left.ToArray(),right.ToArray(),pairs.ToArray(),leftCount,rightCount,pairCount);
        }

        private CosmicJoinInputRow Source<T>(object raw,CosmicQuery<T> intent,bool isLeft,ref long bytes,int maximumBytes) where T:IHolon
            => Source(raw, intent.ReadAccess,
                query.Join.Keys.Select(key => (isLeft ? key.Left : key.Right).ValueType).ToArray(),
                query.Columns.Where(column => column.Source == (isLeft ? CosmicJoinColumnSource.Left : CosmicJoinColumnSource.Right))
                    .Select(column => column.Path.ValueType).ToArray(),
                intent.Orderings.Select(item => item.Path.ValueType).ToArray(), ref bytes, maximumBytes);

        private static void Observe(long count,int actual,int maximum,ref long observed)
        {
            if(count>maximum || actual>=maximum)throw new NeoCqlLimitException("Native join source exceeds remaining rows.");
            if(actual>0 && observed!=count)throw Invalid("Native join source count changed.");observed=count;
        }
    }
}
