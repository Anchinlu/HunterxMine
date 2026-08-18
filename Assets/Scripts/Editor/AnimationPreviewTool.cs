using UnityEngine;
using UnityEditor;
using MineCraftUnity.Player.Combat;
using MineCraftUnity.Player;

namespace MineCraftUnity.Editor
{
    /// <summary>
    /// Preview Tool cho converted attack animations.
    /// 
    /// Features:
    /// - Timeline scrubbing với slider
    /// - Play/Pause/Reset controls
    /// - Per-joint toggle visibility
    /// - Frame-by-frame stepping
    /// - Bind pose comparison
    /// 
    /// Yêu cầu: Scene phải có Player object với PlayerVisual hierarchy đã setup.
    /// </summary>
    public class AnimationPreviewTool : EditorWindow
    {
        private ConvertedAttackAnimation selectedAnimation;
        private GameObject targetPlayer;
        
        private float currentTime = 0f;
        private bool isPlaying = false;
        private float playbackSpeed = 1f;
        
        private Vector2 scrollPos;
        private bool showJointToggles = false;
        private bool[] jointEnabled;
        
        // Cached transforms
        private Transform visualRoot;
        private Transform rootCombatPivot;
        private Transform upperBodyPivot;
        private Transform chestPivot;
        private Transform headPivot;
        private Transform leftShoulderPivot, rightShoulderPivot;
        private Transform leftElbowPivot, rightElbowPivot;
        private Transform leftThighPivot, rightThighPivot;
        private Transform leftKneePivot, rightKneePivot;
        
        // Bind pose storage
        private struct BindPose
        {
            public Vector3 localPosition;
            public Quaternion localRotation;
        }
        private System.Collections.Generic.Dictionary<string, BindPose> bindPoses = 
            new System.Collections.Generic.Dictionary<string, BindPose>();

