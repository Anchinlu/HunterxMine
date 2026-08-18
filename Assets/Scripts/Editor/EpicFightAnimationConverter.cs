using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using MineCraftUnity.Player.Combat;

namespace MineCraftUnity.Editor
{
    /// <summary>
    /// Công cụ chuyển đổi Epic Fight JSON animation sang Unity ConvertedAttackAnimation assets.
    /// 
    /// Pipeline:
    /// 1. Load và parse JSON
    /// 2. Convert matrices từ Epic Fight space sang Unity space
    /// 3. Map joints Epic Fight → Unity pivots
    /// 4. Extract timing metadata
    /// 5. Generate ScriptableObject assets
    /// 
    /// Matrix Convention (đã xác định):
    /// - Format: Row-major, 16 floats [m00,m01,m02,m03, m10,m11,m12,m13, ...]
    /// - Translation: m03, m13, m23
    /// - Coordinate: Right-handed, Y-up
    /// - Space: Parent space (transforms relative to parent joint)
    /// </summary>
    public class EpicFightAnimationConverter : EditorWindow
    {
        private string sourceFolder = @"E:\Project game pk\minecraft\Docs\NhiemVu";
        private string outputFolder = "Assets/Resources/Combat/ConvertedAnimations";
        
        private Vector2 scrollPos;
        private string logOutput = "";

        // Joint mapping Epic Fight → Unity
        private static readonly Dictionary<string, string> JointMapping = new Dictionary<string, string>
        {
            // Bắt buộc - 1:1 mapping
            { "Root", "RootCombatPivot" },
            { "Torso", "UpperBodyPivot" },
            { "Chest", "ChestPivot" },
            { "Head", "HeadPivot" },
            { "Shoulder_L", "LeftShoulderPivot" },
            { "Shoulder_R", "RightShoulderPivot" },
            { "Elbow_L", "LeftElbowPivot" },
            { "Elbow_R", "RightElbowPivot" },
            { "Thigh_L", "LeftThighPivot" },
            { "Thigh_R", "RightThighPivot" },
            { "Knee_L", "LeftKneePivot" },
            { "Knee_R", "RightKneePivot" },
            
            // Intermediate joints - merge vào Shoulder/Thigh (ghi rõ trong báo cáo)
            { "Arm_L", "LeftShoulderPivot" },  // Merge vào Shoulder
            { "Arm_R", "RightShoulderPivot" },  // Merge vào Shoulder
            { "Leg_L", "LeftThighPivot" },      // Merge vào Thigh
            { "Leg_R", "RightThighPivot" },     // Merge vào Thigh
            
            // Bỏ qua phase này
            // { "Hand_L", null },
            // { "Hand_R", null },
            // { "Tool_L", null },
            // { "Tool_R", null },
        };

        // Metadata cho từng attack (phân tích từ timing và gameplay)
        private static readonly Dictionary<string, AttackMetadata> AttackMetadataMap = new Dictionary<string, AttackMetadata>
        {
            { "fist_auto1", new AttackMetadata {
                TotalDuration = 0.5f,
                HitWindowStart = 0.1333f,
                HitWindowEnd = 0.2333f,
                ComboWindowStart = 0.2f,
                ComboWindowEnd = 0.45f,
                NextComboAttackId = "fist_auto2",
                MovementMultiplier = 0.3f,
                IsGroundCompatible = true,
                IsAirCompatible = false
            }},
            { "fist_auto2", new AttackMetadata {
                TotalDuration = 0.5f,
                HitWindowStart = 0.1333f,
                HitWindowEnd = 0.2333f,
                ComboWindowStart = 0.2f,
                ComboWindowEnd = 0.45f,
                NextComboAttackId = "fist_auto3",
                MovementMultiplier = 0.2f,
                IsGroundCompatible = true,
                IsAirCompatible = false
            }},
            { "fist_auto3", new AttackMetadata {
                TotalDuration = 0.5f,
                HitWindowStart = 0.1333f,
                HitWindowEnd = 0.2333f,
                ComboWindowStart = 0.2f,
                ComboWindowEnd = 0.45f,
                NextComboAttackId = null,
                MovementMultiplier = 0.1f,
                IsGroundCompatible = true,
                IsAirCompatible = false
            }},
            { "fist_dash", new AttackMetadata {
                TotalDuration = 0.5f,
                HitWindowStart = 0.1f,
                HitWindowEnd = 0.25f,
                ComboWindowStart = 0.25f,
                ComboWindowEnd = 0.45f,
                NextComboAttackId = "fist_auto2",
                MovementMultiplier = 1.5f,
                IsGroundCompatible = true,
                IsAirCompatible = false
            }},
            { "fist_airslash", new AttackMetadata {
                TotalDuration = 0.5f,
                HitWindowStart = 0.1f,
                HitWindowEnd = 0.3f,
                ComboWindowStart = 0.0f,
                ComboWindowEnd = 0.0f,
                NextComboAttackId = null,
                MovementMultiplier = 0.5f,
                IsGroundCompatible = false,
                IsAirCompatible = true
            }},
        };

