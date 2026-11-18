using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    [Theory]
    [InlineData(HyperDriveModes.V2, false)]
    [InlineData(HyperDriveModes.V2, true)]
    [InlineData(HyperDriveModes.Legacy, false)]
    [InlineData(HyperDriveModes.Legacy, true)]
    public async Task AvatarSaveWithBlankPasswordDoesNotDiscardCallerChanges(string mode, bool synchronous)
    {
        var id = Guid.NewGuid();
        var stored = new Avatar {
            Id = id, Username = "owned-save-fixture", IsActive = true, Version = 1, VersionId = Guid.NewGuid(),
            Password = BCrypt.Net.BCrypt.HashPassword("owned-fixture-password", workFactor: 4),
            MetaData = new Dictionary<string, object> { ["note"] = "before", ["keep"] = "retained" }
        };
        var pending = new Avatar {
            Id = id, Username = stored.Username, IsActive = true, Version = stored.Version, VersionId = stored.VersionId,
            Password = string.Empty,
            MetaData = new Dictionary<string, object> { ["note"] = "after", ["keep"] = "retained" }
        };
        var dna = CreateDna(mode);
        var routing = new ProviderManager(null, dna) {
            IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false
        };
        var provider = CreateActiveProvider();
        provider.Setup(p => p.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(p => p.LoadAvatarAsync(id, 0)).ReturnsAsync(new OASISResult<IAvatar>(stored));
        provider.Setup(p => p.LoadAvatar(id, 0)).Returns(new OASISResult<IAvatar>(stored));
        IAvatar committed = null;
        provider.Setup(p => p.SaveAvatarAsync(It.IsAny<IAvatar>())).ReturnsAsync((IAvatar value) => {
            committed = value;
            return new OASISResult<IAvatar>(value) { IsSaved = true };
        });
        provider.Setup(p => p.SaveAvatar(It.IsAny<IAvatar>())).Returns((IAvatar value) => {
            committed = value;
            return new OASISResult<IAvatar>(value) { IsSaved = true };
        });
        routing.RegisterProvider(provider.Object).Should().BeTrue();
        routing.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new AvatarManager(provider.Object, dna, routing);
        var saved = synchronous ? manager.SaveAvatar(pending) : await manager.SaveAvatarAsync(pending);
        saved.IsError.Should().BeFalse(saved.Message);
        committed.Should().NotBeNull();
        committed.MetaData["note"].Should().Be("after");
        committed.MetaData["keep"].Should().Be("retained");
        committed.Password.Should().Be(stored.Password);
        stored.MetaData["note"].Should().Be("before");
    }
}
