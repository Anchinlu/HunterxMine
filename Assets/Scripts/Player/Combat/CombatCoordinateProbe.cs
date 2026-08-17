using System.Collections.Generic;
using UnityEngine;

namespace MineCraftUnity.Player.Combat
{
    public class CombatCoordinateProbe : MonoBehaviour
    {
        private Dictionary<string, Transform> _pivots = new Dictionary<string, Transform>();
        
        [System.Serializable]
        public class TransformData
        {
            public string joint;
            public string parent;
            public Vector3 localPosition;
            public Vector3 localEulerAngles;
            public Vector3 localScale;
            public Vector3 localRight;
            public Vector3 localUp;
            public Vector3 localForward;
        }

        private Dictionary<string, TransformData> _bindPose = new Dictionary<string, TransformData>();
        private Dictionary<string, TransformData> _punchPose = new Dictionary<string, TransformData>();

        private void Start()
        {
            ResolvePivots();
        }

        private void ResolvePivots()
        {
            _pivots.Clear();

            var rootCombat = transform.Find("RootCombatPivot");
            if (rootCombat != null) AddPivot("RootCombatPivot", rootCombat);

            var upperBody = FindChild(transform, "UpperBodyPivot");
            if (upperBody != null) AddPivot("UpperBodyPivot", upperBody);

            var chest = FindChild(transform, "ChestPivot");
            if (chest != null) AddPivot("ChestPivot", chest);

            var head = FindChild(transform, "HeadPivot");
            if (head != null) AddPivot("HeadPivot", head);

            var lShoulder = FindChild(transform, "LeftShoulderPivot");
            if (lShoulder != null) AddPivot("LeftShoulderPivot", lShoulder);

            var lElbow = FindChild(transform, "LeftElbowPivot");
            if (lElbow != null) AddPivot("LeftElbowPivot", lElbow);

            var rShoulder = FindChild(transform, "RightShoulderPivot");
            if (rShoulder != null) AddPivot("RightShoulderPivot", rShoulder);

            var rElbow = FindChild(transform, "RightElbowPivot");
            if (rElbow != null) AddPivot("RightElbowPivot", rElbow);

            var lThigh = FindChild(transform, "LeftThighPivot");
            if (lThigh != null) AddPivot("LeftThighPivot", lThigh);

            var lKnee = FindChild(transform, "LeftKneePivot");
            if (lKnee != null) AddPivot("LeftKneePivot", lKnee);

            var rThigh = FindChild(transform, "RightThighPivot");
            if (rThigh != null) AddPivot("RightThighPivot", rThigh);

            var rKnee = FindChild(transform, "RightKneePivot");
            if (rKnee != null) AddPivot("RightKneePivot", rKnee);
        }

        private Transform FindChild(Transform parent, string name)
        {
            foreach (Transform t in parent.GetComponentsInChildren<Transform>())
            {
                if (t.name == name) return t;
            }
            return null;
        }

        private void AddPivot(string name, Transform t)
        {
            if (t != null && !_pivots.ContainsKey(name))
            {
                _pivots.Add(name, t);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
            {
                CapturePose(_bindPose, "Bind Pose");
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                CapturePose(_punchPose, "Punch Pose");
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                PrintTransforms();
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                ResetToBindPose();
            }
        }

        private void CapturePose(Dictionary<string, TransformData> storage, string poseName)
        {
            if (poseName == "Punch Pose")
            {
                if (_bindPose.Count == 0)
                {
                    Debug.LogError("[CombatCoordinateProbe] Please capture Bind Pose (F6) first.");
                    return;
                }

                bool hasChanges = false;
                var changedJoints = new List<string>();
                foreach (var kvp in _pivots)
                {
                    if (_bindPose.TryGetValue(kvp.Key, out var bindData))
                    {
                        var t = kvp.Value;
                        float angle = Quaternion.Angle(Quaternion.Euler(bindData.localEulerAngles), t.localRotation);
                        if (angle > 0.1f)
                        {
                            hasChanges = true;
                            changedJoints.Add($"{kvp.Key}: {angle:F2} degrees");
                        }
                    }
                }
                
                if (!hasChanges)
                {
                    Debug.LogError("[CombatCoordinateProbe] Punch pose has no joint changes compared to Bind pose. Pose the right shoulder/elbow before capturing.");
                    return;
                }
                else
                {
                    Debug.Log($"[CombatCoordinateProbe] Punch pose changes detected:\n" + string.Join("\n", changedJoints));
                }
            }

            storage.Clear();
            foreach (var kvp in _pivots)
            {
                var t = kvp.Value;
                var data = new TransformData
                {
                    joint = kvp.Key,
                    parent = t.parent != null ? t.parent.name : "null",
                    localPosition = t.localPosition,
                    localEulerAngles = t.localEulerAngles,
                    localScale = t.localScale,
                    localRight = t.localRotation * Vector3.right,
                    localUp = t.localRotation * Vector3.up,
                    localForward = t.localRotation * Vector3.forward
                };
                storage[kvp.Key] = data;
            }
            Debug.Log($"[CombatCoordinateProbe] Captured {poseName} for {_pivots.Count} pivots.");
            
            // Optionally save to JSON here, but Editor script handles JSON saving better.
        }

        private void PrintTransforms()
        {
            Debug.Log("=== PIVOT TRANSFORMS ===");
            foreach (var kvp in _pivots)
            {
                var t = kvp.Value;
                Debug.Log($"{kvp.Key} | LPos: {t.localPosition} | LRot: {t.localEulerAngles} | LScale: {t.localScale} | W_X: {t.right} | W_Y: {t.up} | W_Z: {t.forward}");
            }
        }

        private void ResetToBindPose()
        {
            if (_bindPose.Count == 0)
            {
                Debug.LogWarning("[CombatCoordinateProbe] No Bind Pose captured. Cannot reset.");
                return;
            }

            foreach (var kvp in _pivots)
            {
                if (_bindPose.TryGetValue(kvp.Key, out var data))
                {
                    kvp.Value.localPosition = data.localPosition;
                    kvp.Value.localEulerAngles = data.localEulerAngles;
                    kvp.Value.localScale = data.localScale;
                }
            }
            Debug.Log("[CombatCoordinateProbe] Visuals reset to Bind Pose.");
        }
    }
}
