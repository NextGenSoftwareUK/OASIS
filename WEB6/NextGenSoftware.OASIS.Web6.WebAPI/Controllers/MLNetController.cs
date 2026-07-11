using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Managers;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// ML.NET in-process machine learning endpoints.
    /// Priority 25 — ML.NET In-Process Machine Learning.
    /// </summary>
    [ApiController]
    [Route("v1/ml")]
    public class MLNetController : Web6ControllerBase
    {
        /// <summary>
        /// Lists deployed in-process models and their capabilities.
        /// GET /v1/ml/models
        /// </summary>
        [HttpGet("models")]
        [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
        public IActionResult ListModels()
        {
            return Ok(new[]
            {
                new { name = "task_classifier",    type = "MulticlassClassification", status = "heuristic_fallback", description = "Classifies FAHRN task type: code/reasoning/writing/mathematics/legal/architecture/real-time/general" },
                new { name = "loop_anomaly",       type = "AnomalyDetection",         status = "heuristic_fallback", description = "Scores FAHRN loop anomaly probability (0=normal, 1=certain loop)" },
                new { name = "sentiment_analysis", type = "Classification",            status = "heuristic_fallback", description = "Classifies text sentiment: Positive/Neutral/Negative" }
            });
        }

        /// <summary>
        /// Classifies the task type of a problem string using the in-process ML.NET model.
        /// POST /v1/ml/classify-task
        /// </summary>
        [HttpPost("classify-task")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult ClassifyTask([FromBody] MLClassifyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Text))
                return BadRequest(new { error = "text is required" });
            var manager = new MLNetManager(AvatarId, OASISDNA);
            string result = manager.ClassifyTaskType(request.Text);
            return Ok(new { taskType = result });
        }

        /// <summary>
        /// Analyses sentiment of the provided text.
        /// POST /v1/ml/sentiment
        /// </summary>
        [HttpPost("sentiment")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult AnalyseSentiment([FromBody] MLClassifyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Text))
                return BadRequest(new { error = "text is required" });
            var manager = new MLNetManager(AvatarId, OASISDNA);
            string result = manager.AnalyseSentiment(request.Text);
            return Ok(new { sentiment = result });
        }

        /// <summary>
        /// Trains the task classifier from a list of labelled problem/taskType pairs.
        /// POST /v1/ml/train
        /// </summary>
        [HttpPost("train")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public IActionResult TrainTaskClassifier([FromBody] MLTrainRequest request)
        {
            if (request?.Samples == null || request.Samples.Count < 10)
                return BadRequest(new { error = "At least 10 samples required" });
            var manager = new MLNetManager(AvatarId, OASISDNA);
            var result = manager.TrainTaskClassifier(request.Samples.ConvertAll(s => (s.Problem, s.TaskType)));
            return result.IsError ? BadRequest(result) : Ok(new { ok = true, message = "Model trained and saved" });
        }
    }

    public class MLClassifyRequest
    {
        public string Text { get; set; }
    }

    public class MLTrainRequest
    {
        public List<MLSample> Samples { get; set; }
    }

    public class MLSample
    {
        public string Problem { get; set; }
        public string TaskType { get; set; }
    }
}
