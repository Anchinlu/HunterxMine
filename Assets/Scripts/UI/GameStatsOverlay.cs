using System.Text;
using MineCraftUnity.Core;
using MineCraftUnity.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MineCraftUnity.UI
{
    /// <summary>
    /// MC-style debug overlay (F3): block XYZ, chunk region, FPS, memory.
    /// Console logs only when stats change (compact one-line) to avoid GC spam.
    /// </summary>
    public sealed class GameStatsOverlay : MonoBehaviour
    {
        [SerializeField] private bool visibleOnStart = true;
        [SerializeField] private bool logToConsole = true;
        [SerializeField] private bool logOnlyOnChange = true;
        [SerializeField] private float consoleHeartbeatInterval = 15f;
        [SerializeField] private Transform target;
        [SerializeField] private ChunkManager chunkManager;

        private bool _visible;
        private float _smoothedDeltaTime = 0.05f;
        private float _heartbeatTimer;
        private int _lastSignature;
        private GUIStyle _boxStyle;
        private GUIStyle _textStyle;
        private readonly StringBuilder _builder = new(256);
        private readonly StringBuilder _consoleBuilder = new(256);

        private void Awake()
        {
            _visible = visibleOnStart;
            _heartbeatTimer = consoleHeartbeatInterval;
            ResolveReferences();
        }

        private void Start()
        {
            if (_visible && logToConsole)
            {
                LogStatsToConsole();
            }
        }

        private void Update()
        {
            if (WasTogglePressed())
            {
                _visible = !_visible;
                if (logToConsole)
                {
                    Debug.Log(_visible ? "[MineCraft Debug] Overlay ON (F3)" : "[MineCraft Debug] Overlay OFF (F3)");
                    if (_visible)
                    {
                        LogStatsToConsole();
                    }
                }
            }

            _smoothedDeltaTime += (Time.unscaledDeltaTime - _smoothedDeltaTime) * 0.12f;

            if (!_visible || !logToConsole)
            {
                return;
            }

            _heartbeatTimer -= Time.unscaledDeltaTime;
            var signature = BuildSignature();
            if (!logOnlyOnChange || signature != _lastSignature)
            {
                LogStatsToConsole();
                _lastSignature = signature;
                _heartbeatTimer = consoleHeartbeatInterval;
                return;
            }

            if (_heartbeatTimer <= 0f)
            {
                LogStatsToConsole();
                _heartbeatTimer = consoleHeartbeatInterval;
            }
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                return;
            }

            EnsureStyles();
            ResolveReferences();

            const float width = 360f;
            const float margin = 10f;
            var statsText = BuildStatsText(includeHeader: true);
            var contentHeight = _textStyle.CalcHeight(new GUIContent(statsText), width - 16f);
            var rect = new Rect(margin, margin, width, contentHeight + 16f);

            GUI.Box(rect, GUIContent.none, _boxStyle);
            GUI.Label(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f), statsText, _textStyle);
        }

        public void SetVisible(bool visible) => _visible = visible;

        private void LogStatsToConsole()
        {
            ResolveReferences();
            _consoleBuilder.Clear();
            AppendCompactStats(_consoleBuilder);
            Debug.Log(_consoleBuilder.ToString());
        }

        private string BuildStatsText(bool includeHeader)
        {
            _builder.Clear();
            AppendStats(_builder, includeHeader);
            return _builder.ToString();
        }

        private int BuildSignature()
        {
            ResolveReferences();

            var fps = _smoothedDeltaTime > 0f ? 1f / _smoothedDeltaTime : 0f;
            var fpsBucket = Mathf.RoundToInt(fps / 5f);

            var blockX = 0;
            var blockY = 0;
            var blockZ = 0;
            if (target != null)
            {
                var pos = target.position;
                blockX = Mathf.FloorToInt(pos.x);
                blockY = Mathf.FloorToInt(pos.y);
                blockZ = Mathf.FloorToInt(pos.z);
            }

            var chunkX = FloorDiv(blockX, WorldConstants.ChunkSize);
            var chunkZ = FloorDiv(blockZ, WorldConstants.ChunkSize);
            var loaded = chunkManager != null ? chunkManager.LoadedChunkCount : 0;
            var genQ = chunkManager != null ? chunkManager.PendingGenerationCount : 0;
            var meshQ = chunkManager != null ? chunkManager.PendingMeshCount : 0;

            unchecked
            {
                var hash = 17;
                hash = hash * 31 + fpsBucket;
                hash = hash * 31 + chunkX;
                hash = hash * 31 + chunkZ;
                hash = hash * 31 + blockY / 4;
                hash = hash * 31 + loaded;
                hash = hash * 31 + genQ;
                hash = hash * 31 + meshQ;
                return hash;
            }
        }

        private void ResolveReferences()
        {
            if (target == null)
            {
                var player = GameObject.Find("Player");
                if (player != null)
                {
                    target = player.transform;
                }
            }

            if (chunkManager == null)
            {
                chunkManager = FindFirstObjectByType<ChunkManager>();
            }
        }

        private void AppendCompactStats(StringBuilder text)
        {
            var fps = _smoothedDeltaTime > 0f ? 1f / _smoothedDeltaTime : 0f;
            var frameMs = _smoothedDeltaTime * 1000f;
            var allocatedMb = BytesToMegabytes(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());

            text.Append("[MineCraft] ");
            text.Append("FPS=").Append(fps.ToString("0.0"));
            text.Append(" (").Append(frameMs.ToString("0.0")).Append("ms)");

            if (target != null)
            {
                var pos = target.position;
                var blockX = Mathf.FloorToInt(pos.x);
                var blockY = Mathf.FloorToInt(pos.y);
                var blockZ = Mathf.FloorToInt(pos.z);
                var chunkX = FloorDiv(blockX, WorldConstants.ChunkSize);
                var chunkZ = FloorDiv(blockZ, WorldConstants.ChunkSize);

                text.Append(" | XYZ=").Append(blockX).Append('/').Append(blockY).Append('/').Append(blockZ);
                text.Append(" | Chunk=").Append(chunkX).Append(',').Append(chunkZ);
            }

            text.Append(" | RAM=").Append(allocatedMb.ToString("0.0")).Append("MB");

            if (chunkManager != null)
            {
                text.Append(" | Chunks=").Append(chunkManager.LoadedChunkCount);
                text.Append(" | Q=").Append(chunkManager.PendingGenerationCount).Append('g').Append('/').Append(chunkManager.PendingMeshCount).Append('m');
                text.Append(" | Seed=").Append(chunkManager.Seed).Append(" View=").Append(chunkManager.ViewDistance);
            }
        }

        private void AppendStats(StringBuilder text, bool includeHeader)
        {
            var fps = _smoothedDeltaTime > 0f ? 1f / _smoothedDeltaTime : 0f;
            var frameMs = _smoothedDeltaTime * 1000f;

            if (includeHeader)
            {
                text.AppendLine("[MineCraft Debug]  (F3 toggle)");
            }

            text.AppendLine($"FPS: {fps:0.0}  ({frameMs:0.0} ms)");

            AppendMemoryLines(text);

            if (target != null)
            {
                var pos = target.position;
                var blockX = Mathf.FloorToInt(pos.x);
                var blockY = Mathf.FloorToInt(pos.y);
                var blockZ = Mathf.FloorToInt(pos.z);
                var chunkX = FloorDiv(blockX, WorldConstants.ChunkSize);
                var chunkZ = FloorDiv(blockZ, WorldConstants.ChunkSize);
                var localX = Mod(blockX, WorldConstants.ChunkSize);
                var localZ = Mod(blockZ, WorldConstants.ChunkSize);

                text.AppendLine($"XYZ block: {blockX} / {blockY} / {blockZ}");
                text.AppendLine($"Chunk vùng: {chunkX}, {chunkZ}  (local {localX}, {localZ})");
                text.AppendLine($"Section Y: {FloorSectionY(blockY)}  (sea {WorldConstants.SeaLevel})");
            }
            else
            {
                text.AppendLine("XYZ block: (no player)");
            }

            if (chunkManager != null)
            {
                text.AppendLine($"Chunks: {chunkManager.LoadedChunkCount} loaded");
                text.AppendLine($"Queue: {chunkManager.PendingGenerationCount} gen, {chunkManager.PendingMeshCount} mesh");
                text.AppendLine($"Seed: {chunkManager.Seed}  View: {chunkManager.ViewDistance}");
            }
        }

        private static void AppendMemoryLines(StringBuilder text)
        {
            var allocatedMb = BytesToMegabytes(UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());
            var reservedMb = BytesToMegabytes(UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong());
            var monoMb = BytesToMegabytes(UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong());
            var gcMb = BytesToMegabytes(System.GC.GetTotalMemory(false));

            text.AppendLine($"RAM Unity: {allocatedMb:0.0} MB used / {reservedMb:0.0} MB reserved");
            text.AppendLine($"RAM Mono: {monoMb:0.0} MB  |  GC: {gcMb:0.0} MB");
        }

        private static float BytesToMegabytes(long bytes) => bytes / (1024f * 1024f);

        private static int FloorSectionY(int blockY) =>
            Mathf.FloorToInt(blockY / 16f) * 16;

        private static int FloorDiv(int value, int divisor)
        {
            var div = value / divisor;
            var rem = value % divisor;
            if (rem != 0 && ((rem < 0) ^ (divisor < 0)))
            {
                div--;
            }

            return div;
        }

        private static int Mod(int value, int size)
        {
            var mod = value % size;
            return mod < 0 ? mod + size : mod;
        }

        private static bool WasTogglePressed()
        {
            return Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame;
        }

        private void EnsureStyles()
        {
            if (_boxStyle != null)
            {
                return;
            }

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.UpperLeft
            };
            _boxStyle.normal.background = MakeBackgroundTexture(new Color(0f, 0f, 0f, 0.72f));

            _textStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 13,
                richText = false,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.98f, 0.92f, 1f) }
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
