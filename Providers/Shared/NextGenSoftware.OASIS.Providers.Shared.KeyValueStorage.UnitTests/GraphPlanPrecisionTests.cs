using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class GraphPlanPrecisionTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void CapturedJsonElementStringRemainsAStringNotQuotedRawJson(bool child)
    {
        using var json=System.Text.Json.JsonDocument.Parse("\"encrypted-value-canary\"");
        var root=new Holon();var nested=new Holon();root.Children.Add(nested);
        (child?nested:root).MetaData["encrypted"]=json.RootElement.Clone();
        var graph=HolonGraphWritePlan.Create(new[]{root},true,true,0,0);
        Assert.AreEqual("encrypted-value-canary",graph.Nodes[child?1:0].Snapshot.MetaData["encrypted"].ToString());
        Assert.AreEqual(Guid.Empty,root.Id);Assert.AreEqual(Guid.Empty,nested.Id);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void CapturedNodeRetainsExactDecimalMetadata(bool child)
    {
        const decimal precise=1.0000000000000000000000000001m;
        var root=new Holon();var nested=new Holon();root.Children.Add(nested);
        var source=child?nested:root;source.MetaData["precise"]=precise;
        var graph=HolonGraphWritePlan.Create(new[]{root},true,true,0,0);var node=graph.Nodes[child?1:0];
        Assert.AreEqual(precise,Convert.ToDecimal(node.Snapshot.MetaData["precise"]));
        StringAssert.Contains(node.CapturedIntentJson,precise.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.AreEqual(Guid.Empty,root.Id);Assert.AreEqual(Guid.Empty,nested.Id);
        Assert.AreEqual(precise,source.MetaData["precise"]);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public void CapturedNodeRetainsWideFiniteDoubleMetadata(bool child)
    {
        var root=new Holon();var nested=new Holon();root.Children.Add(nested);var source=child?nested:root;
        source.MetaData["wide"]=1e100;
        var graph=HolonGraphWritePlan.Create(new[]{root},true,true,0,0);
        Assert.AreEqual(1e100,Convert.ToDouble(graph.Nodes[child?1:0].Snapshot.MetaData["wide"]));
        Assert.AreEqual(Guid.Empty,root.Id);Assert.AreEqual(Guid.Empty,nested.Id);
    }
}
