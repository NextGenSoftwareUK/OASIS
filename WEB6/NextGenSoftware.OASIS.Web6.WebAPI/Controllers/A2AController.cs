using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// Google A2A (Agent-to-Agent) protocol endpoints. Allows peer agents to discover OASIS capabilities,
    /// submit tasks, poll status, and stream state-transition events.
    /// Priority 4 — A2A Proper Protocol.
    /// </summary>
    [ApiController]
    [Route("a2a")]
    public class A2AController : Web6ControllerBase
    {
        // In-memory task store (replace with Holonic Memory for production persistence)
        internal static readonly ConcurrentDictionary<string, A2ATask> _tasks = new ConcurrentDictionary<string, A2ATask>();

        /// <summary>
        /// Submit a task to the OASIS FAHRN agent.
        /// POST /a2a/tasks/send
        /// </summary>
        [HttpPost("tasks/send")]
        [ProducesResponseType(typeof(A2ATask), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendTask([FromBody] A2ATaskSendRequest req)
        {
            if (req?.Message == null)
                return BadRequest(new { error = "message is required" });

            string taskId = req.Id ?? Guid.NewGuid().ToString();
            string input = req.Message.Parts != null && req.Message.Parts.Count > 0 ? req.Message.Parts[0].Text : "";

            var task = new A2ATask
            {
                Id = taskId,
                State = "working",
                CreatedAtUtc = DateTime.UtcNow
            };

            _tasks[taskId] = task;

            // Run FAHRN solve async; update task state when done
            _ = Task.Run(async () =>
            {
                try
                {
                    var manager = new FahrnSolveManager(AvatarId, OASISDNA);
                    var solveResult = await manager.SolveAsync(new FahrnSolveRequest
                    {
                        Problem = input,
                        AvatarId = AvatarId,
                        ReturnReasoning = true
                    });

                    task.State = solveResult.IsError ? "failed" : "completed";
                    task.Output = solveResult.IsError
                        ? solveResult.Message
                        : solveResult.Result?.Answer ?? "";
                    task.Artifacts = solveResult.IsError ? null : new List<A2AArtifact>
                    {
                        new A2AArtifact
                        {
                            Parts = new List<A2APart> { new A2APart { Text = solveResult.Result?.Answer } },
                            Metadata = new Dictionary<string, object>
                            {
                                ["mermaidPlan"] = solveResult.Result?.MermaidPlan ?? "",
                                ["modeUsed"]    = solveResult.Result?.ModeUsed.ToString() ?? "",
                                ["latencyMs"]   = solveResult.Result?.TotalLatencyMs
                            }
                        }
                    };
                }
                catch (Exception ex)
                {
                    task.State = "failed";
                    task.Output = ex.Message;
                }
                finally
                {
                    task.CompletedAtUtc = DateTime.UtcNow;
                }
            });

            return Ok(task);
        }

        /// <summary>
        /// Get task status.
        /// GET /a2a/tasks/{id}
        /// </summary>
        [HttpGet("tasks/{id}")]
        [ProducesResponseType(typeof(A2ATask), StatusCodes.Status200OK)]
        public IActionResult GetTask(string id)
        {
            if (!_tasks.TryGetValue(id, out var task))
                return NotFound(new { error = $"Task '{id}' not found" });
            return Ok(task);
        }

        /// <summary>
        /// SSE stream of task state transitions.
        /// GET /a2a/tasks/{id}/events
        /// </summary>
        [HttpGet("tasks/{id}/events")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task TaskEvents(string id, CancellationToken ct)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";

            string lastState = null;
            while (!ct.IsCancellationRequested)
            {
                if (_tasks.TryGetValue(id, out var task))
                {
                    if (task.State != lastState)
                    {
                        lastState = task.State;
                        string data = JsonSerializer.Serialize(new { id, state = task.State, output = task.Output, artifacts = task.Artifacts });
                        await Response.WriteAsync($"data: {data}\n\n", ct);
                        await Response.Body.FlushAsync(ct);

                        if (task.State == "completed" || task.State == "failed")
                            break;
                    }
                }
                else
                {
                    await Response.WriteAsync($"data: {{\"error\":\"task not found\"}}\n\n", ct);
                    break;
                }

                await Task.Delay(500, ct);
            }
        }

        /// <summary>
        /// Cancel a running task.
        /// POST /a2a/tasks/{id}/cancel
        /// </summary>
        [HttpPost("tasks/{id}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public IActionResult CancelTask(string id)
        {
            if (_tasks.TryGetValue(id, out var task))
            {
                if (task.State == "working")
                {
                    task.State = "cancelled";
                    task.CompletedAtUtc = DateTime.UtcNow;
                }
                return NoContent();
            }
            return NotFound(new { error = $"Task '{id}' not found" });
        }
    }

    public class A2ATaskSendRequest
    {
        public string Id { get; set; }
        public A2AMessage Message { get; set; }
    }

    public class A2AMessage
    {
        public string Role { get; set; } = "user";
        public List<A2APart> Parts { get; set; } = new List<A2APart>();
    }

    public class A2APart
    {
        public string Text { get; set; }
    }

    public class A2ATask
    {
        public string Id { get; set; }
        public string State { get; set; }
        public string Output { get; set; }
        public List<A2AArtifact> Artifacts { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? CompletedAtUtc { get; set; }
    }

    public class A2AArtifact
    {
        public List<A2APart> Parts { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
    }
}
