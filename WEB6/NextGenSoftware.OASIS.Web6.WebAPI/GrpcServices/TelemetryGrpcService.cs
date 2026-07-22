using System;
using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.WebAPI.Controllers;
using NextGenSoftware.OASIS.Web6.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.Web6.WebAPI.GrpcServices
{
    public class TelemetryGrpcService : TelemetryService.TelemetryServiceBase
    {
        private static NextGenSoftware.OASIS.API.DNA.OASISDNA DNA => OASISBootLoader.OASISBootLoader.OASISDNA;

        public override Task<TelemetryHistoryReply> GetTelemetryHistory(HistoryRequest request, ServerCallContext context)
        {
            try
            {
                int limit = request.Limit > 0 ? Math.Min(request.Limit, 500) : 50;
                var events = TelemetryController._events.ToArray().TakeLast(limit).ToList();
                var reply = new TelemetryHistoryReply { IsError = false };
                foreach (var e in events)
                {
                    reply.Events.Add(new TelemetryEventProto
                    {
                        RequestId = e.RequestId ?? "",
                        Timestamp = Timestamp.FromDateTime(e.Timestamp.Kind == DateTimeKind.Utc ? e.Timestamp : e.Timestamp.ToUniversalTime()),
                        Provider = e.Provider ?? "",
                        Model = e.Model ?? "",
                        LatencyMs = e.LatencyMs,
                        PromptTokens = e.PromptTokens,
                        CompletionTokens = e.CompletionTokens,
                        EstimatedCostUsd = e.EstimatedCostUSD,
                        FahrnMode = e.FahrnMode ?? "",
                        BraidGraphReused = e.BraidGraphReused,
                        BraidGraphId = e.BraidGraphId ?? "",
                        AgentsScored = e.AgentsScored,
                        WinningAgent = e.WinningAgent ?? "",
                        AvatarContextInjected = e.AvatarContextInjected,
                        CacheHit = e.CacheHit,
                        LoopDetected = e.LoopDetected,
                        AvatarId = e.AvatarId.ToString()
                    });
                }
                return Task.FromResult(reply);
            }
            catch (Exception ex)
            {
                return Task.FromResult(new TelemetryHistoryReply { IsError = true, Message = ex.Message });
            }
        }

        public override async Task<UsageReply> GetUsage(EmptyRequest request, ServerCallContext context)
        {
            try
            {
                // AvatarId is not available without a JWT in gRPC — callers must pass it via metadata if needed.
                // We read it from gRPC call metadata (header "avatarid") if present.
                string avatarIdHeader = context.RequestHeaders.GetValue("avatarid") ?? "";
                if (!Guid.TryParse(avatarIdHeader, out var avatarId) || avatarId == Guid.Empty)
                    return new UsageReply { IsError = true, Message = "AvatarId required (pass grpc metadata header 'avatarid')" };

                var metering = new UsageMeteringManager(avatarId, DNA);
                var result = await metering.GetUsageSummaryAsync();
                if (result.IsError) return new UsageReply { IsError = true, Message = result.Message };
                return new UsageReply
                {
                    IsError = false,
                    AvatarId = avatarId.ToString(),
                    TotalCostUsd = result.Result?.MonthlySpendUSD ?? 0,
                    TotalTokensToday = result.Result?.DailyTokensUsed ?? 0,
                    DailyTokenLimit = result.Result?.DailyTokenLimit ?? 0,
                    MonthlyBudgetUsd = result.Result?.MonthlyBudgetUSD ?? 0
                };
            }
            catch (Exception ex) { return new UsageReply { IsError = true, Message = ex.Message }; }
        }
    }
}
