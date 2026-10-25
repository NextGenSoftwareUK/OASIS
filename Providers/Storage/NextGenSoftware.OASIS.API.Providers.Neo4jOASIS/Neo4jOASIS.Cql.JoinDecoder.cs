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
    internal sealed class NeoCqlJoinDecoder<TLeft,TRight,TKey,TResult> where TLeft:IHolon where TRight:IHolon
    {
        private readonly CosmicJoinSelection<TLeft,TRight,TKey,TResult> query;
        private readonly CosmicJoinExecutionLimits limits;
        private readonly Action check;
        private readonly List<CosmicJoinInputRow> left=new(),right=new();
        private readonly List<CosmicJoinRow> pairs=new();
        private long leftCount,rightCount,pairCount,leftBytes,rightBytes,outputBytes;
        private long previousKind=-1;
        private static readonly Encoding Utf8=new UTF8Encoding(false,true);

        internal NeoCqlJoinDecoder(CosmicJoinSelection<TLeft,TRight,TKey,TResult> query,CosmicJoinExecutionLimits limits,Action check)
        { this.query=query;this.limits=limits;this.check=check; }

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
        {
            if(raw is not IDictionary<string,object> source || source.Count!=12)throw Invalid("Native join source shape is invalid.");
            if(Flag(source,"cycle"))throw Invalid("Native join traversal encountered a cycle.");
            if(Flag(source,"traversalLimit") || Flag(source,"payloadLimit"))throw new NeoCqlLimitException("Native join source exceeds its traversal/payload bound.");
            if(!Flag(source,"valid") || !Flag(source,"domainValid"))throw Invalid("Native join source ownership or scalar domain is invalid.");
            if(!source.TryGetValue("id",out var rawId) || !source.TryGetValue("creator",out var rawCreator))throw Invalid("Native join identity proof is missing.");
            var id=(Guid)DecodeNativeProjectionValue(rawId,typeof(Guid));
            var creator=(Guid)DecodeNativeProjectionValue(rawCreator,typeof(Guid));
            var visible=Flag(source,"visible");
            if(id==Guid.Empty || intent.ReadAccess!=null && !visible && creator!=intent.ReadAccess.ActorId)
                throw Invalid("Native join source identity/access is invalid.");
            var lower=Number(source,"bytes");
            if(lower<0)throw Invalid("Native join byte proof is invalid.");
            if(lower>maximumBytes)throw new NeoCqlLimitException("Native join source exceeds remaining bytes.");
            var keys=query.Join.Keys.Select(key=>isLeft?key.Left:key.Right).ToArray();
            var columns=query.Columns.Where(column=>column.Source==(isLeft?CosmicJoinColumnSource.Left:CosmicJoinColumnSource.Right)).ToArray();
            var measured=checked(bytes+48);
            var keyValues=Cells("keys",keys.Select(path=>path.ValueType).ToArray(),false,out _);
            var selected=Cells("values",columns.Select(column=>column.Path.ValueType).ToArray(),false,out _);
            var order=Cells("ordering",intent.Orderings.Select(item=>item.Path.ValueType).ToArray(),true,out var states);
            if(measured>maximumBytes)throw new NeoCqlLimitException("Native join source exceeds remaining bytes.");
            bytes=measured;
            return new CosmicJoinInputRow(id,visible,creator,keyValues,selected,order,states);

            object[] Cells(string name,Type[] domains,bool allowAbsent,out int[] states)
            {
                if(!source.TryGetValue(name,out var native) || native is not IReadOnlyList<object> cells || cells.Count!=domains.Length)
                    throw Invalid("Native join cell buffer width is invalid.");
                var values=new object[domains.Length];states=new int[domains.Length];
                for(var index=0;index<values.Length;index++)
                {
                    check();
                    if(cells[index] is not IDictionary<string,object> cell || cell.Count!=2 || !cell.TryGetValue("s",out var rawState)
                        || rawState is not long state || state is <0 or >2 || !cell.TryGetValue("v",out var value))
                        throw Invalid("Native join cell domain is invalid.");
                    states[index]=(int)state;var declared=domains[index];
                    if(state<2)
                    {
                        if(value!=null || !allowAbsent && declared.IsValueType && Nullable.GetUnderlyingType(declared)==null)
                            throw Invalid("A required native join cell is missing/null.");
                    }
                    else values[index]=DecodeNativeProjectionValue(value,Nullable.GetUnderlyingType(declared)??declared);
                    Reserve(values[index],ref measured,maximumBytes);
                }
                return values;
            }
        }

        private static void Observe(long count,int actual,int maximum,ref long observed)
        {
            if(count>maximum || actual>=maximum)throw new NeoCqlLimitException("Native join source exceeds remaining rows.");
            if(actual>0 && observed!=count)throw Invalid("Native join source count changed.");observed=count;
        }
        private void Reserve(object value,ref long bytes,int maximum)
        {
            check();
            try{bytes=checked(bytes+16+(value is string text?Utf8.GetByteCount(text):16));}
            catch(EncoderFallbackException cause){throw new InvalidDataException("Native join string is not valid UTF-8.",cause);}
            if(bytes>maximum)throw new NeoCqlLimitException("Native join exceeds its remaining byte budget.");
        }
        private static long Number(IEnumerable<KeyValuePair<string,object>> fields,string name)
        {
            foreach(var field in fields)if(field.Key==name && field.Value is long number)return number;
            throw Invalid("Native join requires an exact integer "+name+".");
        }
        private static bool Flag(IDictionary<string,object> fields,string name)
        {
            if(fields.TryGetValue(name,out var value) && value is bool flag)return flag;
            throw Invalid("Native join requires a Boolean "+name+".");
        }
        private static InvalidDataException Invalid(string message)=>new(message);
    }
}
