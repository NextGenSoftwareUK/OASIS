using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>
    /// Decides whether a NON-Wizard registration may skip the verification email (suppressVerificationEmail:true).
    /// Honoured only when ALL of these hold; otherwise the request is treated exactly as if the flag were false
    /// (the email is sent and no token is returned):
    ///   1. the operator switched programmatic verification on (OASIS_ALLOW_PROGRAMMATIC_VERIFICATION / OASIS:AllowProgrammaticVerification),
    ///   2. the server is POSITIVELY identified as a development server, and
    ///   3. the registering email's domain is under the reserved ".invalid" TLD (RFC 2606), which can never receive mail.
    /// Fails closed: anything that is not clearly a development server counts as live. There is no default that reads as dev.
    /// </summary>
    public static class ProgrammaticVerificationPolicy
    {
        public const string EnvironmentVariable = "OASIS_ENVIRONMENT";
        private static readonly string[] DevNames = { "development", "dev" };
        private static readonly string[] LiveNames = { "production", "prod", "live" };

        /// <summary>
        /// True only on a positively identified development server. Signals, all read from the server's own environment:
        ///   OASIS_ENVIRONMENT = development | dev      (explicit; production | prod | live forces live and overrides every other signal)
        ///   ASPNETCORE_ENVIRONMENT = Development       (the standard ASP.NET Core convention)
        ///   RAILWAY_ENVIRONMENT_NAME = development | dev  (set automatically by Railway for the deployment's environment)
        /// A Railway environment named production / prod / live always means live. With no signal the answer is false.
        /// </summary>
        public static bool IsDevelopmentEnvironment(Func<string, string> env)
        {
            env ??= Environment.GetEnvironmentVariable;
            string oasis = Norm(env(EnvironmentVariable));
            string aspnet = Norm(env("ASPNETCORE_ENVIRONMENT"));
            string railway = Norm(env("RAILWAY_ENVIRONMENT_NAME"));

            if (Array.IndexOf(LiveNames, oasis) >= 0) return false;
            if (Array.IndexOf(LiveNames, railway) >= 0) return false;

            return Array.IndexOf(DevNames, oasis) >= 0
                || aspnet == "development"
                || Array.IndexOf(DevNames, railway) >= 0;
        }

        /// <summary>
        /// True only for a single plain address whose domain (the text after the one '@') ends in ".invalid".
        /// Rejects look-alikes such as a@b.invalid.com, a.invalid@b.com, lists and anything with unusual characters.
        /// </summary>
        public static bool IsInvalidTestEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            email = email.Trim();
            if (email.Length > 254) return false;

            int at = email.IndexOf('@');
            if (at <= 0 || at != email.LastIndexOf('@') || at == email.Length - 1) return false;

            string local = email.Substring(0, at);
            string domain = email.Substring(at + 1).ToLowerInvariant();
            foreach (char c in local)
                if (char.IsWhiteSpace(c) || char.IsControl(c) || c == ',' || c == ';' || c == '<' || c == '>' || c == '"' || c == '(' || c == ')')
                    return false;
            foreach (char c in domain)
                if (!((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '.' || c == '-'))
                    return false;

            if (domain.StartsWith(".") || domain.EndsWith(".") || domain.Contains("..")) return false;
            return domain.EndsWith(".invalid", StringComparison.Ordinal) && domain.Length > ".invalid".Length;
        }

        /// <summary>Whether a non-Wizard caller's suppressVerificationEmail request is honoured.</summary>
        public static bool Honour(string email, bool programmaticVerificationEnabled, Func<string, string> env = null) =>
            programmaticVerificationEnabled && IsDevelopmentEnvironment(env) && IsInvalidTestEmail(email);

        /// <summary>
        /// The single decision for a registration: is the verification email actually suppressed?
        /// A Wizard caller keeps the existing behaviour (honoured as requested); everyone else needs <see cref="Honour"/>.
        /// </summary>
        public static bool ShouldSuppress(bool requested, bool callerIsWizard, string email, bool programmaticVerificationEnabled, Func<string, string> env = null) =>
            requested && (callerIsWizard || Honour(email, programmaticVerificationEnabled, env));

        private static string Norm(string value) => string.IsNullOrWhiteSpace(value) ? "" : value.Trim().ToLowerInvariant();
    }
}
