using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Grpc.Core;
using NextGenSoftware.OASIS.OASISBootLoader;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;
using NextGenSoftware.OASIS.Web6.WebAPI.Controllers;
using NextGenSoftware.OASIS.Web6.WebAPI.Grpc;

namespace NextGenSoftware.OASIS.Web6.WebAPI.GrpcServices
{
    public class AgentsGrpcService : AgentsService.AgentsServiceBase
    {
        private static NextGenSoftware.OASIS.API.DNA.OASISDNA DNA => OASISBootLoader.OASISBootLoader.OASISDNA;

        // Share A2A task store with the HTTP controller
        private static readonly ConcurrentDictionary<string, A2ATask> _tasks = new ConcurrentDictionary<string, A2ATask>();

        public override async Task<A2ATaskReply> SendA2ATask(SendA2ATaskRequest request, ServerCallContext context)
        {
            try
            {
                string taskId = string.IsNullOrWhiteSpace(request.Id) ? Guid.NewGuid().ToString() : request.Id;
                var task = new A2ATask { Id = taskId, State = "working", CreatedAtUtc = DateTime.UtcNow };
                _tasks[taskId] = task;

                _ = System.Threading.Tasks.Task.Run(async () =>
                {
                    try
                    {
                        var manager = new FahrnSolveManager(Guid.Empty, DNA);
                        var solveResult = await manager.SolveAsync(new FahrnSolveRequest { Problem = request.Text, ReturnReasoning = true });
                        task.State = solveResult.IsError ? "failed" : "completed";
                        task.Output = solveResult.IsError ? solveResult.Message : solveResult.Result?.Answer ?? "";
                    }
                    catch (Exception ex) { task.State = "failed"; task.Output = ex.Message; }
                    finally { task.CompletedAtUtc = DateTime.UtcNow; }
                });

                return new A2ATaskReply { IsError = false, Id = taskId, State = task.State, CreatedAtUtc = task.CreatedAtUtc.ToString("O") };
            }
            catch (Exception ex)
            {
                return new A2ATaskReply { IsError = true, Message = ex.Message };
            }
        }

        public override Task<A2ATaskReply> GetA2ATask(TaskIdRequest request, ServerCallContext context)
        {
            if (!_tasks.TryGetValue(request.Id, out var task))
                return System.Threading.Tasks.Task.FromResult(new A2ATaskReply { IsError = true, Message = $"Task '{request.Id}' not found" });
            return System.Threading.Tasks.Task.FromResult(new A2ATaskReply
            {
                IsError = false,
                Id = task.Id,
                State = task.State,
                Output = task.Output ?? "",
                CreatedAtUtc = task.CreatedAtUtc.ToString("O"),
                CompletedAtUtc = task.CompletedAtUtc?.ToString("O") ?? ""
            });
        }

        public override Task<BaseReply> CancelA2ATask(TaskIdRequest request, ServerCallContext context)
        {
            if (_tasks.TryGetValue(request.Id, out var task))
            {
                if (task.State == "working") { task.State = "cancelled"; task.CompletedAtUtc = DateTime.UtcNow; }
                return System.Threading.Tasks.Task.FromResult(new BaseReply { IsError = false });
            }
            return System.Threading.Tasks.Task.FromResult(new BaseReply { IsError = true, Message = $"Task '{request.Id}' not found" });
        }

