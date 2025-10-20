using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Holons;
using System.Text.Json.Serialization;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IHolonBase : IAuditBase
    {
        Guid Id { get; set; }
        string Name { get; set; }
        string Description { get; set; }
        HolonType HolonType { get; set; }
        bool IsActive { get; set; }
        Dictionary<string, object> MetaData { get; set; }
    }
}