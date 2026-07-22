using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Web6.Core.Enums;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Memory;
using NextGenSoftware.OASIS.Web6.Core.Models;
using NextGenSoftware.OASIS.Web6.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.Web6.WebAPI.GrpcServices
{
    public class MemoryGrpcService : MemoryService.MemoryServiceBase
    {
        private static NextGenSoftware.OASIS.API.DNA.OASISDNA DNA => OASISBootLoader.OASISBootLoader.OASISDNA;

        public override async Task<HolonReply> GetEarthHolon(EmptyRequest request, ServerCallContext context)
        {
            try
            {
                var manager = new HolonicMemoryManager(Guid.Empty, DNA);
                var result = await manager.GetOrCreateEarthHolonAsync();
                if (result.IsError) return new HolonReply { IsError = true, Message = result.Message };
                return MapHolon(result.Result);
            }
            catch (Exception ex) { return new HolonReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<HolonReply> GetOrCreateHolon(GetOrCreateHolonRequest request, ServerCallContext context)
        {
            try
            {
                Guid avatarId = Guid.TryParse(request.AvatarId, out var aid) ? aid : Guid.Empty;
                if (!System.Enum.TryParse<HolonicMemoryLevel>(request.Level, true, out var level))
                    level = HolonicMemoryLevel.User;
                Guid parentId = Guid.TryParse(request.ParentHolonId, out var pid) ? pid : Guid.Empty;
                var manager = new HolonicMemoryManager(avatarId, DNA);
                var result = await manager.GetOrCreateHolonAsync(level, request.Name, parentId);
                if (result.IsError) return new HolonReply { IsError = true, Message = result.Message };
                return MapHolon(result.Result);
            }
            catch (Exception ex) { return new HolonReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<BaseReply> SetMembraneRule(SetMembraneRuleRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.HolonId, out var holonId))
                    return new BaseReply { IsError = true, Message = "Invalid holonId GUID" };
                var rule = new MembraneRule { TriggerCondition = request.RuleType };
                var manager = new HolonicMemoryManager(Guid.Empty, DNA);
                var result = await manager.SetMembraneRuleAsync(holonId, rule);
                return result.IsError ? new BaseReply { IsError = true, Message = result.Message } : new BaseReply { IsError = false };
            }
            catch (Exception ex) { return new BaseReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<BaseReply> RecordMemory(RecordMemoryRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.HolonId, out var holonId))
                    return new BaseReply { IsError = true, Message = "Invalid holonId GUID" };
                var item = new HolonicMemoryItem
                {
                    FieldName = request.Item?.Category ?? "memory",
                    Value = request.Item?.Content ?? ""
                };
                if (!string.IsNullOrWhiteSpace(request.Item?.Category))
                    item.Tags.Add(request.Item.Category);
                var manager = new HolonicMemoryManager(Guid.Empty, DNA);
                var result = await manager.RecordMemoryAsync(holonId, item);
                return result.IsError ? new BaseReply { IsError = true, Message = result.Message } : new BaseReply { IsError = false };
            }
            catch (Exception ex) { return new BaseReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<BaseReply> PropagateUp(PropagateRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.ChildHolonId, out var childId))
                    return new BaseReply { IsError = true, Message = "Invalid childHolonId GUID" };
                var manager = new HolonicMemoryManager(Guid.Empty, DNA);
                var result = await manager.PropagateUpAsync(childId, request.Levels > 0 ? request.Levels : 1);
                return result.IsError ? new BaseReply { IsError = true, Message = result.Message } : new BaseReply { IsError = false };
            }
            catch (Exception ex) { return new BaseReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<BaseReply> Propagate(HolonIdRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.HolonId, out var holonId))
                    return new BaseReply { IsError = true, Message = "Invalid holonId GUID" };
                var manager = new HolonicMemoryManager(Guid.Empty, DNA);
                var result = await manager.PropagateAsync(holonId);
                return result.IsError ? new BaseReply { IsError = true, Message = result.Message } : new BaseReply { IsError = false };
            }
            catch (Exception ex) { return new BaseReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<SearchMemoryReply> SearchMemory(SearchMemoryRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.HolonId, out var holonId))
                    return new SearchMemoryReply { IsError = true, Message = "Invalid holonId GUID" };
                if (string.IsNullOrWhiteSpace(request.Query))
                    return new SearchMemoryReply { IsError = true, Message = "query is required" };
                var manager = new HolonicMemoryManager(Guid.Empty, DNA);
                var result = await manager.QueryMemoryAsync(holonId, request.Query, request.TopK > 0 ? request.TopK : 5, request.Provider ?? "auto");
                if (result.IsError) return new SearchMemoryReply { IsError = true, Message = result.Message };
                var reply = new SearchMemoryReply { IsError = false };
                foreach (var r in result.Result ?? new List<MemorySearchResult>())
                    reply.Results.Add(new MemorySearchResultProto { Content = r.Item?.Value ?? "", Score = r.Score, Category = r.Item?.Tags?.FirstOrDefault() ?? "" });
                return reply;
            }
            catch (Exception ex) { return new SearchMemoryReply { IsError = true, Message = ex.Message }; }
        }

        public override Task<StringListReply> ListExternalProviders(EmptyRequest request, ServerCallContext context)
        {
            try
            {
                var reply = new StringListReply { IsError = false };
                reply.Values.AddRange(MemoryProviderManager.Instance.ProviderNames);
                return Task.FromResult(reply);
            }
            catch (Exception ex) { return Task.FromResult(new StringListReply { IsError = true, Message = ex.Message }); }
        }

        public override async Task<ExternalSearchReply> SearchExternalMemory(ExternalSearchRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                    return new ExternalSearchReply { IsError = true, Message = "query is required" };
                Guid avatarId = Guid.TryParse(request.AvatarId, out var aid) ? aid : Guid.Empty;
                var providers = request.Providers?.Count > 0 ? request.Providers.ToList() : null;
                int topK = request.TopK > 0 ? request.TopK : 5;
                var results = await MemoryProviderManager.Instance.SearchAllAsync(avatarId, request.Query, providers, topK);
                var reply = new ExternalSearchReply { IsError = false };
                foreach (var r in results ?? new List<ExternalMemorySearchResult>())
                    reply.Results.Add(new ExternalMemoryResultProto { Provider = r.Provider ?? "", Content = r.Entry?.Content ?? "", Score = r.Entry?.Score ?? 0 });
                return reply;
            }
            catch (Exception ex) { return new ExternalSearchReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<BaseReply> AddExternalMemory(AddExternalMemoryRequest request, ServerCallContext context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Content))
                    return new BaseReply { IsError = true, Message = "content is required" };
                if (string.IsNullOrWhiteSpace(request.Provider))
                    return new BaseReply { IsError = true, Message = "provider is required" };
                Guid avatarId = Guid.TryParse(request.AvatarId, out var aid) ? aid : Guid.Empty;
                var provider = MemoryProviderManager.Instance.Get(request.Provider);
                if (provider == null)
                    return new BaseReply { IsError = true, Message = $"Provider '{request.Provider}' is not registered" };
                await provider.AddAsync(avatarId, request.Content, null);
                return new BaseReply { IsError = false };
            }
            catch (Exception ex) { return new BaseReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<BaseReply> DeleteExternalMemory(DeleteExternalMemoryRequest request, ServerCallContext context)
        {
            try
            {
                var p = MemoryProviderManager.Instance.Get(request.Provider);
                if (p == null)
                    return new BaseReply { IsError = true, Message = $"Provider '{request.Provider}' is not registered" };
                await p.DeleteAsync(Guid.Empty, request.Id);
                return new BaseReply { IsError = false };
            }
            catch (Exception ex) { return new BaseReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<AvatarContextReply> GetAvatarContext(AvatarIdRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AvatarId, out var avatarId))
                    return new AvatarContextReply { IsError = true, Message = "Invalid avatarId GUID" };
                var manager = new StarnetContextManager(avatarId, DNA);
                var result = await manager.GetAvatarContextAsync(avatarId, request.BearerToken ?? "");
                if (result.IsError) return new AvatarContextReply { IsError = true, Message = result.Message };
                return new AvatarContextReply
                {
                    IsError = false,
                    ContextBlock = result.Result?.DisplayName ?? "",
                    Karma = result.Result?.KarmaScore ?? 0
                };
            }
            catch (Exception ex) { return new AvatarContextReply { IsError = true, Message = ex.Message }; }
        }

        private static HolonReply MapHolon(HolonicMemoryHolonDto h)
        {
            if (h == null) return new HolonReply();
            return new HolonReply
            {
                IsError = false,
                HolonId = h.Id.ToString(),
                Name = h.Name ?? "",
                Level = h.Level.ToString(),
                ParentHolonId = h.ParentHolonId.ToString()
            };
        }
    }
}
