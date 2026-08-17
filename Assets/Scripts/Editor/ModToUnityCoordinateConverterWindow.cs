using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using MineCraftUnity.Player.Combat;

namespace MineCraftUnity.Editor
{
    public class ModToUnityCoordinateConverterWindow : EditorWindow
    {
        private ModCoordinateConversionProfile _profile;
        private string _selectedAttackId = "fist_auto1";
        private Transform _targetPlayerVisual;

        // Preview state
        private GameObject _previewRig;
        private Dictionary<string, Transform> _previewPivots = new Dictionary<string, Transform>();
        private ConvertedAttackAnimation _previewAnim;
        private float _previewTime = 0f;
        private bool _isPlaying = false;
        private double _lastTime;

        private Dictionary<string, string> _jointMap = new Dictionary<string, string>
        {
            { "Root", "RootCombatPivot" },
            { "Torso", "UpperBodyPivot" },
            { "Chest", "ChestPivot" },
            { "Head", "HeadPivot" },
            { "Shoulder_L", "LeftShoulderPivot" },
            { "Elbow_L", "LeftElbowPivot" },
            { "Shoulder_R", "RightShoulderPivot" },
            { "Elbow_R", "RightElbowPivot" },
            { "Thigh_L", "LeftThighPivot" },
            { "Knee_L", "LeftKneePivot" },
            { "Thigh_R", "RightThighPivot" },
            { "Knee_R", "RightKneePivot" }
        };

        private static readonly HashSet<string> _optionalTracks = new HashSet<string>
        {
            "Arm_L", "Arm_R", "Leg_L", "Leg_R",
            "Hand_L", "Hand_R", "Tool_L", "Tool_R"
        };

        [MenuItem("MineCraft/Mod to Unity Converter")]
        public static void ShowWindow()
        {
            GetWindow<ModToUnityCoordinateConverterWindow>("Coordinate Converter");
        }

        private void OnGUI()
        {
            GUILayout.Label("Automatic Mod-to-Unity Coordinate Converter", EditorStyles.boldLabel);
            
            _profile = (ModCoordinateConversionProfile)EditorGUILayout.ObjectField("Conversion Profile", _profile, typeof(ModCoordinateConversionProfile), false);
            
            // Just text field for Attack ID for now, since we read from static EpicFightAnimData
            _selectedAttackId = EditorGUILayout.TextField("Source Attack ID", _selectedAttackId);
            
            _targetPlayerVisual = (Transform)EditorGUILayout.ObjectField("Target PlayerVisual", _targetPlayerVisual, typeof(Transform), true);

            EditorGUILayout.Space();

            if (GUILayout.Button("1. Convert & Generate Preview"))
            {
                if (_profile == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please select a conversion profile.", "OK");
                    return;
                }
                ConvertAnimation();
            }

            if (_previewAnim != null)
            {
                EditorGUILayout.Space();
                GUILayout.Label("Preview Controls", EditorStyles.boldLabel);
                
                EditorGUI.BeginChangeCheck();
                _previewTime = EditorGUILayout.Slider("Time", _previewTime, 0f, _previewAnim.TotalDuration);
                if (EditorGUI.EndChangeCheck() && !_isPlaying)
                {
                    ApplyPreviewPose(_previewTime);
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(_isPlaying ? "Pause" : "Play"))
                {
                    _isPlaying = !_isPlaying;
                    _lastTime = EditorApplication.timeSinceStartup;
                }
                if (GUILayout.Button("Reset to Bind Pose"))
                {
                    _isPlaying = false;
                    _previewTime = 0f;
                    ApplyPreviewPose(0f);
                }
                GUILayout.EndHorizontal();

                EditorGUILayout.Space();
                if (GUILayout.Button("2. Export Unity Animation Asset"))
                {
                    ExportAsset();
                }
            }
        }

