using FluentAssertions;
using Moq;
using System.IdentityModel.Tokens.Jwt;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Services;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Xunit;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed class HyperDriveProviderExecutionTests
{
    [Fact]
    public void RegistrationPreparationUsesTheEmailAndUsernameKeysIndependently()
    {
        const string email = "new-player@example.test";
        const string username = "new-player";
        var provider = CreateActiveProvider();
        provider.Object.ProviderCapabilities = new List<EnumValue<ProviderCategory>>();
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarByEmail(email, 0)).Returns(new OASISResult<IAvatar>());
        provider.Setup(x => x.LoadAvatarByUsername(username, 0)).Returns(new OASISResult<IAvatar>());
        var dna = CreateDna(HyperDriveModes.Legacy);
        var runtime = new ProviderManager(null, dna);
        runtime.RegisterProvider(provider.Object);
        runtime.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new AvatarManager(provider.Object, dna, runtime);
        var method = typeof(AvatarManager).GetMethod("PrepareToRegisterAvatar",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        var result = (OASISResult<IAvatar>)method!.Invoke(manager, new object[]
        {
            "New Player", "New", "Player", email, "Password123!", username,
            AvatarType.User, OASISType.OASISAPIREST
        })!;

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().NotBeNull();
        result.Result.Email.Should().Be(email);
        result.Result.Username.Should().Be(username);
        provider.Verify(x => x.LoadAvatarByEmail(email, 0), Times.Once);
        provider.Verify(x => x.LoadAvatarByUsername(username, 0), Times.Once);
        provider.Verify(x => x.LoadAvatarByUsername(email, It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void SynchronousEmailSendFailsExplicitlyWhenEmailTransportIsNotConfigured()
    {
        string oasisKey = Environment.GetEnvironmentVariable("OASIS_RESEND_KEY");
        string resendKey = Environment.GetEnvironmentVariable("RESEND_API_KEY");

        try
        {
            Environment.SetEnvironmentVariable("OASIS_RESEND_KEY", null);
            Environment.SetEnvironmentVariable("RESEND_API_KEY", null);
            var dna = new OASISDNA
            {
                OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                {
                    Email = new EmailSettings
                    {
                        DisableAllEmails = false,
                        EmailFrom = "noreply@example.test"
                    }
                }
            };
            EmailManager.Initialize(dna);

            Action send = () => EmailManager.Send("player@example.test", "subject", "body");

            send.Should().Throw<InvalidOperationException>()
                .WithMessage("*No Resend API key is configured*");
        }
        finally
        {
            Environment.SetEnvironmentVariable("OASIS_RESEND_KEY", oasisKey);
            Environment.SetEnvironmentVariable("RESEND_API_KEY", resendKey);
        }
    }

    [Fact]
    public void SynchronousProviderActivationUsesTheSynchronousContractOnTheCallingThread()
    {
        var callingThread = Environment.CurrentManagedThreadId;
        var providerThread = 0;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.ActivateProvider())
            .Callback(() => providerThread = Environment.CurrentManagedThreadId)
            .Returns(new OASISResult<bool>(true));
        var runtime = new ProviderManager(null, CreateDna(HyperDriveModes.Legacy));

        OASISResult<bool> result = runtime.ActivateProvider(provider.Object);

        result.Result.Should().BeTrue();
        providerThread.Should().Be(callingThread);
        provider.Verify(x => x.ActivateProvider(), Times.Once);
        provider.Verify(x => x.ActivateProviderAsync(), Times.Never);
    }

    [Fact]
    public async Task AsynchronousProviderActivationAwaitsTheAsynchronousContractOnce()
    {
        var completion = new TaskCompletionSource<OASISResult<bool>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = CreateActiveProvider();
        provider.Setup(x => x.ActivateProviderAsync()).Returns(completion.Task);
        var runtime = new ProviderManager(null, CreateDna(HyperDriveModes.Legacy));

        Task<OASISResult<bool>> pending = runtime.ActivateProviderAsync(provider.Object);
        pending.IsCompleted.Should().BeFalse();
        completion.SetResult(new OASISResult<bool>(true));
        OASISResult<bool> result = await pending;

        result.Result.Should().BeTrue();
        provider.Verify(x => x.ActivateProviderAsync(), Times.Once);
        provider.Verify(x => x.ActivateProvider(), Times.Never);
    }

    [Fact]
    public void SynchronousProviderDeactivationUsesTheSynchronousContractOnTheCallingThread()
    {
        var callingThread = Environment.CurrentManagedThreadId;
        var providerThread = 0;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.DeActivateProvider())
            .Callback(() => providerThread = Environment.CurrentManagedThreadId)
            .Returns(new OASISResult<bool>(true));
        var runtime = new ProviderManager(null, CreateDna(HyperDriveModes.Legacy));

        OASISResult<bool> result = runtime.DeActivateProvider(provider.Object);

        result.Result.Should().BeTrue();
        providerThread.Should().Be(callingThread);
        provider.Verify(x => x.DeActivateProvider(), Times.Once);
        provider.Verify(x => x.DeActivateProviderAsync(), Times.Never);
    }

    [Fact]
    public async Task AsynchronousProviderDeactivationAwaitsTheAsynchronousContractOnce()
    {
        var completion = new TaskCompletionSource<OASISResult<bool>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = CreateActiveProvider();
        provider.Setup(x => x.DeActivateProviderAsync()).Returns(completion.Task);
        var runtime = new ProviderManager(null, CreateDna(HyperDriveModes.Legacy));

        Task<OASISResult<bool>> pending = runtime.DeActivateProviderAsync(provider.Object);
        pending.IsCompleted.Should().BeFalse();
        completion.SetResult(new OASISResult<bool>(true));
        OASISResult<bool> result = await pending;

        result.Result.Should().BeTrue();
        provider.Verify(x => x.DeActivateProviderAsync(), Times.Once);
        provider.Verify(x => x.DeActivateProvider(), Times.Never);
    }

    [Fact]
    public void LegacySynchronousHolonProviderSelectionUsesTheSynchronousProviderOnTheCallingThread()
    {
        var holonId = Guid.NewGuid();
        var holon = new Holon { Id = holonId };
        var callingThread = Environment.CurrentManagedThreadId;
        var providerThread = 0;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolon(holonId, false, false, 3, false, true, 7))
            .Callback(() => providerThread = Environment.CurrentManagedThreadId)
            .Returns(new OASISResult<IHolon>(holon));
        var dna = CreateDna(HyperDriveModes.Legacy);
        var runtime = new ProviderManager(null, dna);
        runtime.RegisterProvider(provider.Object);
        var manager = new HolonManager(provider.Object, dna, runtime);

        var result = manager.LoadHolon(holonId, false, false, 3, false, true, HolonType.All, 7,
            ProviderType.MongoDBOASIS);

        result.Result.Should().BeSameAs(holon);
        providerThread.Should().Be(callingThread);
        provider.Verify(x => x.LoadHolon(holonId, false, false, 3, false, true, 7), Times.Once);
        provider.Verify(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task LegacyAsynchronousHolonProviderSelectionAwaitsTheProviderResultOnce()
    {
        const string providerKey = "legacy-provider-key";
        var holon = new Holon { Id = Guid.NewGuid() };
        var completion = new TaskCompletionSource<OASISResult<IHolon>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = CreateActiveProvider();
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(providerKey, false, false, 4, false, true, 9))
            .Returns(completion.Task);
        var dna = CreateDna(HyperDriveModes.Legacy);
        var runtime = new ProviderManager(null, dna);
        runtime.RegisterProvider(provider.Object);
        var manager = new HolonManager(provider.Object, dna, runtime);

        var pending = manager.LoadHolonAsync(providerKey, false, false, 4, false, true, HolonType.All, 9,
            ProviderType.MongoDBOASIS);
        pending.IsCompleted.Should().BeFalse();
        completion.SetResult(new OASISResult<IHolon>(holon));
        var result = await pending;

        result.Result.Should().BeSameAs(holon);
        provider.Verify(x => x.LoadHolonAsync(providerKey, false, false, 4, false, true, 9), Times.Once);
        provider.Verify(x => x.LoadHolon(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
            It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void LegacySynchronousSearchUsesTheSynchronousProviderOnTheCallingThread()
    {
        ISearchParams search = new SearchParams();
        ISearchResults searchResults = new SearchResults();
        var callingThread = Environment.CurrentManagedThreadId;
        var providerThread = 0;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.Search(search, false, false, 3, false, 7))
            .Callback(() => providerThread = Environment.CurrentManagedThreadId)
            .Returns(new OASISResult<ISearchResults>(searchResults));
        var dna = CreateDna(HyperDriveModes.Legacy);
        var runtime = new ProviderManager(null, dna);
        runtime.RegisterProvider(provider.Object);
        var manager = new SearchManager(provider.Object, dna, runtime);

        var result = manager.Search(search, ProviderType.MongoDBOASIS, loadChildren: false,
            recursive: false, maxChildDepth: 3, continueOnError: false, version: 7);

        result.Result.Should().NotBeNull();
        providerThread.Should().Be(callingThread);
        provider.Verify(x => x.Search(search, false, false, 3, false, 7), Times.Once);
        provider.Verify(x => x.SearchAsync(It.IsAny<ISearchParams>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task LegacyAsynchronousSearchAwaitsTheProviderResultOnce()
    {
        ISearchParams search = new SearchParams();
        ISearchResults searchResults = new SearchResults();
        var completion = new TaskCompletionSource<OASISResult<ISearchResults>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = CreateActiveProvider();
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        provider.Setup(x => x.SearchAsync(search, false, false, 4, false, 9))
            .Returns(completion.Task);
        var dna = CreateDna(HyperDriveModes.Legacy);
        var runtime = new ProviderManager(null, dna);
        runtime.RegisterProvider(provider.Object);
        var manager = new SearchManager(provider.Object, dna, runtime);

        Task<OASISResult<ISearchResults>> pending = manager.SearchAsync(search, ProviderType.MongoDBOASIS,
            loadChildren: false, recursive: false, maxChildDepth: 4, continueOnError: false, version: 9);
        pending.IsCompleted.Should().BeFalse();
        completion.SetResult(new OASISResult<ISearchResults>(searchResults));
        OASISResult<ISearchResults> result = await pending;

        result.Result.Should().NotBeNull();
        provider.Verify(x => x.SearchAsync(search, false, false, 4, false, 9), Times.Once);
        provider.Verify(x => x.Search(It.IsAny<ISearchParams>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DefaultAsynchronousHolonVisibilityProjectionPreservesDiagnosticsAndMaterializesTheProviderResult()
    {
        var avatarId = Guid.NewGuid();
        var owned = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = avatarId };
        var publicHolon = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid(), IsPublic = true };
        var privateHolon = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = Guid.NewGuid() };
        var source = new OASISResult<IEnumerable<IHolon>>(new[] { owned, publicHolon, privateHolon })
        {
            IsWarning = true,
            WarningCount = 1,
            DetailedMessage = "authoritative Holon collection detail",
            MetaData = new Dictionary<string, string> { ["provider"] = "visibility-projection" },
            LoadedCount = 3
        };
        var provider = new Mock<IOASISStorageProvider> { CallBase = true };
        provider.Setup(x => x.LoadHolonsForParentAsync(owned.Id, HolonType.Quest, false, false, 2, 1,
                false, true, 7))
            .ReturnsAsync(source);

        IOASISStorageProvider contract = provider.Object;
        var result = await contract.LoadHolonsForParentAsync(owned.Id, avatarId, true, HolonType.Quest,
            false, false, 2, 1, false, true, 7);

        result.Result.Should().BeAssignableTo<IReadOnlyCollection<IHolon>>();
        result.Result.Should().Equal(owned, publicHolon);
        result.IsWarning.Should().BeTrue();
        result.WarningCount.Should().Be(1);
        result.LoadedCount.Should().Be(3);
        result.DetailedMessage.Should().Be("authoritative Holon collection detail");
        result.MetaData["provider"].Should().Be("visibility-projection");
        provider.Verify(x => x.LoadHolonsForParentAsync(owned.Id, HolonType.Quest, false, false, 2, 1,
            false, true, 7), Times.Once);
        provider.Verify(x => x.LoadHolonsForParent(It.IsAny<Guid>(), It.IsAny<HolonType>(), It.IsAny<bool>(),
            It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void DefaultSynchronousHolonVisibilityProjectionFailsClosedWithoutACollectionPayload()
    {
        var avatarId = Guid.NewGuid();
        var source = new OASISResult<IEnumerable<IHolon>>
        {
            DetailedMessage = "provider returned no Holon collection payload",
            MetaData = new Dictionary<string, string> { ["provider"] = "sync-visibility-projection" }
        };
        var provider = new Mock<IOASISStorageProvider> { CallBase = true };
        provider.Setup(x => x.LoadAllHolons(HolonType.All, false, false, 0, 0, false, true, 4))
            .Returns(source);

        IOASISStorageProvider contract = provider.Object;
        var result = contract.LoadAllHolons(avatarId, false, HolonType.All, false, false, 0, 0, false, true, 4);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HOLON_COLLECTION_PAYLOAD_REQUIRED");
        result.ErrorCount.Should().BeGreaterThan(0);
        result.DetailedMessage.Should().Be("provider returned no Holon collection payload");
        result.MetaData["provider"].Should().Be("sync-visibility-projection");
        provider.Verify(x => x.LoadAllHolons(HolonType.All, false, false, 0, 0, false, true, 4), Times.Once);
        provider.Verify(x => x.LoadAllHolonsAsync(It.IsAny<HolonType>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void DefaultSynchronousAvatarDetailDeleteNeverCrossesTheAsyncProviderBoundary()
    {
        var detail = new AvatarDetail { Id = Guid.NewGuid(), Username = "sync-delete", IsActive = true };
        var saved = new OASISResult<IAvatarDetail>(detail)
        {
            IsWarning = true,
            WarningCount = 1,
            DetailedMessage = "authoritative synchronous delete detail",
            MetaData = new Dictionary<string, string> { ["provider"] = "sync-avatar-detail-delete" }
        };
        var provider = new Mock<IOASISStorageProvider> { CallBase = true };
        provider.Setup(x => x.LoadAvatarDetailByUsername("sync-delete", 0))
            .Returns(new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.SaveAvatarDetail(detail)).Returns(saved);

        var result = provider.Object.DeleteAvatarDetailByUsername("sync-delete");

        result.Result.Should().BeTrue();
        result.IsDeleted.Should().BeTrue();
        result.IsWarning.Should().BeTrue();
        result.DetailedMessage.Should().Be("authoritative synchronous delete detail");
        result.MetaData["provider"].Should().Be("sync-avatar-detail-delete");
        detail.IsActive.Should().BeFalse();
        detail.DeletedDate.Should().BeAfter(DateTime.MinValue);
        provider.Verify(x => x.LoadAvatarDetailByUsernameAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        provider.Verify(x => x.SaveAvatarDetailAsync(It.IsAny<IAvatarDetail>()), Times.Never);
    }

    [Fact]
    public async Task DefaultAsynchronousAvatarDetailDeletePreservesSaveDiagnosticsAndAvoidsASecondLookup()
    {
        var detail = new AvatarDetail { Id = Guid.NewGuid(), Email = "async-delete@example.test", IsActive = true };
        var saved = new OASISResult<IAvatarDetail>(detail)
        {
            IsWarning = true,
            WarningCount = 2,
            DetailedMessage = "authoritative asynchronous delete detail",
            InnerMessages = new List<string> { "secondary delete projection pending" },
            DeletedCount = 1
        };
        var provider = new Mock<IOASISStorageProvider> { CallBase = true };
        provider.Setup(x => x.LoadAvatarDetailByEmailAsync("async-delete@example.test", 0))
            .ReturnsAsync(new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.SaveAvatarDetailAsync(detail)).ReturnsAsync(saved);

        var result = await provider.Object.DeleteAvatarDetailByEmailAsync("async-delete@example.test");

        result.Result.Should().BeTrue();
        result.IsDeleted.Should().BeTrue();
        result.WarningCount.Should().Be(2);
        result.DeletedCount.Should().Be(1);
        result.DetailedMessage.Should().Be("authoritative asynchronous delete detail");
        result.InnerMessages.Should().Contain("secondary delete projection pending");
        provider.Verify(x => x.LoadAvatarDetailByEmailAsync("async-delete@example.test", 0), Times.Once);
        provider.Verify(x => x.LoadAvatarDetailAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        provider.Verify(x => x.SaveAvatarDetailAsync(detail), Times.Once);
    }

    [Fact]
    public async Task DefaultAvatarLookupContractsPreserveDiagnosticsAndAwaitTheAsyncProviderBoundary()
    {
        var avatar = new Avatar
        {
            Id = Guid.NewGuid(),
            VerificationToken = "verification-token",
            ResetToken = "reset-token",
            RefreshTokens = new List<RefreshToken> { new() { Token = "refresh-token" } },
            ProviderWallets = new Dictionary<ProviderType, List<IProviderWallet>>
            {
                [ProviderType.EthereumOASIS] = new()
                {
                    new ProviderWallet { PublicKey = "public-key", PrivateKey = "private-key" }
                }
            }
        };
        var source = new OASISResult<IEnumerable<IAvatar>>(new[] { avatar })
        {
            IsWarning = true,
            WarningCount = 2,
            DetailedMessage = "authoritative avatar collection detail",
            InnerMessages = new List<string> { "secondary avatar index pending" },
            MetaData = new Dictionary<string, string> { ["provider"] = "default-avatar-lookup" },
            LoadedCount = 1
        };
        var provider = new Mock<IOASISStorageProvider> { CallBase = true };
        provider.Setup(x => x.LoadAllAvatarsAsync(7)).ReturnsAsync(source);

        IOASISStorageProvider contract = provider.Object;
        var results = new[]
        {
            await contract.LoadAvatarByVerificationTokenAsync("verification-token", 7),
            await contract.LoadAvatarByResetTokenAsync("reset-token", 7),
            await contract.LoadAvatarByRefreshTokenAsync("refresh-token", 7),
            await contract.LoadAvatarByPublicKeyAsync("public-key", 7),
            await contract.LoadAvatarByPrivateKeyAsync("private-key", 7)
        };

        results.Should().OnlyContain(result => result.Result == avatar && result.IsWarning &&
            result.WarningCount == 2 && result.LoadedCount == 1 &&
            result.DetailedMessage == "authoritative avatar collection detail" &&
            result.InnerMessages.Contains("secondary avatar index pending") &&
            result.MetaData["provider"] == "default-avatar-lookup");
        provider.Verify(x => x.LoadAllAvatarsAsync(7), Times.Exactly(5));
        provider.Verify(x => x.LoadAllAvatars(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void DefaultSynchronousAvatarLookupUsesOnlyTheSynchronousProviderBoundaryAndFailsClosedWithoutPayload()
    {
        var source = new OASISResult<IEnumerable<IAvatar>>
        {
            DetailedMessage = "provider returned no collection payload",
            MetaData = new Dictionary<string, string> { ["provider"] = "sync-default-avatar-lookup" }
        };
        var provider = new Mock<IOASISStorageProvider> { CallBase = true };
        provider.Setup(x => x.LoadAllAvatars(4)).Returns(source);

        IOASISStorageProvider contract = provider.Object;
        var result = contract.LoadAvatarByVerificationToken("missing", 4);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("AVATAR_COLLECTION_PAYLOAD_REQUIRED");
        result.ErrorCount.Should().BeGreaterThan(0);
        result.DetailedMessage.Should().Be("provider returned no collection payload");
        result.MetaData["provider"].Should().Be("sync-default-avatar-lookup");
        provider.Verify(x => x.LoadAllAvatars(4), Times.Once);
        provider.Verify(x => x.LoadAllAvatarsAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void AvatarJwtIssuanceUsesTheManagersRuntimeDna()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.Security = new SecuritySettings
        {
            SecretKey = new string('e', 64),
            JwtTokenExpirationMinutes = 21,
            Oidc = new OidcSettings { Issuer = "edge-runtime" }
        };
        var providerManager = new ProviderManager(null, dna);
        var manager = new AvatarManager(null, dna, providerManager);
        var avatar = new Avatar { Id = Guid.NewGuid(), Email = "edge@example.test", FirstName = "Edge" };

        var token = manager.GenerateJWTToken(avatar);
        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        parsed.Issuer.Should().Be("edge-runtime");
        manager.OASISDNA.Should().BeSameAs(dna);
        providerManager.OASISDNA.Should().BeSameAs(dna);
    }

    [Fact]
    public void IsolatedAvatarRuntimeFailsClosedWhenItsJwtKeyIsMissing()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.Security = new SecuritySettings();
        var providerManager = new ProviderManager(null, dna);
        var manager = new AvatarManager(null, dna, providerManager);

        var action = () => manager.GenerateJWTToken(new Avatar { Id = Guid.NewGuid() });

        action.Should().Throw<ArgumentNullException>()
            .WithMessage("*isolated AvatarManager runtime must be configured with its own JWT signing key*");
        manager.OASISDNA.Should().BeSameAs(dna);
        providerManager.OASISDNA.Should().BeSameAs(dna);
    }

    [Fact]
    public void V2ManagerProviderConfigurationDoesNotReplaceTheCurrentProvider()
    {
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var candidate = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "candidate-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var providerManager = new ProviderManager(null, new OASISDNA
        {
            OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false }
            }
        });
        providerManager.RegisterProvider(current.Object);
        var currentResult = providerManager.SetAndActivateCurrentStorageProvider(current.Object);
        currentResult.IsError.Should().BeFalse(currentResult.Message);

        OASISManager.ConfigureStorageProvider(providerManager, candidate.Object, useHyperDriveV2: true);

        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        providerManager.CurrentStorageProviderType.Value.Should().Be(ProviderType.MongoDBOASIS);
        providerManager.IsProviderRegistered(ProviderType.SQLLiteDBOASIS).Should().BeTrue();
        candidate.Object.IsProviderActivated.Should().BeTrue();
    }

    [Fact]
    public void InjectedManagerRegistersItsProviderOnlyInItsOwnRuntime()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var isolated = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-ipfs");
        var singletonProviderBefore = ProviderManager.Instance.GetStorageProvider(ProviderType.IPFSOASIS);

        var manager = new HolonManager(provider.Object, dna, isolated);

        manager.OASISDNA.Should().BeSameAs(dna);
        isolated.GetStorageProvider(ProviderType.IPFSOASIS).Should().BeSameAs(provider.Object);
        ProviderManager.Instance.GetStorageProvider(ProviderType.IPFSOASIS).Should().BeSameAs(singletonProviderBefore);
    }

    [Fact]
    public async Task V2ManagerUsesItsInjectedProviderWhenTheOperationProviderIsDefault()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var injected = CreateActiveProvider(ProviderType.IPFSOASIS, "injected-ipfs");
        var holonId = Guid.NewGuid();
        var holon = new Mock<IHolon>().Object;
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        injected.Setup(x => x.LoadHolonAsync(holonId, true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon> { Result = holon });
        providerManager.RegisterProvider(current.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();

        var manager = new HolonManager(injected.Object, dna, providerManager);
        var result = await manager.LoadHolonAsync(holonId);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holon);
        injected.Verify(x => x.LoadHolonAsync(holonId, true, true, 0, true, false, 0), Times.Once);
        current.Verify(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
        providerManager.CurrentStorageProviderType.Value.Should().Be(ProviderType.MongoDBOASIS);
    }

    [Fact]
    public async Task TypedProviderKeyLoadPreservesCompleteV2ProviderFailure()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false };
        var provider = CreateActiveProvider(ProviderType.MongoDBOASIS, "typed-provider-key");
        var providerFailure = new OASISResult<IHolon>
        {
            IsError = true,
            IsWarning = true,
            ErrorCode = "HOLON_PROVIDER_KEY_REJECTED",
            Message = "provider rejected key",
            DetailedMessage = "authoritative provider detail",
            ErrorCount = 2,
            WarningCount = 1,
            InnerMessages = new List<string> { "inner provider diagnostic" },
            MetaData = new Dictionary<string, string> { ["provider"] = "typed-provider-key" }
        };
        provider.Setup(x => x.LoadHolonAsync("provider-key", true, true, 0, true, false, 0))
            .ReturnsAsync(providerFailure);

        var manager = new HolonManager(provider.Object, dna, providerManager);
        var result = await manager.LoadHolonAsync<Holon>("provider-key");

        result.IsError.Should().BeTrue();
        result.IsWarning.Should().BeTrue();
        result.ErrorCode.Should().Be(providerFailure.ErrorCode);
        result.Message.Should().Be(providerFailure.Message);
        result.DetailedMessage.Should().Be(providerFailure.DetailedMessage);
        result.ErrorCount.Should().Be(2);
        result.WarningCount.Should().Be(1);
        result.InnerMessages.Should().Equal(providerFailure.InnerMessages);
        result.MetaData.Should().Contain("provider", "typed-provider-key");
        result.Result.Should().BeNull();
        provider.Verify(x => x.LoadHolonAsync("provider-key", true, true, 0, true, false, 0), Times.Once);
    }

    [Fact]
    public void LegacyAvatarKarmaUsesOnlyTheManagersInjectedRuntime()
    {
        var avatar = new AvatarDetail { Id = Guid.NewGuid() };
        var record = new KarmaAkashicRecord();
        var dna = CreateDna(HyperDriveModes.Legacy);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "isolated-karma-sqlite");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.AddKarmaToAvatar(
                avatar, KarmaTypePositive.HelpOtherPerson, KarmaSourceType.Game,
                "quest", "helped another player", null))
            .Returns(new OASISResult<KarmaAkashicRecord>(record));
        provider.Setup(x => x.RemoveKarmaFromAvatar(
                avatar, KarmaTypeNegative.DropLitter, KarmaSourceType.Game,
                "quest", "dropped litter", null))
            .Returns(new OASISResult<KarmaAkashicRecord>(record));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new AvatarManager(null, dna, providerManager);

        var result = manager.AddKarmaToAvatar(
            avatar, KarmaTypePositive.HelpOtherPerson, KarmaSourceType.Game,
            "quest", "helped another player", providerType: ProviderType.SQLLiteDBOASIS);
        var removed = manager.RemoveKarmaFromAvatar(
            avatar, KarmaTypeNegative.DropLitter, KarmaSourceType.Game,
            "quest", "dropped litter", providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(record);
        removed.Should().BeSameAs(record);
        providerManager.CurrentStorageProvider.Should().BeSameAs(provider.Object);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.VerifyAll();
    }

    [Fact]
    public async Task AvatarDetailLegacyReplicationCompletesInsideTheOperationAndUsesInjectedRuntime()
    {
        var avatar = new AvatarDetail { Id = Guid.NewGuid(), Username = "edge-player" };
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "isolated-current-mongo");
        var replica = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "isolated-replica-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        current.Setup(x => x.SaveAvatarDetailAsync(avatar))
            .ReturnsAsync(new OASISResult<IAvatarDetail>(avatar) { IsSaved = true });
        replica.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        replica.Setup(x => x.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        replica.Setup(x => x.SaveAvatarDetailAsync(avatar))
            .ReturnsAsync(new OASISResult<IAvatarDetail>(avatar) { IsSaved = true });
        var dna = CreateDna(HyperDriveModes.Legacy);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object).Should().BeTrue();
        providerManager.RegisterProvider(replica.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        providerManager.SetAutoReplicationForProviders(true,
            new[] { ProviderType.SQLLiteDBOASIS }).Should().BeTrue();
        providerManager.IsAutoLoadBalanceEnabled = false;
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new AvatarManager(null, dna, providerManager);
        var result = await manager.SaveAvatarDetailAsync(avatar, AutoReplicationMode.True,
            waitForAutoReplicationResult: false, providerType: ProviderType.MongoDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        current.Verify(x => x.SaveAvatarDetailAsync(avatar), Times.Once);
        replica.Verify(x => x.SaveAvatarDetailAsync(avatar), Times.Once);
    }

    [Fact]
    public async Task HolonLegacyReplicationUsesInjectedRuntimeForPolicyAndProviders()
    {
        var avatarId = Guid.NewGuid();
        var holon = new Holon { Id = Guid.NewGuid(), Name = "replicated-edge-holon" };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "isolated-holon-primary");
        var replica = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "isolated-holon-replica");
        primary.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        primary.Setup(x => x.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        replica.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        replica.Setup(x => x.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        primary.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon value, bool _, bool _, int _, bool _, bool _) =>
                new OASISResult<IHolon>(value) { IsSaved = true });
        replica.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon value, bool _, bool _, int _, bool _, bool _) =>
                new OASISResult<IHolon>(value) { IsSaved = true });
        var dna = CreateDna(HyperDriveModes.Legacy);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(primary.Object).Should().BeTrue();
        providerManager.RegisterProvider(replica.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        providerManager.SetAutoReplicationForProviders(true,
            new[] { ProviderType.SQLLiteDBOASIS }).Should().BeTrue();
        providerManager.IsAutoLoadBalanceEnabled = false;
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;

        var result = await new HolonManager(null, dna, providerManager).SaveHolonAsync(
            holon, avatarId, providerType: ProviderType.MongoDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        primary.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
        replica.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public async Task HolonLegacyLoadBalancingWritesToTheSelectedInjectedProviderWithoutChangingTheRuntimeDefault()
    {
        var avatarId = Guid.NewGuid();
        var holon = new Holon { Id = Guid.NewGuid(), Name = "load-balanced-legacy-holon" };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "legacy-load-balance-primary");
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "legacy-load-balance-selected");
        primary.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        primary.Setup(x => x.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        selected.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        primary.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon value, bool _, bool _, int _, bool _, bool _) =>
                new OASISResult<IHolon>(value) { IsSaved = true });
        selected.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon value, bool _, bool _, int _, bool _, bool _) =>
                new OASISResult<IHolon>(value) { IsSaved = true });
        var dna = CreateDna(HyperDriveModes.Legacy);
        var providerManager = new ProviderManager(null, dna)
        {
            IsAutoFailOverEnabled = false,
            IsAutoReplicationEnabled = false,
            IsAutoLoadBalanceEnabled = true
        };
        providerManager.RegisterProvider(primary.Object).Should().BeTrue();
        providerManager.RegisterProvider(selected.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        providerManager.SetAndReplaceAutoLoadBalanceListForProviders(new[]
        {
            new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)
        }).IsError.Should().BeFalse();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;

        var result = await new HolonManager(null, dna, providerManager).SaveHolonAsync(
            holon, avatarId, providerType: ProviderType.MongoDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.InnerMessages.Should().Contain(message => message.Contains("Auto-load balanced to SQLLiteDBOASIS"));
        providerManager.LastProviderSelectionDiagnostic.SelectedProvider.Should().Be(ProviderType.SQLLiteDBOASIS);
        providerManager.CurrentStorageProvider.Should().BeSameAs(primary.Object);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        primary.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
        selected.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public void HolonLegacyHardDeleteReplicatesToTheConfiguredInjectedProvider()
    {
        var avatarId = Guid.NewGuid();
        var holon = new Holon { Id = Guid.NewGuid(), Name = "deleted-edge-holon" };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "isolated-delete-primary");
        var replica = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "isolated-delete-replica");
        primary.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        replica.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        primary.Setup(x => x.DeleteHolon(holon.Id)).Returns(new OASISResult<IHolon>(holon));
        replica.Setup(x => x.DeleteHolon(holon.Id)).Returns(new OASISResult<IHolon>(holon));
        var dna = CreateDna(HyperDriveModes.Legacy);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(primary.Object).Should().BeTrue();
        providerManager.RegisterProvider(replica.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        providerManager.SetAutoReplicationForProviders(true,
            new[] { ProviderType.SQLLiteDBOASIS }).Should().BeTrue();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;

        var result = new HolonManager(null, dna, providerManager).DeleteHolon(
            holon.Id, avatarId, softDelete: false, providerType: ProviderType.MongoDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        primary.Verify(x => x.DeleteHolon(holon.Id), Times.Once);
        replica.Verify(x => x.DeleteHolon(holon.Id), Times.Once);
    }

    [Fact]
    public async Task FilesManagerUsesItsInjectedRuntimeWithoutTouchingTheSingleton()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-files");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
                new OASISResult<IHolon>(holon) { IsSaved = true });
        providerManager.RegisterProvider(provider.Object);
        var selected = providerManager.SetAndActivateCurrentStorageProvider(provider.Object);
        selected.IsError.Should().BeFalse(selected.Message);
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new FilesManager(null, dna, providerManager);

        var result = await manager.UploadFileAsync(avatarId, "offline.dat", new byte[] { 1, 2, 3 },
            "application/octet-stream");

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().NotBeNull();
        result.Result.AvatarId.Should().Be(avatarId);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public async Task DomainManagerChildPreservesTheExplicitProviderInAMultiProviderV2Runtime()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var runtimeDefault = CreateActiveProvider(ProviderType.MongoDBOASIS, "domain-default");
        var explicitlySelected = CreateActiveProvider(ProviderType.IPFSOASIS, "domain-explicit");
        runtimeDefault.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        explicitlySelected.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        runtimeDefault.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
                new OASISResult<IHolon>(holon) { IsSaved = true });
        explicitlySelected.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
                new OASISResult<IHolon>(holon) { IsSaved = true });
        providerManager.RegisterProvider(runtimeDefault.Object).Should().BeTrue();
        providerManager.RegisterProvider(explicitlySelected.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(runtimeDefault.Object).IsError.Should().BeFalse();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;

        var result = await new FilesManager(explicitlySelected.Object, dna, providerManager)
            .UploadFileAsync(avatarId, "explicit-provider.dat", new byte[] { 4, 2 },
                "application/octet-stream");

        result.IsError.Should().BeFalse(result.Message);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        explicitlySelected.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
        runtimeDefault.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Never);
    }

    [Fact]
    public async Task SettingsAndMessagingChildrenPreserveTheirExplicitProviderInAMultiProviderV2Runtime()
    {
        var avatarId = Guid.NewGuid();
        var avatar = new Avatar
        {
            Id = avatarId,
            MetaData = new Dictionary<string, object>
            {
                ["preferences"] = new Dictionary<string, object>
                {
                    ["system"] = new Dictionary<string, object> { ["theme"] = "explicit-edge" }
                }
            }
        };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var runtimeDefault = CreateActiveProvider(ProviderType.MongoDBOASIS, "children-default");
        var explicitlySelected = CreateActiveProvider(ProviderType.IPFSOASIS, "children-explicit");
        runtimeDefault.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        explicitlySelected.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        explicitlySelected.Setup(x => x.LoadAvatarAsync(avatarId, 0))
            .ReturnsAsync(new OASISResult<IAvatar>(avatar));
        explicitlySelected.Setup(x => x.LoadHolonsByMetaDataAsync(It.IsAny<string>(), avatarId.ToString(),
                It.IsAny<HolonType>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(new List<IHolon>()));
        providerManager.RegisterProvider(runtimeDefault.Object).Should().BeTrue();
        providerManager.RegisterProvider(explicitlySelected.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(runtimeDefault.Object).IsError.Should().BeFalse();

        var settings = await new SettingsManager(explicitlySelected.Object, dna, providerManager)
            .GetSystemSettingsAsync(avatarId);
        var messages = await new MessagingManager(explicitlySelected.Object, dna, providerManager)
            .GetMessagesAsync(avatarId);

        settings.IsError.Should().BeFalse(settings.Message);
        settings.Result["theme"].Should().Be("explicit-edge");
        messages.IsError.Should().BeFalse(messages.Message);
        messages.Result.Should().BeEmpty();
        explicitlySelected.Verify(x => x.LoadAvatarAsync(avatarId, 0), Times.Once);
        explicitlySelected.Verify(x => x.LoadHolonsByMetaDataAsync(It.IsAny<string>(), avatarId.ToString(),
            It.IsAny<HolonType>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Exactly(2));
        runtimeDefault.Verify(x => x.LoadAvatarAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        runtimeDefault.Verify(x => x.LoadHolonsByMetaDataAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<HolonType>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task FilesManagerDoesNotTurnAProviderFailureIntoAnEmptyFileList()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "files-read-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonsByMetaDataAsync("CreatedByAvatarId", avatarId.ToString(),
                It.IsAny<HolonType>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>
            {
                IsError = true, ErrorCount = 1, ErrorCode = "FILE_STORE_UNAVAILABLE",
                Message = "file store unavailable"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new FilesManager(provider.Object, dna, providerManager)
            .GetAllFilesForAvatarAsync(avatarId);

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.ErrorCode.Should().Be("HYPERDRIVE_FAILOVER_EXHAUSTED");
        result.Message.Should().Contain("unavailable");
    }

    [Fact]
    public async Task RejectedFileMetadataUpdateDoesNotLeakThroughProviderObjectAlias()
    {
        var avatarId = Guid.NewGuid();
        var fileId = Guid.NewGuid();
        var stored = new Holon
        {
            Id = fileId,
            MetaData = new Dictionary<string, object>
            {
                ["oasisFileType"] = "file",
                ["fileName"] = "before.dat"
            }
        };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "files-alias-rejection");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(fileId, true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(stored));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>
            {
                IsError = true, ErrorCount = 1, ErrorCode = "FILE_UPDATE_REJECTED",
                Message = "file update rejected"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new FilesManager(provider.Object, dna, providerManager)
            .UpdateFileMetadataAsync(avatarId, fileId,
                new Dictionary<string, object> { ["fileName"] = "after.dat" });

        result.IsError.Should().BeTrue();
        result.Result.Should().BeFalse();
        stored.MetaData["fileName"].Should().Be("before.dat");
        provider.Verify(x => x.SaveHolonAsync(
            It.Is<IHolon>(h => h.MetaData["fileName"].ToString() == "after.dat"),
            true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public async Task SettingsManagerReadsThroughItsInjectedRuntimeWithoutTouchingTheSingleton()
    {
        var avatarId = Guid.NewGuid();
        var avatar = new Avatar
        {
            Id = avatarId,
            MetaData = new Dictionary<string, object>
            {
                ["preferences"] = new Dictionary<string, object>
                {
                    ["system"] = new Dictionary<string, object> { ["theme"] = "edge-neon" }
                }
            }
        };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-settings");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarAsync(avatarId, 0)).ReturnsAsync(new OASISResult<IAvatar>(avatar));
        providerManager.RegisterProvider(provider.Object);
        var selected = providerManager.SetAndActivateCurrentStorageProvider(provider.Object);
        selected.IsError.Should().BeFalse(selected.Message);
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new SettingsManager(null, dna, providerManager);

        var result = await manager.GetSystemSettingsAsync(avatarId);

        result.IsError.Should().BeFalse(result.Message);
        result.Result["theme"].Should().Be("edge-neon");
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.Verify(x => x.LoadAvatarAsync(avatarId, 0), Times.Once);
    }

    [Fact]
    public async Task SettingsManagerDoesNotReportSuccessWhenItsInjectedProviderRejectsTheSave()
    {
        var avatarId = Guid.NewGuid();
        var avatar = new Avatar { Id = avatarId, MetaData = new Dictionary<string, object>() };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-settings-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarAsync(avatarId, 0)).ReturnsAsync(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.SaveAvatarAsync(avatar)).ReturnsAsync(new OASISResult<IAvatar>
        {
            IsError = true, ErrorCount = 1, ErrorCode = "PROVIDER_WRITE_REJECTED", Message = "write rejected"
        });
        providerManager.RegisterProvider(provider.Object);
        var selected = providerManager.SetAndActivateCurrentStorageProvider(provider.Object);
        selected.IsError.Should().BeFalse(selected.Message);
        var manager = new SettingsManager(null, dna, providerManager);

        var result = await manager.UpdateSystemSettingsAsync(avatarId,
            new Dictionary<string, object> { ["theme"] = "edge-neon" });

        result.IsError.Should().BeTrue();
        result.Result.Should().BeFalse();
        result.Message.Should().Contain("failed");
        provider.Verify(x => x.SaveAvatarAsync(avatar), Times.Once);
        provider.Verify(x => x.SaveAvatar(It.IsAny<IAvatar>()), Times.Never);
        provider.Verify(x => x.LoadAvatar(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task SettingsManagerDoesNotReplaceAProviderReadFailureWithDefaultSettings()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-settings-read-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarAsync(avatarId, 0)).ReturnsAsync(new OASISResult<IAvatar>
        {
            IsError = true,
            ErrorCount = 1,
            ErrorCode = "PROVIDER_UNAVAILABLE",
            Message = "provider unavailable"
        });
        providerManager.RegisterProvider(provider.Object);
        var selected = providerManager.SetAndActivateCurrentStorageProvider(provider.Object);
        selected.IsError.Should().BeFalse(selected.Message);
        var manager = new SettingsManager(null, dna, providerManager);

        var result = await manager.GetSystemSettingsAsync(avatarId);

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Message.Should().Contain("failed");
        provider.Verify(x => x.LoadAvatarAsync(avatarId, 0), Times.Once);
        provider.Verify(x => x.LoadAvatar(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ClanManagerLoadsThroughItsInjectedRuntimeWithoutTouchingTheSingleton()
    {
        var clanId = Guid.NewGuid();
        var clan = new Clan { Id = clanId, Name = "Edge Clan", OwnerAvatarId = Guid.NewGuid() };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-clans");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(clanId, false, false, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(clan));
        providerManager.RegisterProvider(provider.Object);
        var selected = providerManager.SetAndActivateCurrentStorageProvider(provider.Object);
        selected.IsError.Should().BeFalse(selected.Message);
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new ClanManager(providerManager);

        var result = await manager.LoadClanAsync(clanId);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Id.Should().Be(clanId);
        result.Result.Name.Should().Be("Edge Clan");
        result.Result.OwnerAvatarId.Should().Be(clan.OwnerAvatarId);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.Verify(x => x.LoadHolonAsync(clanId, false, false, 0, true, false, 0), Times.Once);
    }

    [Fact]
    public async Task ClanStateRoundTripsWhenAProviderPersistsOnlyTheBaseHolonContract()
    {
        var clanId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var memberId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var clan = new Clan
        {
            Id = clanId,
            Name = "Portable Clan",
            OwnerAvatarId = ownerId,
            MemberIds = new List<Guid> { ownerId, memberId },
            Inventory = new List<IInventoryItem>
            {
                new InventoryItem
                {
                    Id = itemId, Name = "Holo Crystal", Quantity = 3, Stack = true,
                    GameSource = "Our World", ItemType = InventoryItemType.QuestItem,
                    Properties = new Dictionary<string, object> { ["rarity"] = "purple" }
                }
            }
        };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "base-holon-clan-roundtrip");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon saved, bool _, bool _, int _, bool _, bool _) =>
                new OASISResult<IHolon>(new Holon
                {
                    Id = saved.Id, Name = saved.Name, Description = saved.Description,
                    HolonType = saved.HolonType,
                    MetaData = new Dictionary<string, object>(saved.MetaData)
                }));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new ClanManager(providerManager).UpdateClanAsync(clan);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.OwnerAvatarId.Should().Be(ownerId);
        result.Result.MemberIds.Should().Equal(ownerId, memberId);
        result.Result.Inventory.Should().ContainSingle();
        result.Result.Inventory[0].Id.Should().Be(itemId);
        result.Result.Inventory[0].Quantity.Should().Be(3);
        result.Result.Inventory[0].Properties["rarity"].ToString().Should().Be("purple");
    }

    [Fact]
    public async Task BaseHolonClanWithoutCanonicalStateFailsClosed()
    {
        var clanId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "missing-clan-state");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(clanId, false, false, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(new Holon
            {
                Id = clanId, Name = "Incomplete Clan", HolonType = HolonType.Clan
            }));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new ClanManager(providerManager).LoadClanAsync(clanId);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("CLAN_STATE_INVALID");
        result.Result.Should().BeNull();
        result.Message.Should().Contain("OASIS.Clan.State.v1");
    }

    [Fact]
    public async Task AvatarClanInventoryLookupCannotEscapeItsInjectedRuntime()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-avatar-clan");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAllHolonsAsync(It.IsAny<HolonType>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>
            {
                IsError = true, ErrorCount = 1, ErrorCode = "ISOLATED_CLAN_STORE_OFFLINE",
                Message = "injected clan store unavailable"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;

        var result = await new AvatarManager(null, dna, providerManager).SendItemToClanAsync(
            Guid.NewGuid(), "Edge Clan", "Crystal", providerType: ProviderType.IPFSOASIS);

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("injected clan store unavailable");
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.Verify(x => x.LoadAllHolonsAsync(It.IsAny<HolonType>(), It.IsAny<bool>(),
            It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(),
            It.IsAny<bool>(), It.IsAny<int>()), Times.Once);
    }

    [Fact]
    public async Task RejectedClanMembershipWriteDoesNotLeakThroughProviderObjectAlias()
    {
        var clanId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var candidateId = Guid.NewGuid();
        var stored = new Clan
        {
            Id = clanId,
            OwnerAvatarId = ownerId,
            MemberIds = new List<Guid> { ownerId }
        };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "clan-alias-rejection");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(clanId, false, false, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(stored));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>
            {
                IsError = true, ErrorCount = 1, ErrorCode = "CLAN_WRITE_REJECTED",
                Message = "clan write rejected"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new ClanManager(providerManager).AddAvatarToClanAsync(clanId, candidateId);

        result.IsError.Should().BeTrue();
        stored.MemberIds.Should().Equal(ownerId);
        provider.Verify(x => x.SaveHolonAsync(
            It.Is<IHolon>(h => ((IClan)h).MemberIds.Contains(candidateId)),
            true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public async Task RejectedClanInventorySenderWriteDoesNotLeakThroughProviderObjectAlias()
    {
        var clanId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var item = new InventoryItem { Id = Guid.NewGuid(), Name = "Crystal" };
        var storedDetail = new AvatarDetail
        {
            Id = senderId,
            Inventory = new List<IInventoryItem> { item }
        };
        var storedClan = new Clan { Id = clanId, OwnerAvatarId = Guid.NewGuid() };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "clan-sender-write-rejection");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(clanId, false, false, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(storedClan));
        provider.Setup(x => x.LoadAvatarDetailAsync(senderId, 0))
            .ReturnsAsync(new OASISResult<IAvatarDetail>(storedDetail));
        provider.Setup(x => x.SaveAvatarDetailAsync(It.IsAny<IAvatarDetail>()))
            .ReturnsAsync(new OASISResult<IAvatarDetail>
            {
                IsError = true, ErrorCount = 1, ErrorCode = "AVATAR_WRITE_REJECTED",
                Message = "avatar write rejected"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new ClanManager(providerManager).SendItemToClanAsync(
            senderId, clanId, item.Name, itemId: item.Id, providerType: ProviderType.IPFSOASIS);

        result.IsError.Should().BeTrue();
        storedDetail.Inventory.Should().ContainSingle().Which.Should().BeSameAs(item);
        provider.Verify(x => x.SaveAvatarDetailAsync(
            It.Is<IAvatarDetail>(d => d.Inventory.Count == 0)), Times.Once);
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Never);
    }

    [Fact]
    public async Task AtomicClanTransferPreservesClientOperationAndDestinationIdentities()
    {
        var senderId = Guid.NewGuid();
        var clanId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var destinationId = Guid.NewGuid();
        var storedDetail = new AvatarDetail
        {
            Id = senderId,
            Inventory = new List<IInventoryItem>
            {
                new InventoryItem { Id = itemId, Name = "Obsidian Pod", GameSource = "Our World", Quantity = 3 }
            }
        };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = new Mock<IOASISStorageProvider>();
        var commandStore = provider.As<IHostedAvatarGameplayCommandStore>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.IPFSOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.ProviderName = "atomic-clan-transfer";
        provider.Object.IsProviderActivated = true;
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarDetailAsync(senderId, 0))
            .ReturnsAsync(new OASISResult<IAvatarDetail>(storedDetail));
        HostedSyncCommandItem captured = null;
        commandStore.Setup(x => x.ApplyAvatarGameplayCommandAsync(It.IsAny<HostedSyncCommandItem>(), It.IsAny<CancellationToken>()))
            .Callback<HostedSyncCommandItem, CancellationToken>((command, _) => captured = command)
            .ReturnsAsync(new OASISResult<HyperDriveAvatarDetailProjection>(new HyperDriveAvatarDetailProjection
            {
                AvatarId = senderId
            }));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new ClanManager(providerManager).SendItemToClanAtomicAsync(senderId, clanId,
            "Obsidian Pod", 2, operationId, destinationId, itemId, ProviderType.IPFSOASIS);

        result.IsError.Should().BeFalse(result.Message);
        captured.Should().NotBeNull();
        captured.OperationId.Should().Be(operationId);
        captured.AvatarId.Should().Be(senderId);
        captured.EntityId.Should().Be(senderId);
        captured.EntityType.Should().Be(HyperDriveEntityTypes.AvatarGameplay);
        var payload = HyperDriveJson.Deserialize<HyperDriveAvatarGameplayCommand>(captured.PayloadJson);
        payload.Action.Should().Be(HyperDriveAvatarGameplayAction.TransferInventoryToClan);
        payload.InventoryItemId.Should().Be(itemId);
        payload.TargetClanId.Should().Be(clanId);
        payload.DestinationInventoryItemId.Should().Be(destinationId);
        payload.Amount.Should().Be(2);
    }

    [Fact]
    public async Task ClanInventoryTransferRejectsAnIncompleteRequestedQuantityBeforeWriting()
    {
        var clanId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var storedDetail = new AvatarDetail
        {
            Id = senderId,
            Inventory = new List<IInventoryItem>
            {
                new InventoryItem { Id = Guid.NewGuid(), Name = "Crystal" }
            }
        };
        var storedClan = new Clan { Id = clanId, OwnerAvatarId = Guid.NewGuid() };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "clan-insufficient-quantity");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(clanId, false, false, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(storedClan));
        provider.Setup(x => x.LoadAvatarDetailAsync(senderId, 0))
            .ReturnsAsync(new OASISResult<IAvatarDetail>(storedDetail));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new ClanManager(providerManager).SendItemToClanAsync(
            senderId, clanId, "Crystal", quantity: 2, providerType: ProviderType.IPFSOASIS);

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("insufficient quantity");
        storedDetail.Inventory.Should().ContainSingle();
        provider.Verify(x => x.SaveAvatarDetailAsync(It.IsAny<IAvatarDetail>()), Times.Never);
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Never);
    }

    [Fact]
    public async Task MessagingManagerDoesNotTreatAnUnavailableProviderAsAnEmptyInbox()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-messaging-read-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "provider unavailable" });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "provider unavailable" });
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new MessagingManager(null, dna, providerManager);

        var result = await manager.GetMessagesAsync(avatarId);

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Message.Should().Contain("Could not load messages");
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
    }

    [Fact]
    public async Task MessagingManagerDoesNotReportASentMessageWhenPersistenceFails()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-messaging-write-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(() => new OASISResult<IHolon>(new Holon { MetaData = new Dictionary<string, object>() }));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "write rejected" });
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new MessagingManager(null, dna, providerManager);

        var result = await manager.SendMessageToAvatarAsync(Guid.NewGuid(), Guid.NewGuid(), "durable message");

        result.IsError.Should().BeTrue();
        result.Result.Should().BeFalse();
        result.Message.Should().Contain("could not be persisted");
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public async Task MessageAndReadStateSurviveManagerRestartAsOneCanonicalRecord()
    {
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-message-durable");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon> { Result = stored.TryGetValue(id, out var holon) ? holon : null! });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        provider.Setup(x => x.LoadHolonsByMetaDataAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<HolonType>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((string key, string value, HolonType _, bool _, bool _, int _, int _, bool _, bool _, int _) =>
                new OASISResult<IEnumerable<IHolon>>(stored.Values.Where(h => h.MetaData != null &&
                    h.MetaData.TryGetValue(key, out var indexed) && indexed?.ToString() == value).ToList()));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var send = await new MessagingManager(null, dna, providerManager)
            .SendMessageToAvatarAsync(senderId, recipientId, "restart-safe message");
        var recipientMessages = await new MessagingManager(null, dna, providerManager).GetMessagesAsync(recipientId);
        var messageId = recipientMessages.Result.Single().Id;
        var markRead = await new MessagingManager(null, dna, providerManager)
            .MarkMessagesAsReadAsync(recipientId, new List<Guid> { messageId });
        var senderMessages = await new MessagingManager(null, dna, providerManager).GetMessagesAsync(senderId);

        send.IsError.Should().BeFalse(send.Message);
        recipientMessages.Result.Should().ContainSingle(x => x.Id == messageId);
        markRead.Result.Should().BeTrue(markRead.Message);
        senderMessages.Result.Should().ContainSingle(x => x.Id == messageId && x.IsRead && x.ReadAt.HasValue);
        stored.Values.Count(x => x.MetaData != null &&
            x.MetaData.TryGetValue("oasisEntityType", out var kind) && kind?.ToString() == "message").Should().Be(1);
    }

    [Fact]
    public async Task RejectedMessageReadDoesNotLeakThroughProviderObjectAlias()
    {
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-message-alias");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon> { Result = stored.TryGetValue(id, out var holon) ? holon : null! });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        provider.Setup(x => x.LoadHolonsByMetaDataAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<HolonType>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((string key, string value, HolonType _, bool _, bool _, int _, int _, bool _, bool _, int _) =>
                new OASISResult<IEnumerable<IHolon>>(stored.Values.Where(h => h.MetaData != null &&
                    h.MetaData.TryGetValue(key, out var indexed) && indexed?.ToString() == value).ToList()));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var send = await new MessagingManager(null, dna, providerManager)
            .SendMessageToAvatarAsync(senderId, recipientId, "reject read");
        var message = (await new MessagingManager(null, dna, providerManager).GetMessagesAsync(recipientId)).Result.Single();
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "read write rejected" });

        var mark = await new MessagingManager(null, dna, providerManager)
            .MarkMessagesAsReadAsync(recipientId, new List<Guid> { message.Id });
        var reloaded = await new MessagingManager(null, dna, providerManager).GetMessagesAsync(recipientId);

        send.IsError.Should().BeFalse(send.Message);
        mark.IsError.Should().BeTrue();
        reloaded.Result.Should().ContainSingle(x => x.Id == message.Id && !x.IsRead && !x.ReadAt.HasValue);
    }

    [Fact]
    public async Task EggsManagerDoesNotTreatAnUnavailableInjectedRuntimeAsAnEmptyCollection()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-eggs-read-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "egg store unavailable" });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "egg store unavailable" });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new EggsManager(null, dna, providerManager);

        var result = await manager.GetAllEggsAsync(avatarId);

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Message.Should().Contain("Could not load the egg collection");
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
    }

    [Fact]
    public async Task EggsManagerDoesNotReportDiscoveryWhenItsDurableWriteIsRejected()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-eggs-write-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(() => new OASISResult<IHolon>(new Holon { MetaData = new Dictionary<string, object>() }));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "egg write rejected" });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new EggsManager(null, dna, providerManager);

        var result = await manager.DiscoverEggAsync(avatarId, EggType.Gold, "Golden Test Egg",
            Guid.NewGuid(), "test arena", EggDiscoveryMethod.Exploration);

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Message.Should().Contain("could not be committed");
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public async Task EggsManagerDiscoveryAndHatchSurviveManagerRestart()
    {
        var avatarId = Guid.NewGuid();
        IHolon stored = new Holon { MetaData = new Dictionary<string, object>() };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-eggs-durable");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(() => new OASISResult<IHolon>(stored));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var discovery = await new EggsManager(null, dna, providerManager).DiscoverEggAsync(
            avatarId, EggType.Gold, "Durable Golden Egg", Guid.NewGuid(), "edge world",
            EggDiscoveryMethod.Exploration);
        var afterDiscoveryRestart = await new EggsManager(null, dna, providerManager).GetAllEggsAsync(avatarId);
        var hatch = await new EggsManager(null, dna, providerManager)
            .HatchEggAsync(avatarId, discovery.Result.Id);
        var afterHatchRestart = await new EggsManager(null, dna, providerManager).GetAllEggsAsync(avatarId);

        discovery.IsError.Should().BeFalse(discovery.Message);
        discovery.IsSaved.Should().BeTrue();
        afterDiscoveryRestart.Result.Should().ContainSingle(x => x.Id == discovery.Result.Id);
        hatch.IsError.Should().BeFalse(hatch.Message);
        hatch.IsSaved.Should().BeTrue();
        afterHatchRestart.Result.Should().ContainSingle(x => x.Id == discovery.Result.Id && x.IsHatched);
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.AtLeast(2));
    }

    [Fact]
    public async Task CompetitionLeaderboardSurvivesManagerRestart()
    {
        var avatarId = Guid.NewGuid();
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-competition-durable");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon> { Result = stored.TryGetValue(id, out var holon) ? holon : null! });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var update = await new CompetitionManager(null, dna, providerManager).UpdateAvatarScoreAsync(
            avatarId, CompetitionType.Karma, SeasonType.Weekly, 75);
        var leaderboard = await new CompetitionManager(null, dna, providerManager).GetLeaderboardAsync(
            CompetitionType.Karma, SeasonType.Weekly);

        update.IsError.Should().BeFalse(update.Message);
        update.Result.Should().BeTrue();
        leaderboard.IsError.Should().BeFalse(leaderboard.Message);
        leaderboard.Result.Should().ContainSingle(entry =>
            entry.AvatarId == avatarId && entry.Score == 75 && entry.Rank == 1);
    }

    [Fact]
    public async Task CompetitionRejectedWriteDoesNotLeakUncommittedScoreThroughProviderAlias()
    {
        var avatarId = Guid.NewGuid();
        var originalEntries = new List<LeaderboardEntry>
        {
            new LeaderboardEntry
            {
                AvatarId = avatarId, CompetitionType = CompetitionType.Karma,
                SeasonType = SeasonType.Daily, Score = 10, Rank = 1
            }
        };
        var stored = new Holon
        {
            MetaData = new Dictionary<string, object>
            {
                ["entries"] = Newtonsoft.Json.JsonConvert.SerializeObject(originalEntries)
            }
        };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-competition-rejected");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(stored));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>
            {
                IsError = true, ErrorCount = 1, ErrorCode = "LEADERBOARD_WRITE_REJECTED",
                Message = "leaderboard write rejected"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new CompetitionManager(null, dna, providerManager);

        var update = await manager.UpdateAvatarScoreAsync(
            avatarId, CompetitionType.Karma, SeasonType.Daily, 50);
        var reloaded = await new CompetitionManager(null, dna, providerManager).GetLeaderboardAsync(
            CompetitionType.Karma, SeasonType.Daily);

        update.IsError.Should().BeTrue();
        update.Message.Should().Contain("not persisted");
        reloaded.Result.Should().ContainSingle(entry => entry.AvatarId == avatarId && entry.Score == 10);
    }

    [Fact]
    public async Task TournamentEnrollmentIsValidatedPersistedAndRestartSafe()
    {
        var avatarId = Guid.NewGuid();
        var tournament = new Tournament
        {
            Name = "Edge Championship", CompetitionType = CompetitionType.Karma,
            Status = TournamentStatus.Registration, RegistrationStart = DateTime.UtcNow.AddHours(-1),
            RegistrationEnd = DateTime.UtcNow.AddHours(1), StartDate = DateTime.UtcNow.AddHours(2),
            EndDate = DateTime.UtcNow.AddDays(1), MaxParticipants = 2
        };
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-tournament-durable");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon> { Result = stored.TryGetValue(id, out var holon) ? holon : null! });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var seed = await new CompetitionManager(null, dna, providerManager).SaveTournamentAsync(tournament);

        var join = await new CompetitionManager(null, dna, providerManager).JoinTournamentAsync(avatarId, tournament.Id);
        var duplicate = await new CompetitionManager(null, dna, providerManager).JoinTournamentAsync(avatarId, tournament.Id);
        var afterRestart = await new CompetitionManager(null, dna, providerManager)
            .GetActiveTournamentsAsync(CompetitionType.Karma);

        seed.Result.Should().BeSameAs(tournament);
        seed.IsSaved.Should().BeTrue();
        join.Result.Should().BeTrue(join.Message);
        join.IsSaved.Should().BeTrue();
        duplicate.Result.Should().BeTrue(duplicate.Message);
        duplicate.IsSaved.Should().BeFalse();
        afterRestart.Result.Should().ContainSingle(x => x.Id == tournament.Id &&
            x.CurrentParticipants == 1 && x.Participants.Single().AvatarId == avatarId);
    }

    [Fact]
    public async Task TournamentSaveRejectionIsNotReportedAsPublished()
    {
        IHolon stored = null;
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-tournament-rejected");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(() => new OASISResult<IHolon> { Result = stored });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var tournament = new Tournament
        {
            Name = "Rejected Tournament", CompetitionType = CompetitionType.Karma,
            Status = TournamentStatus.Registration, RegistrationStart = DateTime.UtcNow.AddHours(-1),
            RegistrationEnd = DateTime.UtcNow.AddHours(1), StartDate = DateTime.UtcNow.AddHours(2),
            EndDate = DateTime.UtcNow.AddDays(1), MaxParticipants = 8
        };

        var manager = new CompetitionManager(null, dna, providerManager);
        (await manager.SaveTournamentAsync(tournament)).IsError.Should().BeFalse();
        tournament.Description = "This update must be rejected.";
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "catalog write rejected" });

        var save = await manager.SaveTournamentAsync(tournament);

        save.IsError.Should().BeTrue();
        save.Result.Should().BeNull();
        save.Message.Should().Contain("not persisted");
    }

    [Fact]
    public async Task GiftsManagerDoesNotReportSendWhenCanonicalGiftWriteIsRejected()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-gift-rejected");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>
            {
                IsError = true, ErrorCount = 1, ErrorCode = "GIFT_WRITE_REJECTED",
                Message = "gift write rejected"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new GiftsManager(null, dna, providerManager).SendGiftAsync(
            Guid.NewGuid(), Guid.NewGuid(), GiftType.Badge, "durability test");

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Message.Should().Contain("durable record could not be saved");
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public async Task GiftLifecycleAndDerivedHistorySurviveManagerRestart()
    {
        var fromAvatarId = Guid.NewGuid();
        var toAvatarId = Guid.NewGuid();
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-gift-durable");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon> { Result = stored.TryGetValue(id, out var holon) ? holon : null! });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        provider.Setup(x => x.LoadHolonsByMetaDataAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<HolonType>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((string key, string value, HolonType _, bool _, bool _, int _, int _, bool _, bool _, int _) =>
                new OASISResult<IEnumerable<IHolon>>(stored.Values.Where(h => h.MetaData != null &&
                    h.MetaData.TryGetValue(key, out var indexed) && indexed?.ToString() == value).ToList()));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var send = await new GiftsManager(null, dna, providerManager).SendGiftAsync(
            fromAvatarId, toAvatarId, GiftType.Egg, "restart-safe gift");
        var receive = await new GiftsManager(null, dna, providerManager).ReceiveGiftAsync(toAvatarId, send.Result.Id);
        var open = await new GiftsManager(null, dna, providerManager).OpenGiftAsync(toAvatarId, send.Result.Id);
        var gifts = await new GiftsManager(null, dna, providerManager).GetAllGiftsAsync(toAvatarId);
        var senderHistory = await new GiftsManager(null, dna, providerManager).GetGiftHistoryAsync(fromAvatarId);
        var recipientHistory = await new GiftsManager(null, dna, providerManager).GetGiftHistoryAsync(toAvatarId);

        send.IsError.Should().BeFalse(send.Message);
        receive.Result.Should().BeTrue(receive.Message);
        open.Result.Should().BeTrue(open.Message);
        gifts.Result.Should().ContainSingle(x => x.Id == send.Result.Id && x.IsReceived && x.IsOpened);
        senderHistory.Result.Should().Contain(x => x.GiftId == send.Result.Id && x.TransactionType == GiftTransactionType.Sent);
        recipientHistory.Result.Should().Contain(x => x.GiftId == send.Result.Id && x.TransactionType == GiftTransactionType.Opened);
    }

    [Fact]
    public async Task RejectedGiftTransitionDoesNotLeakThroughProviderObjectAlias()
    {
        var senderId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-gift-alias");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon> { Result = stored.TryGetValue(id, out var holon) ? holon : null! });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        provider.Setup(x => x.LoadHolonsByMetaDataAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<HolonType>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((string key, string value, HolonType _, bool _, bool _, int _, int _, bool _, bool _, int _) =>
                new OASISResult<IEnumerable<IHolon>>(stored.Values.Where(h => h.MetaData != null &&
                    h.MetaData.TryGetValue(key, out var indexed) && indexed?.ToString() == value).ToList()));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var send = await new GiftsManager(null, dna, providerManager)
            .SendGiftAsync(senderId, recipientId, GiftType.Title, "reject receive");
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon> { IsError = true, Message = "receive write rejected" });

        var receive = await new GiftsManager(null, dna, providerManager).ReceiveGiftAsync(recipientId, send.Result.Id);
        var reloaded = await new GiftsManager(null, dna, providerManager).GetAllGiftsAsync(recipientId);

        send.IsError.Should().BeFalse(send.Message);
        receive.IsError.Should().BeTrue();
        reloaded.Result.Should().ContainSingle(x => x.Id == send.Result.Id && !x.IsReceived && !x.ReceivedAt.HasValue);
    }

    [Fact]
    public void BridgeManagerBuildsItsProviderMapFromTheInjectedRuntime()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = new Mock<IOASISBlockchainStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.EthereumOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Blockchain);
        provider.Object.ProviderName = "isolated-ethereum";
        provider.Object.IsProviderActivated = true;
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        var singletonEthereum = ProviderManager.Instance.GetStorageProvider(ProviderType.EthereumOASIS);

        var manager = new BridgeManager(null, dna, providerManager);

        manager.GetBridgeProvider("ETH").Should().BeSameAs(provider.Object);
        ProviderManager.Instance.GetStorageProvider(ProviderType.EthereumOASIS).Should().BeSameAs(singletonEthereum);
    }

    [Fact]
    public async Task BridgeManagerResolvesNftProvidersFromTheInjectedRuntime()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var source = new Mock<IOASISBlockchainStorageProvider>();
        var sourceNft = source.As<IOASISNFTProvider>();
        source.SetupAllProperties();
        source.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.EthereumOASIS);
        source.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Blockchain);
        source.Object.ProviderName = "isolated-nft-source";
        source.Object.IsProviderActivated = true;
        sourceNft.Setup(x => x.WithdrawNFTAsync("contract", "token", "from", string.Empty))
            .ReturnsAsync(new OASISResult<NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs.BridgeTransactionResponse>
            {
                IsError = true,
                Message = "injected source reached"
            });

        var destination = new Mock<IOASISBlockchainStorageProvider>();
        destination.As<IOASISNFTProvider>();
        destination.SetupAllProperties();
        destination.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SolanaOASIS);
        destination.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Blockchain);
        destination.Object.ProviderName = "isolated-nft-destination";
        destination.Object.IsProviderActivated = true;
        providerManager.RegisterProvider(source.Object).Should().BeTrue();
        providerManager.RegisterProvider(destination.Object).Should().BeTrue();

        var manager = new BridgeManager(null, dna, providerManager);
        var result = await manager.CreateNFTBridgeOrderAsync(new CreateNFTBridgeOrderRequest
        {
            FromChain = nameof(ProviderType.EthereumOASIS),
            ToChain = nameof(ProviderType.SolanaOASIS),
            NFTTokenAddress = "contract",
            TokenId = "token",
            FromAddress = "from",
            DestinationAddress = "to"
        });

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("injected source reached");
        sourceNft.Verify(x => x.WithdrawNFTAsync("contract", "token", "from", string.Empty), Times.Once);
    }

    [Fact]
    public async Task StatsManagerLoadsTheAvatarFromItsInjectedRuntime()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-stats-provider");
        var avatarId = Guid.NewGuid();
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarAsync(avatarId, 0))
            .ReturnsAsync(new OASISResult<IAvatar>
            {
                IsError = true,
                Message = "isolated stats provider reached"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var manager = new StatsManager(null, dna, providerManager);
        var result = await manager.GetAvatarStatsAsync(avatarId);

        result.IsError.Should().BeTrue();
        provider.Verify(x => x.LoadAvatarAsync(avatarId, 0), Times.Once);
    }

    [Fact]
    public async Task StatsManagerDoesNotReportZeroSystemCountsWhenItsProviderSearchFails()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-stats-search-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.SearchAsync(It.IsAny<ISearchParams>(), true, true, 0, true, 0))
            .ReturnsAsync(new OASISResult<ISearchResults>
            {
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "PROVIDER_UNAVAILABLE",
                Message = "provider unavailable"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var manager = new StatsManager(null, dna, providerManager);
        var result = await manager.GetSystemStatsAsync();

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Message.Should().Contain("total avatar count");
        result.Message.Should().Contain("provider unavailable");
        provider.Verify(x => x.SearchAsync(It.IsAny<ISearchParams>(), true, true, 0, true, 0), Times.Once);
    }

    [Fact]
    public async Task SettingsReadFailureNeverAttemptsToCreateOrOverwriteTheSettingsHolon()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-settings-read-only-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>
            {
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "PROVIDER_UNAVAILABLE",
                Message = "provider unavailable"
            });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
                new OASISResult<IHolon>(holon) { IsSaved = true });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var manager = new HolonManager(null, dna, providerManager);
        var result = await manager.GetAllSettingsAsync(avatarId, "gifts");

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Message.Should().Contain("provider unavailable");
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Never);
    }

    [Fact]
    public void SynchronousSettingsReadFailureNeverAttemptsToCreateOrOverwriteTheSettingsHolon()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-sync-settings-read-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolon(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .Returns(new OASISResult<IHolon>
            {
                IsError = true,
                ErrorCode = "PROVIDER_UNAVAILABLE",
                ErrorCount = 1,
                Message = "provider unavailable"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var manager = new HolonManager(null, dna, providerManager);
        var result = manager.GetAllSettings(avatarId, "karma");

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Message.Should().Contain("provider unavailable");
        provider.Verify(x => x.SaveHolon(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Never);
        provider.Verify(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void SynchronousSaveSettingsUsesOnlyTheSynchronousProviderBoundary()
    {
        var avatarId = Guid.NewGuid();
        var settingsHolon = new Holon
        {
            Id = Guid.NewGuid(),
            CreatedByAvatarId = avatarId,
            MetaData = new Dictionary<string, object> { ["existing"] = "preserved" }
        };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-sync-settings-save");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolon(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .Returns(new OASISResult<IHolon>(settingsHolon));
        IHolon savedHolon = null;
        provider.Setup(x => x.SaveHolon(It.IsAny<IHolon>(), true, true, 0, true, false))
            .Callback((IHolon holon, bool _, bool _, int _, bool _, bool _) => savedHolon = holon)
            .Returns((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
                new OASISResult<IHolon>(holon) { IsSaved = true });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var manager = new HolonManager(null, dna, providerManager);
        var result = manager.SaveSettings(avatarId, "karma",
            new Dictionary<string, object> { ["totalKarma"] = 42L });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeTrue();
        savedHolon.Should().NotBeNull().And.NotBeSameAs(settingsHolon);
        savedHolon.MetaData["existing"].Should().Be("preserved");
        savedHolon.MetaData["totalKarma"].Should().Be(42L);
        savedHolon.MetaData.Should().ContainKey("_versionStamp");
        settingsHolon.MetaData.Should().NotContainKey("totalKarma",
            "provider-returned state must remain unchanged until the provider accepts the detached write");
        provider.Verify(x => x.LoadHolon(It.IsAny<Guid>(), true, true, 0, true, false, 0), Times.Once);
        provider.Verify(x => x.SaveHolon(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
        provider.Verify(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void SynchronousKarmaMutationPersistsLedgerAndProjectionsWithoutAsyncProviderCalls()
    {
        var avatarId = Guid.NewGuid();
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-sync-karma");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolon(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .Returns((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon>(stored.TryGetValue(id, out var holon)
                    ? holon
                    : new Holon { Id = id, MetaData = new Dictionary<string, object>() }));
        provider.Setup(x => x.SaveHolon(It.IsAny<IHolon>(), true, true, 0, true, false))
            .Returns((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var manager = new KarmaManager(null, dna, providerManager);
        var mutation = manager.AddKarma(avatarId, 25, KarmaSourceType.Platform, "offline action");
        var balance = new KarmaManager(null, dna, providerManager).GetKarma(avatarId);

        mutation.IsError.Should().BeFalse(mutation.Message);
        mutation.Result.Should().BeTrue();
        balance.IsError.Should().BeFalse(balance.Message);
        balance.Result.Should().Be(25);
        provider.Verify(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void SynchronousHighLevelKarmaUsesDurableSyncPathAndUpdatesAvatarProjection()
    {
        var avatarId = Guid.NewGuid();
        var detail = new AvatarDetail { Id = avatarId, Karma = 0 };
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-sync-high-level-karma");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarDetail(avatarId, 0))
            .Returns(new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.SaveAvatarDetail(It.IsAny<IAvatarDetail>()))
            .Returns((IAvatarDetail avatar) => new OASISResult<IAvatarDetail>(avatar) { IsSaved = true });
        provider.Setup(x => x.LoadHolon(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .Returns((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon>(stored.TryGetValue(id, out var holon)
                    ? holon
                    : new Holon { Id = id, MetaData = new Dictionary<string, object>() }));
        provider.Setup(x => x.SaveHolon(It.IsAny<IHolon>(), true, true, 0, true, false))
            .Returns((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = new KarmaManager(null, dna, providerManager).AddKarmaToAvatar(
            avatarId, KarmaTypePositive.CreateAvatar, KarmaSourceType.Platform,
            "registration", "registered avatar");

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().NotBeNull();
        detail.Karma.Should().Be(KarmaManager.GetKarmaForType(KarmaTypePositive.CreateAvatar));
        detail.KarmaAkashicRecords.Should().ContainSingle();
        provider.Verify(x => x.SaveAvatarDetail(detail), Times.Once);
        provider.Verify(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void HyperDriveV2AvatarManagerKarmaUsesDurableLedgerInsteadOfProviderKarmaShortcut()
    {
        var avatarId = Guid.NewGuid();
        var detail = new AvatarDetail { Id = avatarId, Karma = 0 };
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-avatar-manager-karma");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarDetail(avatarId, 0))
            .Returns(new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.SaveAvatarDetail(It.IsAny<IAvatarDetail>()))
            .Returns((IAvatarDetail avatar) => new OASISResult<IAvatarDetail>(avatar) { IsSaved = true });
        provider.Setup(x => x.LoadHolon(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .Returns((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon>(stored.TryGetValue(id, out var holon)
                    ? holon
                    : new Holon { Id = id, MetaData = new Dictionary<string, object>() }));
        provider.Setup(x => x.SaveHolon(It.IsAny<IHolon>(), true, true, 0, true, false))
            .Returns((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var manager = new AvatarManager(null, dna, providerManager);
        var result = manager.AddKarmaToAvatar(
            avatarId, KarmaTypePositive.HelpOtherPerson, KarmaSourceType.Platform,
            "registration", "registered avatar", providerType: ProviderType.IPFSOASIS);
        var removed = manager.RemoveKarmaFromAvatar(
            avatarId, KarmaTypeNegative.BeingSelfish, KarmaSourceType.Platform,
            "cleanup", "dropped litter", providerType: ProviderType.IPFSOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().NotBeNull();
        removed.IsError.Should().BeFalse(removed.Message);
        removed.Result.Should().NotBeNull();
        detail.Karma.Should().Be(KarmaManager.GetKarmaForType(KarmaTypePositive.HelpOtherPerson)
            - Math.Abs(KarmaManager.GetKarmaForType(KarmaTypeNegative.BeingSelfish)));
        provider.Verify(x => x.AddKarmaToAvatar(It.IsAny<IAvatarDetail>(), It.IsAny<KarmaTypePositive>(),
            It.IsAny<KarmaSourceType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        provider.Verify(x => x.RemoveKarmaFromAvatar(It.IsAny<IAvatarDetail>(), It.IsAny<KarmaTypeNegative>(),
            It.IsAny<KarmaSourceType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        provider.Verify(x => x.SaveHolon(It.IsAny<IHolon>(), true, true, 0, true, false),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task HyperDriveV2AvatarManagerAsyncKarmaUsesDurableLedgerInsteadOfProviderKarmaShortcut()
    {
        var avatarId = Guid.NewGuid();
        var detail = new AvatarDetail { Id = avatarId, Karma = 0 };
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-avatar-manager-async-karma");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarDetail(avatarId, 0))
            .Returns(new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.SaveAvatarDetail(It.IsAny<IAvatarDetail>()))
            .Returns((IAvatarDetail avatar) => new OASISResult<IAvatarDetail>(avatar) { IsSaved = true });
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon>(stored.TryGetValue(id, out var holon)
                    ? holon
                    : new Holon { Id = id, MetaData = new Dictionary<string, object>() }));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var manager = new AvatarManager(null, dna, providerManager);
        var result = await manager.AddKarmaToAvatarAsync(
            avatarId, KarmaTypePositive.HelpOtherPerson, KarmaSourceType.Platform,
            "registration", "registered avatar", providerType: ProviderType.IPFSOASIS);
        var removed = await manager.RemoveKarmaFromAvatarAsync(
            avatarId, KarmaTypeNegative.BeingSelfish, KarmaSourceType.Platform,
            "cleanup", "dropped litter", providerType: ProviderType.IPFSOASIS);

        result.Should().NotBeNull();
        removed.Should().NotBeNull();
        detail.Karma.Should().Be(KarmaManager.GetKarmaForType(KarmaTypePositive.HelpOtherPerson)
            - Math.Abs(KarmaManager.GetKarmaForType(KarmaTypeNegative.BeingSelfish)));
        provider.Verify(x => x.AddKarmaToAvatarAsync(It.IsAny<IAvatarDetail>(), It.IsAny<KarmaTypePositive>(),
            It.IsAny<KarmaSourceType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        provider.Verify(x => x.RemoveKarmaFromAvatarAsync(It.IsAny<IAvatarDetail>(), It.IsAny<KarmaTypeNegative>(),
            It.IsAny<KarmaSourceType>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task AvatarStatsDoesNotReturnAPartialDashboardWhenDurableStatsCannotLoad()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-avatar-stats-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarAsync(avatarId, 0))
            .ReturnsAsync(new OASISResult<IAvatar>(new Avatar { Id = avatarId }));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>
            {
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "PROVIDER_UNAVAILABLE",
                Message = "provider unavailable"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new StatsManager(null, dna, providerManager).GetAvatarStatsAsync(avatarId);

        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        result.Message.Should().Contain("karma statistics failed");
        result.Message.Should().Contain("provider unavailable");
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Never);
    }

    [Fact]
    public async Task KarmaMutationDoesNotCommitMemoryOrReportSuccessWhenPersistenceFails()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-karma-write-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(new Holon { MetaData = new Dictionary<string, object>() }));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>
            {
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "PROVIDER_WRITE_REJECTED",
                Message = "write rejected"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new KarmaManager(null, dna, providerManager);

        var mutation = await manager.AddKarmaAsync(avatarId, 25, KarmaSourceType.Platform, "offline action");
        var history = await manager.GetKarmaHistoryAsync(avatarId);

        mutation.IsError.Should().BeTrue();
        mutation.Result.Should().BeFalse();
        mutation.Message.Should().Contain("durable persistence failed");
        history.Result.Should().BeEmpty();
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public async Task KarmaHistorySurvivesManagerRestartThroughDurableSettings()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-durable-karma");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        IHolon stored = new Holon { MetaData = new Dictionary<string, object>() };
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(() => new OASISResult<IHolon>(stored));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var mutation = await new KarmaManager(null, dna, providerManager)
            .AddKarmaAsync(avatarId, 25, KarmaSourceType.Platform, "offline action");
        var restartedManager = new KarmaManager(null, dna, providerManager);
        var history = await restartedManager.GetKarmaHistoryAsync(avatarId);

        mutation.IsError.Should().BeFalse(mutation.Message);
        history.IsError.Should().BeFalse(history.Message);
        history.Result.Should().ContainSingle();
        history.Result[0].Amount.Should().Be(25);
        history.Result[0].Description.Should().Be("offline action");
    }

    [Fact]
    public async Task KarmaCommitReportsLeaderboardProjectionFailureWithoutLosingTheLedger()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-karma-projection-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        IHolon stored = new Holon { MetaData = new Dictionary<string, object>() };
        var saveCount = 0;
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(() => new OASISResult<IHolon>(stored));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                saveCount++;
                if (saveCount == 1)
                {
                    stored = holon;
                    return new OASISResult<IHolon>(holon) { IsSaved = true };
                }

                return new OASISResult<IHolon>(holon)
                {
                    IsError = true,
                    ErrorCount = 1,
                    ErrorCode = "LEADERBOARD_WRITE_REJECTED",
                    Message = "leaderboard write rejected"
                };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new KarmaManager(null, dna, providerManager);

        var mutation = await manager.AddKarmaAsync(
            avatarId, 25, KarmaSourceType.Platform, "offline action");
        var history = await manager.GetKarmaHistoryAsync(avatarId);

        mutation.IsError.Should().BeFalse(mutation.Message);
        mutation.Result.Should().BeTrue();
        mutation.WarningCount.Should().BeGreaterThan(0);
        mutation.InnerMessages.Should().Contain(message => message.Contains("leaderboard"));
        history.Result.Should().ContainSingle(transaction => transaction.Amount == 25);
        saveCount.Should().Be(6, "one durable karma commit and five seasonal projections are expected");
    }

    [Fact]
    public async Task KarmaStatsSurviveManagerRestartAndEmptyHistoryIsValid()
    {
        var avatarId = Guid.NewGuid();
        var emptyAvatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-durable-karma-stats");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var stored = new Dictionary<Guid, IHolon>();
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync((Guid id, bool _, bool _, int _, bool _, bool _, int _) =>
                new OASISResult<IHolon>(stored.TryGetValue(id, out var holon)
                    ? holon
                    : new Holon { Id = id, MetaData = new Dictionary<string, object>() }));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var mutation = await new KarmaManager(null, dna, providerManager)
            .AddKarmaAsync(avatarId, 25, KarmaSourceType.Platform, "offline action");
        var restarted = new KarmaManager(null, dna, providerManager);
        var stats = await restarted.GetKarmaStatsAsync(avatarId);
        var emptyStats = await restarted.GetKarmaStatsAsync(emptyAvatarId);

        mutation.IsError.Should().BeFalse(mutation.Message);
        stats.IsError.Should().BeFalse(stats.Message);
        stats.Result["totalKarma"].Should().Be(25L);
        stats.Result["transactionCount"].Should().Be(1);
        stats.Result["largestEarning"].Should().Be(25L);
        emptyStats.IsError.Should().BeFalse(emptyStats.Message);
        emptyStats.Result["largestEarning"].Should().Be(0L);
        emptyStats.Result["largestSpending"].Should().Be(0L);
    }

    [Fact]
    public async Task KarmaHistoryReadsProviderJsonArrayShapes()
    {
        var avatarId = Guid.NewGuid();
        var transactionId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "json-karma-history");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(new Holon
            {
                MetaData = new Dictionary<string, object>
                {
                    ["totalKarma"] = 11L,
                    ["history"] = JArray.Parse($"[{{\"id\":\"{transactionId}\",\"avatarId\":\"{avatarId}\",\"amount\":11,\"sourceType\":\"Platform\",\"description\":\"restored JSON\",\"timestamp\":\"2026-01-02T03:04:05Z\"}}]")
                }
            }));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var history = await new KarmaManager(null, dna, providerManager)
            .GetKarmaHistoryAsync(avatarId);

        history.IsError.Should().BeFalse(history.Message);
        history.Result.Should().ContainSingle();
        history.Result[0].Id.Should().Be(transactionId);
        history.Result[0].Amount.Should().Be(11);
        history.Result[0].Description.Should().Be("restored JSON");
    }

    [Fact]
    public async Task KarmaReadPropagatesDurableProviderFailureInsteadOfUsingAvatarProjection()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "karma-read-failure");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>
            {
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "KARMA_STORE_UNAVAILABLE",
                Message = "karma store unavailable"
            });
        provider.Setup(x => x.LoadAvatarDetail(avatarId, 0))
            .Returns(new OASISResult<IAvatarDetail>(new AvatarDetail { Id = avatarId, Karma = 999 }));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new KarmaManager(null, dna, providerManager).GetKarmaAsync(avatarId);

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("karma store unavailable");
        result.Result.Should().Be(0);
        provider.Verify(x => x.LoadAvatarDetail(avatarId, 0), Times.Never);
    }

    [Fact]
    public async Task HighLevelKarmaDoesNotMutateAvatarProjectionWhenLedgerRejectsWrite()
    {
        var avatarId = Guid.NewGuid();
        var detail = new AvatarDetail { Id = avatarId, Karma = 10 };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "high-level-karma-rejection");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarDetail(avatarId, 0))
            .Returns(new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(new Holon { MetaData = new Dictionary<string, object>() }));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>
            {
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "KARMA_WRITE_REJECTED",
                Message = "karma write rejected"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new KarmaManager(null, dna, providerManager).AddKarmaToAvatarAsync(
            avatarId, KarmaTypePositive.CreateAvatar, KarmaSourceType.Platform,
            "registration", "registered avatar");

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("durable ledger rejected");
        detail.Karma.Should().Be(10);
        detail.KarmaAkashicRecords.Should().BeNullOrEmpty();
        provider.Verify(x => x.SaveAvatarDetail(It.IsAny<IAvatarDetail>()), Times.Never);
    }

    [Fact]
    public void AuthenticationRestoresItsRuntimeFailoverScopeWhenAvatarIsMissing()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "authentication-scope");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarByUsername("missing", 0)).Returns(new OASISResult<IAvatar>());
        provider.Setup(x => x.LoadAvatarByEmail("missing", 0)).Returns(new OASISResult<IAvatar>());
        provider.Setup(x => x.LoadAvatarByPublicKeyAsync("missing", 0))
            .ReturnsAsync(new OASISResult<IAvatar>());
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        providerManager.SetAndReplaceAutoFailOverListForProviders(
            new[] { new EnumValue<ProviderType>(ProviderType.IPFSOASIS) });
        providerManager.SetAndReplaceAutoFailOverListForProvidersForAvatarLogin(
            new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS) });
        var manager = new AvatarManager(null, dna, providerManager);

        var result = manager.Authenticate("missing", "password", "127.0.0.1");

        result.Result.Should().BeNull();
        providerManager.GetProviderAutoFailOverList().Select(x => x.Value)
            .Should().Equal(ProviderType.IPFSOASIS);
    }

    [Fact]
    public async Task SocialStatisticsUseTheManagersIsolatedRuntime()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-social-runtime");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>(), true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(new Holon { MetaData = new Dictionary<string, object>() }));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
                new OASISResult<IHolon>(holon) { IsSaved = true });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new SocialManager(null, dna, providerManager)
            .ShareHolonAsync(avatarId, Guid.NewGuid(), "edge share");

        result.IsError.Should().BeFalse(result.Message);
        provider.Verify(x => x.SaveHolonAsync(
            It.Is<IHolon>(holon => holon.MetaData.ContainsKey("lastShareDate")),
            true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public async Task VideoStartRollsBackTransientCallWhenItsRuntimeRejectsPersistence()
    {
        var initiatorId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-video-runtime");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) => new OASISResult<IHolon>
            {
                Result = holon,
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "VIDEO_WRITE_REJECTED",
                Message = "video write rejected"
            });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new VideoManager(null, dna, providerManager);

        var start = await manager.StartVideoCallAsync(initiatorId, new List<Guid> { Guid.NewGuid() });
        var active = await manager.GetActiveCallsAsync(initiatorId);

        start.IsError.Should().BeTrue();
        start.Message.Should().Contain("video write rejected");
        active.Result.Should().BeEmpty();
    }

    [Fact]
    public async Task ChatStartUsesItsRuntimeIdentityAndRollsBackRejectedPersistence()
    {
        var participantId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-chat-runtime");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) => new OASISResult<IHolon>
            {
                Result = holon,
                IsError = true,
                ErrorCount = 1,
                ErrorCode = "CHAT_WRITE_REJECTED",
                Message = "chat write rejected"
            });
        provider.Setup(x => x.LoadHolonsByMetaDataAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<HolonType>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(Array.Empty<IHolon>()));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new ChatManager(null, dna, providerManager);

        var start = await manager.StartNewChatSessionAsync(
            new List<Guid> { participantId, Guid.NewGuid() });
        var active = await manager.GetActiveSessionsAsync(participantId);

        start.IsError.Should().BeTrue();
        start.Message.Should().Contain("chat write rejected");
        active.Result.Should().BeEmpty();
    }

    [Fact]
    public async Task ChatSessionMessagesAndTerminationSurviveManagerRestart()
    {
        var participantId = Guid.NewGuid();
        var otherParticipantId = Guid.NewGuid();
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "durable-chat-runtime");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        provider.Setup(x => x.LoadHolonsByMetaDataAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<HolonType>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((string key, string value, HolonType _, bool _, bool _, int _, int _, bool _, bool _, int _) =>
                new OASISResult<IEnumerable<IHolon>>(stored.Values.Where(h => h.MetaData != null &&
                    h.MetaData.TryGetValue(key, out var indexed) && indexed?.ToString() == value).ToList()));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var first = new ChatManager(null, dna, providerManager);
        var start = await first.StartNewChatSessionAsync(new List<Guid> { participantId, otherParticipantId }, "durable");
        var send = await first.SendMessageAsync(start.Result, participantId, "survives restart");
        var restarted = new ChatManager(null, dna, providerManager);
        var active = await restarted.GetActiveSessionsAsync(participantId);
        var history = await restarted.GetChatHistoryAsync(start.Result);

        start.IsError.Should().BeFalse(start.Message);
        send.IsError.Should().BeFalse(send.Message);
        active.Result.Should().ContainSingle(x => x.Id == start.Result && x.IsActive);
        history.Result.Should().ContainSingle(x => x.Id == send.Result && x.Content == "survives restart");

        var end = await restarted.EndChatSessionAsync(start.Result, participantId);
        var afterEndRestart = await new ChatManager(null, dna, providerManager).GetActiveSessionsAsync(participantId);
        var sendAfterEnd = await new ChatManager(null, dna, providerManager)
            .SendMessageAsync(start.Result, participantId, "must be rejected");

        end.Result.Should().BeTrue(end.Message);
        afterEndRestart.Result.Should().NotContain(x => x.Id == start.Result);
        sendAfterEnd.IsError.Should().BeTrue();
        sendAfterEnd.Message.Should().Contain("ended");
    }

    [Fact]
    public async Task RejectedChatTerminationDoesNotMutateTheStoredSessionAlias()
    {
        var participantId = Guid.NewGuid();
        var stored = new Dictionary<Guid, IHolon>();
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "rejected-chat-end-runtime");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
            {
                stored[holon.Id] = holon;
                return new OASISResult<IHolon>(holon) { IsSaved = true };
            });
        provider.Setup(x => x.LoadHolonsByMetaDataAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<HolonType>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()))
            .ReturnsAsync((string key, string value, HolonType _, bool _, bool _, int _, int _, bool _, bool _, int _) =>
                new OASISResult<IEnumerable<IHolon>>(stored.Values.Where(h => h.MetaData != null &&
                    h.MetaData.TryGetValue(key, out var indexed) && indexed?.ToString() == value).ToList()));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var start = await new ChatManager(null, dna, providerManager)
            .StartNewChatSessionAsync(new List<Guid> { participantId, Guid.NewGuid() });
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>
                { IsError = true, ErrorCount = 1, Message = "chat termination rejected" });

        var end = await new ChatManager(null, dna, providerManager)
            .EndChatSessionAsync(start.Result, participantId);
        var reloaded = await new ChatManager(null, dna, providerManager).GetActiveSessionsAsync(participantId);

        end.IsError.Should().BeTrue();
        end.Message.Should().Contain("chat termination rejected");
        reloaded.Result.Should().ContainSingle(x => x.Id == start.Result && x.IsActive);
    }

    [Fact]
    public async Task LocalQuotaMustBeExplicitlyEnabled()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig = new SubscriptionConfig
        {
            EnforceLocalQuota = false,
            MaxRequestsPerMonth = 0,
            PayAsYouGoEnabled = false
        };
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "quota-disabled-provider");
        var avatarId = Guid.NewGuid();
        provider.Setup(x => x.LoadAvatarAsync(avatarId, 0))
            .ReturnsAsync(new OASISResult<IAvatar>(new Avatar { Id = avatarId }));
        providerManager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(providerManager).RouteRequestToProviderAsync<IAvatar>(
            new StorageOperationRequest { Operation = "LoadAvatar", AvatarId = avatarId },
            ProviderType.IPFSOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Id.Should().Be(avatarId);
        provider.Verify(x => x.LoadAvatarAsync(avatarId, 0), Times.Once);
    }

    [Fact]
    public async Task V2StorageMutationUsesReplicationQuotaAndDoesNotReachProviderWhenExhausted()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig = new SubscriptionConfig
        {
            EnforceLocalQuota = true,
            MaxRequestsPerMonth = int.MaxValue,
            MaxReplicationsPerMonth = 0,
            PayAsYouGoEnabled = false
        };
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "replication-quota-provider");
        providerManager.RegisterProvider(provider.Object);
        var holon = new Holon { Id = Guid.NewGuid() };

        var result = await new OASISHyperDrive(providerManager).RouteRequestToProviderAsync<IHolon>(
            new StorageOperationRequest { Operation = "SaveHolon", Payload = holon },
            ProviderType.IPFSOASIS);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HYPERDRIVE_QUOTA_EXCEEDED");
        result.Message.Should().Contain("Quota exceeded for Replications");
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task V2FailoverQuotaPreventsAnUnlicensedSecondaryProviderAttempt()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig = new SubscriptionConfig
        {
            EnforceLocalQuota = true,
            MaxRequestsPerMonth = int.MaxValue,
            MaxFailoversPerMonth = 0,
            PayAsYouGoEnabled = false
        };
        var providerManager = new ProviderManager(null, dna);
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "quota-primary");
        var secondary = CreateActiveProvider(ProviderType.IPFSOASIS, "quota-secondary");
        var avatarId = Guid.NewGuid();
        primary.Setup(x => x.LoadAvatarAsync(avatarId, 0)).ReturnsAsync(new OASISResult<IAvatar>
        {
            IsError = true,
            ErrorCount = 1,
            ErrorCode = "PRIMARY_UNAVAILABLE",
            Message = "primary unavailable"
        });
        secondary.Setup(x => x.LoadAvatarAsync(avatarId, 0))
            .ReturnsAsync(new OASISResult<IAvatar>(new Avatar { Id = avatarId }));
        providerManager.RegisterProvider(primary.Object);
        providerManager.RegisterProvider(secondary.Object);
        providerManager.SetAndReplaceAutoFailOverListForProviders(new[]
        {
            new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),
            new EnumValue<ProviderType>(ProviderType.IPFSOASIS)
        }).IsError.Should().BeFalse();

        var hyperDrive = new OASISHyperDrive(providerManager);
        var result = await hyperDrive.RouteRequestAsync<IAvatar>(
            new StorageOperationRequest
            {
                Operation = "LoadAvatar",
                AvatarId = avatarId,
                PreferredProvider = ProviderType.MongoDBOASIS
            });

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HYPERDRIVE_QUOTA_EXCEEDED");
        result.Message.Should().Contain("Quota exceeded for Failovers");
        hyperDrive.LastFailoverDiagnostic.WasQuotaBlocked.Should().BeTrue();
        hyperDrive.LastFailoverDiagnostic.Attempts.Should().ContainSingle()
            .Which.Provider.Should().Be(ProviderType.MongoDBOASIS);
        result.MetaData.Should().ContainKey("hyperDriveFailoverDiagnostic");
        primary.Verify(x => x.LoadAvatarAsync(avatarId, 0), Times.Once);
        secondary.Verify(x => x.LoadAvatarAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ExplicitV2LoadBalancingCannotBypassRequestQuota()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig = new SubscriptionConfig
        {
            EnforceLocalQuota = true,
            MaxRequestsPerMonth = 0,
            PayAsYouGoEnabled = false
        };
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "load-balance-quota-provider");
        providerManager.RegisterProvider(provider.Object);
        var avatarId = Guid.NewGuid();

        var result = await new OASISHyperDrive(providerManager).LoadBalanceRequestAsync<IAvatar>(
            new StorageOperationRequest
            {
                Operation = "LoadAvatar",
                AvatarId = avatarId,
                PreferredProvider = ProviderType.IPFSOASIS
            });

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("Quota exceeded for Requests");
        provider.Verify(x => x.LoadAvatarAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ExplicitV2FailoverCannotBypassFailoverQuota()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig = new SubscriptionConfig
        {
            EnforceLocalQuota = true,
            MaxFailoversPerMonth = 0,
            PayAsYouGoEnabled = false
        };
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "explicit-failover-quota-provider");
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndReplaceAutoFailOverListForProviders(new[]
        {
            new EnumValue<ProviderType>(ProviderType.IPFSOASIS)
        }).IsError.Should().BeFalse();

        var hyperDrive = new OASISHyperDrive(providerManager);
        var result = await hyperDrive.FailoverRequestAsync<IAvatar>(
            new StorageOperationRequest { Operation = "LoadAvatar", AvatarId = Guid.NewGuid() });

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("Quota exceeded for Failovers");
        hyperDrive.LastFailoverDiagnostic.IsExplicitRequest.Should().BeTrue();
        hyperDrive.LastFailoverDiagnostic.WasQuotaBlocked.Should().BeTrue();
        hyperDrive.LastFailoverDiagnostic.Attempts.Should().BeEmpty();
        result.MetaData.Should().ContainKey("hyperDriveFailoverDiagnostic");
        provider.Verify(x => x.LoadAvatarAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ExplicitV2ReplicationCannotBypassReplicationQuota()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig = new SubscriptionConfig
        {
            EnforceLocalQuota = true,
            MaxReplicationsPerMonth = 0,
            PayAsYouGoEnabled = false
        };
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "explicit-replication-quota-provider");
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndReplaceAutoReplicationListForProviders(new[]
        {
            new EnumValue<ProviderType>(ProviderType.IPFSOASIS)
        }).IsError.Should().BeFalse();

        var hyperDrive = new OASISHyperDrive(providerManager);
        var result = await hyperDrive.ReplicateRequestAsync<IHolon>(
            new StorageOperationRequest { Operation = "SaveHolon", Payload = new Holon { Id = Guid.NewGuid() } });

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("Quota exceeded for Replications");
        hyperDrive.LastReplicationDiagnostic.IsExplicitRequest.Should().BeTrue();
        hyperDrive.LastReplicationDiagnostic.WasQuotaBlocked.Should().BeTrue();
        hyperDrive.LastReplicationDiagnostic.Attempts.Should().BeEmpty();
        result.MetaData.Should().ContainKey("hyperDriveReplicationDiagnostic");
        provider.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void KeyManagerLoadsAvatarKeysThroughItsInjectedRuntime()
    {
        var avatarId = Guid.NewGuid();
        var avatar = new Avatar { Id = avatarId };
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.Security = new SecuritySettings();
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-keys");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatar(avatarId, 0))
            .Returns(new OASISResult<IAvatar>(avatar));
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new KeyManager(null, dna, providerManager);

        var result = manager.GetAllProviderPublicKeysForAvatarById(avatarId, ProviderType.IPFSOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().NotBeNull().And.BeEmpty();
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.Verify(x => x.LoadAvatar(avatarId, 0), Times.Once);
        provider.Verify(x => x.LoadAvatarAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void KeyManagerLoadsPrivateKeyWalletsThroughItsInjectedRuntime()
    {
        var avatarId = Guid.NewGuid();
        var avatar = new Avatar
        {
            Id = avatarId,
            ProviderWallets = new Dictionary<ProviderType, List<IProviderWallet>>
            {
                [ProviderType.IPFSOASIS] = new List<IProviderWallet>()
            }
        };
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.Security = new SecuritySettings();
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-private-key-wallets");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatar(avatarId, 0)).Returns(new OASISResult<IAvatar>(avatar));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        AvatarManager.LoggedInAvatar = avatar;
        try
        {
            var result = new KeyManager(null, dna, providerManager)
                .GetProviderPrivateKeysForAvatarById(avatarId, ProviderType.IPFSOASIS);

            result.IsError.Should().BeFalse(result.Message);
            result.Result.Should().NotBeNull().And.BeEmpty();
            ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
            provider.Verify(x => x.LoadAvatar(avatarId, 0), Times.Once);
        }
        finally
        {
            AvatarManager.LoggedInAvatar = null;
        }
    }

    [Fact]
    public void WalletImportUsesRuntimeScopedKeyManagerAndInjectedProvider()
    {
        var avatarId = Guid.NewGuid();
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.Security = new SecuritySettings();
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-wallet-import");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatar(avatarId, 0)).Returns(new OASISResult<IAvatar>
        {
            IsError = true, ErrorCount = 1, ErrorCode = "ISOLATED_AVATAR_UNAVAILABLE",
            Message = "injected avatar store unavailable"
        });
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;

        var result = new WalletManager(null, dna, providerManager)
            .ImportWalletUsingPublicKeyById(avatarId, "public-key", "wallet-address", ProviderType.IPFSOASIS);

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("injected avatar store unavailable");
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.Verify(x => x.LoadAvatar(avatarId, 0), Times.Once);
    }

    [Fact]
    public void AvatarDataLoadCannotEscapeItsInjectedRuntime()
    {
        var avatarId = Guid.NewGuid();
        var avatar = new Avatar { Id = avatarId };
        avatar.MetaData["edge-state"] = "offline-ready";
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-avatar-data");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatar(avatarId, 0))
            .Returns(new OASISResult<IAvatar>(avatar));
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new AvatarManager(null, dna, providerManager);

        var result = manager.LoadData("edge-state", avatarId, ProviderType.IPFSOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().Be("offline-ready");
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.Verify(x => x.LoadAvatar(avatarId, 0), Times.Once);
    }

    [Fact]
    public void HolonVisibilityAuthorizationCannotEscapeItsInjectedRuntime()
    {
        var avatarId = Guid.NewGuid();
        var wizard = new Avatar
        {
            Id = avatarId,
            AvatarType = new EnumValue<AvatarType>(AvatarType.Wizard)
        };
        var holons = new[] { new Holon { Id = Guid.NewGuid(), IsPublic = false } };
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-holon-authorization");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAllHolons(avatarId, false, HolonType.All, false, false,
                0, 0, true, false, 0))
            .Returns(new OASISResult<IEnumerable<IHolon>>(holons));
        provider.Setup(x => x.LoadAvatar(avatarId, 0))
            .Returns(new OASISResult<IAvatar>(wizard));
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new HolonManager(null, dna, providerManager);

        var result = manager.LoadAllHolons(loadChildren: false, recursive: false,
            providerType: ProviderType.IPFSOASIS, cache: false, avatarId: avatarId);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().ContainSingle().Which.Should().BeSameAs(holons[0]);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.Verify(x => x.LoadAvatar(avatarId, 0), Times.Once);
    }

    [Fact]
    public void KeyManagerProviderKeyLookupCannotEscapeItsInjectedRuntime()
    {
        var providerKey = $"provider-{Guid.NewGuid():N}";
        var avatar = new Avatar
        {
            Id = Guid.NewGuid(),
            Username = $"isolated-{Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@example.test"
        };
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.Security = new SecuritySettings();
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-provider-key");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarByProviderKey(providerKey))
            .Returns(new OASISResult<IAvatar>(avatar));
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new KeyManager(null, dna, providerManager);

        var result = manager.GetAvatarForProviderUniqueStorageKey(providerKey, ProviderType.IPFSOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(avatar);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.Verify(x => x.LoadAvatarByProviderKey(providerKey), Times.Once);
    }

    [Fact]
    public void KeyManagerChildPreservesTheExplicitProviderForDefaultV2Lookups()
    {
        var providerKey = $"explicit-provider-{Guid.NewGuid():N}";
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = $"key-{Guid.NewGuid():N}" };
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.Security = new SecuritySettings();
        var providerManager = new ProviderManager(null, dna);
        var runtimeDefault = CreateActiveProvider(ProviderType.MongoDBOASIS, "key-default");
        var explicitlySelected = CreateActiveProvider(ProviderType.IPFSOASIS, "key-explicit");
        runtimeDefault.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        explicitlySelected.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        explicitlySelected.Setup(x => x.LoadAvatarByProviderKey(providerKey))
            .Returns(new OASISResult<IAvatar>(avatar));
        providerManager.RegisterProvider(runtimeDefault.Object).Should().BeTrue();
        providerManager.RegisterProvider(explicitlySelected.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(runtimeDefault.Object).IsError.Should().BeFalse();

        var result = new KeyManager(explicitlySelected.Object, dna, providerManager)
            .GetAvatarForProviderUniqueStorageKey(providerKey);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(avatar);
        explicitlySelected.Verify(x => x.LoadAvatarByProviderKey(providerKey), Times.Once);
        runtimeDefault.Verify(x => x.LoadAvatarByProviderKey(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void KeyManagerPublicKeyLookupCannotEscapeItsInjectedRuntime()
    {
        var publicKey = $"public-{Guid.NewGuid():N}";
        var avatar = new Avatar
        {
            Id = Guid.NewGuid(),
            Username = $"isolated-{Guid.NewGuid():N}",
            Email = $"{Guid.NewGuid():N}@example.test"
        };
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.Security = new SecuritySettings();
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-public-key");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarByPublicKey(publicKey))
            .Returns(new OASISResult<IAvatar>(avatar));
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new KeyManager(null, dna, providerManager);

        var result = manager.GetAvatarForProviderPublicKey(publicKey, ProviderType.IPFSOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(avatar);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.Verify(x => x.LoadAvatarByPublicKey(publicKey), Times.Once);
    }

    [Fact]
    public async Task ViewingKeyAuditCannotEscapeItsInjectedRuntime()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, "isolated-viewing-key-audit");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync((IHolon holon, bool _, bool _, int _, bool _, bool _) =>
                new OASISResult<IHolon>(holon));
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var service = new ViewingKeyAuditService(new HolonManager(null, dna, providerManager));

        await service.RecordViewingKeyAsync(new ViewingKeyAuditEntry
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            ViewingKey = "isolated-viewing-key",
            UserId = Guid.NewGuid(),
            SourceChain = "ZEC",
            DestinationChain = "AZTEC",
            Timestamp = DateTime.UtcNow
        });

        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.Verify(x => x.SaveHolonAsync(
            It.Is<IHolon>(holon => holon.MetaData["ViewingKey"].ToString() == "isolated-viewing-key"),
            true, true, 0, true, false), Times.Once);
    }

    [Fact]
    public void V2ManagerProviderConfigurationRejectsASecondInstanceOfTheSameProviderType()
    {
        var registered = CreateActiveProvider(ProviderType.MongoDBOASIS, "registered-mongo");
        var differentInstance = CreateActiveProvider(ProviderType.MongoDBOASIS, "different-mongo");
        var providerManager = new ProviderManager(null);
        providerManager.RegisterProvider(registered.Object);

        var action = () => OASISManager.ConfigureStorageProvider(
            providerManager,
            differentInstance.Object,
            useHyperDriveV2: true);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*different MongoDBOASIS provider instance is already registered*");
        providerManager.GetStorageProvider(ProviderType.MongoDBOASIS).Should().BeSameAs(registered.Object);
    }

    [Fact]
    public void ProviderResolutionReturnsExplicitProviderWithoutChangingCurrentProvider()
    {
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var providerManager = CreateProviderManager();
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(selected.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();

        var result = providerManager.ResolveStorageProvider(ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse();
        result.Result.Should().BeSameAs(selected.Object);
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
    }

    [Fact]
    public async Task ProviderResolutionUsesCurrentForDefaultAndRejectsAll()
    {
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var providerManager = CreateProviderManager();
        providerManager.RegisterProvider(current.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();

        var defaultResult = await providerManager.ResolveStorageProviderAsync(ProviderType.Default);
        var allResult = providerManager.ResolveStorageProvider(ProviderType.All);

        defaultResult.IsError.Should().BeFalse();
        defaultResult.Result.Should().BeSameAs(current.Object);
        allResult.IsError.Should().BeTrue();
        allResult.Result.Should().BeNull();
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
    }

    [Fact]
    public void WalletLoadInV2UsesTheRequestedLocalProviderWithoutChangingCurrentProvider()
    {
        var avatarId = Guid.NewGuid();
        var wallets = new Dictionary<ProviderType, List<IProviderWallet>>();
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var local = new Mock<IOASISStorageProvider>();
        local.SetupAllProperties();
        var localWallets = local.As<IOASISLocalStorageProvider>();
        local.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        local.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.StorageLocal);
        local.Object.ProviderName = "edge-sqlite";
        local.Object.IsProviderActivated = true;
        localWallets.Setup(x => x.LoadProviderWalletsForAvatarById(avatarId))
            .Returns(new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>(wallets));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(local.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        var walletManager = new WalletManager(null, dna, providerManager);

        var result = walletManager.LoadProviderWalletsForAvatarById(
            avatarId,
            providerTypeToLoadFrom: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse();
        result.Result.Should().BeSameAs(wallets);
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        localWallets.Verify(x => x.LoadProviderWalletsForAvatarById(avatarId), Times.Once);
    }

    [Fact]
    public async Task WalletSaveInV2UsesTheRequestedLocalProviderWithoutChangingCurrentProvider()
    {
        var avatarId = Guid.NewGuid();
        var wallets = new Dictionary<ProviderType, List<IProviderWallet>>();
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var local = new Mock<IOASISStorageProvider>();
        local.SetupAllProperties();
        var localWallets = local.As<IOASISLocalStorageProvider>();
        local.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        local.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.StorageLocal);
        local.Object.ProviderName = "edge-sqlite";
        local.Object.IsProviderActivated = true;
        localWallets.Setup(x => x.SaveProviderWalletsForAvatarByIdAsync(avatarId, wallets))
            .ReturnsAsync(new OASISResult<bool>(true));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(local.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        var walletManager = new WalletManager(null, dna, providerManager);

        var result = await walletManager.SaveProviderWalletsForAvatarByIdAsync(
            avatarId,
            wallets,
            ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse();
        result.Result.Should().BeTrue();
        result.IsSaved.Should().BeTrue();
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        localWallets.Verify(x => x.SaveProviderWalletsForAvatarByIdAsync(avatarId, wallets), Times.Once);
    }

    [Fact]
    public async Task AvatarProviderHelperInV2DeletesFromRequestedProviderWithoutChangingCurrentProvider()
    {
        var avatarId = Guid.NewGuid();
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.DeleteAvatarAsync(avatarId, true))
            .ReturnsAsync(new OASISResult<bool>(true));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(selected.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        var avatarManager = new AvatarManager(null, dna, providerManager);

        var result = await avatarManager.DeleteAvatarForProviderAsync(
            avatarId,
            new OASISResult<bool>(),
            SaveMode.FirstSaveAttempt,
            true,
            ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse();
        result.Result.Should().BeTrue();
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        selected.Verify(x => x.DeleteAvatarAsync(avatarId, true), Times.Once);
    }

    [Fact]
    public async Task AvatarProviderHelperSurfacesNullProviderResultWithoutThrowing()
    {
        var avatarId = Guid.NewGuid();
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        selected.Setup(x => x.DeleteAvatarAsync(avatarId, true))
            .ReturnsAsync((OASISResult<bool>)null!);
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(selected.Object);
        var avatarManager = new AvatarManager(null, dna, providerManager);

        var result = await avatarManager.DeleteAvatarForProviderAsync(
            avatarId,
            new OASISResult<bool>(),
            SaveMode.FirstSaveAttempt,
            true,
            ProviderType.SQLLiteDBOASIS);

        result.Result.Should().BeFalse();
        result.WarningCount.Should().BeGreaterThan(0);
        result.Message.Should().Contain("Unknown");
    }

    [Fact]
    public async Task AvatarSaveHelperInV2UsesRequestedProviderWithoutChangingCurrentProvider()
    {
        var avatarMock = new Mock<IAvatar>();
        avatarMock.SetupAllProperties();
        avatarMock.Object.Id = Guid.NewGuid();
        avatarMock.Object.Name = "Edge Avatar";
        avatarMock.Object.Username = "edge-avatar";
        avatarMock.Object.ProviderWallets = new Dictionary<ProviderType, List<IProviderWallet>>();
        var avatar = avatarMock.Object;
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.SaveAvatarAsync(avatar))
            .ReturnsAsync(new OASISResult<IAvatar>(avatar));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(selected.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        var avatarManager = new AvatarManager(null, dna, providerManager);

        var result = await avatarManager.SaveAvatarForProviderAsync(
            avatar,
            new OASISResult<IAvatar>(),
            SaveMode.FirstSaveAttempt,
            ProviderType.SQLLiteDBOASIS);

        result.IsSaved.Should().BeTrue();
        result.Result.Should().BeSameAs(avatar);
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        selected.Verify(x => x.SaveAvatarAsync(avatar), Times.Once);
    }

    [Fact]
    public void AvatarManagerUsesItsInjectedDnaAndProviderManagerForV2Routing()
    {
        var avatarId = Guid.NewGuid();
        var avatar = new Mock<IAvatar>().Object;
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.LoadAvatar(avatarId, 0))
            .Returns(new OASISResult<IAvatar>(avatar));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(selected.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        var avatarManager = new AvatarManager(null, dna, providerManager);

        var result = avatarManager.LoadAvatar(
            avatarId,
            loadPrivateKeys: false,
            hideAuthDetails: false,
            providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse();
        result.Result.Should().BeSameAs(avatar);
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        selected.Verify(x => x.LoadAvatar(avatarId, 0), Times.Once);
    }

    [Fact]
    public async Task AvatarDetailDeletesRouteAllKeysWithoutChangingCurrentProvider()
    {
        var avatarId = Guid.NewGuid();
        const string username = "edge-user";
        const string email = "edge@example.test";
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.DeleteAvatarDetail(avatarId, true)).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.DeleteAvatarDetailByUsername(username, true)).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.DeleteAvatarDetailByEmail(email, true)).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.DeleteAvatarDetailAsync(avatarId, false)).ReturnsAsync(new OASISResult<bool>(true));
        selected.Setup(x => x.DeleteAvatarDetailByUsernameAsync(username, false)).ReturnsAsync(new OASISResult<bool>(true));
        selected.Setup(x => x.DeleteAvatarDetailByEmailAsync(email, false)).ReturnsAsync(new OASISResult<bool>(true));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(selected.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        var manager = new AvatarManager(null, dna, providerManager);

        manager.DeleteAvatarDetail(avatarId, providerType: ProviderType.SQLLiteDBOASIS).Result.Should().BeTrue();
        manager.DeleteAvatarDetailByUsername(username, providerType: ProviderType.SQLLiteDBOASIS).Result.Should().BeTrue();
        manager.DeleteAvatarDetailByEmail(email, providerType: ProviderType.SQLLiteDBOASIS).Result.Should().BeTrue();
        (await manager.DeleteAvatarDetailAsync(avatarId, false, ProviderType.SQLLiteDBOASIS)).Result.Should().BeTrue();
        (await manager.DeleteAvatarDetailByUsernameAsync(username, false, ProviderType.SQLLiteDBOASIS)).Result.Should().BeTrue();
        (await manager.DeleteAvatarDetailByEmailAsync(email, false, ProviderType.SQLLiteDBOASIS)).Result.Should().BeTrue();

        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        selected.VerifyAll();
    }

    [Fact]
    public async Task ProviderContractSoftDeletesAvatarDetailAndRejectsUnsupportedHardDelete()
    {
        var avatarId = Guid.NewGuid();
        var detail = new AvatarDetail { Id = avatarId, IsActive = true };
        var provider = new Mock<IOASISStorageProvider> { CallBase = true };
        provider.Setup(x => x.LoadAvatarDetailAsync(avatarId, 0))
            .ReturnsAsync(new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.SaveAvatarDetailAsync(detail))
            .ReturnsAsync(new OASISResult<IAvatarDetail>(detail));

        var softDelete = await provider.Object.DeleteAvatarDetailAsync(avatarId, true);
        var hardDelete = await provider.Object.DeleteAvatarDetailAsync(avatarId, false);

        softDelete.IsError.Should().BeFalse();
        softDelete.Result.Should().BeTrue();
        detail.IsActive.Should().BeFalse();
        detail.DeletedDate.Should().NotBe(default);
        hardDelete.IsError.Should().BeTrue();
        hardDelete.Message.Should().Contain("does not implement hard deletion");
        provider.Verify(x => x.SaveAvatarDetailAsync(detail), Times.Once);
    }

    [Fact]
    public void HolonManagerUsesItsInjectedDnaAndProviderManagerForV2Routing()
    {
        var holonId = Guid.NewGuid();
        var holon = new Mock<IHolon>().Object;
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.LoadHolon(holonId, false, false, 0, true, false, 0))
            .Returns(new OASISResult<IHolon>(holon));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(selected.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        var holonManager = new HolonManager(null, dna, providerManager);

        var result = holonManager.LoadHolon(
            holonId,
            loadChildren: false,
            recursive: false,
            providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse();
        result.Result.Should().BeSameAs(holon);
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        selected.Verify(x => x.LoadHolon(holonId, false, false, 0, true, false, 0), Times.Once);
    }

    [Theory]
    [InlineData(HyperDriveModes.Legacy)]
    [InlineData(HyperDriveModes.V2)]
    public async Task CorruptEncryptedHolonMetadataFailsTheLoadInsteadOfReturningCiphertext(string mode)
    {
        var holonId = Guid.NewGuid();
        var stored = new Holon
        {
            Id = holonId,
            MetaData = new Dictionary<string, object>
            {
                ["__oasis_enc__"] = "not-valid-ciphertext"
            }
        };
        var dna = CreateDna(mode);
        var encryptionSettings = new EncryptionSettings
        {
            Rijndael256EncryptionEnabled = true,
            Rijndael256Key = new string('k', 32)
        };
        dna.OASIS.Security = new SecuritySettings
        {
            HolonDataEncryption = encryptionSettings
        };
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, $"corrupt-encrypted-holon-{mode}");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadHolonAsync(holonId, false, false, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(stored));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new HolonManager(null, dna, providerManager);

        var result = await manager.LoadHolonAsync(
            holonId, loadChildren: false, recursive: false, providerType: ProviderType.IPFSOASIS);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HOLON_METADATA_DECRYPTION_FAILED");
        result.Result.Should().BeNull();
        result.Exception.Should().NotBeNull();
        stored.MetaData.Should().ContainKey("__oasis_enc__");
    }

    [Theory]
    [InlineData(HyperDriveModes.Legacy)]
    [InlineData(HyperDriveModes.V2)]
    public async Task ValidEncryptedHolonMetadataIsDecryptedAcrossBothRoutingModes(string mode)
    {
        var holonId = Guid.NewGuid();
        var stored = new Holon
        {
            Id = holonId,
            MetaData = new Dictionary<string, object> { ["secret"] = "nebula" }
        };
        var dna = CreateDna(mode);
        var encryptionSettings = new EncryptionSettings
        {
            Rijndael256EncryptionEnabled = true,
            Rijndael256Key = new string('k', 32)
        };
        dna.OASIS.Security = new SecuritySettings
        {
            HolonDataEncryption = encryptionSettings
        };
        var providerManager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider(ProviderType.IPFSOASIS, $"valid-encrypted-holon-{mode}");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var manager = new HolonManager(null, dna, providerManager);
        stored.MetaData.Clear();
        stored.MetaData["__oasis_enc__"] = NextGenSoftware.OASIS.API.Core.Helpers.PasswordEncryptionHelper.EncryptValue(
            "{\"secret\":\"nebula\"}", encryptionSettings);
        stored.MetaData.Should().ContainKey("__oasis_enc__");
        stored.MetaData.Should().NotContainKey("secret");
        provider.Setup(x => x.LoadHolonAsync(holonId, false, false, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(stored));
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await manager.LoadHolonAsync(
            holonId, loadChildren: false, recursive: false, providerType: ProviderType.IPFSOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().NotBeNull();
        result.Result.MetaData.Should().ContainKey("secret").WhoseValue.ToString().Should().Be("nebula");
        result.Result.MetaData.Should().NotContainKey("__oasis_enc__");
    }

    [Fact]
    public void HolonSearchUsesInjectedRuntimeAndForwardsRoutingOptions()
    {
        var holon = new Mock<IHolon>().Object;
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "isolated-holon-search");
        selected.Setup(x => x.Search(It.IsAny<ISearchParams>(), false, false, 3, false, 7))
            .Returns(new OASISResult<ISearchResults>(new SearchResults
            {
                SearchResultHolons = new List<IHolon> { holon }
            }));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(selected.Object).Should().BeTrue();
        var manager = new HolonManager(null, dna, providerManager);

        var result = manager.SearchHolons("nebula", Guid.NewGuid(), searchOnlyForCurrentAvatar: false,
            loadChildren: false, recursive: false,
            maxChildDepth: 3, continueOnError: false, version: 7,
            providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().ContainSingle().Which.Should().BeSameAs(holon);
        selected.Verify(x => x.Search(It.Is<ISearchParams>(p =>
            p.SearchGroups.OfType<SearchTextGroup>().Single().SearchQuery == "nebula"),
            false, false, 3, false, 7), Times.Once);
    }

    [Fact]
    public async Task HolonSearchAsyncPreservesCompleteV2ProviderSuccessDiagnostics()
    {
        var holon = new Holon { Id = Guid.NewGuid() };
        var providerResult = new OASISResult<ISearchResults>(new SearchResults
        {
            SearchResultHolons = new List<IHolon> { holon }
        })
        {
            IsWarning = true,
            WarningCount = 2,
            Message = "search completed with provider warning",
            DetailedMessage = "authoritative search provider detail",
            InnerMessages = new List<string> { "partial index diagnostic" },
            MetaData = new Dictionary<string, string> { ["provider"] = "search-diagnostics" },
            ResultsCount = 1
        };
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "search-diagnostics");
        selected.Setup(x => x.SearchAsync(It.IsAny<ISearchParams>(), true, true, 0, true, 0))
            .ReturnsAsync(providerResult);
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false };
        providerManager.RegisterProvider(selected.Object).Should().BeTrue();
        var manager = new HolonManager(null, dna, providerManager);

        var result = await manager.SearchHolonsAsync<Holon>("nebula", Guid.NewGuid(),
            searchOnlyForCurrentAvatar: false,
            providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.IsWarning.Should().BeTrue();
        result.WarningCount.Should().Be(2);
        result.Message.Should().Be(providerResult.Message);
        result.DetailedMessage.Should().Be(providerResult.DetailedMessage);
        result.InnerMessages.Should().Equal(providerResult.InnerMessages);
        result.MetaData.Should().Contain("provider", "search-diagnostics");
        result.ResultsCount.Should().Be(1);
        result.Result.Should().ContainSingle().Which.Id.Should().Be(holon.Id);
        selected.Verify(x => x.SearchAsync(It.IsAny<ISearchParams>(), true, true, 0, true, 0), Times.Once);
    }

    [Fact]
    public async Task TypedMetadataLoadPreservesCompleteV2ProviderSuccessDiagnostics()
    {
        var holon = new Holon { Id = Guid.NewGuid() };
        var providerResult = new OASISResult<IEnumerable<IHolon>>(new[] { holon })
        {
            IsWarning = true,
            WarningCount = 2,
            Message = "metadata load completed with provider warning",
            DetailedMessage = "authoritative metadata provider detail",
            InnerMessages = new List<string> { "metadata index diagnostic" },
            MetaData = new Dictionary<string, string> { ["provider"] = "metadata-diagnostics" },
            ResultsCount = 1
        };
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "metadata-diagnostics");
        selected.Setup(x => x.LoadHolonsByMetaDataAsync("kind", "quest", HolonType.All,
                true, true, 0, 0, true, false, 0))
            .ReturnsAsync(providerResult);
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false };
        providerManager.RegisterProvider(selected.Object).Should().BeTrue();

        var result = await new HolonManager(null, dna, providerManager)
            .LoadHolonByMetaDataAsync<Holon>("kind", "quest",
                providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.IsWarning.Should().BeTrue();
        result.WarningCount.Should().Be(2);
        result.Message.Should().Be(providerResult.Message);
        result.DetailedMessage.Should().Be(providerResult.DetailedMessage);
        result.InnerMessages.Should().Equal(providerResult.InnerMessages);
        result.MetaData.Should().Contain("provider", "metadata-diagnostics");
        result.ResultsCount.Should().Be(1);
        result.IsLoaded.Should().BeTrue();
        result.Result.Should().NotBeNull();
        result.Result.Id.Should().Be(holon.Id);
        selected.VerifyAll();
    }

    [Fact]
    public void MetadataLoadWithoutMatchesReturnsExplicitWarning()
    {
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "metadata-empty");
        selected.Setup(x => x.LoadHolonsByMetaData("kind", "missing", HolonType.All,
                true, true, 0, 0, true, false, 0))
            .Returns(new OASISResult<IEnumerable<IHolon>>(Array.Empty<IHolon>()));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false };
        providerManager.RegisterProvider(selected.Object).Should().BeTrue();

        var result = new HolonManager(null, dna, providerManager).LoadHolonByMetaData(
            "kind", "missing", providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.IsWarning.Should().BeTrue();
        result.WarningCount.Should().BeGreaterThan(0);
        result.Message.Should().Be("No holon found");
        result.IsLoaded.Should().BeFalse();
        result.Result.Should().BeNull();
        selected.VerifyAll();
    }

    [Fact]
    public async Task TypedHardDeletePreservesCompleteV2DiagnosticsAndDeleteState()
    {
        var holonId = Guid.NewGuid();
        var holon = new Holon { Id = holonId };
        var providerResult = new OASISResult<IHolon>(holon)
        {
            IsWarning = true,
            WarningCount = 2,
            Message = "delete completed with provider warning",
            DetailedMessage = "authoritative delete provider detail",
            InnerMessages = new List<string> { "replica deletion pending" },
            MetaData = new Dictionary<string, string> { ["provider"] = "delete-diagnostics" },
            DeletedCount = 1
        };
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "delete-diagnostics");
        selected.Setup(x => x.LoadHolonAsync(holonId, true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(holon));
        selected.Setup(x => x.DeleteHolonAsync(holonId)).ReturnsAsync(providerResult);
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false };
        providerManager.RegisterProvider(selected.Object).Should().BeTrue();

        var result = await new HolonManager(null, dna, providerManager).DeleteHolonAsync<Holon>(
            holonId, Guid.NewGuid(), softDelete: false, providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.IsDeleted.Should().BeTrue();
        result.IsSaved.Should().BeFalse();
        result.IsWarning.Should().BeTrue();
        result.WarningCount.Should().Be(2);
        result.DeletedCount.Should().Be(1);
        result.Message.Should().Be(providerResult.Message);
        result.DetailedMessage.Should().Be(providerResult.DetailedMessage);
        result.InnerMessages.Should().Equal(providerResult.InnerMessages);
        result.MetaData.Should().Contain("provider", "delete-diagnostics");
        result.Result.Should().NotBeNull();
        result.Result.Id.Should().Be(holonId);
        selected.VerifyAll();
    }

    [Fact]
    public async Task AvatarDeleteMatrixMarksEverySuccessfulV2ResultDeleted()
    {
        var avatarId = Guid.NewGuid();
        OASISResult<bool> DeleteResult() => new(true)
        {
            IsWarning = true,
            WarningCount = 1,
            DetailedMessage = "authoritative avatar delete detail",
            InnerMessages = new List<string> { "delete diagnostic" },
            MetaData = new Dictionary<string, string> { ["provider"] = "avatar-delete-matrix" },
            DeletedCount = 1
        };
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "avatar-delete-matrix");
        selected.Setup(x => x.DeleteAvatar(avatarId, false)).Returns(DeleteResult);
        selected.Setup(x => x.DeleteAvatarAsync(avatarId, false)).ReturnsAsync(DeleteResult);
        selected.Setup(x => x.DeleteAvatarByUsername("edge", false)).Returns(DeleteResult);
        selected.Setup(x => x.DeleteAvatarByUsernameAsync("edge", false)).ReturnsAsync(DeleteResult);
        selected.Setup(x => x.DeleteAvatarByEmail("edge@example.test", false)).Returns(DeleteResult);
        selected.Setup(x => x.DeleteAvatarByEmailAsync("edge@example.test", false)).ReturnsAsync(DeleteResult);
        selected.Setup(x => x.DeleteAvatarDetail(avatarId, false)).Returns(DeleteResult);
        selected.Setup(x => x.DeleteAvatarDetailAsync(avatarId, false)).ReturnsAsync(DeleteResult);
        selected.Setup(x => x.DeleteAvatarDetailByUsername("edge", false)).Returns(DeleteResult);
        selected.Setup(x => x.DeleteAvatarDetailByUsernameAsync("edge", false)).ReturnsAsync(DeleteResult);
        selected.Setup(x => x.DeleteAvatarDetailByEmail("edge@example.test", false)).Returns(DeleteResult);
        selected.Setup(x => x.DeleteAvatarDetailByEmailAsync("edge@example.test", false)).ReturnsAsync(DeleteResult);
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false };
        providerManager.RegisterProvider(selected.Object).Should().BeTrue();
        var manager = new AvatarManager(null, dna, providerManager);

        var results = new List<OASISResult<bool>>
        {
            manager.DeleteAvatar(avatarId, false, ProviderType.SQLLiteDBOASIS),
            await manager.DeleteAvatarAsync(avatarId, false, ProviderType.SQLLiteDBOASIS),
            manager.DeleteAvatarByUsername("edge", false, ProviderType.SQLLiteDBOASIS),
            await manager.DeleteAvatarByUsernameAsync("edge", false, ProviderType.SQLLiteDBOASIS),
            manager.DeleteAvatarByEmail("edge@example.test", false, ProviderType.SQLLiteDBOASIS),
            await manager.DeleteAvatarByEmailAsync("edge@example.test", false, ProviderType.SQLLiteDBOASIS),
            manager.DeleteAvatarDetail(avatarId, false, ProviderType.SQLLiteDBOASIS),
            await manager.DeleteAvatarDetailAsync(avatarId, false, ProviderType.SQLLiteDBOASIS),
            manager.DeleteAvatarDetailByUsername("edge", false, ProviderType.SQLLiteDBOASIS),
            await manager.DeleteAvatarDetailByUsernameAsync("edge", false, ProviderType.SQLLiteDBOASIS),
            manager.DeleteAvatarDetailByEmail("edge@example.test", false, ProviderType.SQLLiteDBOASIS),
            await manager.DeleteAvatarDetailByEmailAsync("edge@example.test", false, ProviderType.SQLLiteDBOASIS)
        };

        results.Should().HaveCount(12).And.OnlyContain(result =>
            !result.IsError && result.Result && result.IsDeleted && result.WarningCount == 1 &&
            result.DeletedCount == 1 && result.DetailedMessage == "authoritative avatar delete detail" &&
            result.MetaData.ContainsKey("provider"));
        selected.VerifyAll();
    }

    [Fact]
    public void SynchronousAvatarSaveUsesV2RouterAndPreservesProviderDiagnostics()
    {
        var callingThread = Environment.CurrentManagedThreadId;
        var providerThread = 0;
        var avatar = new Avatar
        {
            Id = Guid.NewGuid(),
            Username = "edge",
            Email = "edge@example.test",
            Password = "$2a$11$012345678901234567890u01234567890123456789012345678901"
        };
        var providerResult = new OASISResult<IAvatar>(avatar)
        {
            IsWarning = true,
            WarningCount = 1,
            Message = "avatar saved with provider warning",
            DetailedMessage = "authoritative avatar save detail",
            InnerMessages = new List<string> { "secondary index pending" },
            MetaData = new Dictionary<string, string> { ["provider"] = "avatar-save-v2" },
            SavedCount = 1
        };
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "avatar-save-v2");
        selected.Setup(x => x.SaveAvatar(avatar))
            .Callback(() => providerThread = Environment.CurrentManagedThreadId)
            .Returns(providerResult);
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false };
        providerManager.RegisterProvider(selected.Object).Should().BeTrue();

        var result = new AvatarManager(null, dna, providerManager).SaveAvatar(
            avatar, providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.IsSaved.Should().BeTrue();
        result.IsWarning.Should().BeTrue();
        result.WarningCount.Should().Be(1);
        result.SavedCount.Should().Be(1);
        result.Message.Should().Be(providerResult.Message);
        result.DetailedMessage.Should().Be(providerResult.DetailedMessage);
        result.InnerMessages.Should().Equal(providerResult.InnerMessages);
        result.MetaData.Should().Contain("provider", "avatar-save-v2");
        result.Result.Should().BeSameAs(avatar);
        providerThread.Should().Be(callingThread);
        selected.Verify(x => x.SaveAvatar(avatar), Times.Once);
        selected.Verify(x => x.SaveAvatarAsync(It.IsAny<IAvatar>()), Times.Never);
    }

    [Fact]
    public async Task DerivedAvatarNameQueriesPreserveCompleteV2ProviderDiagnostics()
    {
        var avatar = new Avatar
        {
            Id = Guid.NewGuid(),
            FirstName = "Edge",
            LastName = "Player",
            Username = "edge-player"
        };
        OASISResult<IEnumerable<IAvatar>> ProviderResult() => new(new IAvatar[] { avatar })
        {
            IsWarning = true,
            WarningCount = 2,
            Message = "avatars loaded with provider warning",
            DetailedMessage = "authoritative avatar collection detail",
            InnerMessages = new List<string> { "secondary avatar index pending" },
            MetaData = new Dictionary<string, string> { ["provider"] = "avatar-name-projection" },
            ResultsCount = 1,
            LoadedCount = 1
        };
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "avatar-name-projection");
        selected.Setup(x => x.LoadAllAvatars(0)).Returns(ProviderResult);
        selected.Setup(x => x.LoadAllAvatarsAsync(0))
            .Returns(() => Task.FromResult(ProviderResult()));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false };
        providerManager.RegisterProvider(selected.Object).Should().BeTrue();
        var manager = new AvatarManager(null, dna, providerManager);

        var names = manager.LoadAllAvatarNames(providerType: ProviderType.SQLLiteDBOASIS);
        var namesAsync = await manager.LoadAllAvatarNamesAsync(providerType: ProviderType.SQLLiteDBOASIS);
        var grouped = manager.LoadAllAvatarNamesGroupedByName(providerType: ProviderType.SQLLiteDBOASIS);
        var groupedAsync = await manager.LoadAllAvatarNamesGroupedByNameAsync(providerType: ProviderType.SQLLiteDBOASIS);

        names.Result.Should().ContainSingle().Which.Should().Contain(avatar.Id.ToString()).And.Contain(avatar.Username);
        namesAsync.Result.Should().Equal(names.Result);
        grouped.Result.Should().ContainKey(avatar.FullName).WhoseValue.Should().ContainSingle()
            .Which.Should().Contain(avatar.Id.ToString()).And.Contain(avatar.Username);
        groupedAsync.Result.Should().BeEquivalentTo(grouped.Result);
        names.IsLoaded.Should().BeTrue();
        namesAsync.IsLoaded.Should().BeTrue();
        grouped.IsLoaded.Should().BeTrue();
        groupedAsync.IsLoaded.Should().BeTrue();

        names.WarningCount.Should().Be(2);
        namesAsync.WarningCount.Should().Be(2);
        grouped.WarningCount.Should().Be(2);
        groupedAsync.WarningCount.Should().Be(2);
        names.DetailedMessage.Should().Be("authoritative avatar collection detail");
        namesAsync.InnerMessages.Should().ContainSingle("secondary avatar index pending");
        grouped.MetaData.Should().Contain("provider", "avatar-name-projection");
        groupedAsync.ResultsCount.Should().Be(1);
        groupedAsync.LoadedCount.Should().Be(1);
        selected.Verify(x => x.LoadAllAvatars(0), Times.Exactly(2));
        selected.Verify(x => x.LoadAllAvatarsAsync(0), Times.Exactly(2));
    }

    [Fact]
    public async Task LegacyAvatarDetailUsernameFailoverUsesTheUsernameProviderContract()
    {
        const string username = "edge-player";
        var avatarDetail = new AvatarDetail { Id = Guid.NewGuid(), Username = username };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "username-primary");
        var secondary = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "username-secondary");
        primary.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        secondary.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        primary.Setup(x => x.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        secondary.Setup(x => x.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
        primary.Setup(x => x.LoadAvatarDetailByUsername(username, 4))
            .Returns(() => new OASISResult<IAvatarDetail>());
        secondary.Setup(x => x.LoadAvatarDetailByUsername(username, 4))
            .Returns(() => new OASISResult<IAvatarDetail>(avatarDetail));
        primary.Setup(x => x.LoadAvatarDetailByUsernameAsync(username, 4))
            .ReturnsAsync(() => new OASISResult<IAvatarDetail>());
        secondary.Setup(x => x.LoadAvatarDetailByUsernameAsync(username, 4))
            .ReturnsAsync(() => new OASISResult<IAvatarDetail>(avatarDetail));
        var dna = CreateDna(HyperDriveModes.Legacy);
        var providerManager = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = true, IsAutoLoadBalanceEnabled = false };
        providerManager.RegisterProvider(primary.Object).Should().BeTrue();
        providerManager.RegisterProvider(secondary.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        providerManager.SetAndReplaceAutoFailOverListForProviders(new[]
        {
            new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),
            new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)
        }).IsError.Should().BeFalse();
        var manager = new AvatarManager(null, dna, providerManager);

        var syncResult = manager.LoadAvatarDetailByUsername(username,
            ProviderType.MongoDBOASIS, version: 4);
        var asyncResult = await manager.LoadAvatarDetailByUsernameAsync(username,
            ProviderType.MongoDBOASIS, version: 4);

        syncResult.IsError.Should().BeFalse(syncResult.Message);
        asyncResult.IsError.Should().BeFalse(asyncResult.Message);
        syncResult.Result.Should().NotBeNull().And.BeSameAs(avatarDetail);
        asyncResult.Result.Should().NotBeNull().And.BeSameAs(avatarDetail);
        primary.Verify(x => x.LoadAvatarDetailByUsername(username, 4), Times.Once);
        secondary.Verify(x => x.LoadAvatarDetailByUsername(username, 4), Times.Once);
        primary.Verify(x => x.LoadAvatarDetailByUsernameAsync(username, 4), Times.Once);
        secondary.Verify(x => x.LoadAvatarDetailByUsernameAsync(username, 4), Times.Once);
        primary.Verify(x => x.LoadAvatarDetailByEmail(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        secondary.Verify(x => x.LoadAvatarDetailByEmail(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        primary.Verify(x => x.LoadAvatarDetailByEmailAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        secondary.Verify(x => x.LoadAvatarDetailByEmailAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task HolonSearchAsyncPreservesInjectedProviderFailure()
    {
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "isolated-holon-search-failure");
        selected.Setup(x => x.SearchAsync(It.IsAny<ISearchParams>(), true, true, 0, true, 0))
            .ReturnsAsync(new OASISResult<ISearchResults>
            {
                IsError = true, ErrorCount = 1, ErrorCode = "SEARCH_STORAGE_OFFLINE",
                Message = "isolated provider is offline"
            });
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(selected.Object).Should().BeTrue();
        var manager = new HolonManager(null, dna, providerManager);

        var result = await manager.SearchHolonsAsync("nebula", Guid.NewGuid(),
            providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HYPERDRIVE_FAILOVER_EXHAUSTED");
        result.Message.Should().Contain("isolated provider is offline");
        selected.Verify(x => x.SearchAsync(It.IsAny<ISearchParams>(), true, true, 0, true, 0), Times.Once);
    }

    [Fact]
    public void HolonSearchRejectsProviderSuccessWithoutAResultPayload()
    {
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "isolated-holon-search-empty");
        selected.Setup(x => x.Search(It.IsAny<ISearchParams>(), true, true, 0, true, 0))
            .Returns(new OASISResult<ISearchResults>());
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(selected.Object).Should().BeTrue();

        var result = new HolonManager(null, dna, providerManager).SearchHolons(
            "nebula", Guid.NewGuid(), providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HOLON_SEARCH_FAILED");
        result.Message.Should().Be("The search provider returned no result.");
    }

    [Fact]
    public async Task HyperDriveUsesTheInjectedRuntimeSubscriptionPolicy()
    {
        var holonId = Guid.NewGuid();
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.SubscriptionConfig = new SubscriptionConfig
        {
            EnforceLocalQuota = true,
            MaxRequestsPerMonth = 0,
            PayAsYouGoEnabled = false
        };
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(selected.Object);
        var holonManager = new HolonManager(null, dna, providerManager);

        var result = await holonManager.LoadHolonAsync(
            holonId,
            loadChildren: false,
            recursive: false,
            providerType: ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeTrue();
        result.Message.Should().Contain("Quota exceeded for Requests");
        selected.Verify(x => x.LoadHolonAsync(
            It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>(),
            It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void SearchManagerUsesItsInjectedDnaAndProviderManagerForV2Routing()
    {
        ISearchParams search = new SearchParams();
        ISearchResults searchResults = new SearchResults();
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current-mongo");
        var selected = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "selected-sqlite");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        selected.Setup(x => x.Search(search, false, false, 0, true, 0))
            .Returns(new OASISResult<ISearchResults>(searchResults));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(current.Object);
        providerManager.RegisterProvider(selected.Object);
        providerManager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        var searchManager = new SearchManager(null, dna, providerManager);

        var result = searchManager.Search(
            search,
            ProviderType.SQLLiteDBOASIS,
            loadChildren: false,
            recursive: false);

        result.IsError.Should().BeFalse();
        result.Result.Should().NotBeNull();
        providerManager.CurrentStorageProvider.Should().BeSameAs(current.Object);
        selected.Verify(x => x.Search(search, false, false, 0, true, 0), Times.Once);
    }

    [Fact]
    public void SearchAllEnumeratesOnlyTheManagersInjectedRuntimeProviders()
    {
        ISearchParams search = new SearchParams();
        var mongo = CreateActiveProvider(ProviderType.MongoDBOASIS, "isolated-search-mongo");
        var sqlite = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "isolated-search-sqlite");
        mongo.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        mongo.Setup(x => x.Search(search, true, true, 0, true, 0))
            .Returns(new OASISResult<ISearchResults>(new SearchResults()));
        sqlite.Setup(x => x.Search(search, true, true, 0, true, 0))
            .Returns(new OASISResult<ISearchResults>(new SearchResults()));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(mongo.Object).Should().BeTrue();
        providerManager.RegisterProvider(sqlite.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(mongo.Object).IsError.Should().BeFalse();

        var result = new SearchManager(null, dna, providerManager)
            .Search(search, ProviderType.All);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().NotBeNull();
        providerManager.CurrentStorageProvider.Should().BeSameAs(mongo.Object);
        mongo.Verify(x => x.Search(search, true, true, 0, true, 0), Times.Once);
        sqlite.Verify(x => x.Search(search, true, true, 0, true, 0), Times.Once);
    }

    [Fact]
    public void SynchronousStorageOperationUsesSynchronousProviderWithoutChangingGlobalProvider()
    {
        var holonId = Guid.NewGuid();
        var holon = new Mock<IHolon>().Object;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadHolon(holonId, false, false, 3, false, true, 7))
            .Returns(new OASISResult<IHolon> { Result = holon });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object).Should().BeTrue();
        var original = manager.CurrentStorageProviderType.Value;

        var result = new OASISHyperDrive(manager).RouteRequestToProvider<IHolon>(new StorageOperationRequest
        {
            Operation = "LoadHolon", HolonId = holonId, LoadChildren = false, Recursive = false,
            MaxChildDepth = 3, ContinueOnError = false, ChildrenFromProvider = true, Version = 7
        }, ProviderType.MongoDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holon);
        manager.CurrentStorageProviderType.Value.Should().Be(original);
        provider.Verify(x => x.LoadHolon(holonId, false, false, 3, false, true, 7), Times.Once);
        provider.Verify(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void SynchronousUnsupportedOperationReturnsStructuredErrorWithoutLegacyFallback()
    {
        var provider = CreateActiveProvider();
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = new OASISHyperDrive(manager).RouteRequestToProvider<object>(new StorageOperationRequest
        { Operation = "NotARealOperation" }, ProviderType.MongoDBOASIS);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HYPERDRIVE_OPERATION_UNSUPPORTED");
    }

    [Fact]
    public void SynchronousBatchSavePreservesEveryProviderOption()
    {
        var holons = new[] { new Mock<IHolon>().Object, new Mock<IHolon>().Object };
        var provider = CreateActiveProvider();
        provider.Setup(x => x.SaveHolons(holons, false, false, 5, 2, false, true))
            .Returns(new OASISResult<IEnumerable<IHolon>> { Result = holons });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = new OASISHyperDrive(manager).RouteRequestToProvider<IEnumerable<IHolon>>(
            new StorageOperationRequest
            {
                Operation = "SaveHolons", Payload = holons, SaveChildren = false, Recursive = false,
                MaxChildDepth = 5, CurrentChildDepth = 2, ContinueOnError = false,
                ChildrenFromProvider = true
            }, ProviderType.MongoDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holons);
        provider.Verify(x => x.SaveHolons(holons, false, false, 5, 2, false, true), Times.Once);
    }

    [Fact]
    public void SynchronousMetadataLoadPreservesScopeAndTraversalOptions()
    {
        var avatarId = Guid.NewGuid();
        var holons = new[] { new Mock<IHolon>().Object };
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadHolonsByMetaData("kind", "quest", avatarId, true,
                HolonType.Quest, false, false, 4, 1, false, true, 3))
            .Returns(new OASISResult<IEnumerable<IHolon>> { Result = holons });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = new OASISHyperDrive(manager).RouteRequestToProvider<IEnumerable<IHolon>>(
            new StorageOperationRequest
            {
                Operation = "LoadHolonsByMetaData", MetaKey = "kind", MetaValue = "quest",
                AvatarId = avatarId, IncludePublic = true, HolonType = HolonType.Quest,
                LoadChildren = false, Recursive = false, MaxChildDepth = 4, CurrentChildDepth = 1,
                ContinueOnError = false, ChildrenFromProvider = true, Version = 3
            }, ProviderType.MongoDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holons);
        provider.VerifyAll();
    }

    [Fact]
    public void SynchronousAvatarOperationsUseOnlySynchronousProviderContracts()
    {
        var avatarId = Guid.NewGuid();
        var avatar = new Mock<IAvatar>().Object;
        var detail = new Mock<IAvatarDetail>().Object;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadAvatar(avatarId, 4)).Returns(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.SaveAvatar(avatar)).Returns(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.LoadAvatarDetailByEmail("edge@example.com", 4))
            .Returns(new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.DeleteAvatarByUsername("edge", true))
            .Returns(new OASISResult<bool>(true));
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);
        var hyperDrive = new OASISHyperDrive(manager);

        hyperDrive.RouteRequestToProvider<IAvatar>(new StorageOperationRequest
            { Operation = "LoadAvatar", AvatarId = avatarId, Version = 4 }, ProviderType.MongoDBOASIS)
            .Result.Should().BeSameAs(avatar);
        hyperDrive.RouteRequestToProvider<IAvatar>(new StorageOperationRequest
            { Operation = "SaveAvatar", Payload = avatar }, ProviderType.MongoDBOASIS)
            .Result.Should().BeSameAs(avatar);
        hyperDrive.RouteRequestToProvider<IAvatarDetail>(new StorageOperationRequest
            { Operation = "LoadAvatarDetailByEmail", Email = "edge@example.com", Version = 4 }, ProviderType.MongoDBOASIS)
            .Result.Should().BeSameAs(detail);
        hyperDrive.RouteRequestToProvider<bool>(new StorageOperationRequest
            { Operation = "DeleteAvatarByUsername", Username = "edge", SoftDelete = true }, ProviderType.MongoDBOASIS)
            .Result.Should().BeTrue();

        provider.VerifyAll();
        provider.Verify(x => x.LoadAvatarAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        provider.Verify(x => x.SaveAvatarAsync(It.IsAny<IAvatar>()), Times.Never);
    }

    [Fact]
    public void SynchronousSearchPreservesQueryAndNeverInvokesAsyncProvider()
    {
        ISearchParams search = new SearchParams { AvatarId = Guid.NewGuid(), SearchOnlyForCurrentAvatar = true };
        ISearchResults searchResults = new SearchResults();
        var provider = CreateActiveProvider();
        provider.Setup(x => x.Search(search, false, false, 4, false, 9))
            .Returns(new OASISResult<ISearchResults>(searchResults));
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = new OASISHyperDrive(manager).RouteRequestToProvider<ISearchResults>(
            new StorageOperationRequest
            {
                Operation = "Search", SearchParams = search, LoadChildren = false, Recursive = false,
                MaxChildDepth = 4, ContinueOnError = false, Version = 9
            }, ProviderType.MongoDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(searchResults);
        provider.Verify(x => x.Search(search, false, false, 4, false, 9), Times.Once);
        provider.Verify(x => x.SearchAsync(It.IsAny<ISearchParams>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task StorageOperationExecutesDirectlyOnPreferredProviderWithoutChangingGlobalProvider()
    {
        var holonId = Guid.NewGuid();
        var holon = new Mock<IHolon>().Object;
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.MongoDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.ProviderName = "test-mongo";
        provider.Object.IsProviderActivated = true;
        provider.Setup(x => x.LoadHolonAsync(holonId, true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon> { Result = holon });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object).Should().BeTrue();
        var originalGlobalProvider = manager.CurrentStorageProviderType.Value;
        var hyperDrive = new OASISHyperDrive(manager);

        var result = await hyperDrive.LoadBalanceRequestAsync<IHolon>(new StorageOperationRequest
        {
            Operation = "LoadHolon", HolonId = holonId, PreferredProvider = ProviderType.MongoDBOASIS
        });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holon);
        manager.CurrentStorageProviderType.Value.Should().Be(originalGlobalProvider);
        provider.Verify(x => x.LoadHolonAsync(holonId, true, true, 0, true, false, 0), Times.Once);
    }

    [Fact]
    public async Task UnsupportedStorageOperationReturnsStructuredErrorWithoutCallingProvider()
    {
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.MongoDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.ProviderName = "test-mongo";
        provider.Object.IsProviderActivated = true;
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<object>(new StorageOperationRequest
        { Operation = "NotARealOperation", PreferredProvider = ProviderType.MongoDBOASIS });

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HYPERDRIVE_OPERATION_UNSUPPORTED");
    }

    [Fact]
    public async Task HolonLoadPreservesEveryManagerOptionAtTheProviderBoundary()
    {
        var holonId = Guid.NewGuid();
        var holon = new Mock<IHolon>().Object;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadHolonAsync(holonId, false, false, 3, false, true, 7))
            .ReturnsAsync(new OASISResult<IHolon> { Result = holon });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IHolon>(new StorageOperationRequest
        {
            Operation = "LoadHolon",
            HolonId = holonId,
            PreferredProvider = ProviderType.MongoDBOASIS,
            LoadChildren = false,
            Recursive = false,
            MaxChildDepth = 3,
            ContinueOnError = false,
            ChildrenFromProvider = true,
            Version = 7
        });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holon);
        provider.Verify(x => x.LoadHolonAsync(holonId, false, false, 3, false, true, 7), Times.Once);
    }

    [Fact]
    public async Task ProviderKeyLoadPreservesKeyAndEveryOptionAtTheProviderBoundary()
    {
        const string providerKey = "provider-native-key";
        var holon = new Mock<IHolon>().Object;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadHolonAsync(providerKey, false, false, 4, false, true, 9))
            .ReturnsAsync(new OASISResult<IHolon> { Result = holon });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IHolon>(new StorageOperationRequest
        {
            Operation = "LoadHolonByProviderKey",
            ProviderKey = providerKey,
            PreferredProvider = ProviderType.MongoDBOASIS,
            LoadChildren = false,
            Recursive = false,
            MaxChildDepth = 4,
            ContinueOnError = false,
            ChildrenFromProvider = true,
            Version = 9
        });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holon);
        provider.Verify(x => x.LoadHolonAsync(providerKey, false, false, 4, false, true, 9), Times.Once);
    }

    [Fact]
    public async Task ProviderKeyLoadRejectsAnEmptyKeyBeforeCallingProvider()
    {
        var provider = CreateActiveProvider();
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IHolon>(new StorageOperationRequest
        {
            Operation = "LoadHolonByProviderKey",
            ProviderKey = " ",
            PreferredProvider = ProviderType.MongoDBOASIS
        });

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HYPERDRIVE_PROVIDER_KEY_REQUIRED");
        provider.Verify(x => x.LoadHolonAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task BatchSavePreservesEveryOptionAtTheProviderBoundary()
    {
        var holons = new[] { new Mock<IHolon>().Object, new Mock<IHolon>().Object };
        var provider = CreateActiveProvider();
        provider.Setup(x => x.SaveHolonsAsync(holons, false, false, 5, 2, false, true))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { Result = holons });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IEnumerable<IHolon>>(new StorageOperationRequest
        {
            Operation = "SaveHolons",
            Payload = holons,
            PreferredProvider = ProviderType.MongoDBOASIS,
            SaveChildren = false,
            Recursive = false,
            MaxChildDepth = 5,
            CurrentChildDepth = 2,
            ContinueOnError = false,
            ChildrenFromProvider = true
        });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holons);
        provider.Verify(x => x.SaveHolonsAsync(holons, false, false, 5, 2, false, true), Times.Once);
    }

    [Fact]
    public async Task HardDeleteExecutesDirectlyOnPreferredProvider()
    {
        var holonId = Guid.NewGuid();
        var provider = CreateActiveProvider();
        var deleted = new Mock<IHolon>().Object;
        provider.Setup(x => x.DeleteHolonAsync(holonId))
            .ReturnsAsync(new OASISResult<IHolon> { Result = deleted });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IHolon>(new StorageOperationRequest
        {
            Operation = "DeleteHolon",
            HolonId = holonId,
            PreferredProvider = ProviderType.MongoDBOASIS,
            SoftDelete = false
        });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(deleted);
        provider.Verify(x => x.DeleteHolonAsync(holonId), Times.Once);
    }

    [Fact]
    public async Task ParentLoadPreservesVisibilityAndTraversalOptionsAtProviderBoundary()
    {
        var parentId = Guid.NewGuid();
        var avatarId = Guid.NewGuid();
        var holons = new[] { new Mock<IHolon>().Object };
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadHolonsForParentAsync(parentId, avatarId, true, HolonType.Quest,
                false, false, 6, 2, false, true, 4))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { Result = holons });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IEnumerable<IHolon>>(new StorageOperationRequest
        {
            Operation = "LoadHolonsForParent",
            HolonId = parentId,
            AvatarId = avatarId,
            IncludePublic = true,
            HolonType = HolonType.Quest,
            PreferredProvider = ProviderType.MongoDBOASIS,
            LoadChildren = false,
            Recursive = false,
            MaxChildDepth = 6,
            CurrentChildDepth = 2,
            ContinueOnError = false,
            ChildrenFromProvider = true,
            Version = 4
        });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holons);
        provider.VerifyAll();
    }

    [Fact]
    public async Task MetadataLoadPreservesVisibilityAndTraversalOptionsAtProviderBoundary()
    {
        var avatarId = Guid.NewGuid();
        var holons = new[] { new Mock<IHolon>().Object };
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadHolonsByMetaDataAsync("kind", "quest", avatarId, true, HolonType.Quest,
                false, false, 4, 1, false, true, 3))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { Result = holons });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IEnumerable<IHolon>>(new StorageOperationRequest
        {
            Operation = "LoadHolonsByMetaData",
            MetaKey = "kind",
            MetaValue = "quest",
            AvatarId = avatarId,
            IncludePublic = true,
            HolonType = HolonType.Quest,
            PreferredProvider = ProviderType.MongoDBOASIS,
            LoadChildren = false,
            Recursive = false,
            MaxChildDepth = 4,
            CurrentChildDepth = 1,
            ContinueOnError = false,
            ChildrenFromProvider = true,
            Version = 3
        });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holons);
        provider.VerifyAll();
    }

    [Fact]
    public async Task MetadataPairLoadPreservesMatchModeAtProviderBoundary()
    {
        var pairs = new Dictionary<string, string> { ["kind"] = "quest", ["state"] = "active" };
        var holons = new[] { new Mock<IHolon>().Object };
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadHolonsByMetaDataAsync(pairs, MetaKeyValuePairMatchMode.All,
                HolonType.Quest, true, true, 2, 0, true, false, 5))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { Result = holons });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IEnumerable<IHolon>>(new StorageOperationRequest
        {
            Operation = "LoadHolonsByMetaDataPairs",
            MetaKeyValuePairs = pairs,
            MetaKeyValuePairMatchMode = MetaKeyValuePairMatchMode.All,
            HolonType = HolonType.Quest,
            PreferredProvider = ProviderType.MongoDBOASIS,
            MaxChildDepth = 2,
            Version = 5
        });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holons);
        provider.VerifyAll();
    }

    [Fact]
    public async Task AvatarDetailOperationsExecuteDirectlyOnPreferredProvider()
    {
        var avatarId = Guid.NewGuid();
        var detail = new Mock<IAvatarDetail>().Object;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadAvatarDetailAsync(avatarId, 8))
            .ReturnsAsync(new OASISResult<IAvatarDetail> { Result = detail });
        provider.Setup(x => x.SaveAvatarDetailAsync(detail))
            .ReturnsAsync(new OASISResult<IAvatarDetail> { Result = detail });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);
        var hyperDrive = new OASISHyperDrive(manager);

        var loaded = await hyperDrive.LoadBalanceRequestAsync<IAvatarDetail>(new StorageOperationRequest
        { Operation = "LoadAvatarDetail", AvatarId = avatarId, Version = 8, PreferredProvider = ProviderType.MongoDBOASIS });
        var saved = await hyperDrive.LoadBalanceRequestAsync<IAvatarDetail>(new StorageOperationRequest
        { Operation = "SaveAvatarDetail", Payload = detail, PreferredProvider = ProviderType.MongoDBOASIS });

        loaded.IsError.Should().BeFalse(loaded.Message);
        saved.IsError.Should().BeFalse(saved.Message);
        loaded.Result.Should().BeSameAs(detail);
        saved.Result.Should().BeSameAs(detail);
        provider.VerifyAll();
    }

    [Fact]
    public async Task AvatarUsernameLookupPreservesVersionAtProviderBoundary()
    {
        var avatar = new Mock<IAvatar>().Object;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadAvatarByUsernameAsync("edge-user", 6))
            .ReturnsAsync(new OASISResult<IAvatar> { Result = avatar });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IAvatar>(new StorageOperationRequest
        { Operation = "LoadAvatarByUsername", Username = "edge-user", Version = 6, PreferredProvider = ProviderType.MongoDBOASIS });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(avatar);
        provider.VerifyAll();
    }

    [Fact]
    public async Task AvatarDeletePreservesSoftDeleteAtProviderBoundary()
    {
        var avatarId = Guid.NewGuid();
        var provider = CreateActiveProvider();
        provider.Setup(x => x.DeleteAvatarAsync(avatarId, true))
            .ReturnsAsync(new OASISResult<bool> { Result = true });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<bool>(new StorageOperationRequest
        { Operation = "DeleteAvatar", AvatarId = avatarId, SoftDelete = true, PreferredProvider = ProviderType.MongoDBOASIS });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeTrue();
        provider.VerifyAll();
    }

    [Fact]
    public async Task LoadAllAvatarsPreservesVersionAtProviderBoundary()
    {
        var avatars = new[] { new Mock<IAvatar>().Object };
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadAllAvatarsAsync(12))
            .ReturnsAsync(new OASISResult<IEnumerable<IAvatar>> { Result = avatars });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IEnumerable<IAvatar>>(new StorageOperationRequest
        { Operation = "LoadAllAvatars", Version = 12, PreferredProvider = ProviderType.MongoDBOASIS });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(avatars);
        provider.VerifyAll();
    }

    [Fact]
    public async Task LoadAllHolonsPreservesVisibilityAndTraversalOptionsAtProviderBoundary()
    {
        var avatarId = Guid.NewGuid();
        var holons = new[] { new Mock<IHolon>().Object };
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadAllHolonsAsync(avatarId, true, HolonType.Quest, false, false,
                7, 3, false, true, 11))
            .ReturnsAsync(new OASISResult<IEnumerable<IHolon>> { Result = holons });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<IEnumerable<IHolon>>(
            new StorageOperationRequest
            {
                Operation = "LoadAllHolons", AvatarId = avatarId, IncludePublic = true,
                HolonType = HolonType.Quest, LoadChildren = false, Recursive = false,
                MaxChildDepth = 7, CurrentChildDepth = 3, ContinueOnError = false,
                ChildrenFromProvider = true, Version = 11,
                PreferredProvider = ProviderType.MongoDBOASIS
            });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(holons);
        provider.VerifyAll();
    }

    [Fact]
    public async Task SearchPreservesQueryAndTraversalOptionsAtProviderBoundary()
    {
        ISearchParams search = new SearchParams { AvatarId = Guid.NewGuid(), SearchOnlyForCurrentAvatar = true };
        ISearchResults searchResults = new SearchResults();
        var provider = CreateActiveProvider();
        provider.Setup(x => x.SearchAsync(search, false, false, 4, false, 9))
            .ReturnsAsync(new OASISResult<ISearchResults> { Result = searchResults });
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).LoadBalanceRequestAsync<ISearchResults>(
            new StorageOperationRequest
            {
                Operation = "Search", SearchParams = search, LoadChildren = false, Recursive = false,
                MaxChildDepth = 4, ContinueOnError = false, Version = 9,
                PreferredProvider = ProviderType.MongoDBOASIS
            });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(searchResults);
        provider.VerifyAll();
    }

    [Fact]
    public async Task ExplicitProviderRouteDoesNotFailOverOrMutateGlobalProvider()
    {
        ISearchParams search = new SearchParams();
        ISearchResults searchResults = new SearchResults();
        var selected = CreateActiveProvider();
        selected.Setup(x => x.SearchAsync(search, true, true, 0, true, 0))
            .ReturnsAsync(new OASISResult<ISearchResults> { Result = searchResults });
        var other = new Mock<IOASISStorageProvider>();
        other.SetupAllProperties();
        other.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.Neo4jOASIS);
        other.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        other.Object.ProviderName = "other";
        other.Object.IsProviderActivated = true;
        var manager = new ProviderManager(null);
        manager.RegisterProvider(selected.Object);
        manager.RegisterProvider(other.Object);
        var original = manager.CurrentStorageProviderType.Value;

        var result = await new OASISHyperDrive(manager).RouteRequestToProviderAsync<ISearchResults>(
            new StorageOperationRequest { Operation = "Search", SearchParams = search,
                PreferredProvider = ProviderType.MongoDBOASIS }, ProviderType.MongoDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(searchResults);
        manager.CurrentStorageProviderType.Value.Should().Be(original);
        other.Verify(x => x.SearchAsync(It.IsAny<ISearchParams>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task IndexedAvatarLookupsExecuteDirectlyAndPreserveVersion()
    {
        var avatar = new Mock<IAvatar>().Object;
        var provider = CreateActiveProvider();
        provider.Setup(x => x.LoadAvatarByProviderKeyAsync("provider-key", 6)).ReturnsAsync(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.LoadAvatarByVerificationTokenAsync("verify", 6)).ReturnsAsync(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.LoadAvatarByResetTokenAsync("reset", 6)).ReturnsAsync(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.LoadAvatarByRefreshTokenAsync("refresh", 6)).ReturnsAsync(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.LoadAvatarByPublicKeyAsync("public", 6)).ReturnsAsync(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.LoadAvatarByPrivateKeyAsync("private", 6)).ReturnsAsync(new OASISResult<IAvatar>(avatar));
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);
        var hyperDrive = new OASISHyperDrive(manager);

        var requests = new[]
        {
            new StorageOperationRequest { Operation = "LoadAvatarByProviderKey", ProviderKey = "provider-key" },
            new StorageOperationRequest { Operation = "LoadAvatarByVerificationToken", VerificationToken = "verify" },
            new StorageOperationRequest { Operation = "LoadAvatarByResetToken", ResetToken = "reset" },
            new StorageOperationRequest { Operation = "LoadAvatarByRefreshToken", RefreshToken = "refresh" },
            new StorageOperationRequest { Operation = "LoadAvatarByPublicKey", PublicKey = "public" },
            new StorageOperationRequest { Operation = "LoadAvatarByPrivateKey", PrivateKey = "private" }
        };
        foreach (var request in requests)
        {
            request.Version = 6;
            request.PreferredProvider = ProviderType.MongoDBOASIS;
            var result = await hyperDrive.RouteRequestToProviderAsync<IAvatar>(request, ProviderType.MongoDBOASIS);
            result.IsError.Should().BeFalse(result.Message);
            result.Result.Should().BeSameAs(avatar);
        }
        provider.VerifyAll();
    }

    [Fact]
    public async Task IndexedAvatarLookupRejectsEmptySecretBeforeProviderCall()
    {
        var provider = CreateActiveProvider();
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).RouteRequestToProviderAsync<IAvatar>(
            new StorageOperationRequest { Operation = "LoadAvatarByRefreshToken", RefreshToken = " " },
            ProviderType.MongoDBOASIS);

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HYPERDRIVE_REFRESH_TOKEN_REQUIRED");
        provider.Verify(x => x.LoadAvatarByRefreshTokenAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task RemainingStorageContractOperationsRouteDirectlyInAsyncMode()
    {
        var avatarId = Guid.NewGuid();
        var detail = new Mock<IAvatarDetail>().Object;
        var holons = new[] { new Mock<IHolon>().Object };
        var karma = new KarmaAkashicRecord();
        var provider = CreateActiveProvider();
        provider.Setup(x => x.DeleteAvatarAsync("avatar-provider-key", false)).ReturnsAsync(new OASISResult<bool>(true));
        provider.Setup(x => x.AddKarmaToAvatarAsync(detail, default, default, "source", "description", "https://source"))
            .ReturnsAsync(new OASISResult<KarmaAkashicRecord>(karma));
        provider.Setup(x => x.RemoveKarmaFromAvatarAsync(detail, default, default, "source", "description", "https://source"))
            .ReturnsAsync(new OASISResult<KarmaAkashicRecord>(karma));
        provider.Setup(x => x.ImportAsync(holons)).ReturnsAsync(new OASISResult<bool>(true));
        provider.Setup(x => x.ExportAllDataForAvatarByIdAsync(avatarId, 7)).ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(holons));
        provider.Setup(x => x.ExportAllDataForAvatarByUsernameAsync("edge", 7)).ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(holons));
        provider.Setup(x => x.ExportAllDataForAvatarByEmailAsync("edge@example.com", 7)).ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(holons));
        provider.Setup(x => x.ExportAllAsync(7)).ReturnsAsync(new OASISResult<IEnumerable<IHolon>>(holons));
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);
        var hyperDrive = new OASISHyperDrive(manager);
        var common = new StorageOperationRequest
        {
            Payload = detail, KarmaSourceTitle = "source", KarmaSourceDescription = "description",
            KarmaSourceWebLink = "https://source", PreferredProvider = ProviderType.MongoDBOASIS
        };

        (await hyperDrive.RouteRequestToProviderAsync<bool>(new StorageOperationRequest
            { Operation = "DeleteAvatarByProviderKey", ProviderKey = "avatar-provider-key", SoftDelete = false }, ProviderType.MongoDBOASIS)).Result.Should().BeTrue();
        common.Operation = "AddKarmaToAvatar";
        (await hyperDrive.RouteRequestToProviderAsync<KarmaAkashicRecord>(common, ProviderType.MongoDBOASIS)).Result.Should().BeSameAs(karma);
        common.Operation = "RemoveKarmaFromAvatar";
        (await hyperDrive.RouteRequestToProviderAsync<KarmaAkashicRecord>(common, ProviderType.MongoDBOASIS)).Result.Should().BeSameAs(karma);
        (await hyperDrive.RouteRequestToProviderAsync<bool>(new StorageOperationRequest
            { Operation = "Import", Payload = holons }, ProviderType.MongoDBOASIS)).Result.Should().BeTrue();
        foreach (var request in new[]
        {
            new StorageOperationRequest { Operation = "ExportAllDataForAvatarById", AvatarId = avatarId, Version = 7 },
            new StorageOperationRequest { Operation = "ExportAllDataForAvatarByUsername", Username = "edge", Version = 7 },
            new StorageOperationRequest { Operation = "ExportAllDataForAvatarByEmail", Email = "edge@example.com", Version = 7 },
            new StorageOperationRequest { Operation = "ExportAll", Version = 7 }
        })
            (await hyperDrive.RouteRequestToProviderAsync<IEnumerable<IHolon>>(request, ProviderType.MongoDBOASIS)).Result.Should().BeSameAs(holons);
        provider.VerifyAll();
    }

    [Fact]
    public void RemainingStorageContractOperationsRouteDirectlyInSynchronousMode()
    {
        var avatarId = Guid.NewGuid();
        var detail = new Mock<IAvatarDetail>().Object;
        var holons = new[] { new Mock<IHolon>().Object };
        var karma = new KarmaAkashicRecord();
        var provider = CreateActiveProvider();
        provider.Setup(x => x.DeleteAvatar("avatar-provider-key", false)).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.AddKarmaToAvatar(detail, default, default, "source", "description", null))
            .Returns(new OASISResult<KarmaAkashicRecord>(karma));
        provider.Setup(x => x.Import(holons)).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.ExportAllDataForAvatarById(avatarId, 3)).Returns(new OASISResult<IEnumerable<IHolon>>(holons));
        provider.Setup(x => x.ExportAll(3)).Returns(new OASISResult<IEnumerable<IHolon>>(holons));
        var manager = new ProviderManager(null);
        manager.RegisterProvider(provider.Object);
        var hyperDrive = new OASISHyperDrive(manager);

        hyperDrive.RouteRequestToProvider<bool>(new StorageOperationRequest
            { Operation = "DeleteAvatarByProviderKey", ProviderKey = "avatar-provider-key", SoftDelete = false }, ProviderType.MongoDBOASIS).Result.Should().BeTrue();
        hyperDrive.RouteRequestToProvider<KarmaAkashicRecord>(new StorageOperationRequest
            { Operation = "AddKarmaToAvatar", Payload = detail, KarmaSourceTitle = "source", KarmaSourceDescription = "description" }, ProviderType.MongoDBOASIS).Result.Should().BeSameAs(karma);
        hyperDrive.RouteRequestToProvider<bool>(new StorageOperationRequest
            { Operation = "Import", Payload = holons }, ProviderType.MongoDBOASIS).Result.Should().BeTrue();
        hyperDrive.RouteRequestToProvider<IEnumerable<IHolon>>(new StorageOperationRequest
            { Operation = "ExportAllDataForAvatarById", AvatarId = avatarId, Version = 3 }, ProviderType.MongoDBOASIS).Result.Should().BeSameAs(holons);
        hyperDrive.RouteRequestToProvider<IEnumerable<IHolon>>(new StorageOperationRequest
            { Operation = "ExportAll", Version = 3 }, ProviderType.MongoDBOASIS).Result.Should().BeSameAs(holons);
        provider.VerifyAll();
    }

    [Fact]
    public async Task ProviderRecommendationsAreDeterministicForIdenticalInputs()
    {
        var engine = new AIOptimizationEngine();
        var providers = new List<ProviderType>
        {
            ProviderType.Neo4jOASIS,
            ProviderType.MongoDBOASIS,
            ProviderType.SQLLiteDBOASIS
        };
        var request = new StorageOperationRequest { Operation = "SaveHolon" };

        var first = await engine.GetProviderRecommendationsAsync(request, providers);
        var second = await engine.GetProviderRecommendationsAsync(request, providers);

        second.Select(x => x.ProviderType).Should().Equal(first.Select(x => x.ProviderType));
        second.Select(x => x.Score).Should().Equal(first.Select(x => x.Score));
    }

    [Fact]
    public void WalletUsernameSaveCannotEscapeItsInjectedRuntime()
    {
        var callingThread = Environment.CurrentManagedThreadId;
        var providerThread = 0;
        var username = $"wallet-{Guid.NewGuid():N}";
        var avatarId = Guid.NewGuid();
        var wallets = new Dictionary<ProviderType, List<IProviderWallet>>();
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        var localWallets = provider.As<IOASISLocalStorageProvider>();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.StorageLocal);
        provider.Object.ProviderName = "isolated-wallet-sqlite";
        provider.Object.IsProviderActivated = true;
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarByUsername(username, 0))
            .Returns(new OASISResult<IAvatar>(new Avatar { Id = avatarId, Username = username }));
        localWallets.Setup(x => x.SaveProviderWalletsForAvatarById(avatarId, wallets))
            .Callback(() => providerThread = Environment.CurrentManagedThreadId)
            .Returns(new OASISResult<bool>(true));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var manager = new WalletManager(null, dna, providerManager);

        var result = manager.SaveProviderWalletsForAvatarByUsername(
            username, wallets, ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeTrue();
        providerThread.Should().Be(callingThread);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
        provider.Verify(x => x.LoadAvatarByUsername(username, 0), Times.Once);
        localWallets.Verify(x => x.SaveProviderWalletsForAvatarById(avatarId, wallets), Times.Once);
    }

    [Fact]
    public void LegacySynchronousAvatarKeyLookupUsesOnlyTheSynchronousProviderBoundary()
    {
        var publicKey = $"public-{Guid.NewGuid():N}";
        var avatar = new Avatar { Id = Guid.NewGuid(), Username = "legacy-key-avatar" };
        var provider = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "legacy-key-sqlite");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarByPublicKey(publicKey, 0))
            .Returns(new OASISResult<IAvatar>(avatar));
        var dna = CreateDna(HyperDriveModes.Legacy);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        var manager = new AvatarManager(provider.Object, dna, providerManager);

        var result = manager.LoadAvatarByPublicKeyForProvider(publicKey, ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(avatar);
        provider.Verify(x => x.LoadAvatarByPublicKey(publicKey, 0), Times.Once);
        provider.Verify(x => x.LoadAvatarByPublicKeyAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void LegacySynchronousAvatarAndDetailLoadsUseOnlySynchronousProviderContracts()
    {
        var avatarId = Guid.NewGuid();
        const string username = "legacy-sync-player";
        const string email = "legacy-sync-player@example.test";
        var avatar = new Avatar { Id = avatarId, Username = username, Email = email };
        var detail = new AvatarDetail
        {
            Id = avatarId,
            Username = username,
            Email = email,
            Inventory = new List<IInventoryItem>()
        };
        var provider = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "legacy-sync-avatar-sqlite");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatar(avatarId, 3)).Returns(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.LoadAvatarByUsername(username, 3)).Returns(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.LoadAvatarByEmail(email, 3)).Returns(new OASISResult<IAvatar>(avatar));
        provider.Setup(x => x.LoadAvatarDetail(avatarId, 3)).Returns(new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.LoadAvatarDetailByUsername(username, 3)).Returns(new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.LoadAvatarDetailByEmail(email, 3)).Returns(new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.LoadAllAvatars(3)).Returns(new OASISResult<IEnumerable<IAvatar>>(new[] { avatar }));
        provider.Setup(x => x.LoadAllAvatarDetails(3)).Returns(new OASISResult<IEnumerable<IAvatarDetail>>(new[] { detail }));
        var dna = CreateDna(HyperDriveModes.Legacy);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        var manager = new AvatarManager(provider.Object, dna, providerManager);

        manager.LoadAvatar(avatarId, providerType: ProviderType.SQLLiteDBOASIS, version: 3).Result.Id.Should().Be(avatarId);
        manager.LoadAvatar(username, providerType: ProviderType.SQLLiteDBOASIS, version: 3).Result.Id.Should().Be(avatarId);
        manager.LoadAvatarByEmail(email, providerType: ProviderType.SQLLiteDBOASIS, version: 3).Result.Id.Should().Be(avatarId);
        manager.LoadAvatarDetail(avatarId, ProviderType.SQLLiteDBOASIS, 3).Result.Id.Should().Be(avatarId);
        manager.LoadAvatarDetailByUsername(username, ProviderType.SQLLiteDBOASIS, 3).Result.Id.Should().Be(avatarId);
        manager.LoadAvatarDetailByEmail(email, ProviderType.SQLLiteDBOASIS, 3).Result.Id.Should().Be(avatarId);
        manager.LoadAllAvatars(providerType: ProviderType.SQLLiteDBOASIS, version: 3).Result.Should().ContainSingle();
        manager.LoadAllAvatarDetails(ProviderType.SQLLiteDBOASIS, 3).Result.Should().ContainSingle();

        provider.Verify(x => x.LoadAvatar(avatarId, 3), Times.Once);
        provider.Verify(x => x.LoadAvatarByUsername(username, 3), Times.Once);
        provider.Verify(x => x.LoadAvatarByEmail(email, 3), Times.Once);
        provider.Verify(x => x.LoadAvatarDetail(avatarId, 3), Times.Once);
        provider.Verify(x => x.LoadAvatarDetailByUsername(username, 3), Times.Once);
        provider.Verify(x => x.LoadAvatarDetailByEmail(email, 3), Times.Once);
        provider.Verify(x => x.LoadAllAvatars(3), Times.Once);
        provider.Verify(x => x.LoadAllAvatarDetails(3), Times.Once);
        provider.Verify(x => x.LoadAvatarAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        provider.Verify(x => x.LoadAvatarByUsernameAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        provider.Verify(x => x.LoadAvatarByEmailAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        provider.Verify(x => x.LoadAvatarDetailAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        provider.Verify(x => x.LoadAvatarDetailByUsernameAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        provider.Verify(x => x.LoadAvatarDetailByEmailAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        provider.Verify(x => x.LoadAllAvatarsAsync(It.IsAny<int>()), Times.Never);
        provider.Verify(x => x.LoadAllAvatarDetailsAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void SynchronousAvatarReplicationHelpersUseOnlySynchronousProviderContracts()
    {
        var callingThread = Environment.CurrentManagedThreadId;
        var saveThread = 0;
        var deleteThread = 0;
        var detail = new AvatarDetail { Id = Guid.NewGuid(), Username = "replicated-avatar" };
        var provider = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "replication-sqlite");
        provider.Setup(x => x.SaveAvatarDetail(detail))
            .Callback(() => saveThread = Environment.CurrentManagedThreadId)
            .Returns(new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.DeleteAvatarByUsername(detail.Username, true))
            .Callback(() => deleteThread = Environment.CurrentManagedThreadId)
            .Returns(new OASISResult<bool>(true));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        var manager = new AvatarManager(provider.Object, dna, providerManager);

        var saveResult = manager.SaveAvatarDetailForProvider(detail, new OASISResult<IAvatarDetail>(),
            SaveMode.AutoReplication, ProviderType.SQLLiteDBOASIS);
        var deleteResult = manager.DeleteAvatarByUsernameForProvider(detail.Username, new OASISResult<bool>(),
            SaveMode.AutoReplication, true, ProviderType.SQLLiteDBOASIS);

        saveResult.IsError.Should().BeFalse(saveResult.Message);
        saveResult.IsSaved.Should().BeTrue();
        saveResult.Result.Should().BeSameAs(detail);
        deleteResult.IsError.Should().BeFalse(deleteResult.Message);
        deleteResult.IsSaved.Should().BeTrue();
        deleteResult.Result.Should().BeTrue();
        saveThread.Should().Be(callingThread);
        deleteThread.Should().Be(callingThread);
        provider.Verify(x => x.SaveAvatarDetailAsync(It.IsAny<IAvatarDetail>()), Times.Never);
        provider.Verify(x => x.DeleteAvatarByUsernameAsync(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void SynchronousInventoryReadsUseOnlyTheSynchronousAvatarDetailBoundary()
    {
        var avatarId = Guid.NewGuid();
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            Name = "Obsidian Pod",
            Description = "A durable quest item"
        };
        var detail = new AvatarDetail
        {
            Id = avatarId,
            Inventory = new List<IInventoryItem> { item }
        };
        var provider = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "inventory-read-sqlite");
        provider.Setup(x => x.LoadAvatarDetail(avatarId, 0))
            .Returns(new OASISResult<IAvatarDetail>(detail));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        var manager = new AvatarManager(provider.Object, dna, providerManager);

        manager.GetAvatarInventory(avatarId, ProviderType.SQLLiteDBOASIS).Result.Should().ContainSingle();
        manager.AvatarHasItem(avatarId, item.Id, ProviderType.SQLLiteDBOASIS).Result.Should().BeTrue();
        manager.AvatarHasItemByName(avatarId, "Obsidian Pod", ProviderType.SQLLiteDBOASIS).Result.Should().BeTrue();
        manager.SearchAvatarInventory(avatarId, "quest", ProviderType.SQLLiteDBOASIS).Result.Should().ContainSingle();
        manager.GetAvatarInventoryItem(avatarId, item.Id, ProviderType.SQLLiteDBOASIS).Result.Should().BeSameAs(item);

        provider.Verify(x => x.LoadAvatarDetail(avatarId, 0), Times.Exactly(5));
        provider.Verify(x => x.LoadAvatarDetailAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void SynchronousInventoryRemovalUsesOnlySynchronousAvatarDetailContracts()
    {
        var avatarId = Guid.NewGuid();
        var item = new InventoryItem { Id = Guid.NewGuid(), Name = "Crystal", Quantity = 3 };
        var detail = new AvatarDetail
        {
            Id = avatarId,
            Inventory = new List<IInventoryItem> { item }
        };
        var provider = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "inventory-remove-sqlite");
        provider.Setup(x => x.LoadAvatarDetail(avatarId, 0))
            .Returns(new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.SaveAvatarDetail(detail))
            .Returns(new OASISResult<IAvatarDetail>(detail));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        var manager = new AvatarManager(provider.Object, dna, providerManager);

        var result = manager.RemoveItemFromAvatarInventory(
            avatarId, item.Id, 2, ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeTrue();
        item.Quantity.Should().Be(1);
        provider.Verify(x => x.LoadAvatarDetail(avatarId, 0), Times.Once);
        provider.Verify(x => x.SaveAvatarDetail(detail), Times.Once);
        provider.Verify(x => x.LoadAvatarDetailAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        provider.Verify(x => x.SaveAvatarDetailAsync(It.IsAny<IAvatarDetail>()), Times.Never);
    }

    [Fact]
    public void SynchronousInventoryAdditionUsesOnlySynchronousContractsAndSharesTheDurableOperationLedger()
    {
        var avatarId = Guid.NewGuid();
        var operationId = Guid.NewGuid();
        var detail = new AvatarDetail
        {
            Id = avatarId,
            Inventory = new List<IInventoryItem>(),
            MetaData = new Dictionary<string, object>()
        };
        var item = new InventoryItem
        {
            Name = "Obsidian Pod",
            GameSource = "Our World",
            ItemType = InventoryItemType.QuestItem,
            Quantity = 2,
            Stack = true
        };
        var provider = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "inventory-add-sqlite");
        provider.Setup(x => x.LoadAvatarDetail(avatarId, 0))
            .Returns(new OASISResult<IAvatarDetail>(detail));
        provider.Setup(x => x.SaveAvatarDetail(detail))
            .Returns(new OASISResult<IAvatarDetail>(detail));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(provider.Object).Should().BeTrue();
        var manager = new AvatarManager(provider.Object, dna, providerManager);

        var first = manager.AddItemToAvatarInventory(
            avatarId, item, ProviderType.SQLLiteDBOASIS, operationId);
        var replay = manager.AddItemToAvatarInventory(
            avatarId,
            new InventoryItem
            {
                Name = item.Name,
                GameSource = item.GameSource,
                ItemType = item.ItemType,
                Quantity = item.Quantity,
                Stack = true
            },
            ProviderType.SQLLiteDBOASIS,
            operationId);

        first.IsError.Should().BeFalse(first.Message);
        first.Result.Should().BeSameAs(item);
        replay.IsError.Should().BeFalse(replay.Message);
        replay.Result.Should().BeSameAs(item);
        replay.Message.Should().Be("Inventory operation was already applied.");
        detail.Inventory.Should().ContainSingle().Which.Should().BeSameAs(item);
        provider.Verify(x => x.LoadAvatarDetail(avatarId, 0), Times.Exactly(2));
        provider.Verify(x => x.SaveAvatarDetail(detail), Times.Once);
        provider.Verify(x => x.LoadAvatarDetailAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
        provider.Verify(x => x.SaveAvatarDetailAsync(It.IsAny<IAvatarDetail>()), Times.Never);
    }

    [Fact]
    public void WalletIdentityChildPreservesTheExplicitProviderInAMultiProviderV2Runtime()
    {
        var username = $"wallet-explicit-{Guid.NewGuid():N}";
        var avatarId = Guid.NewGuid();
        var wallets = new Dictionary<ProviderType, List<IProviderWallet>>();
        var explicitProvider = new Mock<IOASISStorageProvider>();
        explicitProvider.SetupAllProperties();
        var localWallets = explicitProvider.As<IOASISLocalStorageProvider>();
        explicitProvider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        explicitProvider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.StorageLocal);
        explicitProvider.Object.ProviderName = "wallet-explicit-sqlite";
        explicitProvider.Object.IsProviderActivated = true;
        explicitProvider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        explicitProvider.Setup(x => x.LoadAvatarByUsername(username, 0))
            .Returns(new OASISResult<IAvatar>(new Avatar { Id = avatarId, Username = username }));
        localWallets.Setup(x => x.SaveProviderWalletsForAvatarById(avatarId, wallets))
            .Returns(new OASISResult<bool>(true));
        var runtimeDefault = CreateActiveProvider(ProviderType.MongoDBOASIS, "wallet-runtime-default");
        runtimeDefault.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(runtimeDefault.Object).Should().BeTrue();
        providerManager.RegisterProvider(explicitProvider.Object).Should().BeTrue();
        providerManager.SetAndActivateCurrentStorageProvider(runtimeDefault.Object).IsError.Should().BeFalse();

        var result = new WalletManager(explicitProvider.Object, dna, providerManager)
            .SaveProviderWalletsForAvatarByUsername(username, wallets, ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeTrue();
        explicitProvider.Verify(x => x.LoadAvatarByUsername(username, 0), Times.Once);
        runtimeDefault.Verify(x => x.LoadAvatarByUsername(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        localWallets.Verify(x => x.SaveProviderWalletsForAvatarById(avatarId, wallets), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task V2WalletIdentityLoadsRemainAsynchronousThroughTheLocalProvider(bool useEmail)
    {
        var identity = useEmail ? $"wallet-{Guid.NewGuid():N}@example.com" : $"wallet-{Guid.NewGuid():N}";
        var avatarId = Guid.NewGuid();
        var wallets = new Dictionary<ProviderType, List<IProviderWallet>>();
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        var localWallets = provider.As<IOASISLocalStorageProvider>();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.StorageLocal);
        provider.Object.ProviderName = "async-wallet-sqlite";
        provider.Object.IsProviderActivated = true;
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        if (useEmail)
            provider.Setup(x => x.LoadAvatarByEmailAsync(identity, 0))
                .ReturnsAsync(new OASISResult<IAvatar>(new Avatar { Id = avatarId, Email = identity }));
        else
            provider.Setup(x => x.LoadAvatarByUsernameAsync(identity, 0))
                .ReturnsAsync(new OASISResult<IAvatar>(new Avatar { Id = avatarId, Username = identity }));
        localWallets.Setup(x => x.LoadProviderWalletsForAvatarByIdAsync(avatarId))
            .ReturnsAsync(new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>(wallets));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new WalletManager(null, dna, providerManager);

        var result = useEmail
            ? await manager.LoadProviderWalletsForAvatarByEmailUsingHyperDriveAsync(identity)
            : await manager.LoadProviderWalletsForAvatarByUsernameUsingHyperDriveAsync(identity);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeSameAs(wallets);
        localWallets.Verify(x => x.LoadProviderWalletsForAvatarByIdAsync(avatarId), Times.Once);
        localWallets.Verify(x => x.LoadProviderWalletsForAvatarById(It.IsAny<Guid>()), Times.Never);
    }

    [Theory]
    [InlineData(ProviderType.SQLLiteDBOASIS)]
    [InlineData(ProviderType.LocalFileOASIS)]
    public async Task V2FreePlanPermitsZeroCostLocalEdgeProviders(ProviderType providerType)
    {
        var avatarId = Guid.NewGuid();
        var provider = CreateActiveProvider(providerType, $"free-local-{providerType}");
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        provider.Setup(x => x.LoadAvatarAsync(avatarId, 0))
            .ReturnsAsync(new OASISResult<IAvatar>(new Avatar { Id = avatarId }));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();

        var result = await new OASISHyperDrive(providerManager).RouteRequestAsync<IAvatar>(
            new StorageOperationRequest { Operation = "LoadAvatar", AvatarId = avatarId });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Id.Should().Be(avatarId);
        provider.Verify(x => x.LoadAvatarAsync(avatarId, 0), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task V2WalletIdentitySavesRemainAsynchronousThroughTheLocalProvider(bool useEmail)
    {
        var identity = useEmail ? $"wallet-{Guid.NewGuid():N}@example.com" : $"wallet-{Guid.NewGuid():N}";
        var avatarId = Guid.NewGuid();
        var wallets = new Dictionary<ProviderType, List<IProviderWallet>>();
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        var localWallets = provider.As<IOASISLocalStorageProvider>();
        provider.Object.ProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.StorageLocal);
        provider.Object.ProviderName = "async-wallet-sqlite";
        provider.Object.IsProviderActivated = true;
        provider.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        if (useEmail)
            provider.Setup(x => x.LoadAvatarByEmailAsync(identity, 0))
                .ReturnsAsync(new OASISResult<IAvatar>(new Avatar { Id = avatarId, Email = identity }));
        else
            provider.Setup(x => x.LoadAvatarByUsernameAsync(identity, 0))
                .ReturnsAsync(new OASISResult<IAvatar>(new Avatar { Id = avatarId, Username = identity }));
        localWallets.Setup(x => x.SaveProviderWalletsForAvatarByIdAsync(avatarId, wallets))
            .ReturnsAsync(new OASISResult<bool>(true));
        var dna = CreateDna(HyperDriveModes.V2);
        var providerManager = new ProviderManager(null, dna);
        providerManager.RegisterProvider(provider.Object);
        providerManager.SetAndActivateCurrentStorageProvider(provider.Object).IsError.Should().BeFalse();
        var manager = new WalletManager(null, dna, providerManager);

        var result = useEmail
            ? await manager.SaveProviderWalletsForAvatarByEmailAsync(identity, wallets, ProviderType.SQLLiteDBOASIS)
            : await manager.SaveProviderWalletsForAvatarByUsernameAsync(identity, wallets, ProviderType.SQLLiteDBOASIS);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeTrue();
        localWallets.Verify(x => x.SaveProviderWalletsForAvatarByIdAsync(avatarId, wallets), Times.Once);
        localWallets.Verify(x => x.SaveProviderWalletsForAvatarById(It.IsAny<Guid>(),
            It.IsAny<Dictionary<ProviderType, List<IProviderWallet>>>()), Times.Never);
    }

    [Fact]
    public async Task V2DoesNotFailOverWhenAutoFailoverIsDisabled()
    {
        var manager = new ProviderManager(null, CreateDna(HyperDriveModes.V2))
            { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "primary");
        var secondary = CreateActiveProvider(ProviderType.IPFSOASIS, "secondary");
        primary.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var avatarId = Guid.NewGuid();
        primary.Setup(x => x.LoadAvatarAsync(avatarId, 0)).ReturnsAsync(new OASISResult<IAvatar>
            { IsError = true, ErrorCount = 1, Message = "primary failed" });
        manager.RegisterProvider(primary.Object);
        manager.RegisterProvider(secondary.Object);
        manager.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoFailOverListForProviders(new[]
            { new EnumValue<ProviderType>(ProviderType.IPFSOASIS) });

        var result = await new OASISHyperDrive(manager).RouteRequestAsync<IAvatar>(
            new StorageOperationRequest { Operation = "LoadAvatar", AvatarId = avatarId });

        result.IsError.Should().BeTrue();
        secondary.Verify(x => x.LoadAvatarAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task V2TreatsANonErrorNullPayloadAsAnAuthoritativeNotFoundResult()
    {
        var manager = new ProviderManager(null, CreateDna(HyperDriveModes.V2))
            { IsAutoFailOverEnabled = true, IsAutoLoadBalanceEnabled = false };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "primary");
        var secondary = CreateActiveProvider(ProviderType.IPFSOASIS, "secondary");
        primary.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var holonId = Guid.NewGuid();
        primary.Setup(x => x.LoadHolonAsync(holonId, true, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon> { Result = null! });
        manager.RegisterProvider(primary.Object);
        manager.RegisterProvider(secondary.Object);
        manager.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoFailOverListForProviders(new[]
            { new EnumValue<ProviderType>(ProviderType.IPFSOASIS) });

        var result = await new OASISHyperDrive(manager).RouteRequestAsync<IHolon>(
            new StorageOperationRequest { Operation = "LoadHolon", HolonId = holonId });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().BeNull();
        secondary.Verify(x => x.LoadHolonAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task V2FailoverPublishesOrderedStructuredDiagnosticOnRecovery()
    {
        var manager = new ProviderManager(null, CreateDna(HyperDriveModes.V2))
            { IsAutoFailOverEnabled = true, IsAutoLoadBalanceEnabled = false };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "diagnostic-primary");
        var secondary = CreateActiveProvider(ProviderType.IPFSOASIS, "diagnostic-secondary");
        primary.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var avatarId = Guid.NewGuid();
        primary.Setup(x => x.LoadAvatarAsync(avatarId, 0)).ReturnsAsync(new OASISResult<IAvatar>
        {
            IsError = true,
            ErrorCount = 1,
            ErrorCode = "PRIMARY_UNAVAILABLE",
            Message = "primary unavailable"
        });
        secondary.Setup(x => x.LoadAvatarAsync(avatarId, 0))
            .ReturnsAsync(new OASISResult<IAvatar>(new Avatar { Id = avatarId }));
        manager.RegisterProvider(primary.Object);
        manager.RegisterProvider(secondary.Object);
        manager.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoFailOverListForProviders(new[]
        {
            new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),
            new EnumValue<ProviderType>(ProviderType.IPFSOASIS)
        }).IsError.Should().BeFalse();
        var hyperDrive = new OASISHyperDrive(manager);

        var result = await hyperDrive.RouteRequestAsync<IAvatar>(new StorageOperationRequest
            { Operation = "LoadAvatar", AvatarId = avatarId });

        result.IsError.Should().BeFalse(result.Message);
        hyperDrive.LastFailoverDiagnostic.Succeeded.Should().BeTrue();
        hyperDrive.LastFailoverDiagnostic.Exhausted.Should().BeFalse();
        hyperDrive.LastFailoverDiagnostic.OriginalProvider.Should().Be(ProviderType.MongoDBOASIS);
        hyperDrive.LastFailoverDiagnostic.SelectedProvider.Should().Be(ProviderType.IPFSOASIS);
        manager.LastFailoverDiagnostic.Should().BeSameAs(hyperDrive.LastFailoverDiagnostic);
        hyperDrive.LastFailoverDiagnostic.Attempts.Select(x => x.Provider).Should().Equal(
            ProviderType.MongoDBOASIS, ProviderType.IPFSOASIS);
        hyperDrive.LastFailoverDiagnostic.Attempts[0].ErrorCode.Should().Be("PRIMARY_UNAVAILABLE");
        result.MetaData.Should().ContainKey("hyperDriveFailoverDiagnostic");
        JObject.Parse(result.MetaData["hyperDriveFailoverDiagnostic"])["SelectedProvider"]
            .Value<int>().Should().Be((int)ProviderType.IPFSOASIS);
    }

    [Fact]
    public async Task V2UsesCurrentProviderWhenAutoLoadBalancingIsDisabled()
    {
        var manager = new ProviderManager(null, CreateDna(HyperDriveModes.V2))
            { IsAutoLoadBalanceEnabled = false };
        var current = CreateActiveProvider(ProviderType.MongoDBOASIS, "current");
        var candidate = CreateActiveProvider(ProviderType.IPFSOASIS, "candidate");
        current.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        var avatarId = Guid.NewGuid();
        current.Setup(x => x.LoadAvatarAsync(avatarId, 0))
            .ReturnsAsync(new OASISResult<IAvatar>(new Avatar { Id = avatarId }));
        manager.RegisterProvider(current.Object);
        manager.RegisterProvider(candidate.Object);
        manager.SetAndActivateCurrentStorageProvider(current.Object).IsError.Should().BeFalse();
        manager.SetAndReplaceAutoLoadBalanceListForProviders(new[]
            { new EnumValue<ProviderType>(ProviderType.IPFSOASIS) });

        var result = await new OASISHyperDrive(manager).RouteRequestAsync<IAvatar>(
            new StorageOperationRequest { Operation = "LoadAvatar", AvatarId = avatarId });

        result.IsError.Should().BeFalse(result.Message);
        current.Verify(x => x.LoadAvatarAsync(avatarId, 0), Times.Once);
        candidate.Verify(x => x.LoadAvatarAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task V2AutomaticallyReplicatesSuccessfulMutationsInConfiguredOrder()
    {
        var manager = new ProviderManager(null, CreateDna(HyperDriveModes.V2))
            { IsAutoReplicationEnabled = true };
        var calls = new List<string>();
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "primary");
        var first = CreateActiveProvider(ProviderType.IPFSOASIS, "first");
        var second = CreateActiveProvider(ProviderType.Neo4jOASIS, "second");
        var holon = new Holon { Id = Guid.NewGuid() };
        primary.Setup(x => x.SaveHolonAsync(holon, true, true, 0, true, false))
            .Callback(() => calls.Add("primary")).ReturnsAsync(new OASISResult<IHolon>(holon));
        first.Setup(x => x.SaveHolonAsync(holon, true, true, 0, true, false))
            .Callback(() => calls.Add("first")).ReturnsAsync(new OASISResult<IHolon>(holon));
        second.Setup(x => x.SaveHolonAsync(holon, true, true, 0, true, false))
            .Callback(() => calls.Add("second")).ReturnsAsync(new OASISResult<IHolon>(holon));
        manager.RegisterProvider(primary.Object);
        manager.RegisterProvider(first.Object);
        manager.RegisterProvider(second.Object);
        manager.SetAndReplaceAutoReplicationListForProviders(new[]
        {
            new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),
            new EnumValue<ProviderType>(ProviderType.IPFSOASIS),
            new EnumValue<ProviderType>(ProviderType.Neo4jOASIS)
        });

        var hyperDrive = new OASISHyperDrive(manager);
        var result = await hyperDrive.RouteRequestAsync<IHolon>(new StorageOperationRequest
            { Operation = "SaveHolon", Payload = holon, PreferredProvider = ProviderType.MongoDBOASIS });

        result.IsError.Should().BeFalse(result.Message);
        calls.Should().Equal("primary", "first", "second");
        hyperDrive.LastReplicationDiagnostic.PrimaryProvider.Should().Be(ProviderType.MongoDBOASIS);
        hyperDrive.LastReplicationDiagnostic.IsExplicitRequest.Should().BeFalse();
        hyperDrive.LastReplicationDiagnostic.Attempts.Select(x => x.Provider).Should()
            .Equal(ProviderType.IPFSOASIS, ProviderType.Neo4jOASIS);
        hyperDrive.LastReplicationDiagnostic.SucceededCount.Should().Be(2);
        hyperDrive.LastReplicationDiagnostic.FailedCount.Should().Be(0);
        manager.LastReplicationDiagnostic.Should().BeSameAs(hyperDrive.LastReplicationDiagnostic);
        result.MetaData.Should().ContainKey("hyperDriveReplicationDiagnostic");
    }

    [Fact]
    public async Task V2ReplicationPublishesStructuredPartialFailureWithoutFailingPrimaryMutation()
    {
        var manager = new ProviderManager(null, CreateDna(HyperDriveModes.V2))
            { IsAutoReplicationEnabled = true };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "primary");
        var failed = CreateActiveProvider(ProviderType.IPFSOASIS, "failed");
        var recovered = CreateActiveProvider(ProviderType.Neo4jOASIS, "recovered");
        var holon = new Holon { Id = Guid.NewGuid() };
        primary.Setup(x => x.SaveHolonAsync(holon, true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>(holon));
        failed.Setup(x => x.SaveHolonAsync(holon, true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>
                { IsError = true, ErrorCode = "REPLICA_UNAVAILABLE", Message = "Replica unavailable." });
        recovered.Setup(x => x.SaveHolonAsync(holon, true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>(holon));
        manager.RegisterProvider(primary.Object);
        manager.RegisterProvider(failed.Object);
        manager.RegisterProvider(recovered.Object);
        manager.SetAndReplaceAutoReplicationListForProviders(new[]
        {
            new EnumValue<ProviderType>(ProviderType.IPFSOASIS),
            new EnumValue<ProviderType>(ProviderType.Neo4jOASIS)
        }).IsError.Should().BeFalse();

        var result = await new OASISHyperDrive(manager).RouteRequestAsync<IHolon>(new StorageOperationRequest
            { Operation = "SaveHolon", Payload = holon, PreferredProvider = ProviderType.MongoDBOASIS });

        result.IsError.Should().BeFalse(result.Message);
        result.IsWarning.Should().BeTrue();
        manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
        manager.LastReplicationDiagnostic.FailedCount.Should().Be(1);
        manager.LastReplicationDiagnostic.Attempts.Select(x => x.Provider).Should()
            .Equal(ProviderType.IPFSOASIS, ProviderType.Neo4jOASIS);
        manager.LastReplicationDiagnostic.Attempts[0].ErrorCode.Should().Be("REPLICA_UNAVAILABLE");
        JObject.Parse(result.MetaData["hyperDriveReplicationDiagnostic"])["FailedCount"]!
            .Value<int>().Should().Be(1);
    }

    [Fact]
    public async Task ReplicatorManagerUsesInjectedRuntimeAndTheAuthoritativeV2Pipeline()
    {
        var runtime = new ProviderManager(null, CreateDna(HyperDriveModes.V2));
        var singletonBefore = ProviderManager.Instance.CurrentStorageProvider;
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "manager-primary");
        var secondary = CreateActiveProvider(ProviderType.IPFSOASIS, "manager-secondary");
        var holon = new Holon { Id = Guid.NewGuid() };
        secondary.Setup(x => x.SaveHolonAsync(holon, true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>(holon));
        runtime.RegisterProvider(primary.Object);
        runtime.RegisterProvider(secondary.Object);
        var manager = new ReplicatorManager(primary.Object, runtime.OASISDNA, runtime);

        manager.ConfigureReplicationProviders(new[] { ProviderType.IPFSOASIS }).IsError.Should().BeFalse();
        var result = await manager.ReplicateAsync<IHolon>(new StorageOperationRequest
            { Operation = "SaveHolon", Payload = holon });

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Should().ContainSingle().Which.Should().BeSameAs(holon);
        manager.OASISStorageProviders.Should().ContainSingle().Which.Should().BeSameAs(secondary.Object);
        manager.LastReplicationDiagnostic.Should().BeSameAs(runtime.LastReplicationDiagnostic);
        runtime.LastReplicationDiagnostic.Attempts.Should().ContainSingle()
            .Which.Provider.Should().Be(ProviderType.IPFSOASIS);
        ProviderManager.Instance.CurrentStorageProvider.Should().BeSameAs(singletonBefore);
    }

    [Fact]
    public void ReplicatorManagerRejectsUnregisteredTargetsWithoutChangingItsConfiguredList()
    {
        var runtime = new ProviderManager(null, CreateDna(HyperDriveModes.V2));
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "manager-primary");
        runtime.RegisterProvider(primary.Object);
        var manager = new ReplicatorManager(primary.Object, runtime.OASISDNA, runtime);

        var result = manager.ConfigureReplicationProviders(new[] { ProviderType.IPFSOASIS });

        result.IsError.Should().BeTrue();
        result.ErrorCode.Should().Be("HYPERDRIVE_REPLICATION_PROVIDER_NOT_REGISTERED");
        manager.OASISStorageProviders.Should().BeEmpty();
    }

    [Fact]
    public async Task V2DoesNotReplicateMutationsWhenAutoReplicationIsDisabled()
    {
        var manager = new ProviderManager(null, CreateDna(HyperDriveModes.V2))
            { IsAutoReplicationEnabled = false };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "primary");
        var secondary = CreateActiveProvider(ProviderType.IPFSOASIS, "secondary");
        var holon = new Holon { Id = Guid.NewGuid() };
        primary.Setup(x => x.SaveHolonAsync(holon, true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>(holon));
        manager.RegisterProvider(primary.Object);
        manager.RegisterProvider(secondary.Object);
        manager.SetAndReplaceAutoReplicationListForProviders(new[]
            { new EnumValue<ProviderType>(ProviderType.IPFSOASIS) });

        var result = await new OASISHyperDrive(manager).RouteRequestAsync<IHolon>(new StorageOperationRequest
            { Operation = "SaveHolon", Payload = holon, PreferredProvider = ProviderType.MongoDBOASIS });

        result.IsError.Should().BeFalse(result.Message);
        secondary.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        manager.LastReplicationDiagnostic.Should().BeNull();
        result.MetaData.Should().NotContainKey("hyperDriveReplicationDiagnostic");
    }

    [Fact]
    public async Task V2ReplicatesOrdinaryMutationsInlineWhenHostedSyncIsEnabled()
    {
        var dna = CreateDna(HyperDriveModes.V2);
        dna.OASIS.OASISHyperDriveConfig = new NextGenSoftware.OASIS.API.Core.Configuration.OASISHyperDriveConfig
            { EnableHostedSync = true };
        var manager = new ProviderManager(null, dna) { IsAutoReplicationEnabled = true };
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "primary");
        var secondary = CreateActiveProvider(ProviderType.IPFSOASIS, "secondary");
        var holon = new Holon { Id = Guid.NewGuid() };
        primary.Setup(x => x.SaveHolonAsync(holon, true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>(holon));
        manager.RegisterProvider(primary.Object);
        manager.RegisterProvider(secondary.Object);
        manager.SetAndReplaceAutoReplicationListForProviders(new[]
            { new EnumValue<ProviderType>(ProviderType.IPFSOASIS) });

        var result = await new OASISHyperDrive(manager).RouteRequestAsync<IHolon>(new StorageOperationRequest
            { Operation = "SaveHolon", Payload = holon, PreferredProvider = ProviderType.MongoDBOASIS });

        result.IsError.Should().BeFalse(result.Message);
        secondary.Verify(x => x.SaveHolonAsync(It.IsAny<IHolon>(), It.IsAny<bool>(), It.IsAny<bool>(),
            It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Once);
        manager.LastReplicationDiagnostic.DeferredToDurableHostedPipeline.Should().BeFalse();
        manager.LastReplicationDiagnostic.Attempts.Should().ContainSingle();
        result.MetaData.Should().ContainKey("hyperDriveReplicationDiagnostic");
    }

    [Fact]
    public async Task V2RecordsEveryProviderOutcomeInTheLoadBalancingMetricsSource()
    {
        var manager = new ProviderManager(null, CreateDna(HyperDriveModes.V2));
        var provider = CreateActiveProvider(ProviderType.MongoDBOASIS, "primary");
        var holon = new Holon { Id = Guid.NewGuid() };
        provider.Setup(x => x.LoadHolonAsync(holon.Id)).ReturnsAsync(new OASISResult<IHolon>(holon));
        manager.RegisterProvider(provider.Object);

        var result = await new OASISHyperDrive(manager).RouteRequestAsync<IHolon>(new StorageOperationRequest
            { Operation = "LoadHolon", HolonId = holon.Id, PreferredProvider = ProviderType.MongoDBOASIS });

        result.IsError.Should().BeFalse(result.Message);
        var metrics = manager.PerformanceMonitor.GetMetrics(ProviderType.MongoDBOASIS);
        metrics.Should().NotBeNull();
        metrics.TotalRequests.Should().Be(1);
        metrics.SuccessfulRequests.Should().Be(1);
        metrics.FailedRequests.Should().Be(0);
    }

    [Fact]
    public async Task ApplyingV2FailoverFlagChangesTheNextRequestsRoutingBehavior()
    {
        var manager = new ProviderManager(null, CreateDna(HyperDriveModes.V2));
        var primary = CreateActiveProvider(ProviderType.MongoDBOASIS, "runtime-policy-primary");
        var secondary = CreateActiveProvider(ProviderType.IPFSOASIS, "runtime-policy-secondary");
        var avatarId = Guid.NewGuid();
        primary.Setup(x => x.ActivateProvider()).Returns(new OASISResult<bool>(true));
        primary.Setup(x => x.LoadAvatarAsync(avatarId, 0)).ReturnsAsync(new OASISResult<IAvatar>
        {
            IsError = true,
            ErrorCount = 1,
            ErrorCode = "PRIMARY_UNAVAILABLE",
            Message = "Primary unavailable."
        });
        secondary.Setup(x => x.LoadAvatarAsync(avatarId, 0))
            .ReturnsAsync(new OASISResult<IAvatar>(new Avatar { Id = avatarId }));
        manager.RegisterProvider(primary.Object);
        manager.RegisterProvider(secondary.Object);
        manager.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        var hyperDrive = new OASISHyperDrive(manager);
        var disabledPolicy = new NextGenSoftware.OASIS.API.Core.Configuration.OASISHyperDriveConfig
        {
            AutoFailoverEnabled = false,
            AutoLoadBalancingEnabled = false,
            AutoReplicationEnabled = false,
            AutoFailoverProviders = new List<string> { "IPFSOASIS" }
        };

        manager.ApplyHyperDriveConfiguration(disabledPolicy).IsError.Should().BeFalse();
        var disabledResult = await hyperDrive.RouteRequestAsync<IAvatar>(new StorageOperationRequest
            { Operation = "LoadAvatar", AvatarId = avatarId });

        disabledResult.IsError.Should().BeTrue();
        secondary.Verify(x => x.LoadAvatarAsync(avatarId, 0), Times.Never);

        var enabledPolicy = new NextGenSoftware.OASIS.API.Core.Configuration.OASISHyperDriveConfig
        {
            AutoFailoverEnabled = true,
            AutoLoadBalancingEnabled = false,
            AutoReplicationEnabled = false,
            AutoFailoverProviders = new List<string> { "IPFSOASIS" }
        };
        manager.ApplyHyperDriveConfiguration(enabledPolicy).IsError.Should().BeFalse();

        var enabledResult = await hyperDrive.RouteRequestAsync<IAvatar>(new StorageOperationRequest
            { Operation = "LoadAvatar", AvatarId = avatarId });

        enabledResult.IsError.Should().BeFalse(enabledResult.Message);
        enabledResult.Result.Id.Should().Be(avatarId);
        secondary.Verify(x => x.LoadAvatarAsync(avatarId, 0), Times.Once);
    }

    [Fact]
    public async Task LiveRecordedLatencyChangesTheNextAutomaticProviderSelection()
    {
        var manager = new ProviderManager(null, CreateDna(HyperDriveModes.V2))
            { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = true };
        var slow = CreateActiveProvider(ProviderType.IPFSOASIS, "slow-live-provider");
        var fast = CreateActiveProvider(ProviderType.SQLLiteDBOASIS, "fast-live-provider");
        slow.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>())).Returns(async () =>
        {
            await Task.Delay(75);
            return new OASISResult<IHolon>(new Holon { Id = Guid.NewGuid() });
        });
        fast.Setup(x => x.LoadHolonAsync(It.IsAny<Guid>())).ReturnsAsync(
            new OASISResult<IHolon>(new Holon { Id = Guid.NewGuid() }));
        manager.RegisterProvider(slow.Object);
        manager.RegisterProvider(fast.Object);
        manager.SetAndReplaceAutoLoadBalanceListForProviders(new[]
        {
            new EnumValue<ProviderType>(ProviderType.IPFSOASIS),
            new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)
        }).IsError.Should().BeFalse();
        var hyperDrive = new OASISHyperDrive(manager);

        (await hyperDrive.RouteRequestAsync<IHolon>(new StorageOperationRequest
        {
            Operation = "LoadHolon",
            HolonId = Guid.NewGuid(),
            PreferredProvider = ProviderType.IPFSOASIS
        })).IsError.Should().BeFalse();
        (await hyperDrive.RouteRequestAsync<IHolon>(new StorageOperationRequest
        {
            Operation = "LoadHolon",
            HolonId = Guid.NewGuid(),
            PreferredProvider = ProviderType.SQLLiteDBOASIS
        })).IsError.Should().BeFalse();

        var automaticallyRouted = await hyperDrive.RouteRequestAsync<IHolon>(new StorageOperationRequest
            { Operation = "LoadHolon", HolonId = Guid.NewGuid() });

        automaticallyRouted.IsError.Should().BeFalse(automaticallyRouted.Message);
        manager.PerformanceMonitor.GetMetrics(ProviderType.IPFSOASIS).TotalRequests.Should().Be(1);
        manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS).TotalRequests.Should().Be(2);
        fast.Verify(x => x.LoadHolonAsync(It.IsAny<Guid>()), Times.Exactly(2));
        slow.Verify(x => x.LoadHolonAsync(It.IsAny<Guid>()), Times.Once);
    }

    private static Mock<IOASISStorageProvider> CreateActiveProvider(
        ProviderType providerType = ProviderType.MongoDBOASIS,
        string providerName = "test-mongo")
    {
        var provider = new Mock<IOASISStorageProvider>();
        provider.SetupAllProperties();
        provider.Object.ProviderType = new EnumValue<ProviderType>(providerType);
        provider.Object.ProviderCategory = new EnumValue<ProviderCategory>(ProviderCategory.Storage);
        provider.Object.ProviderName = providerName;
        provider.Object.IsProviderActivated = true;
        return provider;
    }

    private static ProviderManager CreateProviderManager()
    {
        return new ProviderManager(null, CreateDna());
    }

    private static OASISDNA CreateDna(string hyperDriveMode = HyperDriveModes.Legacy)
    {
        return new OASISDNA
        {
            OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                HyperDriveMode = hyperDriveMode,
                StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false }
            }
        };
    }
}
