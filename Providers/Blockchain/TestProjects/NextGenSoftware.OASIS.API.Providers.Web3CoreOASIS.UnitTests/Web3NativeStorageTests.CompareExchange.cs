using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    private static async Task VerifyCheckedUpsertsAsync(Web3CoreOASIS native)
    {
        foreach(var kind in new[] {"avatar","detail","holon"})
        {
            var id=Guid.NewGuid();var numeric=(uint)Core.Utilities.HashUtility.GetNumericHash(id.ToString());
            var external=Web3CoreOASISBaseProvider.PrivateExternalId(id);
            var emptyHash=Nethereum.Util.Sha3Keccack.Current.CalculateHash(Array.Empty<byte>());
            var first=new byte[] {1,2};var second=new byte[] {3,4};
            var firstHash=Nethereum.Util.Sha3Keccack.Current.CalculateHash(first);
            Func<Task<EntityOASIS>> read=kind switch {"avatar"=>()=>native.GetAvatarByIdAsync(numeric),"detail"=>()=>native.GetAvatarDetailByIdAsync(numeric),_=>()=>native.GetHolonByIdAsync(numeric)};
            Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(await read()));
            await Assert.ThrowsExceptionAsync<Web3MutationReconciliationException>(()=>native.UpsertCheckedAsync(kind,numeric,external,true,emptyHash,first));
            await native.UpsertCheckedAsync(kind,numeric,external,false,emptyHash,first);
            await Assert.ThrowsExceptionAsync<Web3MutationReconciliationException>(()=>native.UpsertCheckedAsync(kind,numeric,external,false,firstHash,second));
            await Assert.ThrowsExceptionAsync<Web3MutationReconciliationException>(()=>native.UpsertCheckedAsync(kind,numeric,external,true,emptyHash,second));
            await Assert.ThrowsExceptionAsync<Web3MutationReconciliationException>(()=>native.UpsertCheckedAsync(kind,numeric,Web3CoreOASISBaseProvider.PrivateExternalId(Guid.NewGuid()),true,firstHash,second));
            CollectionAssert.AreEqual(first,(await read()).Info);
            await native.UpsertCheckedAsync(kind,numeric,external,true,firstHash,second);
            CollectionAssert.AreEqual(second,(await read()).Info);
            await Assert.ThrowsExceptionAsync<Web3MutationReconciliationException>(()=>native.UpsertCheckedAsync(kind,numeric,external,true,firstHash,first));
            CollectionAssert.AreEqual(second,(await read()).Info);
            // Exact synthetic fixture cleanup; these bytes are contract-only, not encrypted application data.
            if(kind=="avatar") await native.DeleteAvatarAsync(numeric);
            else if(kind=="detail") await native.DeleteAvatarDetailAsync(numeric);
            else await native.DeleteHolonAsync(numeric);
            Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(await read()));
        }
    }
}
