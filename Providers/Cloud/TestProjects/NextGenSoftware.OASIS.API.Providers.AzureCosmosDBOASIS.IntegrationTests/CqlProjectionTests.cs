using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

[TestClass]
public class CqlProjectionTests
{
    public sealed class DemoRow
    {
        public static int Calls;
        public string Name { get; set; } = "";
        public DemoRow() { Calls++; }
    }

    private static CosmicProjectionPlan<Holon, DemoRow> Plan()
    {
        var query = CosmicQuery<Holon>.Create().Select(h => new DemoRow { Name = h.Name });
        Assert.IsFalse(query.IsError, query.Message);
        var plan = CosmicProjectionPlanner.Plan(query.Result, ProviderType.AzureCosmosDBOASIS,
            new[] { new CosmicProjectionTarget(ProviderType.AzureCosmosDBOASIS, AzureCosmosDBOASIS.CqlCapabilities, AzureCosmosDBOASIS.CqlProjectionCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);
        return plan.Result;
    }

    private static string Native(Guid id, int state = 2, object? value = null) => JsonSerializer.Serialize(new
    {
        source = new { Id = id, id = id.ToString("D"), IsPublic = true, CreatedByAvatarId = Guid.Empty },
        cells = new[] { new { state, value = value ?? "coffee" } }
    });

    [TestMethod]
    public void NativeCellsValidateBeforeDtoConstruction()
    {
        DemoRow.Calls = 0;
        var plan = Plan();
        var rows = CosmosCqlProjection.Decode(plan, new[] { Native(Guid.NewGuid()) }, 10, 1000, default);
        Assert.AreEqual(0, DemoRow.Calls);
        var materialized = CosmicProjectionResults.Materialize(plan.Query, rows);
        Assert.IsFalse(materialized.IsError, materialized.Message);
        Assert.AreEqual("coffee", materialized.Result.Single().Name);
        Assert.AreEqual(1, DemoRow.Calls);
    }

    [TestMethod]
    public void InvalidLaterCellCannotConstructAnyDto()
    {
        DemoRow.Calls = 0;
        var plan = Plan();
        Assert.ThrowsException<InvalidDataException>(() => CosmosCqlProjection.Decode(plan,
            new[] { Native(Guid.NewGuid()), Native(Guid.NewGuid(), 3) }, 10, 1000, default));
        Assert.AreEqual(0, DemoRow.Calls);
    }

    [TestMethod]
    public void DuplicateSourceIdentityRejectsEvenWhenIdIsNotSelected()
    {
        var plan = Plan();
        var document = Native(Guid.NewGuid());
        Assert.ThrowsException<InvalidDataException>(() => CosmosCqlProjection.Decode(plan,
            new[] { document, document }, 10, 1000, default));
    }

    [TestMethod]
    public void ProjectionPayloadBudgetIsEnforcedOnDetachedCells()
    {
        var plan = Plan();
        Assert.ThrowsException<InvalidDataException>(() => CosmosCqlProjection.Decode(plan,
            new[] { Native(Guid.NewGuid()) }, 10, 1, default));
    }

    [TestMethod]
    public void ProjectionSelectsCellsAndMinimalVisibilityProofWithoutFullEntity()
    {
        var command = CosmosCqlProjection.Compile(Plan(), 10);
        StringAssert.StartsWith(command.QueryText, "SELECT VALUE");
        StringAssert.Contains(command.QueryText, "\"cells\"");
        StringAssert.Contains(command.QueryText, "\"source\"");
        Assert.IsFalse(command.QueryText.Contains("SELECT *"));
    }
}
