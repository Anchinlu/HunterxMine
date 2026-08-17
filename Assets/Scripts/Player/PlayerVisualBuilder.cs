using System.Collections.Generic;
using MineCraftUnity.Rendering;
using UnityEngine;

namespace MineCraftUnity.Player
{
    /// <summary>
    /// Builds a blocky Steve-style player model from Unity cubes under PlayerVisual.
    /// MC scale: 16 pixels = 1 Unity unit.
    /// Hierarchy:
    ///   PlayerVisual
    ///   ├── UpperBodyPivot (0,0,0)
    ///   │   ├── Head
    ///   │   ├── Body
    ///   │   ├── LeftArmPivot
    ///   │   │   └── LeftArm → LeftHandSocket
    ///   │   └── RightArmPivot
    ///   │       └── RightArm → RightHandSocket
    ///   ├── LeftLegPivot
    ///   │   └── LeftLeg
    ///   └── RightLegPivot
    ///       └── RightLeg
    /// </summary>
    public static class PlayerVisualBuilder
    {
        public const string VisualRootName = "PlayerVisual";

        private static readonly Color SkinColor = new(0.776f, 0.588f, 0.478f);
        private static readonly Color ShirtColor = new(0.235f, 0.267f, 0.667f);
        private static readonly Color PantsColor = new(0.180f, 0.157f, 0.314f);

        private static Mesh _unitCubeMesh;

        public static bool TryGetExisting(Transform playerRoot, out Transform visualRoot)
        {
            visualRoot = null;
            if (playerRoot == null)
            {
                return false;
            }

            var existing = playerRoot.Find(VisualRootName);
            if (existing == null)
            {
                return false;
            }

            visualRoot = existing;
            return true;
        }

        public static bool IsSkinModelValid(Transform visualRoot)
        {
            if (visualRoot == null)
            {
                return false;
            }

            if (visualRoot.localPosition != Vector3.zero)
            {
                Debug.LogWarning("[SkinValid] Visual root position is not zero");
                return false;
            }

            if (Vector3.Distance(visualRoot.localScale, Vector3.one * 0.9f) > 0.01f)
            {
                Debug.LogWarning("[SkinValid] Visual root scale is not 0.9");
                return false;
            }

            // Check RootCombatPivot exists
            var rootCombat = visualRoot.Find("RootCombatPivot");
            if (rootCombat == null) { Debug.LogWarning("[SkinValid] Missing RootCombatPivot"); return false; }

            var upperBody = rootCombat.Find("UpperBodyPivot");
            if (upperBody == null) { Debug.LogWarning("[SkinValid] Missing UpperBodyPivot"); return false; }

            if (!IsPositionValid(upperBody, Vector3.zero) ||
                Vector3.Distance(upperBody.localScale, Vector3.one) > 0.01f ||
                Quaternion.Angle(upperBody.localRotation, Quaternion.identity) > 0.01f)
            {
                Debug.LogWarning("[SkinValid] UpperBodyPivot transform invalid");
                return false;
            }

            var chest = upperBody.Find("ChestPivot");
            if (chest == null) { Debug.LogWarning("[SkinValid] Missing ChestPivot"); return false; }

            var upperPartPaths = new Dictionary<string, string>
            {
                { "Head", "RootCombatPivot/UpperBodyPivot/HeadPivot/Head" },
                { "Body", "RootCombatPivot/UpperBodyPivot/Body" },
                { "LeftUpperArm", "RootCombatPivot/UpperBodyPivot/LeftShoulderPivot/LeftUpperArm" },
                { "LeftLowerArm", "RootCombatPivot/UpperBodyPivot/LeftShoulderPivot/LeftElbowPivot/LeftLowerArm" },
                { "RightUpperArm", "RootCombatPivot/UpperBodyPivot/RightShoulderPivot/RightUpperArm" },
                { "RightLowerArm", "RootCombatPivot/UpperBodyPivot/RightShoulderPivot/RightElbowPivot/RightLowerArm" }
            };

            var lowerPartPaths = new Dictionary<string, string>
            {
                { "LeftUpperLeg", "RootCombatPivot/LeftThighPivot/LeftUpperLeg" },
                { "LeftLowerLeg", "RootCombatPivot/LeftThighPivot/LeftKneePivot/LeftLowerLeg" },
                { "RightUpperLeg", "RootCombatPivot/RightThighPivot/RightUpperLeg" },
                { "RightLowerLeg", "RootCombatPivot/RightThighPivot/RightKneePivot/RightLowerLeg" }
            };

            foreach (var kvp in upperPartPaths)
            {
                if (!ValidateMeshPart(visualRoot, kvp.Key, kvp.Value)) return false;
            }
            foreach (var kvp in lowerPartPaths)
            {
                if (!ValidateMeshPart(visualRoot, kvp.Key, kvp.Value)) return false;
            }

            var leftShoulder = upperBody.Find("LeftShoulderPivot");
            var rightShoulder = upperBody.Find("RightShoulderPivot");
            var leftThigh = rootCombat.Find("LeftThighPivot");
            var rightThigh = rootCombat.Find("RightThighPivot");

            if (leftShoulder == null || rightShoulder == null || leftThigh == null || rightThigh == null)
            {
                Debug.LogWarning("[SkinValid] Missing one of the main limb pivots");
                return false;
            }

            if (!IsPositionValid(leftShoulder, new Vector3(-0.375f, 1.5f, 0f)) ||
                !IsPositionValid(rightShoulder, new Vector3(0.375f, 1.5f, 0f)) ||
                !IsPositionValid(leftThigh, new Vector3(-0.125f, 0.75f, 0f)) ||
                !IsPositionValid(rightThigh, new Vector3(0.125f, 0.75f, 0f)))
            {
                Debug.LogWarning($"[SkinValid] Limb pivot positions invalid! LeftShoulder: {leftShoulder.localPosition}");
                return false;
            }

            Transform[] pivots = { leftShoulder, rightShoulder, leftThigh, rightThigh, rootCombat, chest };
            foreach (var pivot in pivots)
            {
                if (Vector3.Distance(pivot.localScale, Vector3.one) > 0.01f) { Debug.LogWarning($"[SkinValid] Scale invalid on {pivot.name}"); return false; }
                if (Quaternion.Angle(pivot.localRotation, Quaternion.identity) > 0.01f) { Debug.LogWarning($"[SkinValid] Rotation invalid on {pivot.name}"); return false; }
            }

            // Check no colliders on model parts
            var colliders = visualRoot.GetComponentsInChildren<Collider>(true);
            if (colliders.Length > 0)
            {
                Debug.LogWarning("[SkinValid] Colliders found on visual mesh");
                return false;
            }

            return true;
        }

