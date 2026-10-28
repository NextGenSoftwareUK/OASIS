using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests
{
    /// <summary>The sandbox's throwaway avatars are removed with everything they own, and only they are.</summary>
    public class SandboxAvatarCleanupTests
    {
        private static readonly DateTime Now = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
        private static readonly ProviderType[] Providers = { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS };

        private static Avatar Demo(string suffix, TimeSpan age, AvatarType type = AvatarType.User) => new Avatar
        {
            Id = Guid.NewGuid(), Username = "sandbox-" + suffix, Email = $"sandbox-{suffix}@sandbox.oasisomniverse.one",
            AvatarType = new EnumValue<AvatarType>(type), CreatedDate = Now - age
        };

        private sealed class World
        {
            public readonly List<IAvatar> Avatars = new List<IAvatar>();
            public readonly List<Holon> Holons = new List<Holon>();
            public readonly List<string> Order = new List<string>();
            public readonly HashSet<Guid> HolonsThatFailToDelete = new HashSet<Guid>();

            public Holon Own(IAvatar avatar, ProviderType provider)
            {
                var holon = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = avatar.Id, ProviderUniqueStorageKey = new Dictionary<ProviderType, string> { [provider] = "x" } };
                Holons.Add(holon);
                _provider[holon.Id] = provider;
                return holon;
            }

            private readonly Dictionary<Guid, ProviderType> _provider = new Dictionary<Guid, ProviderType>();

            public SandboxAvatarCleanup Cleanup(TimeSpan? retention = null, int cap = 50) => new SandboxAvatarCleanup(
                () => Task.FromResult<IReadOnlyList<IAvatar>>(Avatars.ToList()),
                (avatarId, provider) => Task.FromResult<IReadOnlyList<IHolon>>(Holons.Where(h => h.CreatedByAvatarId == avatarId && _provider[h.Id] == provider).Cast<IHolon>().ToList()),
                (holonId, avatarId, provider) =>
                {
                    if (HolonsThatFailToDelete.Contains(holonId)) return Task.FromResult(false);
                    Order.Add("holon");
                    Holons.RemoveAll(h => h.Id == holonId);
                    return Task.FromResult(true);
                },
                (avatarId, provider) =>
                {
                    Order.Add("avatar:" + provider);
                    Avatars.RemoveAll(a => a.Id == avatarId);
                    return Task.FromResult(true);
                },
                Providers, retention ?? TimeSpan.FromHours(24), () => Now, cap);
        }

        [Fact]
        public void AnAvatarIsDemoOnlyWhenEveryMarkerMatches()
        {
            Assert.True(SandboxAvatarCleanup.IsDemoAvatar(Demo("a1", TimeSpan.FromHours(30))));

            var wrongPrefix = Demo("a2", TimeSpan.Zero); wrongPrefix.Username = "alice";
            var wrongDomain = Demo("a3", TimeSpan.Zero); wrongDomain.Email = "sandbox-a3@example.com";
            var lookalikeDomain = Demo("a4", TimeSpan.Zero); lookalikeDomain.Email = "x@sandbox.oasisomniverse.one.evil.com";
            var wizard = Demo("a5", TimeSpan.Zero, AvatarType.Wizard);
            var noId = Demo("a6", TimeSpan.Zero); noId.Id = Guid.Empty;
            foreach (var notDemo in new[] { wrongPrefix, wrongDomain, lookalikeDomain, wizard, noId })
                Assert.False(SandboxAvatarCleanup.IsDemoAvatar(notDemo), notDemo.Username + " " + notDemo.Email);
            Assert.False(SandboxAvatarCleanup.IsDemoAvatar(null));
        }

        [Fact]
        public async Task OnlyExpiredDemoAvatarsAreRemovedWithTheirHolonsOnEveryProviderAndRealUsersAreUntouched()
        {
            var world = new World();
            var old = Demo("old", TimeSpan.FromHours(30));
            var recent = Demo("new", TimeSpan.FromHours(2));
            var real = new Avatar { Id = Guid.NewGuid(), Username = "sandbox-looks-real", Email = "me@example.com", AvatarType = new EnumValue<AvatarType>(AvatarType.User), CreatedDate = Now - TimeSpan.FromDays(90) };
            world.Avatars.AddRange(new IAvatar[] { old, recent, real });
            world.Own(old, ProviderType.MongoDBOASIS); world.Own(old, ProviderType.SQLLiteDBOASIS); world.Own(old, ProviderType.MongoDBOASIS);
            var recentHolon = world.Own(recent, ProviderType.MongoDBOASIS);
            var realHolon = world.Own(real, ProviderType.MongoDBOASIS);

            var report = await world.Cleanup().SweepAsync();

            Assert.Equal(1, report.AvatarsRemoved);
            Assert.Equal(3, report.HolonsRemoved);
            Assert.Empty(report.Failures);
            Assert.DoesNotContain(world.Avatars, a => a.Id == old.Id);
            Assert.Contains(world.Avatars, a => a.Id == recent.Id);
            Assert.Contains(world.Avatars, a => a.Id == real.Id);
            Assert.Equal(new[] { recentHolon.Id, realHolon.Id }.OrderBy(i => i), world.Holons.Select(h => h.Id).OrderBy(i => i));
        }

        [Fact]
        public async Task HolonsGoBeforeTheAvatarAndTheAvatarIsDeletedFromEveryProvider()
        {
            var world = new World();
            var old = Demo("old", TimeSpan.FromHours(30));
            world.Avatars.Add(old);
            world.Own(old, ProviderType.MongoDBOASIS); world.Own(old, ProviderType.SQLLiteDBOASIS);

            await world.Cleanup().SweepAsync();

            Assert.Equal(new[] { "holon", "holon", "avatar:MongoDBOASIS", "avatar:SQLLiteDBOASIS" }, world.Order);
        }

        [Fact]
        public async Task AFailedHolonDeletionKeepsTheAvatarSoTheNextSweepRetriesIt()
        {
            var world = new World();
            var old = Demo("old", TimeSpan.FromHours(30));
            world.Avatars.Add(old);
            var stuck = world.Own(old, ProviderType.MongoDBOASIS);
            world.Own(old, ProviderType.SQLLiteDBOASIS);
            world.HolonsThatFailToDelete.Add(stuck.Id);

            var first = await world.Cleanup().SweepAsync();
            Assert.Equal(0, first.AvatarsRemoved);
            Assert.Equal(1, first.AvatarsKept);
            Assert.Single(first.Failures);
            Assert.Contains(world.Avatars, a => a.Id == old.Id);
            Assert.DoesNotContain("avatar:MongoDBOASIS", world.Order);

            world.HolonsThatFailToDelete.Clear();
            var second = await world.Cleanup().SweepAsync();
            Assert.Equal(1, second.AvatarsRemoved);
            Assert.Empty(world.Holons);
            Assert.Empty(world.Avatars);
        }

        [Fact]
        public async Task AThrowingStoreIsReportedAndDoesNotStopTheOtherAvatars()
        {
            var world = new World();
            var first = Demo("a", TimeSpan.FromHours(40));
            var second = Demo("b", TimeSpan.FromHours(30));
            world.Avatars.AddRange(new IAvatar[] { first, second });
            var cleanup = new SandboxAvatarCleanup(
                () => Task.FromResult<IReadOnlyList<IAvatar>>(world.Avatars.ToList()),
                (avatarId, provider) => avatarId == first.Id ? throw new InvalidOperationException("store down") : Task.FromResult<IReadOnlyList<IHolon>>(new List<IHolon>()),
                (h, a, p) => Task.FromResult(true),
                (avatarId, provider) => { world.Avatars.RemoveAll(x => x.Id == avatarId); return Task.FromResult(true); },
                Providers, TimeSpan.FromHours(24), () => Now);

            var report = await cleanup.SweepAsync();

            Assert.Equal(1, report.AvatarsRemoved);
            Assert.Equal(1, report.AvatarsKept);
            Assert.Contains("store down", report.Failures.Single());
            Assert.Equal(first.Id, world.Avatars.Single().Id);
        }

        [Fact]
        public async Task EachSweepIsCappedAndTakesTheOldestFirst()
        {
            var world = new World();
            var avatars = Enumerable.Range(1, 5).Select(i => Demo("n" + i, TimeSpan.FromHours(24 + i))).ToList();
            world.Avatars.AddRange(avatars);

            var report = await world.Cleanup(cap: 2).SweepAsync();

            Assert.Equal(2, report.AvatarsRemoved);
            Assert.Equal(3, world.Avatars.Count);
            Assert.DoesNotContain(world.Avatars, a => a.Username == "sandbox-n5" || a.Username == "sandbox-n4");
        }

        [Fact]
        public void RetentionAndProvidersAreValidated()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new World().Cleanup(TimeSpan.FromSeconds(5)));
            Assert.Throws<ArgumentException>(() => new SandboxAvatarCleanup(() => null, (a, p) => null, (h, a, p) => null, (a, p) => null, new ProviderType[0], TimeSpan.FromHours(1)));
        }

        [Fact]
        public void SessionLimiterBoundsEachAddressPerHourAndEveryoneAcrossTheDay()
        {
            var now = Now;
            var limiter = new SandboxSessionLimiter(perAddressPerHour: 2, perDay: 5, utcNow: () => now);

            Assert.True(limiter.TryAcquire("1.1.1.1", out _));
            Assert.True(limiter.TryAcquire("1.1.1.1", out _));
            Assert.False(limiter.TryAcquire("1.1.1.1", out var refusal));
            Assert.Contains("per hour", refusal);
            Assert.True(limiter.TryAcquire("2.2.2.2", out _));

            now = now.AddMinutes(61);
            Assert.True(limiter.TryAcquire("1.1.1.1", out _), "the hourly window slides");
            Assert.True(limiter.TryAcquire("3.3.3.3", out _));
            Assert.False(limiter.TryAcquire("4.4.4.4", out var daily));
            Assert.Contains("24 hours", daily);

            now = now.AddHours(25);
            Assert.True(limiter.TryAcquire("4.4.4.4", out _), "the daily window slides too");
        }

        [Fact]
        public void SessionLimiterTreatsAMissingAddressAsOneShared()
        {
            var limiter = new SandboxSessionLimiter(perAddressPerHour: 1, perDay: 10, utcNow: () => Now);
            Assert.True(limiter.TryAcquire(null, out _));
            Assert.False(limiter.TryAcquire("", out _));
        }
    }
}
