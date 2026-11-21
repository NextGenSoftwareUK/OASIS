using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.DNA;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Mfa
{
    public class MfaRecord
    {
        public Guid AvatarId { get; set; }
        public bool Enabled { get; set; }
        /// <summary>AES-GCM encrypted TOTP secret (base64). Never logged, never returned after enrolment.</summary>
        public string SecretCipher { get; set; }
        /// <summary>Secret awaiting its first confirming code.</summary>
        public string PendingSecretCipher { get; set; }
        /// <summary>SHA-256 hex of each unused recovery code.</summary>
        public List<string> RecoveryHashes { get; set; } = new List<string>();
        /// <summary>Last accepted TOTP time-step (replay guard).</summary>
        public long LastStep { get; set; } = -1;
        public int FailedCount { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class MfaChallenge
    {
        /// <summary>SHA-256 hex of the challenge token (the token itself is never stored).</summary>
        public string Id { get; set; }
        public Guid AvatarId { get; set; }
        public DateTime ExpiresAt { get; set; }
        public int Attempts { get; set; }
        public string IpAddress { get; set; }
    }

    public class MfaEvent
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; }
        public Guid AvatarId { get; set; }
        /// <summary>The avatar that did it (the same avatar, or a Wizard resetting someone).</summary>
        public Guid ActorId { get; set; }
        /// <summary>enrol-started, enrol-confirmed, verify-success, verify-failed, recovery-used, disabled, reset, recovery-regenerated, challenge-issued, locked.</summary>
        public string Kind { get; set; }
        public string Detail { get; set; }
    }

    public interface IMfaStore
    {
        Task<MfaRecord> GetAsync(Guid avatarId, CancellationToken c);
        Task SaveAsync(MfaRecord record, CancellationToken c);
        Task DeleteAsync(Guid avatarId, CancellationToken c);
        Task SaveChallengeAsync(MfaChallenge challenge, CancellationToken c);
        Task<MfaChallenge> GetChallengeAsync(string id, CancellationToken c);
        /// <summary>Adds one attempt and returns the new count (0 if the challenge no longer exists).</summary>
        Task<int> AddChallengeAttemptAsync(string id, CancellationToken c);
        /// <summary>Removes the challenge; true only for the single caller that removed it (single use).</summary>
        Task<bool> ConsumeChallengeAsync(string id, CancellationToken c);
        Task AddEventAsync(MfaEvent evt, CancellationToken c);
        Task<IReadOnlyList<MfaEvent>> GetEventsAsync(Guid avatarId, int limit, CancellationToken c);
    }

    public class MfaVerifyResult
    {
        public bool Success { get; set; }
        public Guid AvatarId { get; set; }
        public string Error { get; set; }
        public bool UsedRecoveryCode { get; set; }
    }

    public class MfaEnrolment { public string Secret { get; set; } public string OtpAuthUri { get; set; } }

    /// <summary>
    /// The optional second sign-in step. With MFA.Enabled false every method that decides whether a second step is needed
    /// returns false without touching the store, so existing sign-in behaviour is unchanged.
    /// </summary>
    public class MfaService
    {
        public const string ChallengeKey = "mfaChallengeToken";
        public const string RequiredKey = "mfaRequired";
        private readonly Func<MfaSettings> _settings;
        private readonly IMfaStore _store;
        private readonly Func<DateTime> _clock;
        private readonly byte[] _key;

        /// <param name="encryptionSecret">Existing OASIS_DNA.json Security.SecretKey; the MFA encryption key is derived from it.</param>
        public MfaService(Func<MfaSettings> settings, IMfaStore store, string encryptionSecret, Func<DateTime> clock = null)
        {
            _settings = settings; _store = store; _clock = clock ?? (() => DateTime.UtcNow);
            _key = SHA256.HashData(Encoding.UTF8.GetBytes("oasis-mfa-v1|" + (encryptionSecret ?? "")));
        }

        public bool IsEnabled => _settings()?.Enabled == true;

        private MfaSettings S => _settings() ?? new MfaSettings();

        /// <summary>True when this sign-in must be followed by a second step. Never touches the store while MFA is off.</summary>
        public async Task<bool> NeedsSecondStepAsync(Guid avatarId, string appId, CancellationToken c = default)
        {
            var s = _settings();
            if (s == null || !s.Enabled || avatarId == Guid.Empty) return false;
            if (s.Apps != null && s.Apps.Count > 0 && !s.Apps.Contains(appId ?? "", StringComparer.OrdinalIgnoreCase)) return false;
            var record = await _store.GetAsync(avatarId, c).ConfigureAwait(false);
            return record != null && record.Enabled;
        }

        public async Task<string> CreateChallengeAsync(Guid avatarId, string ip, CancellationToken c = default)
        {
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
            await _store.SaveChallengeAsync(new MfaChallenge
            {
                Id = Hash(token), AvatarId = avatarId, IpAddress = ip,
                ExpiresAt = _clock().AddSeconds(S.ChallengeTtlSeconds <= 0 ? 300 : S.ChallengeTtlSeconds)
            }, c).ConfigureAwait(false);
            await Log(avatarId, avatarId, "challenge-issued", null, c).ConfigureAwait(false);
            return token;
        }

        public async Task<MfaEnrolment> BeginEnrolAsync(Guid avatarId, string accountName, CancellationToken c = default)
        {
            Require();
            var existing = await _store.GetAsync(avatarId, c).ConfigureAwait(false);
            if (existing != null && existing.Enabled) throw new InvalidOperationException("MFA is already enabled. Disable it first to enrol again.");
            var secret = Totp.NewSecret();
            var record = existing ?? new MfaRecord { AvatarId = avatarId };
            record.PendingSecretCipher = Encrypt(secret); record.UpdatedAt = _clock();
            await _store.SaveAsync(record, c).ConfigureAwait(false);
            await Log(avatarId, avatarId, "enrol-started", null, c).ConfigureAwait(false);
            return new MfaEnrolment { Secret = Totp.Base32Encode(secret), OtpAuthUri = Totp.OtpAuthUri(S.Issuer ?? "OASIS", accountName, secret, Digits, Step) };
        }

        /// <summary>Activates MFA when the first code from the authenticator app is right; returns the one-time recovery codes (shown once).</summary>
        public async Task<List<string>> ConfirmEnrolAsync(Guid avatarId, string code, CancellationToken c = default)
        {
            Require();
            var record = await _store.GetAsync(avatarId, c).ConfigureAwait(false);
            if (record == null || string.IsNullOrEmpty(record.PendingSecretCipher)) throw new InvalidOperationException("Start enrolment first.");
            var secret = Decrypt(record.PendingSecretCipher);
            var step = Totp.Match(secret, code, _clock(), Step, S.AllowedDriftSteps, Digits);
            if (step < 0) { await Log(avatarId, avatarId, "verify-failed", "enrol-confirm", c).ConfigureAwait(false); throw new UnauthorizedAccessException("That code is not right."); }
            var codes = NewRecoveryCodes();
            record.SecretCipher = record.PendingSecretCipher; record.PendingSecretCipher = null;
            record.Enabled = true; record.LastStep = step; record.FailedCount = 0; record.LockedUntil = null;
            record.RecoveryHashes = codes.Select(RecoveryHash).ToList(); record.UpdatedAt = _clock();
            await _store.SaveAsync(record, c).ConfigureAwait(false);
            await Log(avatarId, avatarId, "enrol-confirmed", null, c).ConfigureAwait(false);
            return codes;
        }

        /// <summary>Checks the second step for a challenge. A wrong code counts toward the challenge limit and the avatar cool-down.</summary>
        public async Task<MfaVerifyResult> VerifyAsync(string challengeToken, string code, string recoveryCode, CancellationToken c = default)
        {
            var fail = new MfaVerifyResult { Error = "Invalid or expired sign-in challenge." };
            if (!IsEnabled || string.IsNullOrWhiteSpace(challengeToken)) return fail;
            var id = Hash(challengeToken);
            var challenge = await _store.GetChallengeAsync(id, c).ConfigureAwait(false);
            if (challenge == null || challenge.ExpiresAt <= _clock()) { if (challenge != null) await _store.ConsumeChallengeAsync(id, c).ConfigureAwait(false); return fail; }
            var avatarId = challenge.AvatarId;
            var record = await _store.GetAsync(avatarId, c).ConfigureAwait(false);
            if (record == null || !record.Enabled) return fail;
            if (record.LockedUntil.HasValue && record.LockedUntil.Value > _clock())
                return new MfaVerifyResult { AvatarId = avatarId, Error = "Too many wrong codes. Try again later." };

            var ok = false; var usedRecovery = false;
            if (!string.IsNullOrWhiteSpace(recoveryCode))
            {
                var hash = RecoveryHash(recoveryCode);
                if (record.RecoveryHashes.Remove(hash)) { ok = true; usedRecovery = true; }
            }
            else
            {
                var step = Totp.Match(Decrypt(record.SecretCipher), code, _clock(), Step, S.AllowedDriftSteps, Digits);
                if (step >= 0 && step > record.LastStep) { ok = true; record.LastStep = step; } // same code twice = replay
            }

            if (!ok)
            {
                record.FailedCount++;
                var max = S.MaxFailedCodes <= 0 ? 5 : S.MaxFailedCodes;
                if (record.FailedCount >= max)
                {
                    var baseSeconds = S.CooldownSeconds <= 0 ? 60 : S.CooldownSeconds;
                    var seconds = Math.Min(S.MaxCooldownSeconds <= 0 ? 3600 : S.MaxCooldownSeconds, baseSeconds * Math.Pow(2, Math.Min(record.FailedCount - max, 20)));
                    record.LockedUntil = _clock().AddSeconds(seconds);
                    await Log(avatarId, avatarId, "locked", $"{(int)seconds}s", c).ConfigureAwait(false);
                }
                record.UpdatedAt = _clock();
                await _store.SaveAsync(record, c).ConfigureAwait(false);
                await Log(avatarId, avatarId, "verify-failed", null, c).ConfigureAwait(false);
                var attempts = await _store.AddChallengeAttemptAsync(id, c).ConfigureAwait(false);
                if (attempts >= (S.MaxChallengeAttempts <= 0 ? 5 : S.MaxChallengeAttempts)) await _store.ConsumeChallengeAsync(id, c).ConfigureAwait(false);
                return new MfaVerifyResult { AvatarId = avatarId, Error = "That code is not right." };
            }

            if (!await _store.ConsumeChallengeAsync(id, c).ConfigureAwait(false)) return fail; // single use, even under a race
            record.FailedCount = 0; record.LockedUntil = null; record.UpdatedAt = _clock();
            await _store.SaveAsync(record, c).ConfigureAwait(false);
            await Log(avatarId, avatarId, usedRecovery ? "recovery-used" : "verify-success", usedRecovery ? $"{record.RecoveryHashes.Count} left" : null, c).ConfigureAwait(false);
            return new MfaVerifyResult { Success = true, AvatarId = avatarId, UsedRecoveryCode = usedRecovery };
        }

        /// <summary>Turns MFA off for the avatar. The caller must have checked the password; this checks a current code or a recovery code.</summary>
        public async Task DisableAsync(Guid avatarId, string code, string recoveryCode, bool avatarTypeCannotDisable, CancellationToken c = default)
        {
            Require();
            if (avatarTypeCannotDisable) throw new InvalidOperationException("This avatar type must keep MFA on. Ask an administrator.");
            var record = await _store.GetAsync(avatarId, c).ConfigureAwait(false);
            if (record == null || !record.Enabled) throw new InvalidOperationException("MFA is not enabled.");
            if (!await CheckCodeAsync(record, code, recoveryCode, c).ConfigureAwait(false)) throw new UnauthorizedAccessException("That code is not right.");
            await _store.DeleteAsync(avatarId, c).ConfigureAwait(false);
            await Log(avatarId, avatarId, "disabled", null, c).ConfigureAwait(false);
        }

        /// <summary>Administrator reset for a lost device. Always audited with the administrator's id.</summary>
        public async Task ResetAsync(Guid avatarId, Guid wizardId, CancellationToken c = default)
        {
            Require();
            await _store.DeleteAsync(avatarId, c).ConfigureAwait(false);
            await Log(avatarId, wizardId, "reset", null, c).ConfigureAwait(false);
        }

        public async Task<List<string>> RegenerateRecoveryCodesAsync(Guid avatarId, string code, CancellationToken c = default)
        {
            Require();
            var record = await _store.GetAsync(avatarId, c).ConfigureAwait(false);
            if (record == null || !record.Enabled) throw new InvalidOperationException("MFA is not enabled.");
            if (!await CheckCodeAsync(record, code, null, c).ConfigureAwait(false)) throw new UnauthorizedAccessException("That code is not right.");
            var codes = NewRecoveryCodes();
            record.RecoveryHashes = codes.Select(RecoveryHash).ToList(); record.UpdatedAt = _clock();
            await _store.SaveAsync(record, c).ConfigureAwait(false);
            await Log(avatarId, avatarId, "recovery-regenerated", null, c).ConfigureAwait(false);
            return codes;
        }

        public async Task<(bool Enabled, int RecoveryCodesLeft)> StatusAsync(Guid avatarId, CancellationToken c = default)
        {
            var record = await _store.GetAsync(avatarId, c).ConfigureAwait(false);
            return (record?.Enabled == true, record?.RecoveryHashes?.Count ?? 0);
        }

        public Task<IReadOnlyList<MfaEvent>> EventsAsync(Guid avatarId, int limit, CancellationToken c = default)
            => _store.GetEventsAsync(avatarId, limit <= 0 ? 50 : Math.Min(limit, 200), c);

        private async Task<bool> CheckCodeAsync(MfaRecord record, string code, string recoveryCode, CancellationToken c)
        {
            if (record.LockedUntil.HasValue && record.LockedUntil.Value > _clock()) return false;
            bool ok;
            if (!string.IsNullOrWhiteSpace(recoveryCode)) ok = record.RecoveryHashes.Remove(RecoveryHash(recoveryCode));
            else
            {
                var step = Totp.Match(Decrypt(record.SecretCipher), code, _clock(), Step, S.AllowedDriftSteps, Digits);
                ok = step >= 0 && step > record.LastStep;
                if (ok) record.LastStep = step;
            }
            if (!ok)
            {
                record.FailedCount++;
                if (record.FailedCount >= (S.MaxFailedCodes <= 0 ? 5 : S.MaxFailedCodes)) record.LockedUntil = _clock().AddSeconds(S.CooldownSeconds <= 0 ? 60 : S.CooldownSeconds);
                await Log(record.AvatarId, record.AvatarId, "verify-failed", null, c).ConfigureAwait(false);
            }
            else record.FailedCount = 0;
            record.UpdatedAt = _clock();
            await _store.SaveAsync(record, c).ConfigureAwait(false);
            return ok;
        }

        private void Require() { if (!IsEnabled) throw new InvalidOperationException("MFA is not enabled on this node (OASIS.Security.Mfa.Enabled)."); }
        private int Digits => S.CodeDigits is >= 6 and <= 8 ? S.CodeDigits : 6;
        private int Step => S.TimeStepSeconds <= 0 ? 30 : S.TimeStepSeconds;

        private Task Log(Guid avatarId, Guid actor, string kind, string detail, CancellationToken c)
            => _store.AddEventAsync(new MfaEvent { Timestamp = _clock(), AvatarId = avatarId, ActorId = actor, Kind = kind, Detail = detail }, c);

        private List<string> NewRecoveryCodes()
        {
            var count = S.RecoveryCodeCount <= 0 ? 10 : S.RecoveryCodeCount;
            var codes = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var raw = Totp.Base32Encode(RandomNumberGenerator.GetBytes(8)).Substring(0, 10);
                codes.Add(raw.Substring(0, 5) + "-" + raw.Substring(5));
            }
            return codes;
        }

        private static string RecoveryHash(string code) => Hash("recovery|" + code.Replace("-", "").Replace(" ", "").ToUpperInvariant());
        private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

        private string Encrypt(byte[] secret)
        {
            var nonce = RandomNumberGenerator.GetBytes(12); var tag = new byte[16]; var cipher = new byte[secret.Length];
            using var aes = new AesGcm(_key, 16);
            aes.Encrypt(nonce, secret, cipher, tag);
            return Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
        }

        private byte[] Decrypt(string stored)
        {
            var all = Convert.FromBase64String(stored);
            var nonce = all.Take(12).ToArray(); var tag = all.Skip(12).Take(16).ToArray(); var cipher = all.Skip(28).ToArray();
            var plain = new byte[cipher.Length];
            using var aes = new AesGcm(_key, 16);
            aes.Decrypt(nonce, cipher, tag, plain);
            return plain;
        }
    }
}
