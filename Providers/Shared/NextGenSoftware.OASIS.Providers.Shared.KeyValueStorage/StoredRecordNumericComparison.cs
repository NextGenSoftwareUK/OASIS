using System;
using System.Globalization;
using System.IO;
using System.Numerics;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

// Compare the existing Core-normalized coefficient/exponent, without rounding,
// changing numeric text into numbers, or allocating powers for large exponents.
internal static class StoredRecordNumericComparison
{
    internal static bool TryCompare(JToken left,JToken right,out int comparison)
    {
        comparison=0;
        if(!StoredRecordValueEquality.IsNumber(left)||!StoredRecordValueEquality.IsNumber(right))return false;
        static (string Digits,BigInteger Exponent,int Sign) Parts(JToken token)
        {
            if(token is JValue scalar&&(scalar.Value is double d&&!double.IsFinite(d)||scalar.Value is float f&&!float.IsFinite(f)))
                throw new InvalidDataException("Numeric search requires finite JSON numbers.");
            var text=StoredRecordValueEquality.CanonicalNumberText(token);
            var separator=text.IndexOf('e');var coefficient=separator<0?text:text[..separator];
            var exponent=separator<0?BigInteger.Zero:BigInteger.Parse(text[(separator+1)..],NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture);
            var sign=coefficient=="0"?0:coefficient[0]=='-'?-1:1;
            return(sign<0?coefficient[1..]:coefficient,exponent,sign);
        }
        var a=Parts(left);var b=Parts(right);
        if(a.Sign!=b.Sign){comparison=a.Sign.CompareTo(b.Sign);return true;}
        if(a.Sign==0)return true;
        var magnitude=(a.Exponent+a.Digits.Length).CompareTo(b.Exponent+b.Digits.Length);
        if(magnitude==0)
            for(var index=0;index<Math.Max(a.Digits.Length,b.Digits.Length);index++){
                magnitude=(index<a.Digits.Length?a.Digits[index]:'0').CompareTo(index<b.Digits.Length?b.Digits[index]:'0');
                if(magnitude!=0)break;
            }
        comparison=a.Sign*magnitude;return true;
    }
}
