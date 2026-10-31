using System;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

internal static class NeoCqlIntegerDomain
{
    internal static bool TryBounds(Type declared,out (long Minimum,long Maximum) bounds)
    {
        var type=Nullable.GetUnderlyingType(declared)??declared;
        if(type==typeof(byte))bounds=(byte.MinValue,byte.MaxValue);
        else if(type==typeof(sbyte))bounds=(sbyte.MinValue,sbyte.MaxValue);
        else if(type==typeof(short))bounds=(short.MinValue,short.MaxValue);
        else if(type==typeof(ushort))bounds=(ushort.MinValue,ushort.MaxValue);
        else if(type==typeof(int))bounds=(int.MinValue,int.MaxValue);
        else if(type==typeof(uint))bounds=(uint.MinValue,uint.MaxValue);
        else if(type==typeof(long))bounds=(long.MinValue,long.MaxValue);
        else {bounds=default;return false;}
        return true;
    }
}
