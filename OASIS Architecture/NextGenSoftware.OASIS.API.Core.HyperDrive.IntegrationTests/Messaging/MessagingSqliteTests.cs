using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests.Messaging;

/// <summary>
/// One real temporary SQLite store shared by the class: the HyperDrive v2 provider registry allows a single
/// instance per provider type per process, so the provider cannot be recreated per test. Every test uses fresh
/// avatar ids, so tests stay isolated.
/// </summary>
public sealed class MessagingStoreFixture : IAsyncLifetime
{
    private DirectoryInfo _directory = null!;
    private SQLLiteDBOASIS _provider = null!;
    public MessagingManager Messaging { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _directory = Directory.CreateTempSubdirectory("oasis-messaging-");
        _provider = new SQLLiteDBOASIS($"Data Source={Path.Combine(_directory.FullName, "messaging.db")};Pooling=False");
        var activated = await _provider.ActivateProviderAsync();
        Assert.False(activated.IsError, activated.Message);
        var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
        var providers = new ProviderManager(_provider, dna)
            { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false, IsAutoReplicationEnabled = false };
        providers.RegisterProvider(_provider);
        Messaging = new MessagingManager(_provider, dna, providers);
    }

    public Task DisposeAsync()
    {
        _provider.Dispose();
        _directory.Delete(true);
        return Task.CompletedTask;
    }
}

public sealed class MessagingSqliteTests : IClassFixture<MessagingStoreFixture>
{
    private readonly MessagingManager _messaging;
    private readonly Guid _alice = Guid.NewGuid();
    private readonly Guid _bob = Guid.NewGuid();
    private readonly Guid _carol = Guid.NewGuid();

    public MessagingSqliteTests(MessagingStoreFixture fixture) => _messaging = fixture.Messaging;

    [Fact]
    public async Task Send_IsIdempotentByClientId_AndRejectsAnotherSendersId()
    {
        var id = Guid.NewGuid();
        var request = new MessageSendRequest { Id = id, ToAvatarId = _bob, Content = "hello" };

        var first = await _messaging.SendMessageAsync(_alice, request);
        var replay = await _messaging.SendMessageAsync(_alice, request);
        var stolen = await _messaging.SendMessageAsync(_carol, request);

        Assert.False(first.IsError, first.Message);
        Assert.False(replay.IsError, replay.Message);
        Assert.Equal(first.Result.Id, replay.Result.Id);
        Assert.True(stolen.IsError);
        var all = await _messaging.GetMessagesAsync(_bob);
        Assert.Single(all.Result);
    }

    [Fact]
    public async Task DeliveryAndReadReceipts_AdvanceOnly_AndCannotBeSetByTheSender()
    {
        var id = Guid.NewGuid();
        await _messaging.SendMessageAsync(_alice, new MessageSendRequest { Id = id, ToAvatarId = _bob, Content = "hi" });

        Assert.True((await _messaging.MarkMessagesAsDeliveredAsync(_alice, new List<Guid> { id })).IsError);
        Assert.False((await _messaging.MarkMessagesAsDeliveredAsync(_bob, new List<Guid> { id })).IsError);
        Assert.Equal(MessageDeliveryState.Delivered, (await _messaging.GetMessagesAsync(_bob)).Result.Single().DeliveryState);

        Assert.False((await _messaging.MarkMessagesAsReadAsync(_bob, new List<Guid> { id })).IsError);
        Assert.False((await _messaging.MarkMessagesAsDeliveredAsync(_bob, new List<Guid> { id })).IsError);
        Assert.Equal(MessageDeliveryState.Read, (await _messaging.GetMessagesAsync(_bob)).Result.Single().DeliveryState);
    }

    [Fact]
    public async Task Sync_ReturnsMessagesAfterTheCursor_InOrder_WithPaging()
    {
        for (var i = 0; i < 5; i++)
        {
            await _messaging.SendMessageAsync(_alice, new MessageSendRequest { Id = Guid.NewGuid(), ToAvatarId = _bob, Content = "m" + i });
            await Task.Delay(5);
        }

        var firstPage = (await _messaging.GetMessagesSinceAsync(_bob, null, 3)).Result;
        var secondPage = (await _messaging.GetMessagesSinceAsync(_bob, firstPage.NextCursor, 3)).Result;

        Assert.Equal(new[] { "m0", "m1", "m2" }, firstPage.Messages.Select(m => m.Content));
        Assert.True(firstPage.HasMore);
        Assert.Equal(new[] { "m3", "m4" }, secondPage.Messages.Select(m => m.Content));
        Assert.False(secondPage.HasMore);
        Assert.Empty((await _messaging.GetMessagesSinceAsync(_bob, secondPage.NextCursor, 3)).Result.Messages);
    }

