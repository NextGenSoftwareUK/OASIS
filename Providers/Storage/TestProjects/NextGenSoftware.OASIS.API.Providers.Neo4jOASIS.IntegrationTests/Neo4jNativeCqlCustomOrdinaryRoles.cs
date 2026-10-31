using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using OASIS.ProviderTests.Fixtures;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeCqlTests
{
    [DataTestMethod]
    [DataRow(false, false, false)]
    [DataRow(false, false, true)]
    [DataRow(false, true, false)]
    [DataRow(false, true, true)]
    [DataRow(true, false, false)]
    [DataRow(true, false, true)]
    [DataRow(true, true, false)]
    [DataRow(true, true, true)]
    public Task NativeOrdinaryCustomRoleOwnership(bool synchronous, bool batch, bool viaManager)
        => WithNativeGraph(async fixture =>
        {
            var root = (CqlRoleOwner)fixture.Root;
            var manager = GraphManager(fixture.Provider);
            var actor = Guid.NewGuid();
            await CqlCustomRoleGraph.VerifyOrdinaryAsync(manager, fixture.Provider, root, async () =>
            {
                await fixture.Record("before-custom-ordinary-role-write");
                fixture.Known = false;
                if (batch)
                {
                    var saved = viaManager
                        ? synchronous ? manager.SaveHolons(new IHolon[] { root }, actor, saveChildren: true, providerType: ProviderType.Neo4jOASIS)
                            : await manager.SaveHolonsAsync(new IHolon[] { root }, actor, saveChildren: true, providerType: ProviderType.Neo4jOASIS)
                        : synchronous ? fixture.Provider.SaveHolons(new IHolon[] { root }, saveChildren: true)
                            : await fixture.Provider.SaveHolonsAsync(new IHolon[] { root }, saveChildren: true);
                    fixture.Known = !saved.IsError;
                    if (saved.IsError || !saved.IsSaved) throw new InvalidDataException(saved.Message, saved.Exception);
                    return saved.Result.Single();
                }
                else
                {
                    var saved = viaManager
                        ? synchronous ? manager.SaveHolon(root, actor, saveChildren: true, providerType: ProviderType.Neo4jOASIS)
                            : await manager.SaveHolonAsync(root, actor, saveChildren: true, providerType: ProviderType.Neo4jOASIS)
                        : synchronous ? fixture.Provider.SaveHolon(root, saveChildren: true)
                            : await fixture.Provider.SaveHolonAsync(root, saveChildren: true);
                    fixture.Known = !saved.IsError;
                    if (saved.IsError || !saved.IsSaved) throw new InvalidDataException(saved.Message, saved.Exception);
                    return saved.Result;
                }
            });
        }, manifested: false, width: 3,
            configure: CqlCustomRoleGraph.ConfigureOrdinary, createRoot: CqlCustomRoleGraph.CreateRoot);
}
