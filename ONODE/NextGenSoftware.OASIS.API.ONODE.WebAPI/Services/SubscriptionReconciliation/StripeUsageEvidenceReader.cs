using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.SubscriptionReconciliation;

/// <summary>Reads finalized usage-only Stripe invoices. Does not create invoices, charges or meter events.</summary>
public sealed class StripeUsageEvidenceReader
{
    public const string CostBasis = "ledger-cost-usd-v1";
    private readonly HttpClient _http;
    private readonly IConfiguration _configuration;
    private readonly MongoUsageReconciler _reconciler;
    public StripeUsageEvidenceReader(HttpClient http, IConfiguration configuration, MongoUsageReconciler reconciler)
    { _http = http; _configuration = configuration; _reconciler = reconciler; }

    public async Task CollectAsync(string customerId, string userId, string month, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(customerId)) return; // No Stripe account exists for this subscriber.
        string cursor = null;
        do
        {
            var path = "invoices?limit=100&customer=" + Uri.EscapeDataString(customerId) +
                (cursor == null ? "" : "&starting_after=" + Uri.EscapeDataString(cursor));
            using var response = await GetAsync(path, ct);
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            var root = document.RootElement;
            var data = root.GetProperty("data");
            foreach (var invoice in data.EnumerateArray())
            {
                cursor = invoice.GetProperty("id").GetString();
                var metadata = invoice.GetProperty("metadata");
                if (!metadata.TryGetProperty("oasis_usage_month", out var period) || period.GetString() != month) continue;
                if (invoice.GetProperty("status").GetString() == "draft") continue; // Not final evidence yet; report stays incomplete.
                var receipt = ParseFinalizedInvoice(invoice, customerId, userId, month, "stripe-reconciliation-worker");
                await _reconciler.RecordReceiptAsync(receipt, ct);
            }
            if (!root.GetProperty("has_more").GetBoolean()) break;
            if (data.GetArrayLength() == 0) throw new InvalidOperationException("Stripe pagination returned has_more without a cursor.");
        } while (true);
    }

    public async Task<UsageExternalReceipt> ImportAsync(string invoiceId, string customerId, string userId, string month, string actor, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(invoiceId) || !invoiceId.StartsWith("in_", StringComparison.Ordinal) || invoiceId.Length > 100)
            throw new ArgumentException("A Stripe invoice identifier is required.");
        using var response = await GetAsync("invoices/" + Uri.EscapeDataString(invoiceId), ct);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        return await _reconciler.RecordReceiptAsync(ParseFinalizedInvoice(json.RootElement, customerId, userId, month, actor), ct);
    }

    private async Task<HttpResponseMessage> GetAsync(string path, CancellationToken ct)
    {
        string key = SubscriptionStripeConfiguration.SecretKey(_configuration);
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Stripe SecretKey is required in OASIS DNA or STRIPE_SECRET_KEY for reconciliation.");
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.stripe.com/v1/" + path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Headers.Add("Stripe-Version", "2025-02-24.acacia");
        var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var status = response.StatusCode;
            response.Dispose();
            throw new HttpRequestException("Stripe evidence retrieval failed with HTTP " + (int)status, null, status);
        }
        return response;
    }

    public static UsageExternalReceipt ParseFinalizedInvoice(JsonElement invoice, string customerId, string userId, string month, string actor)
    {
        var metadata = invoice.GetProperty("metadata");
        string status = invoice.GetProperty("status").GetString();
        if (status is not ("open" or "paid" or "uncollectible") || invoice.GetProperty("currency").GetString() != "usd" ||
            invoice.GetProperty("customer").GetString() != customerId || string.IsNullOrWhiteSpace(customerId) ||
            !metadata.TryGetProperty("oasis_avatar_id", out var avatar) || avatar.GetString() != userId ||
            !metadata.TryGetProperty("oasis_usage_month", out var period) || period.GetString() != month ||
            !metadata.TryGetProperty("oasis_usage_cost_basis", out var basis) || basis.GetString() != CostBasis)
            throw new InvalidOperationException("Stripe invoice is not finalized USD usage-only evidence bound to this avatar, customer, month and ledger cost basis.");
        // Fingerprint immutable accounting fields, excluding payment status which can change after finalization.
        string externalId = invoice.GetProperty("id").GetString();
        long subtotal = invoice.GetProperty("subtotal").GetInt64();
        string canonical = JsonSerializer.Serialize(new { externalId, customerId, userId, month, subtotal, basis = CostBasis });
        var receipt = new UsageExternalReceipt
        {
            Source = "stripe", ExternalId = externalId, UserId = userId, Month = month, AmountUsd = subtotal / 100m,
            EvidenceSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant(), Actor = actor
        };
        receipt.Validate();
        return receipt;
    }
}
