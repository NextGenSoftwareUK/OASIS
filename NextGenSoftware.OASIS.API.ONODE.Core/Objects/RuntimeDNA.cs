using System;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class RuntimeDNA : OAPPSystemHolonDNA, IRuntimeDNA
    {
        public Guid Id { get; set; }
        //public string Name
        //{
        //    get
        //    {
        //        return string.Concat(Enum.GetName(typeof(RuntimeType), RuntimeType), " ", Version);
        //    }
        //}
    }
}
