using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription
{
    /// <summary>
    /// JSON-file-backed subscription service. Thread-safe via SemaphoreSlim.
    /// Data directory defaults to App_Data/subscriptions relative to content root.
    /// Replace with a database-backed implementation by swapping the ISubscriptionService registration.
    /// </summary>
    public class SubscriptionService : ISubscriptionService
    {
        private readonly string _dataDir;
        private readonly ILogger<SubscriptionService> _logger;

        private readonly SemaphoreSlim _subLock = new(1, 1);
        private readonly SemaphoreSlim _usageLock = new(1, 1);
        private readonly SemaphoreSlim _orderLock = new(1, 1);

        private string SubsFile => Path.Combine(_dataDir, "subscriptions.json");
        private string UsageFile => Path.Combine(_dataDir, "usage.json");
        private string OrdersFile => Path.Combine(_dataDir, "orders.json");

        private static readonly JsonSerializerOptions _json = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public SubscriptionService(IConfiguration configuration, ILogger<SubscriptionService> logger)
        {
            _logger = logger;
            var configured = configuration["Subscription:DataDirectory"];
            _dataDir = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(AppContext.BaseDirectory, "App_Data", "subscriptions")
                : configured;

            Directory.CreateDirectory(_dataDir);
        }

        // ── Subscription ────────────────────────────────────────────────────

        public async Task<SubscriptionRecord> GetSubscriptionAsync(string userId)
        {
            var all = await ReadAsync<List<SubscriptionRecord>>(SubsFile, _subLock) ?? new();
            return all.FirstOrDefault(s => s.UserId == userId);
        }

        public async Task<SubscriptionRecord> GetSubscriptionByStripeCustomerIdAsync(string stripeCustomerId)
        {
            var all = await ReadAsync<List<SubscriptionRecord>>(SubsFile, _subLock) ?? new();
            return all.FirstOrDefault(s => s.StripeCustomerId == stripeCustomerId);
        }

        public async Task<SubscriptionRecord> GetSubscriptionByStripeSubscriptionIdAsync(string stripeSubscriptionId)
        {
            var all = await ReadAsync<List<SubscriptionRecord>>(SubsFile, _subLock) ?? new();
            return all.FirstOrDefault(s => s.StripeSubscriptionId == stripeSubscriptionId);
        }

        public async Task UpsertSubscriptionAsync(SubscriptionRecord record)
        {
            await _subLock.WaitAsync();
            try
            {
                var all = await ReadFileAsync<List<SubscriptionRecord>>(SubsFile) ?? new();
                var existing = all.FindIndex(s => s.UserId == record.UserId);
                record.UpdatedAt = DateTime.UtcNow;
                if (existing >= 0)
                    all[existing] = record;
                else
                    all.Add(record);
                await WriteFileAsync(SubsFile, all);
            }
            finally { _subLock.Release(); }
        }

        public async Task SetPayAsYouGoAsync(string userId, bool enabled)
        {
            await _subLock.WaitAsync();
            try
            {
                var all = await ReadFileAsync<List<SubscriptionRecord>>(SubsFile) ?? new();
                var record = all.FirstOrDefault(s => s.UserId == userId);
                if (record == null)
                {
                    record = new SubscriptionRecord { UserId = userId, PlanId = "free", Status = "active" };
                    all.Add(record);
                }
                record.PayAsYouGoEnabled = enabled;
                record.UpdatedAt = DateTime.UtcNow;
                await WriteFileAsync(SubsFile, all);
            }
            finally { _subLock.Release(); }
        }

        // ── Usage ───────────────────────────────────────────────────────────

        public async Task<UsageRecord> GetUsageAsync(string userId, int year, int month)
        {
            var all = await ReadAsync<List<UsageRecord>>(UsageFile, _usageLock) ?? new();
            return all.FirstOrDefault(u => u.UserId == userId && u.Year == year && u.Month == month)
                   ?? new UsageRecord { UserId = userId, Year = year, Month = month };
        }

        public async Task IncrementUsageAsync(string userId)
        {
            var now = DateTime.UtcNow;
            await _usageLock.WaitAsync();
            try
            {
                var all = await ReadFileAsync<List<UsageRecord>>(UsageFile) ?? new();
                var record = all.FirstOrDefault(u => u.UserId == userId && u.Year == now.Year && u.Month == now.Month);
                if (record == null)
                {
                    record = new UsageRecord { UserId = userId, Year = now.Year, Month = now.Month };
                    all.Add(record);
                }
                record.RequestCount++;
                record.LastUpdated = now;
                await WriteFileAsync(UsageFile, all);
            }
            finally { _usageLock.Release(); }
        }

        public async Task IncrementOverageAsync(string userId)
        {
            var now = DateTime.UtcNow;
            await _usageLock.WaitAsync();
            try
            {
                var all = await ReadFileAsync<List<UsageRecord>>(UsageFile) ?? new();
                var record = all.FirstOrDefault(u => u.UserId == userId && u.Year == now.Year && u.Month == now.Month);
                if (record == null)
                {
                    record = new UsageRecord { UserId = userId, Year = now.Year, Month = now.Month };
                    all.Add(record);
                }
                record.RequestCount++;
                record.OverageCount++;
                record.LastUpdated = now;
                await WriteFileAsync(UsageFile, all);
            }
            finally { _usageLock.Release(); }
        }

        // ── Orders ──────────────────────────────────────────────────────────

        public async Task<List<OrderRecord>> GetOrdersAsync(string userId)
        {
            var all = await ReadAsync<List<OrderRecord>>(OrdersFile, _orderLock) ?? new();
            return all.Where(o => o.UserId == userId).OrderByDescending(o => o.CreatedAt).ToList();
        }

        public async Task AddOrderAsync(OrderRecord order)
        {
            await _orderLock.WaitAsync();
            try
            {
                var all = await ReadFileAsync<List<OrderRecord>>(OrdersFile) ?? new();
                // Deduplicate by Stripe invoice ID
                if (!string.IsNullOrEmpty(order.StripeInvoiceId) && all.Any(o => o.StripeInvoiceId == order.StripeInvoiceId))
                    return;
                all.Add(order);
                await WriteFileAsync(OrdersFile, all);
            }
            finally { _orderLock.Release(); }
        }

        // ── Internal helpers ─────────────────────────────────────────────────

        private async Task<T> ReadAsync<T>(string path, SemaphoreSlim sem) where T : class
        {
            await sem.WaitAsync();
            try { return await ReadFileAsync<T>(path); }
            finally { sem.Release(); }
        }

        private async Task<T> ReadFileAsync<T>(string path) where T : class
        {
            if (!System.IO.File.Exists(path)) return null;
            try
            {
                var json = await System.IO.File.ReadAllTextAsync(path);
                return JsonSerializer.Deserialize<T>(json, _json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read subscription data from {Path}", path);
                return null;
            }
        }

        private async Task WriteFileAsync<T>(string path, T data)
        {
            var json = JsonSerializer.Serialize(data, _json);
            await System.IO.File.WriteAllTextAsync(path, json);
        }
    }
}
