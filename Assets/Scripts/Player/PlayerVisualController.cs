using UnityEngine;

namespace MineCraftUnity.Player
{
    public enum PlayerModelType
    {
        SteveWide,
        AlexSlim
    }

    /// <summary>
    /// Controls blocky player visual visibility and exposes attachment sockets for future gear/weapons.
    /// Yaw follows Player root; pitch from CameraRoot is not applied to this hierarchy.
    /// </summary>
    public sealed class PlayerVisualController : MonoBehaviour
    {
        [SerializeField] private bool showPlayerVisual = true;
        [SerializeField] private bool hideHeadInFirstPerson = false;
        [SerializeField] private PlayerModelType modelType = PlayerModelType.SteveWide;

        private Transform _head;
        private PlayerModelType _lastModelType = PlayerModelType.SteveWide;

        public Transform HeadSocket => FindDeep("HeadSocket");
        public Transform BodySocket => FindDeep("BodySocket");
        public Transform LeftHandSocket => FindDeep("LeftHandSocket");
        public Transform RightHandSocket => FindDeep("RightHandSocket");

        private void Awake()
        {
            _head = FindDeep("Head");
        }

        private void Start()
        {
            _lastModelType = modelType;
            ApplyModelSettings();
            ApplyVisibility();
        }

        private void Update()
        {
            if (modelType != _lastModelType)
            {
                _lastModelType = modelType;
                ApplyModelSettings();
            }
        }

        private void OnValidate()
        {
            ApplyModelSettings();
            if (Application.isPlaying)
            {
                ApplyVisibility();
            }
        }

        public void SetFirstPerson(bool firstPerson)
        {
            hideHeadInFirstPerson = firstPerson;
            ApplyVisibility();
        }

        public void ApplyModelSettings()
        {
            var texturePath = modelType == PlayerModelType.SteveWide ? "Player/Skins/Steve_Wide" : "Player/Skins/Alex_Slim";
            var skin = Resources.Load<Texture2D>(texturePath);
            if (skin == null)
            {
                return;
            }

            // Apply texture to shared material of player parts
            var renderers = GetComponentsInChildren<MeshRenderer>(true);
            foreach (var r in renderers)
            {
                if (r.sharedMaterial != null)
                {
                    r.sharedMaterial.mainTexture = skin;
                    r.sharedMaterial.SetTexture("_BaseMap", skin);
                }
            }

            // Update LeftArm Pivot and Mesh child
            var leftShoulderPivot = FindDeep("LeftShoulderPivot");
            if (leftShoulderPivot != null)
            {
                leftShoulderPivot.localPosition = new Vector3(modelType == PlayerModelType.SteveWide ? -0.375f : -0.34375f, 1.5f, 0f);
                var leftUpperArm = leftShoulderPivot.Find("LeftUpperArm");
                if (leftUpperArm != null)
                {
                    var filter = leftUpperArm.GetComponent<MeshFilter>();
                    if (filter != null)
                    {
                        filter.sharedMesh = MinecraftSkinMeshBuilder.BuildUpperArmMesh(true, modelType);
                    }
                    leftUpperArm.localScale = new Vector3(modelType == PlayerModelType.SteveWide ? 0.25f : 0.1875f, 0.375f, 0.25f);
                    leftUpperArm.localPosition = new Vector3(0f, -0.1875f, 0f);
                }

                var leftElbowPivot = leftShoulderPivot.Find("LeftElbowPivot");
                if (leftElbowPivot != null)
                {
                    var leftLowerArm = leftElbowPivot.Find("LeftLowerArm");
                    if (leftLowerArm != null)
                    {
                        var filter = leftLowerArm.GetComponent<MeshFilter>();
                        if (filter != null)
                        {
                            filter.sharedMesh = MinecraftSkinMeshBuilder.BuildLowerArmMesh(true, modelType);
                        }
                        leftLowerArm.localScale = new Vector3(modelType == PlayerModelType.SteveWide ? 0.25f : 0.1875f, 0.375f, 0.25f);
                        leftLowerArm.localPosition = new Vector3(0f, -0.1875f, 0f);
                    }
                }
            }

            // Update RightArm Pivot and Mesh child
            var rightShoulderPivot = FindDeep("RightShoulderPivot");
            if (rightShoulderPivot != null)
            {
                rightShoulderPivot.localPosition = new Vector3(modelType == PlayerModelType.SteveWide ? 0.375f : 0.34375f, 1.5f, 0f);
                var rightUpperArm = rightShoulderPivot.Find("RightUpperArm");
                if (rightUpperArm != null)
                {
                    var filter = rightUpperArm.GetComponent<MeshFilter>();
                    if (filter != null)
                    {
                        filter.sharedMesh = MinecraftSkinMeshBuilder.BuildUpperArmMesh(false, modelType);
                    }
                    rightUpperArm.localScale = new Vector3(modelType == PlayerModelType.SteveWide ? 0.25f : 0.1875f, 0.375f, 0.25f);
                    rightUpperArm.localPosition = new Vector3(0f, -0.1875f, 0f);
                }

                var rightElbowPivot = rightShoulderPivot.Find("RightElbowPivot");
                if (rightElbowPivot != null)
                {
                    var rightLowerArm = rightElbowPivot.Find("RightLowerArm");
                    if (rightLowerArm != null)
                    {
                        var filter = rightLowerArm.GetComponent<MeshFilter>();
                        if (filter != null)
                        {
                            filter.sharedMesh = MinecraftSkinMeshBuilder.BuildLowerArmMesh(false, modelType);
                        }
                        rightLowerArm.localScale = new Vector3(modelType == PlayerModelType.SteveWide ? 0.25f : 0.1875f, 0.375f, 0.25f);
                        rightLowerArm.localPosition = new Vector3(0f, -0.1875f, 0f);
                    }
                }
            }
        }

        /// <summary>
        /// Forces hideHeadInFirstPerson off and re-applies visibility.
        /// Called by Bootstrap to fix serialized values from older code.
        /// </summary>
        public void ResetHeadVisibility()
        {
            hideHeadInFirstPerson = false;
            ApplyVisibility();
        }

        public void ApplyVisibility()
        {
            if (_head == null)
            {
                _head = FindDeep("Head");
            }

            SetPartActiveDeep("RootCombatPivot/UpperBodyPivot", showPlayerVisual);
            SetPartActiveDeep("RootCombatPivot/LeftThighPivot", showPlayerVisual);
            SetPartActiveDeep("RootCombatPivot/RightThighPivot", showPlayerVisual);

            // Fallbacks for older rig
            if (transform.Find("RootCombatPivot") == null)
            {
                SetPartActive("UpperBodyPivot", showPlayerVisual);
                SetPartActive("LeftLegPivot", showPlayerVisual);
                SetPartActive("RightLegPivot", showPlayerVisual);
            }

            if (_head != null)
            {
                _head.gameObject.SetActive(showPlayerVisual && !hideHeadInFirstPerson);
            }
        }

        private void SetPartActive(string partName, bool active)
        {
            var part = transform.Find(partName);
            if (part != null)
            {
                part.gameObject.SetActive(active);
            }
        }

        private void SetPartActiveDeep(string path, bool active)
        {
            var part = transform.Find(path);
            if (part != null)
            {
                part.gameObject.SetActive(active);
            }
        }

        private Transform FindDeep(string name)
        {
            foreach (var t in transform.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }
            return null;
        }
    }
}
