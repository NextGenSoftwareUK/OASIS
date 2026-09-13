using System;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.Logging;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
{
    /// <summary>
    /// Computes and verifies the QEA Seal — the 14th character of a HerzID.
    ///
    /// The seal is an HMAC-SHA256 digest of five inputs reduced to a single alphanumeric character:
    ///   INPUT 1: Member's sequential number
    ///   INPUT 2: Country code
    ///   INPUT 3: QEA frequency profile (sacred number sequence, e.g. "369-144-999")
    ///   INPUT 4: Private seed (server-side, never exposed — the "Wiccian root authority" secret)
    ///   INPUT 5: Join date numerology (UTC date as yyyy-MM-dd)
    ///
    /// The digest bytes are summed mod 36 to produce a 0–35 index, mapped to 0-9 then A-Z.
    /// The same algorithm runs in the Wiccian QEA Registry. When WiccianRegistryUrl is configured,
    /// OASIS calls the registry to issue a certificate in addition to local computation.
    ///
    /// Display vs storage: the single alphanumeric char is what is stored and compared.
    /// The "✦" glyph shown in the HerzID display string is a UI-layer mapping configured in OASISDNA.
    /// </summary>
    public interface IQeaSealService
    {
        /// <summary>Computes the QEA seal character for a new member.</summary>
        Task<QeaSealResult> ComputeSealAsync(int sequentialNumber, string countryCode, string qeaProfile, DateTime joinDate);

        /// <summary>Verifies that the seal embedded in a HerzID is correct for the given member data.</summary>
        Task<bool> VerifySealAsync(string herzId, int sequentialNumber, string countryCode, string qeaProfile, DateTime joinDate);
    }

    public class QeaSealResult
    {
        /// <summary>Single alphanumeric character (0-9, A-Z) stored as the seal.</summary>
        public string SealChar { get; set; }
        /// <summary>Display glyph shown to users (configured in OASISDNA.HerzId.QeaSealDisplayGlyph for verified members).</summary>
        public string DisplayGlyph { get; set; }
        /// <summary>Whether the Wiccian QEA Registry also issued a certificate (false when registry is not configured).</summary>
        public bool RegistryCertificateIssued { get; set; }
    }

    public class QeaSealService : IQeaSealService
    {
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        private HerzIdSettings HerzCfg =>
            OASISDNAManager.OASISDNA?.OASIS?.Security?.HerzId ?? new HerzIdSettings();

        public Task<QeaSealResult> ComputeSealAsync(int sequentialNumber, string countryCode, string qeaProfile, DateTime joinDate)
        {
            var sealChar = ComputeLocal(sequentialNumber, countryCode, qeaProfile, joinDate);
            var result = new QeaSealResult
            {
                SealChar     = sealChar,
                DisplayGlyph = HerzCfg.QeaSealDisplayGlyph,
            };

            // If Wiccian registry is configured, also call it (fire-and-forget for Phase 1)
            if (!string.IsNullOrEmpty(HerzCfg.WiccianRegistryUrl))
                _ = TryIssueRegistryCertificateAsync(sequentialNumber, countryCode, qeaProfile, joinDate, sealChar, result);
            else
                result.RegistryCertificateIssued = false;

            return Task.FromResult(result);
        }

        public Task<bool> VerifySealAsync(string herzId, int sequentialNumber, string countryCode, string qeaProfile, DateTime joinDate)
        {
            if (string.IsNullOrEmpty(herzId)) return Task.FromResult(false);
            // Last character of the stored HerzID is the seal char
            var storedSeal = herzId[herzId.Length - 1].ToString().ToUpperInvariant();
            var expected   = ComputeLocal(sequentialNumber, countryCode, qeaProfile, joinDate);
            return Task.FromResult(string.Equals(storedSeal, expected, StringComparison.OrdinalIgnoreCase));
        }

        // ── Local HMAC-SHA256 computation ─────────────────────────────────────

        private string ComputeLocal(int sequentialNumber, string countryCode, string qeaProfile, DateTime joinDate)
        {
            var seed = ResolvePrivateSeed();
            var payload = $"{sequentialNumber}|{countryCode}|{qeaProfile}|{joinDate:yyyy-MM-dd}";
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(seed));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));

            // Reduce to 0–35 → map to 0-9 / A-Z
            int sum = 0;
            foreach (var b in hash) sum += b;
            int idx = sum % 36;
            return idx < 10
                ? idx.ToString()
                : ((char)('A' + idx - 10)).ToString();
        }

        private string ResolvePrivateSeed()
        {
            // Environment variable takes priority (for Railway secrets)
            var env = Environment.GetEnvironmentVariable("OASIS_HERZID_QEA_SEED");
            if (!string.IsNullOrEmpty(env)) return env;
            var cfg = HerzCfg.QeaPrivateSeed;
            if (!string.IsNullOrEmpty(cfg)) return cfg;
            // Fallback: derive from the OASIS JWT secret (not ideal — set a dedicated seed in prod)
            return OASISDNAManager.OASISDNA?.OASIS?.Security?.SecretKey ?? "oasis-herzid-default";
        }

        // ── Wiccian QEA Registry integration ─────────────────────────────────

        private async Task TryIssueRegistryCertificateAsync(int seq, string country, string profile,
            DateTime joinDate, string sealChar, QeaSealResult result)
        {
            try
            {
                var apiKey = Environment.GetEnvironmentVariable("OASIS_WICCIAN_API_KEY")
                             ?? HerzCfg.WiccianApiKey;
                var payload = JsonSerializer.Serialize(new
                {
                    sequential_number = seq,
                    country_code      = country,
                    qea_profile       = profile,
                    join_date         = joinDate.ToString("yyyy-MM-dd"),
                    seal_char         = sealChar,
                });
                using var req = new HttpRequestMessage(HttpMethod.Post,
                    $"{HerzCfg.WiccianRegistryUrl.TrimEnd('/')}/api/certificates/issue")
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };
                if (!string.IsNullOrEmpty(apiKey))
                    req.Headers.Add("X-Wiccian-Api-Key", apiKey);

                var resp = await _http.SendAsync(req);
                result.RegistryCertificateIssued = resp.IsSuccessStatusCode;
                if (!resp.IsSuccessStatusCode)
                    LoggingManager.Log($"[QeaSeal] Wiccian registry returned {(int)resp.StatusCode}.", LogType.Warning);
            }
            catch (Exception ex)
            {
                LoggingManager.Log($"[QeaSeal] Wiccian registry call failed: {ex.Message}", LogType.Warning);
                result.RegistryCertificateIssued = false;
            }
        }
    }
}
