extern alias BouncyCastleCryptography;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.JsonRpc.Client;
using Nethereum.Util;
using Nethereum.Web3;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using RipeMD160Digest = BouncyCastleCryptography::Org.BouncyCastle.Crypto.Digests.RipeMD160Digest;

[assembly: InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.SelfProtocolOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.SelfProtocolOASIS
{
    [Function("registry", "address")]
    internal sealed class RegistryFunction : FunctionMessage
    {
        [Parameter("bytes32", "attestationId", 1)] public byte[] AttestationId { get; set; }
    }

    [Function("discloseVerifier", "address")]
    internal sealed class DiscloseVerifierFunction : FunctionMessage
    {
        [Parameter("bytes32", "attestationId", 1)] public byte[] AttestationId { get; set; }
    }

    [Function("checkIdentityCommitmentRoot", "bool")]
    internal sealed class CheckIdentityCommitmentRootFunction : FunctionMessage
    {
        [Parameter("uint256", "root", 1)] public BigInteger Root { get; set; }
    }

    [Function("verifyProof", "bool")]
    internal sealed class VerifyProofFunction : FunctionMessage
    {
        [Parameter("uint256[2]", "a", 1)] public List<BigInteger> A { get; set; }
        [Parameter("uint256[2][2]", "b", 2)] public List<List<BigInteger>> B { get; set; }
        [Parameter("uint256[2]", "c", 3)] public List<BigInteger> C { get; set; }
        [Parameter("uint256[21]", "pubSignals", 4)] public List<BigInteger> PubSignals { get; set; }
    }

    /// <summary>Self attestation document types (attestation ids in the Identity Verification Hub).</summary>
    public enum SelfAttestation { Passport = 1, EuIdCard = 2 }

    public enum SelfUserIdentifierType { Hex, Uuid }

    /// <summary>The disclosure rules the app requested (the SDK's VerificationConfig).</summary>
    public sealed class SelfVerificationConfig
    {
        public int? MinimumAge { get; init; }
        /// <summary>ISO 3166-1 alpha-3 codes the proof must exclude.</summary>
        public IReadOnlyList<string> ExcludedCountries { get; init; } = Array.Empty<string>();
        public bool Ofac { get; init; }
    }

    /// <summary>The body Self posts to the app's verification endpoint.</summary>
    public sealed class SelfVerificationRequest
    {
        public int AttestationId { get; init; }
        public string[] A { get; init; }
        public string[][] B { get; init; }
        public string[] C { get; init; }
        public string[] PublicSignals { get; init; }
        public string UserContextData { get; init; }

        public static SelfVerificationRequest FromJson(string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var proof = root.GetProperty("proof");
            static string S(JsonElement e) => e.ValueKind == JsonValueKind.Number ? e.GetRawText() : e.GetString();
            return new SelfVerificationRequest
            {
                AttestationId = root.GetProperty("attestationId").GetInt32(),
                A = proof.GetProperty("a").EnumerateArray().Select(S).ToArray(),
                B = proof.GetProperty("b").EnumerateArray().Select(r => r.EnumerateArray().Select(S).ToArray()).ToArray(),
                C = proof.GetProperty("c").EnumerateArray().Select(S).ToArray(),
                PublicSignals = root.GetProperty("publicSignals").EnumerateArray().Select(S).ToArray(),
                UserContextData = root.GetProperty("userContextData").GetString()
            };
        }
    }

    public sealed class SelfDisclosure
    {
        public string Nullifier { get; init; }
        public string IssuingState { get; init; }
        public string Name { get; init; }
        public string IdNumber { get; init; }
        public string Nationality { get; init; }
        public string DateOfBirth { get; init; }
        public string Gender { get; init; }
        public string ExpiryDate { get; init; }
        public string MinimumAge { get; init; }
        public bool[] Ofac { get; init; }
    }

    public sealed class SelfVerificationResult
    {
        public SelfAttestation Attestation { get; init; }
        /// <summary>Whether the Groth16 proof verified on-chain.</summary>
        public bool IsValid { get; init; }
        public bool IsMinimumAgeValid { get; init; }
        public bool IsOfacValid { get; init; }
        public IReadOnlyList<string> ForbiddenCountries { get; init; }
        public SelfDisclosure Disclosure { get; init; }
        public string UserIdentifier { get; init; }
        public string UserDefinedData { get; init; }
    }

    /// <summary>
    /// Self Protocol provider. A port of the official @selfxyz/core SelfBackendVerifier: checks the user-context hash,
    /// scope (Poseidon hash of endpoint host and scope), attestation id, identity-commitment Merkle root (Registry on
    /// Celo), forbidden countries, minimum age, circuit date and OFAC flags, then verifies the Groth16 proof with the
    /// disclose verifier the Identity Verification Hub designates. Proof generation happens in the Self app, so this is the
    /// verifier side. Self is not a data store, so this is an <see cref="OASISProvider"/>.
    /// </summary>
    public class SelfProtocolOASIS : OASISProvider
    {
        public const string CeloMainnetRpcUrl = "https://forno.celo.org";
        public const string CeloTestnetRpcUrl = "https://alfajores-forno.celo-testnet.org";
        public const string HubAddress = "0xe57F4773bd9c9d8b6Cd70431117d353298B9f5BF";
        public const string StagingHubAddress = "0x68c931C9a534D37aa78094877F46fE46a49F1A51";

        private static readonly Dictionary<int, DiscloseIndices> Indices = new()
        {
            [1] = new DiscloseIndices(0, 3, 7, 8, 9, 10, 19, 20),
            [2] = new DiscloseIndices(0, 4, 8, 9, 10, 11, 19, 20)
        };

        private static readonly Dictionary<int, int[]> RevealedBytesPerSignal = new() { [1] = new[] { 31, 31, 31 }, [2] = new[] { 31, 31, 31, 1 } };

        private static readonly Dictionary<int, RevealedIndices> Revealed = new()
        {
            [1] = new RevealedIndices((2, 4), (5, 43), (44, 52), (54, 56), (57, 62), (64, 64), (65, 70), (88, 89), (90, 92)),
            [2] = new RevealedIndices((2, 4), (60, 89), (5, 13), (45, 47), (30, 35), (37, 37), (38, 43), (90, 91), (92, 93))
        };

        private readonly IWeb3 _web3;
        private readonly string _hub;
        private readonly BigInteger _scope;
        private readonly SelfUserIdentifierType _userIdentifierType;
        private readonly Func<DateTimeOffset> _now;

        /// <param name="scope">The app scope configured in the Self QR code.</param>
        /// <param name="endpoint">The app's verification endpoint (its host is hashed into the scope).</param>
        /// <param name="mockPassport">True for Self's staging hub on Celo Alfajores (mock passports).</param>
        public SelfProtocolOASIS(string scope, string endpoint, bool mockPassport = false, SelfUserIdentifierType userIdentifierType = SelfUserIdentifierType.Uuid)
            : this(new Web3(mockPassport ? CeloTestnetRpcUrl : CeloMainnetRpcUrl), mockPassport ? StagingHubAddress : HubAddress,
                   HashEndpointWithScope(endpoint, scope), userIdentifierType, () => DateTimeOffset.UtcNow) { }

        internal SelfProtocolOASIS(IWeb3 web3, string hub, BigInteger scope, SelfUserIdentifierType userIdentifierType, Func<DateTimeOffset> now)
        {
            _web3 = web3;
            _hub = hub;
            _scope = scope;
            _userIdentifierType = userIdentifierType;
            _now = now;
            ProviderName = "SelfProtocolOASIS";
            ProviderDescription = "Self Protocol passport/ID zero-knowledge proof verifier (Celo Identity Verification Hub).";
            ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.SelfProtocolOASIS);
            ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Identity);
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var result = new OASISResult<bool>();
            try
            {
                var code = await _web3.Eth.GetCode.SendRequestAsync(_hub);
                if (string.IsNullOrEmpty(code) || code == "0x")
                {
                    OASISErrorHandling.HandleError(ref result, $"SelfProtocolOASIS: no Identity Verification Hub at {_hub}.");
                    return result;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"SelfProtocolOASIS: cannot reach Celo RPC: {ex.Message}", ex);
                return result;
            }
            IsProviderActivated = true;
            result.Result = true;
            result.Message = "SelfProtocolOASIS activated.";
            return result;
        }

        public override OASISResult<bool> ActivateProvider() => ActivateProviderAsync().GetAwaiter().GetResult();

        public override Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return Task.FromResult(new OASISResult<bool>(true) { Message = "SelfProtocolOASIS deactivated." });
        }

        public override OASISResult<bool> DeActivateProvider() => DeActivateProviderAsync().GetAwaiter().GetResult();

        public async Task<OASISResult<SelfVerificationResult>> VerifyAsync(string requestJson, SelfVerificationConfig config)
        {
            SelfVerificationRequest request;
            try { request = SelfVerificationRequest.FromJson(requestJson); }
            catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
            {
                return Error<SelfVerificationResult>($"malformed verification request: {ex.Message}", ex);
            }
            return await VerifyAsync(request, config);
        }

        public async Task<OASISResult<SelfVerificationResult>> VerifyAsync(SelfVerificationRequest request, SelfVerificationConfig config)
        {
            if (config == null) return Error<SelfVerificationResult>("a verification config is required.");
            if (!Indices.TryGetValue(request.AttestationId, out var ix))
                return Error<SelfVerificationResult>($"[InvalidId]: Attestation ID is not allowed, received: {request.AttestationId}");

            BigInteger[] signals;
            byte[] context;
            try
            {
                signals = request.PublicSignals.Select(ParseSignal).ToArray();
                context = Convert.FromHexString(request.UserContextData.StartsWith("0x") ? request.UserContextData[2..] : request.UserContextData);
            }
            catch (FormatException ex) { return Error<SelfVerificationResult>($"malformed public signals or user context: {ex.Message}", ex); }
            if (signals.Length != 21) return Error<SelfVerificationResult>($"expected 21 public signals, received {signals.Length}.");
            if (context.Length < 64) return Error<SelfVerificationResult>("user context data must be at least 64 bytes.");

            var issues = new List<string>();
            if (signals[ix.UserIdentifier] != CalculateUserIdentifierHash(context))
                issues.Add("[InvalidUserContextHash]: User context hash does not match with the one in the circuit");
            if (signals[ix.Scope] != _scope)
                issues.Add($"[InvalidScope]: Scope does not match with the one in the circuit\nCircuit: {signals[ix.Scope]}\nScope: {_scope}");

            var attestationKey = new byte[32];
            attestationKey[31] = (byte)request.AttestationId;
            try
            {
                var registry = await _web3.Eth.GetContractQueryHandler<RegistryFunction>()
                    .QueryAsync<string>(_hub, new RegistryFunction { AttestationId = attestationKey });
                if (IsZeroAddress(registry)) return Error<SelfVerificationResult>("Registry contract not found");
                var rootKnown = await _web3.Eth.GetContractQueryHandler<CheckIdentityCommitmentRootFunction>()
                    .QueryAsync<bool>(registry, new CheckIdentityCommitmentRootFunction { Root = signals[ix.MerkleRoot] });
                if (!rootKnown) issues.Add($"[InvalidRoot]: Onchain root does not exist, received: {signals[ix.MerkleRoot]}");
            }
            catch (Exception ex) { return Error<SelfVerificationResult>($"registry lookup failed: {RpcMessage(ex)}", ex); }

            if (signals[ix.AttestationId] != request.AttestationId)
                issues.Add("[InvalidAttestationId]: Attestation ID does not match with the one in the circuit");

            var userIdentifier = CastToUserIdentifier(new BigInteger(context.AsSpan(32, 32), isUnsigned: true, isBigEndian: true), _userIdentifierType);
            var userDefinedData = Convert.ToHexString(context.AsSpan(64)).ToLowerInvariant();

            var forbidden = UnpackForbiddenCountries(Enumerable.Range(0, 4).Select(i => signals[ix.ForbiddenCountriesPacked + i]));
            var excluded = config.ExcludedCountries ?? Array.Empty<string>();
            if (!excluded.All(forbidden.Contains))
                issues.Add($"[InvalidForbiddenCountriesList]: Forbidden countries list in config does not match with the one in the circuit\nCircuit: {string.Join(", ", forbidden)}\nConfig: {string.Join(", ", excluded)}");

            var disclosure = FormatRevealedData(request.AttestationId, signals, ix);
            if (config.MinimumAge != null && !(int.TryParse(disclosure.MinimumAge, NumberStyles.None, CultureInfo.InvariantCulture, out var age) && age == config.MinimumAge) && disclosure.MinimumAge != "00")
                issues.Add($"[InvalidMinimumAge]: Minimum age in config does not match with the one in the circuit\nCircuit: {disclosure.MinimumAge}\nConfig: {config.MinimumAge}");

            var date = CircuitDate(signals, ix.CurrentDate);
            var now = _now();
            if (date == null) issues.Add("[InvalidTimestamp]: Circuit timestamp is not a valid date");
            else if (date > now.AddDays(1)) issues.Add("[InvalidTimestamp]: Circuit timestamp is in the future");
            else if (date < now.AddDays(-1)) issues.Add("[InvalidTimestamp]: Circuit timestamp is too old");

            if (!config.Ofac)
            {
                if (disclosure.Ofac.ElementAtOrDefault(0)) issues.Add("[InvalidOfac]: Passport number OFAC check is not allowed");
                if (disclosure.Ofac.ElementAtOrDefault(1)) issues.Add("[InvalidOfac]: Name and DOB OFAC check is not allowed");
                if (disclosure.Ofac.ElementAtOrDefault(2)) issues.Add("[InvalidOfac]: Name and YOB OFAC check is not allowed");
            }

            if (issues.Count > 0) return Error<SelfVerificationResult>(string.Join("\n", issues));

            string verifier;
            try
            {
                verifier = await _web3.Eth.GetContractQueryHandler<DiscloseVerifierFunction>()
                    .QueryAsync<string>(_hub, new DiscloseVerifierFunction { AttestationId = attestationKey });
            }
            catch (Exception ex) { return Error<SelfVerificationResult>($"verifier lookup failed: {RpcMessage(ex)}", ex); }
            if (IsZeroAddress(verifier)) return Error<SelfVerificationResult>("Verifier contract not found");

            bool isValid;
            try
            {
                var a = request.A.Take(2).Select(ParseSignal).ToList();
                var b = request.B.Take(2).Select(r => new List<BigInteger> { ParseSignal(r[1]), ParseSignal(r[0]) }).ToList();
                var c = request.C.Take(2).Select(ParseSignal).ToList();
                isValid = await _web3.Eth.GetContractQueryHandler<VerifyProofFunction>()
                    .QueryAsync<bool>(verifier, new VerifyProofFunction { A = a, B = b, C = c, PubSignals = signals.ToList() });
            }
            catch (SmartContractRevertException) { isValid = false; }
            catch (RpcResponseException ex) when (ex.RpcError?.Message?.Contains("revert", StringComparison.OrdinalIgnoreCase) == true) { isValid = false; }
            catch (FormatException ex) { return Error<SelfVerificationResult>($"malformed proof: {ex.Message}", ex); }
            catch (Exception ex) { return Error<SelfVerificationResult>($"verifyProof call failed: {RpcMessage(ex)}", ex); }

            return new OASISResult<SelfVerificationResult>(new SelfVerificationResult
            {
                Attestation = (SelfAttestation)request.AttestationId,
                IsValid = isValid,
                IsMinimumAgeValid = config.MinimumAge == null ||
                    (int.TryParse(disclosure.MinimumAge, NumberStyles.None, CultureInfo.InvariantCulture, out var revealedAge) && config.MinimumAge <= revealedAge),
                IsOfacValid = true,
                ForbiddenCountries = forbidden,
                Disclosure = disclosure,
                UserIdentifier = userIdentifier,
                UserDefinedData = userDefinedData
            });
        }

        // ── Ported helpers (@selfxyz/core, @selfxyz/common) ─────────────────────

        /// <summary>poseidon2([poseidonN(31-char chunks of endpoint host), ascii(scope)]) as in @selfxyz/common hashEndpointWithScope.</summary>
        internal static BigInteger HashEndpointWithScope(string endpoint, string scope)
        {
            var host = Regex.Replace(endpoint, "^https?://", "").Split('/')[0];
            var chunks = Enumerable.Range(0, (host.Length + 30) / 31).Select(i => host.Substring(i * 31, Math.Min(31, host.Length - i * 31))).ToArray();
            if (chunks.Length > 16) throw new ArgumentException("Endpoint must be less than 496 characters");
            var preset = chunks.Length switch
            {
                1 => PoseidonParameterPreset.CircomT1,
                2 => PoseidonParameterPreset.CircomT2,
                3 => PoseidonParameterPreset.CircomT3,
                6 => PoseidonParameterPreset.CircomT6,
                14 => PoseidonParameterPreset.CircomT14,
                16 => PoseidonParameterPreset.CircomT16,
                _ => throw new ArgumentException($"Endpoint host '{host}' needs {chunks.Length} Poseidon inputs; Nethereum ships circom parameters for 1, 2, 3, 6, 14 and 16 inputs only.")
            };
            var endpointHash = new PoseidonHasher(preset).Hash(chunks.Select(AsciiToBigInt).ToArray());
            return new PoseidonHasher(PoseidonParameterPreset.CircomT2).Hash(new[] { endpointHash, AsciiToBigInt(scope) });
        }

        private static BigInteger AsciiToBigInt(string s)
        {
            if (s.Any(ch => ch > 0x7F)) throw new ArgumentException("Input must contain only ASCII characters (0-127)");
            var result = BigInteger.Zero;
            foreach (var ch in s) result = (result << 8) | ch;
            return result;
        }

        /// <summary>ripemd160(sha256(userContextData)) as an integer.</summary>
        internal static BigInteger CalculateUserIdentifierHash(byte[] userContextData)
        {
            var sha = SHA256.HashData(userContextData);
            var ripemd = new RipeMD160Digest();
            ripemd.BlockUpdate(sha, 0, sha.Length);
            var digest = new byte[ripemd.GetDigestSize()];
            ripemd.DoFinal(digest, 0);
            return new BigInteger(digest, isUnsigned: true, isBigEndian: true);
        }

        internal static string CastToUserIdentifier(BigInteger value, SelfUserIdentifierType type)
        {
            var hex = value.ToString("x").TrimStart('0');
            if (type == SelfUserIdentifierType.Hex) return "0x" + hex.PadLeft(40, '0');
            hex = hex.PadLeft(32, '0');
            return $"{hex[..8]}-{hex[8..12]}-{hex[12..16]}-{hex[16..20]}-{hex[20..]}";
        }

        internal static IReadOnlyList<string> UnpackForbiddenCountries(IEnumerable<BigInteger> packed)
        {
            var chars = packed.SelectMany(e => Enumerable.Range(0, 31).Select(i => (char)(byte)((e >> (8 * i)) & 0xFF))).Where(ch => ch != '\0').ToArray();
            var countries = new List<string>();
            for (var i = 0; i + 3 <= chars.Length; i += 3) countries.Add(new string(chars, i, 3));
            return countries;
        }

        private static SelfDisclosure FormatRevealedData(int attestationId, BigInteger[] signals, DiscloseIndices ix)
        {
            var bytes = new List<byte>();
            var counts = RevealedBytesPerSignal[attestationId];
            for (var i = 0; i < counts.Length; i++)
            {
                var signal = signals[ix.RevealedDataPacked + i];
                for (var j = 0; j < counts[i]; j++) { bytes.Add((byte)(signal & 0xFF)); signal >>= 8; }
            }
            var data = bytes.ToArray();
            var r = Revealed[attestationId];
            string Text((int Start, int End) range) => Encoding.UTF8.GetString(data, range.Start, range.End - range.Start + 1);
            var name = Regex.Replace(Text(r.Name), "([A-Z])<+([A-Z])", "$1 $2").Replace("<", "").Trim();
            return new SelfDisclosure
            {
                Nullifier = signals[ix.Nullifier].ToString(),
                IssuingState = Text(r.IssuingState),
                Name = name,
                IdNumber = Text(r.IdNumber),
                Nationality = Text(r.Nationality),
                DateOfBirth = Text(r.DateOfBirth),
                Gender = Text(r.Gender),
                ExpiryDate = Text(r.ExpiryDate),
                MinimumAge = Text(r.OlderThan),
                Ofac = data.Skip(r.Ofac.Start).Take(r.Ofac.End - r.Ofac.Start + 1).Select(b => b != 0).ToArray()
            };
        }

        private static DateTimeOffset? CircuitDate(BigInteger[] signals, int start)
        {
            var d = Enumerable.Range(0, 6).Select(i => signals[start + i]).ToArray();
            if (d.Any(x => x < 0 || x > 9)) return null;
            int year = 2000 + (int)(d[0] * 10 + d[1]), month = (int)(d[2] * 10 + d[3]), day = (int)(d[4] * 10 + d[5]);
            if (month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month)) return null;
            return new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);
        }

        /// <summary>Signals are decimal strings; the SDK treats any containing a-f as hex.</summary>
        private static BigInteger ParseSignal(string s)
        {
            if (string.IsNullOrEmpty(s)) throw new FormatException("empty signal");
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) s = s[2..];
            else if (!Regex.IsMatch(s, "[a-f]")) return BigInteger.Parse(s, NumberStyles.None, CultureInfo.InvariantCulture);
            return BigInteger.Parse("0" + s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture);
        }

        private static bool IsZeroAddress(string address) => string.IsNullOrEmpty(address) || new BigInteger(Convert.FromHexString(address[2..]), true, true).IsZero;

        private static string RpcMessage(Exception ex) => ex is RpcResponseException rpc ? rpc.RpcError?.Message ?? rpc.Message : ex.Message;

        private static OASISResult<T> Error<T>(string message, Exception ex = null)
        {
            var result = new OASISResult<T>();
            OASISErrorHandling.HandleError(ref result, $"SelfProtocolOASIS: {message}", ex);
            return result;
        }

        private sealed record DiscloseIndices(int RevealedDataPacked, int ForbiddenCountriesPacked, int Nullifier, int AttestationId, int MerkleRoot, int CurrentDate, int Scope, int UserIdentifier);

        private sealed record RevealedIndices((int Start, int End) IssuingState, (int Start, int End) Name, (int Start, int End) IdNumber,
            (int Start, int End) Nationality, (int Start, int End) DateOfBirth, (int Start, int End) Gender, (int Start, int End) ExpiryDate,
            (int Start, int End) OlderThan, (int Start, int End) Ofac);
    }
}
