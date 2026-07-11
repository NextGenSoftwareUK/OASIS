using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NextGenSoftware.OASIS.Web6.WebAPI.Controllers
{
    /// <summary>
    /// Real-time telemetry stream and history for AI requests.
    /// Priority 19a — Observability telemetry SSE stream.
    /// </summary>
    [ApiController]
    [Route("v1/telemetry")]
    public class TelemetryController : Web6ControllerBase
    {
        // Shared in-process ring buffer — 500 most recent events
        internal static readonly ConcurrentQueue<TelemetryEvent> _events = new ConcurrentQueue<TelemetryEvent>();
        private const int MaxEvents = 500;

        /// <summary>
        /// Publishes a new telemetry event (called internally by CompletionController, FAHRNManager, etc.).
        /// </summary>
        public static void Publish(TelemetryEvent evt)
        {
            _events.Enqueue(evt);
            while (_events.Count > MaxEvents)
                _events.TryDequeue(out _);
        }

        /// <summary>
        /// SSE stream of real-time per-request trace events.
        /// GET /v1/telemetry/stream
        /// </summary>
        [HttpGet("stream")]
        public async Task Stream(CancellationToken ct)
        {
            Response.ContentType = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["X-Accel-Buffering"] = "no";

            int sent = _events.Count; // start from current tail

            while (!ct.IsCancellationRequested)
            {
                var all = _events.ToArray();
                if (all.Length > sent)
                {
                    foreach (var evt in all.Skip(sent))
                    {
                        string data = JsonSerializer.Serialize(evt);
                        await Response.WriteAsync($"data: {data}\n\n", ct);
                        await Response.Body.FlushAsync(ct);
                    }
                    sent = all.Length;
                }
                await Task.Delay(500, ct);
            }
        }

        /// <summary>
        /// Returns the last N telemetry events (default 50, max 500).
        /// GET /v1/telemetry/history?limit=50
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(List<TelemetryEvent>), StatusCodes.Status200OK)]
        public IActionResult History([FromQuery] int limit = 50)
        {
            int take = Math.Min(limit, MaxEvents);
            var events = _events.ToArray().TakeLast(take).ToList();
            return Ok(events);
        }
    }

    public class TelemetryEvent
    {
        public string RequestId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Provider { get; set; }
        public string Model { get; set; }
        public long LatencyMs { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public double EstimatedCostUSD { get; set; }
        public string FahrnMode { get; set; }
        public bool BraidGraphReused { get; set; }
        public string BraidGraphId { get; set; }
        public int AgentsScored { get; set; }
        public string WinningAgent { get; set; }
        public bool AvatarContextInjected { get; set; }
        public bool CacheHit { get; set; }
        public bool LoopDetected { get; set; }
        public Guid AvatarId { get; set; }
    }
}
