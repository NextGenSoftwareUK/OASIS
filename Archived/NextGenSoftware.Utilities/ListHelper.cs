
using System.Collections.Generic;

namespace NextGenSoftware.Utilities
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

        public static string ConvertFromList<T>(List<T> list)
        {
            string items = "";
            foreach (T id in list)
                items = string.Concat(items, id.ToString(), ",");

            items = items.Substring(0, items.Length - 1);
            return items;
        }
    }
}