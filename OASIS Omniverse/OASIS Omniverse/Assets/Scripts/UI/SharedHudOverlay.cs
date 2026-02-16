using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OASIS.Omniverse.UnityHost.API;
using OASIS.Omniverse.UnityHost.Config;
using OASIS.Omniverse.UnityHost.Native;
using OASIS.Omniverse.UnityHost.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
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

        private enum ToastSeverity
        {
            Success,
            Warning,
            Error
        }

        private const int PageSize = 10;
        private const string ControlCenterPanelId = "control_center";
        private const float SnapAnimationDuration = 0.22f;
        private const int MaxVisibleToasts = 3;
        private const float ToastHeight = 42f;
        private const float ToastWidth = 560f;
        private const float ToastSpacing = 8f;

        [Serializable]
        private class PresetExportPackage
        {
            public string schema = "oasis.omniverse.viewpresets";
            public int schemaVersion = 1;
            public string exportedAtUtc;
            public List<OmniverseViewPreset> viewPresets = new List<OmniverseViewPreset>();
            public List<OmniverseActiveViewPreset> activeViewPresets = new List<OmniverseActiveViewPreset>();
        }

        private sealed class ToastEntry
        {
            public GameObject panel;
            public Image background;
            public Text text;
            public float expireAtRealtime;
        }

        private Canvas _canvas;
        private GameObject _panel;
        private GameObject _contentRoot;
        private GameObject _settingsRoot;
        private GameObject _listControlsRoot;
        private RectTransform _toastRoot;
        private Text _contentText;
        private Text _statusText;
        private InputField _searchInput;
        private Dropdown _sortFieldDropdown;
        private Dropdown _sortDirectionDropdown;
        private Dropdown _presetDropdown;
        private Dropdown _templateDropdown;
        private InputField _presetNameInput;
        private Text _pageIndicator;
        private bool _isVisible;
        private bool _toggleWasDown;
        private bool _hideWasDown;
        private KeyCode _toggleKey = KeyCode.I;
        private KeyCode _hideGameKey = KeyCode.F1;
        private OmniverseTab _currentTab;
        private int _currentPage;
        private bool _isRefreshing;
        private bool _suppressPresetEvents;
        private Coroutine _controlCenterTween;
        private readonly List<ToastEntry> _activeToasts = new List<ToastEntry>();

        private Slider _masterSlider;
        private Slider _musicSlider;
        private Slider _soundSlider;
        private Slider _voiceSlider;
        private Dropdown _graphicsDropdown;
        private Toggle _fullscreenToggle;
        private InputField _openMenuInput;
        private InputField _hideGameInput;
        private Text _settingsFeedbackText;
        private Button _layoutResetButton;
        private Button _controlTopLeftButton;
        private Button _controlTopRightButton;
        private Button _controlCenterButton;
        private Button _trackerTopLeftButton;
        private Button _trackerTopRightButton;
        private Button _trackerCenterButton;

        private readonly List<InventoryItem> _inventoryCache = new List<InventoryItem>();
        private readonly List<QuestItem> _questCache = new List<QuestItem>();
        private readonly List<NftAssetItem> _nftCache = new List<NftAssetItem>();
        private readonly List<KarmaEntry> _karmaCache = new List<KarmaEntry>();
        private AvatarProfileItem _avatarCache;
        private float _karmaTotal;

        private Web4Web5GatewayClient _apiClient;
        private GlobalSettingsService _settingsService;
        private OmniverseKernel _kernel;

        private readonly Dictionary<OmniverseTab, string[]> _sortOptions = new Dictionary<OmniverseTab, string[]>
        {
            { OmniverseTab.Inventory, new[] { "Name", "Type", "Source" } },
            { OmniverseTab.Quests, new[] { "Name", "Status", "Priority" } },
            { OmniverseTab.Nfts, new[] { "Name", "Type", "Source" } },
            { OmniverseTab.Karma, new[] { "Date", "Source", "Amount" } },
            { OmniverseTab.Avatar, new[] { "Name" } },
            { OmniverseTab.Settings, new[] { "Name" } }
        };

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
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(30f, -30f);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, Mathf.Max(900f, Screen.width * 0.90f));
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(560f, Screen.height * 0.90f));
            ApplySavedPanelLayout(rect, ControlCenterPanelId);

            var dragResize = _panel.AddComponent<DraggableResizablePanel>();
            dragResize.SetMinSize(760f, 420f);
            dragResize.OnLayoutCommitted += panelRect => _ = PersistPanelLayoutAsync(ControlCenterPanelId, panelRect);

            var title = CreateText("Title", "OASIS Omniverse Control Center", 28, TextAnchor.MiddleLeft, _panel.transform);
            SetAnchors(title.rectTransform, 0.02f, 0.92f, 0.75f, 0.985f);

            _statusText = CreateText("Status", "Ready", 16, TextAnchor.MiddleRight, _panel.transform);
            SetAnchors(_statusText.rectTransform, 0.55f, 0.92f, 0.98f, 0.985f);

            var tabs = new GameObject("Tabs");
            tabs.transform.SetParent(_panel.transform, false);
            var tabsRect = tabs.AddComponent<RectTransform>();
            SetAnchors(tabsRect, 0.02f, 0.84f, 0.98f, 0.91f);

            CreateTabButton(tabs.transform, "Inventory", OmniverseTab.Inventory, 0);
            CreateTabButton(tabs.transform, "Quests", OmniverseTab.Quests, 1);
            CreateTabButton(tabs.transform, "NFTs", OmniverseTab.Nfts, 2);
            CreateTabButton(tabs.transform, "Avatar", OmniverseTab.Avatar, 3);
            CreateTabButton(tabs.transform, "Karma", OmniverseTab.Karma, 4);
            CreateTabButton(tabs.transform, "Settings", OmniverseTab.Settings, 5);

            BuildListControls();

            _contentRoot = new GameObject("ContentRoot");
            _contentRoot.transform.SetParent(_panel.transform, false);
            var contentRect = _contentRoot.AddComponent<RectTransform>();
            SetAnchors(contentRect, 0.02f, 0.06f, 0.98f, 0.69f);

            _contentText = CreateText("ContentText", string.Empty, 19, TextAnchor.UpperLeft, _contentRoot.transform);
            _contentText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _contentText.verticalOverflow = VerticalWrapMode.Overflow;
            SetAnchors(_contentText.rectTransform, 0.02f, 0.02f, 0.98f, 0.98f);

            _settingsRoot = new GameObject("SettingsRoot");
            _settingsRoot.transform.SetParent(_panel.transform, false);
            var settingsRect = _settingsRoot.AddComponent<RectTransform>();
            SetAnchors(settingsRect, 0.02f, 0.06f, 0.98f, 0.82f);
            BuildSettingsUi(_settingsRoot.transform);
            _settingsRoot.SetActive(false);

            BuildToastUi();
        }

        private void BuildToastUi()
        {
            var root = new GameObject("OmniverseToastRoot");
            root.transform.SetParent(_canvas.transform, false);
            _toastRoot = root.AddComponent<RectTransform>();
            _toastRoot.anchorMin = new Vector2(0.5f, 1f);
            _toastRoot.anchorMax = new Vector2(0.5f, 1f);
            _toastRoot.pivot = new Vector2(0.5f, 1f);
            _toastRoot.anchoredPosition = new Vector2(0f, -10f);
            _toastRoot.sizeDelta = new Vector2(ToastWidth, 1f);
        }

        private void BuildListControls()
        {
            _listControlsRoot = new GameObject("ListControls");
            _listControlsRoot.transform.SetParent(_panel.transform, false);
            var controlsRect = _listControlsRoot.AddComponent<RectTransform>();
            SetAnchors(controlsRect, 0.02f, 0.70f, 0.98f, 0.835f);

            CreateText("SearchLabel", "Search", 16, TextAnchor.MiddleLeft, _listControlsRoot.transform, 0.0f, 0f, 0.06f, 1f);
            _searchInput = CreateInputField(_listControlsRoot.transform, 0.065f, 0.56f, 0.26f, 0.94f);
            _searchInput.onValueChanged.AddListener(_ =>
            {
                _currentPage = 0;
                RedrawListTab();
            });

            CreateText("SortByLabel", "Sort", 16, TextAnchor.MiddleLeft, _listControlsRoot.transform, 0.27f, 0f, 0.31f, 1f);
            _sortFieldDropdown = CreateDropdown(_listControlsRoot.transform, new[] { "Name" }, 0.315f, 0.56f, 0.50f, 0.94f);
            _sortFieldDropdown.onValueChanged.AddListener(_ =>
            {
                _currentPage = 0;
                RedrawListTab();
            });

            _sortDirectionDropdown = CreateDropdown(_listControlsRoot.transform, new[] { "Asc", "Desc" }, 0.505f, 0.56f, 0.59f, 0.94f);
            _sortDirectionDropdown.onValueChanged.AddListener(_ =>
            {
                _currentPage = 0;
                RedrawListTab();
            });

            var refreshButton = CreateButton(_listControlsRoot.transform, "Refresh", 0.595f, 0.56f, 0.69f, 0.94f);
            refreshButton.onClick.AddListener(() => _ = RefreshCurrentTabAsync());

            var prevButton = CreateButton(_listControlsRoot.transform, "< Prev", 0.74f, 0.56f, 0.82f, 0.94f);
            prevButton.onClick.AddListener(() =>
            {
                _currentPage = Mathf.Max(0, _currentPage - 1);
                RedrawListTab();
            });

            _pageIndicator = CreateText("PageIndicator", "Page 1/1", 15, TextAnchor.MiddleCenter, _listControlsRoot.transform, 0.83f, 0.56f, 0.92f, 0.94f);

            var nextButton = CreateButton(_listControlsRoot.transform, "Next >", 0.92f, 0.56f, 1.0f, 0.94f);
            nextButton.onClick.AddListener(() =>
            {
                _currentPage += 1;
                RedrawListTab();
            });

            // Preset row
            CreateText("PresetLabel", "Preset", 15, TextAnchor.MiddleLeft, _listControlsRoot.transform, 0.0f, 0.10f, 0.06f, 0.48f);
            _presetDropdown = CreateDropdown(_listControlsRoot.transform, new[] { "(none)" }, 0.065f, 0.10f, 0.24f, 0.48f);
            _presetDropdown.onValueChanged.AddListener(_ =>
            {
                if (_suppressPresetEvents)
                {
                    return;
                }

                _ = ApplySelectedPresetAsync();
            });

            _presetNameInput = CreateInputField(_listControlsRoot.transform, 0.25f, 0.10f, 0.40f, 0.48f);
            _presetNameInput.text = "MyPreset";

            var savePresetButton = CreateButton(_listControlsRoot.transform, "Save Preset", 0.41f, 0.10f, 0.52f, 0.48f);
            savePresetButton.onClick.AddListener(() => _ = SaveCurrentPresetAsync());

            var applyPresetButton = CreateButton(_listControlsRoot.transform, "Apply", 0.53f, 0.10f, 0.60f, 0.48f);
            applyPresetButton.onClick.AddListener(() => _ = ApplySelectedPresetAsync());

            var deletePresetButton = CreateButton(_listControlsRoot.transform, "Delete", 0.61f, 0.10f, 0.68f, 0.48f);
            deletePresetButton.onClick.AddListener(() => _ = DeleteSelectedPresetAsync());

            _templateDropdown = CreateDropdown(_listControlsRoot.transform, new[] { "Select Template" }, 0.69f, 0.10f, 0.84f, 0.48f);

            var applyTemplateButton = CreateButton(_listControlsRoot.transform, "Template", 0.85f, 0.10f, 0.91f, 0.48f);
            applyTemplateButton.onClick.AddListener(() => ApplySelectedTemplate());

            var exportButton = CreateButton(_listControlsRoot.transform, "Export", 0.92f, 0.10f, 0.96f, 0.48f);
            exportButton.onClick.AddListener(ExportPresetsToClipboard);

            var importButton = CreateButton(_listControlsRoot.transform, "Import", 0.96f, 0.10f, 1.0f, 0.48f);
            importButton.onClick.AddListener(() => _ = ImportPresetsFromClipboardAsync());
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

            HandleLayoutHotkeys();
            TickToastQueue();

            _toggleWasDown = toggleDown;
            _hideWasDown = hideDown;
        }

        private void HandleLayoutHotkeys()
        {
            if (!AreLayoutHotkeysEnabled())
            {
                return;
            }

            var ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            var alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            if (!ctrl || !alt)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                _ = ResetAllPanelLayoutsAsync();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                _ = ApplyControlCenterLayoutPresetAsync("TopLeft");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                _ = ApplyControlCenterLayoutPresetAsync("TopRight");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                _ = ApplyControlCenterLayoutPresetAsync("Center");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                _ = ApplyQuestTrackerLayoutPresetAsync("TopLeft");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                _ = ApplyQuestTrackerLayoutPresetAsync("TopRight");
            }
            else if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                _ = ApplyQuestTrackerLayoutPresetAsync("Center");
            }
        }

        private static bool AreLayoutHotkeysEnabled()
        {
            if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null)
            {
                return true;
            }

            var selected = EventSystem.current.currentSelectedGameObject;
            return selected.GetComponent<InputField>() == null;
        }

        private void ShowToast(string message, ToastSeverity severity = ToastSeverity.Success, float durationSeconds = 1.7f)
        {
            if (_toastRoot == null || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            while (_activeToasts.Count >= MaxVisibleToasts)
            {
                DismissToast(_activeToasts[0]);
            }

            var panel = new GameObject("ToastItem");
            panel.transform.SetParent(_toastRoot, false);
            var panelRect = panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.sizeDelta = new Vector2(ToastWidth, ToastHeight);

            var background = panel.AddComponent<Image>();
            var text = CreateText("ToastText", string.Empty, 16, TextAnchor.MiddleCenter, panel.transform);
            SetAnchors(text.rectTransform, 0.02f, 0.05f, 0.98f, 0.95f);

            ApplyToastStyle(text, background, message, severity);

            _activeToasts.Add(new ToastEntry
            {
                panel = panel,
                background = background,
                text = text,
                expireAtRealtime = Time.realtimeSinceStartup + Mathf.Max(0.4f, durationSeconds)
            });

            RelayoutToasts();
        }

        private void TickToastQueue()
        {
            if (_activeToasts.Count == 0)
            {
                return;
            }

            var now = Time.realtimeSinceStartup;
            for (var i = _activeToasts.Count - 1; i >= 0; i--)
            {
                if (now >= _activeToasts[i].expireAtRealtime)
                {
                    DismissToast(_activeToasts[i]);
                }
            }

            RelayoutToasts();
        }

        private void DismissToast(ToastEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            _activeToasts.Remove(entry);
            if (entry.panel != null)
            {
                Destroy(entry.panel);
            }
        }

        private void RelayoutToasts()
        {
            for (var i = 0; i < _activeToasts.Count; i++)
            {
                var entry = _activeToasts[i];
                if (entry?.panel == null)
                {
                    continue;
                }

                var rect = entry.panel.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(0f, -(i * (ToastHeight + ToastSpacing)));
            }
        }

        private static void ApplyToastStyle(Text toastText, Image toastBackground, string message, ToastSeverity severity)
        {
            string icon;
            Color bg;
            Color fg;
            switch (severity)
            {
                case ToastSeverity.Error:
                    icon = "[!]";
                    bg = new Color(0.40f, 0.06f, 0.06f, 0.90f);
                    fg = new Color(1.0f, 0.86f, 0.86f, 1f);
                    break;
                case ToastSeverity.Warning:
                    icon = "[~]";
                    bg = new Color(0.40f, 0.28f, 0.06f, 0.90f);
                    fg = new Color(1.0f, 0.95f, 0.78f, 1f);
                    break;
                default:
                    icon = "[+]";
                    bg = new Color(0.05f, 0.26f, 0.36f, 0.90f);
                    fg = new Color(0.86f, 0.97f, 1f, 1f);
                    break;
            }

            toastBackground.color = bg;
            toastText.color = fg;
            toastText.text = $"{icon} {message}";
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
            _currentPage = 0;
            ConfigureSortOptionsForTab(tab);

            var isSettings = tab == OmniverseTab.Settings;
            _contentRoot.SetActive(!isSettings);
            _settingsRoot.SetActive(isSettings);
            _listControlsRoot.SetActive(!isSettings);

            if (isSettings)
            {
                RenderSettings();
            }
            else
            {
                await RefreshCurrentTabAsync();
            }

            _statusText.text = $"Viewing {tab}";
        }

        private async Task RefreshCurrentTabAsync()
        {
            if (_isRefreshing || _apiClient == null)
            {
                return;
            }

            _isRefreshing = true;
            try
            {
                switch (_currentTab)
                {
                    case OmniverseTab.Inventory:
                    {
                        var result = await _apiClient.GetSharedInventoryAsync();
                        _inventoryCache.Clear();
                        if (!result.IsError && result.Result != null)
                        {
                            _inventoryCache.AddRange(result.Result);
                        }
                        else
                        {
                            _contentText.text = $"Inventory Error: {result.Message}";
                        }
                        break;
                    }
                    case OmniverseTab.Quests:
                    {
                        var result = await _apiClient.GetCrossGameQuestsAsync();
                        _questCache.Clear();
                        if (!result.IsError && result.Result != null)
                        {
                            _questCache.AddRange(result.Result);
                        }
                        else
                        {
                            _contentText.text = $"Quest Error: {result.Message}";
                        }
                        break;
                    }
                    case OmniverseTab.Nfts:
                    {
                        var result = await _apiClient.GetCrossGameNftsAsync();
                        _nftCache.Clear();
                        if (!result.IsError && result.Result != null)
                        {
                            _nftCache.AddRange(result.Result);
                        }
                        else
                        {
                            _contentText.text = $"NFT Error: {result.Message}";
                        }
                        break;
                    }
                    case OmniverseTab.Avatar:
                    {
                        var result = await _apiClient.GetAvatarProfileAsync();
                        _avatarCache = result.IsError ? null : result.Result;
                        if (result.IsError)
                        {
                            _contentText.text = $"Avatar Error: {result.Message}";
                        }
                        break;
                    }
                    case OmniverseTab.Karma:
                    {
                        var result = await _apiClient.GetKarmaOverviewAsync();
                        _karmaCache.Clear();
                        _karmaTotal = 0f;
                        if (!result.IsError && result.Result != null)
                        {
                            _karmaTotal = result.Result.totalKarma;
                            if (result.Result.history != null)
                            {
                                _karmaCache.AddRange(result.Result.history);
                            }
                        }
                        else
                        {
                            _contentText.text = $"Karma Error: {result.Message}";
                        }
                        break;
                    }
                }
            }
            finally
            {
                _isRefreshing = false;
                RedrawListTab();
            }
        }

        private void RedrawListTab()
        {
            if (_currentTab == OmniverseTab.Settings)
            {
                return;
            }

            var query = (_searchInput?.text ?? string.Empty).Trim();
            var builder = new StringBuilder();

            switch (_currentTab)
            {
                case OmniverseTab.Inventory:
                    DrawInventory(builder, query);
                    break;
                case OmniverseTab.Quests:
                    DrawQuests(builder, query);
                    break;
                case OmniverseTab.Nfts:
                    DrawNfts(builder, query);
                    break;
                case OmniverseTab.Avatar:
                    DrawAvatar(builder);
                    break;
                case OmniverseTab.Karma:
                    DrawKarma(builder, query);
                    break;
            }

            _contentText.text = builder.ToString();
        }

        private void DrawInventory(StringBuilder builder, string query)
        {
            builder.AppendLine("Shared Inventory");
            builder.AppendLine(new string('-', 64));
            var filtered = _inventoryCache.Where(x => Matches(query, x.name, x.description, x.itemType, x.source)).ToList();
            filtered = SortInventory(filtered);
            WritePaged(
                builder,
                filtered,
                item => $"{item.name} [{item.itemType}] from <color=#8EEBFF>{item.source}</color>",
                "(empty)");
        }

        private void DrawQuests(StringBuilder builder, string query)
        {
            builder.AppendLine("Cross-Game Quests (WEB5 STAR API)");
            builder.AppendLine(new string('-', 64));
            var filtered = _questCache.Where(x => Matches(query, x.name, x.description, x.status)).ToList();
            filtered = SortQuests(filtered);
            WritePaged(
                builder,
                filtered,
                item =>
                {
                    var statusColor = StatusColorHex(item.status);
                    var priorityColor = PriorityColorHex(item.name, item.description);
                    return $"<color={priorityColor}>{item.name}</color> (<color={statusColor}>{item.status}</color>) - {item.description}";
                },
                "(none)");
        }

        private void DrawNfts(StringBuilder builder, string query)
        {
            builder.AppendLine("Cross-Game Assets / NFTs");
            builder.AppendLine(new string('-', 64));
            var filtered = _nftCache.Where(x => Matches(query, x.name, x.description, x.type, x.source)).ToList();
            filtered = SortNfts(filtered);
            WritePaged(
                builder,
                filtered,
                item => $"{item.name} [{item.type}] | <color=#8EEBFF>{item.source}</color>\n    {item.description}",
                "(none)");
        }

        private void DrawAvatar(StringBuilder builder)
        {
            _pageIndicator.text = "Page 1/1";
            builder.AppendLine("Avatar Profile");
            builder.AppendLine(new string('-', 64));
            if (_avatarCache == null)
            {
                builder.AppendLine("No avatar data loaded.");
                return;
            }

            builder.AppendLine($"ID: {_avatarCache.id}");
            builder.AppendLine($"Username: {_avatarCache.username}");
            builder.AppendLine($"Name: {_avatarCache.firstName} {_avatarCache.lastName}");
            builder.AppendLine($"Email: {_avatarCache.email}");
            builder.AppendLine($"Title: {_avatarCache.title}");
        }

        private void DrawKarma(StringBuilder builder, string query)
        {
            builder.AppendLine("Karma Timeline");
            builder.AppendLine(new string('-', 64));
            builder.AppendLine($"Total Karma: {_karmaTotal:0.##}");
            builder.AppendLine();
            var filtered = _karmaCache.Where(x => Matches(query, x.source, x.reason, x.karmaType, x.createdDate, x.amount.ToString("0.##"))).ToList();
            filtered = SortKarma(filtered);
            WritePaged(
                builder,
                filtered,
                item =>
                {
                    var amountColor = item.amount >= 0 ? "#7BFF7B" : "#FF7B7B";
                    return $"[{item.createdDate}] <color=#8EEBFF>{item.source}</color> | <color={amountColor}>{item.amount:0.##}</color> | {item.reason}";
                },
                "(no records)");
        }

        private void WritePaged<T>(StringBuilder builder, List<T> filtered, Func<T, string> formatter, string emptyText)
        {
            if (filtered.Count == 0)
            {
                _currentPage = 0;
                _pageIndicator.text = "Page 1/1";
                builder.AppendLine($"  {emptyText}");
                return;
            }

            var totalPages = Mathf.Max(1, Mathf.CeilToInt(filtered.Count / (float)PageSize));
            _currentPage = Mathf.Clamp(_currentPage, 0, totalPages - 1);
            _pageIndicator.text = $"Page {_currentPage + 1}/{totalPages} ({filtered.Count} items)";

            var page = filtered.Skip(_currentPage * PageSize).Take(PageSize);
            foreach (var row in page)
            {
                builder.AppendLine($"  - {formatter(row)}");
            }
        }

        private static bool Matches(string query, params string[] fields)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return true;
            }

            var q = query.ToLowerInvariant();
            foreach (var field in fields)
            {
                if (!string.IsNullOrWhiteSpace(field) && field.ToLowerInvariant().Contains(q))
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplySavedPanelLayout(RectTransform rect, string panelId)
        {
            if (rect == null || _settingsService == null)
            {
                return;
            }

            var layout = (_settingsService.CurrentSettings.panelLayouts ?? new List<OmniversePanelLayout>())
                .FirstOrDefault(x => string.Equals(x.panelId, panelId, StringComparison.OrdinalIgnoreCase));
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

        private async Task PersistPanelLayoutAsync(string panelId, RectTransform rect)
        {
            if (_settingsService == null || _kernel == null || rect == null)
            {
                return;
            }

            var clone = _settingsService.CloneCurrentSettings();
            if (clone.IsError)
            {
                _statusText.text = clone.Message;
                return;
            }

            var settings = clone.Result;
            settings.panelLayouts ??= new List<OmniversePanelLayout>();
            settings.panelLayouts.RemoveAll(x => string.Equals(x.panelId, panelId, StringComparison.OrdinalIgnoreCase));
            settings.panelLayouts.Add(new OmniversePanelLayout
            {
                panelId = panelId,
                anchoredX = rect.anchoredPosition.x,
                anchoredY = rect.anchoredPosition.y,
                width = rect.rect.width,
                height = rect.rect.height
            });

            await _kernel.SaveUiPreferencesAsync(settings);
        }

        private OmniversePanelLayout CaptureControlCenterLayout()
        {
            if (_panel == null)
            {
                return null;
            }

            var rect = _panel.GetComponent<RectTransform>();
            return new OmniversePanelLayout
            {
                panelId = ControlCenterPanelId,
                anchoredX = rect.anchoredPosition.x,
                anchoredY = rect.anchoredPosition.y,
                width = rect.rect.width,
                height = rect.rect.height
            };
        }

        private void ApplyControlCenterLayout(OmniversePanelLayout layout)
        {
            if (_panel == null || layout == null)
            {
                return;
            }

            var rect = _panel.GetComponent<RectTransform>();
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

        private async Task AnimateControlCenterLayoutAsync(OmniversePanelLayout layout, float durationSeconds = SnapAnimationDuration)
        {
            if (_panel == null || layout == null)
            {
                return;
            }

            var rect = _panel.GetComponent<RectTransform>();
            if (_controlCenterTween != null)
            {
                StopCoroutine(_controlCenterTween);
                _controlCenterTween = null;
            }

            var tcs = new TaskCompletionSource<bool>();
            _controlCenterTween = StartCoroutine(AnimateRectLayoutCoroutine(rect, layout, Mathf.Max(0.05f, durationSeconds), () => tcs.TrySetResult(true)));
            await tcs.Task;
            _controlCenterTween = null;
        }

        private static System.Collections.IEnumerator AnimateRectLayoutCoroutine(RectTransform rect, OmniversePanelLayout target, float duration, Action onComplete)
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

        private OmniversePanelLayout BuildControlCenterLayoutPreset(string presetName)
        {
            var current = CaptureControlCenterLayout();
            if (current == null)
            {
                return null;
            }

            var width = Mathf.Max(300f, current.width);
            var height = Mathf.Max(250f, current.height);
            var margin = 30f;

            switch ((presetName ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "topleft":
                    current.anchoredX = margin;
                    current.anchoredY = -margin;
                    break;
                case "topright":
                    current.anchoredX = Mathf.Max(margin, Screen.width - width - margin);
                    current.anchoredY = -margin;
                    break;
                case "center":
                    current.anchoredX = Mathf.Max(0f, (Screen.width - width) * 0.5f);
                    current.anchoredY = -Mathf.Max(0f, (Screen.height - height) * 0.5f);
                    break;
                default:
                    return null;
            }

            return current;
        }

        private async Task PersistPanelLayoutsAsync(params OmniversePanelLayout[] layouts)
        {
            if (_settingsService == null || _kernel == null)
            {
                return;
            }

            var clone = _settingsService.CloneCurrentSettings();
            if (clone.IsError)
            {
                _settingsFeedbackText.text = clone.Message;
                return;
            }

            var settings = clone.Result;
            settings.panelLayouts ??= new List<OmniversePanelLayout>();

            foreach (var layout in layouts.Where(x => x != null && !string.IsNullOrWhiteSpace(x.panelId)))
            {
                settings.panelLayouts.RemoveAll(x => string.Equals(x.panelId, layout.panelId, StringComparison.OrdinalIgnoreCase));
                settings.panelLayouts.Add(layout);
            }

            var save = await _kernel.SaveUiPreferencesAsync(settings);
            if (save.IsError)
            {
                _settingsFeedbackText.text = save.Message;
                ShowToast(save.Message, ToastSeverity.Error, 2.1f);
            }
        }

        private async Task ApplyControlCenterLayoutPresetAsync(string presetName)
        {
            var layout = BuildControlCenterLayoutPreset(presetName);
            if (layout == null)
            {
                _settingsFeedbackText.text = $"Unknown control center preset '{presetName}'.";
                ShowToast(_settingsFeedbackText.text, ToastSeverity.Error, 2.0f);
                return;
            }

            await AnimateControlCenterLayoutAsync(layout);
            var trackerLayout = _kernel.GetQuestTrackerLayout();
            await PersistPanelLayoutsAsync(layout, trackerLayout.IsError ? null : trackerLayout.Result);
            var msg = $"Control Center snapped to {presetName}.";
            _settingsFeedbackText.text = msg;
            ShowToast(msg, ToastSeverity.Success);
        }

        private async Task ApplyQuestTrackerLayoutPresetAsync(string presetName)
        {
            var trackerLayout = await _kernel.ApplyQuestTrackerLayoutPresetAnimatedAsync(presetName, SnapAnimationDuration);
            if (trackerLayout.IsError)
            {
                _settingsFeedbackText.text = trackerLayout.Message;
                ShowToast(_settingsFeedbackText.text, ToastSeverity.Error, 2.0f);
                return;
            }

            await PersistPanelLayoutsAsync(CaptureControlCenterLayout(), trackerLayout.Result);
            var msg = $"Quest Tracker snapped to {presetName}.";
            _settingsFeedbackText.text = msg;
            ShowToast(msg, ToastSeverity.Success);
        }

        private async Task ResetAllPanelLayoutsAsync()
        {
            var defaultControlLayout = new OmniversePanelLayout
            {
                panelId = ControlCenterPanelId,
                anchoredX = 30f,
                anchoredY = -30f,
                width = Mathf.Max(900f, Screen.width * 0.90f),
                height = Mathf.Max(560f, Screen.height * 0.90f)
            };
            await AnimateControlCenterLayoutAsync(defaultControlLayout);

            var trackerLayout = await _kernel.ResetQuestTrackerLayoutToDefaultAnimatedAsync(SnapAnimationDuration);
            await PersistPanelLayoutsAsync(defaultControlLayout, trackerLayout.IsError ? null : trackerLayout.Result);
            _settingsFeedbackText.text = trackerLayout.IsError
                ? $"Control Center reset, tracker reset failed: {trackerLayout.Message}"
                : "All panel layouts reset to defaults.";
            ShowToast(_settingsFeedbackText.text, trackerLayout.IsError ? ToastSeverity.Warning : ToastSeverity.Success, 2.0f);
        }

        private void ConfigureSortOptionsForTab(OmniverseTab tab)
        {
            if (_sortFieldDropdown == null)
            {
                return;
            }

            var options = _sortOptions.TryGetValue(tab, out var tabOptions) ? tabOptions : new[] { "Name" };
            _sortFieldDropdown.options.Clear();
            foreach (var option in options)
            {
                _sortFieldDropdown.options.Add(new Dropdown.OptionData(option));
            }
            _sortFieldDropdown.value = 0;
            _sortFieldDropdown.RefreshShownValue();

            _suppressPresetEvents = true;
            try
            {
                RefreshPresetDropdownForCurrentTab();
                RefreshTemplateDropdownForCurrentTab();
                ApplyActivePresetForCurrentTab();
            }
            finally
            {
                _suppressPresetEvents = false;
            }
        }

        private List<InventoryItem> SortInventory(List<InventoryItem> list)
        {
            var field = CurrentSortField();
            var asc = IsSortAscending();
            IOrderedEnumerable<InventoryItem> sorted;
            switch (field)
            {
                case "Type":
                    sorted = list.OrderBy(x => x.itemType);
                    break;
                case "Source":
                    sorted = list.OrderBy(x => x.source);
                    break;
                default:
                    sorted = list.OrderBy(x => x.name);
                    break;
            }

            return asc ? sorted.ToList() : sorted.Reverse().ToList();
        }

        private List<QuestItem> SortQuests(List<QuestItem> list)
        {
            var field = CurrentSortField();
            var asc = IsSortAscending();
            IOrderedEnumerable<QuestItem> sorted;
            switch (field)
            {
                case "Status":
                    sorted = list.OrderBy(x => x.status);
                    break;
                case "Priority":
                    sorted = list.OrderBy(x => PriorityRank(x.name, x.description));
                    break;
                default:
                    sorted = list.OrderBy(x => x.name);
                    break;
            }

            return asc ? sorted.ToList() : sorted.Reverse().ToList();
        }

        private List<NftAssetItem> SortNfts(List<NftAssetItem> list)
        {
            var field = CurrentSortField();
            var asc = IsSortAscending();
            IOrderedEnumerable<NftAssetItem> sorted;
            switch (field)
            {
                case "Type":
                    sorted = list.OrderBy(x => x.type);
                    break;
                case "Source":
                    sorted = list.OrderBy(x => x.source);
                    break;
                default:
                    sorted = list.OrderBy(x => x.name);
                    break;
            }

            return asc ? sorted.ToList() : sorted.Reverse().ToList();
        }

        private List<KarmaEntry> SortKarma(List<KarmaEntry> list)
        {
            var field = CurrentSortField();
            var asc = IsSortAscending();
            IOrderedEnumerable<KarmaEntry> sorted;
            switch (field)
            {
                case "Source":
                    sorted = list.OrderBy(x => x.source);
                    break;
                case "Amount":
                    sorted = list.OrderBy(x => x.amount);
                    break;
                default:
                    sorted = list.OrderBy(x => x.createdDate);
                    break;
            }

            return asc ? sorted.ToList() : sorted.Reverse().ToList();
        }

        private string CurrentSortField()
        {
            if (_sortFieldDropdown == null || _sortFieldDropdown.options.Count == 0)
            {
                return "Name";
            }

            return _sortFieldDropdown.options[_sortFieldDropdown.value].text;
        }

        private bool IsSortAscending()
        {
            return _sortDirectionDropdown == null || _sortDirectionDropdown.value == 0;
        }

        private void RefreshPresetDropdownForCurrentTab()
        {
            if (_presetDropdown == null || _settingsService == null)
            {
                return;
            }

            var tabName = _currentTab.ToString();
            var presets = (_settingsService.CurrentSettings.viewPresets ?? new List<OmniverseViewPreset>())
                .Where(x => string.Equals(x.tab, tabName, StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.name)
                .ToList();

            _presetDropdown.options.Clear();
            _presetDropdown.options.Add(new Dropdown.OptionData("(none)"));
            foreach (var preset in presets)
            {
                _presetDropdown.options.Add(new Dropdown.OptionData(preset.name));
            }

            _presetDropdown.value = 0;
            _presetDropdown.RefreshShownValue();
        }

        private void RefreshTemplateDropdownForCurrentTab()
        {
            if (_templateDropdown == null)
            {
                return;
            }

            _templateDropdown.options.Clear();
            _templateDropdown.options.Add(new Dropdown.OptionData("Select Template"));
            foreach (var template in GetBuiltInTemplatesForCurrentTab())
            {
                _templateDropdown.options.Add(new Dropdown.OptionData(template.name));
            }

            _templateDropdown.value = 0;
            _templateDropdown.RefreshShownValue();
        }

        private List<OmniverseViewPreset> GetBuiltInTemplatesForCurrentTab()
        {
            var tabName = _currentTab.ToString();
            var list = new List<OmniverseViewPreset>();

            switch (_currentTab)
            {
                case OmniverseTab.Quests:
                    list.Add(new OmniverseViewPreset { name = "Critical Quests First", tab = tabName, sortField = "Priority", sortAscending = true, searchQuery = "critical urgent boss" });
                    list.Add(new OmniverseViewPreset { name = "Active Quests", tab = tabName, sortField = "Status", sortAscending = true, searchQuery = "active progress started" });
                    break;
                case OmniverseTab.Karma:
                    list.Add(new OmniverseViewPreset { name = "Newest Karma First", tab = tabName, sortField = "Date", sortAscending = false, searchQuery = string.Empty });
                    list.Add(new OmniverseViewPreset { name = "Highest Karma First", tab = tabName, sortField = "Amount", sortAscending = false, searchQuery = string.Empty });
                    break;
                case OmniverseTab.Inventory:
                    list.Add(new OmniverseViewPreset { name = "Loot by Source", tab = tabName, sortField = "Source", sortAscending = true, searchQuery = string.Empty });
                    list.Add(new OmniverseViewPreset { name = "Key Items", tab = tabName, sortField = "Type", sortAscending = true, searchQuery = "key" });
                    break;
                case OmniverseTab.Nfts:
                    list.Add(new OmniverseViewPreset { name = "Assets by Source", tab = tabName, sortField = "Source", sortAscending = true, searchQuery = string.Empty });
                    list.Add(new OmniverseViewPreset { name = "Boss NFTs", tab = tabName, sortField = "Type", sortAscending = true, searchQuery = "boss" });
                    break;
            }

            return list;
        }

        private void ApplySelectedTemplate()
        {
            if (_templateDropdown == null)
            {
                return;
            }

            var selected = _templateDropdown.options[_templateDropdown.value].text;
            if (selected == "Select Template")
            {
                return;
            }

            var template = GetBuiltInTemplatesForCurrentTab()
                .FirstOrDefault(x => string.Equals(x.name, selected, StringComparison.OrdinalIgnoreCase));
            if (template == null)
            {
                return;
            }

            ApplyPresetValues(template);
            _presetNameInput.text = template.name;
            _statusText.text = $"Template '{template.name}' applied.";
        }

        private void ExportPresetsToClipboard()
        {
            if (_settingsService == null)
            {
                return;
            }

            var payload = new PresetExportPackage
            {
                schema = "oasis.omniverse.viewpresets",
                schemaVersion = 1,
                exportedAtUtc = DateTime.UtcNow.ToString("O"),
                viewPresets = (_settingsService.CurrentSettings.viewPresets ?? new List<OmniverseViewPreset>()).ToList(),
                activeViewPresets = (_settingsService.CurrentSettings.activeViewPresets ?? new List<OmniverseActiveViewPreset>()).ToList()
            };

            GUIUtility.systemCopyBuffer = JsonConvert.SerializeObject(payload, Formatting.Indented);
            _statusText.text = $"Exported {payload.viewPresets.Count} preset(s) to clipboard.";
        }

        private async Task ImportPresetsFromClipboardAsync()
        {
            if (_settingsService == null || _kernel == null)
            {
                return;
            }

            var json = GUIUtility.systemCopyBuffer;
            if (string.IsNullOrWhiteSpace(json))
            {
                _statusText.text = "Clipboard is empty.";
                return;
            }

            PresetExportPackage payload;
            try
            {
                payload = ParsePresetImportPayload(json);
            }
            catch (Exception ex)
            {
                _statusText.text = $"Import failed: invalid JSON ({ex.Message}).";
                return;
            }

            if (payload == null || payload.viewPresets == null)
            {
                _statusText.text = "Import failed: JSON has no preset payload.";
                return;
            }

            var cloneResult = _settingsService.CloneCurrentSettings();
            if (cloneResult.IsError)
            {
                _statusText.text = cloneResult.Message;
                return;
            }

            var settings = cloneResult.Result;
            settings.viewPresets ??= new List<OmniverseViewPreset>();
            settings.activeViewPresets ??= new List<OmniverseActiveViewPreset>();

            foreach (var preset in payload.viewPresets.Where(x => !string.IsNullOrWhiteSpace(x.name) && !string.IsNullOrWhiteSpace(x.tab)))
            {
                settings.viewPresets.RemoveAll(x => string.Equals(x.tab, preset.tab, StringComparison.OrdinalIgnoreCase) &&
                                                    string.Equals(x.name, preset.name, StringComparison.OrdinalIgnoreCase));
                settings.viewPresets.Add(preset);
            }

            if (payload.activeViewPresets != null && payload.activeViewPresets.Count > 0)
            {
                foreach (var active in payload.activeViewPresets.Where(x => !string.IsNullOrWhiteSpace(x.tab)))
                {
                    settings.activeViewPresets.RemoveAll(x => string.Equals(x.tab, active.tab, StringComparison.OrdinalIgnoreCase));
                    settings.activeViewPresets.Add(active);
                }
            }

            var saveResult = await _kernel.SaveUiPreferencesAsync(settings);
            if (saveResult.IsError)
            {
                _statusText.text = $"Import save failed: {saveResult.Message}";
                return;
            }

            _suppressPresetEvents = true;
            try
            {
                RefreshPresetDropdownForCurrentTab();
                ApplyActivePresetForCurrentTab();
            }
            finally
            {
                _suppressPresetEvents = false;
            }

            _statusText.text = $"Imported {payload.viewPresets.Count} preset(s) from clipboard.";
        }

        private PresetExportPackage ParsePresetImportPayload(string json)
        {
            var token = JToken.Parse(json);
            if (token is JArray legacyArray)
            {
                return new PresetExportPackage
                {
                    schema = "oasis.omniverse.viewpresets",
                    schemaVersion = 1,
                    exportedAtUtc = DateTime.UtcNow.ToString("O"),
                    viewPresets = legacyArray.ToObject<List<OmniverseViewPreset>>() ?? new List<OmniverseViewPreset>(),
                    activeViewPresets = new List<OmniverseActiveViewPreset>()
                };
            }

            if (token is not JObject obj)
            {
                throw new InvalidOperationException("Unsupported import JSON root.");
            }

            // Legacy object format with only viewPresets and no schema metadata.
            if (obj["viewPresets"] != null && obj["schemaVersion"] == null)
            {
                return new PresetExportPackage
                {
                    schema = "oasis.omniverse.viewpresets",
                    schemaVersion = 1,
                    exportedAtUtc = DateTime.UtcNow.ToString("O"),
                    viewPresets = obj["viewPresets"]?.ToObject<List<OmniverseViewPreset>>() ?? new List<OmniverseViewPreset>(),
                    activeViewPresets = obj["activeViewPresets"]?.ToObject<List<OmniverseActiveViewPreset>>() ?? new List<OmniverseActiveViewPreset>()
                };
            }

            var payload = obj.ToObject<PresetExportPackage>();
            if (payload == null)
            {
                throw new InvalidOperationException("Could not deserialize preset package.");
            }

            if (!string.Equals(payload.schema, "oasis.omniverse.viewpresets", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Unsupported schema '{payload.schema}'.");
            }

            if (payload.schemaVersion < 1 || payload.schemaVersion > 1)
            {
                throw new InvalidOperationException($"Unsupported schema version '{payload.schemaVersion}'.");
            }

            payload.viewPresets ??= new List<OmniverseViewPreset>();
            payload.activeViewPresets ??= new List<OmniverseActiveViewPreset>();
            return payload;
        }

        private void ApplyActivePresetForCurrentTab()
        {
            if (_settingsService == null)
            {
                return;
            }

            var active = (_settingsService.CurrentSettings.activeViewPresets ?? new List<OmniverseActiveViewPreset>())
                .FirstOrDefault(x => string.Equals(x.tab, _currentTab.ToString(), StringComparison.OrdinalIgnoreCase));

            if (active == null || string.IsNullOrWhiteSpace(active.presetName))
            {
                _searchInput.text = string.Empty;
                _sortDirectionDropdown.value = 0;
                return;
            }

            var preset = (_settingsService.CurrentSettings.viewPresets ?? new List<OmniverseViewPreset>())
                .FirstOrDefault(x => string.Equals(x.tab, _currentTab.ToString(), StringComparison.OrdinalIgnoreCase) &&
                                     string.Equals(x.name, active.presetName, StringComparison.OrdinalIgnoreCase));
            if (preset == null)
            {
                return;
            }

            ApplyPresetValues(preset);
        }

        private async Task SaveCurrentPresetAsync()
        {
            if (_settingsService == null || _kernel == null)
            {
                return;
            }

            var cloneResult = _settingsService.CloneCurrentSettings();
            if (cloneResult.IsError)
            {
                _statusText.text = cloneResult.Message;
                return;
            }

            var settings = cloneResult.Result;
            settings.viewPresets ??= new List<OmniverseViewPreset>();
            settings.activeViewPresets ??= new List<OmniverseActiveViewPreset>();

            var presetName = (_presetNameInput?.text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(presetName))
            {
                _statusText.text = "Preset name is required.";
                return;
            }

            var tabName = _currentTab.ToString();
            settings.viewPresets.RemoveAll(x => string.Equals(x.tab, tabName, StringComparison.OrdinalIgnoreCase) &&
                                                string.Equals(x.name, presetName, StringComparison.OrdinalIgnoreCase));

            settings.viewPresets.Add(new OmniverseViewPreset
            {
                name = presetName,
                tab = tabName,
                searchQuery = _searchInput?.text ?? string.Empty,
                sortField = CurrentSortField(),
                sortAscending = IsSortAscending()
            });

            settings.activeViewPresets.RemoveAll(x => string.Equals(x.tab, tabName, StringComparison.OrdinalIgnoreCase));
            settings.activeViewPresets.Add(new OmniverseActiveViewPreset
            {
                tab = tabName,
                presetName = presetName
            });

            var saveResult = await _kernel.SaveUiPreferencesAsync(settings);
            _statusText.text = saveResult.IsError ? $"Preset save failed: {saveResult.Message}" : $"Preset '{presetName}' saved.";

            RefreshPresetDropdownForCurrentTab();
            SelectPresetInDropdown(presetName);
        }

        private async Task ApplySelectedPresetAsync()
        {
            if (_presetDropdown == null || _settingsService == null || _kernel == null)
            {
                return;
            }

            var selected = _presetDropdown.options[_presetDropdown.value].text;
            if (selected == "(none)")
            {
                _searchInput.text = string.Empty;
                _sortDirectionDropdown.value = 0;
                _currentPage = 0;
                RedrawListTab();
                return;
            }

            var preset = (_settingsService.CurrentSettings.viewPresets ?? new List<OmniverseViewPreset>())
                .FirstOrDefault(x => string.Equals(x.tab, _currentTab.ToString(), StringComparison.OrdinalIgnoreCase) &&
                                     string.Equals(x.name, selected, StringComparison.OrdinalIgnoreCase));
            if (preset == null)
            {
                return;
            }

            ApplyPresetValues(preset);
            _presetNameInput.text = preset.name;

            var cloneResult = _settingsService.CloneCurrentSettings();
            if (cloneResult.IsError)
            {
                return;
            }

            var settings = cloneResult.Result;
            settings.activeViewPresets ??= new List<OmniverseActiveViewPreset>();
            settings.activeViewPresets.RemoveAll(x => string.Equals(x.tab, _currentTab.ToString(), StringComparison.OrdinalIgnoreCase));
            settings.activeViewPresets.Add(new OmniverseActiveViewPreset
            {
                tab = _currentTab.ToString(),
                presetName = preset.name
            });

            await _kernel.SaveUiPreferencesAsync(settings);
            _statusText.text = $"Preset '{preset.name}' applied.";
        }

        private async Task DeleteSelectedPresetAsync()
        {
            if (_presetDropdown == null || _settingsService == null || _kernel == null)
            {
                return;
            }

            var selected = _presetDropdown.options[_presetDropdown.value].text;
            if (selected == "(none)")
            {
                return;
            }

            var cloneResult = _settingsService.CloneCurrentSettings();
            if (cloneResult.IsError)
            {
                _statusText.text = cloneResult.Message;
                return;
            }

            var settings = cloneResult.Result;
            settings.viewPresets ??= new List<OmniverseViewPreset>();
            settings.activeViewPresets ??= new List<OmniverseActiveViewPreset>();

            settings.viewPresets.RemoveAll(x => string.Equals(x.tab, _currentTab.ToString(), StringComparison.OrdinalIgnoreCase) &&
                                                string.Equals(x.name, selected, StringComparison.OrdinalIgnoreCase));
            settings.activeViewPresets.RemoveAll(x => string.Equals(x.tab, _currentTab.ToString(), StringComparison.OrdinalIgnoreCase) &&
                                                      string.Equals(x.presetName, selected, StringComparison.OrdinalIgnoreCase));

            var saveResult = await _kernel.SaveUiPreferencesAsync(settings);
            _statusText.text = saveResult.IsError ? $"Preset delete failed: {saveResult.Message}" : $"Preset '{selected}' deleted.";

            _suppressPresetEvents = true;
            try
            {
                RefreshPresetDropdownForCurrentTab();
            }
            finally
            {
                _suppressPresetEvents = false;
            }
        }

        private void ApplyPresetValues(OmniverseViewPreset preset)
        {
            if (preset == null)
            {
                return;
            }

            _suppressPresetEvents = true;
            try
            {
                _searchInput.text = preset.searchQuery ?? string.Empty;
                _sortDirectionDropdown.value = preset.sortAscending ? 0 : 1;

                var targetField = string.IsNullOrWhiteSpace(preset.sortField) ? "Name" : preset.sortField;
                var index = 0;
                for (var i = 0; i < _sortFieldDropdown.options.Count; i++)
                {
                    if (string.Equals(_sortFieldDropdown.options[i].text, targetField, StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        break;
                    }
                }

                _sortFieldDropdown.value = index;
                _sortFieldDropdown.RefreshShownValue();
            }
            finally
            {
                _suppressPresetEvents = false;
            }

            _currentPage = 0;
            RedrawListTab();
        }

        private void SelectPresetInDropdown(string presetName)
        {
            if (_presetDropdown == null || string.IsNullOrWhiteSpace(presetName))
            {
                return;
            }

            for (var i = 0; i < _presetDropdown.options.Count; i++)
            {
                if (string.Equals(_presetDropdown.options[i].text, presetName, StringComparison.OrdinalIgnoreCase))
                {
                    _presetDropdown.value = i;
                    _presetDropdown.RefreshShownValue();
                    break;
                }
            }
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

        private static string PriorityColorHex(string name, string description)
        {
            var rank = PriorityRank(name, description);
            if (rank <= 0) return "#FF7070";
            if (rank == 1) return "#FFA24D";
            if (rank == 2) return "#FFE07A";
            return "#A8D8FF";
        }

        private static int PriorityRank(string name, string description)
        {
            var hay = $"{name} {description}".ToLowerInvariant();
            if (hay.Contains("critical") || hay.Contains("urgent") || hay.Contains("boss")) return 0;
            if (hay.Contains("high")) return 1;
            if (hay.Contains("medium")) return 2;
            return 3;
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

            CreateText("GraphicsLabel", "Graphics Preset", 18, TextAnchor.MiddleLeft, root, 0.02f, 0.33f, 0.35f, 0.40f);
            _graphicsDropdown = CreateDropdown(root, new[] { "Low", "Medium", "High", "Ultra", "Custom" }, 0.36f, 0.33f, 0.58f, 0.40f);

            CreateText("FullscreenLabel", "Fullscreen", 18, TextAnchor.MiddleLeft, root, 0.62f, 0.33f, 0.78f, 0.40f);
            _fullscreenToggle = CreateToggle(root, 0.80f, 0.34f, 0.85f, 0.39f);

            CreateText("OpenMenuKeyLabel", "Open Control Center Key", 17, TextAnchor.MiddleLeft, root, 0.02f, 0.21f, 0.30f, 0.28f);
            _openMenuInput = CreateInputField(root, 0.31f, 0.21f, 0.45f, 0.28f);

            CreateText("HideHostedKeyLabel", "Hide Hosted Game Key", 17, TextAnchor.MiddleLeft, root, 0.50f, 0.21f, 0.74f, 0.28f);
            _hideGameInput = CreateInputField(root, 0.75f, 0.21f, 0.89f, 0.28f);

            CreateText("LayoutQuickLabel", "Panel Layout Quick Actions", 16, TextAnchor.MiddleLeft, root, 0.02f, 0.16f, 0.34f, 0.21f);
            _layoutResetButton = CreateButton(root, "Reset Layouts", 0.35f, 0.16f, 0.49f, 0.21f);
            _layoutResetButton.onClick.AddListener(() => _ = ResetAllPanelLayoutsAsync());

            CreateText("CCLayoutLabel", "Control Center", 14, TextAnchor.MiddleLeft, root, 0.02f, 0.12f, 0.18f, 0.16f);
            _controlTopLeftButton = CreateButton(root, "TL", 0.19f, 0.12f, 0.23f, 0.16f);
            _controlTopRightButton = CreateButton(root, "TR", 0.24f, 0.12f, 0.28f, 0.16f);
            _controlCenterButton = CreateButton(root, "C", 0.29f, 0.12f, 0.33f, 0.16f);
            _controlTopLeftButton.onClick.AddListener(() => _ = ApplyControlCenterLayoutPresetAsync("TopLeft"));
            _controlTopRightButton.onClick.AddListener(() => _ = ApplyControlCenterLayoutPresetAsync("TopRight"));
            _controlCenterButton.onClick.AddListener(() => _ = ApplyControlCenterLayoutPresetAsync("Center"));

            CreateText("QTLayoutLabel", "Quest Tracker", 14, TextAnchor.MiddleLeft, root, 0.36f, 0.12f, 0.50f, 0.16f);
            _trackerTopLeftButton = CreateButton(root, "TL", 0.51f, 0.12f, 0.55f, 0.16f);
            _trackerTopRightButton = CreateButton(root, "TR", 0.56f, 0.12f, 0.60f, 0.16f);
            _trackerCenterButton = CreateButton(root, "C", 0.61f, 0.12f, 0.65f, 0.16f);
            _trackerTopLeftButton.onClick.AddListener(() => _ = ApplyQuestTrackerLayoutPresetAsync("TopLeft"));
            _trackerTopRightButton.onClick.AddListener(() => _ = ApplyQuestTrackerLayoutPresetAsync("TopRight"));
            _trackerCenterButton.onClick.AddListener(() => _ = ApplyQuestTrackerLayoutPresetAsync("Center"));

            var applyButton = CreateButton(root, "Save & Apply", 0.70f, 0.06f, 0.92f, 0.15f);
            applyButton.onClick.AddListener(() => _ = SaveAndApplySettingsAsync());

            _settingsFeedbackText = CreateText("SettingsFeedback", string.Empty, 16, TextAnchor.MiddleLeft, root, 0.02f, 0.06f, 0.66f, 0.15f);
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
            var button = CreateButton(parent, label, xMin, 0.05f, xMax, 0.95f);
            button.onClick.AddListener(() => _ = ShowTabAsync(tab));
        }

        private static Button CreateButton(Transform parent, string text, float minX, float minY, float maxX, float maxY)
        {
            var go = new GameObject(text + "_Button");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.18f, 0.28f, 0.95f);
            var button = go.AddComponent<Button>();
            SetAnchors(go.GetComponent<RectTransform>(), minX, minY, maxX, maxY);

            var labelText = CreateText(text + "_Label", text, 16, TextAnchor.MiddleCenter, go.transform);
            SetAnchors(labelText.rectTransform, 0f, 0f, 1f, 1f);
            return button;
        }

        private static Text CreateText(string name, string content, int size, TextAnchor anchor, Transform parent)
        {
            return CreateText(name, content, size, anchor, parent, 0f, 0f, 1f, 1f);
        }

        private static Text CreateText(string name, string content, int size, TextAnchor anchor, Transform parent, float minX, float minY, float maxX, float maxY)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.text = content;
            text.alignment = anchor;
            text.fontSize = size;
            text.color = Color.white;
            SetAnchors(text.rectTransform, minX, minY, maxX, maxY);
            return text;
        }

        private static Slider CreateSlider(Transform parent, string label, float yTop)
        {
            CreateText(label + "_Label", label, 18, TextAnchor.MiddleLeft, parent, 0.02f, yTop, 0.28f, yTop + 0.08f);
            var go = new GameObject(label + "_Slider");
            go.transform.SetParent(parent, false);
            SetAnchors(go.AddComponent<RectTransform>(), 0.30f, yTop + 0.015f, 0.92f, yTop + 0.055f);

            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            var background = new GameObject("Background");
            background.transform.SetParent(go.transform, false);
            var backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            SetAnchors(background.GetComponent<RectTransform>(), 0f, 0.2f, 1f, 0.8f);

            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(go.transform, false);
            SetAnchors(fillArea.AddComponent<RectTransform>(), 0f, 0.2f, 1f, 0.8f);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.2f, 0.8f, 1f, 1f);
            var fillRect = fill.GetComponent<RectTransform>();
            SetAnchors(fillRect, 0f, 0f, 1f, 1f);

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

        private static Dropdown CreateDropdown(Transform parent, string[] values, float minX, float minY, float maxX, float maxY)
        {
            var go = new GameObject("Dropdown");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.14f, 0.14f, 0.14f, 1f);
            var dropdown = go.AddComponent<Dropdown>();
            SetAnchors(go.GetComponent<RectTransform>(), minX, minY, maxX, maxY);

            var label = CreateText("Label", "Select", 16, TextAnchor.MiddleLeft, go.transform, 0.07f, 0f, 0.9f, 1f);
            dropdown.captionText = label;
            dropdown.options.Clear();
            foreach (var value in values)
            {
                dropdown.options.Add(new Dropdown.OptionData(value));
            }
            return dropdown;
        }

        private static Toggle CreateToggle(Transform parent, float minX, float minY, float maxX, float maxY)
        {
            var go = new GameObject("Toggle");
            go.transform.SetParent(parent, false);
            var toggle = go.AddComponent<Toggle>();
            SetAnchors(go.AddComponent<RectTransform>(), minX, minY, maxX, maxY);

            var bg = new GameObject("Background");
            bg.transform.SetParent(go.transform, false);
            var bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f);
            SetAnchors(bg.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);

            var checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(bg.transform, false);
            var checkImage = checkmark.AddComponent<Image>();
            checkImage.color = new Color(0.2f, 0.8f, 1f);
            SetAnchors(checkmark.GetComponent<RectTransform>(), 0.15f, 0.15f, 0.85f, 0.85f);

            toggle.graphic = checkImage;
            toggle.targetGraphic = bgImage;
            return toggle;
        }

        private static InputField CreateInputField(Transform parent, float minX, float minY, float maxX, float maxY)
        {
            var go = new GameObject("InputField");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.12f, 0.12f, 0.12f, 1f);
            var input = go.AddComponent<InputField>();
            SetAnchors(go.GetComponent<RectTransform>(), minX, minY, maxX, maxY);

            var text = CreateText("Text", string.Empty, 16, TextAnchor.MiddleLeft, go.transform, 0.05f, 0f, 0.95f, 1f);
            input.textComponent = text;
            input.text = string.Empty;
            return input;
        }

        private static void SetAnchors(RectTransform rect, float minX, float minY, float maxX, float maxY)
        {
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}

