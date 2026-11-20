using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
public sealed class SessionClosureCaptureControls
{
    private sealed class Capture
    {
        public Guid Id {get;set;}
        public int GetterCalls;
        public Guid ComputedId {get {GetterCalls++;return Id;}}
    }

    [TestMethod]
    public void SessionClosureCaptureAvoidsGetterExecution()
    {
        var value=new Capture {Id=Guid.NewGuid()};
        var root=CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SQLLiteDBOASIS).Result);
        var autoProperty=root.Where(row=>row.Id==value.Id);
        Assert.IsFalse(autoProperty.IsError,autoProperty.Message);
        var computedProperty=root.Where(row=>row.Id==value.ComputedId);
        Assert.IsTrue(computedProperty.IsError,"A computed getter must not be evaluated as a query parameter.");
        Assert.AreEqual(0,value.GetterCalls,"Query binding must never invoke the application getter.");
    }
}
