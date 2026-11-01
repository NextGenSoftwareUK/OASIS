using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Services;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Stripe;
using Stripe.Checkout;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.OLand;

public sealed class OLandPaymentGateway : IOLandPaymentGateway
{
    private readonly OLandCommerceSettings settings;
    private readonly IConfiguration configuration;
    private readonly Func<IEnumerable<IOASISProvider>> providers;
    private readonly IAssetUsdQuoteSource prices;
    public OLandPaymentGateway(OLandCommerceSettings settings, IConfiguration configuration, Func<IEnumerable<IOASISProvider>> providers, IAssetUsdQuoteSource prices)
    { this.settings = settings; this.configuration = configuration; this.providers = providers; this.prices = prices; }
    public bool StripeAvailable => !string.IsNullOrWhiteSpace(SubscriptionStripeConfiguration.SecretKey(configuration)) &&
        !string.IsNullOrWhiteSpace(settings.StripeWebhookSecret) && SafeUrl(settings.SuccessUrl) && SafeUrl(settings.CancelUrl) && settings.CheckoutMinutes >= 30 && settings.CheckoutMinutes <= 1440;
    private StripeClient Client() => new(SubscriptionStripeConfiguration.SecretKey(configuration) ?? throw Error("Stripe is not configured."));
    public async Task<(string SessionId, string Url)> CreateStripe(OLandOrder order, CancellationToken token)
    {
        if (!StripeAvailable) throw Error("Stripe checkout is not configured.");
        var service = new SessionService(Client());
        Session session;
        if (order.StripeSessionId != null) session = await service.GetAsync(order.StripeSessionId, cancellationToken: token);
        else
        {
            // Reconcile by metadata before creating, including after Stripe's idempotency cache expires.
            session = null;
            int inspected = 0;
            await foreach (var candidate in service.ListAutoPagingAsync(new SessionListOptions { Limit = 100,
                Created = new DateRangeOptions { GreaterThanOrEqual = order.CreatedUtc.AddMinutes(-1) } }, cancellationToken: token))
            {
                if (++inspected > 1000) throw Error("Stripe session reconciliation exceeded its scan limit; no replacement checkout was created.");
                if (candidate.Metadata?.GetValueOrDefault("oland_order_id") != order.Id) continue;
                if (session != null) throw Error("Multiple Stripe sessions match this OLAND order; reconciliation is required.");
                session = candidate;
            }
            if (session == null)
            {
                if (order.CreatedUtc < DateTime.UtcNow.AddHours(-23)) throw Error("Old checkout creation requires reconciliation; no replacement charge was created.");
                session = await service.CreateAsync(new SessionCreateOptions
                {
                    Mode = "payment", PaymentMethodTypes = new List<string> { "card" }, CustomerEmail = order.Email,
                    ClientReferenceId = order.Id, SuccessUrl = settings.SuccessUrl, CancelUrl = settings.CancelUrl,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(settings.CheckoutMinutes),
                    LineItems = new List<SessionLineItemOptions> { new() { Quantity = 1, PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd", UnitAmount = order.AmountMinorUsd,
                        ProductData = new SessionLineItemPriceDataProductDataOptions { Name = order.ParcelIds.Count + " OLAND (1 square metre each)" }
                    } } },
                    Metadata = new Dictionary<string, string> { { "oland_order_id", order.Id }, { "oland_avatar_id", order.AvatarId }, { "oland_fingerprint", order.Fingerprint } }
                }, new RequestOptions { IdempotencyKey = "oland-checkout-" + order.Id }, token);
            }
        }
        ValidateStripe(order, session);
        if (session.Status == "expired") throw Error("The Stripe checkout expired; refresh the order status.");
        if (!Uri.TryCreate(session.Url, UriKind.Absolute, out var url) || url.Scheme != "https" || url.Host != "checkout.stripe.com")
            throw Error("Stripe returned no valid hosted Checkout URL.");
        return (session.Id, session.Url);
    }
    public async Task<(string Status, string PaymentIdentity, DateTime PaidUtc)> VerifyStripe(OLandOrder order, CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(order.StripeSessionId)) throw Error("No Stripe session is attached to this order.");
        var session = await new SessionService(Client()).GetAsync(order.StripeSessionId, cancellationToken: token);
        ValidateStripe(order, session);
        if (session.PaymentStatus == "paid") return ("paid", "stripe:" + session.Id, DateTime.UtcNow);
        return (session.Status == "expired" ? "expired" : "pending", null, default);
    }
    internal static void ValidateStripe(OLandOrder order, Session session)
    {
        if (session == null || session.Mode != "payment" || session.AmountTotal != order.AmountMinorUsd || session.Currency != "usd" ||
            session.ClientReferenceId != order.Id || session.Metadata?.GetValueOrDefault("oland_order_id") != order.Id ||
            session.Metadata?.GetValueOrDefault("oland_avatar_id") != order.AvatarId || session.Metadata?.GetValueOrDefault("oland_fingerprint") != order.Fingerprint ||
            (order.StripeSessionId != null && session.Id != order.StripeSessionId))
            throw Error("The verified Stripe session does not match this order, owner, amount and currency.");
    }
    public async Task PrepareNative(OLandOrder order, CancellationToken token)
    {
        var chain = Chain(order.ProviderType);
        var result = await prices.GetUsdQuoteAsync(chain.NativeSymbol, token);
        if (result.IsError || result.Result == null || result.Result.PriceUsd <= 0 || result.Result.FetchedAtUtc < DateTime.UtcNow.AddMinutes(-5) ||
            result.Result.PublishedAtUtc < DateTime.UtcNow.AddMinutes(-5)) throw Error("A current native-token USD price is unavailable.");
        decimal scale = DecimalScale(chain.NativeDecimals);
        decimal amount = decimal.Ceiling((order.AmountMinorUsd / 100m) / result.Result.PriceUsd * scale);
        order.NativeAmountRaw = amount.ToString("0", CultureInfo.InvariantCulture);
        order.NativeDecimals = chain.NativeDecimals; order.NativeSymbol = chain.NativeSymbol;
        order.NativeAmountUnit = chain.NativeAmountUnit; order.TreasuryAddress = chain.TreasuryAddress;
        order.PaymentMemo = "oland:" + order.Id;
        order.NativeUsdPrice = result.Result.PriceUsd; order.NativePriceAssetId = result.Result.AssetId;
        order.NativePriceSource = result.Result.Source; order.NativePriceVersion = result.Result.Version;
        order.NativePricePublishedUtc = result.Result.PublishedAtUtc; order.NativePriceFetchedUtc = result.Result.FetchedAtUtc;
    }
    public async Task<(string PaymentIdentity, DateTime PaidUtc)> VerifyNative(OLandOrder order, string transaction, CancellationToken token)
    {
        var chain = Chain(order.ProviderType);
        if (chain.ChainId != order.ChainId || chain.TreasuryAddress != order.TreasuryAddress || chain.NativeSymbol != order.NativeSymbol ||
            chain.NativeAmountUnit != order.NativeAmountUnit || chain.NativeDecimals != order.NativeDecimals)
            throw Error("The chain payment configuration changed; original payment requires reconciliation.");
        var provider = providers().SingleOrDefault(p => p.ProviderType.Value.ToString() == order.ProviderType) as IOASISPaymentReceiptProvider
            ?? throw Error("The selected provider does not expose authoritative native payment receipts.");
        var result = await provider.GetPaymentReceiptAsync(transaction, token);
        var receipt = result.Result;
        if (result.IsError || receipt == null || !receipt.Finalized || !receipt.Succeeded || receipt.TransactionId != transaction ||
            receipt.ProviderType.ToString() != order.ProviderType || receipt.ChainId != order.ChainId || receipt.Memo != order.PaymentMemo ||
            receipt.CreatedUtc.Kind != DateTimeKind.Utc || receipt.CreatedUtc < order.CreatedUtc || receipt.CreatedUtc > order.ExpiresUtc)
            throw Error("Finalized native receipt does not match this order's provider, network, memo and payment window.");
        var candidates = receipt.Transfers?.Where(t => t.FromAddress == order.PayingWallet && t.ToAddress == order.TreasuryAddress &&
            t.Token == order.NativeSymbol && t.AmountUnit == order.NativeAmountUnit).ToList();
        if (candidates?.Count != 1 || !BigInteger.TryParse(candidates[0].RawAmount, NumberStyles.None, CultureInfo.InvariantCulture, out var raw) ||
            !BigInteger.TryParse(order.NativeAmountRaw, NumberStyles.None, CultureInfo.InvariantCulture, out var expected) || raw != expected)
            throw Error("Native payment does not uniquely match the quoted sender, receiver, token and exact raw amount.");
        return ("chain:" + order.ProviderType + ":" + order.ChainId + ":" + transaction, receipt.CreatedUtc);
    }

    private OLandChainSettings Chain(string provider)
    {
        if (!settings.Chains.TryGetValue(provider, out var chain) || !chain.NativePaymentEvidenceEnabled || string.IsNullOrWhiteSpace(chain.TreasuryAddress) ||
            string.IsNullOrWhiteSpace(chain.NativeSymbol) || string.IsNullOrWhiteSpace(chain.NativeAmountUnit) || string.IsNullOrWhiteSpace(chain.ChainId))
            throw Error("Verified native payments are not configured for this chain.");
        return chain;
    }
    private static decimal DecimalScale(int decimals)
    { if (decimals < 0 || decimals > 18) throw Error("Unsupported native token precision."); decimal scale = 1; for (int i = 0; i < decimals; i++) scale *= 10; return scale; }
    private static bool SafeUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo);
    private static OLandCommerceException Error(string message) => new("OLAND_PAYMENT_UNVERIFIED", message);
}
