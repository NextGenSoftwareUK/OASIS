using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OASIS.Omniverse.UnityHost.API;
using OASIS.Omniverse.UnityHost.Config;
using OASIS.Omniverse.UnityHost.Native;
using UnityEngine;
using UnityEngine.UI;

namespace OASIS.Omniverse.UnityHost.UI
{
    public class SharedHudOverlay : MonoBehaviour
    {
        private const int VkI = 0x49;

        private Canvas _canvas;
        private GameObject _panel;
        private Text _text;
        private bool _isVisible;
        private bool _iWasDown;
        private Web4Web5GatewayClient _apiClient;

        public void Initialize(OmniverseHostConfig config)
        {
            _apiClient = new Web4Web5GatewayClient(config.web4OasisApiBaseUrl, config.web5StarApiBaseUrl, config.apiKey, config.avatarId);
            BuildUi();
            SetVisible(false);
        }

        private void BuildUi()
        {
            _canvas = new GameObject("SharedHudCanvas").AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999;
            _canvas.gameObject.AddComponent<CanvasScaler>();
            _canvas.gameObject.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("InventoryPanel");
            _panel.transform.SetParent(_canvas.transform, false);

            var image = _panel.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.72f);

            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.08f);
            rect.anchorMax = new Vector2(0.95f, 0.92f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var textObject = new GameObject("HudText");
            textObject.transform.SetParent(_panel.transform, false);
            _text = textObject.AddComponent<Text>();
            _text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _text.alignment = TextAnchor.UpperLeft;
            _text.fontSize = 20;
            _text.color = Color.white;
            _text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _text.verticalOverflow = VerticalWrapMode.Overflow;

            var textRect = _text.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.02f, 0.02f);
            textRect.anchorMax = new Vector2(0.98f, 0.98f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private void Update()
        {
            var iPressed = IsKeyDown(VkI);
            if (iPressed && !_iWasDown)
            {
                Toggle();
            }

            _iWasDown = iPressed;
        }

        private bool IsKeyDown(int vk)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            return (Win32Interop.GetAsyncKeyState(vk) & 0x8000) != 0;
#else
            return Input.GetKey(KeyCode.I);
#endif
        }

        private void Toggle()
        {
            SetVisible(!_isVisible);
            if (_isVisible)
            {
                _ = RefreshOverlayAsync();
            }
        }

        private void SetVisible(bool visible)
        {
            _isVisible = visible;
            if (_panel != null)
            {
                _panel.SetActive(visible);
            }
        }

        private async Task RefreshOverlayAsync()
        {
            if (_apiClient == null)
            {
                return;
            }

            var inventoryResult = await _apiClient.GetSharedInventoryAsync();
            var questResult = await _apiClient.GetCrossGameQuestsAsync();

            var builder = new StringBuilder();
            builder.AppendLine("OASIS Shared Inventory + Cross Quests");
            builder.AppendLine("Press I to close");
            builder.AppendLine(new string('-', 64));

            if (inventoryResult.IsError)
            {
                builder.AppendLine($"Inventory Error: {inventoryResult.Message}");
            }
            else
            {
                WriteInventory(builder, inventoryResult.Result);
            }

            builder.AppendLine();
            if (questResult.IsError)
            {
                builder.AppendLine($"Quest Error: {questResult.Message}");
            }
            else
            {
                WriteQuests(builder, questResult.Result);
            }

            _text.text = builder.ToString();
        }

        private static void WriteInventory(StringBuilder builder, List<InventoryItem> items)
        {
            builder.AppendLine("Inventory Items:");
            if (items == null || items.Count == 0)
            {
                builder.AppendLine("  (empty)");
                return;
            }

            foreach (var item in items)
            {
                builder.AppendLine($"  - {item.name} [{item.itemType}] from {item.source}");
            }
        }

        private static void WriteQuests(StringBuilder builder, List<QuestItem> quests)
        {
            builder.AppendLine("Cross-Game Quests:");
            if (quests == null || quests.Count == 0)
            {
                builder.AppendLine("  (none)");
                return;
            }

            foreach (var quest in quests)
            {
                builder.AppendLine($"  - {quest.name} ({quest.status})");
            }
        }

        private void OnDestroy()
        {
            _apiClient?.Dispose();
        }
    }
}

