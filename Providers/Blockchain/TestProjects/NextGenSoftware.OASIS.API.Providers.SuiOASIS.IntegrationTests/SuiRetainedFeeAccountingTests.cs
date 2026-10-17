using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Services;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

// Read-only regression over the retained live test producer's public reports.
// Does not independently authenticate them or authorize/settle a real charge.
[TestClass]
public sealed class SuiRetainedFeeAccountingTests
{
    [DataTestMethod]
    [DataRow("nft",5)] [DataRow("nft-batch",9)] [DataRow("nft-offchain",5)] [DataRow("token",7)]
    public void RebatesNetAcrossOriginalOperationBeforeOneFloor(string scenario,int expectedReceipts)
    {
        var root=new DirectoryInfo(AppContext.BaseDirectory);
        while(root!=null&&!File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/Run-ReusableSuiV2Tests.ps1")))root=root.Parent;
        Assert.IsNotNull(root);
        using var report=JsonDocument.Parse(File.ReadAllBytes(Path.Combine(root.FullName,$"TestResults/SuiCurrent/sui-csharp-{scenario}-receipts-current.json")));
        var data=report.RootElement;
        Assert.AreEqual(scenario,data.GetProperty("scenario").GetString());
        Assert.IsTrue(data.GetProperty("burned").GetBoolean());Assert.IsFalse(data.GetProperty("productionBillingReconciled").GetBoolean());
        var operationId=data.GetProperty("operationId").GetGuid();Assert.AreNotEqual(Guid.Empty,operationId);
        var receipts=data.GetProperty("receipts").EnumerateArray().ToArray();Assert.AreEqual(expectedReceipts,receipts.Length);
        var chain=receipts[0].GetProperty("chainId").GetString()!;
        // Explicit fixture-only $1/SUI-equivalent unit price; NOT a market observation,
        // historical admission quote, production catalogue or sponsored-spend policy.
        var instant=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);
        var quote=NativeFeePriceQuote.BindObservation(operationId,"SuiOASIS",chain,"MIST","test-only-local-sui",9,"test-only-catalogue",
            new AssetUsdQuote("test-only-local-sui",1m,instant,instant,"test-only-synthetic-unit-price","fixture-v1"),instant,instant.AddMinutes(1));
        var rows=new List<NativeFeeValuation>();var identities=new HashSet<string>(StringComparer.Ordinal);BigInteger net=0;
        foreach(var receipt in receipts){
            Assert.AreEqual(chain,receipt.GetProperty("chainId").GetString());Assert.IsTrue(receipt.GetProperty("success").GetBoolean());
            var id=receipt.GetProperty("ProviderRequestId").GetString()!;Assert.IsTrue(identities.Add(id));
            var units=receipt.GetProperty("netUnits").GetString()!;net+=BigInteger.Parse(units,CultureInfo.InvariantCulture);
            rows.Add(quote.ValueOriginalReceipt(id,units));
        }
        var total=NativeOperationFeeAccounting.Calculate(operationId,rows);
        var netUsd=NativeOperationFeeAccounting.ValueNativeUnitsExact(net.ToString(CultureInfo.InvariantCulture),9,1m);
        Assert.AreEqual(Math.Max(0m,netUsd),total.ChargeUsd);Assert.AreEqual(Math.Max(0m,-netUsd),total.UnusedRebateUsd);
        var perReceiptFloor=rows.Sum(row=>NativeOperationFeeAccounting.Calculate(operationId,new[]{row}).ChargeUsd);
        if(scenario.StartsWith("nft",StringComparison.Ordinal)){
            Assert.IsTrue(total.RebatesUsd>0);Assert.IsTrue(total.ChargeUsd<perReceiptFloor,"Per-receipt flooring would discard within-operation rebates.");
        }
        Console.WriteLine($"{scenario}: original receipts={rows.Count}, raw net MIST={net}; synthetic-price accounting only, no production charge.");
    }
}
