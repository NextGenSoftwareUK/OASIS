using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    public sealed class OwnersRequest { public List<string> OwnerAvatarIds { get; set; } public string Reason { get; set; } }
    public sealed class ThresholdsRequest
    {
        public List<int> Thresholds { get; set; }
        /// <summary>Email me these warnings (default true). Only sent when the node has email configured.</summary>
        public bool EmailRequested { get; set; } = true;
        /// <summary>Your own opt-in (true) or opt-out (false) of usage warnings; null follows the node default.</summary>
        public bool? WarningsEnabled { get; set; }
    }
    public sealed class AppSettingsRequest { public bool? OwnerViewEnabled { get; set; } public bool? WarningsEnabled { get; set; } public bool? AccessLogEnabled { get; set; } }
    public sealed class AppPaygRequest { public bool Enabled { get; set; } public decimal MonthlyCapUsd { get; set; } public string PricingVersion { get; set; } public bool Consent { get; set; } }

    /// <summary>
    /// Owners and warnings. Owners see ONLY their own apps (never another app, never request content). People see only their own
    /// alerts. The Wizard administrator sets owners. The node default is OASIS_DNA.json (OASIS.UsageVisibility); each person and each
    /// app account can override it for themselves (their opt-in or opt-out wins over the node default).
    /// </summary>
    [ApiController]
    [Route("api/subscription")]
    public sealed class SubscriptionOwnerController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ISubscriptionService _subscriptions;
        private readonly IUsageSeriesSource _series;
        private readonly MongoSubscriptionUsageRepository _repository;

        public SubscriptionOwnerController(IConfiguration configuration, ISubscriptionService subscriptions, IUsageSeriesSource series, MongoSubscriptionUsageRepository repository)
        { _configuration = configuration; _subscriptions = subscriptions; _series = series; _repository = repository; }

        /// <summary>A person's or app's own choice wins over the node default.</summary>
        public static bool Effective(bool? own, bool nodeDefault) => own ?? nodeDefault;

        private static UsageVisibilitySettings Settings => OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.UsageVisibility ?? new UsageVisibilitySettings();

        /// <summary>Who may open an owner view: an owner of that app, or a Wizard administrator. Everyone else is refused.</summary>
        public static bool CanViewApp(bool isOwner, bool isAdministrator) => isOwner || isAdministrator;

        /// <summary>Administrator: set the owner avatars of an app account (audited).</summary>
        [HttpPost("admin/app-accounts/{appId}/owners")]
        public async Task<IActionResult> SetOwners(string appId, [FromBody] OwnersRequest request)
        {
            string actor;
            try { actor = SubscriptionServiceIdentity.RequireAdministrator(HttpContext, _configuration); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new OASISResult<bool> { IsError = true, Message = ex.Message }); }
            return await Run(async () => await _repository.SetAppOwnersAsync(appId, request?.OwnerAvatarIds, actor, request?.Reason, HttpContext.RequestAborted));
        }

        /// <summary>The apps the signed-in avatar owns.</summary>
        [Authorize]
        [HttpGet("app-owner/apps")]
        public Task<IActionResult> MyApps() => Gate(true, "", async me => (await _repository.GetOwnedAppsAsync(me, HttpContext.RequestAborted)).Select(a => new { a.AppId, a.Name, a.PlanId, a.Status, ownerViewEnabled = Effective(a.Overrides?.OwnerViewEnabled, Settings.OwnerViewEnabled), overrides = a.Overrides }).ToList());

        /// <summary>An owned app's pool: allocation, used this month and today, daily chart, which users used what (names resolved, no content) and its alerts.</summary>
        [Authorize]
        [HttpGet("app-owner/{appId}/usage")]
        public Task<IActionResult> AppUsage(string appId, [FromQuery] string from = null, [FromQuery] string to = null) =>
            Gate(true, "", async me =>
            {
                var owned = await RequireOwnerAsync(appId, me);
                if (!Effective(owned.Overrides?.OwnerViewEnabled, Settings.OwnerViewEnabled)) throw new InvalidOperationException("The owner usage view is turned off for this app.");
                var now = DateTime.UtcNow;
                var (start, end) = UsageSeries.NormaliseRange(from, to, now);
                var pool = await _repository.GetAppPoolUsageAsync(appId, HttpContext.RequestAborted);
                var subjects = Enumerable.Range(0, MongoSubscriptionUsageRepository.MaxPoolShards).Select(s => MongoSubscriptionUsageRepository.PoolShardSubject(appId, s)).ToList();
                var buckets = await _series.GetDailySeriesAsync(subjects, start, end, HttpContext.RequestAborted);
                var series = UsageSeries.Build("app:" + appId, buckets, start, end, pool.Limits?.DailyCallLimit ?? 0, pool.Limits?.MonthlyRequestLimit ?? 0, pool.Usage?.MonthlyRequests ?? 0, now);
                var top = await _repository.GetAppTopUsersAsync(appId, 25, HttpContext.RequestAborted);
                var users = new List<object>();
                foreach (var u in top)
                {
                    string name = "";
                    try { if (Guid.TryParse(u.UserId, out var id)) name = (await AvatarManager.Instance.LoadAvatarAsync(id, false, true))?.Result?.Username ?? ""; } catch (Exception) { }
                    users.Add(new { avatarId = u.UserId, avatarName = name, requests = u.Requests, units = u.Units, costUsd = u.SettledCostUsd });
                }
                var alerts = new List<UsageAlert>();
                var ownerNotify = await _repository.GetNotificationSettingsAsync(NotifySubject(appId, me), HttpContext.RequestAborted);
                if (Effective(ownerNotify?.WarningsOverride ?? owned.Overrides?.WarningsEnabled, Settings.WarningsEnabled))
                {
                    var s = ownerNotify;
                    var a = UsageAlerts.Evaluate("app", appId, pool.Usage?.MonthlyRequests ?? 0, pool.Limits?.MonthlyRequestLimit ?? 0, s?.Thresholds ?? Settings.DefaultWarningPercents);
                    if (a != null) alerts.Add(a);
                }
                return new { app = new { pool.Account.AppId, pool.Account.Name, pool.Account.PlanId }, pool, series, users, alerts };
            });

        /// <summary>Warning thresholds for the signed-in person (default 80% and 100%).</summary>
        [Authorize]
        [HttpGet("usage/notifications")]
        public Task<IActionResult> GetMyNotifications() => Gate(true, "", async me => await ThresholdsFor(me));

        [Authorize]
        [HttpPost("usage/notifications")]
        public Task<IActionResult> SetMyNotifications([FromBody] ThresholdsRequest request) =>
            Gate(true, "", async me => await _repository.SaveNotificationSettingsAsync("user:" + me, request?.Thresholds, request?.EmailRequested ?? true, me, request?.WarningsEnabled, HttpContext.RequestAborted));

        [Authorize]
        [HttpGet("app-owner/{appId}/notifications")]
        public Task<IActionResult> GetOwnerNotifications(string appId) => Gate(true, "", async me => { await RequireOwnerAsync(appId, me); return await ThresholdsFor(NotifySubject(appId, me)); });

        [Authorize]
        [HttpPost("app-owner/{appId}/notifications")]
        public Task<IActionResult> SetOwnerNotifications(string appId, [FromBody] ThresholdsRequest request) =>
            Gate(true, "", async me => { await RequireOwnerAsync(appId, me); return await _repository.SaveNotificationSettingsAsync(NotifySubject(appId, me), request?.Thresholds, request?.EmailRequested ?? true, me, request?.WarningsEnabled, HttpContext.RequestAborted); });

        /// <summary>The in-portal banner source: the signed-in person's own alert and one alert per app they own. Also sends the once-a-month warning email when email is on and configured.</summary>
        [Authorize]
        [HttpGet("usage/alerts")]
        public Task<IActionResult> Alerts() => Gate(true, "", async me =>
        {
            var alerts = new List<UsageAlert>();
            var karma = await SubscriptionController.GetCurrentKarmaAsync(me, HttpContext.RequestAborted);
            var summary = await _subscriptions.GetUsageSummaryAsync(me, karma, HttpContext.RequestAborted);
            var policy = SubscriptionService.GetUsagePolicy(summary.PlanId ?? "free", karma);
            var mine = await _repository.GetNotificationSettingsAsync("user:" + me, HttpContext.RequestAborted);
            if (Effective(mine?.WarningsOverride, Settings.WarningsEnabled))
            {
                var own = UsageAlerts.Evaluate("user", null, summary.MonthlyRequests, policy.MonthlyRequestLimit, mine?.Thresholds is { Count: > 0 } ? mine.Thresholds : Settings.DefaultWarningPercents);
                if (own != null) { alerts.Add(own); await MaybeEmailAsync("user:" + me, me, own, mine?.EmailRequested ?? true); }
            }
            foreach (var app in await _repository.GetOwnedAppsAsync(me, HttpContext.RequestAborted))
            {
                var s = await _repository.GetNotificationSettingsAsync(NotifySubject(app.AppId, me), HttpContext.RequestAborted);
                if (!Effective(app.Overrides?.OwnerViewEnabled, Settings.OwnerViewEnabled) || !Effective(s?.WarningsOverride ?? app.Overrides?.WarningsEnabled, Settings.WarningsEnabled)) continue;
                var pool = await _repository.GetAppPoolUsageAsync(app.AppId, HttpContext.RequestAborted);
                var a = UsageAlerts.Evaluate("app", app.AppId, pool.Usage?.MonthlyRequests ?? 0, pool.Limits?.MonthlyRequestLimit ?? 0, s?.Thresholds is { Count: > 0 } ? s.Thresholds : Settings.DefaultWarningPercents);
                if (a != null) { alerts.Add(a); await MaybeEmailAsync(NotifySubject(app.AppId, me), me, a, s?.EmailRequested ?? true); }
            }
            return new { emailEnabled = Settings.EmailEnabled, alerts };
        });

        /// <summary>App owner (or administrator): see and change this app's own on/off choices, which override the node default. Audited.</summary>
        [Authorize]
        [HttpGet("app-owner/{appId}/settings")]
        public Task<IActionResult> GetAppSettings(string appId) => Gate(true, "", async me =>
        {
            var app = await RequireOwnerAsync(appId, me);
            return new
            {
                overrides = app.Overrides ?? new AppUsageOverrides(),
                effective = new { ownerViewEnabled = Effective(app.Overrides?.OwnerViewEnabled, Settings.OwnerViewEnabled), warningsEnabled = Effective(app.Overrides?.WarningsEnabled, Settings.WarningsEnabled) },
                nodeDefaults = new { ownerViewEnabled = Settings.OwnerViewEnabled, warningsEnabled = Settings.WarningsEnabled, accessLogEnabled = OASISBootLoader.OASISBootLoader.OASISDNA?.OASIS?.AccessLog?.Enabled ?? false }
            };
        });

        [Authorize]
        [HttpPost("app-owner/{appId}/settings")]
        public Task<IActionResult> SetAppSettings(string appId, [FromBody] AppSettingsRequest request) => Gate(true, "", async me =>
        {
            await RequireOwnerAsync(appId, me);
            return await _repository.SetAppOverridesAsync(appId, request?.OwnerViewEnabled, request?.WarningsEnabled, request?.AccessLogEnabled, me, HttpContext.RequestAborted);
        });

        // One warning email per subject, threshold and UTC month, only when email is configured and the person has not opted out.
        private async Task MaybeEmailAsync(string subject, string me, UsageAlert alert, bool emailRequested)
        {
            try
            {
                var dna = OASISBootLoader.OASISBootLoader.OASISDNA;
                if (!Settings.EmailEnabled || !emailRequested || dna == null || !EmailManager.IsEmailConfigured(dna)) return;
                var key = DateTime.UtcNow.ToString("yyyy-MM") + ":" + alert.ThresholdPercent;
                if (!await _repository.TryMarkNotifiedAsync(subject, key, HttpContext.RequestAborted)) return;
                var avatar = (await AvatarManager.Instance.LoadAvatarAsync(Guid.Parse(me), false, true))?.Result;
                if (string.IsNullOrWhiteSpace(avatar?.Email)) return;
                if (!EmailManager.IsInitialized) EmailManager.Initialize(dna);
                EmailManager.Send(avatar.Email, "OASIS - usage warning", "<h4>Usage warning</h4><p>" + System.Net.WebUtility.HtmlEncode(alert.Message) + "</p><p>You can change or turn off these warnings in the OASIS portal under Usage and billing.</p>");
            }
            catch (Exception) { /* a failed email never affects the page */ }
        }

        /// <summary>App pool pay-as-you-go: shows the settings and whether prices exist. SETTINGS AND DISPLAY ONLY; nothing is charged or invoiced.</summary>
        [Authorize]
        [HttpGet("app-owner/{appId}/payg")]
        public Task<IActionResult> GetPayg(string appId) => Gate(true, "", async me =>
        {
            await RequireOwnerAsync(appId, me);
            var account = await RequireOwnerAsync(appId, me);
            var prices = AppOveragePrices.Load();
            var pool = await _repository.GetAppPoolUsageAsync(appId, HttpContext.RequestAborted);
            var payg = account.Payg ?? new AppPaygSettings();
            var estimate = AppOverageEstimate.Compute(pool.Usage?.MonthlyRequests ?? 0, pool.Limits?.MonthlyRequestLimit ?? 0, prices.Rate(account.PlanId), payg.MonthlyCapUsd);
            return new { featureEnabled = Settings.AppPayAsYouGoEnabled, pricesConfigured = prices.HasPrice(account.PlanId), pricingVersion = prices.Version, ratePerRequestUsd = prices.Rate(account.PlanId), settings = payg, estimate, chargingImplemented = false };
        });

        [Authorize]
        [HttpPost("app-owner/{appId}/payg")]
        public Task<IActionResult> SetPayg(string appId, [FromBody] AppPaygRequest request) => Gate(true, "", async me =>
        {
            await RequireOwnerAsync(appId, me);
            if (!Settings.AppPayAsYouGoEnabled) throw new InvalidOperationException("App pay-as-you-go is not enabled on this node (OASIS.UsageVisibility.AppPayAsYouGoEnabled).");
            var account = (await _repository.ListAppAccountsAsync(HttpContext.RequestAborted)).FirstOrDefault(a => a.AppId == appId) ?? throw new KeyNotFoundException("The app account was not found.");
            var prices = AppOveragePrices.Load();
            if (request != null && request.Enabled)
            {
                if (!prices.HasPrice(account.PlanId)) throw new InvalidOperationException("No app overage price is configured for this plan, so pay-as-you-go cannot be enabled.");
                if (!request.Consent) throw new ArgumentException("Consent to the prices and the monthly cap is required.");
            }
            return await _repository.SetAppPaygAsync(appId, request?.Enabled ?? false, request?.MonthlyCapUsd ?? 0, prices.Version, me, HttpContext.RequestAborted);
        });

        // ── helpers ──
        private static string NotifySubject(string appId, string me) => "app:" + appId + ":owner:" + me;

        private async Task<object> ThresholdsFor(string subjectOrMe)
        {
            var subject = subjectOrMe.StartsWith("app:", StringComparison.Ordinal) ? subjectOrMe : "user:" + subjectOrMe;
            var s = await _repository.GetNotificationSettingsAsync(subject, HttpContext.RequestAborted);
            return new { thresholds = s?.Thresholds ?? Settings.DefaultWarningPercents, isDefault = s?.Thresholds == null || s.Thresholds.Count == 0, warningsEnabled = Effective(s?.WarningsOverride, Settings.WarningsEnabled), warningsOverride = s?.WarningsOverride, nodeDefault = Settings.WarningsEnabled, emailEnabled = Settings.EmailEnabled, emailRequested = s?.EmailRequested ?? true };
        }

        private bool IsAdministrator()
        {
            try { SubscriptionServiceIdentity.RequireAdministrator(HttpContext, _configuration); return true; } catch (UnauthorizedAccessException) { return false; }
        }

        private async Task<AppAccountRecord> RequireOwnerAsync(string appId, string me)
        {
            var isOwner = await _repository.IsAppOwnerAsync(appId, me, HttpContext.RequestAborted);
            if (!CanViewApp(isOwner, IsAdministrator())) throw new UnauthorizedAccessException("You are not an owner of this app.");
            return (await _repository.ListAppAccountsAsync(HttpContext.RequestAborted)).FirstOrDefault(a => a.AppId == appId) ?? throw new KeyNotFoundException("The app account was not found.");
        }

        private string Me()
        {
            if (User?.Identity?.IsAuthenticated != true) return null;
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
            if (!Guid.TryParseExact(claim, "D", out var id)) return null;
            if (HttpContext.Items.TryGetValue("Avatar", out var value) && value is IAvatar avatar && avatar.Id != id) return null;
            return id.ToString("D");
        }

        private Task<IActionResult> OwnerGate<T>(Func<string, Task<T>> call) => Gate(Settings.OwnerViewEnabled, "App owner views are not enabled on this node (OASIS.UsageVisibility.OwnerViewEnabled).", call);
        private Task<IActionResult> WarningsGate<T>(Func<string, Task<T>> call) => Gate(Settings.WarningsEnabled, "Usage warnings are not enabled on this node (OASIS.UsageVisibility.WarningsEnabled).", call);

        private async Task<IActionResult> Gate<T>(bool enabled, string disabledMessage, Func<string, Task<T>> call)
        {
            if (!enabled) return NotFound(new OASISResult<bool> { IsError = true, Message = disabledMessage });
            var me = Me();
            if (me == null) return Unauthorized(new OASISResult<bool> { IsError = true, Message = "User not authenticated." });
            return await Run(() => call(me));
        }

        private async Task<IActionResult> Run<T>(Func<Task<T>> call)
        {
            try { return Ok(new OASISResult<T> { Result = await call(), IsLoaded = true }); }
            catch (UnauthorizedAccessException ex) { return StatusCode(403, new OASISResult<bool> { IsError = true, Message = ex.Message }); }
            catch (ArgumentException ex) { return BadRequest(new OASISResult<bool> { IsError = true, Message = ex.Message }); }
            catch (KeyNotFoundException ex) { return NotFound(new OASISResult<bool> { IsError = true, Message = ex.Message }); }
            catch (InvalidOperationException ex) { return Conflict(new OASISResult<bool> { IsError = true, Message = ex.Message }); }
        }
    }

    /// <summary>Reads Configuration/app-overage-prices.json. It ships with NO prices, so pay-as-you-go for app pools cannot be enabled until David fills them in.</summary>
    public sealed class AppOveragePrices
    {
        public string Version { get; private set; } = "app-overages-unset";
        private readonly Dictionary<string, decimal> _rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        public decimal Rate(string planId) => planId != null && _rates.TryGetValue(planId, out var r) ? r : 0m;
        public bool HasPrice(string planId) => planId != null && _rates.TryGetValue(planId, out var r) && r > 0;

        public static AppOveragePrices Load()
        {
            var prices = new AppOveragePrices();
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Configuration", "app-overage-prices.json");
                if (!System.IO.File.Exists(path)) return prices;
                using var doc = JsonDocument.Parse(System.IO.File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("version", out var v)) prices.Version = v.GetString() ?? prices.Version;
                if (doc.RootElement.TryGetProperty("requestRatesUsd", out var rates) && rates.ValueKind == JsonValueKind.Object)
                    foreach (var p in rates.EnumerateObject()) if (p.Value.TryGetDecimal(out var d)) prices._rates[p.Name] = d;
            }
            catch (Exception) { /* unreadable catalogue = no prices = disabled */ }
            return prices;
        }
    }
}
