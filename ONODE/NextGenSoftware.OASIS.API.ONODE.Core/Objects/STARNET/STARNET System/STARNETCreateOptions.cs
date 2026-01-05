using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Objects
{
    public class STARNETCreateOptions<T1, T2> : ISTARNETCreateOptions<T1, T2> 
        where T1 : ISTARNETHolon, new()
        where T2 : ISTARNETDNA, new()
    {
        public T1 STARNETHolon { get; set; }
        public T2 STARNETDNA { get; set; }
        //public string DefaultSTARNETCategory { get; set; }
        public MetaTagMappings MetaTagMappings { get; set; }
        public bool CheckIfSourcePathExists { get; set; }
        public Dictionary<string, object> CustomCreateParams { get; set; }
    }
}
