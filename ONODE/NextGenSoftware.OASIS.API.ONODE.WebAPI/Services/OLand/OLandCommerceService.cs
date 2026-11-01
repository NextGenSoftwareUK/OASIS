using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.OLand;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.OLand;

/// <summary>The shared Unity/web commerce boundary. Clients never decide area, price, payment status or ownership.</summary>
public sealed class OLandCommerceService
{
    private readonly OLandCommerceSettings settings;
    private readonly IOLandOrderStore store;
    private readonly IOLandPaymentGateway payments;
    private readonly IOLandNftIssuer issuer;
    private readonly Func<int, Task<int>> price;
    private readonly Func<IEnumerable<IOASISProvider>> providers;
    private readonly Func<DateTime> now;
    public OLandCommerceService(OLandCommerceSettings settings, IOLandOrderStore store, IOLandPaymentGateway payments,
        IOLandNftIssuer issuer, Func<int, Task<int>> price, Func<IEnumerable<IOASISProvider>> providers, Func<DateTime> now = null)
    {
        this.settings = settings; this.store = store; this.payments = payments; this.issuer = issuer;
        this.price = price; this.providers = providers; this.now = now ?? (() => DateTime.UtcNow);
    }
    public object Options() => new
    {
        Enabled = settings.Enabled, ParcelWidthMetres = 1, ParcelHeightMetres = 1, Currency = "usd",
        MaximumParcelsPerOrder = settings.MaximumParcelsPerOrder,
        PaymentMethods = new[] { new { Id = "stripe", Name = "Stripe / Card", Available = settings.Enabled && payments.StripeAvailable },
            new { Id = "blockchain", Name = "Blockchain wallet", Available = settings.Enabled && Chains().Any(c => NativeEnabled(c.ProviderType)) } },
        Chains = Chains(), DefaultProvider = "SolanaOASIS"
    };
    public sealed record ChainOption(string ProviderType, string Name, string ChainId, bool NativePaymentAvailable);
    public List<ChainOption> Chains() => providers().OfType<IOASISNFTProvider>()
        .Where(p => p.ProviderCapabilities?.Any(c => c.Value is ProviderCategory.Blockchain or ProviderCategory.EVMBlockchain) == true)
        .Select(p => p.ProviderType.Value.ToString()).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)
        .Where(p => settings.Chains.ContainsKey(p))
        .Select(p => new ChainOption(p, p.EndsWith("OASIS", StringComparison.Ordinal) ? p[..^5] : p,
            settings.Chains[p].ChainId, NativeEnabled(p))).ToList();
    private bool NativeEnabled(string provider) => settings.Chains.TryGetValue(provider, out var chain) &&
        chain.NativePaymentEvidenceEnabled && providers().Any(p => p.ProviderType.Value.ToString() == provider && p is IOASISPaymentReceiptProvider) && !string.IsNullOrWhiteSpace(chain.TreasuryAddress) &&
        !string.IsNullOrWhiteSpace(chain.NativeSymbol) && !string.IsNullOrWhiteSpace(chain.NativeAmountUnit) &&
        chain.NativeDecimals >= 0 && chain.NativeDecimals <= 18 && !string.IsNullOrWhiteSpace(settings.CoinGeckoDemoApiKey);

    public async Task<object> Grid(double latitude, double longitude, int width, CancellationToken token)
    {
        var cells = OLandParcelGrid.Around(latitude, longitude, width);
        var claims = settings.Enabled ? await store.Claims(cells.Select(c => c.Id).ToArray(), token) : new Dictionary<string, OLandParcelClaim>();
        foreach (string orderId in claims.Values.Where(c => !c.Owned).Select(c => c.OrderId).Distinct())
        {
            var order = await store.Get(orderId, token);
            if (order?.State == "quoted" && order.ExpiresUtc <= now()) await store.Expire(order.Id, "quoted", token);
        }
        if (claims.Count > 0) claims = await store.Claims(cells.Select(c => c.Id).ToArray(), token);
        return new { ParcelWidthMetres = 1, ParcelHeightMetres = 1, Parcels = cells.Select(c => new
        {
            c.Id, c.Boundary, c.WidthMetres, c.HeightMetres, c.AreaSquareMetres,
            Availability = !settings.Enabled ? "unavailable" : !claims.TryGetValue(c.Id, out var claim) ? "available" : claim.Owned ? "owned" : "reserved"
        }).ToArray() };
    }

    public async Task<OLandOrder> Quote(Guid actor, string idempotencyKey, OLandQuoteRequest request, CancellationToken token)
    {
        Enabled();
        if (actor == Guid.Empty || !Guid.TryParseExact(idempotencyKey, "D", out var requestId) || requestId == Guid.Empty)
            throw Error("OLAND_REQUEST_INVALID", "Authenticated avatar and UUID Idempotency-Key required.");
        if (settings.MaximumParcelsPerOrder < 1 || settings.MaximumParcelsPerOrder > 1024 || settings.QuoteMinutes < 1 || settings.QuoteMinutes > 60)
            throw Error("OLAND_CONFIGURATION_INVALID", "OLAND quote limits are invalid.");
        if (request?.ParcelIds == null || request.ParcelIds.Count < 1 || request.ParcelIds.Count > settings.MaximumParcelsPerOrder ||
            request.ParcelIds.Distinct(StringComparer.Ordinal).Count() != request.ParcelIds.Count)
            throw Error("OLAND_SELECTION_INVALID", "Select distinct parcels within the order limit.");
        foreach (string parcel in request.ParcelIds) OLandParcelGrid.Parse(parcel);
        if (request.PaymentMethod is not ("stripe" or "blockchain") || !Chains().Any(c => c.ProviderType == request.ProviderType))
            throw Error("OLAND_METHOD_UNAVAILABLE", "Choose an available payment method and ownership chain.");
        Bounded(request.ReceivingWallet, 256, "Receiving wallet");
        Bounded(request.Email, 254, "Email");
        if (!MailAddress.TryCreate(request.Email, out var email) || email.Address != request.Email)
            throw Error("OLAND_FORM_INVALID", "Enter a valid email address.");
        Bounded(request.IntendedUse, 2000, "Intended land use");
        if (request.PaymentMethod == "stripe" && !payments.StripeAvailable) throw Error("OLAND_STRIPE_UNAVAILABLE", "Card checkout is not configured.");
        if (request.PaymentMethod == "blockchain")
        {
            if (!NativeEnabled(request.ProviderType)) throw Error("OLAND_CHAIN_PAYMENT_UNAVAILABLE", "Verified native payments are unavailable for this chain; use card checkout if offered.");
            Bounded(request.PayingWallet, 256, "Paying wallet");
        }
        if (!Enum.TryParse<NFTStandardType>(settings.Chains[request.ProviderType].NftStandard, false, out var nftStandard) ||
            !Uri.TryCreate(settings.PublicApiBaseUrl, UriKind.Absolute, out var publicApi) || publicApi.Scheme != "https")
            throw Error("OLAND_CONFIGURATION_INVALID", "The ownership chain NFT standard and public metadata API must be configured before accepting payment.");
        var standardCheck = new NextGenSoftware.OASIS.API.ONODE.Core.Managers.NFTManager(actor)
            .IsNFTStandardTypeValid(nftStandard, Enum.Parse<ProviderType>(request.ProviderType));
        if (standardCheck.IsError) throw Error("OLAND_CONFIGURATION_INVALID", standardCheck.Message);
        string[] ids = request.ParcelIds.OrderBy(p => p, StringComparer.Ordinal).ToArray();
        string fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        { Actor = actor, ParcelIds = ids, request.PaymentMethod, request.ProviderType, request.ReceivingWallet, request.PayingWallet, request.Email, request.IntendedUse }))));
        var replay = await store.FindRequest(actor.ToString("D"), requestId.ToString("D"), token);
        if (replay != null)
        {
            if (replay.Fingerprint != fingerprint) throw Error("OLAND_CONFLICT", "Idempotency-Key belongs to different purchase details.");
            return replay;
        }
        int amountUsd = await price(ids.Length);
        if (amountUsd <= 0) throw Error("OLAND_PRICE_UNAVAILABLE", "A positive OLAND price is required.");
        var order = new OLandOrder
        {
            Id = Guid.NewGuid().ToString("D"), AvatarId = actor.ToString("D"), ActiveAvatar = actor.ToString("D"), RequestId = requestId.ToString("D"),
            Fingerprint = fingerprint, ParcelIds = ids.ToList(), AmountMinorUsd = checked((long)amountUsd * 100), AreaSquareMetres = ids.Length,
            PaymentMethod = request.PaymentMethod, ProviderType = request.ProviderType, ReceivingWallet = request.ReceivingWallet,
            PayingWallet = request.PayingWallet, Email = request.Email, IntendedUse = request.IntendedUse,
            NftStandard = nftStandard.ToString(), CreatedUtc = now(), ExpiresUtc = now().AddMinutes(settings.QuoteMinutes), ChainId = settings.Chains[request.ProviderType].ChainId
        };
        if (request.PaymentMethod == "blockchain") await payments.PrepareNative(order, token);
        return await store.Reserve(order, token);
    }

    public async Task<object> PublicNftMetadata(string id, CancellationToken token)
    {
        var order = await store.Get(id, token);
        if (order == null || order.State != "paid") throw Error("OLAND_ORDER_NOT_FOUND", "Paid OLAND ownership metadata not found.");
        return new { name = "OLAND " + order.Id, symbol = "OLAND",
            description = order.ParcelIds.Count + " OLAND parcels, each 1 by 1 projected metre.",
            attributes = new[] { new { trait_type = "Area (square metres)", value = order.AreaSquareMetres } },
            properties = new { grid = "WGS84 UTM v1", parcelIds = order.ParcelIds,
                boundaries = order.ParcelIds.Select(p => OLandParcelGrid.Parse(p).Boundary).ToArray() } };
    }

    public async Task<object> History(Guid actor, string beforeOrderId, int limit, CancellationToken token)
    {
        if (actor == Guid.Empty) throw Error("OLAND_ORDER_NOT_FOUND", "Authenticated avatar required.");
        var orders = await store.List(actor.ToString("D"), beforeOrderId, limit, token);
        return new { Orders = orders, NextCursor = orders.Count == limit ? orders.Last().Id : null };
    }

    public async Task<OLandOrder> BeginCheckout(Guid actor, string orderId, CancellationToken token)
    {
        Enabled();
        var order = await OwnedOrder(actor, orderId, token);
        if (order.State == "paid") return order;
        if (order.State == "quoted")
        {
            if (string.IsNullOrWhiteSpace(order.NftStandard)) throw Error("OLAND_QUOTE_REFRESH_REQUIRED", "Cancel this older quote and request a new one before payment.");
            if (order.ExpiresUtc <= now()) throw Error("OLAND_QUOTE_EXPIRED", "The quote expired. Request a new quote before paying.");
            if (!await store.Transition(order.Id, "quoted", order.PaymentMethod == "stripe" ? "checkout_starting" : "payment_pending", token))
                return await OwnedOrder(actor, orderId, token);
            order.State = order.PaymentMethod == "stripe" ? "checkout_starting" : "payment_pending";
        }
        if (order.PaymentMethod == "stripe" && order.State == "checkout_starting")
        {
            var session = await payments.CreateStripe(order, token);
            await store.AttachStripe(order.Id, session.SessionId, session.Url, token);
            order.StripeSessionId = session.SessionId; order.CheckoutUrl = session.Url;
        }
        if (order.State is not ("checkout_starting" or "payment_pending")) throw Error("OLAND_ORDER_CLOSED", "This order cannot start checkout.");
        return order;
    }
    public async Task<OLandOrder> Status(Guid actor, string orderId, CancellationToken token)
    {
        var order = await OwnedOrder(actor, orderId, token);
        if (order.State == "quoted" && order.ExpiresUtc <= now())
        {
            await store.Expire(order.Id, "quoted", token);
            return await store.Get(order.Id, token);
        }
        if (order.PaymentMethod == "stripe" && order.StripeSessionId != null && order.State == "checkout_starting")
        {
            var payment = await payments.VerifyStripe(order, token);
            if (payment.Status == "paid") order = await store.GrantPaid(order.Id, payment.PaymentIdentity, payment.PaidUtc, token);
            else if (payment.Status == "expired") await store.Expire(order.Id, "checkout_starting", token);
        }
        return await store.Get(order.Id, token);
    }
    public async Task<OLandOrder> ConfirmNative(Guid actor, string orderId, string transaction, CancellationToken token)
    {
        Enabled();
        var order = await OwnedOrder(actor, orderId, token);
        if (order.State == "paid") return order;
        if (order.State != "payment_pending" || order.PaymentMethod != "blockchain") throw Error("OLAND_PAYMENT_NOT_EXPECTED", "This order is not awaiting a blockchain payment.");
        Bounded(transaction, 256, "Payment transaction ID");
        var payment = await payments.VerifyNative(order, transaction, token);
        return await store.GrantPaid(order.Id, payment.PaymentIdentity, payment.PaidUtc, token);
    }
    public async Task<OLandOrder> StripeNotification(string orderId, string sessionId, CancellationToken token)
    {
        var order = await store.Get(orderId, token) ?? throw Error("OLAND_ORDER_NOT_FOUND", "OLAND order not found.");
        if (order.PaymentMethod != "stripe" || (order.StripeSessionId != null && order.StripeSessionId != sessionId)) throw Error("OLAND_PAYMENT_IDENTITY_MISMATCH", "Stripe session does not match the order.");
        if (order.StripeSessionId == null) { await store.AttachStripe(order.Id, sessionId, null, token); order.StripeSessionId = sessionId; }
        var payment = await payments.VerifyStripe(order, token);
        if (payment.Status == "paid") return await store.GrantPaid(order.Id, payment.PaymentIdentity, payment.PaidUtc, token);
        if (payment.Status == "expired") await store.Expire(order.Id, "checkout_starting", token);
        return await store.Get(order.Id, token);
    }
    public async Task<OLandOrder> Fulfil(Guid actor, string id, CancellationToken token)
    {
        var order = await OwnedOrder(actor, id, token);
        if (order.State != "paid") throw Error("OLAND_PAYMENT_REQUIRED", "Verified payment is required before NFT issuance.");
        if (await store.BeginMint(id, token))
        {
            // One durable submission claim. A timeout/crash stays unresolved; it never blindly mints twice.
            try
            {
                var result = await issuer.Mint(order, token);
                await store.CompleteMint(id, result.TransactionId, result.Message, token);
            }
            catch (Exception ex)
            {
                await store.CompleteMint(id, null, "NFT delivery requires reconciliation: " + ex.Message, CancellationToken.None);
                throw;
            }
        }
        return await store.Get(id, token);
    }
    public async Task CancelQuote(Guid actor, string id, CancellationToken token)
    {
        var order = await OwnedOrder(actor, id, token);
        if (order.State != "quoted") throw Error("OLAND_PAYMENT_RECONCILIATION_REQUIRED", "An initiated payment must be reconciled before its reservation can be released.");
        await store.Expire(id, "quoted", token);
    }
    private async Task<OLandOrder> OwnedOrder(Guid actor, string id, CancellationToken token)
    {
        if (actor == Guid.Empty || !Guid.TryParseExact(id, "D", out _)) throw Error("OLAND_ORDER_NOT_FOUND", "Order not found.");
        var order = await store.Get(id, token);
        if (order == null || order.AvatarId != actor.ToString("D")) throw Error("OLAND_ORDER_NOT_FOUND", "Order not found.");
        return order;
    }
    private void Enabled() { if (!settings.Enabled) throw Error("OLAND_CHECKOUT_UNAVAILABLE", "OLAND checkout is not enabled on this service."); }
    private static void Bounded(string value, int maximum, string name)
    { if (string.IsNullOrWhiteSpace(value) || value.Length > maximum || value.Any(char.IsControl)) throw Error("OLAND_FORM_INVALID", name + " is required and must be within its supported length."); }
    private static OLandCommerceException Error(string code, string message) => new(code, message);
}
