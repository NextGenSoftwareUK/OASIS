using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.DNA;

/// <summary>OLAND commerce configuration. Secrets belong in deployed DNA/secret configuration, never source control.</summary>
public sealed class OLandCommerceSettings
{
    public bool Enabled { get; set; }
    public int MaximumParcelsPerOrder { get; set; } = 1024;
    public int FulfilmentPollSeconds { get; set; } = 10;
    public int QuoteMinutes { get; set; } = 10;
    public int CheckoutMinutes { get; set; } = 30;
    public string MongoDatabase { get; set; } = "oasis_oland";
    public string StripeWebhookSecret { get; set; }
    public string SuccessUrl { get; set; }
    public string CancelUrl { get; set; }
    public string CoinGeckoDemoApiKey { get; set; }
    public Dictionary<string, OLandChainSettings> Chains { get; set; } = new();
}

public sealed class OLandChainSettings
{
    public string ChainId { get; set; }
    public string TreasuryAddress { get; set; }
    public string NativeSymbol { get; set; }
    public string NativeAmountUnit { get; set; }
    public int NativeDecimals { get; set; }
    /// <summary>Enable only after the provider's exact applied transfer and finality evidence is verified.</summary>
    public bool NativePaymentEvidenceEnabled { get; set; }
}
