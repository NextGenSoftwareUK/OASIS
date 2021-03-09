using System;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class EnumHelper
    {
        public static string GetEnumValues(Type enumType)
        {
            string[] values = Enum.GetNames(enumType);
            string enumValues = "";

            foreach (string value in values)
            {
                if (enumValues == "")
                    enumValues = value;
                else
                    enumValues = string.Concat(enumValues, ",", value);
            }

            return enumValues;
        }
    }
}
