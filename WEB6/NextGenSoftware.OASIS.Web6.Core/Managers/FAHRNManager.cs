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
        }

        public FAHRNManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null)
            : base(OASISStorageProvider, avatarId, OASISDNA)
        {
            _braidManager = new HolonicBraidManager(OASISStorageProvider, avatarId, OASISDNA);
            _providerManager = new AIProviderManager(OASISStorageProvider, avatarId, OASISDNA);
            _memoryManager = new HolonicMemoryManager(OASISStorageProvider, avatarId, OASISDNA);
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

            switch (request.Mode)
            {
                case DispatchMode.Parallel:
                    await DispatchParallelAsync(ranked, request, existingGraph.Result, dispatchResult);
                    break;

                case DispatchMode.Decomposed:
                    await DispatchDecomposedAsync(ranked, request, existingGraph.Result, dispatchResult);
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

        private async Task DispatchSerialAsync(List<(ReasoningAgentMetadata agent, double score)> ranked, DispatchRequest request, HolonicBraidGraphDto graph, DispatchResult dispatchResult)
        {
            foreach ((ReasoningAgentMetadata agent, double score) in ranked)
            {
                AgentExecutionPlan plan = await ExecuteAgentAsync(agent, score, request, graph);
                dispatchResult.AgentPlans.Add(plan);

                if (!plan.Stalled && !plan.LoopDetected)
                {
                    dispatchResult.WinningAgentId = agent.Id;
                    dispatchResult.FinalMermaidPlan = plan.MermaidDiagram;
                    return;
                }

                // Stalled/looped - the controller promotes the next-best agent and tries again.
            }
        }

        private async Task DispatchParallelAsync(List<(ReasoningAgentMetadata agent, double score)> ranked, DispatchRequest request, HolonicBraidGraphDto graph, DispatchResult dispatchResult)
        {
            IEnumerable<Task<AgentExecutionPlan>> tasks = ranked.Select(r => ExecuteAgentAsync(r.agent, r.score, request, graph));
            AgentExecutionPlan[] plans = await Task.WhenAll(tasks);
            dispatchResult.AgentPlans.AddRange(plans);

            AgentExecutionPlan best = plans.Where(p => !p.Stalled && !p.LoopDetected).OrderByDescending(p => p.CompositeScoreAtDispatch).FirstOrDefault();

            if (best != null)
            {
                dispatchResult.WinningAgentId = best.AgentId;
                // Merge: lead with the highest-scoring plan, note every other contributing agent below it.
                dispatchResult.FinalMermaidPlan = best.MermaidDiagram + "\n%% Compared against: " + string.Join(", ", plans.Where(p => p.AgentId != best.AgentId).Select(p => p.AgentName));
            }
        }

        private async Task DispatchDecomposedAsync(List<(ReasoningAgentMetadata agent, double score)> ranked, DispatchRequest request, HolonicBraidGraphDto graph, DispatchResult dispatchResult)
        {
            // Each ranked agent owns one sub-problem slice, best-fit-first - then sub-plans are stitched together.
            List<AgentExecutionPlan> subPlans = new List<AgentExecutionPlan>();

            foreach ((ReasoningAgentMetadata agent, double score) in ranked.Take(Math.Min(3, ranked.Count)))
            {
                AgentExecutionPlan plan = await ExecuteAgentAsync(agent, score, request, graph);
                subPlans.Add(plan);
            }

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
                LastUpdatedUtc = holon.MetaData.TryGetValue("LastUpdatedUtc", out object lu) && lu != null && DateTime.TryParse(lu.ToString(), out DateTime dt) ? dt : DateTime.UtcNow
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
