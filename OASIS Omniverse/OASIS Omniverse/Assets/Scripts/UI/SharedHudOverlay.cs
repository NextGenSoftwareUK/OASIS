using System;
using System.Text;
using System.Threading.Tasks;
using OASIS.Omniverse.UnityHost.API;
using OASIS.Omniverse.UnityHost.Config;
using OASIS.Omniverse.UnityHost.Native;
using OASIS.Omniverse.UnityHost.Runtime;
using UnityEngine;
using UnityEngine.UI;

namespace OASIS.Omniverse.UnityHost.UI
{
    public class SharedHudOverlay : MonoBehaviour
    {
        private enum OmniverseTab
        {
            Inventory,
            Quests,
            Nfts,
            Avatar,
            Karma,
            Settings
        }

        private Canvas _canvas;
        private GameObject _panel;
        private GameObject _contentRoot;
        private GameObject _settingsRoot;
        private Text _contentText;
        private Text _statusText;
        private bool _isVisible;
        private bool _toggleWasDown;
        private bool _hideWasDown;
        private KeyCode _toggleKey = KeyCode.I;
        private KeyCode _hideGameKey = KeyCode.F1;
        private OmniverseTab _currentTab;

        private Slider _masterSlider;
        private Slider _musicSlider;
        private Slider _soundSlider;
        private Slider _voiceSlider;
        private Dropdown _graphicsDropdown;
        private Toggle _fullscreenToggle;
        private InputField _openMenuInput;
        private InputField _hideGameInput;
        private Text _settingsFeedbackText;

        private Web4Web5GatewayClient _apiClient;
        private GlobalSettingsService _settingsService;
        private OmniverseKernel _kernel;

        public void Initialize(OmniverseHostConfig config, Web4Web5GatewayClient apiClient, GlobalSettingsService settingsService, OmniverseKernel kernel)
        {
            _apiClient = apiClient;
            _settingsService = settingsService;
            _kernel = kernel;
            SyncHotkeysFromSettings();
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

            _panel = new GameObject("OmniverseControlCenter");
            _panel.transform.SetParent(_canvas.transform, false);

            var image = _panel.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.82f);

            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.03f, 0.05f);
            rect.anchorMax = new Vector2(0.97f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var title = CreateText("Title", "OASIS Omniverse Control Center", 28, TextAnchor.MiddleLeft, _panel.transform);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.02f, 0.92f);
            titleRect.anchorMax = new Vector2(0.75f, 0.985f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            _statusText = CreateText("Status", "Ready", 16, TextAnchor.MiddleRight, _panel.transform);
            var statusRect = _statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.55f, 0.92f);
            statusRect.anchorMax = new Vector2(0.98f, 0.985f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            var tabs = new GameObject("Tabs");
            tabs.transform.SetParent(_panel.transform, false);
            var tabsRect = tabs.AddComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0.02f, 0.84f);
            tabsRect.anchorMax = new Vector2(0.98f, 0.91f);
            tabsRect.offsetMin = Vector2.zero;
            tabsRect.offsetMax = Vector2.zero;

            CreateTabButton(tabs.transform, "Inventory", OmniverseTab.Inventory, 0);
            CreateTabButton(tabs.transform, "Quests", OmniverseTab.Quests, 1);
            CreateTabButton(tabs.transform, "NFTs", OmniverseTab.Nfts, 2);
            CreateTabButton(tabs.transform, "Avatar", OmniverseTab.Avatar, 3);
            CreateTabButton(tabs.transform, "Karma", OmniverseTab.Karma, 4);
            CreateTabButton(tabs.transform, "Settings", OmniverseTab.Settings, 5);

