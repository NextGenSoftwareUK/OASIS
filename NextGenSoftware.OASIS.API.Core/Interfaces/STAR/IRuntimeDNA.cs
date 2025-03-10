using System;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONode.Core.Holons
{
    public interface IRuntimeDNA
    {
        Guid CreatedByAvatarId { get; set; }
        string CreatedByAvatarUsername { get; set; }
        DateTime CreatedOn { get; set; }
        string Description { get; set; }
        int Downloads { get; set; }
        Guid Id { get; set; }
        Guid InstalledBy { get; set; }
        string InstalledByAvatarUsername { get; set; }
        DateTime InstalledOn { get; set; }
        string InstalledPath { get; set; }
        bool IsActive { get; set; }
        string Name { get; set; }
        Guid PublishedByAvatarId { get; set; }
        string PublishedByAvatarUsername { get; set; }
        DateTime PublishedOn { get; set; }
        long RuntimeFileSize { get; set; }
        bool RuntimePublishedOnSTARNET { get; set; }
        ProviderType RuntimePublishedProviderType { get; set; }
        bool RuntimePublishedToCloud { get; set; }
        RuntimeType RunTimeType { get; set; }
        string Version { get; set; }
        int Versions { get; set; }
    }
}