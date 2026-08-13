using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MineCraftUnity.UI
{
    /// <summary>
    /// Records FPS 1%-low, spike max, and GC alloc/frame for regression comparison (Phase 0).
    /// Press F8 to write baseline.json. Runs in Editor and Development builds only.
    /// </summary>
    public sealed class PerformanceBaselineRecorder : MonoBehaviour
    {
        private const int SampleCapacity = 3600;
        private const int SortBufferCapacity = 3600;

        [SerializeField] private bool recordOnStart = true;
        [SerializeField] private int seed = 12345;

        private readonly float[] _frameMs = new float[SampleCapacity];
        private int _sampleCount;
        private float _maxSpikeMs;
        private long _maxGcAllocBytes;
        private float[] _sortBuffer;

        private void Update()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            return;
#else
            if (!recordOnStart)
            {
                return;
            }

            var frameMs = Time.unscaledDeltaTime * 1000f;
            if (_sampleCount < SampleCapacity)
            {
                _frameMs[_sampleCount++] = frameMs;
            }

            if (frameMs > _maxSpikeMs)
            {
                _maxSpikeMs = frameMs;
            }

            var gcAlloc = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            if (gcAlloc > _maxGcAllocBytes)
            {
                _maxGcAllocBytes = gcAlloc;
            }

            if (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame)
            {
                WriteBaseline("manual_f8");
            }
#endif
        }

        public void WriteBaseline(string tag)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var path = Path.Combine(Application.dataPath, "..", "Logs", "baseline.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

            var onePercentLowMs = ComputeOnePercentLowMs();
            var avgFps = ComputeAverageFps();

            var json = new StringBuilder(256);
            json.Append("{\n");
            json.Append("  \"tag\": \"").Append(Escape(tag)).Append("\",\n");
            json.Append("  \"timestamp\": \"").Append(DateTime.UtcNow.ToString("o")).Append("\",\n");
            json.Append("  \"seed\": ").Append(seed).Append(",\n");
            json.Append("  \"samples\": ").Append(_sampleCount).Append(",\n");
            json.Append("  \"avgFps\": ").Append(avgFps.ToString("F1")).Append(",\n");
            json.Append("  \"onePercentLowMs\": ").Append(onePercentLowMs.ToString("F2")).Append(",\n");
            json.Append("  \"maxSpikeMs\": ").Append(_maxSpikeMs.ToString("F2")).Append(",\n");
            json.Append("  \"notes\": \"Compare after each optimization phase; lower spike/max is better.\"\n");
            json.Append("}\n");

            File.WriteAllText(path, json.ToString());
            Debug.Log($"[PerformanceBaseline] Wrote {path} (avg FPS {avgFps:F1}, 1% low {onePercentLowMs:F1}ms, max spike {_maxSpikeMs:F1}ms)");
#endif
        }

        private float ComputeAverageFps()
        {
            if (_sampleCount == 0)
            {
                return 0f;
            }

            var sum = 0f;
            for (var i = 0; i < _sampleCount; i++)
            {
                sum += _frameMs[i];
            }

            var avgMs = sum / _sampleCount;
            return avgMs > 0f ? 1000f / avgMs : 0f;
        }

        private float ComputeOnePercentLowMs()
        {
            if (_sampleCount == 0)
            {
                return 0f;
            }

            _sortBuffer ??= new float[SortBufferCapacity];
            var count = Math.Min(_sampleCount, _sortBuffer.Length);
            Array.Copy(_frameMs, _sortBuffer, count);
            Array.Sort(_sortBuffer, 0, count);

            var index = Math.Min(count - 1, Math.Max(0, (int)Math.Ceiling(count * 0.99f) - 1));
            return _sortBuffer[index];
        }

        private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
