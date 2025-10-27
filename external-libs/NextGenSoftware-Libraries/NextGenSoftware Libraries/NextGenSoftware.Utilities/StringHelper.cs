
namespace NextGenSoftware.Utilities
{
    public class StringHelper
    {
        public static bool IsValidVersion(string version)
        {
            var parts = version.Split('.');
            if (parts.Length > 4) return false;

            foreach (var part in parts)
            {
                if (!int.TryParse(part, out _))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
