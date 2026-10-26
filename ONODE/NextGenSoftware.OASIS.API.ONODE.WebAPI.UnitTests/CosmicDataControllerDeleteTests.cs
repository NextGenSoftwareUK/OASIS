using System.Net;
using System.Net.Http.Json;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed partial class CosmicDataControllerHttpTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task LogicalDeleteAliasesUseOneOwnedConditionalAuthority(int alias)
    {
        await WithHost(false, async (client, actor, provider) =>
        {
            var id = Guid.NewGuid();
            var revision = Guid.NewGuid();
            var operation = Guid.NewGuid();
            var stored = new Holon { Id = id, CreatedByAvatarId = actor };
            CosmicConcurrency.SetProviderRevisionId(stored, ProviderType.SQLLiteDBOASIS, revision);
            provider.Setup(value => value.LoadHolonAsync(id, false, true, 0, true, false, 0))
                .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
            var path = alias switch
            {
                0 => "/api/data/delete-holon",
                1 => $"/api/data/delete-holon/{id}",
                2 => $"/api/data/delete-holon/{id}/false",
                3 => $"/api/data/delete-holon/{id}/true/SQLLiteDBOASIS/false",
                _ => $"/api/data/delete-holon/{id}/true/SQLLiteDBOASIS/false/DEFAULT/DEFAULT/DEFAULT/DEFAULT/DEFAULT/DEFAULT/false/true"
            };
            using var request = new HttpRequestMessage(HttpMethod.Delete, path);
            request.Headers.Add(HolonWriteEndpoint.OperationHeader, operation.ToString("D"));
            request.Headers.Add(HolonWriteEndpoint.ExpectedRevisionHeader, revision.ToString("D"));
            if (alias == 0) request.Content = JsonContent.Create(new { id, softDelete = false });

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var write = Assert.Single(provider.Invocations.Where(call => call.Method.Name == "ExecuteConditionalHolonWriteAsync"));
            var submitted = Assert.IsType<ConditionalHolonWriteRequest>(write.Arguments[0]);
            Assert.Equal(actor, submitted.Write.ActorId);
            Assert.Equal(operation, submitted.Write.OperationId);
            Assert.Equal(revision, Assert.Single(submitted.Write.Conditions).ExpectedRevisionId);
            Assert.Equal(alias is 0 or 2 ? ConditionalHolonOperation.HardDelete : ConditionalHolonOperation.SoftDelete, submitted.Operation);
            Assert.DoesNotContain(provider.Invocations, call => call.Method.Name == "DeleteHolonAsync");
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeleteRejectsMissingOrDisagreeingOriginalRevisionBeforeStorage(bool disagreement)
    {
        await WithHost(false, async (client, actor, provider) =>
        {
            var id = Guid.NewGuid();
            using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/data/delete-holon")
            {
                Content = JsonContent.Create(new { id, operationId = Guid.NewGuid(),
                    expectedRevisions = disagreement ? new Dictionary<Guid, Guid?> { [id] = Guid.NewGuid() } : null })
            };
            if (disagreement) request.Headers.Add(HolonWriteEndpoint.ExpectedRevisionHeader, Guid.NewGuid().ToString("D"));

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.DoesNotContain(provider.Invocations, call => call.Method.Name.Contains("LoadHolon") ||
                call.Method.Name.Contains("DeleteHolon") || call.Method.Name.Contains("Conditional"));
        });
    }

    [Fact]
    public async Task DeleteCannotUseSubmittedIdentityOrRoleToBypassTheStoredCreator()
    {
        await WithHost(false, async (client, actor, provider) =>
        {
            var id = Guid.NewGuid();
            var revision = Guid.NewGuid();
            var stored = new Holon { Id = id, CreatedByAvatarId = Guid.NewGuid() };
            CosmicConcurrency.SetProviderRevisionId(stored, ProviderType.SQLLiteDBOASIS, revision);
            provider.Setup(value => value.LoadHolonAsync(id, false, true, 0, true, false, 0))
                .ReturnsAsync(new OASISResult<IHolon>(stored) { IsLoaded = true });
            using var request = new HttpRequestMessage(HttpMethod.Delete, "/api/data/delete-holon")
            {
                Content = JsonContent.Create(new { id, actorId = stored.CreatedByAvatarId, isWizard = true, operationId = Guid.NewGuid(),
                    expectedRevisions = new Dictionary<Guid, Guid?> { [id] = revision } })
            };

            using var response = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.DoesNotContain(provider.Invocations, call => call.Method.Name.Contains("Conditional") || call.Method.Name == "DeleteHolonAsync");
        });
    }
}
