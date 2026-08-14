using MineCraftUnity.Player;
using MineCraftUnity.Rendering;
using MineCraftUnity.World;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MineCraftUnity.Rendering
{
    /// <summary>
    /// MC ref: Timelines.OVERWORLD_DAY + SkyRenderer — drives sun, skybox, fog, ambient, block sky light.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DayNightController : MonoBehaviour
    {
        public static DayNightController Instance { get; private set; }

        public static readonly int SkyLightGlobalId = Shader.PropertyToID("_MineCraftSkyLight");

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BootstrapSkyLightGlobal()
        {
            Shader.SetGlobalFloat(SkyLightGlobalId, 1f);
        }

        public static void EnsureDefaultSkyLight()
        {
            if (Instance == null)
            {
                Shader.SetGlobalFloat(SkyLightGlobalId, 1f);
            }
        }

        [SerializeField] private long startDayTime = 6000;
        [SerializeField] private float timeScale = 1f;
        [SerializeField] private bool advanceTime = true;
        [SerializeField] private float weatherTransitionSpeed = 0.015f;
        [SerializeField] private Light sunLight;
        [SerializeField] private Material skyboxMaterial;

        private WorldTime _worldTime;
        private OverworldSkyVisuals.Snapshot _currentSnapshot;
        private bool _initialized;
        private float _rainLevel;
        private float _targetRainLevel;
        private bool _thundering;

        public WorldTime WorldTime => _worldTime;
        public float RainLevel => _rainLevel;
        public bool IsThundering => _thundering && _rainLevel > 0.25f;
        public OverworldSkyVisuals.Snapshot CurrentSnapshot => _currentSnapshot;

        public OverworldSkyVisuals.Snapshot BuildSnapshot()
        {
            return OverworldSkyVisuals.Evaluate(
                _worldTime.DayFraction,
                _worldTime.DayTime,
                _rainLevel,
                IsThundering ? 1f : 0f,
                SamplePlayerBiome());
        }

        private static BiomeId SamplePlayerBiome()
        {
            var player = FindFirstObjectByType<PlayerController>();
            var chunkManager = FindFirstObjectByType<ChunkManager>();
            if (player == null || chunkManager == null)
            {
                return BiomeId.Unknown;
            }

            var position = player.transform.position;
            var worldX = Mathf.FloorToInt(position.x);
            var worldY = Mathf.FloorToInt(position.y);
            var worldZ = Mathf.FloorToInt(position.z);
            return chunkManager.Level.GetBiome(worldX, worldY, worldZ);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            _worldTime = new WorldTime(startDayTime);
            EnsureSceneReferences();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Shader.SetGlobalFloat(SkyLightGlobalId, 1f);
            }
        }

        private void Update()
        {
            if (!_initialized)
            {
                EnsureSceneReferences();
            }

            if (advanceTime)
            {
                _worldTime.Advance(Time.deltaTime, timeScale);
            }

            UpdateWeather();
            ApplyVisuals(BuildSnapshot());
        }

        private void UpdateWeather()
        {
            _rainLevel = Mathf.MoveTowards(_rainLevel, _targetRainLevel, weatherTransitionSpeed);
            if (!_thundering && _targetRainLevel <= 0f && _rainLevel <= 0.001f)
            {
                _rainLevel = 0f;
            }
        }

        public void SetWeatherClear()
        {
            _targetRainLevel = 0f;
            _thundering = false;
            ApplyVisuals(BuildSnapshot());
            GetComponent<CelestialRenderer>()?.RefreshNow();
        }

        public void SetWeatherRain()
        {
            _targetRainLevel = 1f;
            _thundering = false;
            ApplyVisuals(BuildSnapshot());
            GetComponent<CelestialRenderer>()?.RefreshNow();
        }

        public void SetWeatherThunder()
        {
            _targetRainLevel = 1f;
            _thundering = true;
            ApplyVisuals(BuildSnapshot());
            GetComponent<CelestialRenderer>()?.RefreshNow();
        }

        private void LateUpdate()
        {
            ConfigureMainCamera();
        }

        public void SetDayTime(long dayTime)
        {
            _worldTime.SetDayTime(dayTime);
            ApplyVisuals(BuildSnapshot());

            GetComponent<CelestialRenderer>()?.RefreshNow();
        }

        public void CopyCurrentOverworldVisuals(
            out bool fogEnabled,
            out FogMode fogMode,
            out Color fogColor,
            out float fogStart,
            out float fogEnd,
            out AmbientMode ambientMode,
            out Color ambientSky,
            out Color ambientEquator,
            out Color ambientGround)
        {
            fogEnabled = RenderSettings.fog;
            fogMode = RenderSettings.fogMode;
            fogColor = _currentSnapshot.FogColor;
            fogStart = _currentSnapshot.FogStart;
            fogEnd = _currentSnapshot.FogEnd;
            ambientMode = AmbientMode.Trilight;
            ambientSky = _currentSnapshot.AmbientSky;
            ambientEquator = _currentSnapshot.AmbientEquator;
            ambientGround = _currentSnapshot.AmbientGround;
        }

        public static DayNightController EnsureOnWorld(GameObject worldRoot, long dayTime = 6000)
        {
            var controller = worldRoot.GetComponent<DayNightController>();
            if (controller == null)
            {
                controller = worldRoot.AddComponent<DayNightController>();
            }

            controller.startDayTime = dayTime;
            if (controller._worldTime == null)
            {
                controller._worldTime = new WorldTime(dayTime);
            }
            else
            {
                controller._worldTime.SetDayTime(dayTime);
            }

            controller.EnsureSceneReferences();
            return controller;
        }

        private void EnsureSceneReferences()
        {
            if (sunLight == null)
            {
                sunLight = FindSunLight();
            }

            if (skyboxMaterial == null)
            {
                skyboxMaterial = CreateSkyboxMaterial();
            }

            if (sunLight != null)
            {
                RenderSettings.sun = sunLight;
            }

            if (skyboxMaterial != null)
            {
                RenderSettings.skybox = skyboxMaterial;
            }

            ConfigureMainCamera();
            ApplyVisuals(BuildSnapshot());
            _initialized = sunLight != null && skyboxMaterial != null;

            CelestialRenderer.EnsureOnDayNightController(this);
        }

        private void ApplyVisuals(OverworldSkyVisuals.Snapshot snapshot)
        {
            _currentSnapshot = snapshot;

            if (skyboxMaterial != null)
            {
                skyboxMaterial.SetColor("_SkyColor", snapshot.SkyTop);
                skyboxMaterial.SetColor("_FogHorizonColor", snapshot.SkyHorizon);
                skyboxMaterial.SetFloat("_StarBrightness", snapshot.StarBrightness);
                skyboxMaterial.SetFloat("_StarAngle", snapshot.StarAngleRadians);
            }

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = snapshot.FogColor;
            RenderSettings.fogStartDistance = snapshot.FogStart;
            RenderSettings.fogEndDistance = snapshot.FogEnd;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = snapshot.AmbientSky;
            RenderSettings.ambientEquatorColor = snapshot.AmbientEquator;
            RenderSettings.ambientGroundColor = snapshot.AmbientGround;

            Shader.SetGlobalFloat(SkyLightGlobalId, snapshot.SkyLightFactor);

            if (sunLight == null)
            {
                return;
            }

            sunLight.transform.rotation = snapshot.SunLightRotation;
            sunLight.color = snapshot.SunLightColor;
            sunLight.intensity = snapshot.SunLightIntensity;
            sunLight.shadows = snapshot.SunLightIntensity > 0.2f
                ? LightShadows.Soft
                : LightShadows.None;
        }

        private static Light FindSunLight()
        {
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    return light;
                }
            }

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.intensity = 1f;
            return sun;
        }

        private static Material CreateSkyboxMaterial()
        {
            var shader = Shader.Find("MineCraft/OverworldSkybox")
                ?? Shader.Find("Skybox/Procedural");
            if (shader == null)
            {
                Debug.LogWarning("[MineCraft] OverworldSkybox shader not found.");
                return null;
            }

            return new Material(shader) { name = "Mat_OverworldSkybox" };
        }

        private static void ConfigureMainCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.clearFlags = CameraClearFlags.Skybox;
        }
    }
}