        private void Update()
        {
            if (_isPlaying && _previewAnim != null)
            {
                double now = EditorApplication.timeSinceStartup;
                float dt = (float)(now - _lastTime);
                _lastTime = now;

                _previewTime += dt;
                if (_previewTime > _previewAnim.TotalDuration)
                {
                    _previewTime = 0f;
                }

                ApplyPreviewPose(_previewTime);
            }
        }

        private void ConvertAnimation()
        {
            var dict = EpicFightAnimData.GetAnimations();
            if (!dict.TryGetValue(_selectedAttackId, out var sourceDef))
            {
                EditorUtility.DisplayDialog("Error", $"Attack '{_selectedAttackId}' not found in EpicFightAnimData.", "OK");
                return;
            }

            _previewAnim = CreateInstance<ConvertedAttackAnimation>();
            _previewAnim.AttackId = sourceDef.Id;
            _previewAnim.TotalDuration = sourceDef.TotalDuration;

            // EpicFightAnimData chỉ chứa ma trận, không chứa metadata gameplay.
            // Gán metadata từ bảng tra cứu nội bộ.
            ApplyKnownMetadata(_previewAnim);

            Matrix4x4 C = _profile.GetBasisMatrix();
            Matrix4x4 CInv = C.inverse;
            bool mirroredBasis = C.determinant < 0;

            bool failed = false;
            float maxRotDelta0 = 0f;
            float maxPosDelta0 = 0f;
            int mappedTracks = 0;
            var optionalIgnored = new List<string>();
            var unknownTracks = new List<string>();

            foreach (var kvp in sourceDef.Joints)
            {
                string sourceJoint = kvp.Key;
                if (!_jointMap.TryGetValue(sourceJoint, out string unityJoint))
                {
                    if (_optionalTracks.Contains(sourceJoint))
                        optionalIgnored.Add(sourceJoint);
                    else
                        unknownTracks.Add(sourceJoint);
                    continue;
                }

                var keyframe = kvp.Value;
                if (keyframe.Times.Length == 0) continue;

                var track = new ConvertedJointTrack
                {
                    UnityJointName = unityJoint,
                    Times = keyframe.Times,
                    PositionDeltas = new Vector3[keyframe.Times.Length],
                    RotationDeltas = new Quaternion[keyframe.Times.Length]
                };

                Matrix4x4 sourceBind = keyframe.Matrices[0];
                Matrix4x4 sourceBindInv = sourceBind.inverse;

                for (int i = 0; i < keyframe.Times.Length; i++)
                {
                    Matrix4x4 sourceFrame = keyframe.Matrices[i];
                    Matrix4x4 sourceDelta = sourceBindInv * sourceFrame;

                    // Convert basis: unityMatrix = C * sourceDelta * inverse(C)
                    Matrix4x4 unityDeltaMatrix = C * sourceDelta * CInv;

                    Vector3 posDelta = new Vector3(unityDeltaMatrix.m03, unityDeltaMatrix.m13, unityDeltaMatrix.m23);
                    posDelta *= _profile.TranslationScale;

                    Vector3 forward = unityDeltaMatrix.GetColumn(2);
                    Vector3 upwards = unityDeltaMatrix.GetColumn(1);
                    Quaternion rotDelta = Quaternion.identity;
                    
                    if (forward.sqrMagnitude > 0.0001f && upwards.sqrMagnitude > 0.0001f)
                    {
                        rotDelta = Quaternion.LookRotation(forward, upwards);
                    }

                    if (i == 0)
                    {
                        float ang = Quaternion.Angle(Quaternion.identity, rotDelta);
                        float posMag = posDelta.magnitude;
                        if (ang > maxRotDelta0) maxRotDelta0 = ang;
                        if (posMag > maxPosDelta0) maxPosDelta0 = posMag;

                        if (ang > 0.1f || posMag > 0.001f)
                        {
                            failed = true;
                        }
                    }

                    track.PositionDeltas[i] = posDelta;
                    track.RotationDeltas[i] = rotDelta;
                }

                _previewAnim.Tracks.Add(track);
                mappedTracks++;
            }

            Debug.Log($"[Converter] Basis: X={_profile.SourceRightAxis}, Y={_profile.SourceUpAxis}, Z={_profile.SourceForwardAxis}");
            Debug.Log($"[Converter] Mirrored basis: {mirroredBasis.ToString().ToLower()}");
            Debug.Log($"[Converter] First-frame max rotation delta: {maxRotDelta0:F2} degrees");
            Debug.Log($"[Converter] First-frame max position delta: {maxPosDelta0:F4} units");
            Debug.Log($"[Converter] Required mappings: {mappedTracks}");
            Debug.Log($"[Converter] Optional ignored tracks: {(optionalIgnored.Count > 0 ? string.Join(", ", optionalIgnored) : "none")}");
            if (unknownTracks.Count > 0)
                Debug.LogWarning($"[Converter] Unknown source tracks (no mapping exists): {string.Join(", ", unknownTracks)}");
            
            if (failed)
            {
                _previewAnim = null;
                Debug.LogError("[Converter] Export aborted! First-frame delta is not identity.");
                EditorUtility.DisplayDialog("Conversion Failed", "First-frame delta is not identity. Check the console.", "OK");
                return;
            }

            SetupPreviewRig();
            _previewTime = 0f;
            ApplyPreviewPose(0f);
        }

