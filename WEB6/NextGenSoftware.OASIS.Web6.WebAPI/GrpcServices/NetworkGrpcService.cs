using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;
using NextGenSoftware.OASIS.Web6.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.Web6.WebAPI.GrpcServices
{
    public class NetworkGrpcService : NetworkService.NetworkServiceBase
    {
        private static NextGenSoftware.OASIS.API.DNA.OASISDNA DNA => OASISBootLoader.OASISBootLoader.OASISDNA;

        public override async Task<FahrnSolveGrpcReply> FahrnSolve(FahrnSolveGrpcRequest request, ServerCallContext context)
        {
            try
            {
                Guid avatarId = Guid.TryParse(request.AvatarId, out var aid) ? aid : Guid.Empty;
                var manager = new FahrnSolveManager(avatarId, DNA);
                var solveRequest = new FahrnSolveRequest
                {
                    Problem = request.Problem,
                    AvatarId = avatarId,
                    ReturnReasoning = request.ReturnReasoning,
                    TaskType = string.IsNullOrWhiteSpace(request.TaskType) ? null : request.TaskType
                };
                var result = await manager.SolveAsync(solveRequest);
                if (result.IsError) return new FahrnSolveGrpcReply { IsError = true, Message = result.Message };
                return new FahrnSolveGrpcReply
                {
                    IsError = false,
                    Answer = result.Result?.Answer ?? "",
                    MermaidPlan = result.Result?.MermaidPlan ?? "",
                    ModeUsed = result.Result?.ModeUsed.ToString() ?? "",
                    TotalLatencyMs = result.Result?.TotalLatencyMs ?? 0
                };
            }
            catch (Exception ex) { return new FahrnSolveGrpcReply { IsError = true, Message = ex.Message }; }
        }

        public override Task<BudgetEstimateReply> FahrnBudgetEstimate(BudgetEstimateRequest request, ServerCallContext context)
        {
            try
            {
                string taskType = string.IsNullOrWhiteSpace(request.TaskType) ? "general" : request.TaskType;
                string mode = string.IsNullOrWhiteSpace(request.Mode) ? "auto" : request.Mode;
                int agentCount = request.AgentCount > 0 ? request.AgentCount : 1;

                int estimatedTokensPerAgent = taskType.ToLowerInvariant() switch
                {
                    "mathematics" or "reasoning" => 2000,
                    "legal" or "architecture" => 3000,
                    "code" => 2500,
                    "real-time" => 500,
                    _ => 1500
                };

                int totalCalls = mode.ToLowerInvariant() switch
                {
                    "serial" => 1,
                    "debate" => Math.Min(agentCount, 3),
                    "decomposed" => Math.Min(agentCount, DNA?.OASIS?.Web6?.FAHRN?.MaxDecomposedSubProblems ?? 3),
                    _ => agentCount
                };

                int totalTokens = estimatedTokensPerAgent * totalCalls;
                double estimatedCost = Math.Round((totalTokens / 1000.0) * 0.0075, 4);

                return Task.FromResult(new BudgetEstimateReply
                {
                    IsError = false,
                    TaskType = taskType,
                    ModeAssumed = mode,
                    AgentCount = agentCount,
                    EstimatedTokensPerAgent = estimatedTokensPerAgent,
                    EstimatedTotalTokens = totalTokens,
                    EstimatedCostUsd = estimatedCost,
                    PricingNote = "Estimates use mid-tier model pricing. Actual cost varies by provider, model, and prompt length."
                });
            }
            catch (Exception ex) { return Task.FromResult(new BudgetEstimateReply { IsError = true, Message = ex.Message }); }
        }

        public override async Task<ProviderStatusReply> GetProviderStatus(ProviderStatusRequest request, ServerCallContext context)
        {
            try
            {
                var monitor = new ProviderHealthMonitor(Guid.Empty, DNA);
                var status = await monitor.GetStatusAsync(forceRefresh: request.Refresh);
                string json = JsonSerializer.Serialize(status);
                return new ProviderStatusReply { IsError = false, StatusJson = json };
            }
            catch (Exception ex) { return new ProviderStatusReply { IsError = true, Message = ex.Message }; }
        }

        public override Task<BaseReply> WebSocketSession(EmptyRequest request, ServerCallContext context)
        {
            return Task.FromResult(new BaseReply { IsError = true, Message = "Use WebSocket endpoint for streaming." });
        }

        public override Task<DiscoveryReply> GetMcpDiscovery(EmptyRequest request, ServerCallContext context)
        {
            var doc = new
            {
                protocol = "mcp",
                version = "2024-11-05",
                name = "OASIS Web6 AI/ML API",
                description = "OASIS Web6 MCP-compatible AI/ML and agent orchestration endpoint"
            };
            return Task.FromResult(new DiscoveryReply { Json = JsonSerializer.Serialize(doc) });
        }

        public override Task<DiscoveryReply> GetA2AAgentCard(EmptyRequest request, ServerCallContext context)
        {
            var card = new
            {
                name = "OASIS Web6 Agent",
                description = "OASIS Web6 AI orchestration agent supporting A2A protocol",
                version = "1.0.0",
                capabilities = new[] { "completion", "embedding", "a2a", "orchestration", "reasoning" },
                a2aEndpoint = "/api/a2a"
            };
            return Task.FromResult(new DiscoveryReply { Json = JsonSerializer.Serialize(card) });
        }
    }
}
