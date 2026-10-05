using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Infrastructure.Extensions
{
    public static class EnumExtension
    {
        public static string GetDescription(this System.Enum value)
        {
            return value?.GetType()
                .GetMember(value.ToString())
                .FirstOrDefault()
                ?.GetCustomAttribute<DescriptionAttribute>()
                ?.Description;
        }
    }
}