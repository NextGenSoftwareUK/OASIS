using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.IntegrationTests;

/// <summary>
/// Tests that OASISRequestContext (AsyncLocal) correctly isolates concurrent users
/// and that the ONODE API handles load and concurrency without cross-talk or failures.
/// </summary>
public class MultiUserAndLoadTests
{
    private const int MultiUserConcurrencyCount = 50;
    private const int LoadTestConcurrencyCount = 100;
    private const int LoadTestRequestsPerTask = 10;

    [Fact]
    public async Task OASISRequestContext_CurrentAvatarId_Is_Isolated_Across_Concurrent_Tasks()
    {
        var ids = Enumerable.Range(0, MultiUserConcurrencyCount).Select(_ => Guid.NewGuid()).ToList();
        var results = new Guid?[MultiUserConcurrencyCount];

        var tasks = ids.Select((avatarId, index) => Task.Run(async () =>
        {
            NextGenSoftware.OASIS.API.Core.OASISRequestContext.CurrentAvatarId = avatarId;
            var avatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = avatarId, Username = $"user_{index}" };
            NextGenSoftware.OASIS.API.Core.OASISRequestContext.CurrentAvatar = avatar;
            await Task.Yield();
            await Task.Delay(1);
            results[index] = NextGenSoftware.OASIS.API.Core.OASISRequestContext.CurrentAvatarId;
            NextGenSoftware.OASIS.API.Core.OASISRequestContext.CurrentAvatarId = null;
            NextGenSoftware.OASIS.API.Core.OASISRequestContext.CurrentAvatar = null;
        }));

        await Task.WhenAll(tasks);

        for (var i = 0; i < ids.Count; i++)
            results[i].Should().Be(ids[i], "each concurrent flow must see only its own avatar id");
    }

    [Fact]
    public async Task OASISRequestContext_CurrentAvatar_Is_Isolated_Across_Concurrent_Tasks()
    {
        var ids = Enumerable.Range(0, MultiUserConcurrencyCount).Select(_ => Guid.NewGuid()).ToList();
        var results = new string[MultiUserConcurrencyCount];

        var tasks = ids.Select((avatarId, index) => Task.Run(async () =>
        {
            var username = $"user_{index}_{avatarId:N}";
            var avatar = new NextGenSoftware.OASIS.API.Core.Holons.Avatar { Id = avatarId, Username = username };
            NextGenSoftware.OASIS.API.Core.OASISRequestContext.CurrentAvatar = avatar;
            NextGenSoftware.OASIS.API.Core.OASISRequestContext.CurrentAvatarId = avatarId;
            await Task.Yield();
            await Task.Delay(1);
            results[index] = NextGenSoftware.OASIS.API.Core.OASISRequestContext.CurrentAvatar?.Username ?? "";
            NextGenSoftware.OASIS.API.Core.OASISRequestContext.CurrentAvatar = null;
            NextGenSoftware.OASIS.API.Core.OASISRequestContext.CurrentAvatarId = null;
        }));

        await Task.WhenAll(tasks);

        for (var i = 0; i < ids.Count; i++)
            results[i].Should().StartWith($"user_{i}_", "each concurrent flow must see only its own avatar");
    }

    [Fact]
    public async Task ONODE_API_Load_Concurrent_Http_Requests_Completes_Without_Exception()
    {
        await using var factory = new ONODEWebAPIApplicationFactory();
        var client = factory.CreateClient();
        var exceptions = new List<Exception>();
        var completed = 0;

        var tasks = Enumerable.Range(0, LoadTestConcurrencyCount).Select(_ =>
            Task.Run(async () =>
            {
                try
                {
                    for (var i = 0; i < LoadTestRequestsPerTask; i++)
                    {
                        // Endpoint that may return 401 when unauthorized - we only assert no server crash
                        var response = await client.GetAsync("api/stats/get-stats-for-current-logged-in-avatar");
                        response.Should().NotBeNull();
                    }
                    Interlocked.Increment(ref completed);
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                }
            }));

        await Task.WhenAll(tasks);

        exceptions.Should().BeEmpty("concurrent HTTP requests should not throw");
        completed.Should().Be(LoadTestConcurrencyCount);
    }

    [Fact]
    public async Task ONODE_API_Load_Many_Concurrent_Get_Requests_Succeeds()
    {
        await using var factory = new ONODEWebAPIApplicationFactory();
        var client = factory.CreateClient();
        const int totalRequests = 200;
        var responses = new HttpResponseMessage[totalRequests];

        var tasks = Enumerable.Range(0, totalRequests).Select(async i =>
        {
            try
            {
                responses[i] = await client.GetAsync("api/v1/onode/oasisdna");
            }
            catch (Exception)
            {
                responses[i] = null!;
            }
        });

        await Task.WhenAll(tasks);

        var completed = responses.Count(r => r != null);
        completed.Should().Be(totalRequests, "every request should complete with a response (no connection/server crash)");
    }
}
