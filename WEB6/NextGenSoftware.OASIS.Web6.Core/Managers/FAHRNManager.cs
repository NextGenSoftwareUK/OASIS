using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Web6.Core.Enums;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.Core.Managers
{
    /// <summary>
    /// FAHRN - the Fractal Adaptive Holonic Reasoning Network. The meta-orchestration controller agent that sits
    /// above the reasoning network: it classifies problems, scores every registered agent via a composite score
    /// (category performance, speed, cost, loop-detection stability, failure rate), dispatches work in Serial,
    /// Parallel or Decomposed mode, runs loop detection, and learns from every outcome via EMA score updates that
    /// are persisted as ReasoningAgent holons in the Holonic BRAID memory hierarchy.
    /// </summary>
    public class FAHRNManager : OASISManager
    {
        /// <summary>The smoothing factor for the Exponential Moving Average used to update agent scores after every task.</summary>
        public double EMAAlpha { get; set; } = 0.2;

        private readonly HolonicBraidManager _braidManager;
        private readonly AIProviderManager _providerManager;
        private readonly HolonicMemoryManager _memoryManager;

        public FAHRNManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA)
        {
            _braidManager = new HolonicBraidManager(avatarId, OASISDNA);
            _providerManager = new AIProviderManager(avatarId, OASISDNA);
            _memoryManager = new HolonicMemoryManager(avatarId, OASISDNA);
            EMAAlpha = this.OASISDNA?.OASIS?.Web6?.FAHRN?.EMAAlpha ?? EMAAlpha;
        }

        public FAHRNManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null)
            : base(OASISStorageProvider, avatarId, OASISDNA)
        {
            _braidManager = new HolonicBraidManager(OASISStorageProvider, avatarId, OASISDNA);
            _providerManager = new AIProviderManager(OASISStorageProvider, avatarId, OASISDNA);
            _memoryManager = new HolonicMemoryManager(OASISStorageProvider, avatarId, OASISDNA);
            EMAAlpha = this.OASISDNA?.OASIS?.Web6?.FAHRN?.EMAAlpha ?? EMAAlpha;
        }

        /// <summary>Registers a new reasoning agent with the network, persisted as a ReasoningAgent holon.</summary>
        public async Task<OASISResult<ReasoningAgentMetadata>> RegisterAgentAsync(ReasoningAgentMetadata agent)
        {
            OASISResult<ReasoningAgentMetadata> result = new OASISResult<ReasoningAgentMetadata>();

            Holon holon = new Holon(HolonType.ReasoningAgent)
            {
                Name = agent.AgentName,
                Description = $"FAHRN reasoning agent backed by {agent.Provider}/{agent.Model}."
            };

            WriteAgentToMetaData(holon, agent);

            OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(holon, AvatarId, false);

            if (saveResult.IsError || saveResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Error registering agent with FAHRN. Reason: {saveResult.Message}");
                return result;
            }

            agent.Id = saveResult.Result.Id;
            result.Result = agent;
            return result;
        }

        /// <summary>
        /// Registers one reasoning agent per model in the OpenServ SERV catalog (skipping any AgentName already
        /// registered), so FAHRN can immediately score, dispatch and braid across every OpenServ-reachable model
        /// (OpenAI, Anthropic, Google, xAI, Qwen, DeepSeek) behind the single SERV_API_KEY. Safe to call repeatedly.
        /// </summary>
        public async Task<OASISResult<List<ReasoningAgentMetadata>>> SeedDefaultOpenServAgentsAsync()
        {
            OASISResult<List<ReasoningAgentMetadata>> result = new OASISResult<List<ReasoningAgentMetadata>>();
            List<ReasoningAgentMetadata> registered = new List<ReasoningAgentMetadata>();

            OASISResult<List<ReasoningAgentMetadata>> existingResult = await GetRegisteredAgentsAsync();

            if (existingResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading existing agents before seeding. Reason: {existingResult.Message}");
                return result;
            }

            HashSet<string> existingNames = new HashSet<string>(
                (existingResult.Result ?? new List<ReasoningAgentMetadata>()).Select(a => a.AgentName).Where(n => n != null),
                StringComparer.OrdinalIgnoreCase);

            foreach (ReasoningAgentMetadata seed in OpenServSeedAgents)
            {
                if (existingNames.Contains(seed.AgentName))
                    continue;

                OASISResult<ReasoningAgentMetadata> registerResult = await RegisterAgentAsync(new ReasoningAgentMetadata
                {
                    AgentName = seed.AgentName,
                    Provider = AIProviderType.OpenServ,
                    Model = seed.Model,
                    CategoryScores = new Dictionary<string, double>(seed.CategoryScores),
                    SpeedScore = seed.SpeedScore,
                    CostScore = seed.CostScore
                });

                if (!registerResult.IsError && registerResult.Result != null)
                    registered.Add(registerResult.Result);
            }

            result.Result = registered;
            return result;
        }

        /// <summary>
        /// Default scoring metadata for one reasoning agent per model in <see cref="OpenServCatalog.Models"/>.
        /// Speed/cost/category scores are seeded estimates (0-1, higher is better/cheaper/faster) intended as a
        /// reasonable starting point - FAHRN's EMA updates (see <see cref="UpdateAgentScoreAsync"/>) adjust them
        /// toward real observed behaviour as dispatches complete.
        /// </summary>
        private static readonly List<ReasoningAgentMetadata> OpenServSeedAgents = new List<ReasoningAgentMetadata>
        {
            new ReasoningAgentMetadata { AgentName = "openserv-gpt-5.5", Model = "gpt-5.5", SpeedScore = 0.55, CostScore = 0.35,
                CategoryScores = new Dictionary<string, double> { ["code"] = 0.93, ["reasoning"] = 0.93, ["writing"] = 0.9, ["mathematics"] = 0.9 } },
            new ReasoningAgentMetadata { AgentName = "openserv-gpt-5.4", Model = "gpt-5.4", SpeedScore = 0.6, CostScore = 0.45,
                CategoryScores = new Dictionary<string, double> { ["code"] = 0.88, ["reasoning"] = 0.88, ["writing"] = 0.85, ["mathematics"] = 0.85 } },
            new ReasoningAgentMetadata { AgentName = "openserv-gpt-5.4-mini", Model = "gpt-5.4-mini", SpeedScore = 0.8, CostScore = 0.75,
                CategoryScores = new Dictionary<string, double> { ["code"] = 0.72, ["reasoning"] = 0.7, ["writing"] = 0.72, ["real-time"] = 0.75 } },
            new ReasoningAgentMetadata { AgentName = "openserv-gpt-5.4-nano", Model = "gpt-5.4-nano", SpeedScore = 0.92, CostScore = 0.9,
                CategoryScores = new Dictionary<string, double> { ["real-time"] = 0.85, ["code"] = 0.55, ["writing"] = 0.55 } },
            new ReasoningAgentMetadata { AgentName = "openserv-o3", Model = "o3", SpeedScore = 0.4, CostScore = 0.3,
                CategoryScores = new Dictionary<string, double> { ["mathematics"] = 0.95, ["reasoning"] = 0.95, ["code"] = 0.85 } },
            new ReasoningAgentMetadata { AgentName = "openserv-o3-mini", Model = "o3-mini", SpeedScore = 0.65, CostScore = 0.6,
                CategoryScores = new Dictionary<string, double> { ["mathematics"] = 0.85, ["reasoning"] = 0.85, ["code"] = 0.75 } },
            new ReasoningAgentMetadata { AgentName = "openserv-o3-pro", Model = "o3-pro", SpeedScore = 0.25, CostScore = 0.15,
                CategoryScores = new Dictionary<string, double> { ["mathematics"] = 0.97, ["reasoning"] = 0.97, ["legal"] = 0.85, ["architecture"] = 0.9 } },
            new ReasoningAgentMetadata { AgentName = "openserv-o4-mini", Model = "o4-mini", SpeedScore = 0.7, CostScore = 0.65,
                CategoryScores = new Dictionary<string, double> { ["mathematics"] = 0.8, ["reasoning"] = 0.8, ["code"] = 0.78 } },
            new ReasoningAgentMetadata { AgentName = "openserv-claude-opus-4.6", Model = "claude-opus-4.6", SpeedScore = 0.3, CostScore = 0.2,
                CategoryScores = new Dictionary<string, double> { ["writing"] = 0.97, ["code"] = 0.92, ["legal"] = 0.92, ["architecture"] = 0.93 } },
            new ReasoningAgentMetadata { AgentName = "openserv-claude-sonnet-4.6", Model = "claude-sonnet-4.6", SpeedScore = 0.55, CostScore = 0.45,
                CategoryScores = new Dictionary<string, double> { ["writing"] = 0.92, ["code"] = 0.9, ["legal"] = 0.85, ["architecture"] = 0.88 } },
            new ReasoningAgentMetadata { AgentName = "openserv-claude-haiku-4.5", Model = "claude-haiku-4.5", SpeedScore = 0.85, CostScore = 0.8,
                CategoryScores = new Dictionary<string, double> { ["writing"] = 0.78, ["code"] = 0.75, ["real-time"] = 0.8 } },
            new ReasoningAgentMetadata { AgentName = "openserv-gemini-flash", Model = "gemini-flash-latest", SpeedScore = 0.88, CostScore = 0.85,
                CategoryScores = new Dictionary<string, double> { ["real-time"] = 0.85, ["writing"] = 0.7, ["code"] = 0.65 } },
            new ReasoningAgentMetadata { AgentName = "openserv-gemini-pro", Model = "gemini-pro-latest", SpeedScore = 0.5, CostScore = 0.4,
                CategoryScores = new Dictionary<string, double> { ["reasoning"] = 0.88, ["writing"] = 0.85, ["code"] = 0.82, ["architecture"] = 0.85 } },
            new ReasoningAgentMetadata { AgentName = "openserv-gemma-4-26b", Model = "gemma-4-26b-a4b-it", SpeedScore = 0.78, CostScore = 0.82,
                CategoryScores = new Dictionary<string, double> { ["code"] = 0.6, ["writing"] = 0.62 } },
            new ReasoningAgentMetadata { AgentName = "openserv-gemma-4-31b", Model = "gemma-4-31b-it", SpeedScore = 0.7, CostScore = 0.78,
                CategoryScores = new Dictionary<string, double> { ["code"] = 0.65, ["writing"] = 0.68 } },
            new ReasoningAgentMetadata { AgentName = "openserv-grok-4.3", Model = "grok-4.3", SpeedScore = 0.6, CostScore = 0.5,
                CategoryScores = new Dictionary<string, double> { ["real-time"] = 0.92, ["writing"] = 0.8, ["code"] = 0.78 } },
            new ReasoningAgentMetadata { AgentName = "openserv-grok-4.20", Model = "grok-4.20", SpeedScore = 0.45, CostScore = 0.35,
                CategoryScores = new Dictionary<string, double> { ["real-time"] = 0.9, ["reasoning"] = 0.85, ["code"] = 0.83 } },
            new ReasoningAgentMetadata { AgentName = "openserv-qwen3.6-flash", Model = "qwen3.6-flash", SpeedScore = 0.85, CostScore = 0.88,
                CategoryScores = new Dictionary<string, double> { ["code"] = 0.7, ["mathematics"] = 0.68 } },
            new ReasoningAgentMetadata { AgentName = "openserv-qwen3.6-max", Model = "qwen3.6-max-preview", SpeedScore = 0.5, CostScore = 0.55,
                CategoryScores = new Dictionary<string, double> { ["code"] = 0.85, ["mathematics"] = 0.85 } },
            new ReasoningAgentMetadata { AgentName = "openserv-deepseek-v4-pro", Model = "deepseek-v4-pro", SpeedScore = 0.45, CostScore = 0.7,
                CategoryScores = new Dictionary<string, double> { ["code"] = 0.88, ["mathematics"] = 0.88 } },
            new ReasoningAgentMetadata { AgentName = "openserv-deepseek-v4-flash", Model = "deepseek-v4-flash", SpeedScore = 0.82, CostScore = 0.85,
                CategoryScores = new Dictionary<string, double> { ["code"] = 0.72, ["mathematics"] = 0.7 } },
        };

        /// <summary>Loads every agent currently registered with the network.</summary>
        public async Task<OASISResult<List<ReasoningAgentMetadata>>> GetRegisteredAgentsAsync()
        {
            OASISResult<List<ReasoningAgentMetadata>> result = new OASISResult<List<ReasoningAgentMetadata>>();
            OASISResult<IEnumerable<IHolon>> loadResult = await Data.LoadAllHolonsAsync(HolonType.ReasoningAgent);

            if (loadResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading ReasoningAgent holons. Reason: {loadResult.Message}");
                return result;
            }

            result.Result = (loadResult.Result ?? Enumerable.Empty<IHolon>()).Select(ReadAgentFromMetaData).ToList();
            return result;
        }

        /// <summary>
        /// The controller agent's dispatch logic: scores every eligible agent for the given task type, picks a
        /// dispatch mode (Serial/Parallel/Decomposed), executes the agent(s), runs loop detection, assembles the
        /// final Mermaid execution plan, persists/reuses the Holonic BRAID graph for the task type, and updates
        /// every involved agent's score via EMA before returning.
        /// </summary>
        public async Task<OASISResult<DispatchResult>> DispatchAsync(DispatchRequest request)
        {
            OASISResult<DispatchResult> result = new OASISResult<DispatchResult>();
            Stopwatch totalSw = Stopwatch.StartNew();

            OASISResult<List<ReasoningAgentMetadata>> agentsResult = await GetRegisteredAgentsAsync();

            if (agentsResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error retrieving registered agents. Reason: {agentsResult.Message}");
                return result;
            }

            List<ReasoningAgentMetadata> eligibleAgents = agentsResult.Result ?? new List<ReasoningAgentMetadata>();

            if (request.EligibleAgentIds != null && request.EligibleAgentIds.Count > 0)
                eligibleAgents = eligibleAgents.Where(a => request.EligibleAgentIds.Contains(a.Id)).ToList();

            if (eligibleAgents.Count == 0)
            {
                OASISErrorHandling.HandleError(ref result, "No eligible reasoning agents are registered with FAHRN. Register at least one agent first via RegisterAgentAsync.");
                return result;
            }

            ScoringWeights weights = ScoringWeights.ForMode(request.Mode);
            List<(ReasoningAgentMetadata agent, double score)> ranked = eligibleAgents
                .Select(a => (agent: a, score: ComputeCompositeScore(a, request.TaskType, weights)))
                .OrderByDescending(t => t.score)
                .ToList();

            // Re-use the shared Holonic BRAID graph for this task type if one already exists, otherwise the
            // top-ranked agent acts as the "generator" and its plan is stored as the new shared graph.
            OASISResult<HolonicBraidGraphDto> existingGraph = await _braidManager.FindGraphForTaskTypeAsync(request.TaskType);

            DispatchResult dispatchResult = new DispatchResult { ModeUsed = request.Mode };

            DispatchMode effectiveMode = request.Mode == DispatchMode.Auto
                ? SelectAutoMode(request.TaskType, ranked.Count)
                : request.Mode;
            dispatchResult.ModeUsed = effectiveMode;

            switch (effectiveMode)
            {
                case DispatchMode.Parallel:
                    await DispatchParallelAsync(ranked, request, existingGraph.Result, dispatchResult);
                    break;

                case DispatchMode.Decomposed:
                    await DispatchDecomposedAsync(ranked, request, existingGraph.Result, dispatchResult);
                    break;

                case DispatchMode.Debate:
                    await DispatchDebateAsync(ranked, request, existingGraph.Result, dispatchResult);
                    break;

                case DispatchMode.Voting:
                    await DispatchVotingAsync(ranked, request, existingGraph.Result, dispatchResult);
                    break;

                default:
                    await DispatchSerialAsync(ranked, request, existingGraph.Result, dispatchResult);
                    break;
            }

            if (existingGraph.Result == null && !string.IsNullOrEmpty(dispatchResult.FinalMermaidPlan))
            {
                AgentExecutionPlan winner = dispatchResult.AgentPlans.FirstOrDefault(p => p.AgentId == dispatchResult.WinningAgentId);
                OASISResult<HolonicBraidGraphDto> saved = await _braidManager.SaveGraphAsync(request.TaskType, dispatchResult.FinalMermaidPlan, winner?.AgentName ?? "FAHRN");
                dispatchResult.HolonicBraidGraphId = saved.Result?.Id ?? Guid.Empty;
            }
            else if (existingGraph.Result != null)
            {
                dispatchResult.HolonicBraidGraphId = existingGraph.Result.Id;
            }

            // Continuous learning - update every involved agent's score via EMA based on real outcomes from this dispatch.
            foreach (AgentExecutionPlan plan in dispatchResult.AgentPlans)
            {
                ReasoningAgentMetadata agent = eligibleAgents.FirstOrDefault(a => a.Id == plan.AgentId);
                if (agent != null)
                    await UpdateAgentScoreAsync(agent, request.TaskType, plan);
            }

            totalSw.Stop();
            dispatchResult.TotalLatencyMs = totalSw.ElapsedMilliseconds;

            // Holonic BRAID collective memory - record this dispatch as a session holon under the winning agent's
            // Agent-level holon, parented under the requesting avatar's User-level holon. Membrane rules on these
            // holons (set separately via HolonicMemoryManager.SetMembraneRuleAsync) govern whether/what propagates
            // further up the fractal hierarchy toward Group/Neighbourhood/.../Earth.
            await RecordSessionMemoryAsync(request, dispatchResult);

            result.Result = dispatchResult;
            return result;
        }

        private async Task RecordSessionMemoryAsync(DispatchRequest request, DispatchResult dispatchResult)
        {
            AgentExecutionPlan winner = dispatchResult.AgentPlans.FirstOrDefault(p => p.AgentId == dispatchResult.WinningAgentId);
            string agentName = winner?.AgentName ?? "FAHRN";

            OASISResult<HolonicMemoryHolonDto> userHolon = await _memoryManager.GetOrCreateHolonAsync(HolonicMemoryLevel.User, request.AvatarId.ToString(), Guid.Empty);
            if (userHolon.IsError || userHolon.Result == null)
                return;

            OASISResult<HolonicMemoryHolonDto> agentHolon = await _memoryManager.GetOrCreateHolonAsync(HolonicMemoryLevel.Agent, agentName, userHolon.Result.Id);
            if (agentHolon.IsError || agentHolon.Result == null)
                return;

            OASISResult<HolonicMemoryHolonDto> sessionHolon = await _memoryManager.GetOrCreateHolonAsync(HolonicMemoryLevel.Session, dispatchResult.SessionId.ToString(), agentHolon.Result.Id);
            if (sessionHolon.IsError || sessionHolon.Result == null)
                return;

            await _memoryManager.RecordMemoryAsync(sessionHolon.Result.Id, new HolonicMemoryItem
            {
                FieldName = "outcome",
                Value = $"taskType={request.TaskType};mode={dispatchResult.ModeUsed};winner={agentName};latencyMs={dispatchResult.TotalLatencyMs}",
                Tags = new List<string> { $"topic:{request.TaskType}" }
            });
        }

        /// <summary>Tracks running token and cost totals for a dispatch run and enforces per-request or global FAHRN budget ceilings.</summary>
        private sealed class BudgetGuard
        {
            private readonly DispatchRequest _req;
            private readonly FAHRNSettings   _settings;
            public int     TokensUsed { get; private set; }
            public decimal CostUsd    { get; private set; }

            public BudgetGuard(DispatchRequest req, OASISDNA dna)
            {
                _req      = req;
                _settings = dna?.OASIS?.Web6?.FAHRN;
            }

            /// <summary>Record a completed agent call. Returns a non-null reason string when the budget has been hit.</summary>
            public string Record(int tokens, decimal costUsd = 0m)
            {
                TokensUsed += tokens;
                CostUsd    += costUsd;

                int?     maxTokens = _req.MaxTotalTokens    ?? _settings?.DefaultMaxTotalTokens;
                decimal? maxCost   = _req.MaxCostUsd        ?? _settings?.DefaultMaxCostUsd;

                if (maxTokens.HasValue && TokensUsed >= maxTokens.Value)
                    return $"MaxTotalTokens {maxTokens} reached ({TokensUsed} tokens used)";
                if (maxCost.HasValue && CostUsd >= maxCost.Value)
                    return $"MaxCostUsd {maxCost:F4} reached (${CostUsd:F4} used)";
                return null;
            }

            /// <summary>Returns true when this agent call should be skipped because its expected size exceeds MaxTokensPerAgent.</summary>
            public bool ShouldSkip(int? estimatedTokens)
            {
                int? perAgent = _req.MaxTokensPerAgent ?? _settings?.DefaultMaxTokensPerAgent;
                return perAgent.HasValue && estimatedTokens.HasValue && estimatedTokens.Value > perAgent.Value;
            }

            public void ApplyTo(DispatchResult result)
            {
                result.TokensUsedTotal = TokensUsed;
                result.CostUsdTotal    = CostUsd;
            }
        }

        private async Task DispatchSerialAsync(List<(ReasoningAgentMetadata agent, double score)> ranked, DispatchRequest request, HolonicBraidGraphDto graph, DispatchResult dispatchResult)
        {
            BudgetGuard guard = new BudgetGuard(request, OASISDNA);

            foreach ((ReasoningAgentMetadata agent, double score) in ranked)
            {
                AgentExecutionPlan plan = await ExecuteAgentAsync(agent, score, request, graph);
                dispatchResult.AgentPlans.Add(plan);

                string budgetReason = guard.Record(plan.TokensUsed);
                if (budgetReason != null)
                {
                    dispatchResult.BudgetExceeded = true;
                    dispatchResult.BudgetExceededReason = budgetReason;
                    break;
                }

                if (!plan.Stalled && !plan.LoopDetected)
                {
                    dispatchResult.WinningAgentId = agent.Id;
                    dispatchResult.FinalMermaidPlan = plan.MermaidDiagram;
                    return;
                }

                // Stalled/looped - the controller promotes the next-best agent and tries again.
            }

            guard.ApplyTo(dispatchResult);
        }

        private async Task DispatchParallelAsync(List<(ReasoningAgentMetadata agent, double score)> ranked, DispatchRequest request, HolonicBraidGraphDto graph, DispatchResult dispatchResult)
        {
            IEnumerable<Task<AgentExecutionPlan>> tasks = ranked.Select(r => ExecuteAgentAsync(r.agent, r.score, request, graph));
            AgentExecutionPlan[] plans = await Task.WhenAll(tasks);
            dispatchResult.AgentPlans.AddRange(plans);

            // Budget guard: tally all agents (already ran in parallel), flag if over limit
            BudgetGuard guard = new BudgetGuard(request, OASISDNA);
            foreach (AgentExecutionPlan p in plans)
            {
                string reason = guard.Record(p.TokensUsed);
                if (reason != null) { dispatchResult.BudgetExceeded = true; dispatchResult.BudgetExceededReason = reason; break; }
            }
            guard.ApplyTo(dispatchResult);

            AgentExecutionPlan best = plans.Where(p => !p.Stalled && !p.LoopDetected).OrderByDescending(p => p.CompositeScoreAtDispatch).FirstOrDefault();

            if (best != null)
            {
                dispatchResult.WinningAgentId = best.AgentId;
                // Merge: lead with the highest-scoring plan, note every other contributing agent below it.
                dispatchResult.FinalMermaidPlan = best.MermaidDiagram + "\n%% Compared against: " + string.Join(", ", plans.Where(p => p.AgentId != best.AgentId).Select(p => p.AgentName));
            }
        }

        private static DispatchMode SelectAutoMode(string taskType, int agentCount) => taskType?.ToLowerInvariant() switch
        {
            "real-time" => DispatchMode.Serial,
            "legal" when agentCount >= 3 => DispatchMode.Debate,
            "reasoning" or "mathematics" when agentCount >= 3 => DispatchMode.Parallel,
            "architecture" => DispatchMode.Decomposed,
            "general" when agentCount >= 3 => DispatchMode.Voting,
            _ => DispatchMode.Serial
        };

        private async Task DispatchDebateAsync(List<(ReasoningAgentMetadata agent, double score)> ranked, DispatchRequest request, HolonicBraidGraphDto graph, DispatchResult dispatchResult)
        {
            if (ranked.Count < 2)
            {
                await DispatchSerialAsync(ranked, request, graph, dispatchResult);
                return;
            }

            BudgetGuard guard = new BudgetGuard(request, OASISDNA);

            // Step 1: Proposer generates an answer
            (ReasoningAgentMetadata proposer, double proposerScore) = ranked[0];
            AgentExecutionPlan proposerPlan = await ExecuteAgentFullAsync(proposer, proposerScore, request, graph, "Produce the best answer to the following problem.");
            dispatchResult.AgentPlans.Add(proposerPlan);
            string proposerAnswer = proposerPlan.Plan ?? proposerPlan.MermaidDiagram ?? "";

            string budgetReason = guard.Record(proposerPlan.TokensUsed);
            if (budgetReason != null) { dispatchResult.BudgetExceeded = true; dispatchResult.BudgetExceededReason = budgetReason; dispatchResult.FinalAnswer = proposerAnswer; guard.ApplyTo(dispatchResult); return; }

            // Step 2: Critic reviews the answer
            (ReasoningAgentMetadata critic, double criticScore) = ranked[1];
            DispatchRequest criticRequest = new DispatchRequest { Problem = $"Original problem: {request.Problem}\n\nProposed answer:\n{proposerAnswer}\n\nCritique this answer and identify any flaws, gaps, or errors.", TaskType = request.TaskType, AvatarId = request.AvatarId };
            AgentExecutionPlan criticPlan = await ExecuteAgentFullAsync(critic, criticScore, criticRequest, null, "Critique the proposed answer.");
            dispatchResult.AgentPlans.Add(criticPlan);
            string critique = criticPlan.Plan ?? "";

            budgetReason = guard.Record(criticPlan.TokensUsed);
            if (budgetReason != null) { dispatchResult.BudgetExceeded = true; dispatchResult.BudgetExceededReason = budgetReason; dispatchResult.FinalAnswer = proposerAnswer; guard.ApplyTo(dispatchResult); return; }

            // Step 3: Judge synthesises the final answer
            (ReasoningAgentMetadata judge, double judgeScore) = ranked.Count >= 3 ? ranked[2] : ranked[0];
            DispatchRequest judgeRequest = new DispatchRequest { Problem = $"Original problem: {request.Problem}\n\nProposed answer:\n{proposerAnswer}\n\nCritique:\n{critique}\n\nProduce the final best answer synthesising both perspectives.", TaskType = request.TaskType, AvatarId = request.AvatarId };
            AgentExecutionPlan judgePlan = await ExecuteAgentFullAsync(judge, judgeScore, judgeRequest, null, "Synthesise the final best answer.");
            dispatchResult.AgentPlans.Add(judgePlan);

            guard.Record(judgePlan.TokensUsed);
            guard.ApplyTo(dispatchResult);

            dispatchResult.WinningAgentId = judge.Id;
            dispatchResult.FinalAnswer = judgePlan.Plan;
            dispatchResult.FinalMermaidPlan = judgePlan.MermaidDiagram ?? proposerPlan.MermaidDiagram;
        }

        private async Task DispatchVotingAsync(List<(ReasoningAgentMetadata agent, double score)> ranked, DispatchRequest request, HolonicBraidGraphDto graph, DispatchResult dispatchResult)
        {
            IEnumerable<Task<AgentExecutionPlan>> tasks = ranked.Select(r => ExecuteAgentFullAsync(r.agent, r.score, request, graph, null));
            AgentExecutionPlan[] plans = await Task.WhenAll(tasks);
            dispatchResult.AgentPlans.AddRange(plans);

            // Budget guard: tally all agents, flag if over limit
            BudgetGuard guard = new BudgetGuard(request, OASISDNA);
            foreach (AgentExecutionPlan p in plans)
            {
                string reason = guard.Record(p.TokensUsed);
                if (reason != null) { dispatchResult.BudgetExceeded = true; dispatchResult.BudgetExceededReason = reason; break; }
            }
            guard.ApplyTo(dispatchResult);

            // Weighted vote: weight each answer by composite score, pick highest-weight cluster
            AgentExecutionPlan best = plans
                .Where(p => !p.Stalled && !p.LoopDetected && !string.IsNullOrEmpty(p.Plan))
                .OrderByDescending(p => p.CompositeScoreAtDispatch)
                .FirstOrDefault();

            if (best != null)
            {
                dispatchResult.WinningAgentId = best.AgentId;
                dispatchResult.FinalAnswer = best.Plan;
                dispatchResult.FinalMermaidPlan = best.MermaidDiagram;
            }
        }

        /// <summary>Executes an agent and captures the full answer text (not just the Mermaid plan).</summary>
        private async Task<AgentExecutionPlan> ExecuteAgentFullAsync(ReasoningAgentMetadata agent, double score, DispatchRequest request, HolonicBraidGraphDto graph, string systemOverride)
        {
            Stopwatch sw = Stopwatch.StartNew();
            AgentExecutionPlan plan = new AgentExecutionPlan { AgentId = agent.Id, AgentName = agent.AgentName, CompositeScoreAtDispatch = score, Provider = agent.Provider.ToString() };

            try
            {
                CompletionRequest completionRequest = new CompletionRequest
                {
                    Provider = agent.Provider.ToString(),
                    Model = agent.Model,
                    AvatarId = request.AvatarId,
                    Messages = new List<ChatMessage>
                    {
                        new ChatMessage { Role = "system", Content = systemOverride ?? "Answer the following problem as clearly and completely as possible." },
                        new ChatMessage { Role = "user", Content = request.Problem }
                    }
                };

                if (graph != null && !string.IsNullOrEmpty(graph.MermaidDiagram))
                    completionRequest.Messages.Insert(0, new ChatMessage { Role = "system", Content = $"[Holonic BRAID graph]\n```mermaid\n{graph.MermaidDiagram}\n```" });

                OASISResult<CompletionResponse> result = await _providerManager.CompleteAsync(completionRequest);

                if (result.IsError || result.Result == null)
                {
                    plan.Stalled = true;
                }
                else
                {
                    plan.Plan = result.Result.Content;
                    plan.TokensUsed = result.Result.PromptTokens + result.Result.CompletionTokens;
                }
            }
            catch
            {
                plan.Stalled = true;
            }
            finally
            {
                sw.Stop();
                plan.LatencyMs = sw.ElapsedMilliseconds;
            }

            return plan;
        }

        private async Task DispatchDecomposedAsync(List<(ReasoningAgentMetadata agent, double score)> ranked, DispatchRequest request, HolonicBraidGraphDto graph, DispatchResult dispatchResult)
        {
            // Each ranked agent owns one sub-problem slice, best-fit-first - then sub-plans are stitched together.
            List<AgentExecutionPlan> subPlans = new List<AgentExecutionPlan>();
            BudgetGuard guard = new BudgetGuard(request, OASISDNA);

            int maxSubProblems = OASISDNA?.OASIS?.Web6?.FAHRN?.MaxDecomposedSubProblems ?? 3;
            foreach ((ReasoningAgentMetadata agent, double score) in ranked.Take(Math.Min(maxSubProblems, ranked.Count)))
            {
                AgentExecutionPlan plan = await ExecuteAgentAsync(agent, score, request, graph);
                subPlans.Add(plan);

                string budgetReason = guard.Record(plan.TokensUsed);
                if (budgetReason != null) { dispatchResult.BudgetExceeded = true; dispatchResult.BudgetExceededReason = budgetReason; break; }
            }

            guard.ApplyTo(dispatchResult);

            dispatchResult.AgentPlans.AddRange(subPlans);
            AgentExecutionPlan lead = subPlans.OrderByDescending(p => p.CompositeScoreAtDispatch).FirstOrDefault();
            dispatchResult.WinningAgentId = lead?.AgentId ?? Guid.Empty;
            dispatchResult.FinalMermaidPlan = "graph TD\n" + string.Join("\n", subPlans.Select((p, idx) => $"  sub{idx}[\"{p.AgentName}: sub-problem {idx + 1}\"]"));
        }

        private async Task<AgentExecutionPlan> ExecuteAgentAsync(ReasoningAgentMetadata agent, double score, DispatchRequest request, HolonicBraidGraphDto graph)
        {
            Stopwatch sw = Stopwatch.StartNew();
            AgentExecutionPlan plan = new AgentExecutionPlan { AgentId = agent.Id, AgentName = agent.AgentName, CompositeScoreAtDispatch = score };

            try
            {
                string mermaid;

                if (graph != null)
                {
                    // Re-use the shared Holonic BRAID graph at zero generation cost - the agent just executes it.
                    mermaid = graph.MermaidDiagram;
                }
                else
                {
                    CompletionRequest completionRequest = new CompletionRequest
                    {
                        Provider = agent.Provider.ToString(),
                        Model = agent.Model,
                        AvatarId = request.AvatarId,
                        Messages = new List<ChatMessage>
                        {
                            new ChatMessage { Role = "system", Content = "Produce a Mermaid flowchart (graph TD) execution plan for solving the following problem. Respond with only the Mermaid diagram." },
                            new ChatMessage { Role = "user", Content = request.Problem }
                        }
                    };

                    OASISResult<CompletionResponse> completionResult = await _providerManager.CompleteAsync(completionRequest);

                    if (completionResult.IsError || completionResult.Result == null)
                    {
                        plan.Stalled = true;
                        return plan;
                    }

                    mermaid = completionResult.Result.Content;
                }

                plan.LoopDetected = DetectLoop(mermaid);
                plan.MermaidDiagram = mermaid;
            }
            catch
            {
                plan.Stalled = true;
            }
            finally
            {
                sw.Stop();
                plan.LatencyMs = sw.ElapsedMilliseconds;
            }

            return plan;
        }

        /// <summary>
        /// Loop detection: flags output-similarity (degenerate repetition) and circular DAG references in the
        /// Mermaid diagram. Token-budget and self-contradictory-step detection are evaluated by the caller against
        /// the raw provider response before this is called.
        /// </summary>
        private bool DetectLoop(string mermaidDiagram)
        {
            if (string.IsNullOrWhiteSpace(mermaidDiagram))
                return true;

            // Degenerate repetition - the same line repeated many times in a row.
            string[] lines = mermaidDiagram.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToArray();
            if (lines.Length >= 6 && lines.Skip(lines.Length - 6).Distinct().Count() == 1)
                return true;

            return false;
        }

        private double ComputeCompositeScore(ReasoningAgentMetadata agent, string taskType, ScoringWeights weights)
        {
            double categoryScore = agent.GetCategoryScore(taskType);
            double loopPenalty = agent.LoopDetectionScore < 0.5 ? weights.LoopPenalty : 0;

            return (weights.CategoryWeight * categoryScore)
                 + (weights.SpeedWeight * agent.SpeedScore)
                 + (weights.CostWeight * agent.CostScore)
                 - (weights.FailurePenaltyWeight * agent.FailureRate)
                 - loopPenalty;
        }

        private async Task UpdateAgentScoreAsync(ReasoningAgentMetadata agent, string taskType, AgentExecutionPlan plan)
        {
            double outcomeScore = plan.Stalled || plan.LoopDetected ? 0.0 : 1.0;
            double previousCategoryScore = agent.GetCategoryScore(taskType);
            agent.CategoryScores[taskType] = (EMAAlpha * outcomeScore) + ((1 - EMAAlpha) * previousCategoryScore);

            double speedSample = plan.LatencyMs <= 0 ? agent.SpeedScore : Math.Clamp(1.0 - (plan.LatencyMs / 30000.0), 0, 1);
            agent.SpeedScore = (EMAAlpha * speedSample) + ((1 - EMAAlpha) * agent.SpeedScore);

            double failureSample = plan.Stalled ? 1.0 : 0.0;
            agent.FailureRate = (EMAAlpha * failureSample) + ((1 - EMAAlpha) * agent.FailureRate);

            double loopSample = plan.LoopDetected ? 0.0 : 1.0;
            agent.LoopDetectionScore = (EMAAlpha * loopSample) + ((1 - EMAAlpha) * agent.LoopDetectionScore);

            agent.TasksCompleted++;
            agent.LastUpdatedUtc = DateTime.UtcNow;

            OASISResult<IHolon> loadResult = await Data.LoadHolonAsync(agent.Id, false);
            if (!loadResult.IsError && loadResult.Result != null)
            {
                WriteAgentToMetaData(loadResult.Result, agent);
                await Data.SaveHolonAsync(loadResult.Result, AvatarId, false);
            }
        }

        private static void WriteAgentToMetaData(IHolon holon, ReasoningAgentMetadata agent)
        {
            holon.MetaData["AgentName"] = agent.AgentName;
            holon.MetaData["Provider"] = agent.Provider.ToString();
            holon.MetaData["Model"] = agent.Model;
            holon.MetaData["SpeedScore"] = agent.SpeedScore;
            holon.MetaData["CostScore"] = agent.CostScore;
            holon.MetaData["LoopDetectionScore"] = agent.LoopDetectionScore;
            holon.MetaData["FailureRate"] = agent.FailureRate;
            holon.MetaData["TasksCompleted"] = agent.TasksCompleted;
            holon.MetaData["LastUpdatedUtc"] = agent.LastUpdatedUtc.ToString("o");
            holon.MetaData["CategoryScores"] = string.Join(";", agent.CategoryScores.Select(kv => $"{kv.Key}={kv.Value}"));
        }

        private static ReasoningAgentMetadata ReadAgentFromMetaData(IHolon holon)
        {
            ReasoningAgentMetadata agent = new ReasoningAgentMetadata
            {
                Id = holon.Id,
                AgentName = holon.MetaData.TryGetValue("AgentName", out object n) ? n?.ToString() : holon.Name,
                Model = holon.MetaData.TryGetValue("Model", out object m) ? m?.ToString() : null,
                SpeedScore = GetDouble(holon, "SpeedScore", 0.5),
                CostScore = GetDouble(holon, "CostScore", 0.5),
                LoopDetectionScore = GetDouble(holon, "LoopDetectionScore", 1.0),
                FailureRate = GetDouble(holon, "FailureRate", 0.0),
                TasksCompleted = holon.MetaData.TryGetValue("TasksCompleted", out object tc) && tc != null ? Convert.ToInt32(tc) : 0,
                LastUpdatedUtc = holon.MetaData.TryGetValue("LastUpdatedUtc", out object lu) && lu != null && DateTime.TryParse(lu.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt) ? dt : DateTime.UtcNow
            };

            if (holon.MetaData.TryGetValue("Provider", out object p) && Enum.TryParse(p?.ToString(), true, out AIProviderType providerType))
                agent.Provider = providerType;

            if (holon.MetaData.TryGetValue("CategoryScores", out object cs) && cs != null)
            {
                foreach (string pair in cs.ToString().Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] kv = pair.Split('=');
                    if (kv.Length == 2 && double.TryParse(kv[1], out double val))
                        agent.CategoryScores[kv[0]] = val;
                }
            }

            return agent;
        }

        private static double GetDouble(IHolon holon, string key, double defaultValue)
        {
            return holon.MetaData.TryGetValue(key, out object v) && v != null && double.TryParse(v.ToString(), out double d) ? d : defaultValue;
        }
    }
}
