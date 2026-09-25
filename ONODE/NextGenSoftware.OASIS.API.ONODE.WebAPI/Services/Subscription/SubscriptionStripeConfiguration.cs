using System;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    /// <summary>Uses the existing OASIS DNA Stripe settings, with standard deployment overrides.</summary>
    public static class SubscriptionStripeConfiguration
    {
        public static string SecretKey(IConfiguration configuration) =>
            Value(configuration, "STRIPE_SECRET_KEY", stripe => stripe.SecretKey);

        public static string WebhookSecret(IConfiguration configuration) =>
            Value(configuration, "STRIPE_WEBHOOK_SECRET", stripe => stripe.WebhookSecret);

        public static string PriceId(IConfiguration configuration, string planId)
        {
            string normalized = planId?.ToLowerInvariant();
            string setting = normalized switch
            {
                "bronze" => "STRIPE_PRICE_BRONZE",
                "silver" => "STRIPE_PRICE_SILVER",
                "gold" => "STRIPE_PRICE_GOLD",
                "enterprise" => "STRIPE_PRICE_ENTERPRISE",
                _ => null
            };
            if (setting == null) return null;
            return Value(configuration, setting, stripe => normalized switch
            {
                "bronze" => stripe.PriceBronze,
                "silver" => stripe.PriceSilver,
                "gold" => stripe.PriceGold,
                "enterprise" => stripe.PriceEnterprise,
                _ => null
            });
        }

        private static string Value(IConfiguration configuration, string overrideName,
            Func<StripeSettings, string> dnaValue)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            string value = configuration[overrideName];
            if (!string.IsNullOrWhiteSpace(value)) return value;
            var stripe = LoadedStripe(configuration);
            value = stripe == null ? null : dnaValue(stripe);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static StripeSettings LoadedStripe(IConfiguration configuration)
        {
            var stripe = OASISDNAManager.OASISDNA?.OASIS?.SubscriptionConfig?.Stripe;
            if (stripe != null) return stripe;
            string dnaPath = configuration[SubscriptionMongoConfiguration.DnaPathSetting];
            var loaded = string.IsNullOrWhiteSpace(dnaPath)
                ? OASISDNAManager.LoadDNA()
                : OASISDNAManager.LoadDNA(dnaPath);
            return loaded.IsError || loaded.Result == null
                ? null
                : loaded.Result.OASIS?.SubscriptionConfig?.Stripe;
        }
    }
}
