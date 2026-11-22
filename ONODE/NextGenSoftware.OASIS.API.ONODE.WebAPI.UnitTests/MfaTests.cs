using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Mfa;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class MfaTests
{
    private sealed class MemoryStore : IMfaStore
    {
        public int Calls; public readonly Dictionary<Guid, MfaRecord> Records = new(); public readonly Dictionary<string, MfaChallenge> Challenges = new(); public readonly List<MfaEvent> Events = new();
        public Task<MfaRecord> GetAsync(Guid id, CancellationToken c) { Calls++; return Task.FromResult(Records.TryGetValue(id, out var r) ? Clone(r) : null); }
        public Task SaveAsync(MfaRecord r, CancellationToken c) { Calls++; Records[r.AvatarId] = Clone(r); return Task.CompletedTask; }
        public Task DeleteAsync(Guid id, CancellationToken c) { Calls++; Records.Remove(id); return Task.CompletedTask; }
        public Task SaveChallengeAsync(MfaChallenge ch, CancellationToken c) { Calls++; Challenges[ch.Id] = ch; return Task.CompletedTask; }
        public Task<MfaChallenge> GetChallengeAsync(string id, CancellationToken c) { Calls++; return Task.FromResult(Challenges.TryGetValue(id, out var ch) ? ch : null); }
        public Task<int> AddChallengeAttemptAsync(string id, CancellationToken c) { if (!Challenges.TryGetValue(id, out var ch)) return Task.FromResult(0); return Task.FromResult(++ch.Attempts); }
        public Task<bool> ConsumeChallengeAsync(string id, CancellationToken c) => Task.FromResult(Challenges.Remove(id));
        public Task AddEventAsync(MfaEvent e, CancellationToken c) { Events.Add(e); return Task.CompletedTask; }
        public Task<IReadOnlyList<MfaEvent>> GetEventsAsync(Guid id, int l, CancellationToken c) => Task.FromResult<IReadOnlyList<MfaEvent>>(Events.Where(e => e.AvatarId == id).ToList());
        private static MfaRecord Clone(MfaRecord r) => new() { AvatarId = r.AvatarId, Enabled = r.Enabled, SecretCipher = r.SecretCipher, PendingSecretCipher = r.PendingSecretCipher, RecoveryHashes = r.RecoveryHashes.ToList(), LastStep = r.LastStep, FailedCount = r.FailedCount, LockedUntil = r.LockedUntil, UpdatedAt = r.UpdatedAt };
    }

    private static readonly Guid Avatar = Guid.NewGuid();
    private DateTime _now = new DateTime(2026, 11, 12, 10, 0, 0, DateTimeKind.Utc);

    private (MfaService Service, MemoryStore Store, MfaSettings Settings) Make(Action<MfaSettings> configure = null)
    {
        var settings = new MfaSettings { Enabled = true };
        configure?.Invoke(settings);
        var store = new MemoryStore();
        return (new MfaService(() => settings, store, "unit-test-secret", () => _now), store, settings);
    }

    private static byte[] SecretOf(string base32) => Totp.Base32Decode(base32);
    private string CodeNow(string base32) => Totp.Code(SecretOf(base32), Totp.Step(_now, 30));

    private async Task<(MfaService, MemoryStore, string Secret, List<string> Recovery)> Enrolled(Action<MfaSettings> configure = null)
    {
        var (service, store, _) = Make(configure);
        var enrolment = await service.BeginEnrolAsync(Avatar, "alice");
        var recovery = await service.ConfirmEnrolAsync(Avatar, CodeNow(enrolment.Secret));
        _now = _now.AddSeconds(31); // the enrolment code's step is used; move on
        return (service, store, enrolment.Secret, recovery);
    }

    // RFC 6238 Appendix B (SHA-1, secret "12345678901234567890", 8 digits)
    [Theory]
    [InlineData(59L, "94287082")]
    [InlineData(1111111109L, "07081804")]
    [InlineData(1234567890L, "89005924")]
    [InlineData(20000000000L, "65353130")]
    public void TotpMatchesTheRfc6238TestVectors(long seconds, string expected)
        => Assert.Equal(expected, Totp.Code(Encoding.ASCII.GetBytes("12345678901234567890"), seconds / 30, 8));

    [Fact] public void Base32RoundTrips() { var s = Totp.NewSecret(); Assert.Equal(s, Totp.Base32Decode(Totp.Base32Encode(s))); }

    [Fact]
    public async Task WhenOffNoSecondStepIsNeededAndTheStoreIsNeverTouched()
    {
        var store = new MemoryStore();
        var service = new MfaService(() => new MfaSettings(), store, "x");
        Assert.False(new MfaSettings().Enabled);
        Assert.False(await service.NeedsSecondStepAsync(Avatar, "app"));
        Assert.False((await service.VerifyAsync("anything", "123456", null)).Success);
        Assert.Equal(0, store.Calls);
    }

    [Fact]
    public async Task AvatarsWithoutMfaSignInExactlyAsBefore()
    {
        var (service, store, _) = Make();
        Assert.False(await service.NeedsSecondStepAsync(Avatar, "app"));
        Assert.Empty(store.Challenges);
    }

    [Fact]
    public async Task AppListLimitsWhichAppsAskForTheSecondStep()
    {
        var (service, _, secret, _) = await Enrolled(s => s.Apps = new() { "piper" });
        Assert.True(await service.NeedsSecondStepAsync(Avatar, "piper"));
        Assert.False(await service.NeedsSecondStepAsync(Avatar, "other"));
    }

    [Fact]
    public async Task EnrolConfirmThenChallengeVerifyIssuesAnIdentityOnce()
    {
        var (service, store, secret, recovery) = await Enrolled();
        Assert.Equal(10, recovery.Count);
        Assert.True(await service.NeedsSecondStepAsync(Avatar, null));
        var token = await service.CreateChallengeAsync(Avatar, "1.1.1.1");
        var ok = await service.VerifyAsync(token, CodeNow(secret), null);
        Assert.True(ok.Success); Assert.Equal(Avatar, ok.AvatarId);
        Assert.False((await service.VerifyAsync(token, CodeNow(secret), null)).Success); // challenge is single use
    }

    [Fact]
    public async Task SecretIsEncryptedAtRestAndRecoveryCodesAreHashed()
    {
        var (_, store, secret, recovery) = await Enrolled();
        var record = store.Records[Avatar];
        Assert.DoesNotContain(secret, record.SecretCipher);
        Assert.All(recovery, code => Assert.DoesNotContain(record.RecoveryHashes, h => h.Contains(code.Replace("-", ""))));
    }

    [Fact]
    public async Task WrongCodeFailsAndExpiredChallengeFails()
    {
        var (service, _, secret, _) = await Enrolled();
        var token = await service.CreateChallengeAsync(Avatar, null);
        Assert.False((await service.VerifyAsync(token, "000000", null)).Success);
        _now = _now.AddSeconds(301);
        Assert.False((await service.VerifyAsync(token, CodeNow(secret), null)).Success);
    }

    [Fact]
    public async Task ReusingACodeInTheSameStepIsReplayAndRejected()
    {
        var (service, _, secret, _) = await Enrolled();
        var code = CodeNow(secret);
        var t1 = await service.CreateChallengeAsync(Avatar, null);
        Assert.True((await service.VerifyAsync(t1, code, null)).Success);
        var t2 = await service.CreateChallengeAsync(Avatar, null);
        Assert.False((await service.VerifyAsync(t2, code, null)).Success);
    }

    [Fact]
    public async Task ChallengeDiesAfterTooManyWrongCodes()
    {
        var (service, store, secret, _) = await Enrolled(s => { s.MaxChallengeAttempts = 3; s.MaxFailedCodes = 99; });
        var token = await service.CreateChallengeAsync(Avatar, null);
        for (var i = 0; i < 3; i++) Assert.False((await service.VerifyAsync(token, "000000", null)).Success);
        Assert.False((await service.VerifyAsync(token, CodeNow(secret), null)).Success); // even the right code now
    }

    [Fact]
    public async Task AvatarCooldownStartsAfterRepeatedWrongCodesAndEnds()
    {
        var (service, _, secret, _) = await Enrolled(s => { s.MaxFailedCodes = 2; s.CooldownSeconds = 60; s.MaxChallengeAttempts = 99; });
        var token = await service.CreateChallengeAsync(Avatar, null);
        await service.VerifyAsync(token, "000000", null); await service.VerifyAsync(token, "000000", null);
        var locked = await service.VerifyAsync(token, CodeNow(secret), null);
        Assert.False(locked.Success); Assert.Contains("Too many", locked.Error);
        _now = _now.AddSeconds(61);
        Assert.True((await service.VerifyAsync(token, CodeNow(secret), null)).Success);
    }

    [Fact]
    public async Task RecoveryCodeWorksOnceAndOnlyOnce()
    {
        var (service, _, _, recovery) = await Enrolled();
        var t1 = await service.CreateChallengeAsync(Avatar, null);
        var first = await service.VerifyAsync(t1, null, recovery[0]);
        Assert.True(first.Success); Assert.True(first.UsedRecoveryCode);
        var t2 = await service.CreateChallengeAsync(Avatar, null);
        Assert.False((await service.VerifyAsync(t2, null, recovery[0])).Success);
        Assert.True((await service.VerifyAsync(t2, null, recovery[1])).Success);
    }

    [Fact]
    public async Task DisableNeedsACodeAndRespectsTypesThatCannotDisable()
    {
        var (service, store, secret, _) = await Enrolled();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.DisableAsync(Avatar, "000000", null, false));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DisableAsync(Avatar, CodeNow(secret), null, true));
        await service.DisableAsync(Avatar, CodeNow(secret), null, false);
        Assert.False(store.Records.ContainsKey(Avatar));
        Assert.False(await service.NeedsSecondStepAsync(Avatar, null));
    }

    [Fact]
    public async Task AdministratorResetIsAuditedWithTheAdministratorId()
    {
        var (service, store, _, _) = await Enrolled();
        var wizard = Guid.NewGuid();
        await service.ResetAsync(Avatar, wizard);
        Assert.False(store.Records.ContainsKey(Avatar));
        var e = store.Events.Last(x => x.Kind == "reset");
        Assert.Equal(wizard, e.ActorId); Assert.Equal(Avatar, e.AvatarId);
    }

    [Fact]
    public async Task EventsAreRecordedAndNeverContainSecretsOrCodes()
    {
        var (service, store, secret, recovery) = await Enrolled();
        var token = await service.CreateChallengeAsync(Avatar, null);
        await service.VerifyAsync(token, "000000", null);
        var kinds = store.Events.Select(e => e.Kind).ToList();
        Assert.Contains("enrol-started", kinds); Assert.Contains("enrol-confirmed", kinds); Assert.Contains("verify-failed", kinds);
        var text = string.Join("|", store.Events.Select(e => e.Detail));
        Assert.DoesNotContain(secret, text); Assert.DoesNotContain(token, text);
    }

    [Fact]
    public async Task RegeneratingRecoveryCodesInvalidatesTheOldOnes()
    {
        var (service, _, secret, recovery) = await Enrolled();
        var fresh = await service.RegenerateRecoveryCodesAsync(Avatar, CodeNow(secret));
        var token = await service.CreateChallengeAsync(Avatar, null);
        Assert.False((await service.VerifyAsync(token, null, recovery[0])).Success);
        Assert.True((await service.VerifyAsync(token, null, fresh[0])).Success);
    }

    [Fact] public void DnaDefaultsAreOff() { var s = new MfaSettings(); Assert.False(s.Enabled); Assert.Empty(s.Apps); Assert.Empty(s.AvatarTypesThatCannotDisable); Assert.Equal(6, s.CodeDigits); }
}
