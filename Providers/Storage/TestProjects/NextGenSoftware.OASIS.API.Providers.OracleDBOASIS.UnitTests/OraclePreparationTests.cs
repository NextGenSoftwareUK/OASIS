using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.UnitTests;

[TestClass]
public class OraclePreparationTests
{
    [DataTestMethod][DataRow("null")][DataRow("authority")][DataRow("cancel")]
    public async Task RejectsInvalidPreparationBeforeOpeningAConnection(string kind)
    {
        var provider=new OracleDBOASIS("deliberately invalid connection string");
        var candidate=kind=="null"?null:new HolonWritePreparation(Guid.NewGuid(),Guid.NewGuid(),
            kind=="authority"?ProviderType.AWSOASIS:ProviderType.OracleDBOASIS,
            new[]{new ProtectedHolonPreparation(Guid.NewGuid(),"oasis-preparation1:opaque")});
        var result=await provider.ClaimHolonWritePreparationAsync(candidate!,kind=="cancel"?new CancellationToken(true):default);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        Assert.AreEqual(kind=="authority"?"COSMIC_AUTHORITY_MISMATCH":kind=="cancel"?"COSMIC_PREPARATION_CANCELLED":"COSMIC_PREPARATION_FAILED",result.ErrorCode);
        if(kind=="null")Assert.IsInstanceOfType(result.Exception,typeof(ArgumentNullException));
        if(kind=="cancel")Assert.IsInstanceOfType(result.Exception,typeof(OperationCanceledException));
    }
}
