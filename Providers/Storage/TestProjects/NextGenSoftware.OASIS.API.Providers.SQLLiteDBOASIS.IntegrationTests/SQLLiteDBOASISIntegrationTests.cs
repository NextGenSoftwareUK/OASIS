using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Xunit;
using FluentAssertions;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public class SQLLiteDBOASISIntegrationTests
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"oasis-sqlite-integration-{Guid.NewGuid():N}.db");
    private string TestConnectionString => $"Data Source={_databasePath}";

    [Fact]
    public async Task SQLLiteDBOASIS_Should_Initialize_Successfully()
    {
        // Arrange & Act
        var provider = new SQLLiteDBOASIS(TestConnectionString);
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
        var provider = new SQLLiteDBOASIS(TestConnectionString);
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
        var provider = new SQLLiteDBOASIS(TestConnectionString);
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
