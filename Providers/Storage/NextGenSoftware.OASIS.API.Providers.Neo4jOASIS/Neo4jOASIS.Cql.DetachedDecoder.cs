using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    // Joins and composition decode the same detached source wire contract.
    internal abstract class NeoCqlDetachedSourceDecoder
    {
        protected readonly Action check;
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        protected NeoCqlDetachedSourceDecoder(Action check) { this.check = check; }
        protected CosmicJoinInputRow Source(object raw, CosmicQueryReadAccess access,
            Type[] keyDomains, Type[] valueDomains, Type[] orderingDomains, ref long bytes, int maximumBytes)
        {
            if(raw is not IDictionary<string,object> source || source.Count!=12)throw Invalid("Native detached source shape is invalid.");
            if(Flag(source,"cycle"))throw Invalid("Native detached traversal encountered a cycle.");
            if(Flag(source,"traversalLimit") || Flag(source,"payloadLimit"))throw new NeoCqlLimitException("Native detached source exceeds its traversal/payload bound.");
            if(!Flag(source,"valid") || !Flag(source,"domainValid"))throw Invalid("Native detached source ownership or scalar domain is invalid.");
            if(!source.TryGetValue("id",out var rawId) || !source.TryGetValue("creator",out var rawCreator))throw Invalid("Native detached identity proof is missing.");
            var id=(Guid)DecodeNativeProjectionValue(rawId,typeof(Guid));
            var creator=(Guid)DecodeNativeProjectionValue(rawCreator,typeof(Guid));
            var visible=Flag(source,"visible");
            if(id==Guid.Empty || access!=null && !access.CanRead(id,visible,creator))
                throw Invalid("Native detached source identity/access is invalid.");
            var lower=Number(source,"bytes");
            if(lower<0)throw Invalid("Native detached byte proof is invalid.");
            if(lower>maximumBytes)throw new NeoCqlLimitException("Native detached source exceeds remaining bytes.");
            var measured=checked(bytes+48);
            var keyValues=Cells("keys",keyDomains,false,out _);
            var selected=Cells("values",valueDomains,false,out _);
            var order=Cells("ordering",orderingDomains,true,out var states);
            if(measured>maximumBytes)throw new NeoCqlLimitException("Native detached source exceeds remaining bytes.");
            bytes=measured;
            return new CosmicJoinInputRow(id,visible,creator,keyValues,selected,order,states);

            object[] Cells(string name,Type[] domains,bool allowAbsent,out int[] states)
            {
                if(!source.TryGetValue(name,out var native) || native is not IReadOnlyList<object> cells || cells.Count!=domains.Length)
                    throw Invalid("Native detached cell buffer width is invalid.");
                var values=new object[domains.Length];states=new int[domains.Length];
                for(var index=0;index<values.Length;index++)
                {
                    check();
                    if(cells[index] is not IDictionary<string,object> cell || cell.Count!=2 || !cell.TryGetValue("s",out var rawState)
                        || rawState is not long state || state is <0 or >2 || !cell.TryGetValue("v",out var value))
                        throw Invalid("Native detached cell domain is invalid.");
                    states[index]=(int)state;var declared=domains[index];
                    if(state<2)
                    {
                        if(value!=null || !allowAbsent && declared.IsValueType && Nullable.GetUnderlyingType(declared)==null)
                            throw Invalid("A required native detached cell is missing/null.");
                    }
                    else values[index]=DecodeNativeProjectionValue(value,Nullable.GetUnderlyingType(declared)??declared);
                    Reserve(values[index],ref measured,maximumBytes);
                }
                return values;
            }
        }

        protected void Reserve(object value,ref long bytes,int maximum)
        {
            check();
            try{bytes=checked(bytes+16+(value is string text?Utf8.GetByteCount(text):16));}
            catch(EncoderFallbackException cause){throw new InvalidDataException("Native detached string is not valid UTF-8.",cause);}
            if(bytes>maximum)throw new NeoCqlLimitException("Native detached exceeds its remaining byte budget.");
        }
        protected static long Number(IEnumerable<KeyValuePair<string,object>> fields,string name)
        {
            foreach(var field in fields)if(field.Key==name && field.Value is long number)return number;
            throw Invalid("Native detached requires an exact integer "+name+".");
        }
        protected static bool Flag(IDictionary<string,object> fields,string name)
        {
            if(fields.TryGetValue(name,out var value) && value is bool flag)return flag;
            throw Invalid("Native detached requires a Boolean "+name+".");
        }
        protected static InvalidDataException Invalid(string message)=>new(message);
    }
}