        private static bool ValidateMeshPart(Transform visualRoot, string key, string path)
        {
            var part = visualRoot.Find(path);
            if (part == null)
            {
                Debug.LogWarning($"[SkinValid] Missing part: {path}");
                return false;
            }

            var meshFilter = part.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                Debug.LogWarning($"[SkinValid] Missing MeshFilter or sharedMesh on part: {path}");
                return false;
            }

            if (!meshFilter.sharedMesh.name.StartsWith("SkinPart_"))
            {
                Debug.LogWarning($"[SkinValid] Mesh name doesn't start with SkinPart_ on part: {path} (found: {meshFilter.sharedMesh.name})");
                return false;
            }

            if (key.Contains("Arm") || key.Contains("Leg"))
            {
                if (!IsPositionValid(part, new Vector3(0f, -0.1875f, 0f)))
                {
                    Debug.LogWarning($"[SkinValid] Invalid local position for split limb part {path}: {part.localPosition} (expected 0, -0.1875, 0)");
                    return false;
                }
            }

            return true;
        }

        private static bool IsPositionValid(Transform target, Vector3 expected, float tolerance = 0.01f)
        {
            return target != null && Vector3.Distance(target.localPosition, expected) <= tolerance;
        }

        public static Transform Build(Transform playerRoot, PlayerModelType modelType = PlayerModelType.SteveWide)
        {
            if (playerRoot == null)
            {
                return null;
            }

            if (TryGetExisting(playerRoot, out var existing))
            {
                return existing;
            }

            var texturePath = modelType == PlayerModelType.SteveWide ? "Player/Skins/Steve_Wide" : "Player/Skins/Alex_Slim";
            var skin = Resources.Load<Texture2D>(texturePath);
            if (skin == null)
            {
                Debug.LogWarning($"[PlayerVisualBuilder] Could not load skin texture at Resources/{texturePath}");
            }

            var shader = Shader.Find("MineCraft/BlockUnlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit");
            var material = new Material(shader)
            {
                name = "Mat_Player_Skin"
            };
            if (skin != null)
            {
                material.mainTexture = skin;
                material.SetTexture("_BaseMap", skin);
            }
            material.SetColor("_BaseColor", Color.white);

            var visualGo = new GameObject(VisualRootName);
            var visualRoot = visualGo.transform;
            visualRoot.SetParent(playerRoot, false);
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;
            visualRoot.localScale = Vector3.one * 0.9f;

            // RootCombatPivot: groups the entire combat rig, allows scaling or offset if needed
            var rootCombatGo = new GameObject("RootCombatPivot");
            var rootCombat = rootCombatGo.transform;
            rootCombat.SetParent(visualRoot, false);
            rootCombat.localPosition = Vector3.zero;
            rootCombat.localRotation = Quaternion.identity;
            rootCombat.localScale = Vector3.one;

            // UpperBodyPivot: Torso
            var upperBodyGo = new GameObject("UpperBodyPivot");
            var upperBody = upperBodyGo.transform;
            upperBody.SetParent(rootCombat, false);
            upperBody.localPosition = Vector3.zero;
            upperBody.localRotation = Quaternion.identity;
            upperBody.localScale = Vector3.one;

            // ChestPivot: Empty pivot inside UpperBody for upper body bending
            var chestGo = new GameObject("ChestPivot");
            var chest = chestGo.transform;
            chest.SetParent(upperBody, false);
            chest.localPosition = new Vector3(0f, 1.125f, 0f);
            chest.localRotation = Quaternion.identity;
            chest.localScale = Vector3.one;

            // Legs
            var leftUpperLegMesh = MinecraftSkinMeshBuilder.BuildUpperLegMesh(true);
            var leftLowerLegMesh = MinecraftSkinMeshBuilder.BuildLowerLegMesh(true);
            var leftThigh = CreatePartWithPivot(rootCombat, "LeftThigh", new Vector3(-0.125f, 0.75f, 0f), new Vector3(0.25f, 0.375f, 0.25f), new Vector3(0f, -0.1875f, 0f), leftUpperLegMesh, material);
            leftThigh.gameObject.name = "LeftUpperLeg"; // mesh part name
            leftThigh.parent.gameObject.name = "LeftThighPivot";
            var leftKnee = CreatePartWithPivot(leftThigh.parent, "LeftKnee", new Vector3(0f, -0.375f, 0f), new Vector3(0.25f, 0.375f, 0.25f), new Vector3(0f, -0.1875f, 0f), leftLowerLegMesh, material);
            leftKnee.gameObject.name = "LeftLowerLeg";

            var rightUpperLegMesh = MinecraftSkinMeshBuilder.BuildUpperLegMesh(false);
            var rightLowerLegMesh = MinecraftSkinMeshBuilder.BuildLowerLegMesh(false);
            var rightThigh = CreatePartWithPivot(rootCombat, "RightThigh", new Vector3(0.125f, 0.75f, 0f), new Vector3(0.25f, 0.375f, 0.25f), new Vector3(0f, -0.1875f, 0f), rightUpperLegMesh, material);
            rightThigh.gameObject.name = "RightUpperLeg";
            rightThigh.parent.gameObject.name = "RightThighPivot";
            var rightKnee = CreatePartWithPivot(rightThigh.parent, "RightKnee", new Vector3(0f, -0.375f, 0f), new Vector3(0.25f, 0.375f, 0.25f), new Vector3(0f, -0.1875f, 0f), rightLowerLegMesh, material);
            rightKnee.gameObject.name = "RightLowerLeg";

            // Body
            var bodyMesh = MinecraftSkinMeshBuilder.BuildBodyMesh();
            var body = CreatePart(upperBody, "Body", new Vector3(0.5f, 0.75f, 0.25f), new Vector3(0f, 1.125f, 0f), bodyMesh, material);
            CreateSocket(body, "BodySocket", Vector3.zero);

            // Head (Child of UpperBody as sibling to Chest, per strict plan)
            var headPivotGo = new GameObject("HeadPivot");
            var headPivot = headPivotGo.transform;
            headPivot.SetParent(upperBody, false);
            headPivot.localPosition = new Vector3(0f, 1.5f, 0f);
            
            var headMesh = MinecraftSkinMeshBuilder.BuildHeadMesh();
            var head = CreatePart(headPivot, "Head", new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0f, 0.25f, 0f), headMesh, material);
            CreateSocket(head, "HeadSocket", new Vector3(0f, 0.25f, 0f));

