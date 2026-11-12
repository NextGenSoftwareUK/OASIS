using System;
using NextGenSoftware.OASIS.API.Providers.SupabaseOASIS;

namespace NextGenSoftware.OASIS.API.Providers.SupabaseOASIS.TestHarness
{
    internal static class SupabaseOASISTestFactory
    {
        public static SupabaseOASIS Create()
        {
            var url = Environment.GetEnvironmentVariable("SUPABASEOASIS_SUPABASEURL");
            var apiKey = Environment.GetEnvironmentVariable("SUPABASEOASIS_APIKEY");
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Set SUPABASEOASIS_SUPABASEURL and SUPABASEOASIS_APIKEY for the disposable Supabase test project.");

            return new SupabaseOASIS(url, apiKey);
        }
    }
}