        [MenuItem("MineCraft/Epic Fight/Animation Preview Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<AnimationPreviewTool>("Animation Preview");
            window.minSize = new Vector2(400, 600);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            RestoreBindPose();
        }

        private void OnEditorUpdate()
        {
            if (isPlaying && selectedAnimation != null)
            {
                currentTime += Time.deltaTime * playbackSpeed;
                
                if (currentTime > selectedAnimation.TotalDuration)
                {
                    if (EditorApplication.isPlaying)
                    {
                        currentTime = 0f; // Loop in play mode
                    }
                    else
                    {
                        currentTime = selectedAnimation.TotalDuration;
                        isPlaying = false;
                    }
                }
                
                ApplyAnimationAtTime(currentTime);
                Repaint();
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Animation Preview Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // Animation Selection
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Setup", EditorStyles.boldLabel);
            
            selectedAnimation = (ConvertedAttackAnimation)EditorGUILayout.ObjectField(
                "Animation Asset", 
                selectedAnimation, 
                typeof(ConvertedAttackAnimation), 
                false
            );

            targetPlayer = (GameObject)EditorGUILayout.ObjectField(
                "Target Player", 
                targetPlayer, 
                typeof(GameObject), 
                true
            );

            if (GUILayout.Button("Locate Player in Scene", GUILayout.Height(25)))
            {
                var player = GameObject.FindFirstObjectByType<MineCraftUnity.Player.PlayerController>();
                if (player != null)
                {
                    targetPlayer = player.gameObject;
                    CachePivots();
                    CaptureBindPose();
                    EditorUtility.DisplayDialog("Success", $"Found player: {targetPlayer.name}", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "No PlayerController found in scene. Please open a scene with Player.", "OK");
                }
            }

            if (GUILayout.Button("Cache Pivots & Bind Pose", GUILayout.Height(25)))
            {
                if (targetPlayer != null)
                {
                    CachePivots();
                    CaptureBindPose();
                    EditorUtility.DisplayDialog("Success", $"Cached {bindPoses.Count} pivot bind poses", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Please assign Target Player first", "OK");
                }
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);

            // Preview Controls
            if (selectedAnimation == null || targetPlayer == null || bindPoses.Count == 0)
            {
                EditorGUILayout.HelpBox("Please select animation, assign target player, and cache bind pose to begin preview.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Playback Controls", EditorStyles.boldLabel);

            // Timeline
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"Time: {currentTime:F3}s / {selectedAnimation.TotalDuration:F3}s", GUILayout.Width(150));
            currentTime = EditorGUILayout.Slider(currentTime, 0f, selectedAnimation.TotalDuration);
            EditorGUILayout.EndHorizontal();

            // Buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button(isPlaying ? "⏸ Pause" : "▶ Play", GUILayout.Height(30)))
            {
                isPlaying = !isPlaying;
            }

            if (GUILayout.Button("⏹ Stop", GUILayout.Height(30)))
            {
                isPlaying = false;
                currentTime = 0f;
                ApplyAnimationAtTime(currentTime);
            }

            if (GUILayout.Button("🔄 Reset Bind Pose", GUILayout.Height(30)))
            {
                RestoreBindPose();
                currentTime = 0f;
            }

            EditorGUILayout.EndHorizontal();

            // Frame stepping
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("◀◀ -0.1s"))
            {
                currentTime = Mathf.Max(0f, currentTime - 0.1f);
                ApplyAnimationAtTime(currentTime);
            }
            if (GUILayout.Button("◀ -0.01s"))
            {
                currentTime = Mathf.Max(0f, currentTime - 0.01f);
                ApplyAnimationAtTime(currentTime);
            }
            if (GUILayout.Button("▶ +0.01s"))
            {
                currentTime = Mathf.Min(selectedAnimation.TotalDuration, currentTime + 0.01f);
                ApplyAnimationAtTime(currentTime);
            }
            if (GUILayout.Button("▶▶ +0.1s"))
            {
                currentTime = Mathf.Min(selectedAnimation.TotalDuration, currentTime + 0.1f);
                ApplyAnimationAtTime(currentTime);
            }
            EditorGUILayout.EndHorizontal();

            // Playback speed
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Speed:", GUILayout.Width(50));
            playbackSpeed = EditorGUILayout.Slider(playbackSpeed, 0.1f, 2f);
            if (GUILayout.Button("1x", GUILayout.Width(40)))
                playbackSpeed = 1f;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);

            // Animation Info
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Animation Info", EditorStyles.boldLabel);
            
            GUILayout.Label($"Attack ID: {selectedAnimation.AttackId}");
            GUILayout.Label($"Total Duration: {selectedAnimation.TotalDuration}s");
            GUILayout.Label($"Hit Window: {selectedAnimation.HitWindowStart:F3}s ~ {selectedAnimation.HitWindowEnd:F3}s");
            GUILayout.Label($"Combo Window: {selectedAnimation.ComboWindowStart:F3}s ~ {selectedAnimation.ComboWindowEnd:F3}s");
            GUILayout.Label($"Tracks: {(selectedAnimation.Tracks != null ? selectedAnimation.Tracks.Count : 0)}");

            // Phase indicator
            string phase = "Idle";
            if (currentTime >= selectedAnimation.HitWindowStart && currentTime <= selectedAnimation.HitWindowEnd)
                phase = "ACTIVE (Hit Window)";
            else if (currentTime < selectedAnimation.HitWindowStart)
                phase = "Windup";
            else
                phase = "Recovery";
            
            GUILayout.Label($"Current Phase: {phase}", EditorStyles.boldLabel);

            EditorGUILayout.EndVertical();
            GUILayout.Space(10);

            // Joint Toggles
            showJointToggles = EditorGUILayout.Foldout(showJointToggles, "Per-Joint Toggles", true);
            if (showJointToggles && selectedAnimation.Tracks != null)
            {
                if (jointEnabled == null || jointEnabled.Length != selectedAnimation.Tracks.Count)
                {
                    jointEnabled = new bool[selectedAnimation.Tracks.Count];
                    for (int i = 0; i < jointEnabled.Length; i++)
                        jointEnabled[i] = true;
                }

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
                for (int i = 0; i < selectedAnimation.Tracks.Count; i++)
                {
                    var track = selectedAnimation.Tracks[i];
                    jointEnabled[i] = EditorGUILayout.ToggleLeft(
                        $"{track.UnityJointName} ({track.Times.Length} keyframes)",
                        jointEnabled[i]
                    );
                }
                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("Enable All"))
                {
                    for (int i = 0; i < jointEnabled.Length; i++)
                        jointEnabled[i] = true;
                }
                if (GUILayout.Button("Disable All"))
                {
                    for (int i = 0; i < jointEnabled.Length; i++)
                        jointEnabled[i] = false;
                }
            }

            GUILayout.Space(10);

            // Apply button
            if (GUILayout.Button("Apply Current Frame", GUILayout.Height(30)))
            {
                ApplyAnimationAtTime(currentTime);
            }
        }

