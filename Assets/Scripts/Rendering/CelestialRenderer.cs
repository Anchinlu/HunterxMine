using MineCraftUnity.Core;
using MineCraftUnity.Player;
using MineCraftUnity.World;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MineCraftUnity.Rendering
{
    /// <summary>
    /// MC ref: SkyRenderer — sun/moon quads on camera-attached celestial dome.
    /// </summary>
    [DefaultExecutionOrder(250)]
    [DisallowMultipleComponent]
    public sealed class CelestialRenderer : MonoBehaviour
    {
        private const float McSunScale = 28f;
        private const float McMoonScale = 20f;
        private const float McSunOrbitRadius = 165f;
        private const float McMoonOrbitRadius = 100f;
        private const float McSunOpacity = 0.72f;
        private const float McDarkDiscRadius = 128f;
        private const float SunHorizonHeight = -0.05f;
        private const float MoonHorizonHeight = 0.02f;

        [SerializeField] private Camera targetCamera;

        private Transform _domeRoot;
        private Transform _sunrisePivot;
        private Transform _sunPivot;
        private Transform _moonPivot;
        private Transform _darkDiscPivot;
        private Transform _sun;
        private Transform _moon;
        private Transform _sunriseFan;
        private Transform _darkDisc;
        private Mesh _sunQuadMesh;
        private Mesh _moonQuadMesh;
        private Mesh _sunriseMesh;
        private Mesh _darkDiscMesh;
        private MaterialPropertyBlock _propertyBlock;
        private MoonPhase _activeMoonPhase = (MoonPhase)(-1);

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Initialize();
            RefreshCelestialPositions();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            RefreshCelestialPositions();
        }

        public void RefreshNow()
        {
            RefreshCelestialPositions();
        }

        public void Initialize()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _sunQuadMesh ??= BuildSunQuadMesh();
            _moonQuadMesh ??= BuildMoonQuadMesh();
            _sunriseMesh ??= BuildSunriseFanMesh();
            _darkDiscMesh ??= BuildDarkDiscMesh();
            EnsureHierarchy();
        }

        public static CelestialRenderer EnsureOnDayNightController(DayNightController controller)
        {
            var renderer = controller.GetComponent<CelestialRenderer>();
            if (renderer == null)
            {
                renderer = controller.gameObject.AddComponent<CelestialRenderer>();
            }

            renderer.Initialize();
            if (Application.isPlaying)
            {
                renderer.RefreshCelestialPositions();
            }

            return renderer;
        }

        private void RefreshCelestialPositions()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (!_domeRoot || !_sun || !_moon)
            {
                Initialize();
            }

            if (targetCamera == null || !_domeRoot)
            {
                return;
            }

            var snapshot = ResolveSnapshot();
            var cameraTransform = targetCamera.transform;

            _domeRoot.gameObject.SetActive(true);
            // MC ref: sky dome follows camera position and yaw only — not pitch/roll.
            var cameraYaw = cameraTransform.eulerAngles.y;
            _domeRoot.SetPositionAndRotation(cameraTransform.position, Quaternion.Euler(0f, cameraYaw, 0f));

            ApplySunriseFan(snapshot);
            ApplyMcOrbit(_sunPivot, _sun, _sunQuadMesh, snapshot.SunAngleRadians, snapshot.SunDiscColor,
                McSunScale, snapshot.RainBrightness, McSunOrbitRadius, McSunOpacity, isMoon: false);
            ApplyMcOrbit(_moonPivot, _moon, _moonQuadMesh, snapshot.MoonAngleRadians, snapshot.MoonDiscColor,
                McMoonScale, snapshot.RainBrightness, McMoonOrbitRadius, 1f, isMoon: true);
            UpdateMoonMaterial(snapshot.MoonPhase);
            ApplyDarkDisc(snapshot, cameraTransform.position.y);
        }

        private static OverworldSkyVisuals.Snapshot ResolveSnapshot()
        {
            var controller = DayNightController.Instance;
            if (controller != null)
            {
                return controller.BuildSnapshot();
            }

            return OverworldSkyVisuals.Evaluate(6000f / WorldTime.TicksPerDay, 6000);
        }

        private void EnsureHierarchy()
        {
            _domeRoot = ResolveDomeRoot();
            _sunrisePivot = EnsureChild(_domeRoot, "SunrisePivot");
            _sunPivot = EnsureChild(_domeRoot, "SunPivot");
            _moonPivot = EnsureChild(_domeRoot, "MoonPivot");
            _darkDiscPivot = EnsureChild(_domeRoot, "DarkDiscPivot");

            ReparentLegacyBillboard(_domeRoot, "SunBillboard", _sunPivot);
            ReparentLegacyBillboard(_domeRoot, "MoonBillboard", _moonPivot);

            _sunriseFan = EnsureMeshBody(_sunrisePivot, "SunriseFan", _sunriseMesh, CelestialMaterialLibrary.SunriseMaterial);
            _sun = EnsureMeshBody(_sunPivot, "SunBillboard", _sunQuadMesh, CelestialMaterialLibrary.SunMaterial);
            _moon = EnsureMeshBody(_moonPivot, "MoonBillboard", _moonQuadMesh, CelestialMaterialLibrary.GetMoonMaterial(MoonPhase.FullMoon));
            _darkDisc = EnsureMeshBody(_darkDiscPivot, "DarkDisc", _darkDiscMesh, CelestialMaterialLibrary.DarkDiscMaterial);
            _darkDisc.gameObject.SetActive(false);

            RemoveDuplicateDome(_domeRoot);
        }

        /// <summary>Prefer existing scene folder: Celestials, then CelestialDome.</summary>
        private Transform ResolveDomeRoot()
        {
            var celestials = transform.Find("Celestials");
            if (celestials != null)
            {
                return celestials;
            }

            return EnsureChild(transform, "CelestialDome");
        }

        private static void ReparentLegacyBillboard(Transform domeRoot, string billboardName, Transform pivot)
        {
            var legacy = domeRoot.Find(billboardName);
            if (legacy == null || legacy.parent == pivot)
            {
                return;
            }

            legacy.SetParent(pivot, false);
        }

        /// <summary>Remove stale CelestialDome sibling when Celestials is the active root.</summary>
        private void RemoveDuplicateDome(Transform activeRoot)
        {
            var celestials = transform.Find("Celestials");
            var dome = transform.Find("CelestialDome");
            if (celestials == null || dome == null || celestials != activeRoot)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(dome.gameObject);
                return;
            }

