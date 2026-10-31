using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class ReplicaBaselineRepairRequestTests
{
    private static ReplicaBaselineRepairCommand.Request Reviewed() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        6, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new string('A',64), "mongo-store:reviewed", "/data/reviewed.db");

    [Fact]
    public void CanonicalPreviouslyReviewedRequestRetainsEveryIdentity()
    {
        var reviewed=Reviewed();
        Assert.Equal(reviewed,ReplicaBaselineRepairCommand.ParseReviewedRequest(JsonSerializer.SerializeToUtf8Bytes(reviewed)));
    }

    [Theory]
    [InlineData("OperatorId")] [InlineData("InitializationOperationId")] [InlineData("CatalogVersion")]
    [InlineData("SourceOperationId")] [InlineData("SourceActorId")] [InlineData("RootId")]
    public void ConflictingDuplicateIdentityIsRejectedBeforeProviderComposition(string field)
    {
        var reviewed=Reviewed();
        var canonical=JsonSerializer.Serialize(reviewed);
        var ambiguous=canonical[..^1]+",\""+field+"\":\""+Guid.NewGuid()+"\"}";
        // Reproduce the old reader's last-value behavior on the same shipping type.
        Assert.NotEqual(reviewed,JsonSerializer.Deserialize<ReplicaBaselineRepairCommand.Request>(ambiguous));
        Assert.Throws<InvalidDataException>(()=>ReplicaBaselineRepairCommand.ParseReviewedRequest(Encoding.UTF8.GetBytes(ambiguous)));
    }

    [Theory]
    [InlineData("null")] [InlineData("[]")] [InlineData("{\"unknown\":1}")]
    public void NonObjectOrUnknownFieldsCannotBecomeAnIntent(string json)
        =>Assert.ThrowsAny<Exception>(()=>ReplicaBaselineRepairCommand.ParseReviewedRequest(Encoding.UTF8.GetBytes(json)));

    [Fact]
    public async Task FileBoundaryReadsTheCanonicalIntentAndRejectsOversizeWithoutProviderWork()
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null && !System.IO.File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1")))root=root.Parent;
        Assert.NotNull(root);
        var directory=Path.Combine(root!.FullName,"TestResults/HostedCqlIntegrationCurrent");
        Directory.CreateDirectory(directory);
        var path=Path.Combine(directory,"repair-request-parser-control.json");
        var owned=false;
        try
        {
            var reviewed=Reviewed();
            await using(var file=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {
                owned=true;
                await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(reviewed));
            }
            Assert.Equal(reviewed,await ReplicaBaselineRepairCommand.ReadReviewedRequestAsync(path));
            await System.IO.File.WriteAllBytesAsync(path,new byte[8193]);
            await Assert.ThrowsAsync<InvalidDataException>(()=>ReplicaBaselineRepairCommand.ReadReviewedRequestAsync(path));
        }
        finally { if(owned)System.IO.File.Delete(path); }
    }

    [Fact]
    public void OversizeInputIsRejectedBeforeJsonOrNativeWork()
        =>Assert.Throws<InvalidDataException>(()=>ReplicaBaselineRepairCommand.ParseReviewedRequest(new byte[8193]));
}
