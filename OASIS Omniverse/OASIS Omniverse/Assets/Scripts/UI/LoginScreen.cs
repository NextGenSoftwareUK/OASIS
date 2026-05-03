using System;
using System.Threading.Tasks;
using OASIS.Omniverse.UnityHost.API;
using OASIS.Omniverse.UnityHost.Config;
using OASIS.Omniverse.UnityHost.Core;
using UnityEngine;
using UnityEngine.UI;

namespace OASIS.Omniverse.UnityHost.UI
{
    public class LoginScreen : MonoBehaviour
    {
        private Canvas _canvas;
        private GameObject _panel;
        private InputField _usernameInput;
        private InputField _passwordInput;
        private Button _beamInButton;
        private Text _statusText;
        private Text _titleText;
        private bool _isBeamingIn;
        private string _web4BaseUrl;

        public event Action<string, string> OnBeamInSuccess; // avatarId, jwtToken

        public void Initialize(string web4BaseUrl)
        {
            _web4BaseUrl = web4BaseUrl;
            BuildUi();
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(_web4BaseUrl))
            {
                // Fallback if Initialize wasn't called
                _web4BaseUrl = "https://oasisweb4.one/api";
            }
            if (_canvas == null)
            {
                BuildUi();
            }
        }

        private void BuildUi()
        {
            // Create canvas
            _canvas = new GameObject("LoginCanvas").AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 10000; // Highest priority
            _canvas.gameObject.AddComponent<CanvasScaler>();
            _canvas.gameObject.AddComponent<GraphicRaycaster>();

            // Create EventSystem if needed
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                var eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Create main panel
            _panel = new GameObject("LoginPanel");
            _panel.transform.SetParent(_canvas.transform, false);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 500f);
            panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 400f);

            // Background
            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.15f, 0.95f);

            // Title
            _titleText = CreateText("Title", "OASIS Omniverse Beam In", 36, TextAnchor.MiddleCenter, _panel.transform);
            _titleText.color = new Color(1f, 1f, 1f, 1f);
            var titleRect = _titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.80f);
            titleRect.anchorMax = new Vector2(0.95f, 0.95f);
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            // Username/Email label
            var usernameLabel = CreateText("UsernameLabel", "Username/Email:", 18, TextAnchor.MiddleLeft, _panel.transform);
            usernameLabel.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            var usernameLabelRect = usernameLabel.GetComponent<RectTransform>();
            usernameLabelRect.anchorMin = new Vector2(0.05f, 0.60f);
            usernameLabelRect.anchorMax = new Vector2(0.45f, 0.70f);
            usernameLabelRect.offsetMin = Vector2.zero;
            usernameLabelRect.offsetMax = Vector2.zero;

            // Username/Email input
            _usernameInput = CreateInputField(_panel.transform, 0.05f, 0.45f, 0.95f, 0.58f);
            _usernameInput.placeholder.GetComponent<Text>().text = "Enter your username or email";
            _usernameInput.contentType = InputField.ContentType.Standard;

            // Password label
            var passwordLabel = CreateText("PasswordLabel", "Password:", 18, TextAnchor.MiddleLeft, _panel.transform);
            passwordLabel.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            var passwordLabelRect = passwordLabel.GetComponent<RectTransform>();
            passwordLabelRect.anchorMin = new Vector2(0.05f, 0.35f);
            passwordLabelRect.anchorMax = new Vector2(0.45f, 0.45f);
            passwordLabelRect.offsetMin = Vector2.zero;
            passwordLabelRect.offsetMax = Vector2.zero;

            // Password input
            _passwordInput = CreateInputField(_panel.transform, 0.05f, 0.20f, 0.95f, 0.33f);
            _passwordInput.placeholder.GetComponent<Text>().text = "Enter your password";
            _passwordInput.contentType = InputField.ContentType.Standard;
            _passwordInput.inputType = InputField.InputType.Password;

            // Set up Tab navigation between fields
            // Unity's InputField navigation works differently - we need to use the Navigation component properly
            var usernameNav = _usernameInput.navigation;
            usernameNav.mode = Navigation.Mode.Explicit;
            usernameNav.selectOnDown = _passwordInput;
            _usernameInput.navigation = usernameNav;

            var passwordNav = _passwordInput.navigation;
            passwordNav.mode = Navigation.Mode.Explicit;
            passwordNav.selectOnUp = _usernameInput;
            _passwordInput.navigation = passwordNav;
            
            // Ensure fields are in the navigation order
            _usernameInput.transform.SetSiblingIndex(0);
            _passwordInput.transform.SetSiblingIndex(1);

            // Status text
            _statusText = CreateText("StatusText", "", 14, TextAnchor.MiddleCenter, _panel.transform);
            _statusText.color = new Color(1f, 0.8f, 0.8f, 1f);
            var statusRect = _statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.05f, 0.10f);
            statusRect.anchorMax = new Vector2(0.95f, 0.18f);
            statusRect.offsetMin = Vector2.zero;
            statusRect.offsetMax = Vector2.zero;

            // Beam In button
            var beamInButtonObj = new GameObject("BeamInButton");
            beamInButtonObj.transform.SetParent(_panel.transform, false);
            var beamInButtonRect = beamInButtonObj.AddComponent<RectTransform>();
            beamInButtonRect.anchorMin = new Vector2(0.25f, 0.02f);
            beamInButtonRect.anchorMax = new Vector2(0.75f, 0.10f);
            beamInButtonRect.offsetMin = Vector2.zero;
            beamInButtonRect.offsetMax = Vector2.zero;

            var beamInButtonBg = beamInButtonObj.AddComponent<Image>();
            beamInButtonBg.color = new Color(0.2f, 0.6f, 0.9f, 1f);

            _beamInButton = beamInButtonObj.AddComponent<Button>();
            var beamInButtonText = CreateText("BeamInButtonText", "Beam In", 20, TextAnchor.MiddleCenter, beamInButtonObj.transform);
            beamInButtonText.color = Color.white;
            var beamInButtonTextRect = beamInButtonText.GetComponent<RectTransform>();
            beamInButtonTextRect.anchorMin = Vector2.zero;
            beamInButtonTextRect.anchorMax = Vector2.one;
            beamInButtonTextRect.offsetMin = Vector2.zero;
            beamInButtonTextRect.offsetMax = Vector2.zero;

            _beamInButton.onClick.AddListener(OnBeamInButtonClicked);
        }

        private Text CreateText(string name, string content, int fontSize, TextAnchor anchor, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                text.font = font;
            }
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            return text;
        }

        private InputField CreateInputField(Transform parent, float minX, float minY, float maxX, float maxY)
        {
            var go = new GameObject("InputField");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.15f, 1f);

            var input = go.AddComponent<InputField>();
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(minX, minY);
            rect.anchorMax = new Vector2(maxX, maxY);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Placeholder
            var placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(go.transform, false);
            var placeholderText = placeholderObj.AddComponent<Text>();
            var placeholderFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (placeholderFont != null)
            {
                placeholderText.font = placeholderFont;
            }
            placeholderText.text = "";
            placeholderText.fontSize = 16;
            placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            var placeholderRect = placeholderObj.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = new Vector2(10f, 0f);
            placeholderRect.offsetMax = new Vector2(-10f, 0f);
            input.placeholder = placeholderText;

            // Text component
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(go.transform, false);
            var text = textObj.AddComponent<Text>();
            var textFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (textFont != null)
            {
                text.font = textFont;
            }
            text.fontSize = 16;
            text.color = Color.white;
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 0f);
            textRect.offsetMax = new Vector2(-10f, 0f);
            input.textComponent = text;

            return input;
        }

        private async void OnBeamInButtonClicked()
        {
            if (_isBeamingIn)
            {
                return;
            }

            var username = _usernameInput.text?.Trim() ?? string.Empty;
            var password = _passwordInput.text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                _statusText.text = "Please enter your username or email";
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                _statusText.text = "Please enter your password";
                return;
            }

            _isBeamingIn = true;
            _beamInButton.interactable = false;
            _statusText.text = "Beaming in...";

            try
            {
                // Create a temporary API client for authentication
                var tempClient = new Web4Web5GatewayClient(_web4BaseUrl, _web4BaseUrl, string.Empty, string.Empty);
                
                // Log the URI being used for debugging
                UnityEngine.Debug.Log($"[Beam In] Authenticating to: {_web4BaseUrl}/api/avatar/authenticate");
                
                // Authenticate
                var authResult = await tempClient.AuthenticateAsync(username, password);
                tempClient.Dispose();

                if (authResult.IsError)
                {
                    _statusText.text = $"Authentication failed: {authResult.Message}";
                    UnityEngine.Debug.LogError($"[Beam In] Authentication error: {authResult.Message}");
                    return;
                }

                var authResponse = authResult.Result;
                if (string.IsNullOrWhiteSpace(authResponse.id))
                {
                    _statusText.text = "Authentication succeeded but no avatar ID returned";
                    return;
                }

                // Save credentials to config
                var configResult = HostConfigLoader.Load();
                if (configResult.IsError)
                {
                    _statusText.text = $"Error loading config: {configResult.Message}";
                    return;
                }

                var config = configResult.Result;
                config.avatarId = authResponse.id;
                if (!string.IsNullOrWhiteSpace(authResponse.jwtToken))
                {
                    config.apiKey = authResponse.jwtToken;
                }

                var saveResult = HostConfigLoader.Save(config);
                if (saveResult.IsError)
                {
                    _statusText.text = $"Error saving config: {saveResult.Message}";
                    return;
                }

                // Wait a moment for the save to complete
                await Task.Delay(100);

                // Trigger beam in success
                OnBeamInSuccess?.Invoke(authResponse.id, authResponse.jwtToken ?? string.Empty);
            }
            catch (Exception ex)
            {
                _statusText.text = $"Error: {ex.Message}";
            }
            finally
            {
                _isBeamingIn = false;
                _beamInButton.interactable = true;
            }
        }

        private void Update()
        {
            // Allow Enter key to submit beam in
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (!_isBeamingIn && _beamInButton != null && _beamInButton.interactable)
                {
                    OnBeamInButtonClicked();
                }
            }

            // Manual Tab navigation between fields
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (_usernameInput != null && _usernameInput.isFocused)
                {
                    _passwordInput?.Select();
                    _passwordInput?.ActivateInputField();
                }
                else if (_passwordInput != null && _passwordInput.isFocused)
                {
                    _usernameInput?.Select();
                    _usernameInput?.ActivateInputField();
                }
                else if (_usernameInput != null && !_usernameInput.isFocused && !_passwordInput.isFocused)
                {
                    // If neither is focused, focus username
                    _usernameInput.Select();
                    _usernameInput.ActivateInputField();
                }
            }
        }

        public void Show()
        {
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(false);
            }
        }

        public void Destroy()
        {
            if (_canvas != null)
            {
                UnityEngine.Object.Destroy(_canvas.gameObject);
            }
        }
    }
}

