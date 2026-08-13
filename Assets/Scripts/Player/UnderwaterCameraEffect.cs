using MineCraftUnity.Rendering;
using MineCraftUnity.World;
using UnityEngine;
using UnityEngine.Rendering;

namespace MineCraftUnity.Player
{
    /// <summary>
    /// MC ref: GameRenderer underwater fog — blue murk + limited visibility when eyes in water.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UnderwaterCameraEffect : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private ChunkManager chunkManager;
        [SerializeField] private Camera targetCamera;

        [Header("Underwater look")]
        [SerializeField] private Color underwaterFogColor = new(0.05f, 0.14f, 0.32f, 1f);
        [SerializeField] private float fogStart = 0f;
        [SerializeField] private float fogEnd = 18f;
        [SerializeField] private float blendSpeed = 5f;

        private float _blend;
        private bool _defaultsCaptured;
        private bool _savedFogEnabled;
        private FogMode _savedFogMode;
        private Color _savedFogColor;
        private float _savedFogStart;
        private float _savedFogEnd;
        private float _savedFogDensity;
        private CameraClearFlags _savedClearFlags;
        private Color _savedBackgroundColor;
        private AmbientMode _savedAmbientMode;
        private Color _savedAmbientSky;
        private Color _savedAmbientEquator;
        private Color _savedAmbientGround;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            if (player == null)
            {
                player = GetComponentInParent<PlayerController>();
            }

            if (chunkManager == null)
            {
                chunkManager = FindFirstObjectByType<ChunkManager>();
            }

            CaptureDefaults();
        }

        private void LateUpdate()
        {
            RefreshOverworldDefaults();

            var target = GetSubmergeTarget();
            _blend = Mathf.MoveTowards(_blend, target, blendSpeed * Time.deltaTime);

            if (_blend > 0.001f)
            {
                ApplyUnderwater(_blend);
            }
            else if (_defaultsCaptured)
            {
                RestoreDefaults();
            }
        }

        private float GetSubmergeTarget()
        {
            if (player != null)
            {
                if (player.IsHeadUnderwater)
                {
                    return 1f;
                }

                if (player.IsInWater)
                {
                    return 0.35f;
                }

                return 0f;
            }

            if (chunkManager == null || targetCamera == null)
            {
                return 0f;
            }

            var eye = targetCamera.transform.position;
            return chunkManager.Level.IsWaterAt(
                Mathf.FloorToInt(eye.x),
                Mathf.FloorToInt(eye.y),
                Mathf.FloorToInt(eye.z))
                ? 1f
                : 0f;
        }

        private void CaptureDefaults()
        {
            if (_defaultsCaptured && DayNightController.Instance == null)
            {
                return;
            }

            RefreshOverworldDefaults();
            _defaultsCaptured = true;
        }

        private void RefreshOverworldDefaults()
        {
            if (DayNightController.Instance != null)
            {
                DayNightController.Instance.CopyCurrentOverworldVisuals(
                    out _savedFogEnabled,
                    out _savedFogMode,
                    out _savedFogColor,
                    out _savedFogStart,
                    out _savedFogEnd,
                    out _savedAmbientMode,
                    out _savedAmbientSky,
                    out _savedAmbientEquator,
                    out _savedAmbientGround);
            }
            else if (!_defaultsCaptured)
            {
                _savedFogEnabled = RenderSettings.fog;
                _savedFogMode = RenderSettings.fogMode;
                _savedFogColor = RenderSettings.fogColor;
                _savedFogStart = RenderSettings.fogStartDistance;
                _savedFogEnd = RenderSettings.fogEndDistance;
                _savedFogDensity = RenderSettings.fogDensity;
                _savedAmbientMode = RenderSettings.ambientMode;
                _savedAmbientSky = RenderSettings.ambientSkyColor;
                _savedAmbientEquator = RenderSettings.ambientEquatorColor;
                _savedAmbientGround = RenderSettings.ambientGroundColor;
            }

            if (targetCamera != null)
            {
                _savedClearFlags = CameraClearFlags.Skybox;
                _savedBackgroundColor = DayNightController.Instance != null
                    ? DayNightController.Instance.CurrentSnapshot.SkyHorizon
                    : targetCamera.backgroundColor;
            }
        }

        private void ApplyUnderwater(float blend)
        {
            var fogColor = GetBiomeUnderwaterFogColor();
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.Lerp(_savedFogColor, fogColor, blend);
            RenderSettings.fogStartDistance = Mathf.Lerp(_savedFogStart, fogStart, blend);
            RenderSettings.fogEndDistance = Mathf.Lerp(_savedFogEnd, fogEnd, blend);

            var ambient = Color.Lerp(_savedAmbientSky, fogColor * 0.65f, blend);
            RenderSettings.ambientSkyColor = ambient;
            RenderSettings.ambientEquatorColor = Color.Lerp(_savedAmbientEquator, ambient, blend);
            RenderSettings.ambientGroundColor = Color.Lerp(_savedAmbientGround, ambient * 0.5f, blend);

            if (targetCamera == null)
            {
                return;
            }

            targetCamera.backgroundColor = Color.Lerp(_savedBackgroundColor, fogColor, blend);
            if (blend > 0.65f)
            {
                targetCamera.clearFlags = CameraClearFlags.SolidColor;
            }
            else
            {
                targetCamera.clearFlags = _savedClearFlags;
            }
        }

        private Color GetBiomeUnderwaterFogColor()
        {
            if (chunkManager == null || targetCamera == null)
            {
                return underwaterFogColor;
            }

            var eye = targetCamera.transform.position;
            var biome = chunkManager.Level.GetBiome(
                Mathf.FloorToInt(eye.x),
                Mathf.FloorToInt(eye.y),
                Mathf.FloorToInt(eye.z));
            if (biome == BiomeId.Unknown)
            {
                return underwaterFogColor;
            }

            return BiomeRegistry.GetWaterFogColor(biome);
        }

        private void RestoreDefaults()
        {
            RenderSettings.fog = _savedFogEnabled;
            RenderSettings.fogMode = _savedFogMode;
            RenderSettings.fogColor = _savedFogColor;
            RenderSettings.fogStartDistance = _savedFogStart;
            RenderSettings.fogEndDistance = _savedFogEnd;
            RenderSettings.fogDensity = _savedFogDensity;

            RenderSettings.ambientSkyColor = _savedAmbientSky;
            RenderSettings.ambientEquatorColor = _savedAmbientEquator;
            RenderSettings.ambientGroundColor = _savedAmbientGround;
            RenderSettings.ambientMode = _savedAmbientMode;

            if (targetCamera != null)
            {
                targetCamera.clearFlags = _savedClearFlags;
                targetCamera.backgroundColor = _savedBackgroundColor;
            }
        }
    }
}
