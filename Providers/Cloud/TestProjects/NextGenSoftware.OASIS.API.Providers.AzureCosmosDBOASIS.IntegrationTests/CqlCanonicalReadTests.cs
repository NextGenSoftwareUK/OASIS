using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

[TestClass]
public class CqlCanonicalReadTests
{
    private static string Document(Guid id)
    {
        var document = OasisJson.ParseStoredObject(OasisJson.Serialize(new Holon { Id = id, Name = "coffee", Version = 3 }));
        document["id"] = id.ToString("D");
        return document.ToString();
    }

    [TestMethod]
    public void CanonicalReadUsesCoreRecordAndPreservesVersion()
    {
        var repository = new HolonRepository(null!);
        var id = Guid.NewGuid();
        var rows = repository.DecodeCurrentQueryRows(new[] { Document(id) }, 10, default);
        Assert.AreEqual(typeof(Holon), rows.Single().GetType());
        Assert.AreEqual(id, rows.Single().Id);
        Assert.AreEqual(3, rows.Single().Version);
    }

    [TestMethod]
    public void DuplicateNativeIdentityRejectsWholeResult()
    {
        var repository = new HolonRepository(null!);
        var document = Document(Guid.NewGuid());
        Assert.ThrowsException<InvalidDataException>(() => repository.DecodeCurrentQueryRows(new[] { document, document }, 10, default));
    }

    [TestMethod]
    public void NativeKeyPayloadMismatchUsesExistingIdentityValidation()
    {
        var repository = new HolonRepository(null!);
        var document = JObject.Parse(Document(Guid.NewGuid()));
        document["id"] = Guid.NewGuid().ToString("D");
        Assert.ThrowsException<InvalidDataException>(() => repository.DecodeCurrentQueryRows(new[] { document.ToString() }, 10, default));
    }

    [TestMethod]
    public void DecoderCannotPublishMoreThanCallerBudget()
    {
        var repository = new HolonRepository(null!);
        Assert.ThrowsException<InvalidDataException>(() => repository.DecodeCurrentQueryRows(
            new[] { Document(Guid.NewGuid()), Document(Guid.NewGuid()) }, 1, default));
    }

    [TestMethod]
    public void CancellationRejectsEvenEmptyAcknowledgement()
    {
        var repository = new HolonRepository(null!);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.ThrowsException<OperationCanceledException>(() => repository.DecodeCurrentQueryRows(Array.Empty<string>(), 10, cancellation.Token));
    }

    [TestMethod]
    public void VisibilityViolationRejectsRatherThanFilteringNativeRows()
    {
        var access = CosmicQueryReadAccess.OwnerOrPublic(Guid.NewGuid()).Result;
        var row = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid(), IsPublic = false };
        Assert.ThrowsException<InvalidDataException>(() => HolonRepository.ValidateCqlLeafRows(new IHolon[] { row }, access, default));
    }

    [TestMethod]
    public void LeafReadDoesNotExposeEmbeddedChildGraph()
    {
        var row = new Holon { Id = Guid.NewGuid(), IsPublic = true };
        row.Children.Add(new Holon { Id = Guid.NewGuid() });
        var rows = HolonRepository.ValidateCqlLeafRows(new IHolon[] { row }, null!, default);
        Assert.AreEqual(1, rows.Count);
        Assert.AreEqual(0, rows[0].Children.Count);
    }
}
