using FluentAssertions;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.Utilities;
using Xunit;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static readonly string[] QueryKeys = { "CreatedByAvatarId", "Active", "HolonType", "STARNETHolonId", "_versionStamp", "data",
        "Username", "Email", "Name", "Description", "ParentId", "ParentProviderKey", "SearchQuery", "parkType", "env",
        "web3TokenId", "NFT.MintedByAvatarId", "NFT.MintWalletAddress", "NFT.ParentWeb4NFTId", "GEONFT.PlacedByAvatarId",
        "GEONFT.MintedByAvatarId", "GEONFT.OriginalOASISNFT.MintWalletAddress", "GEONFT.LatLong", "Status", "ActiveQuestId",
        "ActiveObjectiveId", "ParentMissionId", "Avatar", "AppRegion" };
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task HolonReplicationUsesBothRealPriorityStores(bool synchronous, bool mongoPrimary)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
            throw new InvalidOperationException("Owned reusable Mongo runner required; refusing other databases.");
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1"))) root = root.Parent;
        if (root == null) throw new InvalidOperationException("Existing checkout required.");
        var directory = Path.Combine(root.FullName,"TestResults/HyperDriveMongoSqliteCurrent");
        Directory.CreateDirectory(directory);
        using var lease = new FileStream(Path.Combine(directory,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var dbPath = Path.Combine(directory,"owned.db");
        const string databaseName = "hyperdrive_mongo_sqlite_fixture";
        var client = new MongoClient(uri);
        using var names = await client.ListDatabaseNamesAsync();
        if (File.Exists(dbPath) || (await names.ToListAsync()).Contains(databaseName))
            throw new InvalidOperationException("Existing fixture requires inspection; refusing overwrite or cleanup.");
        try
        {
            var mongo = new MongoDBOASIS(uri,databaseName);
            using var sqlite = new SQLLiteDBOASIS($"Data Source={dbPath};Pooling=False");
            (await mongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
            var deletionId=Guid.NewGuid(); var deletingActor=Guid.NewGuid();
            (await mongo.SaveAvatarAsync(new Avatar {Id=deletionId,Username="owned tombstone primary",IsActive=true,DeletedByAvatarId=deletingActor,AvatarType=new EnumValue<AvatarType>(AvatarType.User)})).IsError.Should().BeFalse();
            (await mongo.SaveAvatarDetailAsync(new AvatarDetail {Id=deletionId,Username="owned tombstone primary",IsActive=true,XP=57,Karma=29})).IsError.Should().BeFalse();
            var deletion=synchronous ? mongo.DeleteAvatar(deletionId) : await mongo.DeleteAvatarAsync(deletionId);
            deletion.IsError.Should().BeFalse(deletion.Message);
            NextGenSoftware.OASIS.API.Core.Helpers.AvatarTombstoneEvidence.TryGet(deletion,out var committedDeletion).Should().BeTrue();
            var deletedAvatar=(await mongo.LoadAvatarAsync(deletionId)).Result;
            var primaryDeletedDetail=(await mongo.LoadAvatarDetailAsync(deletionId)).Result;
            deletedAvatar.IsActive.Should().BeFalse(); primaryDeletedDetail.IsActive.Should().BeFalse();
            deletedAvatar.DeletedDate.Should().Be(committedDeletion.DeletedAtUtc);
            primaryDeletedDetail.DeletedDate.Should().Be(committedDeletion.DeletedAtUtc);
            deletedAvatar.DeletedByAvatarId.Should().Be(deletingActor); primaryDeletedDetail.DeletedByAvatarId.Should().Be(deletingActor);
            Convert.ToInt64(primaryDeletedDetail.XP).Should().Be(57); Convert.ToInt64(primaryDeletedDetail.Karma).Should().Be(29);
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var manager = new ProviderManager(null,dna) { IsAutoReplicationEnabled = true };
            manager.RegisterProvider(mongo);
            manager.RegisterProvider(sqlite);
            manager.SetAndReplaceAutoReplicationListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError.Should().BeFalse();
            var holon = new Holon { Id = Guid.NewGuid(), Name = "priority-replication" };
            var request = new StorageOperationRequest { Operation = "SaveHolon", Payload = holon, SaveChildren = false, PreferredProvider = mongoPrimary ? ProviderType.MongoDBOASIS : ProviderType.SQLLiteDBOASIS };
            var router = new OASISHyperDrive(manager);
            await VerifyMongoHolonDeleteOutcomesAsync(mongo,sqlite,manager,router,synchronous);
            var replicatedDeletionId=Guid.NewGuid(); var replicatedActor=Guid.NewGuid();
            foreach(var store in new IOASISStorageProvider[] {mongo,sqlite})
            {
                (await store.SaveAvatarAsync(new Avatar {Id=replicatedDeletionId,Username="owned exact replica",IsActive=true,DeletedByAvatarId=replicatedActor,AvatarType=new EnumValue<AvatarType>(AvatarType.User)})).IsError.Should().BeFalse();
                (await store.SaveAvatarDetailAsync(new AvatarDetail {Id=replicatedDeletionId,Username="owned exact replica",IsActive=true,XP=store==mongo ? 101:202,Karma=29})).IsError.Should().BeFalse();
            }
            var deletionRequest=new StorageOperationRequest {Operation="DeleteAvatar",AvatarId=replicatedDeletionId,SoftDelete=true,PreferredProvider=request.PreferredProvider};
            var replicatedDeletion=synchronous ? router.RouteRequest<bool>(deletionRequest) : await router.RouteRequestAsync<bool>(deletionRequest);
            replicatedDeletion.IsError.Should().BeFalse(replicatedDeletion.Message);
            replicatedDeletion.IsWarning.Should().BeFalse(replicatedDeletion.Message);
            NextGenSoftware.OASIS.API.Core.Helpers.AvatarTombstoneEvidence.TryGet(replicatedDeletion,out var replicatedSnapshot).Should().BeTrue();
            foreach(var store in new IOASISStorageProvider[] {mongo,sqlite})
            {
                var profile=(await store.LoadAvatarAsync(replicatedDeletionId)).Result;
                var statistics=(await store.LoadAvatarDetailAsync(replicatedDeletionId)).Result;
                profile.DeletedDate.Should().Be(replicatedSnapshot.DeletedAtUtc); statistics.DeletedDate.Should().Be(replicatedSnapshot.DeletedAtUtc);
                profile.DeletedByAvatarId.Should().Be(replicatedActor); statistics.DeletedByAvatarId.Should().Be(replicatedActor);
                profile.IsActive.Should().BeFalse(); statistics.IsActive.Should().BeFalse();
                Convert.ToInt64(statistics.XP).Should().Be(store==mongo ? 101:202);
                Convert.ToInt64(statistics.Karma).Should().Be(29);
            }
            var repeatedTombstone=synchronous ? mongo.ApplyAvatarTombstone(replicatedSnapshot) : await mongo.ApplyAvatarTombstoneAsync(replicatedSnapshot);
            repeatedTombstone.IsError.Should().BeFalse(repeatedTombstone.Message); repeatedTombstone.IsSaved.Should().BeFalse();
            var conflictingTombstone=new AvatarTombstone(replicatedDeletionId,replicatedSnapshot.DeletedAtUtc.AddTicks(1),replicatedActor);
            var conflictResult=synchronous ? mongo.ApplyAvatarTombstone(conflictingTombstone) : await mongo.ApplyAvatarTombstoneAsync(conflictingTombstone);
            conflictResult.IsError.Should().BeTrue();
            conflictResult.ErrorCode.Should().Be(NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode);
            (await mongo.LoadAvatarAsync(replicatedDeletionId)).Result.DeletedDate.Should().Be(replicatedSnapshot.DeletedAtUtc);
            (await mongo.ApplyAvatarTombstoneAsync(new AvatarTombstone(Guid.NewGuid(),replicatedSnapshot.DeletedAtUtc))).IsError.Should().BeTrue();
            var failedReplicaId=Guid.NewGuid();
            foreach(var store in new IOASISStorageProvider[] {mongo,sqlite})
            {
                (await store.SaveAvatarAsync(new Avatar {Id=failedReplicaId,Username="owned rollback replica",IsActive=true,AvatarType=new EnumValue<AvatarType>(AvatarType.User)})).IsError.Should().BeFalse();
                (await store.SaveAvatarDetailAsync(new AvatarDetail {Id=failedReplicaId,Username="owned rollback replica",IsActive=true,XP=73})).IsError.Should().BeFalse();
            }
            var fixtureDatabase=client.GetDatabase(databaseName);
            await fixtureDatabase.RunCommandAsync<BsonDocument>(new BsonDocument {
                {"collMod","AvatarDetail"},
                {"validator",new BsonDocument("$or",new BsonArray {
                    new BsonDocument("HolonId",new BsonDocument("$ne",failedReplicaId.ToString())),
                    new BsonDocument("IsActive",true)})},
                {"validationLevel","strict"},{"validationAction","error"}});
            try
            {
                var failedReplicaRequest=new StorageOperationRequest {Operation="DeleteAvatar",AvatarId=failedReplicaId,SoftDelete=true,PreferredProvider=ProviderType.SQLLiteDBOASIS};
                var failure=synchronous ? router.RouteRequest<bool>(failedReplicaRequest) : await router.RouteRequestAsync<bool>(failedReplicaRequest);
                failure.IsError.Should().BeFalse(failure.Message); failure.Result.Should().BeTrue(); failure.IsWarning.Should().BeTrue();
                var unchangedProfile=(await mongo.LoadAvatarAsync(failedReplicaId)).Result;
                var unchangedDetail=(await mongo.LoadAvatarDetailAsync(failedReplicaId)).Result;
                unchangedProfile.IsActive.Should().BeTrue(); unchangedProfile.DeletedDate.Should().Be(DateTime.MinValue);
                unchangedDetail.IsActive.Should().BeTrue(); unchangedDetail.DeletedDate.Should().Be(DateTime.MinValue);
                Convert.ToInt64(unchangedDetail.XP).Should().Be(73);
                (await sqlite.LoadAvatarAsync(failedReplicaId)).Result.IsActive.Should().BeFalse();
                (await sqlite.LoadAvatarDetailAsync(failedReplicaId)).Result.IsActive.Should().BeFalse();
            }
            finally
            {
                await fixtureDatabase.RunCommandAsync<BsonDocument>(new BsonDocument {{"collMod","AvatarDetail"},{"validator",new BsonDocument()}});
            }
            var uncertainReplicaId=Guid.NewGuid();
            (await mongo.SaveAvatarAsync(new Avatar {Id=uncertainReplicaId,Username="owned uncertain replica",IsActive=true,AvatarType=new EnumValue<AvatarType>(AvatarType.User)})).IsError.Should().BeFalse();
            (await mongo.SaveAvatarDetailAsync(new AvatarDetail {Id=uncertainReplicaId,Username="owned uncertain replica",IsActive=true,XP=83})).IsError.Should().BeFalse();
            var replicaAdmin=client.GetDatabase("admin");
            await replicaAdmin.RunCommandAsync<BsonDocument>(new BsonDocument {
                {"configureFailPoint","failCommand"},{"mode",new BsonDocument("times",1)},
                {"data",new BsonDocument {{"failCommands",new BsonArray {"commitTransaction"}},{"errorCode",2}}}});
            try
            {
                var uncertainSnapshot=new AvatarTombstone(uncertainReplicaId,DateTime.UtcNow,Guid.NewGuid());
                var uncertainResult=synchronous ? mongo.ApplyAvatarTombstone(uncertainSnapshot) : await mongo.ApplyAvatarTombstoneAsync(uncertainSnapshot);
                uncertainResult.IsError.Should().BeTrue(); uncertainResult.Result.Should().BeFalse();
                NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(uncertainResult.Exception).Should().BeTrue();
                NextGenSoftware.OASIS.API.Core.Helpers.AvatarTombstoneEvidence.TryGet(uncertainResult,out _).Should().BeFalse();
            }
            finally
            {
                await replicaAdmin.RunCommandAsync<BsonDocument>(new BsonDocument {{"configureFailPoint","failCommand"},{"mode","off"}});
            }
            var invalidPayloadManager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true, IsAutoReplicationEnabled = true };
            invalidPayloadManager.RegisterProvider(mongo);
            invalidPayloadManager.RegisterProvider(sqlite);
            var invalidProviders = new[] { new EnumValue<ProviderType>(request.PreferredProvider),
                new EnumValue<ProviderType>(mongoPrimary ? ProviderType.SQLLiteDBOASIS : ProviderType.MongoDBOASIS) };
            invalidPayloadManager.SetAndReplaceAutoFailOverListForProviders(invalidProviders).IsError.Should().BeFalse();
            invalidPayloadManager.SetAndReplaceAutoReplicationListForProviders(invalidProviders).IsError.Should().BeFalse();
            var invalidRouter = new OASISHyperDrive(invalidPayloadManager);
            var invalidImport = new StorageOperationRequest { Operation = "Import", Payload = "invalid import payload", PreferredProvider = request.PreferredProvider };
            var invalidImportResult = synchronous ? invalidRouter.RouteRequest<bool>(invalidImport) : await invalidRouter.RouteRequestAsync<bool>(invalidImport);
            invalidImportResult.IsError.Should().BeTrue();
            invalidImportResult.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");
            var duplicatePayloadId = Guid.NewGuid();
            invalidImport.Payload = new IHolon[] { new Holon { Id = duplicatePayloadId }, new Holon { Id = duplicatePayloadId } };
            var invalidDuplicateResult = synchronous ? invalidRouter.RouteRequest<bool>(invalidImport) : await invalidRouter.RouteRequestAsync<bool>(invalidImport);
            invalidDuplicateResult.IsError.Should().BeTrue();
            invalidDuplicateResult.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");
            invalidDuplicateResult.Exception.Should().BeOfType<ArgumentException>();
            var explicitInvalidImport = await invalidRouter.FailoverRequestAsync<bool>(invalidImport);
            explicitInvalidImport.IsError.Should().BeTrue();
            explicitInvalidImport.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");
            var repeatedNewHolon = new Holon();
            IEnumerable<IHolon> ThrowingImportSource()
            {
                yield return new Holon();
                throw new InvalidOperationException("Owned enumeration failure.");
            }
            foreach (var invalidBatch in new IEnumerable<IHolon>[] {
                new IHolon[] { null }, new IHolon[] { repeatedNewHolon, repeatedNewHolon }, ThrowingImportSource() })
            {
                invalidImport.Payload = invalidBatch;
                var invalidBatchResult = synchronous ? invalidRouter.RouteRequest<bool>(invalidImport) : await invalidRouter.RouteRequestAsync<bool>(invalidImport);
                invalidBatchResult.IsError.Should().BeTrue();
                invalidBatchResult.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");
                invalidBatchResult.Exception.Should().NotBeNull();
            }
            (invalidPayloadManager.PerformanceMonitor.GetMetrics(request.PreferredProvider)?.TotalRequests ?? 0).Should().Be(0);
            (invalidPayloadManager.PerformanceMonitor.GetMetrics(invalidProviders[1].Value)?.TotalRequests ?? 0).Should().Be(0);
            var result = synchronous ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);
            result.IsError.Should().BeFalse(result.Message);
            result.IsWarning.Should().BeFalse(string.Join("; ",manager.LastReplicationDiagnostic.Attempts.Select(x=>x.Message)));
            (await mongo.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Name.Should().Be(holon.Name);
            (await sqlite.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Name.Should().Be(holon.Name);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            var replicatedGraphRoot = new Holon { Id = Guid.NewGuid(), Name = "owned-v2-graph-root" };
            var replicatedGraphChild = new Holon { Id = Guid.NewGuid(), Name = "owned-v2-graph-child", ParentHolonId = replicatedGraphRoot.Id };
            var replicatedGraphGrandchild = new Holon { Id = Guid.NewGuid(), Name = "owned-v2-graph-grandchild", ParentHolonId = replicatedGraphChild.Id };
            replicatedGraphRoot.Children = new List<IHolon> { replicatedGraphChild };
            replicatedGraphChild.Children = new List<IHolon> { replicatedGraphGrandchild };
            var graphRequest = new StorageOperationRequest { Operation = "SaveHolon", Payload = replicatedGraphRoot,
                PreferredProvider = request.PreferredProvider, SaveChildren = true, Recursive = true, ChildrenFromProvider = true };
            var graphSaved = synchronous ? router.RouteRequest<IHolon>(graphRequest) : await router.RouteRequestAsync<IHolon>(graphRequest);
            graphSaved.IsError.Should().BeFalse(graphSaved.Message);
            graphSaved.IsWarning.Should().BeFalse(graphSaved.Message);
            foreach (var graphNode in new[] { replicatedGraphRoot, replicatedGraphChild, replicatedGraphGrandchild })
            foreach (var graphStore in new IOASISStorageProvider[] { mongo, sqlite })
            {
                var graphRead = await graphStore.LoadHolonAsync(graphNode.Id,loadChildren:false);
                graphRead.IsError.Should().BeFalse(graphRead.Message);
                graphRead.Result.Should().NotBeNull("each graph node must be stored independently");
                graphRead.Result.ParentHolonId.Should().Be(graphNode.ParentHolonId);
            }
            var generatedImportHolons = new IHolon[] { new Holon { Name = "owned-generated-import-one" }, new Holon { Name = "owned-generated-import-two" } };
            var cyclicMongoRoot = new Holon { Id = Guid.NewGuid(), Name = "owned-invalid-cycle" };
            cyclicMongoRoot.Children = new List<IHolon> { cyclicMongoRoot };
            var cyclicSave = synchronous ? mongo.SaveHolon(cyclicMongoRoot,saveChildren:true,saveChildrenOnProvider:true)
                : await mongo.SaveHolonAsync(cyclicMongoRoot,saveChildren:true,saveChildrenOnProvider:true);
            cyclicSave.IsError.Should().BeTrue();
            cyclicSave.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");
            cyclicSave.Exception.Should().BeOfType<ArgumentException>();
            var cyclicImport = synchronous ? mongo.Import(new[] { cyclicMongoRoot }) : await mongo.ImportAsync(new[] { cyclicMongoRoot });
            cyclicImport.IsError.Should().BeTrue();
            cyclicImport.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");
            cyclicImport.Exception.Should().BeOfType<ArgumentException>();
            (await mongo.LoadHolonAsync(cyclicMongoRoot.Id,loadChildren:false)).Result.Should().BeNull();
            foreach (var nonrecursive in new[] { false, true })
            {
                var boundedRoot = new Holon { Id = Guid.NewGuid(), Name = "owned-bounded-root" };
                var boundedChild = new Holon { Id = Guid.NewGuid(), Name = "owned-bounded-child", ParentHolonId = boundedRoot.Id };
                var excludedGrandchild = new Holon { Id = Guid.NewGuid(), Name = "owned-excluded-grandchild", ParentHolonId = boundedChild.Id };
                boundedRoot.Children = new List<IHolon> { boundedChild };
                boundedChild.Children = new List<IHolon> { excludedGrandchild };
                var boundedRequest = new StorageOperationRequest { Operation = "SaveHolon", Payload = boundedRoot,
                    PreferredProvider = request.PreferredProvider, SaveChildren = true, Recursive = !nonrecursive,
                    MaxChildDepth = nonrecursive ? 0 : 1, ChildrenFromProvider = true };
                var boundedSave = synchronous ? router.RouteRequest<IHolon>(boundedRequest) : await router.RouteRequestAsync<IHolon>(boundedRequest);
                boundedSave.IsError.Should().BeFalse(boundedSave.Message);
                boundedSave.IsWarning.Should().BeFalse(boundedSave.Message);
                foreach (var boundedStore in new IOASISStorageProvider[] { mongo, sqlite })
                {
                    (await boundedStore.LoadHolonAsync(boundedChild.Id,loadChildren:false)).Result.Should().NotBeNull();
                    (await boundedStore.LoadHolonAsync(excludedGrandchild.Id,loadChildren:false)).Result.Should().BeNull();
                }
            }
            var enumerationCount = 0;
            IEnumerable<IHolon> SingleEnumerationImport()
            {
                if (++enumerationCount != 1) throw new InvalidOperationException("Import source was enumerated twice.");
                foreach (var generatedHolon in generatedImportHolons) yield return generatedHolon;
            }
            var generatedImportRequest = new StorageOperationRequest { Operation = "Import", Payload = SingleEnumerationImport(), PreferredProvider = request.PreferredProvider };
            var generatedImportResult = synchronous ? router.RouteRequest<bool>(generatedImportRequest) : await router.RouteRequestAsync<bool>(generatedImportRequest);
            generatedImportResult.IsError.Should().BeFalse(generatedImportResult.Message);
            generatedImportResult.IsWarning.Should().BeFalse(generatedImportResult.Message);
            enumerationCount.Should().Be(1);
            generatedImportHolons.Select(item => item.Id).Should().OnlyHaveUniqueItems().And.NotContain(Guid.Empty);
            foreach (var generatedHolon in generatedImportHolons)
            foreach (var generatedRead in new[] { await mongo.LoadHolonAsync(generatedHolon.Id,loadChildren:false), await sqlite.LoadHolonAsync(generatedHolon.Id,loadChildren:false) })
                generatedRead.Result.Name.Should().Be(generatedHolon.Name);
            var importedHolons = new IHolon[] {
                new Holon { Id = Guid.NewGuid(), Name = "owned-v2-import-one" },
                new Holon { Id = Guid.NewGuid(), Name = "owned-v2-import-two" }
            };
            var importRequest = new StorageOperationRequest { Operation = "Import", Payload = importedHolons,
                PreferredProvider = request.PreferredProvider };
            var imported = synchronous ? router.RouteRequest<bool>(importRequest) : await router.RouteRequestAsync<bool>(importRequest);
            imported.IsError.Should().BeFalse(imported.Message);
            imported.Result.Should().BeTrue();
            imported.IsWarning.Should().BeFalse(imported.Message);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            foreach (var importedHolon in importedHolons)
            foreach (var importRead in new[] { await mongo.LoadHolonAsync(importedHolon.Id,loadChildren:false), await sqlite.LoadHolonAsync(importedHolon.Id,loadChildren:false) })
            {
                importRead.IsError.Should().BeFalse(importRead.Message);
                importRead.Result.Name.Should().Be(importedHolon.Name);
            }
            foreach (var importedHolon in importedHolons)
            {
                // Fresh native provider readback must carry the key published by the initial insert.
                var importedRead = await mongo.LoadHolonAsync(importedHolon.Id,loadChildren:false);
                importedRead.Result.ProviderUniqueStorageKey.Should().ContainKey(ProviderType.MongoDBOASIS);
                ObjectId.TryParse(importedRead.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS],out _).Should().BeTrue();
                var storedImport = importedRead.Result;
                var importReplacement = new Holon { Id = storedImport.Id, Name = "owned-import-replacement",
                    CreatedDate = new DateTime(2000,1,1), CreatedByAvatarId = Guid.NewGuid() };
                var replacementResult = synchronous ? mongo.Import(new[] { importReplacement }) : await mongo.ImportAsync(new[] { importReplacement });
                replacementResult.IsError.Should().BeFalse(replacementResult.Message);
                replacementResult.Result.Should().BeTrue();
                var replacementRead = (await mongo.LoadHolonAsync(storedImport.Id,loadChildren:false)).Result;
                replacementRead.Name.Should().Be(importReplacement.Name);
                replacementRead.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS].Should().Be(storedImport.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]);
                replacementRead.CreatedDate.Should().Be(storedImport.CreatedDate);
                replacementRead.CreatedByAvatarId.Should().Be(storedImport.CreatedByAvatarId);
                replacementRead.CreatedProviderType.Value.Should().Be(storedImport.CreatedProviderType.Value);
            }
            // Seed the real engine's recommendation without fabricating a prediction model score.
            // The real stores below prove routing/replication, not WEB4 endpoint prediction generation.
            var engine = PredictiveFailoverEngine.ForProviderManager(manager);
            var redirect = typeof(PredictiveFailoverEngine).GetMethod("ExecutePreventiveFailoverAsync",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            var selected = request.PreferredProvider;
            var alternative = mongoPrimary ? ProviderType.SQLLiteDBOASIS : ProviderType.MongoDBOASIS;
            await (Task)redirect.Invoke(engine, new object[] { selected, alternative })!;
            holon.Name = "predictive-replicated";
            var redirected = synchronous ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);
            redirected.IsError.Should().BeFalse(redirected.Message);
            redirected.IsWarning.Should().BeFalse(redirected.Message);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            foreach (var predictiveRead in new[] { await mongo.LoadHolonAsync(holon.Id,loadChildren:false), await sqlite.LoadHolonAsync(holon.Id,loadChildren:false) })
                predictiveRead.Result.Name.Should().Be("predictive-replicated");
            engine.ClearFailoverOverride(selected);
            holon.Name = "priority-updated";
            holon.Version = 3;
            holon.MetaData["Email"] = "priority@test.local";
            foreach (var key in QueryKeys.Where(key=>key!="Email")) holon.MetaData[key] = "query-value:" + key;
            var updated = synchronous ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);
            updated.IsError.Should().BeFalse(updated.Message);
            updated.IsWarning.Should().BeFalse(string.Join("; ",manager.LastReplicationDiagnostic.Attempts.Select(x=>x.Message)));
            foreach (var loaded in new[] { await mongo.LoadHolonAsync(holon.Id,loadChildren:false), await sqlite.LoadHolonAsync(holon.Id,loadChildren:false) })
            {
                loaded.IsError.Should().BeFalse(loaded.Message);
                loaded.Result.Name.Should().Be("priority-updated");
                loaded.Result.Version.Should().Be(3);
                loaded.Result.MetaData["Email"].ToString().Should().Be("priority@test.local");
            }
            var balanceManager = new ProviderManager(null,dna) { IsAutoLoadBalanceEnabled = true };
            foreach (var key in QueryKeys)
            foreach (var query in new[] {
                synchronous ? mongo.LoadHolonsByMetaData(key,holon.MetaData[key].ToString(),loadChildren:false) : await mongo.LoadHolonsByMetaDataAsync(key,holon.MetaData[key].ToString(),loadChildren:false),
                synchronous ? sqlite.LoadHolonsByMetaData(key,holon.MetaData[key].ToString(),loadChildren:false) : await sqlite.LoadHolonsByMetaDataAsync(key,holon.MetaData[key].ToString(),loadChildren:false) })
            {
                query.IsError.Should().BeFalse(query.Message);
                query.Result.Select(x=>x.Id).Should().Equal(new[] { holon.Id }, "metadata key {0} must remain queryable", key);
            }
            balanceManager.RegisterProvider(mongo);
            foreach (var mode in new[] { MetaKeyValuePairMatchMode.All, MetaKeyValuePairMatchMode.Any })
            foreach (var mismatchCount in new[] { 0, 1, 2 })
            foreach (var requestedVersion in new[] { 0, 2, 3 })
            {
                var pairs = new Dictionary<string,string> { ["Email"] = mismatchCount == 2 ? "not-matching" : "priority@test.local",
                    ["parkType"] = mismatchCount > 0 ? "not-matching" : holon.MetaData["parkType"].ToString() };
                foreach (var query in new[] {
                    synchronous ? mongo.LoadHolonsByMetaData(pairs,mode,loadChildren:false,version:requestedVersion) : await mongo.LoadHolonsByMetaDataAsync(pairs,mode,loadChildren:false,version:requestedVersion),
                    synchronous ? sqlite.LoadHolonsByMetaData(pairs,mode,loadChildren:false,version:requestedVersion) : await sqlite.LoadHolonsByMetaDataAsync(pairs,mode,loadChildren:false,version:requestedVersion) })
                {
                    query.IsError.Should().BeFalse(query.Message);
                    if (requestedVersion != 2 && (mismatchCount == 0 || (mode == MetaKeyValuePairMatchMode.Any && mismatchCount == 1)))
                        query.Result.Select(x=>x.Id).Should().Equal(holon.Id);
                    else query.Result.Should().BeEmpty();
                }
            }
            balanceManager.RegisterProvider(sqlite);
            foreach (var provider in new IOASISStorageProvider[] { mongo, sqlite })
            {
                var mismatch = synchronous ? provider.LoadHolonsByMetaData("Email","priority@test.local",loadChildren:false,version:2) : await provider.LoadHolonsByMetaDataAsync("Email","priority@test.local",loadChildren:false,version:2);
                mismatch.IsError.Should().BeFalse(mismatch.Message);
                mismatch.Result.Should().BeEmpty();
                var wrongType = synchronous ? provider.LoadHolonsByMetaData("Email","priority@test.local",HolonType.Quest,loadChildren:false,version:3) : await provider.LoadHolonsByMetaDataAsync("Email","priority@test.local",HolonType.Quest,loadChildren:false,version:3);
                wrongType.IsError.Should().BeFalse(wrongType.Message);
                wrongType.Result.Should().BeEmpty();
            }
            balanceManager.SetAndReplaceAutoLoadBalanceListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError.Should().BeFalse();
            var timer = Stopwatch.StartNew();
            (await mongo.LoadHolonAsync(holon.Id,loadChildren:false)).IsError.Should().BeFalse();
            timer.Stop();
            var mongoMs = Math.Max(timer.Elapsed.TotalMilliseconds,0.001);
            timer.Restart();
            (await sqlite.LoadHolonAsync(holon.Id,loadChildren:false)).IsError.Should().BeFalse();
            timer.Stop();
            var sqliteMs = Math.Max(timer.Elapsed.TotalMilliseconds,0.001);
            balanceManager.PerformanceMonitor.RecordRequest(ProviderType.MongoDBOASIS,true,mongoMs);
            balanceManager.PerformanceMonitor.RecordRequest(ProviderType.SQLLiteDBOASIS,true,sqliteMs);
            var balancedRouter = new OASISHyperDrive(balanceManager);
            var read = new StorageOperationRequest { Operation = "LoadHolon", HolonId = holon.Id, LoadChildren = false };
            var balanced = synchronous ? balancedRouter.RouteRequest<IHolon>(read,LoadBalancingStrategy.Performance) : await balancedRouter.RouteRequestAsync<IHolon>(read,LoadBalancingStrategy.Performance);
            balanced.IsError.Should().BeFalse(balanced.Message);
            balanced.Result.Id.Should().Be(holon.Id);
            balanceManager.PerformanceMonitor.GetMetrics(mongoMs <= sqliteMs ? ProviderType.MongoDBOASIS : ProviderType.SQLLiteDBOASIS).TotalRequests.Should().BeGreaterThan(1);
            foreach (var searchProvider in new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS })
            {
                var searchRequest = new StorageOperationRequest { Operation = "Search", LoadChildren = false, PreferredProvider = searchProvider,
                    SearchParams = new SearchParams { SearchOnlyForCurrentAvatar = false,
                        SearchGroups = new() { new SearchTextGroup { SearchQuery = "priority-updated", SearchHolons = true,
                            HolonType = holon.HolonType, HolonSearchParams = new SearchHolonParams { Name = true } } } } };
                var searched = synchronous ? balancedRouter.RouteRequest<ISearchResults>(searchRequest) : await balancedRouter.RouteRequestAsync<ISearchResults>(searchRequest);
                searched.IsError.Should().BeFalse(searched.Message);
                searched.Result.SearchResultHolons.Select(h => h.Id).Should().Equal(holon.Id);
                searched.Result.SearchResultAvatars.Should().BeEmpty();
            }
            var searchCounts = new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS }
                .ToDictionary(provider => provider, provider => balanceManager.PerformanceMonitor.GetMetrics(provider).TotalRequests);
            var roundRobinSearch = new StorageOperationRequest { Operation = "Search", LoadChildren = false,
                SearchParams = new SearchParams { SearchOnlyForCurrentAvatar = false,
                    SearchGroups = new() { new SearchTextGroup { SearchQuery = "priority-updated", SearchHolons = true,
                        HolonType = holon.HolonType, HolonSearchParams = new SearchHolonParams { Name = true } } } } };
            for (var index = 0; index < 6; index++)
            {
                var searched = synchronous ? balancedRouter.RouteRequest<ISearchResults>(roundRobinSearch, LoadBalancingStrategy.RoundRobin)
                    : await balancedRouter.RouteRequestAsync<ISearchResults>(roundRobinSearch, LoadBalancingStrategy.RoundRobin);
                searched.IsError.Should().BeFalse(searched.Message);
                searched.Result.SearchResultHolons.Select(h => h.Id).Should().Equal(holon.Id);
            }
            foreach (var provider in searchCounts.Keys)
                balanceManager.PerformanceMonitor.GetMetrics(provider).TotalRequests.Should().Be(searchCounts[provider] + 3,
                    "six real round-robin search requests must execute three times on each provider");
            var delete = new StorageOperationRequest { Operation = "DeleteHolon", HolonId = holon.Id, SoftDelete = false, PreferredProvider = request.PreferredProvider };
            var deleted = synchronous ? router.RouteRequest<IHolon>(delete) : await router.RouteRequestAsync<IHolon>(delete);
            deleted.IsError.Should().BeFalse(deleted.Message);
            deleted.IsWarning.Should().BeFalse(string.Join("; ",manager.LastReplicationDiagnostic.Attempts.Select(x=>x.Message)));
            (await mongo.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Should().BeNull();
            (await sqlite.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Should().BeNull();
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "priority-avatar", Email = "priority-avatar@test.local", FirstName = "Initial",
                IsActive = false, Verified = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), AvatarType = new EnumValue<AvatarType>(AvatarType.User) };
            avatar.VerificationToken = "priority-verification-owned";
            avatar.ResetToken = "priority-reset-owned";
            const string collectionRefreshToken = "priority-refresh-owned";
            avatar.RefreshToken = null;
            avatar.RefreshTokens = new List<RefreshToken> { new RefreshToken { Id = 7, Token = collectionRefreshToken,
                CreatedByIp = "127.0.0.1", Expires = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc) } };
            var avatarRequest = new StorageOperationRequest { Operation = "SaveAvatar", Payload = avatar, PreferredProvider = request.PreferredProvider };
            foreach (var updating in new[] { false, true })
            {
                if (updating) { avatar.FirstName = "Updated"; avatar.Email = "priority-updated@test.local"; avatar.IsActive = true; avatar.Verified = null; avatar.Version = 3; }
                var saved = synchronous ? router.RouteRequest<IAvatar>(avatarRequest) : await router.RouteRequestAsync<IAvatar>(avatarRequest);
                saved.IsError.Should().BeFalse(saved.Message);
                saved.IsWarning.Should().BeFalse(string.Join("; ",manager.LastReplicationDiagnostic.Attempts.Select(x=>x.Message)));
                if (!updating)
                {
                    var localWallet = new ProviderWallet { WalletId = Guid.NewGuid(), AvatarId = avatar.Id,
                        ProviderType = ProviderType.SolanaOASIS, PublicKey = "owned-public-wallet",
                        PrivateKey = "opaque-owned-test-ciphertext", SecretRecoveryPhrase = "opaque-owned-test-recovery" };
                    var storedWallet = await sqlite.SaveProviderWalletsForAvatarByIdAsync(avatar.Id, new()
                    {
                        [ProviderType.SolanaOASIS] = new() { localWallet }
                    });
                    storedWallet.IsError.Should().BeFalse(storedWallet.Message);
                }
                else
                {
                    var retainedWallets = await sqlite.LoadProviderWalletsForAvatarByIdAsync(avatar.Id);
                    retainedWallets.IsError.Should().BeFalse(retainedWallets.Message);
                    var retainedWallet = retainedWallets.Result[ProviderType.SolanaOASIS].Should().ContainSingle().Subject;
                    retainedWallet.PrivateKey.Should().Be("opaque-owned-test-ciphertext");
                    retainedWallet.SecretRecoveryPhrase.Should().Be("opaque-owned-test-recovery");
                    avatar.ProviderWallets.Should().BeEmpty("public replicated avatar does not own local wallet credentials");
                }
                foreach (var loaded in new[] { await mongo.LoadAvatarAsync(avatar.Id), await sqlite.LoadAvatarAsync(avatar.Id),
                    await mongo.LoadAvatarByUsernameAsync(avatar.Username), await sqlite.LoadAvatarByEmailAsync(avatar.Email) })
                {
                    loaded.IsError.Should().BeFalse(loaded.Message);
                    loaded.Result.Id.Should().Be(avatar.Id);
                    loaded.Result.Email.Should().Be(avatar.Email);
                    loaded.Result.FirstName.Should().Be(avatar.FirstName);
                    loaded.Result.IsActive.Should().Be(avatar.IsActive);
                    loaded.Result.Verified.Should().Be(avatar.Verified);
                    loaded.Result.IsVerified.Should().Be(avatar.IsVerified);
                    loaded.Result.RefreshToken.Should().BeNull();
                    loaded.Result.RefreshTokens.Should().ContainSingle();
                    loaded.Result.RefreshTokens[0].Token.Should().Be(collectionRefreshToken);
                    loaded.Result.RefreshTokens[0].Id.Should().Be(7);
                    loaded.Result.RefreshTokens[0].CreatedByIp.Should().Be("127.0.0.1");
                    loaded.Result.RefreshTokens[0].Expires.Should().Be(avatar.RefreshTokens[0].Expires);
                }
            }
            foreach (var tokenProvider in new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS })
            foreach (var operation in new[] { "LoadAvatarByVerificationToken", "LoadAvatarByResetToken", "LoadAvatarByRefreshToken" })
            foreach (var requestedVersion in new[] { 0, 2, 3 })
            {
                var tokenRequest = new StorageOperationRequest { Operation = operation, PreferredProvider = tokenProvider, Version = requestedVersion,
                    VerificationToken = avatar.VerificationToken, ResetToken = avatar.ResetToken, RefreshToken = collectionRefreshToken };
                var beforeTokenRead = manager.PerformanceMonitor.GetMetrics(tokenProvider).TotalRequests;
                var loaded = synchronous ? router.RouteRequest<IAvatar>(tokenRequest) : await router.RouteRequestAsync<IAvatar>(tokenRequest);
                loaded.IsError.Should().BeFalse(loaded.Message);
                loaded.IsLoaded.Should().Be(requestedVersion != 2, "{0} on {1}, version {2}, result {3}, message {4}",
                    operation, tokenProvider, requestedVersion, loaded.Result?.Id, loaded.Message);
                if (requestedVersion == 2) loaded.Result.Should().BeNull();
                else loaded.Result.Id.Should().Be(avatar.Id);
                manager.PerformanceMonitor.GetMetrics(tokenProvider).TotalRequests.Should().Be(beforeTokenRead + 1);
            }
            foreach (var invalid in new[] { null, "", " " })
            foreach (var provider in new IOASISStorageProvider[] { mongo, sqlite })
            foreach (var invalidRead in new[] {
                synchronous ? provider.LoadAvatarByVerificationToken(invalid) : await provider.LoadAvatarByVerificationTokenAsync(invalid),
                synchronous ? provider.LoadAvatarByResetToken(invalid) : await provider.LoadAvatarByResetTokenAsync(invalid),
                synchronous ? provider.LoadAvatarByRefreshToken(invalid) : await provider.LoadAvatarByRefreshTokenAsync(invalid)
            }) {
                invalidRead.IsError.Should().BeTrue();
                invalidRead.IsLoaded.Should().BeFalse();
                invalidRead.Result.Should().BeNull();
                invalidRead.Exception.Should().BeAssignableTo<ArgumentException>();
            }
            const string rotatedRefreshToken = "priority-rotated-refresh-owned";
            foreach (var provider in new IOASISStorageProvider[] { mongo, sqlite })
            {
                foreach (var duplicateId in new[] { Guid.NewGuid(), Guid.NewGuid() })
                {
                    var duplicate = new Avatar { Id = duplicateId, Username = duplicateId.ToString(), Email = duplicateId + "@ambiguous.invalid",
                        VerificationToken = "owned-ambiguous-verification", ResetToken = "owned-ambiguous-reset",
                        RefreshTokens = new() { new RefreshToken { Token = "owned-ambiguous-refresh" } },
                        AvatarType = new EnumValue<AvatarType>(AvatarType.User) };
                    var saved = synchronous ? provider.SaveAvatar(duplicate) : await provider.SaveAvatarAsync(duplicate);
                    saved.IsError.Should().BeFalse(saved.Message);
                }
                foreach (var ambiguous in new[] {
                    synchronous ? provider.LoadAvatarByVerificationToken("owned-ambiguous-verification") : await provider.LoadAvatarByVerificationTokenAsync("owned-ambiguous-verification"),
                    synchronous ? provider.LoadAvatarByResetToken("owned-ambiguous-reset") : await provider.LoadAvatarByResetTokenAsync("owned-ambiguous-reset"),
                    synchronous ? provider.LoadAvatarByRefreshToken("owned-ambiguous-refresh") : await provider.LoadAvatarByRefreshTokenAsync("owned-ambiguous-refresh")
                }) {
                    ambiguous.IsError.Should().BeTrue();
                    ambiguous.IsLoaded.Should().BeFalse();
                    ambiguous.Result.Should().BeNull();
                    ambiguous.Exception.Should().BeOfType<StorageIdentityAmbiguousException>();
                    ambiguous.ErrorCode.Should().Be(OASISResultErrorCodes.AmbiguousIdentity);
                }
            }
            avatar.RefreshTokens = new List<RefreshToken> { new RefreshToken { Id = 7, Token = rotatedRefreshToken,
                CreatedByIp = "127.0.0.2", Expires = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc) } };
            var rotatedSave = synchronous ? router.RouteRequest<IAvatar>(avatarRequest) : await router.RouteRequestAsync<IAvatar>(avatarRequest);
            foreach (var ambiguousPrimary in new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS })
            {
                var primaryStore = ambiguousPrimary == ProviderType.MongoDBOASIS ? (IOASISStorageProvider)mongo : sqlite;
                var secondaryStore = ambiguousPrimary == ProviderType.MongoDBOASIS ? (IOASISStorageProvider)sqlite : mongo;
                var secondaryType = ambiguousPrimary == ProviderType.MongoDBOASIS ? ProviderType.SQLLiteDBOASIS : ProviderType.MongoDBOASIS;
                var token = "owned-asymmetric-" + ambiguousPrimary;
                var integrityIds = new List<Guid>();
                for (var index = 0; index < 2; index++) {
                    var identity = new Avatar { Id = Guid.NewGuid(), Username = "owned-asymmetric-user-" + ambiguousPrimary,
                        Email = ambiguousPrimary + "@integrity.invalid",
                        VerificationToken = token, ResetToken = token, RefreshTokens = new() { new RefreshToken { Token = token } },
                        AvatarType = new EnumValue<AvatarType>(AvatarType.User) };
                    (await primaryStore.SaveAvatarAsync(identity)).IsError.Should().BeFalse();
                    integrityIds.Add(identity.Id);
                    if (index == 0) (await secondaryStore.SaveAvatarAsync(identity)).IsError.Should().BeFalse();
                    var identityDetail = new AvatarDetail { Id = identity.Id, Username = identity.Username, Email = identity.Email, Karma = index + 1 };
                    (await primaryStore.SaveAvatarDetailAsync(identityDetail)).IsError.Should().BeFalse();
                    if (index == 0) (await secondaryStore.SaveAvatarDetailAsync(identityDetail)).IsError.Should().BeFalse();
                }
                var integrityManager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true };
                integrityManager.RegisterProvider(mongo);
                integrityManager.RegisterProvider(sqlite);
                integrityManager.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ambiguousPrimary), new EnumValue<ProviderType>(secondaryType) }).IsError.Should().BeFalse();
                var integrityRouter = new OASISHyperDrive(integrityManager);
                foreach (var operation in new[] { "LoadAvatarByVerificationToken", "LoadAvatarByResetToken", "LoadAvatarByRefreshToken", "LoadAvatarByUsername", "LoadAvatarByEmail" }) {
                    var beforeSecondary = integrityManager.PerformanceMonitor.GetMetrics(secondaryType)?.TotalRequests ?? 0;
                    var integrityRequest = new StorageOperationRequest { Operation = operation, PreferredProvider = ambiguousPrimary,
                        VerificationToken = token, ResetToken = token, RefreshToken = token,
                        Username = "owned-asymmetric-user-" + ambiguousPrimary, Email = ambiguousPrimary + "@integrity.invalid" };
                    var rejected = synchronous ? integrityRouter.RouteRequest<IAvatar>(integrityRequest) : await integrityRouter.RouteRequestAsync<IAvatar>(integrityRequest);
                    rejected.IsError.Should().BeTrue();
                    rejected.Result.Should().BeNull();
                    rejected.ErrorCode.Should().Be(OASISResultErrorCodes.AmbiguousIdentity);
                    rejected.Exception.Should().BeOfType<StorageIdentityAmbiguousException>();
                    (integrityManager.PerformanceMonitor.GetMetrics(secondaryType)?.TotalRequests ?? 0).Should().Be(beforeSecondary);
                    var explicitRejected = await integrityRouter.FailoverRequestAsync<IAvatar>(integrityRequest);
                    explicitRejected.ErrorCode.Should().Be(OASISResultErrorCodes.AmbiguousIdentity);
                    explicitRejected.Result.Should().BeNull();
                    (integrityManager.PerformanceMonitor.GetMetrics(secondaryType)?.TotalRequests ?? 0).Should().Be(beforeSecondary);
                }
                foreach (var operation in new[] { "LoadAvatarDetailByUsername", "LoadAvatarDetailByEmail" }) {
                    // Read-side integrity assertions below also remain valid after rejected deletes.
                    var beforeSecondary = integrityManager.PerformanceMonitor.GetMetrics(secondaryType)?.TotalRequests ?? 0;
                    var detailIntegrityRequest = new StorageOperationRequest { Operation = operation, PreferredProvider = ambiguousPrimary,
                        Username = "owned-asymmetric-user-" + ambiguousPrimary, Email = ambiguousPrimary + "@integrity.invalid" };
                    var rejected = synchronous ? integrityRouter.RouteRequest<IAvatarDetail>(detailIntegrityRequest)
                        : await integrityRouter.RouteRequestAsync<IAvatarDetail>(detailIntegrityRequest);
                    rejected.IsError.Should().BeTrue();
                    rejected.Result.Should().BeNull();
                    rejected.ErrorCode.Should().Be(OASISResultErrorCodes.AmbiguousIdentity);
                    rejected.Exception.Should().BeOfType<StorageIdentityAmbiguousException>();
                    var explicitRejected = await integrityRouter.FailoverRequestAsync<IAvatarDetail>(detailIntegrityRequest);
                    explicitRejected.ErrorCode.Should().Be(OASISResultErrorCodes.AmbiguousIdentity);
                    explicitRejected.Result.Should().BeNull();
                    (integrityManager.PerformanceMonitor.GetMetrics(secondaryType)?.TotalRequests ?? 0).Should().Be(beforeSecondary);
                }
                foreach (var operation in new[] { "DeleteAvatarByUsername", "DeleteAvatarByEmail", "DeleteAvatarDetailByUsername", "DeleteAvatarDetailByEmail" })
                foreach (var softDelete in new[] { false, true }) {
                    var beforeSecondary = integrityManager.PerformanceMonitor.GetMetrics(secondaryType)?.TotalRequests ?? 0;
                    var rejectedDeleteRequest = new StorageOperationRequest { Operation = operation, PreferredProvider = ambiguousPrimary,
                        Username = "owned-asymmetric-user-" + ambiguousPrimary, Email = ambiguousPrimary + "@integrity.invalid", SoftDelete = softDelete };
                    var rejected = synchronous ? integrityRouter.RouteRequest<bool>(rejectedDeleteRequest)
                        : await integrityRouter.RouteRequestAsync<bool>(rejectedDeleteRequest);
                    rejected.IsError.Should().BeTrue();
                    rejected.Result.Should().BeFalse();
                    rejected.ErrorCode.Should().Be(OASISResultErrorCodes.AmbiguousIdentity);
                    rejected.Exception.Should().BeOfType<StorageIdentityAmbiguousException>();
                    var explicitRejected = await integrityRouter.FailoverRequestAsync<bool>(rejectedDeleteRequest);
                    explicitRejected.ErrorCode.Should().Be(OASISResultErrorCodes.AmbiguousIdentity);
                    explicitRejected.Result.Should().BeFalse();
                    (integrityManager.PerformanceMonitor.GetMetrics(secondaryType)?.TotalRequests ?? 0).Should().Be(beforeSecondary);
                    foreach (var id in integrityIds) {
                        (await primaryStore.LoadAvatarAsync(id)).Result.DeletedDate.Should().Be(DateTime.MinValue);
                        (await primaryStore.LoadAvatarDetailAsync(id)).Result.DeletedDate.Should().Be(DateTime.MinValue);
                    }
                    (await secondaryStore.LoadAvatarAsync(integrityIds[0])).Result.DeletedDate.Should().Be(DateTime.MinValue);
                    (await secondaryStore.LoadAvatarDetailAsync(integrityIds[0])).Result.Karma.Should().Be(1);
                }
            }
            rotatedSave.IsError.Should().BeFalse(rotatedSave.Message);
            rotatedSave.IsWarning.Should().BeFalse();
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            foreach (var tokenProvider in new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS })
            foreach (var token in new[] { collectionRefreshToken, rotatedRefreshToken })
            {
                var readRotated = new StorageOperationRequest { Operation = "LoadAvatarByRefreshToken", RefreshToken = token,
                    PreferredProvider = tokenProvider, Version = 3 };
                var loaded = synchronous ? router.RouteRequest<IAvatar>(readRotated) : await router.RouteRequestAsync<IAvatar>(readRotated);
                loaded.IsError.Should().BeFalse(loaded.Message);
                if (token == collectionRefreshToken) { loaded.IsLoaded.Should().BeFalse(); loaded.Result.Should().BeNull(); }
                else {
                    loaded.Result.Id.Should().Be(avatar.Id);
                    loaded.Result.RefreshTokens.Should().ContainSingle();
                    loaded.Result.RefreshTokens[0].Id.Should().Be(7);
                    loaded.Result.RefreshTokens[0].Token.Should().Be(rotatedRefreshToken);
                    loaded.Result.RefreshTokens[0].CreatedByIp.Should().Be("127.0.0.2");
                }
            }
            foreach (var searchProvider in new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS })
            foreach (var byUsername in new[] { false, true })
            {
                var emailSearch = new StorageOperationRequest { Operation = "Search", LoadChildren = false, PreferredProvider = searchProvider,
                    SearchParams = new SearchParams { SearchOnlyForCurrentAvatar = false,
                        SearchGroups = new() { new SearchTextGroup { SearchQuery = (byUsername ? avatar.Username : avatar.Email).ToUpperInvariant(), SearchAvatars = true, HolonType = HolonType.All,
                            AvatarSearchParams = new NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar.SearchAvatarParams { Email = !byUsername, Username = byUsername } } } } };
                var beforeSearch = manager.PerformanceMonitor.GetMetrics(searchProvider).TotalRequests;
                var searched = synchronous ? router.RouteRequest<ISearchResults>(emailSearch) : await router.RouteRequestAsync<ISearchResults>(emailSearch);
                searched.IsError.Should().BeFalse(searched.Message);
                searched.Result.SearchResultAvatars.Select(a => a.Id).Should().Equal(avatar.Id);
                searched.Result.SearchResultHolons.Should().BeEmpty();
                manager.PerformanceMonitor.GetMetrics(searchProvider).TotalRequests.Should().Be(beforeSearch + 1);
            }
            var detail = new AvatarDetail { Id = avatar.Id, Username = avatar.Username, Email = avatar.Email, Karma = 345, XP = 123 };
            detail.Stats.HP.Current = 37;
            detail.Stats.Mana.Current = 42;
            detail.Stats.Stamina.Current = 53;
            detail.MetaData["park-source"] = "priority-park";
            var item = new InventoryItem { Id = Guid.NewGuid(), Name = "priority-seed", Quantity = 3 };
            item.MetaData["quest-source"] = "priority-tree";
            detail.Inventory.Add(item);
            var detailRequest = new StorageOperationRequest { Operation = "SaveAvatarDetail", Payload = detail, PreferredProvider = request.PreferredProvider };
            foreach (var updating in new[] { false, true })
            {
                if (updating) { detail.Karma = 456; detail.XP = 234; detail.Stats.HP.Current = 38; item.Quantity = 4; }
                var saved = synchronous ? router.RouteRequest<IAvatarDetail>(detailRequest) : await router.RouteRequestAsync<IAvatarDetail>(detailRequest);
                saved.IsError.Should().BeFalse(saved.Message);
                saved.IsWarning.Should().BeFalse(string.Join("; ",manager.LastReplicationDiagnostic.Attempts.Select(x=>x.Message)));
                foreach (var loaded in new[] { await mongo.LoadAvatarDetailAsync(detail.Id), await sqlite.LoadAvatarDetailAsync(detail.Id) })
                {
                    loaded.IsError.Should().BeFalse(loaded.Message);
                    loaded.Result.Karma.Should().Be(detail.Karma);
                    loaded.Result.XP.Should().Be(detail.XP);
                    loaded.Result.Stats.HP.Current.Should().Be(detail.Stats.HP.Current);
                    loaded.Result.Stats.Mana.Current.Should().Be(42);
                    loaded.Result.Stats.Stamina.Current.Should().Be(53);
                    loaded.Result.MetaData["park-source"].ToString().Should().Be("priority-park");
                    loaded.Result.Inventory.Single().Id.Should().Be(item.Id);
                    loaded.Result.Inventory.Single().Quantity.Should().Be(item.Quantity);
                    loaded.Result.Inventory.Single().MetaData["quest-source"].ToString().Should().Be("priority-tree");
                }
            }
            var freshMongo = new MongoDBOASIS(uri,databaseName);
            using (var freshSqlite = new SQLLiteDBOASIS($"Data Source={dbPath};Pooling=False"))
            {
                (await freshMongo.ActivateProviderAsync()).IsError.Should().BeFalse();
                (await freshSqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
                foreach (var loaded in new[] { await freshMongo.LoadAvatarDetailAsync(detail.Id), await freshSqlite.LoadAvatarDetailAsync(detail.Id) })
                {
                    loaded.IsError.Should().BeFalse(loaded.Message);
                    loaded.Result.Karma.Should().Be(456);
                    loaded.Result.XP.Should().Be(234);
                    loaded.Result.Stats.HP.Current.Should().Be(38);
                    loaded.Result.Inventory.Single().Id.Should().Be(item.Id);
                    loaded.Result.Inventory.Single().Quantity.Should().Be(4);
                    loaded.Result.MetaData["park-source"].ToString().Should().Be("priority-park");
                }
                (await freshMongo.DeActivateProviderAsync()).IsError.Should().BeFalse();
            }
            const string failureApp = "oasis-v2-owned-failure";
            foreach (var collectionName in new[] { "Avatar", "AvatarDetail" })
            {
                var collection = client.GetDatabase(databaseName).GetCollection<BsonDocument>(collectionName);
                var existingRecord = await collection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();
                var duplicateRecord = existingRecord.DeepClone().AsBsonDocument;
                duplicateRecord["_id"] = existingRecord["_id"].IsObjectId ? new BsonObjectId(ObjectId.GenerateNewId()) : new BsonString(ObjectId.GenerateNewId().ToString());
                Func<Task> duplicateInsert = () => collection.InsertOneAsync(duplicateRecord);
                var duplicateError = await duplicateInsert.Should().ThrowAsync<MongoWriteException>();
                duplicateError.Which.WriteError.Category.Should().Be(ServerErrorCategory.DuplicateKey);
                var identityIndex = collectionName == "Avatar" ? "oasis_avatar_public_identity" : "oasis_avatar_detail_public_identity";
                await collection.Indexes.DropOneAsync(identityIndex);
                await collection.InsertOneAsync(duplicateRecord);
                try
                {
                    for (var activationAttempt = 0; activationAttempt < 2; activationAttempt++)
                    {
                        var duplicateActivation = synchronous ? mongo.ActivateProvider() : await mongo.ActivateProviderAsync();
                        duplicateActivation.IsError.Should().BeTrue();
                        duplicateActivation.Exception.Should().BeOfType<MongoCommandException>();
                        mongo.IsProviderActivated.Should().BeFalse();
                    }
                    (await collection.CountDocumentsAsync(new BsonDocument("_id", duplicateRecord["_id"]))).Should().Be(1);
                    (await collection.CountDocumentsAsync(new BsonDocument("_id", existingRecord["_id"]))).Should().Be(1);
                }
                finally
                {
                    await collection.DeleteOneAsync(new BsonDocument("_id", duplicateRecord["_id"]));
                }
                (synchronous ? mongo.ActivateProvider() : await mongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            }
            var staleDetail = new AvatarDetail { Id = Guid.NewGuid(), Username = "secondary-only-detail", Karma = 999 };
            (await sqlite.SaveAvatarDetailAsync(staleDetail)).IsError.Should().BeFalse();
            var absenceManager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true };
            absenceManager.RegisterProvider(mongo);
            absenceManager.RegisterProvider(sqlite);
            absenceManager.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError.Should().BeFalse();
            absenceManager.SetAndActivateCurrentStorageProvider(mongo).IsError.Should().BeFalse();
            var absenceRouter = new OASISHyperDrive(absenceManager);
            var absenceRequest = new StorageOperationRequest { Operation = "LoadAvatarDetail", AvatarId = staleDetail.Id, PreferredProvider = ProviderType.MongoDBOASIS };
            var authoritativeAbsence = synchronous ? absenceRouter.RouteRequest<IAvatarDetail>(absenceRequest) : await absenceRouter.RouteRequestAsync<IAvatarDetail>(absenceRequest);
            authoritativeAbsence.IsError.Should().BeFalse(authoritativeAbsence.Message);
            authoritativeAbsence.Result.Should().BeNull();
            (await sqlite.LoadAvatarDetailAsync(staleDetail.Id)).Result.Karma.Should().Be(999);
            foreach (var selector in new[] { "email", "username", "key" })
            {
                var selectorAvatar = new Avatar { Id = Guid.NewGuid(), Version = 3, Username = "mongo-selector-" + selector, Email = selector + "@mongo-selector.local",
                    AvatarType = new EnumValue<AvatarType>(AvatarType.User) };
                selectorAvatar.VerificationToken = "owned-verification-" + selector;
                selectorAvatar.ResetToken = "owned-reset-" + selector;
                selectorAvatar.RefreshTokens = new List<RefreshToken> { new RefreshToken { Token = "owned-refresh-" + selector } };
                var savedSelector = await mongo.SaveAvatarAsync(selectorAvatar);
                savedSelector.IsError.Should().BeFalse(savedSelector.Message);
                var selectorDetail = new AvatarDetail { Id = selectorAvatar.Id, Version = 3, Username = "independent-" + selector, Email = "detail-" + selector + "@mongo-selector.local", Karma = 123 };
                (await mongo.SaveAvatarDetailAsync(selectorDetail)).IsError.Should().BeFalse();
                var localKey = savedSelector.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS];
                foreach (var tokenVersion in new[] { 0, 3, 2 })
                {
                    foreach (var tokenRead in new[] {
                        synchronous ? mongo.LoadAvatarByVerificationToken(selectorAvatar.VerificationToken, tokenVersion) : await mongo.LoadAvatarByVerificationTokenAsync(selectorAvatar.VerificationToken, tokenVersion),
                        synchronous ? mongo.LoadAvatarByResetToken(selectorAvatar.ResetToken, tokenVersion) : await mongo.LoadAvatarByResetTokenAsync(selectorAvatar.ResetToken, tokenVersion),
                        synchronous ? mongo.LoadAvatarByRefreshToken("owned-refresh-" + selector, tokenVersion) : await mongo.LoadAvatarByRefreshTokenAsync("owned-refresh-" + selector, tokenVersion) })
                    {
                        tokenRead.IsError.Should().BeFalse(tokenRead.Message);
                        if (tokenVersion == 2) tokenRead.Result.Should().BeNull();
                        else tokenRead.Result.Id.Should().Be(selectorAvatar.Id);
                    }
                }
                foreach (var listVersion in new[] { 0, 3, 2 })
                {
                    var avatarList = synchronous ? mongo.LoadAllAvatars(listVersion) : await mongo.LoadAllAvatarsAsync(listVersion);
                    var detailList = synchronous ? mongo.LoadAllAvatarDetails(listVersion) : await mongo.LoadAllAvatarDetailsAsync(listVersion);
                    avatarList.IsError.Should().BeFalse(avatarList.Message);
                    detailList.IsError.Should().BeFalse(detailList.Message);
                    avatarList.Result.Any(a => a.Id == selectorAvatar.Id).Should().Be(listVersion != 2);
                    detailList.Result.Any(a => a.Id == selectorDetail.Id).Should().Be(listVersion != 2);
                }
                foreach (var requestedVersion in new[] { 0, 3, 2 })
                {
                    foreach (var detailRead in new[] {
                        synchronous ? mongo.LoadAvatarDetail(selectorDetail.Id, requestedVersion) : await mongo.LoadAvatarDetailAsync(selectorDetail.Id, requestedVersion),
                        synchronous ? mongo.LoadAvatarDetailByUsername(selectorDetail.Username, requestedVersion) : await mongo.LoadAvatarDetailByUsernameAsync(selectorDetail.Username, requestedVersion),
                        synchronous ? mongo.LoadAvatarDetailByEmail(selectorDetail.Email, requestedVersion) : await mongo.LoadAvatarDetailByEmailAsync(selectorDetail.Email, requestedVersion) })
                    {
                        detailRead.IsError.Should().BeFalse(detailRead.Message);
                        if (requestedVersion == 2) detailRead.Result.Should().BeNull();
                        else detailRead.Result.Karma.Should().Be(123);
                    }
                }
                foreach (var requestedVersion in new[] { 0, 3, 2 })
                {
                    foreach (var versionRead in new[] {
                        synchronous ? mongo.LoadAvatar(selectorAvatar.Id, requestedVersion) : await mongo.LoadAvatarAsync(selectorAvatar.Id, requestedVersion),
                        synchronous ? mongo.LoadAvatarByUsername(selectorAvatar.Username, requestedVersion) : await mongo.LoadAvatarByUsernameAsync(selectorAvatar.Username, requestedVersion),
                        synchronous ? mongo.LoadAvatarByEmail(selectorAvatar.Email, requestedVersion) : await mongo.LoadAvatarByEmailAsync(selectorAvatar.Email, requestedVersion) })
                    {
                        versionRead.IsError.Should().BeFalse(versionRead.Message);
                        if (requestedVersion == 2) versionRead.Result.Should().BeNull();
                        else versionRead.Result.Id.Should().Be(selectorAvatar.Id);
                    }
                }
                var keyRead = synchronous ? mongo.LoadAvatarByProviderKey(localKey) : await mongo.LoadAvatarByProviderKeyAsync(localKey);
                keyRead.IsError.Should().BeFalse(keyRead.Message);
                keyRead.Result.Should().NotBeNull();
                keyRead.Result.Id.Should().Be(selectorAvatar.Id);
                (synchronous ? mongo.LoadAvatarByProviderKey(localKey, 3) : await mongo.LoadAvatarByProviderKeyAsync(localKey, 3)).Result.Id.Should().Be(selectorAvatar.Id);
                (synchronous ? mongo.LoadAvatarByProviderKey(localKey, 2) : await mongo.LoadAvatarByProviderKeyAsync(localKey, 2)).Result.Should().BeNull();
                (synchronous ? mongo.LoadAvatarByProviderKey(selectorAvatar.Username) : await mongo.LoadAvatarByProviderKeyAsync(selectorAvatar.Username)).Result.Should().BeNull();
                var selectorDeleted = selector == "email"
                    ? (synchronous ? mongo.DeleteAvatarByEmail(selectorAvatar.Email, true) : await mongo.DeleteAvatarByEmailAsync(selectorAvatar.Email, true))
                    : selector == "username"
                        ? (synchronous ? mongo.DeleteAvatarByUsername(selectorAvatar.Username, true) : await mongo.DeleteAvatarByUsernameAsync(selectorAvatar.Username, true))
                        : (synchronous ? mongo.DeleteAvatar(localKey, true) : await mongo.DeleteAvatarAsync(localKey, true));
                selectorDeleted.IsError.Should().BeFalse(selectorDeleted.Message);
                selectorDeleted.Result.Should().BeTrue();
                (await mongo.LoadAvatarDetailAsync(selectorAvatar.Id)).Result.DeletedDate.Should().NotBe(DateTime.MinValue);
                var hardDeleted = selector == "email"
                    ? (synchronous ? mongo.DeleteAvatarByEmail(selectorAvatar.Email, false) : await mongo.DeleteAvatarByEmailAsync(selectorAvatar.Email, false))
                    : selector == "username"
                        ? (synchronous ? mongo.DeleteAvatarByUsername(selectorAvatar.Username, false) : await mongo.DeleteAvatarByUsernameAsync(selectorAvatar.Username, false))
                        : (synchronous ? mongo.DeleteAvatar(localKey, false) : await mongo.DeleteAvatarAsync(localKey, false));
                hardDeleted.IsError.Should().BeFalse(hardDeleted.Message);
                hardDeleted.Result.Should().BeTrue();
                (await mongo.LoadAvatarAsync(selectorAvatar.Id)).Result.Should().BeNull();
                (await mongo.LoadAvatarDetailAsync(selectorAvatar.Id)).Result.Should().BeNull();
                var missingDeleted = selector == "email"
                    ? (synchronous ? mongo.DeleteAvatarByEmail(selectorAvatar.Email, false) : await mongo.DeleteAvatarByEmailAsync(selectorAvatar.Email, false))
                    : selector == "username"
                        ? (synchronous ? mongo.DeleteAvatarByUsername(selectorAvatar.Username, false) : await mongo.DeleteAvatarByUsernameAsync(selectorAvatar.Username, false))
                        : (synchronous ? mongo.DeleteAvatar(localKey, false) : await mongo.DeleteAvatarAsync(localKey, false));
                missingDeleted.IsError.Should().BeTrue();
                missingDeleted.Result.Should().BeFalse();
            }
            var softAvatar = new Avatar { Id = Guid.NewGuid(), Username = "mongo-soft-avatar", Email = "mongo-soft@test.local",
                AvatarType = new EnumValue<AvatarType>(AvatarType.User) };
            (await mongo.SaveAvatarAsync(softAvatar)).IsError.Should().BeFalse();
            var softDetail = new AvatarDetail { Id = softAvatar.Id, Username = "independent-mongo-detail", Karma = 123 };
            (await mongo.SaveAvatarDetailAsync(softDetail)).IsError.Should().BeFalse();
            var softDeleted = synchronous ? mongo.DeleteAvatar(softAvatar.Id, true) : await mongo.DeleteAvatarAsync(softAvatar.Id, true);
            softDeleted.IsError.Should().BeFalse(softDeleted.Message);
            softDeleted.Result.Should().BeTrue();
            var softStoredAvatar = (await mongo.LoadAvatarAsync(softAvatar.Id)).Result;
            var softStoredDetail = (await mongo.LoadAvatarDetailAsync(softAvatar.Id)).Result;
            softStoredAvatar.DeletedDate.Should().NotBe(DateTime.MinValue);
            softStoredDetail.DeletedDate.Should().Be(softStoredAvatar.DeletedDate);
            softStoredDetail.Karma.Should().Be(123);
            var failureMongo = new MongoDBOASIS(uri + "&appName=" + failureApp,databaseName);
            (await failureMongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            var failManager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true };
            failManager.RegisterProvider(failureMongo);
            failManager.RegisterProvider(sqlite);
            failManager.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError.Should().BeFalse();
            failManager.SetAndActivateCurrentStorageProvider(failureMongo).IsError.Should().BeFalse();
            var admin = client.GetDatabase("admin");
            await VerifyMongoHolonDeleteWriteConcernAsync(failureMongo,mongo,sqlite,dna,admin,failureApp,synchronous,
                client.GetDatabase(databaseName).GetCollection<API.Providers.MongoDBOASIS.Entities.Holon>("Holon"));
            var retainedImportRows = new List<IHolon>();
            foreach (var importedHolon in importedHolons)
                retainedImportRows.Add((await mongo.LoadHolonAsync(importedHolon.Id,loadChildren:false)).Result);
            var duplicateImport = new IHolon[] {
                new Holon { Id = retainedImportRows[0].Id, Name = "invalid-duplicate-one" },
                new Holon { Id = retainedImportRows[0].Id, Name = "invalid-duplicate-two" }
            };
            var duplicateImportResult = synchronous ? failureMongo.Import(duplicateImport) : await failureMongo.ImportAsync(duplicateImport);
            duplicateImportResult.IsError.Should().BeTrue();
            duplicateImportResult.Result.Should().BeFalse();
            duplicateImportResult.Exception.Should().BeOfType<ArgumentException>();
            (await mongo.LoadHolonAsync(retainedImportRows[0].Id,loadChildren:false)).Result.Name.Should().Be(retainedImportRows[0].Name);
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("skip",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "update" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var replacementBatch = retainedImportRows.Select(row => (IHolon)new Holon { Id = row.Id, Name = "rejected-import-update" }).ToList();
                var rejectedReplacement = synchronous ? failureMongo.Import(replacementBatch) : await failureMongo.ImportAsync(replacementBatch);
                rejectedReplacement.IsError.Should().BeTrue();
                rejectedReplacement.Result.Should().BeFalse();
                rejectedReplacement.Exception.Should().BeOfType<MongoCommandException>();
                foreach (var retainedImport in retainedImportRows)
                {
                    var retainedRead = (await mongo.LoadHolonAsync(retainedImport.Id,loadChildren:false)).Result;
                    retainedRead.Name.Should().Be(retainedImport.Name);
                    retainedRead.CreatedDate.Should().Be(retainedImport.CreatedDate);
                    retainedRead.CreatedByAvatarId.Should().Be(retainedImport.CreatedByAvatarId);
                    retainedRead.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS].Should().Be(retainedImport.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]);
                }
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            }
            MongoTransactionReconciliationException importCommitEvidence = null;
            var uncertainImport = new IHolon[] { new Holon { Id = Guid.NewGuid(), Name = "owned-import-commit-rejected" } };
            try
            {
                var importCommitManager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true, IsAutoReplicationEnabled = true };
                importCommitManager.RegisterProvider(failureMongo);
                importCommitManager.RegisterProvider(sqlite);
                var importProviders = new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) };
                importCommitManager.SetAndReplaceAutoFailOverListForProviders(importProviders).IsError.Should().BeFalse();
                importCommitManager.SetAndReplaceAutoReplicationListForProviders(importProviders).IsError.Should().BeFalse();
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "commitTransaction" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var commitRouter = new OASISHyperDrive(importCommitManager);
                var commitRequest = new StorageOperationRequest { Operation = "Import", Payload = uncertainImport, PreferredProvider = ProviderType.MongoDBOASIS };
                var rejectedCommitImport = synchronous ? commitRouter.RouteRequest<bool>(commitRequest) : await commitRouter.RouteRequestAsync<bool>(commitRequest);
                rejectedCommitImport.IsError.Should().BeTrue();
                rejectedCommitImport.Result.Should().BeFalse();
                importCommitEvidence = rejectedCommitImport.Exception as MongoTransactionReconciliationException;
                importCommitEvidence.Should().NotBeNull();
                importCommitEvidence.InnerException.Should().BeOfType<MongoCommandException>();
                importCommitEvidence.TransactionNumber.Should().BeGreaterThanOrEqualTo(0);
                (importCommitManager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(0);
                foreach (var storage in new IOASISStorageProvider[] { mongo, sqlite })
                    (await storage.LoadHolonAsync(uncertainImport[0].Id,loadChildren:false)).Result.Should().BeNull();
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                if (importCommitEvidence != null)
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument("killSessions", new BsonArray { BsonDocument.Parse(importCommitEvidence.SessionId) }));
            }
            MongoTransactionReconciliationException graphCommitEvidence = null;
            var uncertainGraph = new Holon { Id = Guid.NewGuid(), Name = "owned-graph-commit-rejected" };
            var uncertainGraphChild = new Holon { Id = Guid.NewGuid(), Name = "owned-graph-commit-child", ParentHolonId = uncertainGraph.Id };
            uncertainGraph.Children = new List<IHolon> { uncertainGraphChild };
            try
            {
                var graphManager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true, IsAutoReplicationEnabled = true };
                graphManager.RegisterProvider(failureMongo);
                graphManager.RegisterProvider(sqlite);
                var graphProviders = new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) };
                graphManager.SetAndReplaceAutoFailOverListForProviders(graphProviders).IsError.Should().BeFalse();
                graphManager.SetAndReplaceAutoReplicationListForProviders(graphProviders).IsError.Should().BeFalse();
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "commitTransaction" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var graphRouter = new OASISHyperDrive(graphManager);
                var uncertainGraphRequest = new StorageOperationRequest { Operation = "SaveHolon", Payload = uncertainGraph,
                    PreferredProvider = ProviderType.MongoDBOASIS, SaveChildren = true, Recursive = true, ChildrenFromProvider = true };
                var rejectedGraphCommit = synchronous ? graphRouter.RouteRequest<IHolon>(uncertainGraphRequest)
                    : await graphRouter.RouteRequestAsync<IHolon>(uncertainGraphRequest);
                rejectedGraphCommit.IsError.Should().BeTrue();
                rejectedGraphCommit.Result.Should().BeNull();
                graphCommitEvidence = rejectedGraphCommit.Exception as MongoTransactionReconciliationException;
                graphCommitEvidence.Should().NotBeNull();
                graphCommitEvidence.InnerException.Should().BeOfType<MongoCommandException>();
                graphCommitEvidence.TransactionNumber.Should().BeGreaterThanOrEqualTo(0);
                (graphManager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(0);
                foreach (var storage in new IOASISStorageProvider[] { mongo, sqlite })
                foreach (var node in new[] { uncertainGraph, uncertainGraphChild })
                    (await storage.LoadHolonAsync(node.Id,loadChildren:false)).Result.Should().BeNull();
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                if (graphCommitEvidence != null)
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument("killSessions", new BsonArray { BsonDocument.Parse(graphCommitEvidence.SessionId) }));
            }
            var rejectedImportBatch = new IHolon[] {
                new Holon { Id = Guid.NewGuid(), Name = "owned-import-rollback-one" },
                new Holon { Id = Guid.NewGuid(), Name = "owned-import-rollback-two" }
            };
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("skip",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "insert" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var rejectedImport = synchronous ? failureMongo.Import(rejectedImportBatch) : await failureMongo.ImportAsync(rejectedImportBatch);
                rejectedImport.IsError.Should().BeTrue();
                rejectedImport.Result.Should().BeFalse();
                rejectedImport.Exception.Should().BeOfType<MongoCommandException>();
                foreach (var rejectedHolon in rejectedImportBatch)
                    (await mongo.LoadHolonAsync(rejectedHolon.Id,loadChildren:false)).Result.Should().BeNull();
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            }
            var bulkChild = new Holon { Id = Guid.NewGuid(), Name = "owned-bulk-rollback-child", ParentHolonId = rejectedImportBatch[0].Id };
            rejectedImportBatch[0].Children = new List<IHolon> { bulkChild };
            var bulkNodes = rejectedImportBatch.Concat(new IHolon[] { bulkChild }).ToList();
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("skip",2) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "insert" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var rejectedBulk = synchronous ? failureMongo.SaveHolons(rejectedImportBatch,saveChildren:true,saveChildrenOnProvider:true)
                    : await failureMongo.SaveHolonsAsync(rejectedImportBatch,saveChildren:true,saveChildrenOnProvider:true);
                rejectedBulk.IsError.Should().BeTrue();
                rejectedBulk.Exception.Should().BeOfType<MongoCommandException>();
                rejectedBulk.Result.Should().BeNull();
                foreach (var node in bulkNodes)
                    (await mongo.LoadHolonAsync(node.Id,loadChildren:false)).Result.Should().BeNull("a rejected bulk write must roll back earlier roots");
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            }
            var invalidBulkRoot = new Holon { Id = Guid.NewGuid(), Name = "owned-invalid-bulk-root" };
            var invalidBulkChild = new Holon { Id = Guid.NewGuid(), Name = "owned-invalid-bulk-child", ParentHolonId = invalidBulkRoot.Id };
            invalidBulkRoot.Children = new List<IHolon> { invalidBulkChild };
            foreach (var invalidRoots in new[] {
                new IHolon[] { invalidBulkRoot, invalidBulkRoot },
                new IHolon[] { invalidBulkRoot, new Holon { Id = invalidBulkRoot.Id } },
                new IHolon[] { invalidBulkRoot, invalidBulkChild },
                new IHolon[] { invalidBulkRoot, null }
            })
            {
                var invalidBulkManager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true, IsAutoReplicationEnabled = true };
                invalidBulkManager.RegisterProvider(failureMongo);
                invalidBulkManager.RegisterProvider(sqlite);
                var invalidBulkProviders = new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) };
                invalidBulkManager.SetAndReplaceAutoFailOverListForProviders(invalidBulkProviders).IsError.Should().BeFalse();
                invalidBulkManager.SetAndReplaceAutoReplicationListForProviders(invalidBulkProviders).IsError.Should().BeFalse();
                var invalidBulkRouter = new OASISHyperDrive(invalidBulkManager);
                var invalidRequest = new StorageOperationRequest { Operation = "SaveHolons", Payload = invalidRoots,
                    PreferredProvider = ProviderType.MongoDBOASIS, SaveChildren = true, Recursive = true, ChildrenFromProvider = true };
                var invalidBulk = synchronous ? invalidBulkRouter.RouteRequest<IEnumerable<IHolon>>(invalidRequest)
                    : await invalidBulkRouter.RouteRequestAsync<IEnumerable<IHolon>>(invalidRequest);
                invalidBulk.IsError.Should().BeTrue();
                invalidBulk.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");
                invalidBulk.Exception.Should().BeAssignableTo<ArgumentException>();
                (invalidBulkManager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(0);
                foreach (var store in new IOASISStorageProvider[] { mongo, sqlite })
                foreach (var node in new[] { invalidBulkRoot, invalidBulkChild })
                    (await store.LoadHolonAsync(node.Id,loadChildren:false)).Result.Should().BeNull();
            }
            var healthyBulk = synchronous ? failureMongo.SaveHolons(rejectedImportBatch,saveChildren:true,saveChildrenOnProvider:true)
                : await failureMongo.SaveHolonsAsync(rejectedImportBatch,saveChildren:true,saveChildrenOnProvider:true);
            healthyBulk.IsError.Should().BeFalse(healthyBulk.Message);
            healthyBulk.Result.Select(node => node.Id).Should().BeEquivalentTo(rejectedImportBatch.Select(node => node.Id));
            foreach (var node in bulkNodes)
                (await mongo.LoadHolonAsync(node.Id,loadChildren:false)).Result.Should().NotBeNull();
            // A rejected child insert must roll back the parent in the same graph transaction.
            var partialGraphRoot = new Holon { Id = Guid.NewGuid(), Name = "owned-partial-graph-root" };
            var partialGraphChild = new Holon { Id = Guid.NewGuid(), Name = "owned-partial-graph-child", ParentHolonId = partialGraphRoot.Id };
            partialGraphRoot.Children = new List<IHolon> { partialGraphChild };
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("skip",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "insert" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var partialGraph = synchronous
                    ? failureMongo.SaveHolon(partialGraphRoot,saveChildren:true,saveChildrenOnProvider:true)
                    : await failureMongo.SaveHolonAsync(partialGraphRoot,saveChildren:true,saveChildrenOnProvider:true);
                partialGraph.IsError.Should().BeTrue();
                partialGraph.Exception.Should().BeOfType<MongoCommandException>();
                (await mongo.LoadHolonAsync(partialGraphRoot.Id,loadChildren:false)).Result.Should().BeNull("the rejected graph must leave no committed parent");
                (await mongo.LoadHolonAsync(partialGraphChild.Id,loadChildren:false)).Result.Should().BeNull();
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            }
            var abortedGraph = new Holon { Id = Guid.NewGuid(), Name = "owned-v2-aborted-graph" };
            var abortedChild = new Holon { Id = Guid.NewGuid(), Name = "owned-v2-aborted-child", ParentHolonId = abortedGraph.Id };
            abortedGraph.Children = new List<IHolon> { abortedChild };
            try
            {
                var graphFailManager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true, IsAutoReplicationEnabled = true };
                graphFailManager.RegisterProvider(failureMongo);
                graphFailManager.RegisterProvider(sqlite);
                var graphFailProviders = new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) };
                graphFailManager.SetAndReplaceAutoFailOverListForProviders(graphFailProviders).IsError.Should().BeFalse();
                graphFailManager.SetAndReplaceAutoReplicationListForProviders(graphFailProviders).IsError.Should().BeFalse();
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("skip",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "insert" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var graphFailRouter = new OASISHyperDrive(graphFailManager);
                var graphFailRequest = new StorageOperationRequest { Operation = "SaveHolon", Payload = abortedGraph,
                    PreferredProvider = ProviderType.MongoDBOASIS, SaveChildren = true, Recursive = true, ChildrenFromProvider = true };
                var recoveredGraph = synchronous ? graphFailRouter.RouteRequest<IHolon>(graphFailRequest)
                    : await graphFailRouter.RouteRequestAsync<IHolon>(graphFailRequest);
                recoveredGraph.IsError.Should().BeFalse(recoveredGraph.Message);
                recoveredGraph.IsWarning.Should().BeTrue("the still-rejecting Mongo replica must not be reported as repaired");
                recoveredGraph.Result.Should().NotBeNull();
                foreach (var node in new[] { abortedGraph, abortedChild })
                {
                    var survivingNode = await sqlite.LoadHolonAsync(node.Id,loadChildren:false);
                    survivingNode.Result.Should().NotBeNull("the successful backup must contain the complete graph");
                    survivingNode.Result.ParentHolonId.Should().Be(node.ParentHolonId);
                    (await mongo.LoadHolonAsync(node.Id,loadChildren:false)).Result.Should().BeNull("both failed Mongo graph transactions must roll back");
                }
                graphFailManager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS).TotalRequests.Should().Be(1);
                graphFailManager.PerformanceMonitor.GetMetrics(ProviderType.MongoDBOASIS).TotalRequests.Should().Be(2);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            }
            var rejectedInsertHolon = new Holon { Id = Guid.NewGuid(), Name = "owned-holon-insert-rejected" };
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "insert" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var rejectedInsert = synchronous ? failureMongo.SaveHolon(rejectedInsertHolon,saveChildren:false)
                    : await failureMongo.SaveHolonAsync(rejectedInsertHolon,saveChildren:false);
                rejectedInsert.IsError.Should().BeTrue();
                rejectedInsert.Exception.Should().BeOfType<MongoCommandException>();
                rejectedInsert.Result.Should().BeNull();
                (await mongo.LoadHolonAsync(rejectedInsertHolon.Id,loadChildren:false)).Result.Should().BeNull();
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            }
            var searchProbe = new Holon { Id = Guid.NewGuid(), Name = "owned-search-failure-probe" };
            var saveSearchProbe = new StorageOperationRequest { Operation = "SaveHolon", Payload = searchProbe, SaveChildren = false,
                PreferredProvider = request.PreferredProvider };
            var probeSaved = synchronous ? router.RouteRequest<IHolon>(saveSearchProbe) : await router.RouteRequestAsync<IHolon>(saveSearchProbe);
            probeSaved.IsError.Should().BeFalse(probeSaved.Message);
            probeSaved.IsWarning.Should().BeFalse();
            failManager.SetAndActivateCurrentStorageProvider(failureMongo).IsError.Should().BeFalse();
            try
            {
                var failureEngine = PredictiveFailoverEngine.ForProviderManager(failManager);
                await (Task)redirect.Invoke(failureEngine, new object[] { ProviderType.SQLLiteDBOASIS, ProviderType.MongoDBOASIS })!;
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "find" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var searchFailureRequest = new StorageOperationRequest { Operation = "Search", LoadChildren = false,
                    PreferredProvider = ProviderType.SQLLiteDBOASIS, SearchParams = new SearchParams { SearchOnlyForCurrentAvatar = false,
                        SearchGroups = new() { new SearchTextGroup { SearchQuery = searchProbe.Name, SearchHolons = true,
                            HolonType = searchProbe.HolonType, HolonSearchParams = new SearchHolonParams { Name = true } } } } };
                var searchFailureRouter = new OASISHyperDrive(failManager);
                var recoveredSearch = synchronous ? searchFailureRouter.RouteRequest<ISearchResults>(searchFailureRequest)
                    : await searchFailureRouter.RouteRequestAsync<ISearchResults>(searchFailureRequest);
                recoveredSearch.IsError.Should().BeFalse(recoveredSearch.Message);
                recoveredSearch.Result.SearchResultHolons.Select(h => h.Id).Should().Equal(searchProbe.Id);
                failManager.LastFailoverDiagnostic.Should().NotBeNull("the Mongo command must fail before SQLite is selected");
                failManager.LastFailoverDiagnostic.OriginalProvider.Should().Be(ProviderType.MongoDBOASIS);
                failManager.LastFailoverDiagnostic.SelectedProvider.Should().Be(ProviderType.SQLLiteDBOASIS);
            }
            finally
            {
                PredictiveFailoverEngine.ForProviderManager(failManager).ClearFailoverOverride(ProviderType.SQLLiteDBOASIS);
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            }
            foreach (var rejectDetailUpdate in new[] { false, true })
            {
                try
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                        { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                        { "data", new BsonDocument { { "failCommands", new BsonArray { "update" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                    });
                    if (rejectDetailUpdate)
                    {
                        var attempted = (await mongo.LoadAvatarDetailAsync(detail.Id)).Result;
                        attempted.Karma = 999;
                        var rejectedUpdate = synchronous ? failureMongo.SaveAvatarDetail(attempted) : await failureMongo.SaveAvatarDetailAsync(attempted);
                        rejectedUpdate.IsError.Should().BeTrue();
                        rejectedUpdate.Exception.Should().BeOfType<MongoCommandException>();
                        (await mongo.LoadAvatarDetailAsync(detail.Id)).Result.Karma.Should().Be(456);
                    }
                    else
                    {
                        var attempted = (await mongo.LoadAvatarAsync(avatar.Id)).Result;
                        attempted.FirstName = "rejected-update";
                        var rejectedUpdate = synchronous ? failureMongo.SaveAvatar(attempted) : await failureMongo.SaveAvatarAsync(attempted);
                        rejectedUpdate.IsError.Should().BeTrue();
                        rejectedUpdate.Exception.Should().BeOfType<MongoCommandException>();
                        (await mongo.LoadAvatarAsync(avatar.Id)).Result.FirstName.Should().Be("Updated");
                    }
                }
                finally
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                }
            }
            foreach (var rejectDetailLookup in new[] { false, true })
            {
                try
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                        { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                        { "data", new BsonDocument { { "failCommands", new BsonArray { "find" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                    });
                    if (rejectDetailLookup)
                    {
                        var attempted = (await mongo.LoadAvatarDetailAsync(detail.Id)).Result;
                        attempted.Karma = 998;
                        var rejected = synchronous ? failureMongo.SaveAvatarDetail(attempted) : await failureMongo.SaveAvatarDetailAsync(attempted);
                        rejected.IsError.Should().BeTrue();
                        rejected.Exception.Should().BeOfType<MongoCommandException>();
                        (await mongo.LoadAvatarDetailAsync(detail.Id)).Result.Karma.Should().Be(456);
                    }
                    else
                    {
                        var attempted = (await mongo.LoadAvatarAsync(avatar.Id)).Result;
                        attempted.FirstName = "rejected-preflight";
                        var rejected = synchronous ? failureMongo.SaveAvatar(attempted) : await failureMongo.SaveAvatarAsync(attempted);
                        rejected.IsError.Should().BeTrue();
                        rejected.Exception.Should().BeOfType<MongoCommandException>();
                        (await mongo.LoadAvatarAsync(avatar.Id)).Result.FirstName.Should().Be("Updated");
                    }
                }
                finally
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                }
            }
            foreach (var rejectDetailInsert in new[] { false, true })
            {
                var rejectedId = Guid.NewGuid();
                try
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                        { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                        { "data", new BsonDocument { { "failCommands", new BsonArray { "insert" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                    });
                    if (rejectDetailInsert)
                    {
                        var rejectedRecord = new AvatarDetail { Id = rejectedId, Username = "rejected-create-detail" };
                        var rejectedInsert = synchronous ? failureMongo.SaveAvatarDetail(rejectedRecord) : await failureMongo.SaveAvatarDetailAsync(rejectedRecord);
                        rejectedInsert.IsError.Should().BeTrue();
                        rejectedInsert.Exception.Should().BeOfType<MongoCommandException>();
                        (await mongo.LoadAvatarDetailAsync(rejectedId)).Result.Should().BeNull();
                    }
                    else
                    {
                        var rejectedRecord = new Avatar { Id = rejectedId, Username = "rejected-create-avatar", AvatarType = new EnumValue<AvatarType>(AvatarType.User) };
                        var rejectedInsert = synchronous ? failureMongo.SaveAvatar(rejectedRecord) : await failureMongo.SaveAvatarAsync(rejectedRecord);
                        rejectedInsert.IsError.Should().BeTrue();
                        rejectedInsert.Exception.Should().BeOfType<MongoCommandException>();
                        (await mongo.LoadAvatarAsync(rejectedId)).Result.Should().BeNull();
                    }
                }
                finally
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                }
            }
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", "alwaysOn" },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "update" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var singleWriteAvatar = new Avatar { Id = Guid.NewGuid(), Username = "single-write-avatar", AvatarType = new EnumValue<AvatarType>(AvatarType.User),
                    CreatedProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) };
                (synchronous ? failureMongo.SaveAvatar(singleWriteAvatar) : await failureMongo.SaveAvatarAsync(singleWriteAvatar)).IsError.Should().BeFalse();
                var singleWriteDetail = new AvatarDetail { Id = singleWriteAvatar.Id, Username = singleWriteAvatar.Username, Karma = 123,
                    CreatedProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) };
                (synchronous ? failureMongo.SaveAvatarDetail(singleWriteDetail) : await failureMongo.SaveAvatarDetailAsync(singleWriteDetail)).IsError.Should().BeFalse();
                var storedAvatar = await client.GetDatabase(databaseName).GetCollection<API.Providers.MongoDBOASIS.Entities.Avatar>("Avatar").Find(x => x.HolonId == singleWriteAvatar.Id).FirstOrDefaultAsync();
                var storedDetail = await client.GetDatabase(databaseName).GetCollection<API.Providers.MongoDBOASIS.Entities.AvatarDetail>("AvatarDetail").Find(x => x.HolonId == singleWriteAvatar.Id).FirstOrDefaultAsync();
                storedAvatar.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS].Should().Be(storedAvatar.Id);
                storedDetail.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS].Should().Be(storedDetail.Id);
                storedAvatar.CreatedProviderType.Value.Should().Be(ProviderType.SQLLiteDBOASIS);
                storedDetail.CreatedProviderType.Value.Should().Be(ProviderType.SQLLiteDBOASIS);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            }
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "createIndexes" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var failedActivation = synchronous ? failureMongo.ActivateProvider() : await failureMongo.ActivateProviderAsync();
                failedActivation.IsError.Should().BeTrue();
                failedActivation.Exception.Should().BeOfType<MongoCommandException>();
                failureMongo.IsProviderActivated.Should().BeFalse();
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            }
            (synchronous ? failureMongo.ActivateProvider() : await failureMongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            failureMongo.IsProviderActivated.Should().BeTrue();
            foreach (var detailsList in new[] { false, true })
            {
                try
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                        { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                        { "data", new BsonDocument { { "failCommands", new BsonArray { "find" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                    });
                    if (detailsList)
                    {
                        var listFailure = synchronous ? failureMongo.LoadAllAvatarDetails() : await failureMongo.LoadAllAvatarDetailsAsync();
                        listFailure.IsError.Should().BeTrue();
                        listFailure.Exception.Should().BeOfType<MongoCommandException>();
                        listFailure.Result.Should().BeNull();
                    }
                    else
                    {
                        var listFailure = synchronous ? failureMongo.LoadAllAvatars() : await failureMongo.LoadAllAvatarsAsync();
                        listFailure.IsError.Should().BeTrue();
                        listFailure.Exception.Should().BeOfType<MongoCommandException>();
                        listFailure.Result.Should().BeNull();
                    }
                }
                finally
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                }
            }
            foreach (var detailSelector in new[] { "id", "username", "email" })
            {
                try
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                        { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                        { "data", new BsonDocument { { "failCommands", new BsonArray { "find" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                    });
                    var detailFailure = detailSelector == "id"
                        ? (synchronous ? failureMongo.LoadAvatarDetail(detail.Id) : await failureMongo.LoadAvatarDetailAsync(detail.Id))
                        : detailSelector == "username"
                            ? (synchronous ? failureMongo.LoadAvatarDetailByUsername(detail.Username) : await failureMongo.LoadAvatarDetailByUsernameAsync(detail.Username))
                            : (synchronous ? failureMongo.LoadAvatarDetailByEmail(detail.Email) : await failureMongo.LoadAvatarDetailByEmailAsync(detail.Email));
                    detailFailure.IsError.Should().BeTrue();
                    detailFailure.Exception.Should().BeOfType<MongoCommandException>();
                    detailFailure.Result.Should().BeNull();
                }
                finally
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                }
            }
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "find" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var keyFailure = synchronous ? failureMongo.LoadAvatarByProviderKey(avatar.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS])
                    : await failureMongo.LoadAvatarByProviderKeyAsync(avatar.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]);
                keyFailure.IsError.Should().BeTrue();
                keyFailure.Exception.Should().BeOfType<MongoCommandException>();
                keyFailure.Result.Should().BeNull();
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            }
            foreach (var selector in new[] { "email", "username", "key" })
            {
                try
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                        { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                        { "data", new BsonDocument { { "failCommands", new BsonArray { "find" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                    });
                    var selectorFailure = selector == "email"
                        ? (synchronous ? failureMongo.DeleteAvatarByEmail(avatar.Email, false) : await failureMongo.DeleteAvatarByEmailAsync(avatar.Email, false))
                        : selector == "username"
                            ? (synchronous ? failureMongo.DeleteAvatarByUsername(avatar.Username, false) : await failureMongo.DeleteAvatarByUsernameAsync(avatar.Username, false))
                            : (synchronous ? failureMongo.DeleteAvatar(avatar.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS], false) : await failureMongo.DeleteAvatarAsync(avatar.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS], false));
                    selectorFailure.IsError.Should().BeTrue();
                    selectorFailure.Result.Should().BeFalse();
                    selectorFailure.Exception.Should().BeOfType<MongoCommandException>();
                    (await mongo.LoadAvatarAsync(avatar.Id)).Result.Should().NotBeNull();
                    (await mongo.LoadAvatarDetailAsync(detail.Id)).Result.Karma.Should().Be(456);
                }
                finally
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                }
            }
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "find" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var failRouter = new OASISHyperDrive(failManager);
                var detailRead = new StorageOperationRequest { Operation = "LoadAvatarDetail", AvatarId = detail.Id };
                var recovered = synchronous ? failRouter.RouteRequest<IAvatarDetail>(detailRead) : await failRouter.RouteRequestAsync<IAvatarDetail>(detailRead);
                recovered.IsError.Should().BeFalse(recovered.Message);
                recovered.Result.Karma.Should().Be(456);
                failManager.LastFailoverDiagnostic.SelectedProvider.Should().Be(ProviderType.SQLLiteDBOASIS);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                await failureMongo.DeActivateProviderAsync();
            }
            (await failureMongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            var partialManager = new ProviderManager(null,dna) { IsAutoReplicationEnabled = true };
            partialManager.RegisterProvider(failureMongo);
            partialManager.RegisterProvider(sqlite);
            partialManager.SetAndReplaceAutoReplicationListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS), new EnumValue<ProviderType>(ProviderType.MongoDBOASIS) }).IsError.Should().BeFalse();
            var primaryOnly = new Holon { Id = Guid.NewGuid(), Name = "mongo-replica-rejected" };
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "insert" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var partialRouter = new OASISHyperDrive(partialManager);
                var partialRequest = new StorageOperationRequest { Operation = "SaveHolon", Payload = primaryOnly, SaveChildren = false, PreferredProvider = ProviderType.SQLLiteDBOASIS };
                var partial = synchronous ? partialRouter.RouteRequest<IHolon>(partialRequest) : await partialRouter.RouteRequestAsync<IHolon>(partialRequest);
                partial.IsError.Should().BeFalse(partial.Message);
                partial.IsWarning.Should().BeTrue();
                partialManager.LastReplicationDiagnostic.FailedCount.Should().Be(1);
                partialManager.LastReplicationDiagnostic.SucceededCount.Should().Be(0);
                (await sqlite.LoadHolonAsync(primaryOnly.Id,loadChildren:false)).Result.Name.Should().Be(primaryOnly.Name);
                (await mongo.LoadHolonAsync(primaryOnly.Id,loadChildren:false)).Result.Should().BeNull();
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                await failureMongo.DeActivateProviderAsync();
            }
            foreach (var operation in new[] { "DeleteAvatarDetail", "DeleteAvatarDetailByUsername", "DeleteAvatarDetailByEmail" })
            foreach (var softDelete in new[] { false, true })
            {
                var standaloneAvatar = new Avatar { Id = Guid.NewGuid(), Username = Guid.NewGuid().ToString(), Email = Guid.NewGuid() + "@standalone.invalid",
                    AvatarType = new EnumValue<AvatarType>(AvatarType.User) };
                var standaloneDetail = new AvatarDetail { Id = standaloneAvatar.Id, Username = standaloneAvatar.Username, Email = standaloneAvatar.Email, Karma = 123,
                    Inventory = new List<IInventoryItem> { new InventoryItem { Id = Guid.NewGuid(), Name = "standalone-owned-seed", Quantity = 3 } } };
                standaloneDetail.Stats.HP.Current = 37;
                var saveIdentity = new StorageOperationRequest { Operation = "SaveAvatar", Payload = standaloneAvatar, PreferredProvider = request.PreferredProvider };
                var savedIdentity = synchronous ? router.RouteRequest<IAvatar>(saveIdentity) : await router.RouteRequestAsync<IAvatar>(saveIdentity);
                savedIdentity.IsError.Should().BeFalse(savedIdentity.Message);
                savedIdentity.IsWarning.Should().BeFalse();
                var saveDetail = new StorageOperationRequest { Operation = "SaveAvatarDetail", Payload = standaloneDetail, PreferredProvider = request.PreferredProvider };
                var savedDetail = synchronous ? router.RouteRequest<IAvatarDetail>(saveDetail) : await router.RouteRequestAsync<IAvatarDetail>(saveDetail);
                savedDetail.IsError.Should().BeFalse(savedDetail.Message);
                savedDetail.IsWarning.Should().BeFalse();
                var deleteDetail = new StorageOperationRequest { Operation = operation, AvatarId = standaloneAvatar.Id,
                    Username = standaloneAvatar.Username, Email = standaloneAvatar.Email, SoftDelete = softDelete, PreferredProvider = request.PreferredProvider };
                var deletedDetail = synchronous ? router.RouteRequest<bool>(deleteDetail) : await router.RouteRequestAsync<bool>(deleteDetail);
                deletedDetail.IsError.Should().BeFalse(deletedDetail.Message);
                deletedDetail.IsWarning.Should().BeFalse(string.Join("; ",manager.LastReplicationDiagnostic.Attempts.Select(x=>x.Message)));
                deletedDetail.Result.Should().BeTrue();
                manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
                foreach (var provider in new IOASISStorageProvider[] { mongo, sqlite }) {
                    (await provider.LoadAvatarAsync(standaloneAvatar.Id)).Result.DeletedDate.Should().Be(DateTime.MinValue);
                    var loaded = (await provider.LoadAvatarDetailAsync(standaloneAvatar.Id)).Result;
                    if (!softDelete) loaded.Should().BeNull();
                    else {
                        loaded.IsActive.Should().BeFalse();
                        loaded.DeletedDate.Should().NotBe(DateTime.MinValue);
                        loaded.Inventory.Single().Name.Should().Be("standalone-owned-seed");
                        loaded.Stats.HP.Current.Should().Be(37);
                    }
                    (await provider.LoadAvatarDetailAsync(detail.Id)).Result.Karma.Should().Be(456);
                }
            }
            var avatarDelete = new StorageOperationRequest { Operation = "DeleteAvatar", AvatarId = avatar.Id, SoftDelete = false, PreferredProvider = request.PreferredProvider };
            (await failureMongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            foreach (var softDelete in new[] { false, true })
            {
                try
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                        { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                        { "data", new BsonDocument { { "failCommands", new BsonArray { softDelete ? "update" : "delete" } },
                            { "appName", failureApp }, { "errorCode", 2 } } }
                    });
                    IOASISStorageProvider failingStore = failureMongo;
                    var rejectedDetail = synchronous ? failingStore.DeleteAvatarDetail(detail.Id, softDelete)
                        : await failingStore.DeleteAvatarDetailAsync(detail.Id, softDelete);
                    rejectedDetail.IsError.Should().BeTrue();
                    rejectedDetail.Result.Should().BeFalse();
                    rejectedDetail.Exception.Should().BeOfType<MongoCommandException>();
                    (await mongo.LoadAvatarAsync(avatar.Id)).Result.DeletedDate.Should().Be(DateTime.MinValue);
                    var retainedDetail = (await mongo.LoadAvatarDetailAsync(detail.Id)).Result;
                    retainedDetail.DeletedDate.Should().Be(DateTime.MinValue);
                    retainedDetail.Karma.Should().Be(456);
                    retainedDetail.Inventory.Single().Name.Should().Be(item.Name);
                    retainedDetail.Stats.HP.Current.Should().Be(detail.Stats.HP.Current);
                }
                finally
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                }
            }
            (await failureMongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("skip",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "delete" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var rejectedDelete = synchronous ? failureMongo.DeleteAvatar(avatar.Id, false) : await failureMongo.DeleteAvatarAsync(avatar.Id, false);
                rejectedDelete.IsError.Should().BeTrue();
                rejectedDelete.Result.Should().BeFalse();
                rejectedDelete.Exception.Should().NotBeNull();
                (await mongo.LoadAvatarAsync(avatar.Id)).Result.Should().NotBeNull();
                (await mongo.LoadAvatarDetailAsync(detail.Id)).Result.Karma.Should().Be(456);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                await failureMongo.DeActivateProviderAsync();
            }
            (await failureMongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            API.Providers.MongoDBOASIS.MongoTransactionReconciliationException commitEvidence = null;
            foreach (var softDelete in new[] { false, true })
            {
                var failoverDetail = new AvatarDetail { Id = Guid.NewGuid(), Username = Guid.NewGuid().ToString(), Email = Guid.NewGuid() + "@detail-failover.invalid", Karma = 123 };
                var saveFailoverDetail = new StorageOperationRequest { Operation = "SaveAvatarDetail", Payload = failoverDetail, PreferredProvider = request.PreferredProvider };
                var saved = synchronous ? router.RouteRequest<IAvatarDetail>(saveFailoverDetail) : await router.RouteRequestAsync<IAvatarDetail>(saveFailoverDetail);
                saved.IsError.Should().BeFalse(saved.Message);
                saved.IsWarning.Should().BeFalse();
                try
                {
                    var detailFailoverManager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true, IsAutoReplicationEnabled = true };
                    detailFailoverManager.RegisterProvider(failureMongo);
                    detailFailoverManager.RegisterProvider(sqlite);
                    detailFailoverManager.SetAndReplaceAutoReplicationListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError.Should().BeFalse();
                    detailFailoverManager.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError.Should().BeFalse();
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                        { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                        { "data", new BsonDocument { { "failCommands", new BsonArray { softDelete ? "update" : "delete" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                    });
                    var deleteFailoverDetail = new StorageOperationRequest { Operation = "DeleteAvatarDetail", AvatarId = failoverDetail.Id,
                        SoftDelete = softDelete, PreferredProvider = ProviderType.MongoDBOASIS };
                    var detailFailoverRouter = new OASISHyperDrive(detailFailoverManager);
                    var recovered = synchronous ? detailFailoverRouter.RouteRequest<bool>(deleteFailoverDetail)
                        : await detailFailoverRouter.RouteRequestAsync<bool>(deleteFailoverDetail);
                    recovered.IsError.Should().BeFalse(recovered.Message);
                    recovered.Result.Should().BeTrue();
                    recovered.IsWarning.Should().BeFalse();
                    detailFailoverManager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
                    detailFailoverManager.LastFailoverDiagnostic.SelectedProvider.Should().Be(ProviderType.SQLLiteDBOASIS);
                    var primary = (await mongo.LoadAvatarDetailAsync(failoverDetail.Id)).Result;
                    if (softDelete) { primary.DeletedDate.Should().NotBe(DateTime.MinValue); primary.Karma.Should().Be(123); }
                    else primary.Should().BeNull();
                    var secondary = (await sqlite.LoadAvatarDetailAsync(failoverDetail.Id)).Result;
                    if (softDelete) { secondary.DeletedDate.Should().NotBe(DateTime.MinValue); secondary.IsActive.Should().BeFalse(); }
                    else secondary.Should().BeNull();
                }
                finally
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                }
            }
            foreach (var softDelete in new[] { false, true })
            {
                API.Providers.MongoDBOASIS.MongoTransactionReconciliationException detailCommitEvidence = null;
                var sqliteFailoverDetail = new AvatarDetail { Id = Guid.NewGuid(), Username = Guid.NewGuid().ToString(), Karma = 123 };
                var saveSqliteFailoverDetail = new StorageOperationRequest { Operation = "SaveAvatarDetail", Payload = sqliteFailoverDetail, PreferredProvider = request.PreferredProvider };
                var savedSqliteProbe = synchronous ? router.RouteRequest<IAvatarDetail>(saveSqliteFailoverDetail) : await router.RouteRequestAsync<IAvatarDetail>(saveSqliteFailoverDetail);
                savedSqliteProbe.IsError.Should().BeFalse(savedSqliteProbe.Message);
                savedSqliteProbe.IsWarning.Should().BeFalse();
                using (var control = new DataContext($"Data Source={dbPath};Pooling=False"))
                {
                    if (softDelete) control.Database.ExecuteSqlRaw("CREATE TRIGGER reject_owned_v2_detail BEFORE UPDATE ON AvatarDetail BEGIN SELECT RAISE(ABORT, 'owned V2 detail rejection'); END;");
                    else control.Database.ExecuteSqlRaw("CREATE TRIGGER reject_owned_v2_detail BEFORE DELETE ON AvatarDetail BEGIN SELECT RAISE(ABORT, 'owned V2 detail rejection'); END;");
                    try
                    {
                        var reverseManager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true, IsAutoReplicationEnabled = true };
                        reverseManager.RegisterProvider(sqlite);
                        reverseManager.RegisterProvider(mongo);
                        reverseManager.SetAndReplaceAutoReplicationListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS), new EnumValue<ProviderType>(ProviderType.MongoDBOASIS) }).IsError.Should().BeFalse();
                        reverseManager.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS), new EnumValue<ProviderType>(ProviderType.MongoDBOASIS) }).IsError.Should().BeFalse();
                        var reverseRequest = new StorageOperationRequest { Operation = "DeleteAvatarDetail", AvatarId = sqliteFailoverDetail.Id,
                            SoftDelete = softDelete, PreferredProvider = ProviderType.SQLLiteDBOASIS };
                        var reverseRouter = new OASISHyperDrive(reverseManager);
                        var recovered = synchronous ? reverseRouter.RouteRequest<bool>(reverseRequest) : await reverseRouter.RouteRequestAsync<bool>(reverseRequest);
                        recovered.IsError.Should().BeFalse(recovered.Message);
                        recovered.Result.Should().BeTrue();
                        recovered.IsWarning.Should().BeTrue();
                        reverseManager.LastReplicationDiagnostic.FailedCount.Should().Be(1);
                        reverseManager.LastReplicationDiagnostic.SucceededCount.Should().Be(0);
                        reverseManager.PerformanceMonitor.GetMetrics(ProviderType.MongoDBOASIS).TotalRequests.Should().Be(1);
                        reverseManager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS).TotalRequests.Should().Be(2);
                        reverseManager.LastFailoverDiagnostic.SelectedProvider.Should().Be(ProviderType.MongoDBOASIS);
                        var retained = (await sqlite.LoadAvatarDetailAsync(sqliteFailoverDetail.Id)).Result;
                        retained.DeletedDate.Should().Be(DateTime.MinValue);
                        retained.Karma.Should().Be(123);
                        var changed = (await mongo.LoadAvatarDetailAsync(sqliteFailoverDetail.Id)).Result;
                        if (softDelete) { changed.DeletedDate.Should().NotBe(DateTime.MinValue); changed.IsActive.Should().BeFalse(); }
                        else changed.Should().BeNull();
                    }
                    finally { control.Database.ExecuteSqlRaw("DROP TRIGGER reject_owned_v2_detail;"); }
                }
                try
                {
                    var detailCommitManager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true, IsAutoReplicationEnabled = true };
                    detailCommitManager.RegisterProvider(failureMongo);
                    detailCommitManager.RegisterProvider(sqlite);
                    detailCommitManager.SetAndReplaceAutoReplicationListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError.Should().BeFalse();
                    detailCommitManager.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError.Should().BeFalse();
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                        { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                        { "data", new BsonDocument { { "failCommands", new BsonArray { "commitTransaction" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                    });
                    var detailCommitRequest = new StorageOperationRequest { Operation = "DeleteAvatarDetail", AvatarId = detail.Id,
                        SoftDelete = softDelete, PreferredProvider = ProviderType.MongoDBOASIS };
                    var detailCommitRouter = new OASISHyperDrive(detailCommitManager);
                    var rejected = synchronous ? detailCommitRouter.RouteRequest<bool>(detailCommitRequest)
                        : await detailCommitRouter.RouteRequestAsync<bool>(detailCommitRequest);
                    rejected.IsError.Should().BeTrue();
                    rejected.Result.Should().BeFalse();
                    detailCommitEvidence = rejected.Exception as API.Providers.MongoDBOASIS.MongoTransactionReconciliationException;
                    detailCommitEvidence.Should().NotBeNull();
                    detailCommitEvidence.TransactionNumber.Should().BeGreaterThanOrEqualTo(0);
                    detailCommitEvidence.InnerException.Should().BeOfType<MongoCommandException>();
                    (detailCommitManager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0).Should().Be(0);
                    foreach (var provider in new IOASISStorageProvider[] { mongo, sqlite }) {
                        (await provider.LoadAvatarAsync(avatar.Id)).Result.DeletedDate.Should().Be(DateTime.MinValue);
                        var retained = (await provider.LoadAvatarDetailAsync(detail.Id)).Result;
                        retained.DeletedDate.Should().Be(DateTime.MinValue);
                        retained.Karma.Should().Be(456);
                        retained.Inventory.Single().Name.Should().Be(item.Name);
                    }
                }
                finally
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                    if (detailCommitEvidence != null)
                        await admin.RunCommandAsync<BsonDocument>(new BsonDocument("killSessions", new BsonArray { BsonDocument.Parse(detailCommitEvidence.SessionId) }));
                }
            }
            try
            {
                var commitManager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true };
                commitManager.RegisterProvider(failureMongo);
                commitManager.RegisterProvider(sqlite);
                commitManager.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError.Should().BeFalse();
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "commitTransaction" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var commitRequest = new StorageOperationRequest { Operation = "DeleteAvatar", AvatarId = avatar.Id, SoftDelete = false, PreferredProvider = ProviderType.MongoDBOASIS };
                var commitRouter = new OASISHyperDrive(commitManager);
                var rejectedCommit = synchronous ? commitRouter.RouteRequest<bool>(commitRequest) : await commitRouter.RouteRequestAsync<bool>(commitRequest);
                commitEvidence = rejectedCommit.Exception as API.Providers.MongoDBOASIS.MongoTransactionReconciliationException;
                rejectedCommit.IsError.Should().BeTrue();
                rejectedCommit.Result.Should().BeFalse();
                commitEvidence.Should().NotBeNull();
                commitEvidence.TransactionNumber.Should().BeGreaterThanOrEqualTo(0);
                commitEvidence.InnerException.Should().NotBeNull();
                (await sqlite.LoadAvatarAsync(avatar.Id)).Result.Should().NotBeNull();
                (await sqlite.LoadAvatarDetailAsync(detail.Id)).Result.Karma.Should().Be(456);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                if (commitEvidence != null)
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument("killSessions", new BsonArray { BsonDocument.Parse(commitEvidence.SessionId) }));
                await failureMongo.DeActivateProviderAsync();
            }
            (await mongo.LoadAvatarAsync(avatar.Id)).Result.Should().NotBeNull();
            (await mongo.LoadAvatarDetailAsync(detail.Id)).Result.Karma.Should().Be(456);
            var avatarDeleted = synchronous ? router.RouteRequest<bool>(avatarDelete) : await router.RouteRequestAsync<bool>(avatarDelete);
            avatarDeleted.IsError.Should().BeFalse(avatarDeleted.Message);
            avatarDeleted.IsWarning.Should().BeFalse(string.Join("; ",manager.LastReplicationDiagnostic.Attempts.Select(x=>x.Message)));
            (await mongo.LoadAvatarAsync(avatar.Id)).Result.Should().BeNull();
            (await sqlite.LoadAvatarAsync(avatar.Id)).Result.Should().BeNull();
            (await mongo.LoadAvatarDetailAsync(detail.Id)).Result.Should().BeNull();
            (await sqlite.LoadAvatarDetailAsync(detail.Id)).Result.Should().BeNull();
            using (var control = new DataContext($"Data Source={dbPath};Pooling=False")) control.Database.ExecuteSqlRaw("DROP TABLE Holon;");
            manager.IsAutoReplicationEnabled = false;
            manager.IsAutoFailOverEnabled = true;
            manager.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS), new EnumValue<ProviderType>(ProviderType.MongoDBOASIS) }).IsError.Should().BeFalse();
            manager.SetAndActivateCurrentStorageProvider(sqlite).IsError.Should().BeFalse();
            var fallbackHolon = new Holon { Id = Guid.NewGuid(), Name = "real-mongo-fallback" };
            var fallbackRequest = new StorageOperationRequest { Operation = "SaveHolon", Payload = fallbackHolon, SaveChildren = false };
            var fallback = synchronous ? router.RouteRequest<IHolon>(fallbackRequest) : await router.RouteRequestAsync<IHolon>(fallbackRequest);
            fallback.IsError.Should().BeFalse(fallback.Message);
            (await mongo.LoadHolonAsync(fallbackHolon.Id,loadChildren:false)).Result.Name.Should().Be(fallbackHolon.Name);
            manager.LastFailoverDiagnostic.SelectedProvider.Should().Be(ProviderType.MongoDBOASIS);
            var searchFallbackRequest = new StorageOperationRequest { Operation = "Search", LoadChildren = false,
                PreferredProvider = ProviderType.SQLLiteDBOASIS, SearchParams = new SearchParams { SearchOnlyForCurrentAvatar = false,
                    SearchGroups = new() { new SearchTextGroup { SearchQuery = "real-mongo-fallback", SearchHolons = true,
                        HolonType = fallbackHolon.HolonType, HolonSearchParams = new SearchHolonParams { Name = true } } } } };
            var searchFallback = synchronous ? router.RouteRequest<ISearchResults>(searchFallbackRequest) : await router.RouteRequestAsync<ISearchResults>(searchFallbackRequest);
            searchFallback.IsError.Should().BeFalse(searchFallback.Message);
            searchFallback.Result.SearchResultHolons.Select(h => h.Id).Should().Equal(fallbackHolon.Id);
            manager.LastFailoverDiagnostic.SelectedProvider.Should().Be(ProviderType.MongoDBOASIS);
            await mongo.DeActivateProviderAsync();
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
            foreach (var path in new[] { dbPath, dbPath + "-wal", dbPath + "-shm" })
                if (File.Exists(path)) File.Delete(path);
        }
    }
}
