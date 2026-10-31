using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using OASIS.ProviderTests.Fixtures;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicCqlNativeTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public Task NativeOrdinaryCustomRoleOwnership(bool synchronous, bool batch, bool viaManager)
        => WithMongoGraph(async fixture =>
        {
            var root = (CqlRoleOwner)fixture.Root;
            var manager = Manager(fixture.Provider);
            var actor = Guid.NewGuid();
            await CqlCustomRoleGraph.VerifyOrdinaryAsync(manager, fixture.Provider, root, async () =>
            {
                await fixture.Database.GetCollection<BsonDocument>("CosmicOwnedFixture").InsertOneAsync(
                    new BsonDocument { { "customOrdinaryRoleWrite", true }, { "root", root.Id.ToString() },
                        { "synchronous", synchronous }, { "batch", batch }, { "manager", viaManager } });
                fixture.KnownOutcome = false;
                if (batch)
                {
                    var saved = viaManager
                        ? synchronous ? manager.SaveHolons(new IHolon[] { root }, actor, saveChildren: true, providerType: ProviderType.MongoDBOASIS)
                            : await manager.SaveHolonsAsync(new IHolon[] { root }, actor, saveChildren: true, providerType: ProviderType.MongoDBOASIS)
                        : synchronous ? fixture.Provider.SaveHolons(new IHolon[] { root }, saveChildren: true)
                            : await fixture.Provider.SaveHolonsAsync(new IHolon[] { root }, saveChildren: true);
                    fixture.KnownOutcome = !saved.IsError;
                    if (saved.IsError || !saved.IsSaved) throw new InvalidDataException(saved.Message, saved.Exception);
                    return saved.Result.Single();
                }
                else
                {
                    var saved = viaManager
                        ? synchronous ? manager.SaveHolon(root, actor, saveChildren: true, providerType: ProviderType.MongoDBOASIS)
                            : await manager.SaveHolonAsync(root, actor, saveChildren: true, providerType: ProviderType.MongoDBOASIS)
                        : synchronous ? fixture.Provider.SaveHolon(root, saveChildren: true)
                            : await fixture.Provider.SaveHolonAsync(root, saveChildren: true);
                    fixture.KnownOutcome = !saved.IsError;
                    if (saved.IsError || !saved.IsSaved) throw new InvalidDataException(saved.Message, saved.Exception);
                    return saved.Result;
                }
            });
        }, manifest: false, width: 3,
            configure: CqlCustomRoleGraph.ConfigureOrdinary, createRoot: CqlCustomRoleGraph.CreateRoot);
}