            _contentRoot = new GameObject("ContentRoot");
            _contentRoot.transform.SetParent(_panel.transform, false);
            var contentRect = _contentRoot.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.02f, 0.06f);
            contentRect.anchorMax = new Vector2(0.98f, 0.82f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            _contentText = CreateText("ContentText", "", 19, TextAnchor.UpperLeft, _contentRoot.transform);
            _contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _contentText.verticalOverflow = VerticalWrapMode.Overflow;
            var textRect = _contentText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.02f, 0.02f);
            textRect.anchorMax = new Vector2(0.98f, 0.98f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _settingsRoot = new GameObject("SettingsRoot");
            _settingsRoot.transform.SetParent(_panel.transform, false);
            var settingsRect = _settingsRoot.AddComponent<RectTransform>();
            settingsRect.anchorMin = new Vector2(0.02f, 0.06f);
            settingsRect.anchorMax = new Vector2(0.98f, 0.82f);
            settingsRect.offsetMin = Vector2.zero;
            settingsRect.offsetMax = Vector2.zero;

            BuildSettingsUi(_settingsRoot.transform);
            _settingsRoot.SetActive(false);
        }

        private void Update()
        {
            var toggleDown = IsHotkeyDown(_toggleKey);
            if (toggleDown && !_toggleWasDown)
            {
                Toggle();
            }

            var hideDown = IsHotkeyDown(_hideGameKey);
            if (hideDown && !_hideWasDown)
            {
                _kernel.HideHostedGames();
            }

            _toggleWasDown = toggleDown;
            _hideWasDown = hideDown;
        }

        private void Toggle()
        {
            SetVisible(!_isVisible);
            if (_isVisible)
            {
                _ = ShowTabAsync(_currentTab);
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

        private async Task ShowTabAsync(OmniverseTab tab)
        {
            _currentTab = tab;
            _statusText.text = $"Loading {tab}...";
            _contentRoot.SetActive(tab != OmniverseTab.Settings);
            _settingsRoot.SetActive(tab == OmniverseTab.Settings);

            switch (tab)
            {
                case OmniverseTab.Inventory:
                    await RenderInventoryAsync();
                    break;
                case OmniverseTab.Quests:
                    await RenderQuestsAsync();
                    break;
                case OmniverseTab.Nfts:
                    await RenderNftsAsync();
                    break;
                case OmniverseTab.Avatar:
                    await RenderAvatarAsync();
                    break;
                case OmniverseTab.Karma:
                    await RenderKarmaAsync();
                    break;
                case OmniverseTab.Settings:
                    RenderSettings();
                    break;
            }

            _statusText.text = $"Viewing {tab}";
        }

        private async Task RenderInventoryAsync()
        {
            var inventoryResult = await _apiClient.GetSharedInventoryAsync();
            var builder = new StringBuilder();
            builder.AppendLine("Shared Inventory");
            builder.AppendLine(new string('-', 64));
            if (inventoryResult.IsError)
            {
                builder.AppendLine($"Inventory Error: {inventoryResult.Message}");
            }
            else
            {
                WriteInventory(builder, inventoryResult.Result);
            }

            _contentText.text = builder.ToString();
        }

        private async Task RenderQuestsAsync()
        {
            var questResult = await _apiClient.GetCrossGameQuestsAsync();
            var builder = new StringBuilder();
            builder.AppendLine("Cross-Game Quests (WEB5 STAR API)");
            builder.AppendLine(new string('-', 64));
            if (questResult.IsError)
            {
                builder.AppendLine($"Quest Error: {questResult.Message}");
            }
            else
            {
                WriteQuests(builder, questResult.Result);
            }

            _contentText.text = builder.ToString();
        }

        private async Task RenderNftsAsync()
        {
            var nftResult = await _apiClient.GetCrossGameNftsAsync();
            var builder = new StringBuilder();
            builder.AppendLine("Cross-Game Assets / NFTs");
            builder.AppendLine(new string('-', 64));
            if (nftResult.IsError)
            {
                builder.AppendLine($"NFT Error: {nftResult.Message}");
            }
            else
            {
                if (nftResult.Result.Count == 0)
                {
                    builder.AppendLine("  (none)");
                }
                else
                {
                    foreach (var nft in nftResult.Result)
                    {
                        builder.AppendLine($"  - {nft.name} [{nft.type}] | {nft.source}");
                        if (!string.IsNullOrWhiteSpace(nft.description))
                        {
                            builder.AppendLine($"      {nft.description}");
                        }
                    }
                }
            }

            _contentText.text = builder.ToString();
        }

        private async Task RenderAvatarAsync()
        {
            var avatar = await _apiClient.GetAvatarProfileAsync();
            var builder = new StringBuilder();
            builder.AppendLine("Avatar Profile");
            builder.AppendLine(new string('-', 64));
            if (avatar.IsError)
            {
                builder.AppendLine($"Avatar Error: {avatar.Message}");
            }
            else
            {
                var p = avatar.Result;
                builder.AppendLine($"ID: {p.id}");
                builder.AppendLine($"Username: {p.username}");
                builder.AppendLine($"Name: {p.firstName} {p.lastName}");
                builder.AppendLine($"Email: {p.email}");
                builder.AppendLine($"Title: {p.title}");
            }

            _contentText.text = builder.ToString();
        }

        private async Task RenderKarmaAsync()
        {
            var karma = await _apiClient.GetKarmaOverviewAsync();
            var builder = new StringBuilder();
            builder.AppendLine("Karma Timeline");
            builder.AppendLine(new string('-', 64));
            if (karma.IsError)
            {
                builder.AppendLine($"Karma Error: {karma.Message}");
            }
            else
            {
                builder.AppendLine($"Total Karma: {karma.Result.totalKarma:0.##}");
                builder.AppendLine();
                builder.AppendLine("History:");
                if (karma.Result.history.Count == 0)
                {
                    builder.AppendLine("  (no records)");
                }
                else
                {
                    foreach (var row in karma.Result.history)
                    {
                        builder.AppendLine($"  - [{row.createdDate}] {row.source} | {row.amount:0.##} | {row.reason}");
                    }
                }
            }

            _contentText.text = builder.ToString();
        }

        private void RenderSettings()
        {
            var s = _settingsService.CurrentSettings;
            _masterSlider.value = s.masterVolume;
            _musicSlider.value = s.musicVolume;
            _soundSlider.value = s.soundVolume;
            _voiceSlider.value = s.voiceVolume;
            _graphicsDropdown.value = GraphicsValue(s.graphicsPreset);
            _fullscreenToggle.isOn = s.fullscreen;
            _openMenuInput.text = s.keyOpenControlCenter;
            _hideGameInput.text = s.keyHideHostedGame;
            _settingsFeedbackText.text = "Update values, then Save & Apply.";
        }

        private void BuildSettingsUi(Transform root)
        {
            _masterSlider = CreateSlider(root, "Master Volume", 0.88f);
            _musicSlider = CreateSlider(root, "Music Volume", 0.74f);
            _soundSlider = CreateSlider(root, "Sound FX Volume", 0.60f);
            _voiceSlider = CreateSlider(root, "Voice Volume", 0.46f);

            CreateText("GraphicsLabel", "Graphics Preset", 18, TextAnchor.MiddleLeft, root, new Vector2(0.02f, 0.33f), new Vector2(0.35f, 0.40f));
            _graphicsDropdown = CreateDropdown(root, new[] { "Low", "Medium", "High", "Ultra", "Custom" }, new Vector2(0.36f, 0.33f), new Vector2(0.58f, 0.40f));

            CreateText("FullscreenLabel", "Fullscreen", 18, TextAnchor.MiddleLeft, root, new Vector2(0.62f, 0.33f), new Vector2(0.78f, 0.40f));
            _fullscreenToggle = CreateToggle(root, new Vector2(0.80f, 0.34f), new Vector2(0.85f, 0.39f));

            CreateText("OpenMenuKeyLabel", "Open Control Center Key", 17, TextAnchor.MiddleLeft, root, new Vector2(0.02f, 0.21f), new Vector2(0.30f, 0.28f));
            _openMenuInput = CreateInputField(root, new Vector2(0.31f, 0.21f), new Vector2(0.45f, 0.28f));

            CreateText("HideHostedKeyLabel", "Hide Hosted Game Key", 17, TextAnchor.MiddleLeft, root, new Vector2(0.50f, 0.21f), new Vector2(0.74f, 0.28f));
            _hideGameInput = CreateInputField(root, new Vector2(0.75f, 0.21f), new Vector2(0.89f, 0.28f));

            var applyButton = CreateButton(root, "Save & Apply", new Vector2(0.70f, 0.06f), new Vector2(0.92f, 0.15f));
            applyButton.onClick.AddListener(() => _ = SaveAndApplySettingsAsync());

            _settingsFeedbackText = CreateText("SettingsFeedback", "", 16, TextAnchor.MiddleLeft, root, new Vector2(0.02f, 0.06f), new Vector2(0.66f, 0.15f));
            _settingsFeedbackText.color = new Color(0.7f, 0.95f, 1f);
        }

        private async Task SaveAndApplySettingsAsync()
        {
            var settings = new OmniverseGlobalSettings
            {
                masterVolume = _masterSlider.value,
                musicVolume = _musicSlider.value,
                soundVolume = _soundSlider.value,
                voiceVolume = _voiceSlider.value,
                graphicsPreset = _graphicsDropdown.options[_graphicsDropdown.value].text,
                fullscreen = _fullscreenToggle.isOn,
                resolution = _settingsService.CurrentSettings.resolution,
                keyOpenControlCenter = _openMenuInput.text.Trim().ToUpperInvariant(),
                keyHideHostedGame = _hideGameInput.text.Trim().ToUpperInvariant()
            };

            var apply = await _kernel.ApplyGlobalSettingsAndRebuildSessionsAsync(settings);
            if (apply.IsError)
            {
                _settingsFeedbackText.text = $"Apply failed: {apply.Message}";
                return;
            }

            SyncHotkeysFromSettings();
            _settingsFeedbackText.text = "Saved + applied to host and cached game sessions.";
        }

        private void SyncHotkeysFromSettings()
        {
            if (_settingsService == null)
            {
                return;
            }

            var openResult = _settingsService.ResolveKeyBinding(_settingsService.CurrentSettings.keyOpenControlCenter, KeyCode.I);
            if (!openResult.IsError)
            {
                _toggleKey = openResult.Result;
            }

            var hideResult = _settingsService.ResolveKeyBinding(_settingsService.CurrentSettings.keyHideHostedGame, KeyCode.F1);
            if (!hideResult.IsError)
            {
                _hideGameKey = hideResult.Result;
            }
        }

        private static int GraphicsValue(string value)
        {
            if (value.Equals("Low", StringComparison.OrdinalIgnoreCase)) return 0;
            if (value.Equals("Medium", StringComparison.OrdinalIgnoreCase)) return 1;
            if (value.Equals("High", StringComparison.OrdinalIgnoreCase)) return 2;
            if (value.Equals("Ultra", StringComparison.OrdinalIgnoreCase)) return 3;
            return 4;
        }

        private static bool IsHotkeyDown(KeyCode keyCode)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var vk = ToVirtualKeyCode(keyCode);
            if (vk > 0)
            {
                return (Win32Interop.GetAsyncKeyState(vk) & 0x8000) != 0;
            }
#endif
            return Input.GetKey(keyCode);
        }

        private static int ToVirtualKeyCode(KeyCode keyCode)
        {
            if (keyCode >= KeyCode.A && keyCode <= KeyCode.Z)
            {
                return 'A' + (keyCode - KeyCode.A);
            }

            if (keyCode >= KeyCode.Alpha0 && keyCode <= KeyCode.Alpha9)
            {
                return '0' + (keyCode - KeyCode.Alpha0);
            }

            switch (keyCode)
            {
                case KeyCode.F1: return 0x70;
                case KeyCode.F2: return 0x71;
                case KeyCode.F3: return 0x72;
                case KeyCode.F4: return 0x73;
                case KeyCode.F5: return 0x74;
                case KeyCode.F6: return 0x75;
                case KeyCode.F7: return 0x76;
                case KeyCode.F8: return 0x77;
                case KeyCode.F9: return 0x78;
                case KeyCode.F10: return 0x79;
                case KeyCode.F11: return 0x7A;
                case KeyCode.F12: return 0x7B;
                case KeyCode.Tab: return 0x09;
                case KeyCode.Escape: return 0x1B;
                case KeyCode.Space: return 0x20;
                default: return 0;
            }
        }

        private void CreateTabButton(Transform parent, string label, OmniverseTab tab, int index)
        {
            var xMin = 0.005f + (index * 0.165f);
            var xMax = xMin + 0.155f;
            var button = CreateButton(parent, label, new Vector2(xMin, 0.05f), new Vector2(xMax, 0.95f));
            button.onClick.AddListener(() => _ = ShowTabAsync(tab));
        }

        private static Button CreateButton(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(text + "_Button");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.18f, 0.28f, 0.95f);
            var button = go.AddComponent<Button>();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var labelText = CreateText(text + "_Label", text, 16, TextAnchor.MiddleCenter, go.transform);
            labelText.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            return button;
        }

        private static Text CreateText(string name, string content, int size, TextAnchor anchor, Transform parent)
        {
            return CreateText(name, content, size, anchor, parent, new Vector2(0f, 0f), new Vector2(1f, 1f));
        }

        private static Text CreateText(string name, string content, int size, TextAnchor anchor, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = content;
            text.alignment = anchor;
            text.fontSize = size;
            text.color = Color.white;
            var rect = text.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return text;
        }

        private static Slider CreateSlider(Transform parent, string label, float yTop)
        {
            CreateText(label + "_Label", label, 18, TextAnchor.MiddleLeft, parent, new Vector2(0.02f, yTop), new Vector2(0.28f, yTop + 0.08f));
            var go = new GameObject(label + "_Slider");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.30f, yTop + 0.015f);
            rect.anchorMax = new Vector2(0.92f, yTop + 0.055f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            var background = new GameObject("Background");
            background.transform.SetParent(go.transform, false);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.2f);
            backgroundRect.anchorMax = new Vector2(1f, 0.8f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.2f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.8f);
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.8f, 1f, 1f);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var handle = new GameObject("Handle");
            handle.transform.SetParent(go.transform, false);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(0.95f, 0.95f, 1f, 1f);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20f, 28f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private static Dropdown CreateDropdown(Transform parent, string[] values, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Dropdown");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.14f, 0.14f, 0.14f, 1f);
            var dropdown = go.AddComponent<Dropdown>();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var label = CreateText("Label", "Select", 16, TextAnchor.MiddleLeft, go.transform, new Vector2(0.07f, 0f), new Vector2(0.9f, 1f));
            dropdown.captionText = label;
            dropdown.options.Clear();
            foreach (var value in values)
            {
                dropdown.options.Add(new Dropdown.OptionData(value));
            }
            return dropdown;
        }

        private static Toggle CreateToggle(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("Toggle");
            go.transform.SetParent(parent, false);
            var toggle = go.AddComponent<Toggle>();
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var bg = new GameObject("Background");
            bg.transform.SetParent(go.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0f);
            bgRect.anchorMax = new Vector2(1f, 1f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            var checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(bg.transform, false);
            var checkImage = checkmark.AddComponent<Image>();
            checkImage.color = new Color(0.2f, 0.8f, 1f);
            var checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.15f, 0.15f);
            checkRect.anchorMax = new Vector2(0.85f, 0.85f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;

            toggle.graphic = checkImage;
            toggle.targetGraphic = bgImage;
            return toggle;
        }

        private static InputField CreateInputField(Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject("InputField");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            var input = go.AddComponent<InputField>();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var text = CreateText("Text", "", 16, TextAnchor.MiddleCenter, go.transform, new Vector2(0.08f, 0f), new Vector2(0.92f, 1f));
            input.textComponent = text;
            input.text = "";
            return input;
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

