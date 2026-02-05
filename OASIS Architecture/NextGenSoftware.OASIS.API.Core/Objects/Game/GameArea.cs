using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects.Game
{
    /// <summary>
    /// Represents a loaded game area with spatial coordinates
    /// </summary>
    public class GameArea : IHolon
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Guid GameId { get; set; }
        public Guid AvatarId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Radius { get; set; }
        public DateTime LoadedAt { get; set; }
        public bool IsActive { get; set; }
        
        // IHolon implementation
        public int Version { get; set; }
        public Guid CreatedByAvatarId { get; set; }
        public DateTime CreatedDate { get; set; }
        public Guid ModifiedByAvatarId { get; set; }
        public DateTime ModifiedDate { get; set; }
        public DateTime DeletedDate { get; set; }
        public int VersionId { get; set; }
        public bool IsActive1 { get; set; }
        public string PreviousVersionId { get; set; }
        public string ProviderMetaData { get; set; }
        public string MetaData { get; set; }
        public string MetaData2 { get; set; }
        public HolonType HolonType { get; set; }
    }
}