        private class AttackMetadata
        {
            public float TotalDuration;
            public float HitWindowStart;
            public float HitWindowEnd;
            public float ComboWindowStart;
            public float ComboWindowEnd;
            public string NextComboAttackId;
            public float MovementMultiplier;
            public bool IsGroundCompatible;
            public bool IsAirCompatible;
        }

        [MenuItem("MineCraft/Epic Fight/Animation Converter")]
        public static void ShowWindow()
        {
            var window = GetWindow<EpicFightAnimationConverter>("EF Animation Converter");
            window.minSize = new Vector2(600, 500);
        }

        private void OnGUI()
        {
            GUILayout.Label("Epic Fight Animation Converter", EditorStyles.boldLabel);
            GUILayout.Label("Convert 5 fist animations from JSON to Unity assets", EditorStyles.miniLabel);
            GUILayout.Space(10);

            EditorGUILayout.LabelField("Source Folder (JSON files):");
            sourceFolder = EditorGUILayout.TextField(sourceFolder);

            EditorGUILayout.LabelField("Output Folder (Unity assets):");
            outputFolder = EditorGUILayout.TextField(outputFolder);

            GUILayout.Space(10);

            if (GUILayout.Button("Convert All 5 Animations", GUILayout.Height(40)))
            {
                ConvertAllAnimations();
            }

            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Convert fist_auto1"))
                ConvertSingleAnimation("fist_auto1");
            if (GUILayout.Button("Convert fist_auto2"))
                ConvertSingleAnimation("fist_auto2");
            if (GUILayout.Button("Convert fist_auto3"))
                ConvertSingleAnimation("fist_auto3");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Convert fist_dash"))
                ConvertSingleAnimation("fist_dash");
            if (GUILayout.Button("Convert fist_airslash"))
                ConvertSingleAnimation("fist_airslash");
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);
            GUILayout.Label("Conversion Log:", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(logOutput, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void ConvertAllAnimations()
        {
            logOutput = "=== BATCH CONVERSION START ===\n\n";
            
            string[] animations = { "fist_auto1", "fist_auto2", "fist_auto3", "fist_dash", "fist_airslash" };
            int successCount = 0;

            foreach (string animName in animations)
            {
                if (ConvertSingleAnimation(animName))
                {
                    successCount++;
                }
                logOutput += "\n";
            }

            logOutput += $"\n=== CONVERSION COMPLETE: {successCount}/{animations.Length} successful ===\n";
            Repaint();
        }

        private bool ConvertSingleAnimation(string animationName)
        {
            logOutput += $"[{animationName}] Starting conversion...\n";

            try
            {
                // 1. Load JSON
                string jsonPath = Path.Combine(sourceFolder, animationName + ".json");
                if (!File.Exists(jsonPath))
                {
                    logOutput += $"[{animationName}] ERROR: File not found: {jsonPath}\n";
                    return false;
                }

                string jsonText = File.ReadAllText(jsonPath);
                var jointDataList = ParseEpicFightJSON(jsonText);

                if (jointDataList == null || jointDataList.Count == 0)
                {
                    logOutput += $"[{animationName}] ERROR: Failed to parse JSON (no joints found)\n";
                    return false;
                }

                logOutput += $"[{animationName}] Parsed {jointDataList.Count} joints from JSON\n";

                // 2. Get metadata
                if (!AttackMetadataMap.TryGetValue(animationName, out var metadata))
                {
                    logOutput += $"[{animationName}] ERROR: No metadata defined for this animation\n";
                    return false;
                }

                // 3. Create asset
                var asset = ScriptableObject.CreateInstance<ConvertedAttackAnimation>();
                asset.AttackId = animationName;
                asset.TotalDuration = metadata.TotalDuration;
                asset.HitWindowStart = metadata.HitWindowStart;
                asset.HitWindowEnd = metadata.HitWindowEnd;
                asset.ComboWindowStart = metadata.ComboWindowStart;
                asset.ComboWindowEnd = metadata.ComboWindowEnd;
                asset.NextComboAttackId = metadata.NextComboAttackId;
                asset.MovementMultiplier = metadata.MovementMultiplier;
                asset.IsAirCompatible = metadata.IsAirCompatible;
                asset.IsGroundCompatible = metadata.IsGroundCompatible;

                // 4. Convert joints → tracks
                asset.Tracks = new List<ConvertedJointTrack>();
                int tracksConverted = 0;
                int tracksMerged = 0;
                int tracksSkipped = 0;

                foreach (var jointData in jointDataList)
                {
                    if (!JointMapping.TryGetValue(jointData.name, out string unityJointName))
                    {
                        // Joint không có trong mapping → skip
                        tracksSkipped++;
                        logOutput += $"[{animationName}]   Skip: {jointData.name} (not in mapping table)\n";
                        continue;
                    }

                    // Check nếu là intermediate joint đang được merge
                    bool isMerged = jointData.name.StartsWith("Arm_") || jointData.name.StartsWith("Leg_");
                    if (isMerged)
                    {
                        tracksMerged++;
                        logOutput += $"[{animationName}]   Merge: {jointData.name} → {unityJointName}\n";
                    }

                    var track = ConvertJointToTrack(jointData, unityJointName);
                    if (track != null)
                    {
                        asset.Tracks.Add(track);
                        tracksConverted++;
                    }
                }

                logOutput += $"[{animationName}] Converted: {tracksConverted} tracks, Merged: {tracksMerged}, Skipped: {tracksSkipped}\n";

                // 5. Save asset
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                string assetPath = Path.Combine(outputFolder, animationName + "_Converted.asset");
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                logOutput += $"[{animationName}] ✓ SUCCESS: Saved to {assetPath}\n";
                logOutput += $"[{animationName}]   Duration: {asset.TotalDuration}s, Hit: {asset.HitWindowStart}~{asset.HitWindowEnd}, Combo: {asset.ComboWindowStart}~{asset.ComboWindowEnd}\n";

                return true;
            }
            catch (System.Exception e)
            {
                logOutput += $"[{animationName}] EXCEPTION: {e.Message}\n{e.StackTrace}\n";
                return false;
            }
        }

        private ConvertedJointTrack ConvertJointToTrack(JointData jointData, string unityJointName)
        {
            if (jointData.matrices == null || jointData.matrices.Count == 0)
                return null;

            var track = new ConvertedJointTrack();
            track.UnityJointName = unityJointName;
            track.Times = jointData.times.ToArray();
            track.PositionDeltas = new Vector3[jointData.matrices.Count];
            track.RotationDeltas = new Quaternion[jointData.matrices.Count];

            for (int i = 0; i < jointData.matrices.Count; i++)
            {
                float[] m = jointData.matrices[i];
                
                // Parse matrix from JSON array
                // Epic Fight JSON uses column-major order (OpenGL/Forge convention)
                // Unity Matrix4x4 also uses column-major internally
                // So we parse as columns (transpose from row-major interpretation)
                Matrix4x4 epicMatrix = new Matrix4x4();
                
                // Column 0 (right vector)
                epicMatrix.m00 = m[0];
                epicMatrix.m10 = m[1];
                epicMatrix.m20 = m[2];
                epicMatrix.m30 = m[3];
                
                // Column 1 (up vector)
                epicMatrix.m01 = m[4];
                epicMatrix.m11 = m[5];
                epicMatrix.m21 = m[6];
                epicMatrix.m31 = m[7];
                
                // Column 2 (forward vector)
                epicMatrix.m02 = m[8];
                epicMatrix.m12 = m[9];
                epicMatrix.m22 = m[10];
                epicMatrix.m32 = m[11];
                
                // Column 3 (position)
                epicMatrix.m03 = m[12];
                epicMatrix.m13 = m[13];
                epicMatrix.m23 = m[14];
                epicMatrix.m33 = m[15];

                // Convert coordinate system: Epic Fight → Unity
                // Epic Fight: Y-up, Right-handed, Z-forward
                // Unity: Y-up, Left-handed, Z-forward
                Matrix4x4 unityMatrix = ConvertEpicFightToUnity(epicMatrix);

                // Extract position and rotation
                track.PositionDeltas[i] = ExtractPosition(unityMatrix);
                track.RotationDeltas[i] = ExtractRotation(unityMatrix);
            }

            return track;
        }

        private Matrix4x4 ConvertEpicFightToUnity(Matrix4x4 epicMatrix)
        {
            // DISCOVERY: After extensive testing with MatrixDebugTool:
            // - Chest joint with Mode 0 (no conversion) works perfectly!
            // - Frame 0 ≈ (0,0,0) confirming animations are ABSOLUTE transforms
            // - Epic Fight animations are ALREADY in compatible format!
            //
            // Epic Fight format (tested):
            // - Y-up coordinate system (Column 1 has Y dominance)
            // - Column-major matrix layout
            // - Rotations work directly in Unity without conversion
            //
            // Root joint fails with Mode 0, but that's likely a special case
            // (world-space orientation) that may need different handling.
            //
            // For now: NO CONVERSION for all joints
            
            return epicMatrix;
        }

        private Vector3 ExtractPosition(Matrix4x4 matrix)
        {
            return new Vector3(matrix.m03, matrix.m13, matrix.m23);
        }

        private Quaternion ExtractRotation(Matrix4x4 matrix)
        {
            // Extract 3x3 rotation matrix and normalize
            Vector3 forward = new Vector3(matrix.m02, matrix.m12, matrix.m22).normalized;
            Vector3 up = new Vector3(matrix.m01, matrix.m11, matrix.m21).normalized;
            
            if (forward.sqrMagnitude < 0.01f || up.sqrMagnitude < 0.01f)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(forward, up);
        }

        // JSON Parser - Improved version with better error handling
        private class JointData
        {
            public string name;
            public List<float> times = new List<float>();
            public List<float[]> matrices = new List<float[]>();
        }

        private List<JointData> ParseEpicFightJSON(string json)
        {
            var result = new List<JointData>();
            
            try
            {
                Debug.Log($"[Parser] Starting parse, JSON length: {json.Length}");
                
                // Find "animation" array
                int animStart = json.IndexOf("\"animation\"");
                if (animStart < 0)
                {
                    Debug.LogError("[Parser] 'animation' key not found in JSON");
                    return result;
                }
                
                Debug.Log($"[Parser] Found 'animation' key at index {animStart}");

                int arrayStart = json.IndexOf('[', animStart);
                if (arrayStart < 0)
                {
                    Debug.LogError("[Parser] Animation array '[' not found");
                    return result;
                }
                
                Debug.Log($"[Parser] Found array start '[' at index {arrayStart}");

                int depth = 1; // Start at depth 1 because we just found the '['
                int objStart = -1;
                int objCount = 0;

                // Start parsing from AFTER the array '['
                for (int i = arrayStart + 1; i < json.Length; i++)
                {
                    char c = json[i];
                    
                    if (c == '{')
                    {
                        if (depth == 1)
                        {
                            objStart = i; // Start of joint object
                        }
                        depth++;
                    }
                    else if (c == '}')
                    {
                        depth--;
                        if (depth == 1 && objStart >= 0)
                        {
                            objCount++;
                            // Parse one joint object
                            string jointJson = json.Substring(objStart, i - objStart + 1);
                            Debug.Log($"[Parser] Parsing joint object #{objCount}, length: {jointJson.Length}");
                            
                            var joint = ParseJoint(jointJson);
                            if (joint != null && !string.IsNullOrEmpty(joint.name))
                            {
                                result.Add(joint);
                                Debug.Log($"[Parser] ✓ Successfully parsed joint: '{joint.name}' with {joint.times.Count} keyframes and {joint.matrices.Count} matrices");
                            }
                            else
                            {
                                Debug.LogWarning($"[Parser] ✗ Failed to parse joint object #{objCount}");
                            }
                            objStart = -1;
                        }
                        else if (depth == 0)
                        {
                            Debug.Log($"[Parser] Reached end of animation array after processing {objCount} objects");
                            break; // End of animation array (matched the opening '[')
                        }
                    }
                    else if (c == ']' && depth == 1)
                    {
                        // End of animation array
                        Debug.Log($"[Parser] Reached end ']' of animation array after processing {objCount} objects");
                        break;
                    }
                }

                if (result.Count == 0)
                {
                    Debug.LogError($"[Parser] No joints parsed from JSON (found {objCount} objects, parsed 0 successfully)");
                }
                else
                {
                    Debug.Log($"[Parser] ✓ Successfully parsed {result.Count} joints out of {objCount} objects");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Parser] Exception in ParseEpicFightJSON: {e.Message}\n{e.StackTrace}");
            }

            return result;
        }

        private JointData ParseJoint(string jointJson)
        {
            try
            {
                var joint = new JointData();

                // Parse name
                int nameIdx = jointJson.IndexOf("\"name\"");
                if (nameIdx >= 0)
                {
                    int nameStart = jointJson.IndexOf('\"', nameIdx + 6) + 1;
                    int nameEnd = jointJson.IndexOf('\"', nameStart);
                    if (nameEnd > nameStart)
                    {
                        joint.name = jointJson.Substring(nameStart, nameEnd - nameStart);
                        Debug.Log($"[Parser] Joint name: '{joint.name}'");
                    }
                    else
                    {
                        Debug.LogWarning("[Parser] Failed to extract joint name");
                    }
                }
                else
                {
                    Debug.LogWarning("[Parser] 'name' field not found in joint JSON");
                }

                // Parse time array
                int timeIdx = jointJson.IndexOf("\"time\"");
                if (timeIdx >= 0)
                {
                    int timeArrStart = jointJson.IndexOf('[', timeIdx);
                    int timeArrEnd = jointJson.IndexOf(']', timeArrStart);
                    if (timeArrEnd > timeArrStart)
                    {
                        string timeStr = jointJson.Substring(timeArrStart + 1, timeArrEnd - timeArrStart - 1);
                        string[] timeTokens = timeStr.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                        
                        foreach (string t in timeTokens)
                        {
                            string trimmed = t.Trim();
                            if (float.TryParse(trimmed, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float time))
                            {
                                joint.times.Add(time);
                            }
                        }
                        
                        Debug.Log($"[Parser] Parsed {joint.times.Count} time values");
                    }
                }

                // Parse transform array (array of arrays)
                int transformIdx = jointJson.IndexOf("\"transform\"");
                if (transformIdx >= 0)
                {
                    int transformArrStart = jointJson.IndexOf('[', transformIdx);
                    int depth = 0;
                    int matrixStart = -1;
                    int matrixCount = 0;

                    for (int i = transformArrStart; i < jointJson.Length; i++)
                    {
                        char c = jointJson[i];
                        
                        if (c == '[')
                        {
                            if (depth == 1) matrixStart = i;
                            depth++;
                        }
                        else if (c == ']')
                        {
                            if (depth == 2 && matrixStart >= 0)
                            {
                                // Parse one matrix (16 floats)
                                string matrixStr = jointJson.Substring(matrixStart + 1, i - matrixStart - 1);
                                string[] values = matrixStr.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                                
                                float[] matrix = new float[16];
                                int parsedCount = 0;
                                
                                for (int j = 0; j < values.Length && parsedCount < 16; j++)
                                {
                                    string val = values[j].Trim();
                                    // Handle scientific notation (e.g., -5e-06, 1e-06)
                                    if (float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float result))
                                    {
                                        matrix[parsedCount] = result;
                                        parsedCount++;
                                    }
                                }
                                
                                if (parsedCount == 16)
                                {
                                    joint.matrices.Add(matrix);
                                    matrixCount++;
                                }
                                else
                                {
                                    Debug.LogWarning($"[Parser] Matrix #{matrixCount} incomplete: only {parsedCount}/16 values");
                                }
                                
                                matrixStart = -1;
                            }
                            
                            depth--;
                            if (depth == 0) break;
                        }
                    }
                    
                    Debug.Log($"[Parser] Parsed {matrixCount} matrices");
                }

                return joint;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Parser] Exception in ParseJoint: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
    }
}
