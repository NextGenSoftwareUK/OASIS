using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class HolonRecoveryEvidenceTests
{
    [Fact]
    public void NativeIdentityCollectionIsDetachedAndOpaque()
    {
        const string identity="{\"session\":\"owned\",\"transaction\":17}";
        var input=new List<string>{identity,identity};var evidence=new HolonDeliveryRecoveryEvidence(true,input);input.Clear();
        Assert.Equal(identity,Assert.Single(evidence.OriginalTransactionIds));
    }
    [Theory]
    [InlineData(false,"native")] [InlineData(true,"")] [InlineData(true," ")]
    public void NativeIdentitiesRequireExplicitReconciliationAndNonblankValues(bool required,string identity)
        =>Assert.Throws<ArgumentException>(()=>new HolonDeliveryRecoveryEvidence(required,new[]{identity}));
    [Fact]
    public void MissingNativeIdentityIsRepresentedWithoutInventingAValue()
    {
        var evidence=new HolonDeliveryRecoveryEvidence(true,Array.Empty<string>());
        Assert.True(evidence.RequiresNativeReconciliation);Assert.Empty(evidence.OriginalTransactionIds);
    }
}
