using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.DNA
{
    /// <summary>
    /// Optional second step at sign-in (authenticator-app TOTP codes plus one-time recovery codes). Off by default:
    /// with Enabled false every sign-in path behaves exactly as before. Set in OASIS_DNA.json under
    /// OASIS.Security.Mfa (on Railway, edit the OASIS_DNA_JSON variable). State is stored server-side in the
    /// MongoDBOASIS database (collections AvatarMfa and AvatarMfaEvents), never in avatar data an API user can edit.
    /// </summary>
    public class MfaSettings
    {
        /// <summary>Master switch. Default false: no MFA endpoints work, no sign-in path asks for a second step.</summary>
        public bool Enabled { get; set; } = false;
        /// <summary>Name shown in the authenticator app.</summary>
        public string Issuer { get; set; } = "OASIS";
        /// <summary>Avatar types that cannot turn MFA off themselves once enrolled (e.g. ["Wizard"]); an administrator reset is needed. Empty = anyone can disable their own.</summary>
        public List<string> AvatarTypesThatCannotDisable { get; set; } = new List<string>();
        /// <summary>App ids (X-OASIS-App-Id) whose sign-ins require MFA for every avatar that has enrolled. Empty = all apps.</summary>
        public List<string> Apps { get; set; } = new List<string>();
        public int CodeDigits { get; set; } = 6;
        public int TimeStepSeconds { get; set; } = 30;
        /// <summary>Accepted clock drift in time-steps either side of now.</summary>
        public int AllowedDriftSteps { get; set; } = 1;
        /// <summary>Seconds a second-step challenge stays valid after the password check.</summary>
        public int ChallengeTtlSeconds { get; set; } = 300;
        /// <summary>Wrong codes allowed on one challenge before it is destroyed.</summary>
        public int MaxChallengeAttempts { get; set; } = 5;
        /// <summary>Consecutive wrong codes for one avatar before a cool-down starts.</summary>
        public int MaxFailedCodes { get; set; } = 5;
        /// <summary>First cool-down in seconds; doubles for each further failure up to MaxCooldownSeconds.</summary>
        public int CooldownSeconds { get; set; } = 60;
        public int MaxCooldownSeconds { get; set; } = 3600;
        public int RecoveryCodeCount { get; set; } = 10;
        /// <summary>Days audit events (enrol, confirm, disable, recovery use, failures, resets) are kept. 0 = forever.</summary>
        public int EventRetentionDays { get; set; } = 730;
    }
}
