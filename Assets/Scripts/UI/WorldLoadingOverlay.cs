using UnityEngine;

namespace MineCraftUnity.UI
{
    /// <summary>
    /// Simple full-screen loading UI while spawn chunks generate (Phase 1.4).
    /// </summary>
    public sealed class WorldLoadingOverlay : MonoBehaviour
    {
        private static WorldLoadingOverlay _instance;

        private bool _visible;
        private string _message = "Generating world…";
        private float _progress;
        private GUIStyle _boxStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _progressStyle;

        public static bool IsVisible => _instance != null && _instance._visible;

        public static void EnsureExists()
        {
            if (_instance != null)
            {
                return;
            }

            var go = new GameObject("WorldLoadingOverlay");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<WorldLoadingOverlay>();
        }

        public static void Show(string message)
        {
            EnsureExists();
            _instance._visible = true;
            _instance._message = message;
            _instance._progress = 0f;
        }

        public static void SetProgress(float progress, string message = null)
        {
            if (_instance == null)
            {
                return;
            }

            _instance._progress = Mathf.Clamp01(progress);
            if (message != null)
            {
                _instance._message = message;
            }
        }

        public static void Hide()
        {
            if (_instance == null)
            {
                return;
            }

            _instance._visible = false;
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                return;
            }

            EnsureStyles();

            var screen = new Rect(0f, 0f, Screen.width, Screen.height);
            GUI.Box(screen, GUIContent.none, _boxStyle);

            const float panelWidth = 420f;
            const float panelHeight = 120f;
            var panel = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUI.Label(new Rect(panel.x, panel.y + 16f, panel.width, 28f), _message, _labelStyle);

            var barOuter = new Rect(panel.x + 24f, panel.y + 56f, panel.width - 48f, 22f);
            GUI.Box(barOuter, GUIContent.none, _progressStyle);
            var barInner = new Rect(barOuter.x + 2f, barOuter.y + 2f, (barOuter.width - 4f) * _progress, barOuter.height - 4f);
            GUI.Box(barInner, GUIContent.none, _progressStyle);

            var percent = Mathf.RoundToInt(_progress * 100f);
            GUI.Label(new Rect(panel.x, panel.y + 84f, panel.width, 24f), $"{percent}%", _labelStyle);
        }

        private void EnsureStyles()
        {
            if (_boxStyle != null)
            {
                return;
            }

            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = MakeTexture(new Color(0f, 0f, 0f, 0.82f));

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 16,
                normal = { textColor = Color.white }
            };

            _progressStyle = new GUIStyle(GUI.skin.box);
            _progressStyle.normal.background = MakeTexture(new Color(0.35f, 0.65f, 0.35f, 1f));
        }

        private static Texture2D MakeTexture(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
