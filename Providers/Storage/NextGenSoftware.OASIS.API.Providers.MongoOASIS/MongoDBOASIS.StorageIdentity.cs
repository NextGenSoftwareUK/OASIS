using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;

public partial class MongoDBOASIS : ICosmicStorageIdentityProvider
{
    public async Task<OASISResult<CosmicStorageIdentity>> CaptureStorageIdentityAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var repository = _holonRepository;
        if (!IsProviderActivated || repository == null)
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_STORAGE_IDENTITY_NOT_READY",
                Message = "Activate the existing Mongo store before capturing its binding." };
        try
        {
            return new(await repository.CaptureStorageIdentityAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (Exception exception) when (exception is MongoException || exception is InvalidDataException ||
            exception is InvalidOperationException || exception is ArgumentException || exception is FormatException)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_STORAGE_IDENTITY_CAPTURE_FAILED",
                Message = "The native Mongo storage identity could not be captured; preserve the original store.", Exception = exception };
        }
    }
}