            // Arms (Children of UpperBody)
            float armWidth = modelType == PlayerModelType.SteveWide ? 0.25f : 0.1875f;
            float leftArmX = modelType == PlayerModelType.SteveWide ? -0.375f : -0.34375f;
            float rightArmX = modelType == PlayerModelType.SteveWide ? 0.375f : 0.34375f;

            var leftUpperArmMesh = MinecraftSkinMeshBuilder.BuildUpperArmMesh(true, modelType);
            var leftLowerArmMesh = MinecraftSkinMeshBuilder.BuildLowerArmMesh(true, modelType);
            var leftShoulder = CreatePartWithPivot(upperBody, "LeftShoulder", new Vector3(leftArmX, 1.5f, 0f), new Vector3(armWidth, 0.375f, 0.25f), new Vector3(0f, -0.1875f, 0f), leftUpperArmMesh, material);
            leftShoulder.gameObject.name = "LeftUpperArm";
            leftShoulder.parent.gameObject.name = "LeftShoulderPivot";
            var leftElbow = CreatePartWithPivot(leftShoulder.parent, "LeftElbow", new Vector3(0f, -0.375f, 0f), new Vector3(armWidth, 0.375f, 0.25f), new Vector3(0f, -0.1875f, 0f), leftLowerArmMesh, material);
            leftElbow.gameObject.name = "LeftLowerArm";
            var leftHandSocket = CreateSocket(leftElbow.parent, "LeftHandSocket", new Vector3(0f, -0.375f, 0f));
            CreateSocket(leftHandSocket, "CharacterStatusPanelSocket", Vector3.zero);

