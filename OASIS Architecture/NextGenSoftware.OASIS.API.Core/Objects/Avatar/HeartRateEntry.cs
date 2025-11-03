
using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;

namespace NextGenSoftware.OASIS.API.Core.Objects.Avatar
{
    public class HeartRateEntry : IHeartRateEntry
    {
        public int HeartRateValue { get; set; }
        public DateTime TimeStamp { get; set; }
    }
}
