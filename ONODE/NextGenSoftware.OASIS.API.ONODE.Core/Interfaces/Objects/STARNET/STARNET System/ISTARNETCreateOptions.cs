using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Objects.STARNET;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces
{
    public interface ISTARNETCreateOptions<T1, T2>
        where T1 : ISTARNETHolon, new()
        where T2 : ISTARNETDNA, new()
    {
        T1 STARNETHolon { get; set; }
        T2 STARNETDNA { get; set; }
        MetaTagMappings MetaTagMappings { get; set; }
        bool CheckIfSourcePathExists { get; set; }
        Dictionary<string, object> CustomCreateParams { get; set; }
    }
}