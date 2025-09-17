using System;
using System.Collections.Generic;
using System.Dynamic;

namespace NextGenSoftware.Utilities.ExtentionMethods
{
    public static class ExpandoObjectHelpers
    {
        public static void AddProperty(ExpandoObject expando, string propertyName, object propertyValue)
        {
            var exDict = expando as IDictionary<string, object>;

            if (exDict.ContainsKey(propertyName))
                exDict[propertyName] = propertyValue;
            else
                exDict.Add(propertyName, propertyValue);
        }
    }
}