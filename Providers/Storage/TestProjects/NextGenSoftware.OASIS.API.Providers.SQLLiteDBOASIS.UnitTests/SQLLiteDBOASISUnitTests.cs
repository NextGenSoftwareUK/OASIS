using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.UnitTests
{
    [TestClass]
    public class SQLLiteDBOASISProviderTests
    {
        private SQLLiteDBOASIS _provider = null!;

        [TestInitialize]
        public void Setup()
        {
            _provider = new SQLLiteDBOASIS("Data Source=:memory:");
        }

        [TestMethod]
        public void ProviderType_ShouldBeSQLLiteDBOASIS()
        {
            // Arrange & Act
            var providerType = _provider.ProviderType;

            // Assert
            Assert.AreEqual(ProviderType.SQLLiteDBOASIS, providerType.Value);
        }

        [DataTestMethod]
        [DataRow(0,false,false)][DataRow(0,true,false)][DataRow(1,false,false)][DataRow(1,true,false)]
        [DataRow(2,false,false)][DataRow(2,true,false)][DataRow(3,false,false)][DataRow(3,true,false)]
        [DataRow(0,false,true)][DataRow(0,true,true)][DataRow(1,false,true)][DataRow(1,true,true)]
        [DataRow(2,false,true)][DataRow(2,true,true)][DataRow(3,false,true)][DataRow(3,true,true)]
        public async Task SuccessfulExportsReportReturnedRootCount(int selector,bool sync,bool empty)
        {
            await using var keepAlive=new Microsoft.Data.Sqlite.SqliteConnection("Data Source=owned-export-count;Mode=Memory;Cache=Shared");
            await keepAlive.OpenAsync();
            _provider.Dispose();_provider=new SQLLiteDBOASIS(keepAlive.ConnectionString);
            var activated=await _provider.ActivateProviderAsync();Assert.IsFalse(activated.IsError,activated.Message);
            var avatar=new NextGenSoftware.OASIS.API.Core.Holons.Avatar{Id=Guid.NewGuid(),Username="owned-export-count",Email="owned-export-count@example.invalid",IsActive=true};
            var saved=await _provider.SaveAvatarAsync(avatar);Assert.IsFalse(saved.IsError,saved.Message);
            var holon=new NextGenSoftware.OASIS.API.Core.Holons.Holon{Id=Guid.NewGuid(),CreatedByAvatarId=avatar.Id,Name="owned export count",IsActive=true};
            if(!empty)Assert.IsFalse((await _provider.SaveHolonAsync(holon,saveChildren:false)).IsError);
            var result=selector switch{
                0=>sync?_provider.ExportAll():await _provider.ExportAllAsync(),
                1=>sync?_provider.ExportAllDataForAvatarById(avatar.Id):await _provider.ExportAllDataForAvatarByIdAsync(avatar.Id),
                2=>sync?_provider.ExportAllDataForAvatarByUsername(avatar.Username):await _provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username),
                _=>sync?_provider.ExportAllDataForAvatarByEmail(avatar.Email):await _provider.ExportAllDataForAvatarByEmailAsync(avatar.Email)
            };
            Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(empty?0:1,result.Result.Count());
            if(!empty)Assert.AreEqual(holon.Id,result.Result.Single().Id);
            Assert.IsTrue(result.IsLoaded);Assert.AreEqual(empty?0:1,result.LoadedCount);Assert.IsFalse(result.IsSaved);
        }

        [DataTestMethod]
        [DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)]
        [DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)]
        public async Task AllVersionExportsRetainExactVersionsAndCurrentOwnerIdentity(int selector,bool sync)
        {
            await using var keepAlive=new Microsoft.Data.Sqlite.SqliteConnection("Data Source=owned-export-history;Mode=Memory;Cache=Shared");
            await keepAlive.OpenAsync();_provider.Dispose();_provider=new SQLLiteDBOASIS(keepAlive.ConnectionString){IsVersionControlEnabled=true};
            Assert.IsFalse((await _provider.ActivateProviderAsync()).IsError);
            var avatar=new NextGenSoftware.OASIS.API.Core.Holons.Avatar{Id=Guid.NewGuid(),Username="old-history-owner",Email="old-history-owner@example.invalid",IsActive=true};
            Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);
            avatar.Username="current-history-owner";avatar.Email="current-history-owner@example.invalid";
            Assert.IsFalse((await _provider.SaveAvatarAsync(avatar)).IsError);
            var own=new NextGenSoftware.OASIS.API.Core.Holons.Holon{Id=Guid.NewGuid(),CreatedByAvatarId=avatar.Id,IsActive=true};
            var foreign=new NextGenSoftware.OASIS.API.Core.Holons.Holon{Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),IsActive=true};
            for(var version=1;version<=3;version++){
                own.PreviousVersionId=own.VersionId;own.VersionId=Guid.NewGuid();own.Version=version;
                foreign.PreviousVersionId=foreign.VersionId;foreign.VersionId=Guid.NewGuid();foreign.Version=version;
                own.Name="owned-v"+version;foreign.Name="foreign-v"+version;
                var saved=await _provider.SaveHolonAsync(own,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Message);
                Assert.IsFalse((await _provider.SaveHolonAsync(foreign,saveChildren:false)).IsError);
            }
            var result=selector switch{
                0=>sync?_provider.ExportAll(-1):await _provider.ExportAllAsync(-1),
                1=>sync?_provider.ExportAllDataForAvatarById(avatar.Id,-1):await _provider.ExportAllDataForAvatarByIdAsync(avatar.Id,-1),
                2=>sync?_provider.ExportAllDataForAvatarByUsername(avatar.Username,-1):await _provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username,-1),
                _=>sync?_provider.ExportAllDataForAvatarByEmail(avatar.Email,-1):await _provider.ExportAllDataForAvatarByEmailAsync(avatar.Email,-1)
            };
            Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(selector==0?6:3,result.LoadedCount);
            var selected=result.Result.Where(row=>row.Id==own.Id).ToArray();CollectionAssert.AreEqual(new[]{1,2,3},selected.Select(row=>row.Version).ToArray());
            for(var index=0;index<selected.Length;index++)Assert.AreEqual("owned-v"+(index+1),selected[index].Name);
            if(selector!=0)Assert.IsTrue(result.Result.All(row=>row.CreatedByAvatarId==avatar.Id));
        }

        [DataTestMethod]
        [DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)]
        [DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)]
        public async Task InvalidExportVersionRejectsBeforeActivation(int selector,bool sync)
        {
            var result=selector switch{
                0=>sync?_provider.ExportAll(-2):await _provider.ExportAllAsync(-2),
                1=>sync?_provider.ExportAllDataForAvatarById(Guid.NewGuid(),-2):await _provider.ExportAllDataForAvatarByIdAsync(Guid.NewGuid(),-2),
                2=>sync?_provider.ExportAllDataForAvatarByUsername("owned",-2):await _provider.ExportAllDataForAvatarByUsernameAsync("owned",-2),
                _=>sync?_provider.ExportAllDataForAvatarByEmail("owned@example.invalid",-2):await _provider.ExportAllDataForAvatarByEmailAsync("owned@example.invalid",-2)
            };
            Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);
            Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);Assert.IsFalse(_provider.IsProviderActivated);
        }

        [DataTestMethod][DataRow(false)][DataRow(true)]
        public async Task EmptyAllVersionExportIsLoadedWithZeroRoots(bool sync)
        {
            await using var keepAlive=new Microsoft.Data.Sqlite.SqliteConnection("Data Source=owned-empty-history;Mode=Memory;Cache=Shared");
            await keepAlive.OpenAsync();_provider.Dispose();_provider=new SQLLiteDBOASIS(keepAlive.ConnectionString);
            Assert.IsFalse((await _provider.ActivateProviderAsync()).IsError);
            var result=sync?_provider.ExportAll(-1):await _provider.ExportAllAsync(-1);
            Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);Assert.IsNotNull(result.Result);Assert.AreEqual(0,result.Result.Count());
        }

        [TestMethod]
        public void IsProviderActivated_ShouldBeFalseInitially()
        {
            // Arrange & Act
            var isActivated = _provider.IsProviderActivated;

            // Assert
            Assert.IsFalse(isActivated);
        }

        [TestMethod]
        public void ProviderName_ShouldBeSQLLiteDBOASIS()
        {
            // Arrange & Act
            var providerName = _provider.ProviderName;

            // Assert
            Assert.AreEqual("SQLLiteDBOASIS", providerName);
        }

        [TestMethod]
        public void ProviderDescription_ShouldNotBeEmpty()
        {
            // Arrange & Act
            var description = _provider.ProviderDescription;

            // Assert
            Assert.IsNotNull(description);
            Assert.IsFalse(string.IsNullOrEmpty(description));
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (_provider != null && _provider.IsProviderActivated)
            {
                _provider.DeActivateProvider();
            }
            _provider?.Dispose();
        }

        [TestMethod]
        public void NativeCodeGenesis_WithoutGenerator_DoesNotAcknowledgeSuccessOrActivateStorage()
        {
            // The IOASISSuperStar contract reserves true for successful generation.
            IOASISSuperStar generator = _provider;
            Assert.IsFalse(generator.NativeCodeGenesis(null!, null!, null!));
            Assert.IsFalse(_provider.IsProviderActivated);
        }

    }
}
