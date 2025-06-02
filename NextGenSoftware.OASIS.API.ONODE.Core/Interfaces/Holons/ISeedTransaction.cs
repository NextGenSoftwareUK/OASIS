using System;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface ISeedTransaction : IHolon
    {
        int Amount { get; set; }
        Guid AvatarId { get; set; }
        string AvatarUserName { get; set; }
        string Memo { get; set; }
    }
}