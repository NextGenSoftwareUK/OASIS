using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.LocalFileOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public class LocalFileLatestVersionTests
{
    [Fact]
    public async Task DetailDeletionAllProjectionsPreserveAvatarAndOtherDetail()
    {
        var root = Path.GetFullPath(Path.Combine("TestResults", "HyperDriveLocalVersion"));
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var provider = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        NextGenSoftware.OASIS.API.Core.Interfaces.IOASISStorageProvider storage = provider;
        var avatar = new Avatar { Id = Guid.Parse("c8a911fd-87c7-4dc2-8dce-80616135ce2e"), Username = "detail-delete-owner", Email = "detail-delete-owner@example.invalid" };
        var other = new AvatarDetail { Id = Guid.Parse("cc7fcd3b-1d29-45d1-90d4-5518d7b62874"), Karma = 99 };
        try
        {
            (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
            (await provider.SaveAvatarDetailAsync(other)).IsError.Should().BeFalse();
            foreach (var selector in new[] { "id", "username", "email" })
            foreach (var asynchronous in new[] { false, true })
            {
                (await provider.SaveAvatarDetailAsync(new AvatarDetail { Id = avatar.Id, Username = avatar.Username, Email = avatar.Email, IsActive = true, Karma = 42 })).IsError.Should().BeFalse();
                async Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> Delete(bool soft) => (selector, asynchronous) switch {
                    ("id", true) => await storage.DeleteAvatarDetailAsync(avatar.Id, soft), ("id", false) => storage.DeleteAvatarDetail(avatar.Id, soft),
                    ("username", true) => await storage.DeleteAvatarDetailByUsernameAsync(avatar.Username, soft), ("username", false) => storage.DeleteAvatarDetailByUsername(avatar.Username, soft),
                    ("email", true) => await storage.DeleteAvatarDetailByEmailAsync(avatar.Email, soft), _ => storage.DeleteAvatarDetailByEmail(avatar.Email, soft) };
                var soft = await Delete(true); soft.IsError.Should().BeFalse(soft.Message); soft.Result.Should().BeTrue();
                var retained = (await storage.LoadAvatarDetailAsync(avatar.Id)).Result;
                retained.IsActive.Should().BeFalse(); retained.DeletedDate.Should().NotBe(DateTime.MinValue); retained.Karma.Should().Be(42);
                var hard = await Delete(false); hard.IsError.Should().BeFalse(hard.Message); hard.Result.Should().BeTrue();
                (await storage.LoadAvatarDetailAsync(avatar.Id)).Result.Should().BeNull();
                (await storage.LoadAvatarByUsernameAsync(avatar.Username)).Result.Id.Should().Be(avatar.Id);
                (await storage.LoadAvatarByEmailAsync(avatar.Email)).Result.Id.Should().Be(avatar.Id);
                (await storage.LoadAvatarDetailAsync(other.Id)).Result.Karma.Should().Be(99);
                (await Delete(false)).IsError.Should().BeTrue();
            }
        }
        finally
        {
            if ((await provider.LoadAvatarDetailAsync(other.Id)).Result != null) (await provider.DeleteAvatarDetailAsync(other.Id, false)).IsError.Should().BeFalse();
            if ((await provider.LoadAvatarAsync(avatar.Id)).Result != null) (await provider.DeleteAvatarAsync(avatar.Id, false)).IsError.Should().BeFalse();
            await provider.DeActivateProviderAsync();
        }
    }

    [Fact]
    public async Task HolonExportsExcludeAvatarIdentityAndOtherOwners()
    {
        var root = Path.GetFullPath(Path.Combine("TestResults", "HyperDriveLocalVersion"));
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var provider = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        var avatar = new Avatar { Id = Guid.Parse("fe89a086-7869-4243-935a-156774c53716"),
            Username = "holon-export-owner", Email = "holon-export-owner@example.invalid" };
        var owned = new Holon { Id = Guid.Parse("b45f6fd8-7f96-4196-ad05-b5a3510cc115"), Name = "owned export",
            CreatedByAvatarId = avatar.Id, ParentHolonId = Guid.NewGuid() };
        var other = new Holon { Id = Guid.Parse("70a2b2c7-de74-40f7-9135-c6d3d075a7df"), Name = "other owner",
            CreatedByAvatarId = Guid.Parse("22222222-2222-2222-2222-222222222222"), ParentHolonId = avatar.Id };
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        try
        {
            (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
            (await provider.SaveHolonAsync(owned)).IsError.Should().BeFalse();
            (await provider.SaveHolonAsync(other)).IsError.Should().BeFalse();
            foreach (var export in new[] { provider.ExportAll(), await provider.ExportAllAsync() })
            {
                export.IsError.Should().BeFalse(export.Message);
                export.Result.Should().NotContainNulls();
                export.Result.Select(value => value.Id).Should().Contain(owned.Id).And.Contain(other.Id).And.NotContain(avatar.Id);
            }
            foreach (var export in new[] {
                provider.ExportAllDataForAvatarById(avatar.Id), await provider.ExportAllDataForAvatarByIdAsync(avatar.Id),
                provider.ExportAllDataForAvatarByUsername(avatar.Username), await provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username),
                provider.ExportAllDataForAvatarByEmail(avatar.Email), await provider.ExportAllDataForAvatarByEmailAsync(avatar.Email) })
            {
                export.IsError.Should().BeFalse(export.Message);
                export.Result.Should().NotContainNulls();
                export.Result.Select(value => value.Id).Should().Equal(owned.Id);
            }
        }
        finally
        {
            foreach (var id in new[] { owned.Id, other.Id })
                if ((await provider.LoadHolonAsync(id)).Result != null)
                    (await provider.DeleteHolonAsync(id)).IsError.Should().BeFalse();
            if ((await provider.LoadAvatarAsync(avatar.Id)).Result != null)
                (await provider.DeleteAvatarAsync(avatar.Id, softDelete: false)).IsError.Should().BeFalse();
            await provider.DeActivateProviderAsync();
        }
    }

    [Fact]
    public async Task DefaultVersionLoadsLatestWithoutTreatingOtherVersionsAsMatches()
    {
        var root = Path.GetFullPath(Path.Combine("TestResults", "HyperDriveLocalVersion"));
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var provider = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        var holon = new Holon { Id = Guid.Parse("4eb1a73f-edc2-4d88-b6c5-0a3c8130726a"), Name = "latest-version-regression", Version = 7,
            ProviderUniqueStorageKey = new() { [ProviderType.LocalFileOASIS] = "latest-version-regression" } };
        var avatar = new Avatar { Id = Guid.Parse("451d94b6-2fda-4a76-b5da-7a37457b4f8b"), Version = 7,
            Username = "latest-version-regression", Email = "latest-version-regression@example.invalid", IsActive = true };
        var detail = new AvatarDetail { Id = avatar.Id, Version = 7, Username = avatar.Username, Email = avatar.Email };
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        try
        {
            (await provider.SaveHolonAsync(holon)).IsError.Should().BeFalse();
            (await provider.LoadHolonAsync(holon.Id)).Result.Version.Should().Be(7);
            provider.LoadHolon(holon.Id).Result.Version.Should().Be(7);
            (await provider.LoadHolonAsync(holon.Id, version: 7)).Result.Version.Should().Be(7);
            (await provider.LoadHolonAsync(holon.Id, version: 6)).Result.Should().BeNull();
            (await provider.LoadHolonAsync("latest-version-regression")).Result.Version.Should().Be(7);
            (await provider.LoadHolonAsync("latest-version-regression", version: 6)).Result.Should().BeNull();
            (await provider.SaveAvatarAsync(avatar)).IsError.Should().BeFalse();
            (await provider.LoadAvatarAsync(avatar.Id)).Result.Version.Should().Be(7);
            (await provider.LoadAvatarByUsernameAsync(avatar.Username)).Result.Version.Should().Be(7);
            (await provider.LoadAvatarByEmailAsync(avatar.Email)).Result.Version.Should().Be(7);
            (await provider.LoadAvatarAsync(avatar.Id, version: 7)).Result.Version.Should().Be(7);
            (await provider.LoadAvatarAsync(avatar.Id, version: 6)).Result.Should().BeNull();
            (await provider.SaveAvatarDetailAsync(detail)).IsError.Should().BeFalse();
            (await provider.LoadAvatarDetailAsync(detail.Id)).Result.Version.Should().Be(7);
            provider.LoadAvatarDetail(detail.Id).Result.Version.Should().Be(7);
            (await provider.LoadAvatarDetailByUsernameAsync(detail.Username)).Result.Version.Should().Be(7);
            (await provider.LoadAvatarDetailByEmailAsync(detail.Email)).Result.Version.Should().Be(7);
            (await provider.LoadAvatarDetailAsync(detail.Id, version: 7)).Result.Version.Should().Be(7);
            (await provider.LoadAvatarDetailAsync(detail.Id, version: 6)).Result.Should().BeNull();
            (await provider.LoadAvatarDetailByUsernameAsync(detail.Username, version: 6)).Result.Should().BeNull();
            (await provider.LoadAvatarDetailByEmailAsync(detail.Email, version: 6)).Result.Should().BeNull();
            var softDeleted = await provider.DeleteAvatarAsync(avatar.Id, softDelete: true);
            softDeleted.IsError.Should().BeFalse(softDeleted.Message);
            softDeleted.Result.Should().BeTrue();
            (await provider.LoadAvatarDetailAsync(detail.Id)).Result.Version.Should().Be(7,
                "soft deletion must preserve avatar detail rather than invoke permanent cleanup");
            var retained = await provider.LoadAvatarAsync(avatar.Id);
            retained.Result.Should().NotBeNull();
            retained.Result.DeletedDate.Should().NotBe(DateTime.MinValue);
            retained.Result.IsActive.Should().BeFalse("soft-deleted avatars cannot remain active");
        }
        finally
        {
            (await provider.DeleteHolonAsync(holon.Id)).IsError.Should().BeFalse();
            (await provider.DeleteAvatarAsync(avatar.Id, softDelete: false)).IsError.Should().BeFalse();
            (await provider.LoadAvatarDetailAsync(detail.Id)).Result.Should().BeNull();
            await provider.DeActivateProviderAsync();
            var detailFile = Path.GetFullPath(Path.Combine(root, "files", "AvatarDetails", detail.Id + ".json"));
            detailFile.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase).Should().BeTrue();
            File.Exists(detailFile).Should().BeFalse("explicit permanent avatar deletion removes the associated detail");
        }
    }
}
