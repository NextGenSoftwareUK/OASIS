using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Core.Objects.Game
{
    /// <summary>
    /// Represents a loaded game area with spatial coordinates
    /// </summary>
    public class GameArea : Holon
    {
        public Guid GameId { get; set; }
        public Guid AvatarId { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public double Radius { get; set; }
        public DateTime LoadedAt { get; set; }
    }
}