            var rightUpperArmMesh = MinecraftSkinMeshBuilder.BuildUpperArmMesh(false, modelType);
            var rightLowerArmMesh = MinecraftSkinMeshBuilder.BuildLowerArmMesh(false, modelType);
            var rightShoulder = CreatePartWithPivot(upperBody, "RightShoulder", new Vector3(rightArmX, 1.5f, 0f), new Vector3(armWidth, 0.375f, 0.25f), new Vector3(0f, -0.1875f, 0f), rightUpperArmMesh, material);
            rightShoulder.gameObject.name = "RightUpperArm";
            rightShoulder.parent.gameObject.name = "RightShoulderPivot";
            var rightElbow = CreatePartWithPivot(rightShoulder.parent, "RightElbow", new Vector3(0f, -0.375f, 0f), new Vector3(armWidth, 0.375f, 0.25f), new Vector3(0f, -0.1875f, 0f), rightLowerArmMesh, material);
            rightElbow.gameObject.name = "RightLowerArm";
            CreateSocket(rightElbow.parent, "RightHandSocket", new Vector3(0f, -0.375f, 0f));

            return visualRoot;
        }

        private static Transform CreatePartWithPivot(
            Transform parent,
            string partName,
            Vector3 pivotPosition,
            Vector3 partScale,
            Vector3 partLocalPosition,
            Mesh mesh,
            Material material)
        {
            var pivotGo = new GameObject(partName + "Pivot");
            var pivotTransform = pivotGo.transform;
            pivotTransform.SetParent(parent, false);
            pivotTransform.localPosition = pivotPosition;
            pivotTransform.localRotation = Quaternion.identity;
            pivotTransform.localScale = Vector3.one;

            var partGo = new GameObject(partName);
            var partTransform = partGo.transform;
            partTransform.SetParent(pivotTransform, false);
            partTransform.localPosition = partLocalPosition;
            partTransform.localRotation = Quaternion.identity;
            partTransform.localScale = partScale;

            var meshFilter = partGo.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var renderer = partGo.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            return partTransform;
        }

        private static Transform CreatePart(
            Transform parent,
            string name,
            Vector3 size,
            Vector3 localPosition,
            Mesh mesh,
            Material material)
        {
            var go = new GameObject(name);
            var partTransform = go.transform;
            partTransform.SetParent(parent, false);
            partTransform.localPosition = localPosition;
            partTransform.localRotation = Quaternion.identity;
            partTransform.localScale = size;

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = mesh;

            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            return partTransform;
        }

        private static Transform CreateSocket(Transform parent, string name, Vector3 localPosition)
        {
            var socketGo = new GameObject(name);
            var socketTransform = socketGo.transform;
            socketTransform.SetParent(parent, false);
            socketTransform.localPosition = localPosition;
            socketTransform.localRotation = Quaternion.identity;
            socketTransform.localScale = Vector3.one;
            return socketTransform;
        }

        private static Material CreateSolidMaterial(Color color, string partName)
        {
            var shader = Shader.Find("MineCraft/BlockUnlit")
                ?? Shader.Find("Universal Render Pipeline/Unlit");
            var material = new Material(shader)
            {
                name = $"Mat_Player_{partName}"
            };
            material.SetColor("_BaseColor", color);
            return material;
        }
    }
}
