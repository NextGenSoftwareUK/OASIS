using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.Core.Managers
{
    /// <summary>
    /// SkillOpt — self-evolving agent skill documents (based on Microsoft Research arXiv:2605.23904).
    /// Treats the agent's natural-language skill document as the optimisation target: an optimiser model
    /// proposes bounded textual edits; a validation gate accepts only edits that improve held-out performance.
    /// +23.5% avg gain demonstrated across GPT-5.5 on SearchQA, SpreadsheetBench, OfficeQA, etc.
    /// Priority 24.
    /// </summary>
    public class SkillOptManager : OASISManager
    {
        private readonly AIProviderManager _providerManager;

        public SkillOptManager(Guid avatarId, OASISDNA dna = null) : base(avatarId, dna)
        {
            _providerManager = new AIProviderManager(avatarId, dna);
        }

        /// <summary>
        /// Runs one SkillOpt epoch for the given agent + task category:
        /// collect rollouts → split success/failure → optimiser proposes edits →
        /// validate on held-out set → accept only if selection score improves.
        /// </summary>
        public async Task<OASISResult<SkillDocument>> RunEpochAsync(Guid agentHolonId, string taskCategory)
        {
            var result = new OASISResult<SkillDocument>();
            try
            {
                // Load current best skill
                var currentSkill = await LoadSkillAsync(agentHolonId, taskCategory);
                string currentContent = currentSkill.Result?.Content ?? $"# Default skill for {taskCategory}\n\nApproach problems methodically and return a clear, concise answer.";

                // Load recent rollout records from the agent holon's metadata
                var rollouts = await LoadRolloutsAsync(agentHolonId, taskCategory);
                if (rollouts.Count < 3)
                {
                    result.Result = currentSkill.Result ?? new SkillDocument { TaskCategory = taskCategory, Content = currentContent };
                    result.Message = "Insufficient rollouts for SkillOpt epoch (need at least 3)";
                    return result;
                }

                var successes = rollouts.Where(r => r.Score >= 0.7).Take(5).ToList();
                var failures  = rollouts.Where(r => r.Score < 0.7).Take(5).ToList();

                // Call optimiser model to propose edits
                string optimiserPrompt =
                    $"You are a skill document optimiser. The current skill document is:\n\n{currentContent}\n\n" +
                    $"Recent FAILURES (problems where the skill underperformed):\n{FormatRollouts(failures)}\n\n" +
                    $"Recent SUCCESSES:\n{FormatRollouts(successes)}\n\n" +
                    $"Propose a bounded edit to the skill document that addresses the failures without breaking the successes. " +
                    $"Make at most 3 line changes (add/replace/delete). Return only the updated skill document — no explanation.";

                var optimiserRequest = new CompletionRequest
                {
                    Provider = "auto",
                    MaxTokens = 500,
                    Messages = new List<ChatMessage>
                    {
                        new ChatMessage { Role = "user", Content = optimiserPrompt }
                    }
                };

                var optimiserResult = await _providerManager.CompleteAsync(optimiserRequest);
                if (optimiserResult.IsError || string.IsNullOrEmpty(optimiserResult.Result?.Content))
                {
                    result.IsError = true;
                    result.Message = "Optimiser model failed to produce a candidate skill";
                    return result;
                }

                string candidateContent = optimiserResult.Result.Content.Trim();

                // Validate: score candidate on held-out problems
                double currentScore = await EvaluateSkillAsync(currentContent, taskCategory, rollouts.TakeLast(3).ToList());
                double candidateScore = await EvaluateSkillAsync(candidateContent, taskCategory, rollouts.TakeLast(3).ToList());

                var skill = currentSkill.Result ?? new SkillDocument();
                skill.TaskCategory = taskCategory;
                skill.EpochsRun = (skill.EpochsRun) + 1;

                if (candidateScore > currentScore)
                {
                    skill.SlowUpdateContent = skill.Content; // checkpoint previous
                    skill.Content = candidateContent;
                    skill.SelectionScore = candidateScore;
                    await SaveSkillAsync(agentHolonId, taskCategory, skill);
                }
                else
                {
                    skill.RejectedEdits ??= new List<RejectedEdit>();
                    skill.RejectedEdits.Add(new RejectedEdit { Content = candidateContent, Score = candidateScore, RejectedAtUtc = DateTime.UtcNow });
                }

                result.Result = skill;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"SkillOpt epoch failed: {ex.Message}";
                result.Exception = ex;
            }
            return result;
        }

        /// <summary>Loads the current best skill document for this agent + task category.</summary>
        public async Task<OASISResult<SkillDocument>> LoadSkillAsync(Guid agentHolonId, string taskCategory)
        {
            var result = new OASISResult<SkillDocument>();
            try
            {
                var holon = await Data.LoadHolonAsync(agentHolonId, false);
                if (!holon.IsError && holon.Result?.MetaData != null)
                {
                    string key = $"SkillOpt_{taskCategory}";
                    if (holon.Result.MetaData.TryGetValue(key, out object stored) && stored != null)
                    {
                        result.Result = System.Text.Json.JsonSerializer.Deserialize<SkillDocument>(stored.ToString()) ?? new SkillDocument { TaskCategory = taskCategory };
                        return result;
                    }
                }
                result.Result = null; // no skill yet
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }
            return result;
        }

        /// <summary>Saves the skill document as metadata on the agent holon.</summary>
        public async Task<OASISResult<SkillDocument>> SaveSkillAsync(Guid agentHolonId, string taskCategory, SkillDocument skill)
        {
            var result = new OASISResult<SkillDocument>();
            try
            {
                var holon = await Data.LoadHolonAsync(agentHolonId, false);
                if (holon.IsError) { result.IsError = true; result.Message = holon.Message; return result; }

                string key = $"SkillOpt_{taskCategory}";
                holon.Result.MetaData[key] = System.Text.Json.JsonSerializer.Serialize(skill);
                await Data.SaveHolonAsync(holon.Result, false);
                result.Result = skill;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }
            return result;
        }

        private async Task<double> EvaluateSkillAsync(string skillContent, string taskCategory, List<RolloutRecord> holdout)
        {
            if (holdout.Count == 0) return 0.5;
            double total = 0;
            foreach (var rollout in holdout)
            {
                var req = new CompletionRequest
                {
                    Provider = "auto",
                    MaxTokens = 200,
                    Messages = new List<ChatMessage>
                    {
                        new ChatMessage { Role = "system", Content = $"[AGENT SKILL FOR {taskCategory}]\n{skillContent}" },
                        new ChatMessage { Role = "user",   Content = rollout.Problem }
                    }
                };
                var r = await _providerManager.CompleteAsync(req);
                // Simple scoring: compare response similarity to the known-good answer
                double score = r.IsError ? 0 : CosineLike(r.Result?.Content ?? "", rollout.ExpectedAnswer ?? "");
                total += score;
            }
            return total / holdout.Count;
        }

        private static double CosineLike(string a, string b)
        {
            if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0;
            var aWords = new HashSet<string>(a.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
            var bWords = new HashSet<string>(b.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
            int intersection = aWords.Intersect(bWords).Count();
            return intersection / (double)Math.Max(aWords.Count, bWords.Count);
        }

        private async Task<List<RolloutRecord>> LoadRolloutsAsync(Guid agentHolonId, string taskCategory)
        {
            try
            {
                var holon = await Data.LoadHolonAsync(agentHolonId, false);
                if (!holon.IsError && holon.Result?.MetaData != null)
                {
                    string key = $"Rollouts_{taskCategory}";
                    if (holon.Result.MetaData.TryGetValue(key, out object stored) && stored != null)
                        return System.Text.Json.JsonSerializer.Deserialize<List<RolloutRecord>>(stored.ToString()) ?? new List<RolloutRecord>();
                }
            }
            catch { }
            return new List<RolloutRecord>();
        }

        private static string FormatRollouts(IEnumerable<RolloutRecord> rollouts)
        {
            return string.Join("\n", rollouts.Select((r, i) => $"{i + 1}. [{r.Score:F2}] {r.Problem}"));
        }
    }

    public class SkillDocument
    {
        public string TaskCategory { get; set; }
        public string Content { get; set; }
        public double SelectionScore { get; set; }
        public int EpochsRun { get; set; }
        public List<RejectedEdit> RejectedEdits { get; set; } = new List<RejectedEdit>();
        public string SlowUpdateContent { get; set; }
    }

    public class RejectedEdit
    {
        public string Content { get; set; }
        public double Score { get; set; }
        public DateTime RejectedAtUtc { get; set; }
    }

    public class RolloutRecord
    {
        public string Problem { get; set; }
        public string ExpectedAnswer { get; set; }
        public double Score { get; set; }
    }
}
