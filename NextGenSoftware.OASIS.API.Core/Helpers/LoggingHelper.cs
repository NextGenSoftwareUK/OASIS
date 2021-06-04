using System;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class LoggingHelper
    {
        public static string GetHolonInfoForLogging(IHolon holon)
        {
            return string.Concat("holon with id ", holon.Id, " and name ", holon.Name, " of type ", Enum.GetName(holon.HolonType));
        }
    }
}