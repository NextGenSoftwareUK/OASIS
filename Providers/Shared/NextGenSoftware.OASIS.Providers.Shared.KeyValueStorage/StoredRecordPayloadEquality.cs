using System;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

/// <summary>JSON value equivalence for canonical domain acknowledgements; excludes no fields.</summary>
public static class StoredRecordPayloadEquality
{
    public static bool Equivalent(JToken expected,JToken actual)
    {
        if(expected==null || actual==null)return expected==actual;
        if(IsNumber(expected) && IsNumber(actual))return Number(expected)==Number(actual);
        if(expected is JObject left && actual is JObject right)
            return left.Count==right.Count && left.Properties().All(property=>
                right.TryGetValue(property.Name,StringComparison.Ordinal,out var value) && Equivalent(property.Value,value));
        if(expected is JArray a && actual is JArray b)
            return a.Count==b.Count && a.Zip(b,Equivalent).All(equal=>equal);
        return JToken.DeepEquals(expected,actual);
    }

    private static bool IsNumber(JToken token)=>token.Type is JTokenType.Integer or JTokenType.Float;

    // Normalize serialized decimal coefficient/exponent, without converting large integers
    // to double, rounding through decimal, or allocating powers for large exponents.
    private static (string Coefficient,long Exponent) Number(JToken token)
    {
        var text=token.ToString(Formatting.None);
        var index=text.IndexOfAny(new[]{'e','E'});
        var exponent=index<0?0:long.Parse(text.Substring(index+1),NumberStyles.AllowLeadingSign,CultureInfo.InvariantCulture);
        var digits=index<0?text:text.Substring(0,index);
        var negative=digits.StartsWith("-",StringComparison.Ordinal);
        if(negative)digits=digits.Substring(1);
        var point=digits.IndexOf('.');
        if(point>=0){exponent=checked(exponent-(digits.Length-point-1));digits=digits.Remove(point,1);}
        digits=digits.TrimStart('0');
        if(digits.Length==0)return("0",0);
        var normalized=digits.TrimEnd('0');
        exponent=checked(exponent+digits.Length-normalized.Length);
        return((negative?"-":"")+normalized,exponent);
    }
}
