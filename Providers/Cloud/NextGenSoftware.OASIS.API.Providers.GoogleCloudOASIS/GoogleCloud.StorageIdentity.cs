using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using Grpc.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS;

public partial class GoogleCloudOASIS : ICosmicStorageIdentityProvider
{
    private static DocumentReference FirestoreStorageIdentityDocument(FirestoreDb database)
        => database.Collection("cosmicStorageIdentity").Document("store");

    public async Task<OASISResult<CosmicStorageIdentity>> CaptureStorageIdentityAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var database = _firestoreDb;
        if (!IsProviderActivated || !_enableFirestore || database == null)
            return new() { IsError = true, ErrorCode = "COSMIC_STORAGE_IDENTITY_NOT_READY",
                Message = "Activate Firestore before capturing its routing identity." };

        try
        {
            var reference = FirestoreStorageIdentityDocument(database);
            var snapshot = await reference.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
            if (!snapshot.Exists)
            {
                RequireSingleAttemptFirestoreCommit(database);
                var id = Guid.NewGuid();
                try
                {
                    RequireFirestoreAcknowledgement(await reference.CreateAsync(
                        new Dictionary<string, object> { ["storeId"] = id.ToString("D") },
                        cancellationToken).ConfigureAwait(false));
                    return new(CreateFirestoreStorageIdentity(database, id));
                }
                catch (RpcException exception) when (exception.StatusCode == StatusCode.AlreadyExists)
                {
                    // A concurrent initializer created the immutable identity document.
                    snapshot = await reference.GetSnapshotAsync(cancellationToken).ConfigureAwait(false);
                }
            }

            return new(ReadFirestoreStorageIdentity(snapshot));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new() { IsError = true, ErrorCount = 1, ErrorCode = "COSMIC_STORAGE_IDENTITY_CAPTURE_FAILED",
                Message = "The native Firestore storage identity could not be captured.", Exception = exception };
        }
    }

    private static CosmicStorageIdentity ReadFirestoreStorageIdentity(DocumentSnapshot snapshot)
    {
        if (!snapshot.Exists || !snapshot.TryGetValue<string>("storeId", out var value))
            throw new InvalidOperationException("The Firestore storage identity document is absent or invalid.");

        return ParseFirestoreStorageIdentity(snapshot.Reference.Database, value);
    }

    private static CosmicStorageIdentity ParseFirestoreStorageIdentity(FirestoreDb database, string value)
    {
        if (!Guid.TryParseExact(value, "D", out var id) || id == Guid.Empty || id.ToString("D") != value)
            throw new InvalidOperationException("The Firestore storage identity is invalid.");
        return CreateFirestoreStorageIdentity(database, id);
    }

    private static CosmicStorageIdentity CreateFirestoreStorageIdentity(FirestoreDb database, Guid id)
    {
        var resource = $"firestore:projects/{database.ProjectId}/databases/{database.DatabaseId}";
        using var digest = SHA256.Create();
        var fingerprint = BitConverter.ToString(digest.ComputeHash(Encoding.UTF8.GetBytes(resource))).Replace("-", "");
        return new(Core.Enums.ProviderType.GoogleCloudOASIS, fingerprint, id);
    }
}
