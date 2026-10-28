using System.Net;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.TestSupport;
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
        using var client = new ControlledS3Client { ReadError = Failure(code, HttpStatusCode.NotFound) };
        using var provider = Create(client);
        var result = synchronous ? provider.LoadAvatar(Guid.NewGuid()) : await provider.LoadAvatarAsync(Guid.NewGuid());
        Assert.IsTrue(result.IsError);
        Assert.AreSame(client.ReadError, result.Exception);
    }

    [DataTestMethod, DataRow(false, false), DataRow(true, false), DataRow(false, true), DataRow(true, true)]
    public async Task FailedReadCannotAcknowledgeSoftDeletion(bool synchronous, bool holon)
    {
        using var client = new ControlledS3Client { ReadError = Failure("InternalError", HttpStatusCode.InternalServerError) };
        using var provider = Create(client);
        var id = client.ReadError == null ? await Seed(provider, holon) : Guid.NewGuid();
        var before = client.PutCalls;
        if (client.ReadError == null) client.WriteError = Failure("AccessDenied", HttpStatusCode.Forbidden);
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
        using var client = new ControlledS3Client();
        using var provider = Create(client);
        var id = client.ReadError == null ? await Seed(provider, holon) : Guid.NewGuid();
        var before = client.PutCalls;
        if (client.ReadError == null) client.WriteError = Failure("AccessDenied", HttpStatusCode.Forbidden);
        var result = holon
            ? (synchronous ? provider.DeleteHolonSoft(id) : await provider.DeleteHolonSoftAsync(id))
            : (synchronous ? provider.DeleteAvatar(id) : await provider.DeleteAvatarAsync(id));
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.Result);
        Assert.AreSame(client.WriteError, result.Exception);
        Assert.AreEqual(before + 1, client.PutCalls);
    }

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task OnlyMissingKeyIsRecordAbsence(bool holon)
    {
        using var client = new ControlledS3Client { ReadError = Failure("NoSuchKey", HttpStatusCode.NotFound) };
        using var provider = Create(client);
        var result = holon ? await provider.DeleteHolonSoftAsync(Guid.NewGuid()) : await provider.DeleteAvatarAsync(Guid.NewGuid());
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(result.Result);
        Assert.IsNull(result.Exception);
        Assert.AreEqual(0, client.PutCalls);
    }

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task ConfirmedSoftDeletionAcknowledgesExactlyOneRecord(bool holon)
    {
        using var client = new ControlledS3Client { WriteError = null };
        using var provider = Create(client);
        var id = await Seed(provider, holon);
        var before = client.PutCalls;
        var result = holon ? await provider.DeleteHolonSoftAsync(id) : await provider.DeleteAvatarAsync(id);
        Assert.IsFalse(result.IsError, result.Message);
        Assert.IsTrue(result.Result);
        Assert.IsTrue(result.IsDeleted);
        Assert.AreEqual(1, result.DeletedCount);
        Assert.IsTrue(client.PutCalls > before);
    }

    [DataTestMethod]
    [DataRow(false, false, false), DataRow(true, false, false), DataRow(false, true, false), DataRow(true, true, false)]
    [DataRow(false, false, true), DataRow(true, false, true), DataRow(false, true, true), DataRow(true, true, true)]
    public async Task PublicHolonDeleteRetainsReadOrWriteFailure(bool synchronous, bool providerKey, bool readFailure)
    {
        using var client = new ControlledS3Client
        { ReadError = readFailure ? Failure("InternalError", HttpStatusCode.InternalServerError) : null };
        using var provider = Create(client);
        var id = readFailure ? Guid.NewGuid() : await Seed(provider, true);
        client.WriteError = Failure("AccessDenied", HttpStatusCode.Forbidden);
        var result = providerKey
            ? (synchronous ? provider.DeleteHolon(id.ToString()) : await provider.DeleteHolonAsync(id.ToString()))
            : (synchronous ? provider.DeleteHolon(id) : await provider.DeleteHolonAsync(id));
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        Assert.AreSame(readFailure ? client.ReadError : client.WriteError, result.Exception);
    }

    private static MinIOOASIS Create(ControlledS3Client client)
        => new(client, "synthetic", () => new byte[32]);

    private static async Task<Guid> Seed(MinIOOASIS provider, bool holon)
    {
        var id = Guid.NewGuid();
        if (holon)
        {
            var result = await provider.SaveHolonAsync(new Holon { Id = id, Name = "synthetic-record" }, saveChildren: false);
            Assert.IsFalse(result.IsError, result.Message);
        }
        else
        {
            var result = await provider.SaveAvatarAsync(new Avatar { Id = id, Username = "synthetic", Email = "synthetic@example.invalid" });
            Assert.IsFalse(result.IsError, result.Message);
        }
        return id;
    }

    private static AmazonS3Exception Failure(string code, HttpStatusCode status)
        => new(code) { ErrorCode = code, StatusCode = status };

}
