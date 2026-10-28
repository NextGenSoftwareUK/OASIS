using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS : ICosmicStorageIdentityProvider
{
    public async Task<OASISResult<CosmicStorageIdentity>> CaptureStorageIdentityAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!IsProviderActivated || _driver == null)
                return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_STORAGE_IDENTITY_NOT_READY",
                    Message = "Activate the existing Neo store before capturing its binding." };
            if (_storageDriverEndpoint == null)
                return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_STORAGE_IDENTITY_UNSUPPORTED",
                    Message = "The injected native driver has no captured endpoint binding; mutable Host is not proof of its endpoint." };
            using var session = _driver.AsyncSession(options => options.WithDefaultAccessMode(AccessMode.Read));
            var cursor = await session.RunAsync("CALL db.info() YIELD id,name RETURN id,name").ConfigureAwait(false);
            var rows = await cursor.ToListAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            if (rows.Count != 1 || rows[0]["id"] is not string identity || rows[0]["name"] is not string name || string.IsNullOrWhiteSpace(name))
                throw new InvalidDataException("The native database identity metadata is missing or malformed.");
            var fields = new[] { "neo-storage-endpoint-v1", _storageDriverEndpoint.AbsoluteUri, name };
            var canonical = string.Concat(fields.Select(value => Encoding.UTF8.GetByteCount(value).ToString(CultureInfo.InvariantCulture) + ":" + value));
            using var hash = SHA256.Create();
            var endpoint = BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(canonical))).Replace("-", "");
            return new(new CosmicStorageIdentity(API.Core.Enums.ProviderType.Neo4jOASIS, endpoint, identity));
        }
        catch (Exception exception) when (exception is Neo4jException || exception is InvalidDataException ||
            exception is InvalidOperationException || exception is ArgumentException || exception is FormatException)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_STORAGE_IDENTITY_CAPTURE_FAILED",
                Message = "The native Neo storage identity could not be captured; no identity record was created.", Exception = exception };
        }
        finally { _lifecycleGate.Release(); }
    }
}
