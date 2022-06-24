using NextGenSoftware.OASIS.API.Core.Enums;
using System;
using System.Collections.Generic;

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

        public static OASISResult<IEnumerable<EnumValue<T>>> ConvertToEnumValueList<T>(IEnumerable<T> list)
        {
            OASISResult<IEnumerable<EnumValue<T>>> result = new OASISResult<IEnumerable<EnumValue<T>>>();
            List<EnumValue<T>> enumList = new List<EnumValue<T>>();

            foreach (T item in list)
                enumList.Add(new EnumValue<T>(item));

            result.Result = enumList;
            return result;
        }

        public static OASISResult<IEnumerable<T>> ConvertFromEnumValueList<T>(IEnumerable<EnumValue<T>> enumList)
        {
            OASISResult<IEnumerable<T>> result = new OASISResult<IEnumerable<T>>();
            List<T> list = new List<T>();

            foreach (EnumValue<T> item in enumList)
                list.Add(item.Value);

            result.Result = list;
            return result;
        }
    }
}