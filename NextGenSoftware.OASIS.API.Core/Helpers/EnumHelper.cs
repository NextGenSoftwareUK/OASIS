using NextGenSoftware.OASIS.API.Core.Enums;
using System;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class EnumHelper
    {
        public static string GetEnumValues(Type enumType, EnumHelperListType listType = EnumHelperListType.ItemsSeperatedByNewLine)
        {
            string[] values = Enum.GetNames(enumType);
            string enumValues = "";

            switch (listType)
            {
                case EnumHelperListType.ItemsSeperatedByNewLine:
                {
                    foreach (string value in values)
                    {
                        if (enumValues == "")
                            enumValues = value;
                        else
                            enumValues = string.Concat(enumValues, "\n", value);
                    }
                }
                break;

                case EnumHelperListType.ItemsSeperatedByComma:
                {
                    for (int i = 0; i < values.Length; i++)
                    {
                        enumValues = string.Concat(enumValues, values[i]);

                        if (i < values.Length - 2)
                            enumValues = string.Concat(enumValues, ", ");

                        else if (i == values.Length - 2)
                            enumValues = string.Concat(enumValues, " or ");
                    }
                }
                break;
            }

            return enumValues;
        }
    }
}