        private void CachePivots()
        {
            if (targetPlayer == null) return;

            visualRoot = targetPlayer.transform.Find("PlayerVisual");
            if (visualRoot == null)
            {
                Debug.LogError("[AnimationPreview] PlayerVisual not found!");
                return;
            }

            rootCombatPivot = visualRoot.Find("RootCombatPivot");
            if (rootCombatPivot == null)
            {
                Debug.LogError("[AnimationPreview] RootCombatPivot not found!");
                return;
            }

            upperBodyPivot = rootCombatPivot.Find("UpperBodyPivot");
            chestPivot = upperBodyPivot?.Find("ChestPivot");
            headPivot = upperBodyPivot?.Find("HeadPivot");
            
            leftShoulderPivot = upperBodyPivot?.Find("LeftShoulderPivot");
            rightShoulderPivot = upperBodyPivot?.Find("RightShoulderPivot");
            leftElbowPivot = leftShoulderPivot?.Find("LeftElbowPivot");
            rightElbowPivot = rightShoulderPivot?.Find("RightElbowPivot");
            
            leftThighPivot = rootCombatPivot.Find("LeftThighPivot");
            rightThighPivot = rootCombatPivot.Find("RightThighPivot");
            leftKneePivot = leftThighPivot?.Find("LeftKneePivot");
            rightKneePivot = rightThighPivot?.Find("RightKneePivot");

            Debug.Log($"[AnimationPreview] Cached pivots successfully");
        }

        private void CaptureBindPose()
        {
            bindPoses.Clear();

            StorePose("RootCombatPivot", rootCombatPivot);
            StorePose("UpperBodyPivot", upperBodyPivot);
            StorePose("ChestPivot", chestPivot);
            StorePose("HeadPivot", headPivot);
            StorePose("LeftShoulderPivot", leftShoulderPivot);
            StorePose("RightShoulderPivot", rightShoulderPivot);
            StorePose("LeftElbowPivot", leftElbowPivot);
            StorePose("RightElbowPivot", rightElbowPivot);
            StorePose("LeftThighPivot", leftThighPivot);
            StorePose("RightThighPivot", rightThighPivot);
            StorePose("LeftKneePivot", leftKneePivot);
            StorePose("RightKneePivot", rightKneePivot);
        }

        private void StorePose(string name, Transform t)
        {
            if (t == null) return;
            
            bindPoses[name] = new BindPose
            {
                localPosition = t.localPosition,
                localRotation = t.localRotation
            };
        }