        private Dictionary<string, Vector3> _previewBasePos = new Dictionary<string, Vector3>();
        private Dictionary<string, Quaternion> _previewBaseRot = new Dictionary<string, Quaternion>();

        private void SetupPreviewRig()
        {
            if (_previewRig != null) DestroyImmediate(_previewRig);
            if (_targetPlayerVisual == null) return;

            _previewRig = Instantiate(_targetPlayerVisual.gameObject);
            _previewRig.name = _targetPlayerVisual.name + " (Preview)";
            _previewRig.transform.position = _targetPlayerVisual.position + Vector3.right * 2f; // Offset to not overlap

            // Remove runtime scripts to avoid interference
            foreach (var comp in _previewRig.GetComponentsInChildren<MonoBehaviour>())
            {
                DestroyImmediate(comp);
            }

            _previewPivots.Clear();
            _previewBasePos.Clear();
            _previewBaseRot.Clear();
            
            foreach (var t in _previewRig.GetComponentsInChildren<Transform>())
            {
                if (_jointMap.ContainsValue(t.name))
                {
                    _previewPivots[t.name] = t;
                    _previewBasePos[t.name] = t.localPosition;
                    _previewBaseRot[t.name] = t.localRotation;
                }
            }
        }

        private void ApplyPreviewPose(float time)
        {
            if (_previewRig == null || _previewAnim == null) return;

            foreach (var track in _previewAnim.Tracks)
            {
                if (_previewPivots.TryGetValue(track.UnityJointName, out Transform pivot))
                {
                    GetInterpolatedDelta(track, time, out Vector3 posDelta, out Quaternion rotDelta);
                    
                    Vector3 basePos = _previewBasePos[track.UnityJointName];
                    Quaternion baseRot = _previewBaseRot[track.UnityJointName];
                    
                    pivot.localRotation = baseRot * rotDelta;
                    pivot.localPosition = basePos + posDelta;
                }
            }
        }

        private void GetInterpolatedDelta(ConvertedJointTrack track, float time, out Vector3 posDelta, out Quaternion rotDelta)
        {
            posDelta = Vector3.zero;
            rotDelta = Quaternion.identity;
            
            if (track.Times.Length == 0) return;
            if (time <= track.Times[0])
            {
                posDelta = track.PositionDeltas[0];
                rotDelta = track.RotationDeltas[0];
                return;
            }
            if (time >= track.Times[track.Times.Length - 1])
            {
                posDelta = track.PositionDeltas[track.PositionDeltas.Length - 1];
                rotDelta = track.RotationDeltas[track.RotationDeltas.Length - 1];
                return;
            }

            for (int i = 0; i < track.Times.Length - 1; i++)
            {
                if (time >= track.Times[i] && time <= track.Times[i + 1])
                {
                    float t = (time - track.Times[i]) / (track.Times[i + 1] - track.Times[i]);
                    posDelta = Vector3.Lerp(track.PositionDeltas[i], track.PositionDeltas[i + 1], t);
                    rotDelta = Quaternion.Slerp(track.RotationDeltas[i], track.RotationDeltas[i + 1], t);
                    return;
                }
            }
        }

