using NextGenSoftware.OASIS.API.Core.Enums;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class ListHelper
    {
        public static List<string> ConvertToList(string commaDelimitedList)
        {
            List<string> list = new List<string>();
            string[] parts = commaDelimitedList.Split(',');
            list.AddRange(parts);
            return list;
        }
    }
}