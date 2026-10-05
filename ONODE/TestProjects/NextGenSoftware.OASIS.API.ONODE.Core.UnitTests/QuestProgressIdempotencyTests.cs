using FluentAssertions;
using Moq;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.Core.UnitTests;

public sealed class QuestProgressIdempotencyTests
{
    [Fact]
    public async Task ReplayingStableProgressOperationReturnsOriginalRewardsWithoutUpdatingQuestAgain()
    {
        const string game = "Our World";
        var avatarId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var rewardId = Guid.NewGuid();
        var collectedIdentity = "geonft:" + Guid.NewGuid().ToString("D");
        var quest = new Quest
        {
            Id = questId,
            Name = "Offline replay quest",
            Status = QuestStatus.InProgress,
            RewardKarma = 7,
            RewardXP = 11,
            RewardInventoryItemIds = new List<Guid> { rewardId },
            Objectives = new List<Objective>
            {
                new Objective
                {
                    Id = Guid.NewGuid(), Order = 1, RewardKarma = 3, RewardXP = 5,
                    NeedToCollectItems = new Dictionary<string, IList<string>>
                    {
                        [game] = new List<string> { collectedIdentity }
                    }
                }
            }
        };
        var provider = CreateProvider();
        var dna = CreateDna();
        ProviderManager.Instance.OASISDNA = dna;
        var manager = new InMemoryQuestManager(provider.Object, avatarId, quest, dna);
        var delta = new QuestProgressDelta { OperationId = operationId, ItemCollectedName = collectedIdentity };

        var first = await manager.ApplyQuestProgressAsync(avatarId, questId, game, delta);
        var replay = await manager.ApplyQuestProgressAsync(avatarId, questId, game, delta);

        first.IsError.Should().BeFalse(first.Message);
        replay.IsError.Should().BeFalse(replay.Message);
        manager.UpdateCount.Should().Be(1);
        replay.Result.QuestCompleted.Should().BeTrue();
        replay.Result.KarmaAwarded.Should().Be(10);
        replay.Result.XPAwarded.Should().Be(16);
        replay.Result.InventoryItemsToGrant.Should().Equal(rewardId);
        replay.Result.CompletedObjectives.Should().ContainSingle();
        quest.Objectives.Single().IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task ReusingOperationIdWithDifferentDeltaReturnsConflictWithoutUpdatingQuestAgain()
    {
        const string game = "Our World";
        var avatarId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var collectedIdentity = "geonft:" + Guid.NewGuid().ToString("D");
        var quest = CreateCompletableQuest(collectedIdentity);
        var provider = CreateProvider();
        var dna = CreateDna();
        ProviderManager.Instance.OASISDNA = dna;
        var manager = new InMemoryQuestManager(provider.Object, avatarId, quest, dna);

        var first = await manager.ApplyQuestProgressAsync(avatarId, quest.Id, game,
            new QuestProgressDelta { OperationId = operationId, ItemCollectedName = collectedIdentity });
        var conflict = await manager.ApplyQuestProgressAsync(avatarId, quest.Id, game,
            new QuestProgressDelta { OperationId = operationId, ItemCollectedName = "different-item" });

        first.IsError.Should().BeFalse(first.Message);
        conflict.IsError.Should().BeTrue();
        conflict.ErrorCode.Should().Be("QUEST_PROGRESS_OPERATION_CONFLICT");
        manager.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task ReusingOperationIdForDifferentGameSourceReturnsConflict()
    {
        var avatarId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var collectedIdentity = "geonft:" + Guid.NewGuid().ToString("D");
        var quest = CreateCompletableQuest(collectedIdentity);
        var provider = CreateProvider();
        var dna = CreateDna();
        ProviderManager.Instance.OASISDNA = dna;
        var manager = new InMemoryQuestManager(provider.Object, avatarId, quest, dna);
        var delta = new QuestProgressDelta { OperationId = operationId, ItemCollectedName = collectedIdentity };

        (await manager.ApplyQuestProgressAsync(avatarId, quest.Id, "Our World", delta)).IsError.Should().BeFalse();
        var conflict = await manager.ApplyQuestProgressAsync(avatarId, quest.Id, "ODOOM", delta);

        conflict.IsError.Should().BeTrue();
        conflict.ErrorCode.Should().Be("QUEST_PROGRESS_OPERATION_CONFLICT");
        manager.UpdateCount.Should().Be(1);
    }

    [Fact]
    public async Task ReplayRecordWithoutPayloadFingerprintFailsClosed()
    {
        var avatarId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var quest = CreateCompletableQuest("geonft:" + Guid.NewGuid().ToString("D"));
        quest.MetaData["Quest.ProgressOperationLedger.v2"] = JsonSerializer.Serialize(
            new Dictionary<Guid, object> { [operationId] = new { QuestCompleted = true } });
        var provider = CreateProvider();
        var dna = CreateDna();
        ProviderManager.Instance.OASISDNA = dna;
        var manager = new InMemoryQuestManager(provider.Object, avatarId, quest, dna);

        var result = await manager.ApplyQuestProgressAsync(avatarId, quest.Id, "Our World",
            new QuestProgressDelta { OperationId = operationId, ItemCollectedName = "anything" });

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("QUEST_PROGRESS_REPLAY_FINGERPRINT_MISSING");
        manager.UpdateCount.Should().Be(0);
    }

    private static Quest CreateCompletableQuest(string collectedIdentity) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Offline replay quest",
        Status = QuestStatus.InProgress,
        Objectives = new List<Objective>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Order = 1,
                NeedToCollectItems = new Dictionary<string, IList<string>>
                {
                    ["Our World"] = new List<string> { collectedIdentity }
                }
            }
        }
    };

    private static Mock<IOASISStorageProvider> CreateProvider()
    {
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.MongoDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.ProviderName = "quest-idempotency-test";
        provider.Object.IsProviderActivated = true;
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        return provider;
    }

    private sealed class InMemoryQuestManager : QuestManager
    {
        private readonly Quest _quest;
        public int UpdateCount { get; private set; }

        public InMemoryQuestManager(IOASISStorageProvider provider, Guid avatarId, Quest quest, OASISDNA dna)
            : base(provider, avatarId, new STARDNA(), dna) => _quest = quest;

        public override Task<OASISResult<Quest>> LoadAsync(Guid avatarId, Guid id, int version = 0,
            HolonType holonType = HolonType.Default, ProviderType providerType = ProviderType.Default) =>
            Task.FromResult(new OASISResult<Quest>(_quest));

        public override Task<OASISResult<Quest>> UpdateAsync(Guid avatarId, Quest holon,
            bool updateDNAJSONFile = false, string STARNETDNAJSONName = "Default",
            ProviderType providerType = ProviderType.Default)
        {
            UpdateCount++;
            return Task.FromResult(new OASISResult<Quest>(holon));
        }
    }

    private static OASISDNA CreateDna() => new()
    {
        OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
        {
            StorageProviders = new StorageProviderSettings
            {
                LogSwitchingProviders = false,
                ActivateProviderTimeOutSeconds = 5
            }
        }
    };
}
