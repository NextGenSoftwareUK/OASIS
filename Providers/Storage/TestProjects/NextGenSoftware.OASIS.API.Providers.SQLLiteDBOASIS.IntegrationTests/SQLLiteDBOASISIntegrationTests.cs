using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public class SQLLiteDBOASISIntegrationTests : IDisposable
{
    private readonly string _databasePath;
    private readonly FileStream _lease;
    private string TestConnectionString => $"Data Source={_databasePath};Pooling=False";

    public SQLLiteDBOASISIntegrationTests()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1"))) root = root.Parent;
        if (root == null) throw new InvalidOperationException("Existing task checkout required.");
        var directory = Path.Combine(root.FullName,"TestResults/SQLiteCurrent");
        Directory.CreateDirectory(directory);
        _databasePath = Path.Combine(directory,"integration-current.db");
        _lease = new FileStream(Path.Combine(directory,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        if (File.Exists(_databasePath)) { _lease.Dispose(); throw new InvalidOperationException("Existing fixture DB requires inspection; refusing overwrite."); }
    }

    public void Dispose()
    {
        try
        {
            foreach (var file in new[] { _databasePath, _databasePath + "-wal", _databasePath + "-shm" })
                if (File.Exists(file)) File.Delete(file);
        }
        finally { _lease.Dispose(); }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ParentSelectorsIsolateChildrenAndRespectTypeAndVersion(bool synchronous)
    {
        using var context = new DataContext(TestConnectionString);
        context.Database.EnsureCreated();
        var repository = new HolonRepository(context);
        var parent = new Holon { Id = Guid.NewGuid() };
        parent.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "local-parent";
        parent.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = "foreign-parent";
        var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = parent.Id, HolonType = HolonType.Quest, Version = 3 };
        foreach (var row in new[] { parent, child, new Holon { Id = Guid.NewGuid(), HolonType = HolonType.Quest, Version = 3 }, new Holon { Id = Guid.NewGuid(), ParentHolonId = parent.Id, Version = 2 } })
            repository.SaveHolon(row).IsError.Should().BeFalse();
        var byId = synchronous ? repository.LoadHolonsForParent(parent.Id, HolonType.Quest, loadChildren:false) : await repository.LoadHolonsForParentAsync(parent.Id, HolonType.Quest, loadChildren:false);
        byId.Result.Select(row => row.Id).Should().Equal(child.Id);
        var byKey = synchronous ? repository.LoadHolonsForParent("local-parent", HolonType.Quest, loadChildren:false, version:3) : await repository.LoadHolonsForParentAsync("local-parent", HolonType.Quest, loadChildren:false, version:3);
        byKey.Result.Select(row => row.Id).Should().Equal(child.Id);
        repository.LoadHolonsForParent("foreign-parent", loadChildren:false).Result.Should().BeEmpty();
        repository.LoadHolonsForParent(parent.Id, HolonType.Quest, loadChildren:false, version:2).Result.Should().BeEmpty();
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task RejectedHardDeleteRetainsErrorAndDoesNotLeakIntoLaterSave(bool synchronous, bool byKey)
    {
        using var context = new DataContext(TestConnectionString);
        context.Database.EnsureCreated();
        var repository = new HolonRepository(context);
        var id = Guid.NewGuid();
        repository.SaveHolon(new Holon { Id = id, IsActive = true }).IsError.Should().BeFalse();
        context.Database.ExecuteSqlRaw("CREATE TRIGGER reject_delete BEFORE DELETE ON Holon BEGIN SELECT RAISE(ABORT, 'owned test rejection'); END;");
        var rejected = byKey
            ? (synchronous ? repository.DeleteHolon(id.ToString(), false) : await repository.DeleteHolonAsync(id.ToString(), false))
            : (synchronous ? repository.DeleteHolon(id, false) : await repository.DeleteHolonAsync(id, false));
        rejected.IsError.Should().BeTrue();
        rejected.Exception.Should().NotBeNull();
        rejected.IsDeleted.Should().BeFalse();
        context.Database.ExecuteSqlRaw("DROP TRIGGER reject_delete;");
        repository.SaveHolon(new Holon { Id = Guid.NewGuid() }).IsError.Should().BeFalse();
        repository.LoadHolon(id, loadChildren:false).Result.Id.Should().Be(id);
        var deleted = synchronous ? repository.DeleteHolon(id, false) : await repository.DeleteHolonAsync(id, false);
        deleted.IsDeleted.Should().BeTrue();
        repository.LoadHolon(id, loadChildren:false).Result.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GuidDeletionReturnsDeletedRecordAndPersistsBeforeCompletion(bool synchronous)
    {
        var id = Guid.NewGuid();
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await provider.SaveHolonAsync(new Holon { Id = id, IsActive = true }, saveChildren:false)).IsError.Should().BeFalse();
            var deleted = synchronous ? provider.DeleteHolon(id) : await provider.DeleteHolonAsync(id);
            deleted.IsError.Should().BeFalse(deleted.Message);
            deleted.IsDeleted.Should().BeTrue();
            deleted.DeletedCount.Should().Be(1);
            deleted.Result.Id.Should().Be(id);
            deleted.Result.IsActive.Should().BeFalse();
            deleted.Result.DeletedDate.Should().NotBe(default(DateTime));
        }
        using var reopened = new SQLLiteDBOASIS(TestConnectionString);
        (await reopened.ActivateProviderAsync()).IsError.Should().BeFalse();
        var loaded = await reopened.LoadHolonAsync(id, loadChildren:false);
        loaded.Result.IsActive.Should().BeFalse();
        loaded.Result.DeletedDate.Should().NotBe(default(DateTime));
    }

    [Fact]
    public async Task LatestAndExplicitVersionMetadataQueriesRespectCurrentVersionAndType()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var holon = new Holon { Id = Guid.NewGuid(), Version = 3, HolonType = HolonType.Quest,
            MetaData = new Dictionary<string,object> { ["Email"] = "query@example.invalid" } };
        (await provider.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
        provider.LoadHolon(holon.Id,loadChildren:false).Result.Should().NotBeNull();
        (await provider.LoadHolonAsync(holon.Id,loadChildren:false,version:2)).Result.Should().BeNull();
        (await provider.LoadAllHolonsAsync(loadChildren:false)).Result.Should().ContainSingle();
        (await provider.LoadHolonsByMetaDataAsync("Email","query@example.invalid",type:HolonType.Quest,loadChildren:false,version:3)).Result.Should().ContainSingle();
        (await provider.LoadHolonsByMetaDataAsync("Email","query@example.invalid",type:HolonType.Avatar,loadChildren:false,version:3)).Result.Should().BeEmpty();
    }

    [Fact]
    public async Task ProviderKeySelectorsReturnOnlyTheirOwnedRecordAndRejectDuplicateKeys()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var first = new Holon { Id = Guid.NewGuid(), Version = 1 };
        first.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "first-local-key";
        first.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = "foreign-provider-key";
        var second = new Holon { Id = Guid.NewGuid(), Version = 2 };
        second.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "second-local-key";
        (await provider.SaveHolonAsync(first,saveChildren:false)).IsError.Should().BeFalse();
        (await provider.SaveHolonAsync(second,saveChildren:false)).IsError.Should().BeFalse();
        provider.LoadHolon("second-local-key",loadChildren:false).Result.Id.Should().Be(second.Id);
        (await provider.LoadHolonAsync("first-local-key",loadChildren:false)).Result.Id.Should().Be(first.Id);
        (await provider.LoadHolonAsync("first-local-key",loadChildren:false,version:2)).Result.Should().BeNull();
        (await provider.LoadHolonAsync("foreign-provider-key",loadChildren:false)).Result.Should().BeNull();
        provider.LoadHolon("absent",loadChildren:false).Result.Should().BeNull();
        var duplicate = new Holon { Id = Guid.NewGuid() };
        duplicate.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "first-local-key";
        (await provider.SaveHolonAsync(duplicate,saveChildren:false)).IsError.Should().BeTrue();
    }

    [Fact]
    public async Task ProviderKeyDeletionUsesOnlyTheLocalKey()
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var holon = new Holon { Id = Guid.NewGuid(), IsActive = true };
        holon.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "local-delete-key";
        holon.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = "foreign-delete-key";
        (await provider.SaveHolonAsync(holon,saveChildren:false)).IsError.Should().BeFalse();
        provider.DeleteHolon("foreign-delete-key").IsError.Should().BeTrue();
        (await provider.LoadHolonAsync(holon.Id,loadChildren:false)).Result.IsActive.Should().BeTrue();
        (await provider.DeleteHolonAsync("local-delete-key")).IsError.Should().BeFalse();
        (await provider.LoadHolonAsync(holon.Id,loadChildren:false)).Result.IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TrackedUpdatesAndRejectedWritesDoNotPoisonLaterSaves(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var original = new Holon { Id = Guid.NewGuid(), Name = "first", Version = 1 };
        original.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "owned-key";
        (await provider.SaveHolonAsync(original,saveChildren:false)).IsError.Should().BeFalse();
        (await provider.LoadHolonAsync(original.Id,loadChildren:false)).Result.Name.Should().Be("first");
        original.Name = "updated"; original.Version = 2;
        var update = synchronous ? provider.SaveHolon(original,saveChildren:false) : await provider.SaveHolonAsync(original,saveChildren:false);
        update.IsError.Should().BeFalse(update.Message);
        var rejected = new Holon { Id = Guid.NewGuid() };
        rejected.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "owned-key";
        var failure = synchronous ? provider.SaveHolon(rejected,saveChildren:false) : await provider.SaveHolonAsync(rejected,saveChildren:false);
        failure.IsError.Should().BeTrue(); failure.Exception.Should().NotBeNull();
        var next = new Holon { Id = Guid.NewGuid(), Name = "valid after failure" };
        var saved = synchronous ? provider.SaveHolon(next,saveChildren:false) : await provider.SaveHolonAsync(next,saveChildren:false);
        saved.IsError.Should().BeFalse(saved.Message);
        (await provider.LoadHolonAsync(original.Id,loadChildren:false)).Result.Name.Should().Be("updated");
        (await provider.LoadHolonAsync(rejected.Id,loadChildren:false)).Result.Should().BeNull();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RejectedBulkWriteRollsBackAndDoesNotPoisonLaterWrites(bool synchronous)
    {
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var held = new Holon { Id = Guid.NewGuid() };
        held.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "held-key";
        (await provider.SaveHolonAsync(held,saveChildren:false)).IsError.Should().BeFalse();
        var valid = new Holon { Id = Guid.NewGuid() };
        var conflicting = new Holon { Id = Guid.NewGuid() };
        conflicting.ProviderUniqueStorageKey[ProviderType.SQLLiteDBOASIS] = "held-key";
        var batch = new IHolon[] { valid, conflicting };
        var rejected = synchronous ? provider.SaveHolons(batch,saveChildren:false) : await provider.SaveHolonsAsync(batch,saveChildren:false);
        rejected.IsError.Should().BeTrue(); rejected.Exception.Should().NotBeNull();
        (await provider.LoadHolonAsync(valid.Id,loadChildren:false)).Result.Should().BeNull();
        (await provider.LoadHolonAsync(conflicting.Id,loadChildren:false)).Result.Should().BeNull();
        var next = new IHolon[] { new Holon(), new Holon() };
        var saved = synchronous ? provider.SaveHolons(next,saveChildren:false) : await provider.SaveHolonsAsync(next,saveChildren:false);
        saved.IsError.Should().BeFalse(saved.Message);
        saved.Result.Should().HaveCount(2);
        saved.Result.Select(row => row.Id).Should().OnlyHaveUniqueItems().And.NotContain(Guid.Empty);
        foreach (var row in saved.Result)
            (await provider.LoadHolonAsync(row.Id,loadChildren:false)).Result.Id.Should().Be(row.Id);
    }

    [Fact]
    public async Task SQLLiteDBOASIS_Should_Initialize_Successfully()
    {
        // Arrange & Act
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        var activation = await provider.ActivateProviderAsync();
        activation.IsError.Should().BeFalse(activation.Message);
        
        // Assert
        provider.Should().NotBeNull();
        provider.ProviderType.Value.Should().Be(ProviderType.SQLLiteDBOASIS);
        await Task.CompletedTask;
    }

    [Fact]
    public async Task SQLLiteDBOASIS_Should_Support_Avatar_Integration()
    {
        // Arrange
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        var activation = await provider.ActivateProviderAsync();
        activation.IsError.Should().BeFalse(activation.Message);
        
        // Act & Assert
        provider.Should().NotBeNull();
        provider.ProviderType.Value.Should().Be(ProviderType.SQLLiteDBOASIS);
        
        // Test avatar operations
        var avatar = new Avatar
        {
            Id = Guid.NewGuid(),
            Username = "testuser",
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            AvatarType = new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User)
        };
        
        var saveResult = await provider.SaveAvatarAsync(avatar);
        saveResult.IsError.Should().BeFalse(saveResult.Message);
        saveResult.Result.Should().NotBeNull();

        var loaded = await provider.LoadAvatarByUsernameAsync(avatar.Username);
        loaded.IsError.Should().BeFalse(loaded.Message);
        loaded.Result.Should().NotBeNull();
        loaded.Result.Email.Should().Be(avatar.Email);

        (await provider.DeActivateProviderAsync()).IsError.Should().BeFalse();
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var afterReactivation = await provider.LoadAvatarByUsernameAsync(avatar.Username);
        afterReactivation.IsError.Should().BeFalse(afterReactivation.Message);
        afterReactivation.Result.Id.Should().Be(avatar.Id);
    }

    [Fact]
    public async Task SQLLiteDBOASIS_Should_Support_Holon_Integration()
    {
        // Arrange
        using var provider = new SQLLiteDBOASIS(TestConnectionString);
        var activation = await provider.ActivateProviderAsync();
        activation.IsError.Should().BeFalse(activation.Message);
        
        // Act & Assert
        provider.Should().NotBeNull();
        provider.ProviderType.Value.Should().Be(ProviderType.SQLLiteDBOASIS);
        
        // Test holon operations
        var holon = new Holon
        {
            Name = "Test Holon",
            Description = "Test Description"
        };
        
        var saveResult = await provider.SaveHolonAsync(holon);
        saveResult.IsError.Should().BeFalse(saveResult.Message);
        saveResult.Result.Should().NotBeNull();

        var loaded = await provider.LoadHolonAsync(saveResult.Result.Id);
        loaded.IsError.Should().BeFalse(loaded.Message);
        loaded.Result.Should().NotBeNull();
        loaded.Result.Name.Should().Be(holon.Name);
    }
}
