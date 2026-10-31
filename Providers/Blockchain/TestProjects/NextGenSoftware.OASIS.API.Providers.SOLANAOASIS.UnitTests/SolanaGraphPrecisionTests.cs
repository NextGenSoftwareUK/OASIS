using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaGraphPrecisionTests
{
    [DataTestMethod]
    [DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public void GraphDtoStagingRetainsExactDetachedMetadata(bool child,bool wide)
    {
        var root=new Holon();var nested=new Holon();root.Children.Add(nested);
        var source=child?nested:root;var original=wide?(object)1e100:1.0000000000000000000000000001m;
        source.MetaData["precise"]=original;
        var method=typeof(SolanaOASIS).GetMethod("PlanSolanaGraph",BindingFlags.Static|BindingFlags.NonPublic)!;
        var plan=(ITuple)method.Invoke(null,new object[]{new IHolon[]{root},true,true,0,0})!;
        var nodes=(System.Collections.IList)plan[1]!;
        var node=nodes[child?1:0]!;
        var staged=(SolanaHolonDto)node.GetType().GetProperty("Record")!.GetValue(node)!;
        if(wide)Assert.AreEqual(1e100,Convert.ToDouble(staged.MetaData["precise"],System.Globalization.CultureInfo.InvariantCulture));
        else Assert.AreEqual((decimal)original,Convert.ToDecimal(staged.MetaData["precise"],System.Globalization.CultureInfo.InvariantCulture));
        source.MetaData["precise"]="newer source edit";
        Assert.AreNotEqual("newer source edit",staged.MetaData["precise"]);
        Assert.AreEqual(Guid.Empty,root.Id);Assert.AreEqual(Guid.Empty,nested.Id);
        Assert.AreEqual(Guid.Empty,nested.ParentHolonId);Assert.AreNotEqual(Guid.Empty,staged.Id);
    }
}
