using System.Text;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass]
[DoNotParallelize]
public class Web3PrivateStorageTests
{
    [TestMethod]
    public void CanonicalEncryptedDetailPreservesInventoryAndBindsIdentityKindAndContract()
    {
        var prior=Environment.GetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY");
        try
        {
            Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY",Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            var provider=new PolygonOASIS.PolygonOASIS("http://127.0.0.1:18545","unused","0xowned");
            var detail=new AvatarDetail {Id=Guid.NewGuid(),Username="private-name",Email="private@example.invalid",XP=321,Karma=29,IsActive=true};
            detail.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name="owned seed"});
            var encrypted=provider.EncodePrivateRecord("detail",detail);
            Assert.IsFalse(encrypted.Contains(detail.Email));Assert.IsFalse(encrypted.Contains(detail.Username));
            var entity=new EntityOASIS {EntityId=(uint)HashUtility.GetNumericHash(detail.Id.ToString()),ExternalId=Web3CoreOASISBaseProvider.PrivateExternalId(detail.Id),Info=Encoding.UTF8.GetBytes(encrypted)};
            Assert.AreEqual(32,entity.ExternalId.Length);
            var restored=provider.DecodePrivateRecord<AvatarDetail>("detail",detail.Id,entity);
            Assert.AreEqual(detail.Id,restored.Id);Assert.AreEqual(321L,restored.XP);Assert.AreEqual(29L,restored.Karma);
            Assert.AreEqual("owned seed",restored.Inventory.Single().Name);
            var enumerated=provider.DecodePrivateEntity<AvatarDetail>("detail",entity);
            Assert.AreEqual(detail.Id,enumerated.Id);Assert.AreEqual("owned seed",enumerated.Inventory.Single().Name);
            Assert.ThrowsException<AuthenticationTagMismatchException>(()=>provider.DecodePrivateRecord<AvatarDetail>("avatar",detail.Id,entity));
            var other=new PolygonOASIS.PolygonOASIS("http://127.0.0.1:18545","unused","0xother");
            Assert.ThrowsException<AuthenticationTagMismatchException>(()=>other.DecodePrivateRecord<AvatarDetail>("detail",detail.Id,entity));
            entity.ExternalId=new byte[32];
            Assert.ThrowsException<InvalidDataException>(()=>provider.DecodePrivateRecord<AvatarDetail>("detail",detail.Id,entity));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY",prior);}
    }
}
