using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using OASIS.ProviderTests.Fixtures;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task NativeOrdinaryCustomRoleOwnership(bool synchronous, bool batch, bool viaManager)
    {
        var root = (CqlRoleOwner)CqlCustomRoleGraph.CreateRoot();
        for (var index = 0; index < 3; index++)
            root.Children.Add(new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id,
                Name = "owned custom role child " + index });
        CqlCustomRoleGraph.ConfigureOrdinary(root);
        var intent = await BeginOrdinaryRoleIntent(root);
        using (var provider = new SQLLiteDBOASIS(TestConnectionString))
        {
            var activation = await provider.ActivateProviderAsync();
            Assert.False(activation.IsError, activation.Message);
            var manager = CosmicManager(provider, true);
            var actor = Guid.NewGuid();
            await CqlCustomRoleGraph.VerifyOrdinaryAsync(manager, provider, root, async () =>
            {
                if (batch)
                {
                    var saved = viaManager
                        ? synchronous ? manager.SaveHolons(new IHolon[] { root }, actor, saveChildren: true, providerType: ProviderType.SQLLiteDBOASIS)
                            : await manager.SaveHolonsAsync(new IHolon[] { root }, actor, saveChildren: true, providerType: ProviderType.SQLLiteDBOASIS)
                        : synchronous ? provider.SaveHolons(new IHolon[] { root }, saveChildren: true)
                            : await provider.SaveHolonsAsync(new IHolon[] { root }, saveChildren: true);
                    if (saved.IsError || !saved.IsSaved) throw new InvalidDataException(saved.Message, saved.Exception);
                    return saved.Result.Single();
                }
                else
                {
                    var saved = viaManager
                        ? synchronous ? manager.SaveHolon(root, actor, saveChildren: true, providerType: ProviderType.SQLLiteDBOASIS)
                            : await manager.SaveHolonAsync(root, actor, saveChildren: true, providerType: ProviderType.SQLLiteDBOASIS)
                        : synchronous ? provider.SaveHolon(root, saveChildren: true)
                            : await provider.SaveHolonAsync(root, saveChildren: true);
                    if (saved.IsError || !saved.IsSaved) throw new InvalidDataException(saved.Message, saved.Exception);
                    return saved.Result;
                }
            });
        }
        await EndOrdinaryRoleIntent(intent);
    }
}