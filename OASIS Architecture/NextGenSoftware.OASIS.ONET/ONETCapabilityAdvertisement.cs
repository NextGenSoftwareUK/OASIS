using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.ONET
{
    public enum ONETNodeProfile { Full = 0, Edge = 1 }

    public sealed class ONETProviderCapability
    {
        public string ProviderType { get; set; } = string.Empty;
        public string ProviderCategory { get; set; } = string.Empty;
        public IReadOnlyList<string> Capabilities { get; set; } = Array.Empty<string>();
    }

    public sealed class ONETCapabilityAdvertisement
    {
        public const int CurrentProtocolVersion = 1;
        public int ProtocolVersion { get; set; } = CurrentProtocolVersion;
        public string NodeId { get; set; } = string.Empty;
        public ONETNodeProfile NodeProfile { get; set; }
        public DateTime IssuedUtc { get; set; }
        public DateTime ExpiresUtc { get; set; }
        public string Nonce { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public IReadOnlyList<string> Services { get; set; } = Array.Empty<string>();
        public IReadOnlyList<ONETProviderCapability> Providers { get; set; } = Array.Empty<ONETProviderCapability>();
        public string Signature { get; set; } = string.Empty;
    }

    public static class ONETCapabilityProof
    {
        public const string Protocol = "OASIS_ONET_CAPABILITIES_V1";

        public static async Task<OASISResult<ONETCapabilityAdvertisement>> CreateAsync(string nodeId,
            string publicKey, ONETNodeProfile profile, IEnumerable<string> services,
            IEnumerable<ONETProviderCapability> providers, DateTime issuedUtc, TimeSpan lifetime,
            Func<string, CancellationToken, Task<OASISResult<string>>> signAsync,
            CancellationToken cancellationToken)
        {
            var result = new OASISResult<ONETCapabilityAdvertisement>();
            if (signAsync == null) return Failure(result, "ONET_CAPABILITY_SIGNER_REQUIRED", "A node signer is required.");
            if (lifetime <= TimeSpan.Zero) return Failure(result, "ONET_CAPABILITY_LIFETIME_INVALID", "The advertisement lifetime must be greater than zero.");
            var advertisement = new ONETCapabilityAdvertisement
            {
                NodeId = nodeId ?? string.Empty,
                PublicKey = publicKey ?? string.Empty,
                NodeProfile = profile,
                IssuedUtc = issuedUtc.ToUniversalTime(),
                ExpiresUtc = issuedUtc.ToUniversalTime().Add(lifetime),
                Nonce = Guid.NewGuid().ToString("N"),
                Services = Normalize(services),
                Providers = Normalize(providers)
            };
            if (!HasValidIdentity(advertisement))
                return Failure(result, "ONET_CAPABILITY_IDENTITY_INVALID", "The node id must fingerprint a valid base64 public key.");
            var signed = await signAsync(BuildMessage(advertisement), cancellationToken).ConfigureAwait(false);
            if (signed == null || signed.IsError || string.IsNullOrWhiteSpace(signed.Result))
                return Failure(result, signed?.ErrorCode ?? "ONET_CAPABILITY_SIGN_FAILED",
                    signed?.Message ?? "The node identity did not sign the capability advertisement.");
            advertisement.Signature = signed.Result;
            result.Result = advertisement;
            return result;
        }

        public static bool Verify(ONETCapabilityAdvertisement advertisement, DateTime utcNow)
        {
            if (advertisement == null || advertisement.ProtocolVersion != ONETCapabilityAdvertisement.CurrentProtocolVersion ||
                advertisement.IssuedUtc.Kind != DateTimeKind.Utc || advertisement.ExpiresUtc.Kind != DateTimeKind.Utc ||
                advertisement.ExpiresUtc <= advertisement.IssuedUtc || utcNow.ToUniversalTime() >= advertisement.ExpiresUtc ||
                advertisement.IssuedUtc > utcNow.ToUniversalTime().AddMinutes(5) || string.IsNullOrWhiteSpace(advertisement.Nonce) ||
                string.IsNullOrWhiteSpace(advertisement.Signature) || !HasValidIdentity(advertisement)) return false;
            try
            {
                using (var ecdsa = ECDsa.Create())
                {
                    ecdsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(advertisement.PublicKey), out _);
                    return ecdsa.VerifyData(Encoding.UTF8.GetBytes(BuildMessage(advertisement)),
                        Convert.FromBase64String(advertisement.Signature), HashAlgorithmName.SHA256);
                }
            }
            catch (FormatException) { return false; }
            catch (CryptographicException) { return false; }
            catch (ArgumentException) { return false; }
        }

        public static string BuildMessage(ONETCapabilityAdvertisement advertisement)
        {
            if (advertisement == null) throw new ArgumentNullException(nameof(advertisement));
            var services = Normalize(advertisement.Services);
            var providers = Normalize(advertisement.Providers);
            string providerText = string.Join(",", providers.Select(provider =>
                Encode(provider.ProviderType) + ":" + Encode(provider.ProviderCategory) + ":" +
                string.Join(".", Normalize(provider.Capabilities).Select(Encode))));
            return string.Join("|", Protocol, advertisement.ProtocolVersion.ToString(), Encode(advertisement.NodeId),
                ((int)advertisement.NodeProfile).ToString(), advertisement.IssuedUtc.ToUniversalTime().Ticks.ToString(),
                advertisement.ExpiresUtc.ToUniversalTime().Ticks.ToString(), Encode(advertisement.Nonce),
                Encode(advertisement.PublicKey), string.Join(",", services.Select(Encode)), providerText);
        }

        private static bool HasValidIdentity(ONETCapabilityAdvertisement advertisement)
        {
            try
            {
                byte[] key = Convert.FromBase64String(advertisement.PublicKey);
                using (var ecdsa = ECDsa.Create()) ecdsa.ImportSubjectPublicKeyInfo(key, out _);
                string fingerprint;
                using (var sha = SHA256.Create())
                    fingerprint = BitConverter.ToString(sha.ComputeHash(key)).Replace("-", string.Empty).ToLowerInvariant();
                return string.Equals(fingerprint, advertisement.NodeId, StringComparison.Ordinal);
            }
            catch (FormatException) { return false; }
            catch (CryptographicException) { return false; }
        }

        private static IReadOnlyList<string> Normalize(IEnumerable<string> values) =>
            (values ?? Array.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim()).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        private static IReadOnlyList<ONETProviderCapability> Normalize(IEnumerable<ONETProviderCapability> providers) =>
            (providers ?? Array.Empty<ONETProviderCapability>()).Where(x => x != null && !string.IsNullOrWhiteSpace(x.ProviderType))
                .Select(x => new ONETProviderCapability
                {
                    ProviderType = x.ProviderType.Trim(), ProviderCategory = x.ProviderCategory?.Trim() ?? string.Empty,
                    Capabilities = Normalize(x.Capabilities)
                }).GroupBy(x => x.ProviderType, StringComparer.Ordinal).Select(x => x.First())
                .OrderBy(x => x.ProviderType, StringComparer.Ordinal).ToArray();

        private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        private static OASISResult<ONETCapabilityAdvertisement> Failure(OASISResult<ONETCapabilityAdvertisement> result,
            string code, string message)
        { result.IsError = true; result.ErrorCount = 1; result.ErrorCode = code; result.Message = message; return result; }
    }

    public interface IONETCapabilityPublisher
    {
        Task<OASISResult<bool>> PublishAsync(ONETCapabilityAdvertisement advertisement,
            CancellationToken cancellationToken);
    }

    public sealed class ONETCapabilityPublisher : IONETCapabilityPublisher
    {
        public const string OperationName = "oasis.onet.capabilities.publish.v1";
        private readonly ONETRequestResponseEndpoint _endpoint;
        private readonly string _registryNodeId;
        private readonly JsonSerializerOptions _jsonOptions;

        public ONETCapabilityPublisher(ONETRequestResponseEndpoint endpoint, string registryNodeId,
            JsonSerializerOptions jsonOptions = null)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _registryNodeId = !string.IsNullOrWhiteSpace(registryNodeId) ? registryNodeId :
                throw new ArgumentException("A capability registry node is required.", nameof(registryNodeId));
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
        }

        public async Task<OASISResult<bool>> PublishAsync(ONETCapabilityAdvertisement advertisement,
            CancellationToken cancellationToken)
        {
            var result = new OASISResult<bool>();
            if (advertisement == null) return Failure(result, "ONET_CAPABILITY_REQUIRED", "A capability advertisement is required.");
            var response = await _endpoint.RequestAsync(_registryNodeId, OperationName,
                JsonSerializer.Serialize(advertisement, _jsonOptions), cancellationToken).ConfigureAwait(false);
            if (response == null || response.IsError)
                return Failure(result, response?.ErrorCode ?? "ONET_CAPABILITY_PUBLISH_FAILED",
                    response?.Message ?? "The capability registry returned no result.");
            if (!bool.TryParse(response.Result, out bool accepted) || !accepted)
                return Failure(result, "ONET_CAPABILITY_REJECTED", "The capability registry rejected the advertisement.");
            result.Result = true;
            return result;
        }

        private static OASISResult<bool> Failure(OASISResult<bool> result, string code, string message)
        { result.IsError = true; result.ErrorCount = 1; result.ErrorCode = code; result.Message = message; return result; }
    }

    public sealed class ONETFederatedCapabilityPublisher : IONETCapabilityPublisher
    {
        private readonly IReadOnlyList<IONETCapabilityPublisher> _publishers;
        private readonly int _minimumAcknowledgements;

        public ONETFederatedCapabilityPublisher(IEnumerable<IONETCapabilityPublisher> publishers,
            int minimumAcknowledgements)
        {
            _publishers = (publishers ?? throw new ArgumentNullException(nameof(publishers))).ToArray();
            if (_publishers.Count == 0 || _publishers.Any(x => x == null))
                throw new ArgumentException("At least one non-null capability publisher is required.", nameof(publishers));
            if (minimumAcknowledgements < 1 || minimumAcknowledgements > _publishers.Count)
                throw new ArgumentOutOfRangeException(nameof(minimumAcknowledgements));
            _minimumAcknowledgements = minimumAcknowledgements;
        }

        public async Task<OASISResult<bool>> PublishAsync(ONETCapabilityAdvertisement advertisement,
            CancellationToken cancellationToken)
        {
            if (advertisement == null)
                return Failure("ONET_CAPABILITY_REQUIRED", "A capability advertisement is required.");
            var responses = await Task.WhenAll(_publishers.Select(x => PublishSafelyAsync(x, advertisement, cancellationToken)))
                .ConfigureAwait(false);
            int acknowledged = responses.Count(x => x != null && !x.IsError && x.Result);
            if (acknowledged < _minimumAcknowledgements)
                return Failure("ONET_CAPABILITY_PUBLISH_QUORUM_FAILED",
                    $"Only {acknowledged} of {_publishers.Count} capability registries accepted the lease; " +
                    $"{_minimumAcknowledgements} are required.");
            int failed = responses.Length - acknowledged;
            var result = new OASISResult<bool> { Result = true };
            if (failed > 0)
            {
                result.IsWarning = true;
                result.WarningCount = failed;
                result.Message = $"{failed} capability registry publication(s) failed; the configured quorum was satisfied.";
                result.InnerMessages.AddRange(responses.Where(x => x == null || x.IsError || !x.Result)
                    .Select(x => x?.Message ?? "A capability registry returned no result."));
            }
            return result;
        }

        private static OASISResult<bool> Failure(string code, string message) => new OASISResult<bool>
        { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };

        private static async Task<OASISResult<bool>> PublishSafelyAsync(IONETCapabilityPublisher publisher,
            ONETCapabilityAdvertisement advertisement, CancellationToken cancellationToken)
        {
            try { return await publisher.PublishAsync(advertisement, cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex) { return Failure("ONET_CAPABILITY_PUBLISH_EXCEPTION", ex.Message); }
        }
    }

    public sealed class ONETCapabilityQuery
    {
        public string Service { get; set; } = string.Empty;
        public string ProviderType { get; set; } = string.Empty;
        public string Capability { get; set; } = string.Empty;
        public ONETNodeProfile? NodeProfile { get; set; }
        public int MaximumResults { get; set; } = 50;
    }

    /// <summary>
    /// Reads signed capability leases from a registry. Every returned lease is verified locally, so a
    /// registry can distribute advertisements but cannot forge or extend a node's capabilities.
    /// </summary>
    public interface IONETCapabilityDirectory
    {
        Task<OASISResult<IReadOnlyList<ONETCapabilityAdvertisement>>> QueryAsync(
            ONETCapabilityQuery query, CancellationToken cancellationToken);
        Task<OASISResult<ONETCapabilityAdvertisement>> SelectNodeAsync(
            ONETCapabilityQuery query, CancellationToken cancellationToken);
    }

    public sealed class ONETCapabilityDirectoryClient : IONETCapabilityDirectory
    {
        public const string OperationName = "oasis.onet.capabilities.query.v1";
        private readonly ONETRequestResponseEndpoint _endpoint;
        private readonly string _registryNodeId;
        private readonly Func<DateTime> _utcNow;
        private readonly JsonSerializerOptions _jsonOptions;

        public ONETCapabilityDirectoryClient(ONETRequestResponseEndpoint endpoint, string registryNodeId,
            Func<DateTime> utcNow = null, JsonSerializerOptions jsonOptions = null)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _registryNodeId = !string.IsNullOrWhiteSpace(registryNodeId) ? registryNodeId :
                throw new ArgumentException("A capability registry node is required.", nameof(registryNodeId));
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
        }

        public async Task<OASISResult<IReadOnlyList<ONETCapabilityAdvertisement>>> QueryAsync(
            ONETCapabilityQuery query, CancellationToken cancellationToken)
        {
            var result = new OASISResult<IReadOnlyList<ONETCapabilityAdvertisement>>();
            if (query == null) return Failure(result, "ONET_CAPABILITY_QUERY_REQUIRED", "A capability query is required.");
            if (query.MaximumResults < 1 || query.MaximumResults > 200)
                return Failure(result, "ONET_CAPABILITY_QUERY_LIMIT_INVALID", "MaximumResults must be between 1 and 200.");
            var response = await _endpoint.RequestAsync(_registryNodeId, OperationName,
                JsonSerializer.Serialize(query, _jsonOptions), cancellationToken).ConfigureAwait(false);
            if (response == null || response.IsError)
                return Failure(result, response?.ErrorCode ?? "ONET_CAPABILITY_QUERY_FAILED",
                    response?.Message ?? "The capability registry returned no result.");
            IReadOnlyList<ONETCapabilityAdvertisement> advertisements;
            try
            {
                advertisements = JsonSerializer.Deserialize<ONETCapabilityAdvertisement[]>(response.Result, _jsonOptions) ??
                    Array.Empty<ONETCapabilityAdvertisement>();
            }
            catch (JsonException)
            {
                return Failure(result, "ONET_CAPABILITY_DIRECTORY_INVALID", "The capability registry returned invalid JSON.");
            }
            if (advertisements.Any(x => !ONETCapabilityProof.Verify(x, _utcNow())))
                return Failure(result, "ONET_CAPABILITY_DIRECTORY_UNVERIFIED",
                    "The capability registry returned an invalid or expired signed advertisement.");
            result.Result = advertisements.OrderByDescending(x => x.IssuedUtc)
                .ThenBy(x => x.NodeId, StringComparer.Ordinal).ToArray();
            return result;
        }

        public async Task<OASISResult<ONETCapabilityAdvertisement>> SelectNodeAsync(ONETCapabilityQuery query,
            CancellationToken cancellationToken)
        {
            var queried = await QueryAsync(query, cancellationToken).ConfigureAwait(false);
            if (queried == null || queried.IsError)
                return Failure(new OASISResult<ONETCapabilityAdvertisement>(),
                    queried?.ErrorCode ?? "ONET_CAPABILITY_QUERY_FAILED", queried?.Message ?? "Capability lookup failed.");
            var selected = queried.Result.FirstOrDefault();
            if (selected == null)
                return Failure(new OASISResult<ONETCapabilityAdvertisement>(), "ONET_CAPABILITY_ROUTE_NOT_FOUND",
                    "No current ONET node advertises the requested capability.");
            return new OASISResult<ONETCapabilityAdvertisement>(selected);
        }

        private static OASISResult<T> Failure<T>(OASISResult<T> result, string code, string message)
        { result.IsError = true; result.ErrorCount = 1; result.ErrorCode = code; result.Message = message; return result; }
    }

    /// <summary>
    /// Reconciles independently signed leases returned by multiple registries. Registry availability is
    /// governed by an explicit quorum; node signatures remain the authority for capability contents.
    /// </summary>
    public sealed class ONETFederatedCapabilityDirectory : IONETCapabilityDirectory
    {
        private readonly IReadOnlyList<IONETCapabilityDirectory> _directories;
        private readonly int _minimumSuccessfulRegistries;

        public ONETFederatedCapabilityDirectory(IEnumerable<IONETCapabilityDirectory> directories,
            int minimumSuccessfulRegistries)
        {
            _directories = (directories ?? throw new ArgumentNullException(nameof(directories))).ToArray();
            if (_directories.Count == 0 || _directories.Any(x => x == null))
                throw new ArgumentException("At least one non-null capability directory is required.", nameof(directories));
            if (minimumSuccessfulRegistries < 1 || minimumSuccessfulRegistries > _directories.Count)
                throw new ArgumentOutOfRangeException(nameof(minimumSuccessfulRegistries));
            _minimumSuccessfulRegistries = minimumSuccessfulRegistries;
        }

        public async Task<OASISResult<IReadOnlyList<ONETCapabilityAdvertisement>>> QueryAsync(
            ONETCapabilityQuery query, CancellationToken cancellationToken)
        {
            if (query == null)
                return Failure<IReadOnlyList<ONETCapabilityAdvertisement>>("ONET_CAPABILITY_QUERY_REQUIRED",
                    "A capability query is required.");
            var responses = await Task.WhenAll(_directories.Select(x => QuerySafelyAsync(x, query, cancellationToken)))
                .ConfigureAwait(false);
            var successful = responses.Where(x => x != null && !x.IsError && x.Result != null).ToArray();
            if (successful.Length < _minimumSuccessfulRegistries)
                return Failure<IReadOnlyList<ONETCapabilityAdvertisement>>("ONET_CAPABILITY_REGISTRY_QUORUM_FAILED",
                    $"Only {successful.Length} of {_directories.Count} capability registries responded successfully; " +
                    $"{_minimumSuccessfulRegistries} are required.");

            var reconciled = new List<ONETCapabilityAdvertisement>();
            foreach (var nodeGroup in successful.SelectMany(x => x.Result).GroupBy(x => x.NodeId, StringComparer.Ordinal))
            {
                var newestTime = nodeGroup.Max(x => x.IssuedUtc);
                var newest = nodeGroup.Where(x => x.IssuedUtc == newestTime).ToArray();
                if (newest.Select(x => x.Signature).Distinct(StringComparer.Ordinal).Count() > 1)
                    return Failure<IReadOnlyList<ONETCapabilityAdvertisement>>("ONET_CAPABILITY_REGISTRY_EQUIVOCATION",
                        $"Registries returned conflicting signed leases for node '{nodeGroup.Key}' at the same issue time.");
                reconciled.Add(newest.OrderBy(x => x.Signature, StringComparer.Ordinal).First());
            }

            var result = new OASISResult<IReadOnlyList<ONETCapabilityAdvertisement>>
            {
                Result = reconciled.OrderByDescending(x => x.IssuedUtc)
                    .ThenBy(x => x.NodeId, StringComparer.Ordinal).Take(query.MaximumResults).ToArray()
            };
            int failed = responses.Length - successful.Length;
            if (failed > 0)
            {
                result.IsWarning = true;
                result.WarningCount = failed;
                result.Message = $"{failed} capability registry request(s) failed; the configured quorum was satisfied.";
                result.InnerMessages.AddRange(responses.Where(x => x == null || x.IsError)
                    .Select(x => x?.Message ?? "A capability registry returned no result."));
            }
            return result;
        }

        public async Task<OASISResult<ONETCapabilityAdvertisement>> SelectNodeAsync(ONETCapabilityQuery query,
            CancellationToken cancellationToken)
        {
            var queried = await QueryAsync(query, cancellationToken).ConfigureAwait(false);
            if (queried == null || queried.IsError)
                return Failure<ONETCapabilityAdvertisement>(queried?.ErrorCode ?? "ONET_CAPABILITY_QUERY_FAILED",
                    queried?.Message ?? "Capability lookup failed.");
            var selected = queried.Result.FirstOrDefault();
            if (selected == null)
                return Failure<ONETCapabilityAdvertisement>("ONET_CAPABILITY_ROUTE_NOT_FOUND",
                    "No current ONET node advertises the requested capability.");
            return new OASISResult<ONETCapabilityAdvertisement>
            {
                Result = selected, IsWarning = queried.IsWarning, WarningCount = queried.WarningCount,
                Message = queried.Message, InnerMessages = queried.InnerMessages
            };
        }

        private static OASISResult<T> Failure<T>(string code, string message) => new OASISResult<T>
        { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };

        private static async Task<OASISResult<IReadOnlyList<ONETCapabilityAdvertisement>>> QuerySafelyAsync(
            IONETCapabilityDirectory directory, ONETCapabilityQuery query, CancellationToken cancellationToken)
        {
            try { return await directory.QueryAsync(query, cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                return Failure<IReadOnlyList<ONETCapabilityAdvertisement>>(
                    "ONET_CAPABILITY_QUERY_EXCEPTION", ex.Message);
            }
        }
    }

    public sealed class ONETCapabilityRegistry
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, ONETCapabilityAdvertisement> _advertisements =
            new System.Collections.Concurrent.ConcurrentDictionary<string, ONETCapabilityAdvertisement>(StringComparer.Ordinal);
        private readonly object _updateGate = new object();
        private readonly Func<DateTime> _utcNow;
        private readonly TimeSpan _maximumLifetime;
        private readonly JsonSerializerOptions _jsonOptions;

        public ONETCapabilityRegistry(ONETRequestResponseEndpoint endpoint, Func<DateTime> utcNow = null,
            JsonSerializerOptions jsonOptions = null, TimeSpan? maximumLifetime = null)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            _utcNow = utcNow ?? (() => DateTime.UtcNow);
            _maximumLifetime = maximumLifetime ?? TimeSpan.FromMinutes(15);
            if (_maximumLifetime <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(maximumLifetime));
            _jsonOptions = jsonOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
            endpoint.RegisterHandler(ONETCapabilityPublisher.OperationName, AcceptAsync);
            endpoint.RegisterHandler(ONETCapabilityDirectoryClient.OperationName, QueryAsync);
        }

        public bool TryGetCurrent(string nodeId, out ONETCapabilityAdvertisement advertisement)
        {
            advertisement = null;
            if (string.IsNullOrWhiteSpace(nodeId) || !_advertisements.TryGetValue(nodeId, out var candidate)) return false;
            if (!ONETCapabilityProof.Verify(candidate, _utcNow()))
            {
                _advertisements.TryRemove(nodeId, out _);
                return false;
            }
            advertisement = candidate;
            return true;
        }

        public OASISResult<bool> RegisterLocal(ONETCapabilityAdvertisement advertisement, string authenticatedNodeId)
        {
            var stored = ValidateAndStore(advertisement, authenticatedNodeId);
            return stored.IsError
                ? new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = stored.ErrorCode, Message = stored.Message }
                : new OASISResult<bool>(true);
        }

        public OASISResult<bool> MergeSignedLease(ONETCapabilityAdvertisement advertisement)
        {
            var stored = ValidateAndStore(advertisement, advertisement?.NodeId);
            if (stored.IsError && stored.ErrorCode == "ONET_CAPABILITY_STALE")
                return new OASISResult<bool>(false);
            return stored.IsError
                ? new OASISResult<bool> { IsError = true, ErrorCount = 1, ErrorCode = stored.ErrorCode, Message = stored.Message }
                : new OASISResult<bool>(true);
        }

        private Task<OASISResult<string>> AcceptAsync(ONETRequestContext context, CancellationToken cancellationToken)
        {
            var result = new OASISResult<string>();
            ONETCapabilityAdvertisement advertisement;
            try { advertisement = JsonSerializer.Deserialize<ONETCapabilityAdvertisement>(context.PayloadJson, _jsonOptions); }
            catch (JsonException) { return Task.FromResult(Failure(result, "ONET_CAPABILITY_INVALID", "The capability advertisement was not valid JSON.")); }
            var stored = ValidateAndStore(advertisement, context.SourceNodeId);
            return Task.FromResult(stored);
        }

        private Task<OASISResult<string>> QueryAsync(ONETRequestContext context, CancellationToken cancellationToken)
        {
            var result = new OASISResult<string>();
            ONETCapabilityQuery query;
            try { query = JsonSerializer.Deserialize<ONETCapabilityQuery>(context.PayloadJson, _jsonOptions); }
            catch (JsonException) { return Task.FromResult(Failure(result, "ONET_CAPABILITY_QUERY_INVALID", "The capability query was not valid JSON.")); }
            if (query == null)
                return Task.FromResult(Failure(result, "ONET_CAPABILITY_QUERY_INVALID", "The capability query was empty."));
            if (query.MaximumResults < 1 || query.MaximumResults > 200)
                return Task.FromResult(Failure(result, "ONET_CAPABILITY_QUERY_LIMIT_INVALID", "MaximumResults must be between 1 and 200."));
            var now = _utcNow();
            foreach (var entry in _advertisements.ToArray())
                if (!ONETCapabilityProof.Verify(entry.Value, now)) _advertisements.TryRemove(entry.Key, out _);
            var matches = _advertisements.Values.Where(x => Matches(x, query))
                .OrderByDescending(x => x.IssuedUtc).ThenBy(x => x.NodeId, StringComparer.Ordinal)
                .Take(query.MaximumResults).ToArray();
            result.Result = JsonSerializer.Serialize(matches, _jsonOptions);
            return Task.FromResult(result);
        }

        private static bool Matches(ONETCapabilityAdvertisement advertisement, ONETCapabilityQuery query)
        {
            if (query.NodeProfile.HasValue && advertisement.NodeProfile != query.NodeProfile.Value) return false;
            if (!string.IsNullOrWhiteSpace(query.Service) &&
                !advertisement.Services.Contains(query.Service.Trim(), StringComparer.Ordinal)) return false;
            if (string.IsNullOrWhiteSpace(query.ProviderType) && string.IsNullOrWhiteSpace(query.Capability)) return true;
            return advertisement.Providers.Any(provider =>
                (string.IsNullOrWhiteSpace(query.ProviderType) ||
                    string.Equals(provider.ProviderType, query.ProviderType.Trim(), StringComparison.Ordinal)) &&
                (string.IsNullOrWhiteSpace(query.Capability) ||
                    provider.Capabilities.Contains(query.Capability.Trim(), StringComparer.Ordinal)));
        }

        private OASISResult<string> ValidateAndStore(ONETCapabilityAdvertisement advertisement, string authenticatedNodeId)
        {
            var result = new OASISResult<string>();
            if (advertisement == null || !string.Equals(advertisement.NodeId, authenticatedNodeId, StringComparison.Ordinal))
                return Failure(result, "ONET_CAPABILITY_IDENTITY_MISMATCH", "The advertisement node must match the authenticated ONET source.");
            if (!ONETCapabilityProof.Verify(advertisement, _utcNow()))
                return Failure(result, "ONET_CAPABILITY_SIGNATURE_INVALID", "The capability advertisement signature or lifetime is invalid.");
            if (advertisement.ExpiresUtc - advertisement.IssuedUtc > _maximumLifetime)
                return Failure(result, "ONET_CAPABILITY_LIFETIME_EXCEEDED", "The capability advertisement exceeds the registry lifetime policy.");
            lock (_updateGate)
            {
                if (_advertisements.TryGetValue(advertisement.NodeId, out var current))
                {
                    if (advertisement.IssuedUtc < current.IssuedUtc)
                        return Failure(result, "ONET_CAPABILITY_STALE", "A newer capability advertisement is already registered.");
                    if (advertisement.IssuedUtc == current.IssuedUtc &&
                        !string.Equals(advertisement.Signature, current.Signature, StringComparison.Ordinal))
                        return Failure(result, "ONET_CAPABILITY_EQUIVOCATION", "The node signed different capabilities for the same issue time.");
                }
                _advertisements[advertisement.NodeId] = advertisement;
            }
            result.Result = bool.TrueString;
            return result;
        }

        private static OASISResult<string> Failure(OASISResult<string> result, string code, string message)
        { result.IsError = true; result.ErrorCount = 1; result.ErrorCode = code; result.Message = message; return result; }
    }

    public sealed class ONETCapabilityReconciliationResult
    {
        public int RegistriesQueried { get; set; }
        public int RegistriesSucceeded { get; set; }
        public int LeasesImported { get; set; }
        public int StaleLeasesIgnored { get; set; }
    }

    /// <summary>Pulls signed leases from peer registries and merges them without trusting the registry as signer.</summary>
    public sealed class ONETCapabilityRegistryReconciler
    {
        private readonly ONETCapabilityRegistry _localRegistry;
        private readonly IReadOnlyList<IONETCapabilityDirectory> _peerDirectories;
        private readonly int _minimumSuccessfulRegistries;

        public ONETCapabilityRegistryReconciler(ONETCapabilityRegistry localRegistry,
            IEnumerable<IONETCapabilityDirectory> peerDirectories, int minimumSuccessfulRegistries)
        {
            _localRegistry = localRegistry ?? throw new ArgumentNullException(nameof(localRegistry));
            _peerDirectories = (peerDirectories ?? throw new ArgumentNullException(nameof(peerDirectories))).ToArray();
            if (_peerDirectories.Count == 0 || _peerDirectories.Any(x => x == null))
                throw new ArgumentException("At least one non-null peer registry directory is required.", nameof(peerDirectories));
            if (minimumSuccessfulRegistries < 1 || minimumSuccessfulRegistries > _peerDirectories.Count)
                throw new ArgumentOutOfRangeException(nameof(minimumSuccessfulRegistries));
            _minimumSuccessfulRegistries = minimumSuccessfulRegistries;
        }

        public async Task<OASISResult<ONETCapabilityReconciliationResult>> ReconcileAsync(
            CancellationToken cancellationToken)
        {
            var query = new ONETCapabilityQuery { MaximumResults = 200 };
            var responses = await Task.WhenAll(_peerDirectories.Select(x => QuerySafelyAsync(x, query, cancellationToken)))
                .ConfigureAwait(false);
            var successful = responses.Where(x => x != null && !x.IsError && x.Result != null).ToArray();
            if (successful.Length < _minimumSuccessfulRegistries)
                return Failure("ONET_CAPABILITY_GOSSIP_QUORUM_FAILED",
                    $"Only {successful.Length} of {_peerDirectories.Count} peer registries responded successfully; " +
                    $"{_minimumSuccessfulRegistries} are required.");
            var summary = new ONETCapabilityReconciliationResult
            { RegistriesQueried = responses.Length, RegistriesSucceeded = successful.Length };
            foreach (var lease in successful.SelectMany(x => x.Result)
                .OrderBy(x => x.IssuedUtc).ThenBy(x => x.NodeId, StringComparer.Ordinal))
            {
                var merged = _localRegistry.MergeSignedLease(lease);
                if (merged.IsError)
                    return Failure(merged.ErrorCode ?? "ONET_CAPABILITY_GOSSIP_MERGE_FAILED", merged.Message);
                if (merged.Result) summary.LeasesImported++;
                else summary.StaleLeasesIgnored++;
            }
            var result = new OASISResult<ONETCapabilityReconciliationResult>(summary);
            int failed = responses.Length - successful.Length;
            if (failed > 0)
            {
                result.IsWarning = true;
                result.WarningCount = failed;
                result.Message = $"{failed} peer registry reconciliation request(s) failed; the configured quorum was satisfied.";
                result.InnerMessages.AddRange(responses.Where(x => x == null || x.IsError)
                    .Select(x => x?.Message ?? "A peer capability registry returned no result."));
            }
            return result;
        }

        private static OASISResult<ONETCapabilityReconciliationResult> Failure(string code, string message) =>
            new OASISResult<ONETCapabilityReconciliationResult>
            { IsError = true, ErrorCount = 1, ErrorCode = code, Message = message };

        private static async Task<OASISResult<IReadOnlyList<ONETCapabilityAdvertisement>>> QuerySafelyAsync(
            IONETCapabilityDirectory directory, ONETCapabilityQuery query, CancellationToken cancellationToken)
        {
            try { return await directory.QueryAsync(query, cancellationToken).ConfigureAwait(false); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                return new OASISResult<IReadOnlyList<ONETCapabilityAdvertisement>>
                { IsError = true, ErrorCount = 1, ErrorCode = "ONET_CAPABILITY_GOSSIP_EXCEPTION", Message = ex.Message };
            }
        }
    }
}
