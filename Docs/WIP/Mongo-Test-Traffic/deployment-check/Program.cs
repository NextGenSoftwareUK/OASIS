using System;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

var source=new Holon { Name="validator regression" };
var legacy=HolonGraphWritePlan.Create(new IHolon[]{source},false,false,0,0);
if(legacy.Nodes.Count!=1 || source.Id!=Guid.Empty) throw new Exception("Legacy planning contract changed");
var called=0;
var validated=HolonGraphWritePlan.Create(new IHolon[]{source},false,false,0,0,h=>{if(!ReferenceEquals(h,source))throw new Exception("Wrong source");called++;});
if(called!=1 || validated.Nodes.Count!=1 || source.Id!=Guid.Empty) throw new Exception("Validation/planning contract changed");
try {
    HolonGraphWritePlan.Create(new IHolon[]{source},false,false,0,0,h=>throw new InvalidOperationException("reject-source"));
    throw new Exception("Validator failure ignored");
} catch(InvalidOperationException e) when(e.Message=="reject-source") {}
if(source.Id!=Guid.Empty) throw new Exception("Validation published caller identity");
Console.WriteLine("PASS: legacy overload, source validator, propagated rejection and unchanged caller identity");
