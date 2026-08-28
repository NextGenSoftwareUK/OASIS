namespace NextGenSoftware.OASIS.API.DNA
{
    /// <summary>
    /// Stripe payment integration settings — lives under OASISDNA.OASIS.SubscriptionConfig.Stripe.
    /// Environment variables always override these values at runtime.
    /// Use these fields for local development only — never commit real keys.
    /// Env vars: STRIPE_SECRET_KEY, STRIPE_PUBLISHABLE_KEY, STRIPE_WEBHOOK_SECRET,
    ///           STRIPE_PRICE_BRONZE, STRIPE_PRICE_SILVER, STRIPE_PRICE_GOLD, STRIPE_PRICE_ENTERPRISE.
    /// Get Price IDs from: Stripe Dashboard → Products → your plan → Pricing → copy price_xxx.
    /// </summary>
    public class StripeSettings
    {
        public string SecretKey { get; set; } = "";
        public string PublishableKey { get; set; } = "";
        public string WebhookSecret { get; set; } = "";
        public string PriceBronze { get; set; } = "";
        public string PriceSilver { get; set; } = "";
        public string PriceGold { get; set; } = "";
        public string PriceEnterprise { get; set; } = "";
    }
}
