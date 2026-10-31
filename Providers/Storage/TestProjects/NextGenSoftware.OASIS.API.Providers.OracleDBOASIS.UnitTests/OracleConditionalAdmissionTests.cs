using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public sealed class OracleConditionalAdmissionTests
{
    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)]
    [DataRow(3,false)][DataRow(3,true)][DataRow(4,false)][DataRow(4,true)][DataRow(5,false)][DataRow(5,true)][DataRow(6,false)][DataRow(6,true)]
    public async Task InvalidConditionalPlanRejectsBeforeConnection(int scenario,bool synchronous)
    {
        var root=new Holon{Id=Guid.NewGuid()};var conditions=new List<HolonWriteCondition>{new(root.Id,null)};
        if(scenario==1||scenario==5){var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=scenario==5?Guid.NewGuid():root.Id};root.Children.Add(child);if(scenario==5)conditions.Add(new(child.Id,null));}
        if(scenario==2)conditions.Add(new(Guid.NewGuid(),null));if(scenario==4)root.Children.Add(root);
        var request=scenario==6?null!:new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),scenario==0?ProviderType.SQLLiteDBOASIS:ProviderType.OracleDBOASIS,conditions),scenario==3?ConditionalHolonOperation.HardDelete:ConditionalHolonOperation.Save,new[]{root});
        IConditionalHolonStorageProvider provider=new OracleDBOASIS("UnsupportedOracleOptionForOwnedTest=true");var result=synchronous?provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsSaved);Assert.IsInstanceOfType<ArgumentException>(result.Exception);
        // Own admission cause, not the SDK's ArgumentException from this deliberately invalid connection string.
        if(scenario==6){Assert.IsInstanceOfType<ArgumentNullException>(result.Exception);Assert.AreEqual("request",((ArgumentNullException)result.Exception).ParamName);}
        else StringAssert.Contains(result.Exception.Message,scenario switch{0=>"another authority",1 or 2=>"Exactly one condition",3=>"existing revisions",4=>"null/repeated nodes or a cycle",_=>"conflicting parent identity"});
        Assert.AreEqual(0,root.Version);Assert.IsNull(CosmicConcurrency.GetProviderRevisionId(root,ProviderType.OracleDBOASIS));Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));
    }
    [TestMethod]
    public async Task PreCancelledConditionalRequestDoesNotConnectOrPublish()
    {
        var root=new Holon{Id=Guid.NewGuid()};var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),Guid.NewGuid(),ProviderType.OracleDBOASIS,new[]{new HolonWriteCondition(root.Id,null)}),ConditionalHolonOperation.Save,new[]{root});
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();IConditionalHolonStorageProvider provider=new OracleDBOASIS("UnsupportedOracleOptionForOwnedTest=true");
        var result=await provider.ExecuteConditionalHolonWriteAsync(request,cancellation.Token);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType<OperationCanceledException>(result.Exception);Assert.AreEqual(0,root.Version);Assert.IsNull(CosmicConcurrency.GetProviderRevisionId(root,ProviderType.OracleDBOASIS));
        Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));
    }
}