        public override async Task<OrchestratorConfigReply> RegisterOrchestrator(OrchestratorConfigRequest request, ServerCallContext context)
        {
            try
            {
                var manager = new OrchestratorManager(Guid.Empty, DNA);
                if (!System.Enum.TryParse<NextGenSoftware.OASIS.Web6.Core.Enums.OrchestratorProtocolType>(request.Protocol, true, out var proto))
                    proto = NextGenSoftware.OASIS.Web6.Core.Enums.OrchestratorProtocolType.Webhook;
                var config = new OrchestratorAdapterConfig { Name = request.Name, Protocol = proto, EndpointUrl = request.EndpointUrl };
                var result = await manager.RegisterAdapterAsync(config);
                if (result.IsError) return new OrchestratorConfigReply { IsError = true, Message = result.Message };
                return new OrchestratorConfigReply { IsError = false, Name = result.Result?.Name ?? request.Name, Protocol = request.Protocol, EndpointUrl = request.EndpointUrl };
            }
            catch (Exception ex) { return new OrchestratorConfigReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<OrchestratorListReply> GetOrchestrators(EmptyRequest request, ServerCallContext context)
        {
            try
            {
                var manager = new OrchestratorManager(Guid.Empty, DNA);
                var result = await manager.GetAdaptersAsync();
                if (result.IsError) return new OrchestratorListReply { IsError = true, Message = result.Message };
                var reply = new OrchestratorListReply { IsError = false };
                foreach (var a in result.Result ?? new List<OrchestratorAdapterConfig>())
                    reply.Adapters.Add(new OrchestratorConfigReply { Name = a.Name ?? "", Protocol = a.Protocol.ToString(), EndpointUrl = a.EndpointUrl ?? "" });
                return reply;
            }
            catch (Exception ex) { return new OrchestratorListReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<InvokeOrchestratorReply> InvokeOrchestrator(InvokeOrchestratorRequest request, ServerCallContext context)
        {
            try
            {
                Guid avatarId = Guid.TryParse(request.AvatarId, out var aid) ? aid : Guid.Empty;
                var manager = new OrchestratorManager(avatarId, DNA);
                var invokeRequest = new OrchestratorInvokeRequest { Input = request.Input };
                var result = await manager.InvokeAsync(invokeRequest);
                if (result.IsError) return new InvokeOrchestratorReply { IsError = true, Message = result.Message };
                return new InvokeOrchestratorReply { IsError = false, Output = result.Result?.Output ?? "" };
            }
            catch (Exception ex) { return new InvokeOrchestratorReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<ReasoningAgentReply> RegisterReasoningAgent(ReasoningAgentRequest request, ServerCallContext context)
        {
            try
            {
                var manager = new FAHRNManager(Guid.Empty, DNA);
                if (!System.Enum.TryParse<NextGenSoftware.OASIS.Web6.Core.Enums.AIProviderType>(request.Provider, true, out var agentProviderType))
                    agentProviderType = NextGenSoftware.OASIS.Web6.Core.Enums.AIProviderType.OpenAI;
                var agent = new ReasoningAgentMetadata
                {
                    Id = Guid.TryParse(request.AgentId, out var gid) ? gid : Guid.NewGuid(),
                    AgentName = request.AgentName,
                    Provider = agentProviderType,
                    Model = request.Model
                };
                var result = await manager.RegisterAgentAsync(agent);
                if (result.IsError) return new ReasoningAgentReply { IsError = true, Message = result.Message };
                return MapAgent(result.Result);
            }
            catch (Exception ex) { return new ReasoningAgentReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<ReasoningAgentListReply> GetReasoningAgents(EmptyRequest request, ServerCallContext context)
        {
            try
            {
                var manager = new FAHRNManager(Guid.Empty, DNA);
                var result = await manager.GetRegisteredAgentsAsync();
                if (result.IsError) return new ReasoningAgentListReply { IsError = true, Message = result.Message };
                var reply = new ReasoningAgentListReply { IsError = false };
                foreach (var a in result.Result ?? new List<ReasoningAgentMetadata>())
                    reply.Agents.Add(MapAgent(a));
                return reply;
            }
            catch (Exception ex) { return new ReasoningAgentListReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<ReasoningAgentListReply> SeedOpenServAgents(EmptyRequest request, ServerCallContext context)
        {
            try
            {
                var manager = new FAHRNManager(Guid.Empty, DNA);
                var result = await manager.SeedDefaultOpenServAgentsAsync();
                if (result.IsError) return new ReasoningAgentListReply { IsError = true, Message = result.Message };
                var reply = new ReasoningAgentListReply { IsError = false };
                foreach (var a in result.Result ?? new List<ReasoningAgentMetadata>())
                    reply.Agents.Add(MapAgent(a));
                return reply;
            }
            catch (Exception ex) { return new ReasoningAgentListReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<DispatchAgentReply> Dispatch(DispatchAgentRequest request, ServerCallContext context)
        {
            try
            {
                Guid avatarId = Guid.TryParse(request.AvatarId, out var aid) ? aid : Guid.Empty;
                var manager = new FAHRNManager(avatarId, DNA);
                var dispatchRequest = new DispatchRequest { Problem = request.Problem, TaskType = request.TaskType, AvatarId = avatarId };
                var result = await manager.DispatchAsync(dispatchRequest);
                if (result.IsError) return new DispatchAgentReply { IsError = true, Message = result.Message };
                return new DispatchAgentReply
                {
                    IsError = false,
                    FinalMermaidPlan = result.Result?.FinalMermaidPlan ?? "",
                    ModeUsed = result.Result?.ModeUsed.ToString() ?? "",
                    TotalLatencyMs = result.Result?.TotalLatencyMs ?? 0,
                    Answer = result.Result?.FinalAnswer ?? ""
                };
            }
            catch (Exception ex) { return new DispatchAgentReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<SkillReply> GetSkill(SkillRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AgentId, out var agentId))
                    return new SkillReply { IsError = true, Message = "Invalid agentId GUID" };
                var manager = new SkillOptManager(Guid.Empty, DNA);
                var result = await manager.LoadSkillAsync(agentId, request.Category);
                if (result.IsError) return new SkillReply { IsError = true, Message = result.Message };
                return new SkillReply { IsError = false, SkillDocumentJson = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new SkillReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<SkillReply> EvolveSkill(SkillRequest request, ServerCallContext context)
        {
            try
            {
                if (!Guid.TryParse(request.AgentId, out var agentId))
                    return new SkillReply { IsError = true, Message = "Invalid agentId GUID" };
                var manager = new SkillOptManager(Guid.Empty, DNA);
                var result = await manager.RunEpochAsync(agentId, request.Category);
                if (result.IsError) return new SkillReply { IsError = true, Message = result.Message };
                return new SkillReply { IsError = false, SkillDocumentJson = JsonSerializer.Serialize(result.Result) };
            }
            catch (Exception ex) { return new SkillReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<BraidGraphReply> GetBraidGraph(TaskTypeRequest request, ServerCallContext context)
        {
            try
            {
                var manager = new HolonicBraidManager(Guid.Empty, DNA);
                var result = await manager.FindGraphForTaskTypeAsync(request.TaskType);
                if (result.IsError) return new BraidGraphReply { IsError = true, Message = result.Message };
                if (result.Result == null) return new BraidGraphReply { IsError = true, Message = "No graph found for task type" };
                return new BraidGraphReply
                {
                    IsError = false,
                    TaskType = result.Result.TaskType ?? request.TaskType,
                    MermaidDiagram = result.Result.MermaidDiagram ?? "",
                    GeneratedByModel = result.Result.GeneratedByModel ?? ""
                };
            }
            catch (Exception ex) { return new BraidGraphReply { IsError = true, Message = ex.Message }; }
        }

        public override async Task<BraidGraphReply> SaveBraidGraph(SaveBraidGraphRequest request, ServerCallContext context)
        {
            try
            {
                var manager = new HolonicBraidManager(Guid.Empty, DNA);
                var result = await manager.SaveGraphAsync(request.TaskType, request.MermaidDiagram, request.GeneratedByModel);
                if (result.IsError) return new BraidGraphReply { IsError = true, Message = result.Message };
                return new BraidGraphReply
                {
                    IsError = false,
                    TaskType = result.Result?.TaskType ?? request.TaskType,
                    MermaidDiagram = result.Result?.MermaidDiagram ?? request.MermaidDiagram,
                    GeneratedByModel = result.Result?.GeneratedByModel ?? request.GeneratedByModel
                };
            }
            catch (Exception ex) { return new BraidGraphReply { IsError = true, Message = ex.Message }; }
        }

        private static ReasoningAgentReply MapAgent(ReasoningAgentMetadata a)
        {
            if (a == null) return new ReasoningAgentReply();
            return new ReasoningAgentReply
            {
                IsError = false,
                AgentId = a.Id.ToString(),
                AgentName = a.AgentName ?? "",
                Provider = a.Provider.ToString(),
                Model = a.Model ?? "",
                CompositeScore = a.SpeedScore
            };
        }
    }
}
