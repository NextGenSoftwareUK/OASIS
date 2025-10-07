using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core
{
    // Simple IRequest implementation used to route storage operations through HyperDrive
    public class StorageOperationRequest : IRequest
	{
		public string Operation { get; set; } = string.Empty; // e.g. "SaveHolon", "LoadHolon"
		public Guid AvatarId { get; set; }
		public Guid HolonId { get; set; }
		public string ProviderKey { get; set; }
		public object Payload { get; set; }
		public ProviderType PreferredProvider { get; set; } = ProviderType.Default;
	}
}


