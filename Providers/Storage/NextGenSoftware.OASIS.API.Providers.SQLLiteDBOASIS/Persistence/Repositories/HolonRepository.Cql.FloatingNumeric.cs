using System;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private static bool FloatingNumeric(Type declared)
    {
        if (declared == null) return false;
        var type=Nullable.GetUnderlyingType(declared) ?? declared;
        return type==typeof(float) || type==typeof(double);
    }
    private static OASISResult<object> DecodeFloatingNumeric(CosmicScalarOperation operation,
        Type declared, long inputs, long present, byte[] bytes, bool grouped)
    {
        var type=Nullable.GetUnderlyingType(declared) ?? declared;
        var packet=SqliteCqlFloatingAggregates.ReadPacket(bytes,type==typeof(float));
        if(packet.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<SqliteCqlFloatingAggregates.Packet,object>(packet);
        if(inputs>CosmicNumericScalarResults.MaximumParticipatingValues)
            return new OASISResult<object> { IsError=true,ErrorCount=1,ErrorCode="CQL_QUERY_LIMIT",Message="Floating input proof exceeds its work bound." };
        if(inputs<(grouped?1:0) || present<0 || present>inputs
            || Nullable.GetUnderlyingType(declared)==null && present!=inputs)
            return new OASISResult<object> { IsError=true,ErrorCount=1,ErrorCode="CQL_DATA_INVALID",Message="Floating participation contradicts its scalar/grouped source proof." };
        if(packet.Result.Count!=present)
            return new OASISResult<object> { IsError=true,ErrorCount=1,ErrorCode="CQL_DATA_INVALID",Message="Floating packet participation disagrees with native source proof." };
        return CosmicFloatingAggregateResults.FromParts(operation,declared,packet.Result.Parts,packet.Result.Count);
    }
}
