using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Web6.Core.Enums;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.Core.Managers
{
    /// <summary>
    /// The FAHRN hero pipeline: classify → avatar context → BRAID → dispatch → return answer + trace.
    /// Backs POST /v1/fahrn/solve and the web6_fahrn_solve MCP tool.
    /// </summary>
    public class FahrnSolveManager : OASISManager
    {
        private readonly FAHRNManager _fahrnManager;
        private readonly HolonicBraidManager _braidManager;
        private readonly FahrnTaskClassifier _classifier;
        private readonly StarnetContextManager _contextManager;

        public FahrnSolveManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {
            _fahrnManager = new FAHRNManager(avatarId, OASISDNA);
            _braidManager = new HolonicBraidManager(avatarId, OASISDNA);
            _classifier = new FahrnTaskClassifier(avatarId, OASISDNA);
            _contextManager = new StarnetContextManager(avatarId, OASISDNA);
        }

        public FahrnSolveManager(IOASISStorageProvider provider, Guid avatarId, OASISDNA OASISDNA = null) : base(provider, avatarId, OASISDNA)
        {
            _fahrnManager = new FAHRNManager(provider, avatarId, OASISDNA);
            _braidManager = new HolonicBraidManager(provider, avatarId, OASISDNA);
            _classifier = new FahrnTaskClassifier(avatarId, OASISDNA);
            _contextManager = new StarnetContextManager(provider, avatarId, OASISDNA);
        }

        public async Task<OASISResult<FahrnSolveResponse>> SolveAsync(FahrnSolveRequest request)
        {
            OASISResult<FahrnSolveResponse> result = new OASISResult<FahrnSolveResponse>();

            if (string.IsNullOrWhiteSpace(request?.Problem))
            {
                OASISErrorHandling.HandleError(ref result, "FahrnSolveRequest.Problem is required.");
                return result;
            }

            Stopwatch sw = Stopwatch.StartNew();
            FahrnSolveResponse response = new FahrnSolveResponse();
            string problem = request.Problem;

            // Step 1: classify task type
            string taskType = string.IsNullOrEmpty(request.TaskType) || request.TaskType == "auto"
                ? await _classifier.ClassifyAsync(problem)
                : request.TaskType;
            response.TaskTypeClassified = taskType;

            // Step 2: avatar context injection
            if (request.InjectAvatarContext && request.AvatarId != Guid.Empty)
            {
                string ctxStr = await _contextManager.GetAvatarContextStringAsync(request.AvatarId);
                if (!string.IsNullOrEmpty(ctxStr))
                {
                    problem = ctxStr + "\n\n" + problem;
                    response.AvatarContextInjected = true;
                }
            }

            // Step 3: BRAID graph lookup
            OASISResult<HolonicBraidGraphDto> braidResult = await _braidManager.FindGraphForTaskTypeAsync(taskType);
            if (!braidResult.IsError && braidResult.Result != null)
            {
                response.BraidGraphId = braidResult.Result.Id;
                response.BraidGraphWasReused = braidResult.Result.TimesReused > 0;
                if (!string.IsNullOrEmpty(braidResult.Result.MermaidDiagram))
                    problem += $"\n\n[Holonic BRAID graph for task type '{taskType}']\n```mermaid\n{braidResult.Result.MermaidDiagram}\n```";
            }

            // Step 4: resolve dispatch mode (Auto mode selection)
            DispatchMode mode = request.Mode == DispatchMode.Auto
                ? SelectMode(taskType)
                : request.Mode;
            response.ModeUsed = mode;

            // Step 5: FAHRN dispatch
            DispatchRequest dispatchReq = new DispatchRequest
            {
                Problem = problem,
                TaskType = taskType,
                AvatarId = request.AvatarId,
                Mode = mode,
                ScoringWeights = request.ScoringWeights
            };

            OASISResult<DispatchResult> dispatchResult = await _fahrnManager.DispatchAsync(dispatchReq);
            sw.Stop();

            if (dispatchResult.IsError || dispatchResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"FAHRN dispatch failed: {dispatchResult.Message}");
                return result;
            }

            DispatchResult dr = dispatchResult.Result;
            response.Answer = dr.FinalAnswer ?? dr.FinalMermaidPlan;
            response.TotalLatencyMs = sw.ElapsedMilliseconds;
            response.TotalTokensUsed = dr.AgentPlans?.Sum(p => p.TokensUsed) ?? 0;
            response.AgentsUsed = dr.AgentPlans?.Select(p => p.AgentName).Where(n => n != null).Distinct().ToList() ?? new List<string>();
            response.Providers = dr.AgentPlans?.Select(p => p.Provider).Where(p => p != null).Distinct().ToList() ?? new List<string>();

            if (request.ReturnReasoning)
            {
                response.MermaidPlan = dr.FinalMermaidPlan;
                response.ReasoningTrace = dr.AgentPlans != null
                    ? string.Join("\n\n", dr.AgentPlans.Select(p => $"[{p.AgentName}]\n{p.Plan}"))
                    : null;
            }

            result.Result = response;
            return result;
        }

        private static DispatchMode SelectMode(string taskType) => taskType?.ToLowerInvariant() switch
        {
            "real-time" => DispatchMode.Serial,
            "legal" => DispatchMode.Debate,
            "mathematics" or "reasoning" => DispatchMode.Parallel,
            "architecture" => DispatchMode.Decomposed,
            _ => DispatchMode.Serial
        };
    }
}
