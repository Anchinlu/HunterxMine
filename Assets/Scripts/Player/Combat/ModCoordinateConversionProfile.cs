using UnityEngine;

namespace MineCraftUnity.Player.Combat
{
    public enum CoordinatePreset
    {
        EpicFight_ModelLocal_Default,
        Minecraft_World_XEast_YUp_ZSouth,
        Unity_XRight_YUp_ZForward
    }

    [CreateAssetMenu(fileName = "NewModCoordinateConversionProfile", menuName = "MineCraft/Combat/Mod Coordinate Conversion Profile")]
    public class ModCoordinateConversionProfile : ScriptableObject
    {
        public enum Handedness { LeftHanded, RightHanded }

        [Header("Preset Configuration")]
        public CoordinatePreset Preset = CoordinatePreset.EpicFight_ModelLocal_Default;
        public bool AdvancedMode = false;

        [Header("Source Basis Axes (Locked unless AdvancedMode=true)")]
        public Vector3 SourceRightAxis = Vector3.right;
        public Vector3 SourceUpAxis = Vector3.up;
        public Vector3 SourceForwardAxis = Vector3.forward;

        [Header("Properties")]
        [Tooltip("Handedness is only for checking/logging, no double flips.")]
        public Handedness ExpectedHandedness = Handedness.RightHanded;
        
        [Tooltip("Scale to convert source translation into Unity units. (e.g. 0.0625 for 1/16 Minecraft pixels)")]
        public float TranslationScale = 0.0625f;

        private void OnValidate()
        {
            if (!AdvancedMode)
            {
                switch (Preset)
                {
                    case CoordinatePreset.EpicFight_ModelLocal_Default:
                        SourceRightAxis = new Vector3(1, 0, 0);
                        SourceUpAxis = new Vector3(0, 1, 0);
                        SourceForwardAxis = new Vector3(0, 0, 1);
                        ExpectedHandedness = Handedness.RightHanded;
                        break;
                    case CoordinatePreset.Minecraft_World_XEast_YUp_ZSouth:
                        SourceRightAxis = new Vector3(1, 0, 0);
                        SourceUpAxis = new Vector3(0, 1, 0);
                        SourceForwardAxis = new Vector3(0, 0, -1);
                        ExpectedHandedness = Handedness.RightHanded;
                        break;
                    case CoordinatePreset.Unity_XRight_YUp_ZForward:
                        SourceRightAxis = new Vector3(1, 0, 0);
                        SourceUpAxis = new Vector3(0, 1, 0);
                        SourceForwardAxis = new Vector3(0, 0, 1);
                        ExpectedHandedness = Handedness.LeftHanded; // Unity is LeftHanded
                        break;
                }
            }
        }

        /// <summary>
        /// Gets the conversion basis matrix C from Source to Unity space and validates it.
        /// </summary>
        public Matrix4x4 GetBasisMatrix()
        {
            Matrix4x4 basis = Matrix4x4.identity;
            basis.SetColumn(0, new Vector4(SourceRightAxis.x, SourceRightAxis.y, SourceRightAxis.z, 0));
            basis.SetColumn(1, new Vector4(SourceUpAxis.x, SourceUpAxis.y, SourceUpAxis.z, 0));
            basis.SetColumn(2, new Vector4(SourceForwardAxis.x, SourceForwardAxis.y, SourceForwardAxis.z, 0));
            basis.SetColumn(3, new Vector4(0, 0, 0, 1));

            float det = basis.determinant;
            if (Mathf.Abs(det) < 0.0001f)
            {
                Debug.LogError("[SkinValid] Basis matrix determinant is close to 0 (invalid basis).");
            }

            // Check orthogonality
            if (Mathf.Abs(Vector3.Dot(SourceRightAxis, SourceUpAxis)) > 0.01f ||
                Mathf.Abs(Vector3.Dot(SourceRightAxis, SourceForwardAxis)) > 0.01f ||
                Mathf.Abs(Vector3.Dot(SourceUpAxis, SourceForwardAxis)) > 0.01f)
            {
                Debug.LogError("[SkinValid] Basis axes are not orthogonal!");
            }

            return basis;
        }
    }
}
