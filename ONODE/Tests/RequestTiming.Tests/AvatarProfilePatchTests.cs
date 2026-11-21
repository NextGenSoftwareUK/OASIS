using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.AvatarProfile;
using Xunit;

public class AvatarProfilePatchTests
{
    [Fact]
    public void ExplicitOptionalClearsRemainDistinctFromOmittedFieldsOnRoundTrip()
    {
        var patch = JsonSerializer.Deserialize<AvatarProfilePatch>("{\"address\":null,\"title\":\"\"}")!;
        Assert.True(patch.IsSpecified(AvatarProfileField.Address));
        Assert.True(patch.IsSpecified(AvatarProfileField.Title));
        Assert.False(patch.IsSpecified(AvatarProfileField.FirstName));
        Assert.Null(patch.Address);
        Assert.Equal("", patch.Title);
        using var result = JsonDocument.Parse(JsonSerializer.Serialize(patch));
        Assert.Equal(2, result.RootElement.EnumerateObject().Count());
        Assert.Equal(JsonValueKind.Null, result.RootElement.GetProperty("address").ValueKind);
        Assert.False(result.RootElement.TryGetProperty("firstName", out _));
    }

    [Theory]
    [InlineData("{\"activeQuestId\":\"quest\"}")]
    [InlineData("{\"isVerified\":true}")]
    [InlineData("{\"karma\":99}")]
    [InlineData("{\"firstName\":\"one\",\"FirstName\":\"two\"}")]
    [InlineData("{\"title\":123}")]
    [InlineData("{\"title\":{}}")]
    [InlineData("{\"username\":null}")]
    [InlineData("{\"username\":\"  \"}")]
    [InlineData("{\"email\":\"\"}")]
    [InlineData("{\"email\":\"invalid\"}")]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("[]")]
    public void InvalidOrNonProfileChangesAreRejected(string json)
        => Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<AvatarProfilePatch>(json));

    [Fact]
    public void CanonicalFieldsAcceptCaseInsensitiveNamesWithoutAddingUnspecifiedState()
    {
        var patch = JsonSerializer.Deserialize<AvatarProfilePatch>("{\"FirstName\":\"  Example  \",\"email\":\"person@example.test\"}")!;
        Assert.Equal("Example", patch.FirstName);
        Assert.Equal("person@example.test", patch.Email);
        Assert.False(patch.IsSpecified(AvatarProfileField.Address));
        Assert.Empty(patch.Validate(new ValidationContext(patch)));
    }

    [Fact]
    public void ProgrammaticChangesUseTheSameIdentityValidation()
    {
        var patch = new AvatarProfilePatch { Username = null };
        Assert.Single(patch.Validate(new ValidationContext(patch)));
    }
}
