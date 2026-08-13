using MineCraftUnity.World;
using UnityEngine;
using UnityEngine.Rendering;

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

        [SerializeField] private long startDayTime = 1000;
        [SerializeField] private float timeScale = 1f;
        [SerializeField] private bool advanceTime = true;
        [SerializeField] private Light sunLight;
        [SerializeField] private Material skyboxMaterial;

        private WorldTime _worldTime;
        private OverworldSkyVisuals.Snapshot _currentSnapshot;
        private bool _initialized;

        public WorldTime WorldTime => _worldTime;
        public OverworldSkyVisuals.Snapshot CurrentSnapshot => _currentSnapshot;

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

            ApplyVisuals(OverworldSkyVisuals.Evaluate(_worldTime.DayFraction));
        }

        public void SetDayTime(long dayTime)
        {
            _worldTime.SetDayTime(dayTime);
            ApplyVisuals(OverworldSkyVisuals.Evaluate(_worldTime.DayFraction));
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

        public static DayNightController EnsureOnWorld(GameObject worldRoot, long dayTime = 1000)
        {
            var controller = worldRoot.GetComponent<DayNightController>();
            if (controller == null)
            {
                controller = worldRoot.AddComponent<DayNightController>();
            }

            controller.startDayTime = dayTime;
            controller._worldTime ??= new WorldTime(dayTime);
            controller._worldTime.SetDayTime(dayTime);
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
            ApplyVisuals(OverworldSkyVisuals.Evaluate(_worldTime.DayFraction));
            _initialized = sunLight != null && skyboxMaterial != null;
        }

        private void ApplyVisuals(OverworldSkyVisuals.Snapshot snapshot)
        {
            _currentSnapshot = snapshot;

            if (skyboxMaterial != null)
            {
                skyboxMaterial.SetColor("_SkyTop", snapshot.SkyTop);
                skyboxMaterial.SetColor("_SkyHorizon", snapshot.SkyHorizon);
                skyboxMaterial.SetVector("_SunDirection", snapshot.SunDirection);
                skyboxMaterial.SetColor("_SunColor", snapshot.SunDiscColor);
                skyboxMaterial.SetFloat("_SunSize", snapshot.SunDiscSize);
                skyboxMaterial.SetVector("_MoonDirection", snapshot.MoonDirection);
                skyboxMaterial.SetColor("_MoonColor", snapshot.MoonDiscColor);
                skyboxMaterial.SetFloat("_MoonSize", snapshot.MoonDiscSize);
                skyboxMaterial.SetFloat("_StarBrightness", snapshot.StarBrightness);
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
