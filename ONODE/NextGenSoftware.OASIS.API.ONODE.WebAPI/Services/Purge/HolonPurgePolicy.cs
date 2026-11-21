using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Purge
{
    public enum HolonPurgeDecision
    {
        /// <summary>The master switch is off: nothing may be purged.</summary>
        MasterOff,
        /// <summary>Allowed by the master but this holon's owner/app/default says no.</summary>
        Off,
        On
    }

    /// <summary>
    /// How the switches combine, most specific first (a legal hold is checked separately and always wins):
    /// master Enabled (false forbids everything) -> the holon owner's choice -> the calling app's choice -> ApplyToAll.
    /// Answers from memory only.
    /// </summary>
    public sealed class HolonPurgePolicy
    {
        private readonly Func<VersionPurgeSettings> _settings;
        private readonly ConcurrentDictionary<Guid, PurgeChoice> _owners = new ConcurrentDictionary<Guid, PurgeChoice>();
        private volatile Dictionary<string, PurgeChoice> _apps = new Dictionary<string, PurgeChoice>(StringComparer.OrdinalIgnoreCase);

        public HolonPurgePolicy(Func<VersionPurgeSettings> settings) { _settings = settings ?? throw new ArgumentNullException(nameof(settings)); }

        public VersionPurgeSettings Settings => _settings() ?? new VersionPurgeSettings();
        public bool MasterEnabled => Settings.Enabled;

        public HolonPurgeDecision Decide(Guid ownerAvatarId, string appId)
        {
            var s = Settings;
            if (!s.Enabled) return HolonPurgeDecision.MasterOff;
            if (ownerAvatarId != Guid.Empty && _owners.TryGetValue(ownerAvatarId, out var owner) && owner.Enabled.HasValue)
                return owner.Enabled.Value ? HolonPurgeDecision.On : HolonPurgeDecision.Off;
            if (!string.IsNullOrEmpty(appId) && _apps.TryGetValue(appId, out var app) && app.Enabled.HasValue)
                return app.Enabled.Value ? HolonPurgeDecision.On : HolonPurgeDecision.Off;
            return s.ApplyToAll ? HolonPurgeDecision.On : HolonPurgeDecision.Off;
        }

        /// <summary>
        /// THE one place the number of days is worked out (used when a deletion is scheduled): the owner's explicit days, else the app's,
        /// else OASIS.VersionPurge.PurgeAfterDays (typed default 30). Independent of the on/off switch. Source is "avatar", "app" or "global".
        /// </summary>
        public (int Days, string Source) EffectiveDays(Guid ownerAvatarId, string appId)
        {
            if (ownerAvatarId != Guid.Empty && _owners.TryGetValue(ownerAvatarId, out var owner) && owner.Days.HasValue) return (owner.Days.Value, "avatar");
            if (!string.IsNullOrEmpty(appId) && _apps.TryGetValue(appId, out var app) && app.Days.HasValue) return (app.Days.Value, "app");
            return (Math.Max(0, Settings.PurgeAfterDays), "global");
        }

        public bool? OwnerChoice(Guid avatarId) => _owners.TryGetValue(avatarId, out var c) ? c.Enabled : null;
        public bool? AppChoice(string appId) => !string.IsNullOrEmpty(appId) && _apps.TryGetValue(appId, out var c) ? c.Enabled : null;
        public void SetOwner(Guid avatarId, bool? enabled, int? days = null) => SetOwnerChoice(avatarId, new PurgeChoice { Enabled = enabled, Days = days });
        public void SetOwnerChoice(Guid avatarId, PurgeChoice choice) { if (choice != null && !choice.IsEmpty) _owners[avatarId] = choice; else _owners.TryRemove(avatarId, out _); }
        public void ReplaceOwners(IReadOnlyDictionary<Guid, PurgeChoice> choices)
        {
            foreach (var k in _owners.Keys.ToList()) if (!choices.ContainsKey(k)) _owners.TryRemove(k, out _);
            foreach (var c in choices) _owners[c.Key] = c.Value;
        }
        public void ReplaceApps(IEnumerable<KeyValuePair<string, PurgeChoice>> choices)
            => _apps = choices.Where(c => !string.IsNullOrEmpty(c.Key) && c.Value != null && !c.Value.IsEmpty).GroupBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Last().Value, StringComparer.OrdinalIgnoreCase);
        public void ReplaceApps(IEnumerable<KeyValuePair<string, bool>> choices)
            => ReplaceApps(choices.Select(c => new KeyValuePair<string, PurgeChoice>(c.Key, new PurgeChoice { Enabled = c.Value })));
    }
}
