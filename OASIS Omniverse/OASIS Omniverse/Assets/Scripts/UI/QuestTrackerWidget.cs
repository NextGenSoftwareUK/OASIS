using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using OASIS.Omniverse.UnityHost.API;
using OASIS.Omniverse.UnityHost.Config;
using OASIS.Omniverse.UnityHost.Core;
using OASIS.Omniverse.UnityHost.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace OASIS.Omniverse.UnityHost.UI
{
    public class QuestTrackerWidget : MonoBehaviour
    {
        private enum TrackerMode
        {
            Detailed,
            CompactProgress
        }

        private const string QuestTrackerPanelId = "quest_tracker";

        private Canvas _canvas;
        private GameObject _panel;
        private Text _text;
        private bool _isRefreshing;
        private float _nextRefreshTime;
        private readonly List<QuestItem> _questCache = new List<QuestItem>();

        private Web4Web5GatewayClient _apiClient;
        private GlobalSettingsService _settingsService;
        private OmniverseKernel _kernel;
        private KeyCode _toggleKey = KeyCode.BackQuote;
        private KeyCode _modeToggleKey = KeyCode.Equals;
        private bool _toggleWasDown;
        private bool _modeToggleWasDown;
        private TrackerMode _mode = TrackerMode.Detailed;
        private Coroutine _layoutTween;

        public void Initialize(Web4Web5GatewayClient apiClient, GlobalSettingsService settingsService, OmniverseKernel kernel)
        {
            _apiClient = apiClient;
            _settingsService = settingsService;
            _kernel = kernel;
            BuildUi();
            _nextRefreshTime = Time.unscaledTime;
        }

        private void BuildUi()
        {
            _canvas = new GameObject("QuestTrackerCanvas").AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9998;
            _canvas.gameObject.AddComponent<CanvasScaler>();
            _canvas.gameObject.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("QuestTrackerPanel");
            _panel.transform.SetParent(_canvas.transform, false);
            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.62f);

            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(Mathf.Max(30f, Screen.width - 430f), -30f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 400f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 250f);
            ApplySavedPanelLayout(rect);

            var dragResize = _panel.AddComponent<DraggableResizablePanel>();
            dragResize.SetMinSize(260f, 180f);
            dragResize.OnLayoutCommitted += panelRect => _ = PersistPanelLayoutAsync(panelRect);

            _text = new GameObject("QuestTrackerText").AddComponent<Text>();
            _text.transform.SetParent(_panel.transform, false);
            _text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _text.fontSize = 16;
            _text.alignment = TextAnchor.UpperLeft;
            _text.color = new Color(0.86f, 0.97f, 1f, 1f);
            _text.supportRichText = true;
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            var textRect = _text.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.04f, 0.04f);
            textRect.anchorMax = new Vector2(0.96f, 0.96f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _text.text = "Quest Tracker\nLoading...";
        }

        private void Update()
        {
            var toggleDown = Input.GetKey(_toggleKey);
            if (toggleDown && !_toggleWasDown)
            {
                _panel.SetActive(!_panel.activeSelf);
            }
            _toggleWasDown = toggleDown;

            var modeDown = Input.GetKey(_modeToggleKey);
            if (modeDown && !_modeToggleWasDown)
            {
                _mode = _mode == TrackerMode.Detailed ? TrackerMode.CompactProgress : TrackerMode.Detailed;
                Render();
            }
            _modeToggleWasDown = modeDown;

            if (Time.unscaledTime >= _nextRefreshTime)
            {
                _nextRefreshTime = Time.unscaledTime + 20f;
                _ = RefreshAsync();
            }
        }

        private async Task RefreshAsync()
        {
            if (_isRefreshing || _apiClient == null)
            {
                return;
            }

            _isRefreshing = true;
            try
            {
                var questResult = await _apiClient.GetCrossGameQuestsAsync();
                if (questResult.IsError)
                {
                    _text.text = $"Quest Tracker\nError: {questResult.Message}";
                    return;
                }

                _questCache.Clear();
                if (questResult.Result != null)
                {
                    _questCache.AddRange(questResult.Result);
                }

                Render();
            }
            finally
            {
                _isRefreshing = false;
            }
        }

        private void Render()
        {
            var active = _questCache
                .Where(q => !string.IsNullOrWhiteSpace(q.name))
                .OrderBy(q => string.IsNullOrWhiteSpace(q.status) ? 1 : 0)
                .ThenBy(q => q.status)
                .Take(4)
                .ToList();

            var builder = new StringBuilder();
            builder.AppendLine("Cross-Quest Tracker");
            builder.AppendLine(DateTime.Now.ToString("HH:mm:ss"));
            builder.AppendLine($"Mode: {_mode}");
            builder.AppendLine(new string('-', 26));

            if (active.Count == 0)
            {
                builder.AppendLine("No active quests.");
            }
            else
            {
                if (_mode == TrackerMode.Detailed)
                {
                    foreach (var quest in active)
                    {
                        var statusColor = StatusColorHex(quest.status);
                        builder.AppendLine($"- <color={statusColor}>{quest.name}</color>");
                        builder.AppendLine($"  Status: <color={statusColor}>{quest.status}</color>");
                        if (quest.objectivesTotal > 0)
                        {
                            builder.AppendLine($"  Objectives: {quest.objectivesCompleted}/{quest.objectivesTotal}");
                        }
                        if (!string.IsNullOrWhiteSpace(quest.gameSource) || !string.IsNullOrWhiteSpace(quest.priority))
                        {
                            builder.AppendLine($"  Source: {quest.gameSource} | Priority: {quest.priority}");
                        }
                        if (!string.IsNullOrWhiteSpace(quest.description))
                        {
                            builder.AppendLine($"  {quest.description}");
                        }
                    }
                }
                else
                {
                    foreach (var quest in active)
                    {
                        var progress = EstimateProgress(quest);
                        var bar = BuildProgressBar(progress);
                        var statusColor = StatusColorHex(quest.status);
                        builder.AppendLine($"- <color={statusColor}>{quest.name}</color>");
                        builder.AppendLine($"  {bar} {Mathf.RoundToInt(progress * 100f)}%");
                    }
                }
            }

            builder.AppendLine();
            builder.AppendLine("` to toggle tracker");
            builder.AppendLine("= to switch mode");
            _text.text = builder.ToString();
        }

        private static float EstimateProgress(QuestItem quest)
        {
            if (quest == null)
            {
                return 0f;
            }

            if (quest.progress > 0f)
            {
                return Mathf.Clamp01(quest.progress);
            }

            if (quest.objectivesTotal > 0)
            {
                return Mathf.Clamp01(quest.objectivesCompleted / (float)quest.objectivesTotal);
            }

            var status = (quest.status ?? string.Empty).ToLowerInvariant();
            if (status.Contains("complete")) return 1f;
            if (status.Contains("failed")) return 0f;
            if (status.Contains("progress") || status.Contains("active")) return 0.5f;

            if (!string.IsNullOrWhiteSpace(quest.description))
            {
                var match = Regex.Match(quest.description, @"(\d+)\s*/\s*(\d+)");
                if (match.Success &&
                    int.TryParse(match.Groups[1].Value, out var done) &&
                    int.TryParse(match.Groups[2].Value, out var total) &&
                    total > 0)
                {
                    return Mathf.Clamp01(done / (float)total);
                }
            }

            return 0.1f;
        }

        private static string BuildProgressBar(float progress)
        {
            var clamped = Mathf.Clamp01(progress);
            const int blocks = 12;
            var filled = Mathf.RoundToInt(clamped * blocks);
            return "[" + new string('#', filled) + new string('-', blocks - filled) + "]";
        }

        private static string StatusColorHex(string status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return "#CCCCCC";
            }

            var s = status.ToLowerInvariant();
            if (s.Contains("complete")) return "#7BFF7B";
            if (s.Contains("progress") || s.Contains("active") || s.Contains("started")) return "#FFD86B";
            if (s.Contains("failed") || s.Contains("blocked")) return "#FF7B7B";
            return "#CCCCCC";
        }

        public OASISResult<bool> SetToggleKey(KeyCode keyCode)
        {
            _toggleKey = keyCode;
            return OASISResult<bool>.Success(true, $"Quest tracker toggle key set to {keyCode}.");
        }

        public OASISResult<OmniversePanelLayout> GetCurrentLayout()
        {
            return CaptureCurrentLayout();
        }

        public OASISResult<OmniversePanelLayout> ResetLayoutToDefault()
        {
            if (_panel == null)
            {
                return OASISResult<OmniversePanelLayout>.Error("Quest tracker panel is not initialized.");
            }

            var rect = _panel.GetComponent<RectTransform>();
            var layout = GetDefaultLayout();
            ApplyLayout(rect, layout);
            return CaptureCurrentLayout();
        }

        public OASISResult<OmniversePanelLayout> ApplyLayoutPreset(string presetName)
        {
            if (_panel == null)
            {
                return OASISResult<OmniversePanelLayout>.Error("Quest tracker panel is not initialized.");
            }

            var rect = _panel.GetComponent<RectTransform>();
            var layoutResult = BuildLayoutPreset(presetName);
            if (layoutResult.IsError)
            {
                return layoutResult;
            }
            var layout = layoutResult.Result;

            ApplyLayout(rect, layout);
            return CaptureCurrentLayout();
        }

        public async Task<OASISResult<OmniversePanelLayout>> ApplyLayoutPresetAnimatedAsync(string presetName, float durationSeconds = 0.22f)
        {
            if (_panel == null)
            {
                return OASISResult<OmniversePanelLayout>.Error("Quest tracker panel is not initialized.");
            }

            var layoutResult = BuildLayoutPreset(presetName);
            if (layoutResult.IsError)
            {
                return layoutResult;
            }

            var rect = _panel.GetComponent<RectTransform>();
            await AnimateLayoutAsync(rect, layoutResult.Result, durationSeconds);
            return CaptureCurrentLayout();
        }

        public async Task<OASISResult<OmniversePanelLayout>> ResetLayoutToDefaultAnimatedAsync(float durationSeconds = 0.22f)
        {
            if (_panel == null)
            {
                return OASISResult<OmniversePanelLayout>.Error("Quest tracker panel is not initialized.");
            }

            var rect = _panel.GetComponent<RectTransform>();
            await AnimateLayoutAsync(rect, GetDefaultLayout(), durationSeconds);
            return CaptureCurrentLayout();
        }

        private void ApplySavedPanelLayout(RectTransform rect)
        {
            if (rect == null || _settingsService == null)
            {
                return;
            }

            var layout = (_settingsService.CurrentSettings.panelLayouts ?? new List<OmniversePanelLayout>())
                .FirstOrDefault(x => string.Equals(x.panelId, QuestTrackerPanelId, StringComparison.OrdinalIgnoreCase));
            if (layout == null)
            {
                return;
            }

            rect.anchoredPosition = new Vector2(layout.anchoredX, layout.anchoredY);
            if (layout.width > 100f)
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, layout.width);
            }
            if (layout.height > 100f)
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, layout.height);
            }
        }

        private async Task PersistPanelLayoutAsync(RectTransform rect)
        {
            if (_settingsService == null || _kernel == null || rect == null)
            {
                return;
            }

            var clone = _settingsService.CloneCurrentSettings();
            if (clone.IsError)
            {
                return;
            }

            var settings = clone.Result;
            settings.panelLayouts ??= new List<OmniversePanelLayout>();
            settings.panelLayouts.RemoveAll(x => string.Equals(x.panelId, QuestTrackerPanelId, StringComparison.OrdinalIgnoreCase));
            settings.panelLayouts.Add(new OmniversePanelLayout
            {
                panelId = QuestTrackerPanelId,
                anchoredX = rect.anchoredPosition.x,
                anchoredY = rect.anchoredPosition.y,
                width = rect.rect.width,
                height = rect.rect.height
            });

            await _kernel.SaveUiPreferencesAsync(settings);
        }

        private OASISResult<OmniversePanelLayout> CaptureCurrentLayout()
        {
            if (_panel == null)
            {
                return OASISResult<OmniversePanelLayout>.Error("Quest tracker panel is not initialized.");
            }

            var rect = _panel.GetComponent<RectTransform>();
            return OASISResult<OmniversePanelLayout>.Success(new OmniversePanelLayout
            {
                panelId = QuestTrackerPanelId,
                anchoredX = rect.anchoredPosition.x,
                anchoredY = rect.anchoredPosition.y,
                width = rect.rect.width,
                height = rect.rect.height
            });
        }

        private static void ApplyLayout(RectTransform rect, OmniversePanelLayout layout)
        {
            if (rect == null || layout == null)
            {
                return;
            }

            rect.anchoredPosition = new Vector2(layout.anchoredX, layout.anchoredY);
            if (layout.width > 100f)
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, layout.width);
            }
            if (layout.height > 100f)
            {
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, layout.height);
            }
        }

        private static OmniversePanelLayout GetDefaultLayout()
        {
            return new OmniversePanelLayout
            {
                panelId = QuestTrackerPanelId,
                anchoredX = Mathf.Max(30f, Screen.width - 430f),
                anchoredY = -30f,
                width = 400f,
                height = 250f
            };
        }

        private OASISResult<OmniversePanelLayout> BuildLayoutPreset(string presetName)
        {
            var current = CaptureCurrentLayout();
            if (current.IsError)
            {
                return current;
            }

            var layout = current.Result;
            var width = Mathf.Max(200f, layout.width);
            var height = Mathf.Max(140f, layout.height);
            var margin = 30f;

            switch ((presetName ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "topleft":
                    layout.anchoredX = margin;
                    layout.anchoredY = -margin;
                    break;
                case "topright":
                    layout.anchoredX = Mathf.Max(margin, Screen.width - width - margin);
                    layout.anchoredY = -margin;
                    break;
                case "center":
                    layout.anchoredX = Mathf.Max(0f, (Screen.width - width) * 0.5f);
                    layout.anchoredY = -Mathf.Max(0f, (Screen.height - height) * 0.5f);
                    break;
                default:
                    return OASISResult<OmniversePanelLayout>.Error($"Unknown layout preset '{presetName}'.");
            }

            return OASISResult<OmniversePanelLayout>.Success(layout);
        }

        private async Task AnimateLayoutAsync(RectTransform rect, OmniversePanelLayout target, float duration)
        {
            if (rect == null || target == null)
            {
                return;
            }

            if (_layoutTween != null)
            {
                StopCoroutine(_layoutTween);
                _layoutTween = null;
            }

            var tcs = new TaskCompletionSource<bool>();
            _layoutTween = StartCoroutine(AnimateLayoutCoroutine(rect, target, Mathf.Max(0.05f, duration), () => tcs.TrySetResult(true)));
            await tcs.Task;
            _layoutTween = null;
        }

        private static System.Collections.IEnumerator AnimateLayoutCoroutine(RectTransform rect, OmniversePanelLayout target, float duration, Action onComplete)
        {
            var startPos = rect.anchoredPosition;
            var startSize = rect.rect.size;
            var endPos = new Vector2(target.anchoredX, target.anchoredY);
            var endSize = new Vector2(Mathf.Max(100f, target.width), Mathf.Max(100f, target.height));
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var eased = Mathf.SmoothStep(0f, 1f, t);
                rect.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, eased);
                var size = Vector2.LerpUnclamped(startSize, endSize, eased);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
                yield return null;
            }

            rect.anchoredPosition = endPos;
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, endSize.x);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, endSize.y);
            onComplete?.Invoke();
        }
    }
}