        private void RestoreBindPose()
        {
            RestorePose("RootCombatPivot", rootCombatPivot);
            RestorePose("UpperBodyPivot", upperBodyPivot);
            RestorePose("ChestPivot", chestPivot);
            RestorePose("HeadPivot", headPivot);
            RestorePose("LeftShoulderPivot", leftShoulderPivot);
            RestorePose("RightShoulderPivot", rightShoulderPivot);
            RestorePose("LeftElbowPivot", leftElbowPivot);
            RestorePose("RightElbowPivot", rightElbowPivot);
            RestorePose("LeftThighPivot", leftThighPivot);
            RestorePose("RightThighPivot", rightThighPivot);
            RestorePose("LeftKneePivot", leftKneePivot);
            RestorePose("RightKneePivot", rightKneePivot);
        }

        private void RestorePose(string name, Transform t)
        {
            if (t == null || !bindPoses.ContainsKey(name)) return;
            
            var pose = bindPoses[name];
            t.localPosition = pose.localPosition;
            t.localRotation = pose.localRotation;
        }

        private void ApplyAnimationAtTime(float time)
        {
            if (selectedAnimation == null || selectedAnimation.Tracks == null) return;

            for (int i = 0; i < selectedAnimation.Tracks.Count; i++)
            {
                // Check per-joint toggle
                if (jointEnabled != null && i < jointEnabled.Length && !jointEnabled[i])
                    continue;

                var track = selectedAnimation.Tracks[i];
                Transform target = GetTransformByName(track.UnityJointName);
                
                if (target == null) continue;

                // Interpolate position and rotation at current time
                InterpolateTrack(track, time, out Vector3 pos, out Quaternion rot);

                // Get bind pose
                if (bindPoses.TryGetValue(track.UnityJointName, out var bindPose))
                {
                    // Apply as delta from bind pose
                    target.localPosition = bindPose.localPosition + pos;
                    target.localRotation = bindPose.localRotation * rot;
                }
                else
                {
                    // No bind pose, apply directly
                    target.localPosition = pos;
                    target.localRotation = rot;
                }
            }

            SceneView.RepaintAll();
        }

        private void InterpolateTrack(ConvertedJointTrack track, float time, out Vector3 pos, out Quaternion rot)
        {
            if (track.Times == null || track.Times.Length == 0)
            {
                pos = Vector3.zero;
                rot = Quaternion.identity;
                return;
            }

            // Find keyframe indices
            if (time <= track.Times[0])
            {
                pos = track.PositionDeltas[0];
                rot = track.RotationDeltas[0];
                return;
            }

            if (time >= track.Times[track.Times.Length - 1])
            {
                pos = track.PositionDeltas[track.Times.Length - 1];
                rot = track.RotationDeltas[track.Times.Length - 1];
                return;
            }

            // Binary search for interpolation
            for (int i = 0; i < track.Times.Length - 1; i++)
            {
                if (time >= track.Times[i] && time <= track.Times[i + 1])
                {
                    float t = (time - track.Times[i]) / (track.Times[i + 1] - track.Times[i]);
                    pos = Vector3.Lerp(track.PositionDeltas[i], track.PositionDeltas[i + 1], t);
                    rot = Quaternion.Slerp(track.RotationDeltas[i], track.RotationDeltas[i + 1], t);
                    return;
                }
            }

            pos = Vector3.zero;
            rot = Quaternion.identity;
        }

        private Transform GetTransformByName(string name)
        {
            switch (name)
            {
                case "RootCombatPivot": return rootCombatPivot;
                case "UpperBodyPivot": return upperBodyPivot;
                case "ChestPivot": return chestPivot;
                case "HeadPivot": return headPivot;
                case "LeftShoulderPivot": return leftShoulderPivot;
                case "RightShoulderPivot": return rightShoulderPivot;
                case "LeftElbowPivot": return leftElbowPivot;
                case "RightElbowPivot": return rightElbowPivot;
                case "LeftThighPivot": return leftThighPivot;
                case "RightThighPivot": return rightThighPivot;
                case "LeftKneePivot": return leftKneePivot;
                case "RightKneePivot": return rightKneePivot;
                default: return null;
            }
        }
    }
}
