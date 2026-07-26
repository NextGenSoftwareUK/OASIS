using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Data;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Web6.Core.Managers
{
    /// <summary>
    /// ML.NET in-process machine learning for WEB6 — provides fast, zero-latency classification and
    /// anomaly detection without external API calls. Priority 25.
    ///
    /// Use cases:
    /// - Task type classification (replaces heuristic in FahrnTaskClassifier)
    /// - Loop anomaly scoring for FAHRN
    /// - Sentiment analysis for content moderation
    /// - Semantic similarity pre-filter for SemanticCacheManager
    /// </summary>
    public class MLNetManager : OASISManager
    {
        private readonly MLContext _ctx = new MLContext(seed: 42);
        private readonly string _modelDir;
        private ITransformer _taskClassifierModel;
        private PredictionEngine<TaskClassifierInput, TaskClassifierOutput> _taskClassifierEngine;

        private static readonly string[] ValidCategories = { "code", "reasoning", "writing", "mathematics", "legal", "architecture", "real-time", "general" };

        public MLNetManager(Guid avatarId, OASISDNA dna = null) : base(avatarId, dna)
        {
            _modelDir = Path.Combine(AppContext.BaseDirectory, "MLModels");
            Directory.CreateDirectory(_modelDir);
            TryLoadTaskClassifier();
        }

        /// <summary>
        /// Classifies the task type of a problem string in-process (no LLM call).
        /// Falls back to keyword heuristic if no trained model exists.
        /// </summary>
        public string ClassifyTaskType(string problemText)
        {
            if (_taskClassifierEngine != null)
            {
                var prediction = _taskClassifierEngine.Predict(new TaskClassifierInput { Text = problemText });
                string predicted = prediction.PredictedLabel?.ToLowerInvariant()?.Trim() ?? "general";
                return ValidCategories.Contains(predicted) ? predicted : "general";
            }
            return HeuristicClassify(problemText);
        }

        /// <summary>
        /// Scores the probability of a loop in a FAHRN dispatch output (0 = normal, 1 = certain loop).
        /// Uses token velocity and growth rate features.
        /// </summary>
        public double ScoreLoopAnomaly(LoopFeatureVector features)
        {
            if (features == null) return 0;
            // Simple heuristic anomaly: rapid token growth + repetition rate triggers high anomaly score
            double velocityScore = Math.Min(features.TokenVelocity / 1000.0, 1.0);
            double repetitionScore = features.RepetitionRate;
            return Math.Min((velocityScore + repetitionScore) / 2.0, 1.0);
        }

        /// <summary>Classifies sentiment: "Positive", "Neutral", or "Negative".</summary>
        public string AnalyseSentiment(string text)
        {
            if (string.IsNullOrEmpty(text)) return "Neutral";
            // Simple lexicon-based fallback until a trained model exists
            var lower = text.ToLowerInvariant();
            int positives = CountKeywords(lower, new[] { "good", "great", "excellent", "amazing", "love", "perfect", "happy", "wonderful", "fantastic", "awesome" });
            int negatives = CountKeywords(lower, new[] { "bad", "terrible", "awful", "hate", "horrible", "wrong", "poor", "broken", "fail", "error" });
            if (positives > negatives + 1) return "Positive";
            if (negatives > positives + 1) return "Negative";
            return "Neutral";
        }

        /// <summary>
        /// Trains the task classifier from a list of (problem, taskType) pairs.
        /// Called automatically from background when sufficient dispatch history exists.
        /// </summary>
        public OASISResult<bool> TrainTaskClassifier(IEnumerable<(string problem, string taskType)> trainingData)
        {
            var result = new OASISResult<bool>();
            try
            {
                var data = trainingData.Select(t => new TaskClassifierInput { Text = t.problem, Label = t.taskType }).ToList();
                if (data.Count < 10) { result.Result = false; result.Message = "Need at least 10 samples"; return result; }

                var dataView = _ctx.Data.LoadFromEnumerable(data);
                var pipeline = _ctx.Transforms.Conversion.MapValueToKey("Label")
                    .Append(_ctx.Transforms.Text.FeaturizeText("Features", nameof(TaskClassifierInput.Text)))
                    .Append(_ctx.MulticlassClassification.Trainers.SdcaMaximumEntropy())
                    .Append(_ctx.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

                _taskClassifierModel = pipeline.Fit(dataView);
                _taskClassifierEngine = _ctx.Model.CreatePredictionEngine<TaskClassifierInput, TaskClassifierOutput>(_taskClassifierModel);

                string modelPath = Path.Combine(_modelDir, "task_classifier.zip");
                _ctx.Model.Save(_taskClassifierModel, dataView.Schema, modelPath);
                result.Result = true;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = ex.Message;
                result.Exception = ex;
            }
            return result;
        }

        private void TryLoadTaskClassifier()
        {
            try
            {
                string modelPath = Path.Combine(_modelDir, "task_classifier.zip");
                if (File.Exists(modelPath))
                {
                    _taskClassifierModel = _ctx.Model.Load(modelPath, out _);
                    _taskClassifierEngine = _ctx.Model.CreatePredictionEngine<TaskClassifierInput, TaskClassifierOutput>(_taskClassifierModel);
                }
            }
            catch { /* no model yet — fall through to heuristic */ }
        }

        private static string HeuristicClassify(string text)
        {
            if (string.IsNullOrEmpty(text)) return "general";
            string lower = text.ToLowerInvariant();
            if (ContainsAny(lower, "function", "code", "class", "method", "algorithm", "debug", "compile", "syntax")) return "code";
            if (ContainsAny(lower, "proof", "theorem", "equation", "integral", "derivative", "calculate", "solve for")) return "mathematics";
            if (ContainsAny(lower, "law", "contract", "legal", "regulation", "compliance", "liability", "statute")) return "legal";
            if (ContainsAny(lower, "design", "architect", "system", "infrastructure", "scalab", "microservice", "diagram")) return "architecture";
            if (ContainsAny(lower, "write", "essay", "story", "blog", "summarise", "summarize", "draft", "compose")) return "writing";
            if (ContainsAny(lower, "latest", "news", "current", "today", "live", "real-time", "stock", "weather")) return "real-time";
            if (ContainsAny(lower, "why", "explain", "reason", "analys", "compare", "evaluat", "think", "consider")) return "reasoning";
            return "general";
        }

        private static bool ContainsAny(string text, params string[] keywords) => keywords.Any(text.Contains);
        private static int CountKeywords(string text, string[] keywords) => keywords.Count(k => text.Contains(k));
    }

    public class TaskClassifierInput
    {
        [LoadColumn(0)] public string Text { get; set; }
        [LoadColumn(1)] public string Label { get; set; }
    }

    public class TaskClassifierOutput
    {
        [ColumnName("PredictedLabel")] public string PredictedLabel { get; set; }
        public float[] Score { get; set; }
    }

    public class LoopFeatureVector
    {
        public double TokenVelocity { get; set; }
        public double RepetitionRate { get; set; }
        public double GraphGrowthRate { get; set; }
    }
}
