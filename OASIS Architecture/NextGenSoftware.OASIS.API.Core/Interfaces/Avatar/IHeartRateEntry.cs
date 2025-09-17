using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Avatar
{
    public interface IHeartRateEntry
    {
        int HeartRateValue { get; set; }
        DateTime TimeStamp { get; set; }
    }
}