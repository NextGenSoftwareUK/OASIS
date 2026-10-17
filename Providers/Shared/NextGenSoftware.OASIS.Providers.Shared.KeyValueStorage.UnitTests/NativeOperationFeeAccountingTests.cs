using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class NativeOperationFeeAccountingTests
{
    private static NativeFeeValuation Value(Guid operation,string receipt,decimal cost,string denomination="MIST")
        =>new(operation,"SuiOASIS",receipt,denomination,Math.Sign(cost).ToString(),cost,"test-only-valuation","fixture-v1",new DateTime(2026,10,16,0,0,0,DateTimeKind.Utc));
    [DataTestMethod]
    [DataRow(10,3,7,0)] [DataRow(3,10,0,7)] [DataRow(3,3,0,0)] [DataRow(0,0,0,0)]
    public void NetFeesAndRebatesOncePerOperation(int fee,int rebate,int charge,int unused)
    {
        var id=Guid.NewGuid();var total=NativeOperationFeeAccounting.Calculate(id,new[]{Value(id,"fee",fee),Value(id,"rebate",-rebate)});
        Assert.AreEqual((decimal)fee,total.FeesUsd);Assert.AreEqual((decimal)rebate,total.RebatesUsd);
        Assert.AreEqual((decimal)charge,total.ChargeUsd);Assert.AreEqual((decimal)unused,total.UnusedRebateUsd);
        Assert.AreEqual(2,total.Evidence.Count);
    }
    [TestMethod]
    public void MixedDenominationsUseExplicitValuedCostsNotAdditionOfNativeUnits()
    {
        var id=Guid.NewGuid();var total=NativeOperationFeeAccounting.Calculate(id,new[]{Value(id,"one",0.1m),Value(id,"two",-0.03m,"other-base-unit")});
        Assert.AreEqual(0.07m,total.ChargeUsd);
        Assert.AreEqual(0.03m,total.RebatesUsd);
    }
    [TestMethod]
    public void RebateCannotBeMovedToAnotherOperationOrCountedTwice()
    {
        var id=Guid.NewGuid();var value=Value(id,"original",-3m);
        Assert.ThrowsException<ArgumentException>(()=>NativeOperationFeeAccounting.Calculate(Guid.NewGuid(),new[]{value}));
        Assert.ThrowsException<ArgumentException>(()=>NativeOperationFeeAccounting.Calculate(id,new[]{value,value}));
        var other=Guid.NewGuid();
        Assert.AreEqual(4m,NativeOperationFeeAccounting.Calculate(other,new[]{Value(other,"separate-operation",4m)}).ChargeUsd);
    }
    [TestMethod]
    public void MissingConflictingOrUnrepresentableValuationNeverBecomesZeroCharge()
    {
        var id=Guid.NewGuid();var value=Value(id,"original",1m);
        foreach(var invalid in new[]{value with{ValuationSource=""},value with{ValuedAtUtc=DateTime.Now},
            value with{NativeNetUnits="01"},value with{NativeNetUnits="-1"},value with{NetCostUsd=0m}})
            Assert.ThrowsException<ArgumentException>(()=>NativeOperationFeeAccounting.Calculate(id,new[]{invalid}));
        Assert.ThrowsException<ArgumentException>(()=>NativeOperationFeeAccounting.Calculate(id,Array.Empty<NativeFeeValuation>()));
        Assert.ThrowsException<OverflowException>(()=>NativeOperationFeeAccounting.Calculate(id,new[]{Value(id,"one",decimal.MaxValue),Value(id,"two",1m)}));
    }
}