        private void ExportAsset()
        {
            if (_previewAnim == null) return;
            string dir = "Assets/Resources/Combat/ConvertedAnimations";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            string path = Path.Combine(dir, $"{_previewAnim.AttackId}_Converted.asset");
            AssetDatabase.CreateAsset(_previewAnim, path);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"[Converter] Exported: {path}");
            EditorUtility.DisplayDialog("Export Successful", $"Saved to {path}", "OK");
        }

        private void OnDestroy()
        {
            if (_previewRig != null)
            {
                DestroyImmediate(_previewRig);
            }
        }

        /// <summary>
        /// Bảng tra cứu metadata gameplay cho các attack đã biết.
        /// EpicFightAnimData chỉ chứa ma trận keyframe, không chứa HitWindow/ComboWindow.
        /// Dữ liệu này lấy từ JSON config gốc của mod.
        /// </summary>
        private static void ApplyKnownMetadata(ConvertedAttackAnimation anim)
        {
            switch (anim.AttackId)
            {
                case "fist_auto1":
                    anim.HitWindowStart = 0.05f;
                    anim.HitWindowEnd = 0.1333f;
                    anim.ComboWindowStart = 0.2f;
                    anim.ComboWindowEnd = 0.5f;
                    anim.NextComboAttackId = "fist_auto2";
                    anim.MovementMultiplier = 0.6f;
                    anim.IsGroundCompatible = true;
                    anim.IsAirCompatible = false;
                    break;

                case "fist_auto2":
                    anim.HitWindowStart = 0.05f;
                    anim.HitWindowEnd = 0.15f;
                    anim.ComboWindowStart = 0.25f;
                    anim.ComboWindowEnd = anim.TotalDuration;
                    anim.NextComboAttackId = "fist_auto3";
                    anim.MovementMultiplier = 0.6f;
                    anim.IsGroundCompatible = true;
                    anim.IsAirCompatible = false;
                    break;

                case "fist_auto3":
                    anim.HitWindowStart = 0.1f;
                    anim.HitWindowEnd = 0.2f;
                    anim.ComboWindowStart = 0.35f;
                    anim.ComboWindowEnd = anim.TotalDuration;
                    anim.NextComboAttackId = null;
                    anim.MovementMultiplier = 0.5f;
                    anim.IsGroundCompatible = true;
                    anim.IsAirCompatible = false;
                    break;

                case "fist_dash":
                    anim.HitWindowStart = 0.1f;
                    anim.HitWindowEnd = 0.3f;
                    anim.ComboWindowStart = anim.TotalDuration;
                    anim.ComboWindowEnd = anim.TotalDuration;
                    anim.MovementMultiplier = 1.2f;
                    anim.IsGroundCompatible = true;
                    anim.IsAirCompatible = false;
                    break;

                case "fist_airslash":
                    anim.HitWindowStart = 0.1f;
                    anim.HitWindowEnd = 0.3f;
                    anim.ComboWindowStart = anim.TotalDuration;
                    anim.ComboWindowEnd = anim.TotalDuration;
                    anim.MovementMultiplier = 1.0f;
                    anim.IsGroundCompatible = false;
                    anim.IsAirCompatible = true;
                    break;

                default:
                    Debug.LogWarning($"[Converter] No known metadata for attack '{anim.AttackId}'. Metadata fields will be 0.");
                    break;
            }
        }
    }
}
