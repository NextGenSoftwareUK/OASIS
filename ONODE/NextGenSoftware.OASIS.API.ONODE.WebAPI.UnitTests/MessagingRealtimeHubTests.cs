using NextGenSoftware.OASIS.API.ONODE.WebAPI.Hubs;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class MessagingRealtimeHubTests
{
    [Fact]
    public void Ticket_RedeemsOnce_ForTheAvatarItWasIssuedTo()
    {
        var avatar = Guid.NewGuid();
        var ticket = MessagingRealtimeHub.IssueTicket(avatar);

        Assert.True(MessagingRealtimeHub.TryRedeemTicket(ticket, out var redeemed));
        Assert.Equal(avatar, redeemed);
        Assert.False(MessagingRealtimeHub.TryRedeemTicket(ticket, out _));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-ticket")]
    public void UnknownTicket_IsRejected(string? ticket)
    {
        Assert.False(MessagingRealtimeHub.TryRedeemTicket(ticket!, out var avatar));
        Assert.Equal(Guid.Empty, avatar);
    }

    [Fact]
    public void Tickets_AreUniqueAndUnguessableLength()
    {
        var a = MessagingRealtimeHub.IssueTicket(Guid.NewGuid());
        var b = MessagingRealtimeHub.IssueTicket(Guid.NewGuid());
        Assert.NotEqual(a, b);
        Assert.True(a.Length >= 43);
    }

    [Fact]
    public async Task PublishingToAnOfflineAvatar_DoesNothingAndDoesNotThrow()
    {
        var avatar = Guid.NewGuid();
        Assert.False(MessagingRealtimeHub.IsOnline(avatar));
        await MessagingRealtimeHub.PublishAsync(avatar, new { type = "message" });
    }
}