#if UNITY_EDITOR
            DestroyImmediate(dome.gameObject);
#endif
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing;
            }

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private Transform EnsureMeshBody(Transform pivot, string name, Mesh mesh, Material material)
        {
            var existing = pivot.Find(name);
            Transform body;
            if (existing != null)
            {
                body = existing;
            }
            else
            {
                var go = new GameObject(name);
                go.transform.SetParent(pivot, false);
                go.AddComponent<MeshFilter>();
                go.AddComponent<MeshRenderer>();
                body = go.transform;
            }

            var meshFilter = body.GetComponent<MeshFilter>();
            var meshRenderer = body.GetComponent<MeshRenderer>();
            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            return body;
        }

        private void UpdateMoonMaterial(MoonPhase phase)
        {
            if (_moon == null || _activeMoonPhase == phase)
            {
                return;
            }

            var renderer = _moon.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.sharedMaterial = CelestialMaterialLibrary.GetMoonMaterial(phase);
            _activeMoonPhase = phase;
        }

        private void ApplySunriseFan(OverworldSkyVisuals.Snapshot snapshot)
        {
            if (_sunrisePivot == null || _sunriseFan == null)
            {
                return;
            }

            var color = snapshot.SunriseSunsetColor;
            if (color.a <= 0.001f)
            {
                _sunriseFan.gameObject.SetActive(false);
                return;
            }

            _sunriseFan.gameObject.SetActive(true);

            var zAngle = Mathf.Sin(snapshot.SunAngleRadians) < 0f ? 270f : 90f;
            _sunrisePivot.localRotation = Quaternion.Euler(90f, 0f, zAngle);
            _sunriseFan.localPosition = Vector3.zero;
            _sunriseFan.localRotation = Quaternion.identity;
            _sunriseFan.localScale = new Vector3(1f, 1f, color.a);

            var renderer = _sunriseFan.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.SetColor("_Color", color);
            renderer.SetPropertyBlock(_propertyBlock);
        }

        private void ApplyDarkDisc(OverworldSkyVisuals.Snapshot snapshot, float cameraY)
        {
            if (_darkDiscPivot == null || _darkDisc == null)
            {
                return;
            }

            var belowHorizon = cameraY < WorldConstants.SeaLevel && !IsCameraUnderwater();
            _darkDisc.gameObject.SetActive(belowHorizon);
            if (!belowHorizon)
            {
                return;
            }

            _darkDiscPivot.localPosition = new Vector3(0f, 12f, 0f);
            _darkDisc.localPosition = Vector3.zero;
            _darkDisc.localRotation = Quaternion.identity;
            _darkDisc.localScale = Vector3.one;

            var renderer = _darkDisc.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }

            var discColor = Color.Lerp(snapshot.SkyHorizon, snapshot.SkyTop, 0.35f);
            discColor.a = 1f;
            renderer.sharedMaterial = CelestialMaterialLibrary.DarkDiscMaterial;
            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.SetColor("_Color", discColor);
            renderer.SetPropertyBlock(_propertyBlock);
        }

        private static bool IsCameraUnderwater()
        {
            var player = Object.FindFirstObjectByType<PlayerController>();
            return player != null && player.IsHeadUnderwater;
        }

        /// <summary>MC rule: sun when cos(angle) above horizon; moon when opposite side is up.</summary>
        private static bool IsDiscAboveHorizon(float angleRadians, bool isMoon)
        {
            var height = Mathf.Cos(angleRadians);
            return isMoon ? height > MoonHorizonHeight : height > SunHorizonHeight;
        }

        private void ApplyMcOrbit(
            Transform pivot,
            Transform body,
            Mesh mesh,
            float angleRadians,
            Color tint,
            float scale,
            float rainBrightness,
            float orbitRadius,
            float opacityMultiplier,
            bool isMoon)
        {
            if (pivot == null || body == null)
            {
                return;
            }

            pivot.localRotation = Quaternion.AngleAxis(-90f, Vector3.up)
                * Quaternion.AngleAxis(angleRadians * Mathf.Rad2Deg, Vector3.right);

            var visible = IsDiscAboveHorizon(angleRadians, isMoon);
            body.gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            var meshFilter = body.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != mesh)
            {
                meshFilter.sharedMesh = mesh;
            }

            body.localPosition = new Vector3(0f, orbitRadius, 0f);
            body.localRotation = Quaternion.identity;
            body.localScale = new Vector3(scale, 1f, scale);

            Color displayTint;
            if (isMoon)
            {
                displayTint = tint;
                displayTint.a = tint.a * rainBrightness;
            }
            else
            {
                displayTint = new Color(1f, 0.97f, 0.9f, rainBrightness * opacityMultiplier);
            }

            var renderer = body.GetComponent<MeshRenderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.enabled = true;
            _propertyBlock ??= new MaterialPropertyBlock();
            _propertyBlock.SetColor("_Color", displayTint);
            renderer.SetPropertyBlock(_propertyBlock);
        }

        private static Mesh BuildSunQuadMesh() => BuildXZQuadMesh("CelestialSunQuad", flipUv: false);

        private static Mesh BuildMoonQuadMesh() => BuildXZQuadMesh("CelestialMoonQuad", flipUv: true);

        private static Mesh BuildXZQuadMesh(string meshName, bool flipUv)
        {
            var mesh = new Mesh { name = meshName };
            mesh.vertices = new[]
            {
                new Vector3(-1f, 0f, -1f),
                new Vector3(1f, 0f, -1f),
                new Vector3(1f, 0f, 1f),
                new Vector3(-1f, 0f, 1f)
            };
            mesh.uv = flipUv
                ? new[]
                {
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f),
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f)
                }
                : new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 1f)
                };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh BuildSunriseFanMesh()
        {
            const int steps = 16;
            var vertexCount = steps + 2;
            var vertices = new Vector3[vertexCount];
            var colors = new Color[vertexCount];
            var triangles = new int[steps * 3];

            vertices[0] = new Vector3(0f, McSunOrbitRadius, 0f);
            colors[0] = new Color(1f, 1f, 1f, 1f);

            for (var i = 0; i <= steps; i++)
            {
                var angle = i * Mathf.PI * 2f / steps;
                var sin = Mathf.Sin(angle);
                var cos = Mathf.Cos(angle);
                vertices[i + 1] = new Vector3(sin * 120f, cos * 120f, -cos * 40f);
                colors[i + 1] = new Color(1f, 1f, 1f, 0f);

                if (i < steps)
                {
                    var tri = i * 3;
                    triangles[tri] = 0;
                    triangles[tri + 1] = i + 1;
                    triangles[tri + 2] = i + 2;
                }
            }

            var mesh = new Mesh { name = "SunriseFan" };
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }

        private static Mesh BuildDarkDiscMesh()
        {
            const int segments = 8;
            var vertexCount = segments + 2;
            var vertices = new Vector3[vertexCount];
            var triangles = new int[segments * 3];

            vertices[0] = new Vector3(0f, -16f, 0f);
            for (var i = 0; i <= segments; i++)
            {
                var degrees = -180f + i * (360f / segments);
                var radians = degrees * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(
                    -McDarkDiscRadius * Mathf.Cos(radians),
                    -16f,
                    McDarkDiscRadius * Mathf.Sin(radians));

                if (i < segments)
                {
                    var tri = i * 3;
                    triangles[tri] = 0;
                    triangles[tri + 1] = i + 2;
                    triangles[tri + 2] = i + 1;
                }
            }

            var mesh = new Mesh { name = "SkyDarkDisc" };
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
