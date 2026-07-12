using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Services.Subscription
{
    public class SubscriptionService : ISubscriptionService
    {
        private readonly string _dataDir;
        private readonly ILogger<SubscriptionService> _logger;

        private readonly SemaphoreSlim _subLock = new(1, 1);
        private readonly SemaphoreSlim _usageLock = new(1, 1);

        private string SubsFile => Path.Combine(_dataDir, "subscriptions.json");
        private string UsageFile => Path.Combine(_dataDir, "usage.json");

        private static readonly JsonSerializerOptions _json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

        public SubscriptionService(IConfiguration configuration, ILogger<SubscriptionService> logger)
        {
            _logger = logger;
            var configured = configuration["Subscription:DataDirectory"];
            _dataDir = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(AppContext.BaseDirectory, "App_Data", "subscriptions")
                : configured;
            Directory.CreateDirectory(_dataDir);
        }

        public async Task<SubscriptionRecord> GetSubscriptionAsync(string userId)
        {
            await _subLock.WaitAsync();
            try
            {
                var all = await ReadAsync<List<SubscriptionRecord>>(SubsFile) ?? new();
                return all.FirstOrDefault(s => s.UserId == userId);
            }
            finally { _subLock.Release(); }
        }

        public async Task<UsageRecord> GetUsageAsync(string userId, int year, int month)
        {
            await _usageLock.WaitAsync();
            try
            {
                var all = await ReadAsync<List<UsageRecord>>(UsageFile) ?? new();
                return all.FirstOrDefault(u => u.UserId == userId && u.Year == year && u.Month == month)
                       ?? new UsageRecord { UserId = userId, Year = year, Month = month };
            }
            finally { _usageLock.Release(); }
        }

        public async Task IncrementUsageAsync(string userId) => await BumpCounter(userId, overage: false);
        public async Task IncrementOverageAsync(string userId) => await BumpCounter(userId, overage: true);

        private async Task BumpCounter(string userId, bool overage)
        {
            var now = DateTime.UtcNow;
            await _usageLock.WaitAsync();
            try
            {
                var all = await ReadAsync<List<UsageRecord>>(UsageFile) ?? new();
                var record = all.FirstOrDefault(u => u.UserId == userId && u.Year == now.Year && u.Month == now.Month);
                if (record == null) { record = new() { UserId = userId, Year = now.Year, Month = now.Month }; all.Add(record); }
                record.RequestCount++;
                if (overage) record.OverageCount++;
                record.LastUpdated = now;
                await WriteAsync(UsageFile, all);
            }
            finally { _usageLock.Release(); }
        }

        private async Task<T> ReadAsync<T>(string path) where T : class
        {
            if (!File.Exists(path)) return null;
            try { return JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(path), _json); }
            catch (Exception ex) { _logger.LogError(ex, "Failed to read {Path}", path); return null; }
        }

        private async Task WriteAsync<T>(string path, T data) =>
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data, _json));
    }
}
