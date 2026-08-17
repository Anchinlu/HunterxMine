using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace MineCraftUnity.Editor
{
    public class CombatCoordinateCalibrationWindow : EditorWindow
    {
        private Transform _playerVisual;
        private Vector2 _scrollPos;
        
        // Caches
        private Transform _rootCombat, _upperBody, _chest, _head, _lShoulder, _lElbow, _rShoulder, _rElbow, _lThigh, _lKnee, _rThigh, _rKnee;
        private List<Transform> _allPivots = new List<Transform>();
        private Dictionary<string, Quaternion> _lastCapturedBindRotations = new Dictionary<string, Quaternion>();

        [MenuItem("MineCraft/Combat Coordinate Calibration")]
        public static void ShowWindow()
        {
            GetWindow<CombatCoordinateCalibrationWindow>("Combat Calibration");
        }

        private void OnGUI()
        {
            GUILayout.Label("Combat Rig Coordinate Calibration", EditorStyles.boldLabel);
            _playerVisual = (Transform)EditorGUILayout.ObjectField("Player Visual Root", _playerVisual, typeof(Transform), true);

            if (GUILayout.Button("Resolve Pivots"))
            {
                ResolvePivots();
            }

            if (_playerVisual == null || _allPivots.Count == 0)
            {
                EditorGUILayout.HelpBox("Select a PlayerVisual root and click Resolve Pivots.", MessageType.Info);
                return;
            }

            EditorGUILayout.Space();
            GUILayout.Label("Capture Controls", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Capture Bind Pose (Unity)"))
            {
                SavePose("bindPose");
            }
            if (GUILayout.Button("Capture Punch Pose (Unity)"))
            {
                SavePose("punchPose");
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();
            GUILayout.Label("Pivot Diagnostics", EditorStyles.boldLabel);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            foreach (var pivot in _allPivots)
            {
                if (pivot == null) continue;
                
                GUILayout.BeginVertical("box");
                GUILayout.Label(pivot.name, EditorStyles.boldLabel);
                GUILayout.Label($"Parent: {(pivot.parent != null ? pivot.parent.name : "None")}");
                GUILayout.Label($"L-Pos: {pivot.localPosition}");
                GUILayout.Label($"L-Rot: {pivot.localEulerAngles}");
                GUILayout.Label($"L-Scale: {pivot.localScale}");
                GUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
            
            SceneView.RepaintAll();
        }

        private void ResolvePivots()
        {
            _allPivots.Clear();
            if (_playerVisual == null) return;

            _rootCombat = FindChild(_playerVisual, "RootCombatPivot");
            _upperBody = FindChild(_playerVisual, "UpperBodyPivot");
            _chest = FindChild(_playerVisual, "ChestPivot");
            _head = FindChild(_playerVisual, "HeadPivot");
            
            _lShoulder = FindChild(_playerVisual, "LeftShoulderPivot");
            _lElbow = FindChild(_playerVisual, "LeftElbowPivot");
            _rShoulder = FindChild(_playerVisual, "RightShoulderPivot");
            _rElbow = FindChild(_playerVisual, "RightElbowPivot");
            
            _lThigh = FindChild(_playerVisual, "LeftThighPivot");
            _lKnee = FindChild(_playerVisual, "LeftKneePivot");
            _rThigh = FindChild(_playerVisual, "RightThighPivot");
            _rKnee = FindChild(_playerVisual, "RightKneePivot");

            AddPivot(_rootCombat);
            AddPivot(_upperBody);
            AddPivot(_chest);
            AddPivot(_head);
            AddPivot(_lShoulder);
            AddPivot(_lElbow);
            AddPivot(_rShoulder);
            AddPivot(_rElbow);
            AddPivot(_lThigh);
            AddPivot(_lKnee);
            AddPivot(_rThigh);
            AddPivot(_rKnee);
        }

        private void AddPivot(Transform t)
        {
            if (t != null && !_allPivots.Contains(t))
                _allPivots.Add(t);
        }

        private Transform FindChild(Transform parent, string name)
        {
            foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }
            return null;
        }

        private void SavePose(string poseName)
        {
            if (poseName == "bindPose")
            {
                _lastCapturedBindRotations.Clear();
                foreach (var t in _allPivots)
                {
                    if (t != null) _lastCapturedBindRotations[t.name] = t.localRotation;
                }
            }
            else if (poseName == "punchPose")
            {
                if (_lastCapturedBindRotations.Count == 0)
                {
                    EditorUtility.DisplayDialog("Calibration Error", "Please capture Bind Pose first in this session.", "OK");
                    return;
                }
                
                bool hasChanges = false;
                var changedJoints = new List<string>();
                foreach (var t in _allPivots)
                {
                    if (t == null) continue;
                    if (_lastCapturedBindRotations.TryGetValue(t.name, out var bindRot))
                    {
                        float angle = Quaternion.Angle(bindRot, t.localRotation);
                        if (angle > 0.1f)
                        {
                            hasChanges = true;
                            changedJoints.Add($"{t.name}: {angle:F2} degrees");
                        }
                    }
                }
                
                if (!hasChanges)
                {
                    EditorUtility.DisplayDialog("Calibration Error", "Punch pose has no joint changes compared to Bind pose.\nPose the right shoulder/elbow before saving.", "OK");
                    Debug.LogError("[CombatCalibration] Punch pose has no joint changes. Save aborted.");
                    return;
                }
                else
                {
                    Debug.Log($"[CombatCalibration] Punch pose changes detected:\n" + string.Join("\n", changedJoints));
                }
            }

            string dir = "Assets/Resources/Combat/Calibration";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            string path = Path.Combine(dir, $"{poseName}.json");
            
            var records = new List<string>();
            foreach(var t in _allPivots)
            {
                if (t == null) continue;
                Vector3 right = t.localRotation * Vector3.right;
                Vector3 up = t.localRotation * Vector3.up;
                Vector3 forward = t.localRotation * Vector3.forward;

                string json = $@"{{
  ""joint"": ""{t.name}"",
  ""parent"": ""{(t.parent != null ? t.parent.name : "null")}"",
  ""localPosition"": {{ ""x"": {t.localPosition.x}, ""y"": {t.localPosition.y}, ""z"": {t.localPosition.z} }},
  ""localEulerAngles"": {{ ""x"": {t.localEulerAngles.x}, ""y"": {t.localEulerAngles.y}, ""z"": {t.localEulerAngles.z} }},
  ""localScale"": {{ ""x"": {t.localScale.x}, ""y"": {t.localScale.y}, ""z"": {t.localScale.z} }},
  ""localRight"": {{ ""x"": {right.x}, ""y"": {right.y}, ""z"": {right.z} }},
  ""localUp"": {{ ""x"": {up.x}, ""y"": {up.y}, ""z"": {up.z} }},
  ""localForward"": {{ ""x"": {forward.x}, ""y"": {forward.y}, ""z"": {forward.z} }}
}}";
                records.Add(json);
            }

            File.WriteAllText(path, "[\n" + string.Join(",\n", records) + "\n]");
            AssetDatabase.Refresh();
            Debug.Log($"[CombatCalibration] Saved {poseName} to {path}");
        }

        private void OnFocus()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDestroy()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_allPivots == null) return;
            foreach (var t in _allPivots)
            {
                if (t == null) continue;
                
                Handles.color = Color.red;
                Handles.DrawLine(t.position, t.position + t.right * 0.2f);
                Handles.color = Color.green;
                Handles.DrawLine(t.position, t.position + t.up * 0.2f);
                Handles.color = Color.blue;
                Handles.DrawLine(t.position, t.position + t.forward * 0.2f);
            }
        }
    }
}
