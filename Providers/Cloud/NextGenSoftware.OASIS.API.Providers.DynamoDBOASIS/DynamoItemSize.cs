using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.DynamoDBv2.Model;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS;

// Admission lower bound, NOT a billing/capacity estimator. AWS documents numeric
// storage size only approximately; never reject a valid item using invented padding.
// https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/CapacityUnitCalculations.html
internal static class DynamoItemSize
{
    internal const long MaximumItemBytes=400L*1024;
    internal static long MinimumBytes(IReadOnlyDictionary<string,AttributeValue> item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var path=new HashSet<AttributeValue>(ReferenceEqualityComparer.Instance);
        return item.Sum(pair=>checked((long)Encoding.UTF8.GetByteCount(pair.Key)+Value(pair.Value,path)));
    }
    private static long Value(AttributeValue value,HashSet<AttributeValue> path)
    {
        ArgumentNullException.ThrowIfNull(value);
        if(!path.Add(value))throw new ArgumentException("DynamoDB attribute values cannot contain a cycle.");
        try
        {
            if(value.S!=null)return Encoding.UTF8.GetByteCount(value.S);
            if(value.N!=null)return 0; // Native service owns exact compressed-number accounting.
            if(value.B!=null)return value.B.Length;
            if(value.IsBOOLSet || value.NULL)return 1;
            if(value.IsMSet)return checked(3L+value.M.Sum(pair=>checked(1L+Encoding.UTF8.GetByteCount(pair.Key)+Value(pair.Value,path))));
            if(value.IsLSet)return checked(3L+value.L.Sum(child=>checked(1L+Value(child,path))));
            if(value.IsSSSet)return value.SS.Sum(text=>(long)Encoding.UTF8.GetByteCount(text));
            if(value.IsNSSet)return 0;
            if(value.IsBSSet)return value.BS.Sum(bytes=>bytes.Length);
            throw new ArgumentException("A DynamoDB value must declare a native attribute type.");
        }
        finally{path.Remove(value);}
    }
}
