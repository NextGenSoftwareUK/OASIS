using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.MinIOOASIS.UnitTests;

[TestClass]
public class MinIoFailureBoundaryTests
{
    [DataTestMethod]
    [DataRow("NoSuchBucket", false)]
    [DataRow("NoSuchBucket", true)]
    [DataRow("AccessDenied", false)]
    [DataRow("AccessDenied", true)]
    public async Task Service404IsPreservedRatherThanTreatedAsMissingRecord(string code, bool synchronous)
    {
        using var client = new ResponseClient { ReadError = Failure(code, HttpStatusCode.NotFound) };
        var provider = new MinIOOASIS(client);
        var result = synchronous ? provider.LoadAvatar(Guid.NewGuid()) : await provider.LoadAvatarAsync(Guid.NewGuid());
        Assert.IsTrue(result.IsError);
        Assert.AreSame(client.ReadError, result.Exception);
    }

    [DataTestMethod, DataRow(false, false), DataRow(true, false), DataRow(false, true), DataRow(true, true)]
    public async Task FailedReadCannotAcknowledgeSoftDeletion(bool synchronous, bool holon)
    {
        using var client = new ResponseClient { ReadError = Failure("InternalError", HttpStatusCode.InternalServerError) };
        var provider = new MinIOOASIS(client);
        var id = Guid.NewGuid();
        var result = holon
            ? (synchronous ? provider.DeleteHolonSoft(id) : await provider.DeleteHolonSoftAsync(id))
            : (synchronous ? provider.DeleteAvatar(id) : await provider.DeleteAvatarAsync(id));
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.Result);
        Assert.AreSame(client.ReadError, result.Exception);
        Assert.AreEqual(0, client.PutCalls);
    }

    [DataTestMethod, DataRow(false, false), DataRow(true, false), DataRow(false, true), DataRow(true, true)]
    public async Task FailedWriteCannotAcknowledgeSoftDeletion(bool synchronous, bool holon)
    {
        using var client = new ResponseClient();
        var provider = new MinIOOASIS(client);
        var id = Guid.NewGuid();
        var result = holon
            ? (synchronous ? provider.DeleteHolonSoft(id) : await provider.DeleteHolonSoftAsync(id))
            : (synchronous ? provider.DeleteAvatar(id) : await provider.DeleteAvatarAsync(id));
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.Result);
        Assert.AreSame(client.WriteError, result.Exception);
        Assert.AreEqual(1, client.PutCalls);
    }

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task OnlyMissingKeyIsRecordAbsence(bool holon)
    {
        using var client = new ResponseClient { ReadError = Failure("NoSuchKey", HttpStatusCode.NotFound) };
        var provider = new MinIOOASIS(client);
        var result = holon ? await provider.DeleteHolonSoftAsync(Guid.NewGuid()) : await provider.DeleteAvatarAsync(Guid.NewGuid());
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.Result);
        Assert.IsNull(result.Exception);
        Assert.AreEqual(0, client.PutCalls);
    }

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task ConfirmedSoftDeletionAcknowledgesExactlyOneRecord(bool holon)
    {
        using var client = new ResponseClient { WriteError = null };
        var provider = new MinIOOASIS(client);
        var result = holon ? await provider.DeleteHolonSoftAsync(Guid.NewGuid()) : await provider.DeleteAvatarAsync(Guid.NewGuid());
        Assert.IsFalse(result.IsError, result.Message);
        Assert.IsTrue(result.Result);
        Assert.IsTrue(result.IsDeleted);
        Assert.AreEqual(1, result.DeletedCount);
        Assert.AreEqual(holon ? 1 : 2, client.PutCalls); // avatar record and synthetic username index
    }

    [DataTestMethod]
    [DataRow(false, false, false), DataRow(true, false, false), DataRow(false, true, false), DataRow(true, true, false)]
    [DataRow(false, false, true), DataRow(true, false, true), DataRow(false, true, true), DataRow(true, true, true)]
    public async Task PublicHolonDeleteRetainsReadOrWriteFailure(bool synchronous, bool providerKey, bool readFailure)
    {
        using var client = new ResponseClient
        { ReadError = readFailure ? Failure("InternalError", HttpStatusCode.InternalServerError) : null };
        var provider = new MinIOOASIS(client);
        var id = Guid.NewGuid();
        var result = providerKey
            ? (synchronous ? provider.DeleteHolon(id.ToString()) : await provider.DeleteHolonAsync(id.ToString()))
            : (synchronous ? provider.DeleteHolon(id) : await provider.DeleteHolonAsync(id));
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        Assert.AreSame(readFailure ? client.ReadError : client.WriteError, result.Exception);
    }

    private static AmazonS3Exception Failure(string code, HttpStatusCode status)
        => new(code) { ErrorCode = code, StatusCode = status };

    private sealed class ResponseClient : AmazonS3Client
    {
        public AmazonS3Exception? ReadError { get; init; }
        public AmazonS3Exception? WriteError { get; init; } = Failure("AccessDenied", HttpStatusCode.Forbidden);
        public int PutCalls { get; private set; }

        public ResponseClient() : base(new AnonymousAWSCredentials(), new AmazonS3Config
            { ServiceURL = "http://127.0.0.1:1", ForcePathStyle = true, MaxErrorRetry = 0 }) { }

        public override Task<GetObjectResponse> GetObjectAsync(GetObjectRequest request, CancellationToken cancellationToken = default)
            => ReadError != null ? Task.FromException<GetObjectResponse>(ReadError) : Task.FromResult(new GetObjectResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                ResponseStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(
                    "{\"Id\":\"09e7a7dc-1be1-4aab-97f6-6386337c179a\",\"Username\":\"synthetic\"}"))
            });

        public override Task<PutObjectResponse> PutObjectAsync(PutObjectRequest request, CancellationToken cancellationToken = default)
        {
            PutCalls++;
            return WriteError != null ? Task.FromException<PutObjectResponse>(WriteError)
                : Task.FromResult(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });
        }
    }
}