    [Fact]
    public async Task Conversations_AreListedWithUnreadCounts()
    {
        await _messaging.SendMessageAsync(_alice, new MessageSendRequest { Id = Guid.NewGuid(), ToAvatarId = _bob, Content = "1" });
        await _messaging.SendMessageAsync(_alice, new MessageSendRequest { Id = Guid.NewGuid(), ToAvatarId = _bob, Content = "2" });
        await _messaging.SendMessageAsync(_carol, new MessageSendRequest { Id = Guid.NewGuid(), ToAvatarId = _bob, Content = "3" });

        var conversations = (await _messaging.GetConversationsAsync(_bob)).Result;

        Assert.Equal(2, conversations.Count);
        Assert.Equal(2, conversations.Single(c => c.OtherAvatarId == _alice).UnreadCount);
        Assert.Equal(MessagingConversation.DirectId(_alice, _bob), conversations.Single(c => c.OtherAvatarId == _alice).ConversationId);
    }

    [Fact]
    public async Task Groups_OnlyMembersSendReadAndSee_AndReceiptsAreTrackedPerMember()
    {
        var created = await _messaging.CreateGroupAsync(_alice, "Team", new[] { _bob });
        Assert.False(created.IsError, created.Message);
        var group = created.Result.Id;

        var outsider = await _messaging.SendMessageAsync(_carol, new MessageSendRequest { Id = Guid.NewGuid(), ConversationId = group, Content = "let me in" });
        Assert.True(outsider.IsError);

        var id = Guid.NewGuid();
        var sent = await _messaging.SendMessageAsync(_alice, new MessageSendRequest { Id = id, ConversationId = group, Content = "welcome" });
        Assert.False(sent.IsError, sent.Message);

        Assert.Single((await _messaging.GetGroupMessagesAsync(_bob, group)).Result);
        Assert.True((await _messaging.GetGroupMessagesAsync(_carol, group)).IsError);
        Assert.Single((await _messaging.GetMessagesAsync(_bob)).Result);
        Assert.Empty((await _messaging.GetMessagesAsync(_carol)).Result);

        Assert.True((await _messaging.MarkMessagesAsReadAsync(_carol, new List<Guid> { id })).IsError);
        Assert.False((await _messaging.MarkMessagesAsReadAsync(_bob, new List<Guid> { id })).IsError);
        Assert.Contains(_bob, (await _messaging.GetGroupMessagesAsync(_alice, group)).Result.Single().ReadBy);

        var conversation = (await _messaging.GetConversationsAsync(_bob)).Result.Single();
        Assert.True(conversation.IsGroup);
        Assert.Equal("Team", conversation.Title);
        Assert.Equal(0, conversation.UnreadCount);
    }

    [Fact]
    public async Task Groups_CreatorManagesMembership_AndLeavingRemovesAccess()
    {
        var group = (await _messaging.CreateGroupAsync(_alice, "Crew", new[] { _bob })).Result.Id;

        Assert.True((await _messaging.AddGroupMembersAsync(_bob, group, new[] { _carol })).IsError);
        var added = await _messaging.AddGroupMembersAsync(_alice, group, new[] { _carol });
        Assert.False(added.IsError, added.Message);
        var carolView = await _messaging.GetGroupAsync(_carol, group);
        Assert.False(carolView.IsError, carolView.Message);

        Assert.True((await _messaging.LeaveGroupAsync(_alice, group)).IsError);
        var left = await _messaging.LeaveGroupAsync(_carol, group);
        Assert.False(left.IsError, left.Message);
        Assert.True((await _messaging.GetGroupAsync(_carol, group)).IsError);
        Assert.DoesNotContain(group, (await _messaging.GetGroupsAsync(_carol)).Result.Select(g => g.Id));
    }

    [Fact]
    public async Task Events_FireForNewMessagesAndReceipts()
    {
        var stored = new List<(Message Message, int Recipients)>();
        var receipts = new List<MessageDeliveryState>();
        _messaging.MessageStored += (m, r) => stored.Add((m, r.Count));
        _messaging.ReceiptsChanged += (_, _, state) => receipts.Add(state);
        var id = Guid.NewGuid();

        await _messaging.SendMessageAsync(_alice, new MessageSendRequest { Id = id, ToAvatarId = _bob, Content = "x" });
        await _messaging.SendMessageAsync(_alice, new MessageSendRequest { Id = id, ToAvatarId = _bob, Content = "x" });
        await _messaging.MarkMessagesAsDeliveredAsync(_bob, new List<Guid> { id });
        await _messaging.MarkMessagesAsReadAsync(_bob, new List<Guid> { id });

        Assert.Single(stored);
        Assert.Equal(new[] { MessageDeliveryState.Delivered, MessageDeliveryState.Read }, receipts);
    }
}
