using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>Explicit wire representation for stores limited to binary64 JSON numbers.
/// Canonical records retain numeric types; only non-roundtrippable numbers become
/// native strings, bound to exact structural paths in a versioned envelope.
/// No I/O, configuration, CLR type activation or caller mutation.</summary>
public static class Binary64JsonNumberCodec
{
    public const string PropertyName="_oasis_exact_numbers";

    private static bool RequiresEncoding(JToken value)
    {
        if(!StoredRecordPayloadEquality.IsNumber(value))return false;
        var raw=value.ToString(Formatting.None);
        return !double.TryParse(raw,NumberStyles.Float,CultureInfo.InvariantCulture,out var number)||!double.IsFinite(number)||
            !StoredRecordPayloadEquality.Equivalent(value,new JRaw(number.ToString("R",CultureInfo.InvariantCulture)));
    }

    public static JObject Encode(JObject canonical)
    {
        ArgumentNullException.ThrowIfNull(canonical);
        if(canonical.Property(PropertyName)!=null)throw new InvalidDataException("Canonical record collides with reserved exact-number envelope.");
        var result=(JObject)canonical.DeepClone();var values=new JArray();
        void Visit(JToken parent,List<string> path)
        {
            IEnumerable<(string Key,JToken Value)> children=parent is JObject obj
                ?obj.Properties().Select(property=>(property.Name,property.Value)).ToArray()
                :parent is JArray array?array.Select((value,index)=>(index.ToString(CultureInfo.InvariantCulture),value)).ToArray()
                :Array.Empty<(string,JToken)>();
            foreach(var child in children){
                var next=new List<string>(path){child.Key};
                if(RequiresEncoding(child.Value)){
                    var literal=child.Value.ToString(Formatting.None);
                    values.Add(new JObject{["path"]=new JArray(next),["literal"]=literal});
                    child.Value.Replace(new JValue(literal));
                }else Visit(child.Value,next);
            }
        }
        Visit(result,new());
        if(values.Count>0)result[PropertyName]=new JObject{["schema"]="binary64-v1",["values"]=values};
        return result;
    }

    public static JObject Decode(JObject wire)
    {
        ArgumentNullException.ThrowIfNull(wire);var result=(JObject)wire.DeepClone();
        var marker=result.Property(PropertyName);if(marker==null)return result;
        if(marker.Value is not JObject envelope||envelope.Count!=2||envelope["schema"]?.Type!=JTokenType.String||
            envelope["schema"].Value<string>()!="binary64-v1"||envelope["values"] is not JArray values||values.Count==0)
            throw new InvalidDataException("Unknown or malformed exact-number envelope.");
        var seen=new HashSet<string>(StringComparer.Ordinal);
        foreach(var item in values){
            if(item is not JObject entry||entry.Count!=2||entry["path"] is not JArray path||path.Count==0||
                path.Any(part=>part.Type!=JTokenType.String)||entry["literal"]?.Type!=JTokenType.String)
                throw new InvalidDataException("Malformed exact-number entry.");
            var steps=path.Values<string>().ToArray();
            if(steps[0]==PropertyName||!seen.Add(path.ToString(Formatting.None)))throw new InvalidDataException("Duplicate or reserved exact-number path.");
            var literal=entry["literal"].Value<string>();JToken number;
            try{
                var parsed=OasisJson.ParseStoredObject("{\"value\":"+literal+"}");
                if(parsed.Count!=1)throw new InvalidDataException("Exact-number literal must be a single JSON value.");
                number=parsed["value"];
            }
            catch(Exception cause)when(cause is InvalidDataException or ArgumentException){throw new InvalidDataException("Invalid exact-number literal.",cause);}
            if(!RequiresEncoding(number))throw new InvalidDataException("Exact-number frame must bind a non-roundtrippable JSON number.");
            JToken current=result;
            foreach(var step in steps){
                if(current is JObject obj&&obj.TryGetValue(step,StringComparison.Ordinal,out var property))current=property;
                else if(current is JArray array&&int.TryParse(step,NumberStyles.None,CultureInfo.InvariantCulture,out var index)&&
                    index>=0&&index<array.Count&&index.ToString(CultureInfo.InvariantCulture)==step)current=array[index];
                else throw new InvalidDataException("Exact-number path does not resolve to its native value.");
            }
            if(current.Type!=JTokenType.String||current.Value<string>()!=literal)
                throw new InvalidDataException("Exact-number literal differs from its native string value.");
            current.Replace(number.DeepClone());
        }
        marker.Remove();return result;
    }
}
