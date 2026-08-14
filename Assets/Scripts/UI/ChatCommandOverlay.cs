using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MineCraftUnity.UI
{
    /// <summary>
    /// MC-style chat bar — press T or / to type commands (e.g. /time set noon).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class ChatCommandOverlay : MonoBehaviour
    {
        private const int MaxMessages = 8;
        private const int MaxVisibleSuggestions = 8;
        private const float BarHeight = 28f;
        private const float BarWidth = 520f;
        private const float BottomMargin = 12f;
        private const float LogHeight = 140f;
        private const float SuggestionLineHeight = 20f;

        public static bool IsOpen { get; private set; }

        private bool _open;
        private bool _focusInput;
        private string _input = string.Empty;
        private string _lastSuggestionInput = string.Empty;
        private int _selectedSuggestionIndex;
        private readonly List<string> _messages = new();
        private readonly List<string> _suggestions = new();
        private GUIStyle _barStyle;
        private GUIStyle _inputStyle;
        private GUIStyle _logStyle;
        private GUIStyle _hintStyle;
        private GUIStyle _suggestionStyle;
        private GUIStyle _suggestionSelectedStyle;

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (!_open)
            {
                if (WasSlashPressed())
                {
                    OpenChat("/");
                    return;
                }

                if (Keyboard.current.tKey.wasPressedThisFrame)
                {
                    OpenChat(string.Empty);
                }

                return;
            }

            RefreshSuggestions();

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                CloseChat();
                return;
            }

            HandleChatKeyboardInput();
        }

        private void HandleChatKeyboardInput()
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame
                || Keyboard.current.numpadEnterKey.wasPressedThisFrame)
            {
                SubmitInput();
                return;
            }

            if (_suggestions.Count == 0)
            {
                return;
            }

            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                ApplySuggestion(_selectedSuggestionIndex);
                return;
            }

            if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                _selectedSuggestionIndex = (_selectedSuggestionIndex - 1 + _suggestions.Count) % _suggestions.Count;
            }
            else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                _selectedSuggestionIndex = (_selectedSuggestionIndex + 1) % _suggestions.Count;
            }
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (_open)
            {
                RefreshSuggestions();
            }

            DrawMessageLog();

            if (!_open)
            {
                return;
            }

            DrawSuggestions();

            var barRect = GetBarRect();
            GUI.Box(barRect, GUIContent.none, _barStyle);

            const float pad = 6f;
            var fieldRect = new Rect(barRect.x + pad, barRect.y + 4f, barRect.width - pad * 2f, barRect.height - 8f);
            DrawChatTextField(fieldRect);
        }

        private void DrawChatTextField(Rect fieldRect)
        {
            GUI.SetNextControlName("MineCraftChatInput");
            if (_focusInput)
            {
                GUI.FocusControl("MineCraftChatInput");
                _focusInput = false;
            }

            ConsumeTextFieldNavigationEvents();

            var previousInput = _input;
            _input = GUI.TextField(fieldRect, _input, 256, _inputStyle);
            if (!string.Equals(previousInput, _input))
            {
                _selectedSuggestionIndex = 0;
                _lastSuggestionInput = string.Empty;
            }
        }

        private static void ConsumeTextFieldNavigationEvents()
        {
            if (Event.current.type != EventType.KeyDown)
            {
                return;
            }

            switch (Event.current.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                case KeyCode.Tab:
                case KeyCode.UpArrow:
                case KeyCode.DownArrow:
                    Event.current.Use();
                    break;
            }
        }

        private void RefreshSuggestions()
        {
            if (!_input.StartsWith('/'))
            {
                _suggestions.Clear();
                _lastSuggestionInput = _input;
                return;
            }

            if (string.Equals(_input, _lastSuggestionInput, System.StringComparison.Ordinal))
            {
                return;
            }

            _lastSuggestionInput = _input;
            _suggestions.Clear();
            foreach (var suggestion in ChatCommandSuggestions.GetMatches(_input))
            {
                _suggestions.Add(suggestion);
            }

            if (_selectedSuggestionIndex >= _suggestions.Count)
            {
                _selectedSuggestionIndex = 0;
            }
        }

        private void DrawSuggestions()
        {
            if (_suggestions.Count == 0)
            {
                return;
            }

            var barRect = GetBarRect();
            var visibleCount = Mathf.Min(_suggestions.Count, MaxVisibleSuggestions);
            var panelHeight = visibleCount * SuggestionLineHeight + 10f;
            var panelRect = new Rect(
                barRect.x,
                barRect.y - panelHeight - 4f,
                barRect.width,
                panelHeight);

            GUI.Box(panelRect, GUIContent.none, _barStyle);

            var y = panelRect.y + 5f;
            for (var i = 0; i < visibleCount; i++)
            {
                var lineRect = new Rect(panelRect.x + 8f, y, panelRect.width - 16f, SuggestionLineHeight);
                var style = i == _selectedSuggestionIndex ? _suggestionSelectedStyle : _suggestionStyle;
                GUI.Label(lineRect, _suggestions[i], style);

                if (Event.current.type == EventType.MouseDown
                    && Event.current.button == 0
                    && lineRect.Contains(Event.current.mousePosition))
                {
                    ApplySuggestion(i);
                    Event.current.Use();
                }

                y += SuggestionLineHeight;
            }
        }

        private void ApplySuggestion(int index)
        {
            if (index < 0 || index >= _suggestions.Count)
            {
                return;
            }

            _input = _suggestions[index];
            _selectedSuggestionIndex = index;
            _lastSuggestionInput = _input;
            _focusInput = true;
            GUIUtility.keyboardControl = 0;
        }

        private void OpenChat(string initialText)
        {
            if (_open)
            {
                return;
            }

            _open = true;
            IsOpen = true;
            _focusInput = true;
            _input = initialText;
            _lastSuggestionInput = string.Empty;
            _selectedSuggestionIndex = 0;
            GUIUtility.keyboardControl = 0;
            UnlockCursor();
        }

        private void CloseChat()
        {
            if (!_open)
            {
                return;
            }

            _open = false;
            IsOpen = false;
            _input = string.Empty;
            _suggestions.Clear();
            LockCursor();
        }

        private void SubmitInput()
        {
            var text = _input.Trim();
            if (text.Length > 0)
            {
                AddMessage($"> {text}");
                if (ChatCommandParser.TryExecute(text, out var response))
                {
                    AddMessage(response);
                }
                else if (!string.IsNullOrEmpty(response))
                {
                    AddMessage(response);
                }
            }

            CloseChat();
        }

        private void AddMessage(string message)
        {
            _messages.Add(message);
            while (_messages.Count > MaxMessages)
            {
                _messages.RemoveAt(0);
            }
        }

        private void DrawMessageLog()
        {
            if (_messages.Count == 0 && !_open)
            {
                return;
            }

            var barRect = GetBarRect();
            var extraHeight = _input.StartsWith('/') && _suggestions.Count > 0
                ? Mathf.Min(_suggestions.Count, MaxVisibleSuggestions) * SuggestionLineHeight + 14f
                : 0f;

            var logRect = new Rect(
                barRect.x,
                barRect.y - LogHeight - extraHeight - 4f,
                barRect.width,
                LogHeight);

            GUI.Box(logRect, GUIContent.none, _barStyle);

            var y = logRect.y + 6f;
            var start = Mathf.Max(0, _messages.Count - 6);
            for (var i = start; i < _messages.Count; i++)
            {
                var lineRect = new Rect(logRect.x + 8f, y, logRect.width - 16f, 18f);
                GUI.Label(lineRect, _messages[i], _logStyle);
                y += 18f;
            }

            if (_open)
            {
                var hint = _suggestions.Count > 0
                    ? "Tab/Click = gợi ý   ↑↓ = chọn   Enter = gửi   Esc = hủy"
                    : "Enter = gửi   Esc = hủy";
                var hintRect = new Rect(logRect.x + 8f, logRect.yMax - 20f, logRect.width - 16f, 18f);
                GUI.Label(hintRect, hint, _hintStyle);
            }
        }

        private static Rect GetBarRect()
        {
            var x = (Screen.width - BarWidth) * 0.5f;
            var y = Screen.height - BarHeight - BottomMargin;
            return new Rect(x, y, BarWidth, BarHeight);
        }

        private static bool WasSlashPressed()
        {
            return Keyboard.current.slashKey.wasPressedThisFrame
                || Keyboard.current.numpadDivideKey.wasPressedThisFrame;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void EnsureStyles()
        {
            if (_barStyle != null)
            {
                return;
            }

            _barStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleLeft
            };
            _barStyle.normal.background = MakeBackgroundTexture(new Color(0f, 0f, 0f, 0.78f));

            _inputStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = Color.white },
                focused = { textColor = Color.white }
            };

            _logStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.98f, 0.92f, 1f) }
            };

            _hintStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.75f, 0.75f, 0.75f, 0.9f) }
            };

            _suggestionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.82f, 0.82f, 0.82f, 1f) }
            };

            _suggestionSelectedStyle = new GUIStyle(_suggestionStyle)
            {
                normal =
                {
                    textColor = Color.white,
                    background = MakeBackgroundTexture(new Color(0.25f, 0.35f, 0.55f, 0.85f))
                }
            };
        }

        private static Texture2D MakeBackgroundTexture(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Point
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
