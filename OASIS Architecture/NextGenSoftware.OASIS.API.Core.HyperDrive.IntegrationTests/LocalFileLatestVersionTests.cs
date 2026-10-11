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
    public async Task DefaultVersionLoadsLatestWithoutTreatingOtherVersionsAsMatches()
    {
        var root = Path.GetFullPath(Path.Combine("TestResults", "HyperDriveLocalVersion"));
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "store.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var provider = new LocalFileOASIS(storageDirectory: Path.Combine(root, "files"));
        var holon = new Holon { Id = Guid.Parse("4eb1a73f-edc2-4d88-b6c5-0a3c8130726a"), Name = "latest-version-regression", Version = 7,
            ProviderUniqueStorageKey = new() { [ProviderType.LocalFileOASIS] = "latest-version-regression" } };
        var avatar = new Avatar { Id = Guid.Parse("451d94b6-2fda-4a76-b5da-7a37457b4f8b"), Version = 7,
            Username = "latest-version-regression", Email = "latest-version-regression@example.invalid" };
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
