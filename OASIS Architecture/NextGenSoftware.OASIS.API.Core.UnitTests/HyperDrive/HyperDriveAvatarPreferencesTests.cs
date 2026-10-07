using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed class HyperDriveAvatarPreferencesTests
{
    [Fact]
    public void PortableContract_RoundTripsThroughGeneratedAotJsonMetadata()
    {
        var value = new HyperDriveAvatarPreferences
        {
            MasterVolume = 0.42f,
            GraphicsPreset = "Ultra",
            UiHighContrast = true,
            ViewPresets = new[]
            {
                new HyperDriveViewPreset { Name = "Nearby", Tab = "quests", SearchQuery = "geo", SortAscending = false }
            },
            PanelLayouts = new[]
            {
                new HyperDrivePanelLayout { PanelId = "tracker", AnchoredX = 12, Width = 420, Height = 260 }
            }
        };

        var restored = HyperDriveJson.Deserialize<HyperDriveAvatarPreferences>(HyperDriveJson.Serialize(value));

        Assert.Equal(0.42f, restored.MasterVolume);
        Assert.Equal("Ultra", restored.GraphicsPreset);
        Assert.True(restored.UiHighContrast);
        Assert.Single(restored.ViewPresets);
        Assert.Equal("Nearby", restored.ViewPresets[0].Name);
        Assert.Single(restored.PanelLayouts);
        Assert.Equal(420, restored.PanelLayouts[0].Width);
    }
}
