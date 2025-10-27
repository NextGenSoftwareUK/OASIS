using System;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IAuditBase
    {
        [JsonIgnore]
        IAvatar CreatedByAvatar { get; set; }
        Guid CreatedByAvatarId { get; set; }
        DateTime CreatedDate { get; set; }

        [JsonIgnore]
        IAvatar ModifiedByAvatar { get; set; }
        Guid ModifiedByAvatarId { get; set; }
        DateTime ModifiedDate { get; set; }

        [JsonIgnore]
        IAvatar DeletedByAvatar { get; set; }
        Guid DeletedByAvatarId { get; set; }
        DateTime DeletedDate { get; set; }
        int Version { get; set; }
        Guid VersionId { get; set; }
    }
}