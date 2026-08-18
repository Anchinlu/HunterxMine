using UnityEngine;
using UnityEditor;
using System.IO;

namespace MineCraftUnity.Editor
{
    public class MatrixDebugTool : EditorWindow
    {
        private string jsonPath = @"E:\Project game pk\minecraft\Docs\NhiemVu\fist_auto1.json";
        private GameObject playerObject; // Changed: store Player GameObject instead
        private Transform targetJoint; // The specific joint we're inspecting
        private int frameIndex = 0;
        private string jointName = "Root";
        
        private Matrix4x4 rawMatrix;
        private Matrix4x4 parsedMatrix;
        private Vector3 extractedPosition;
        private Quaternion rotation;
        private Vector3 eulerAngles;
        
        private bool applyToTransform = false;
        private int conversionMode = 0; // 0=None, 1=NegateZ, 2=SwapYZ, 3=Custom
        
        // Bind pose storage - store the ENTIRE hierarchy
        private System.Collections.Generic.Dictionary<Transform, TransformData> bindPoseData = new System.Collections.Generic.Dictionary<Transform, TransformData>();
        
        private Vector2 scrollPos;
        
        [System.Serializable]
        private struct TransformData
        {
            public Vector3 localPosition;
            public Quaternion localRotation;
        }

        [MenuItem("MineCraft/Epic Fight/Matrix Debug Tool")]
        public static void ShowWindow()
        {
            var window = GetWindow<MatrixDebugTool>("Matrix Debug");
            window.minSize = new Vector2(600, 700);
        }

        private void OnGUI()
        {
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            GUILayout.Label("Matrix Debug Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);

            // ========== INPUT ==========
            EditorGUILayout.LabelField("Input", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("JSON File:", GUILayout.Width(100));
            jsonPath = EditorGUILayout.TextField(jsonPath);
            EditorGUILayout.EndHorizontal();
            
            jointName = EditorGUILayout.TextField("Joint Name:", jointName);
            frameIndex = EditorGUILayout.IntField("Frame Index:", frameIndex);
            
            // Player selection
            EditorGUILayout.HelpBox("📌 Select the PLAYER GameObject (not a child joint).", MessageType.Info);
            playerObject = (GameObject)EditorGUILayout.ObjectField("Player GameObject:", playerObject, typeof(GameObject), true);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Find Player in Scene"))
            {
                // Try multiple ways to find Player
                GameObject player = GameObject.Find("Player");
                if (player == null)
                {
                    player = GameObject.FindGameObjectWithTag("Player");
                }
                if (player == null)
                {
                    var cc = GameObject.FindFirstObjectByType<UnityEngine.CharacterController>();
                    if (cc != null) player = cc.gameObject;
                }
                
                if (player != null)
                {
                    playerObject = player;
                    Debug.Log($"[MatrixDebug] Found Player: {player.name}");
                    
                    // Also try to find the target joint
                    targetJoint = FindChildRecursive(player.transform, GetUnityJointName(jointName));
                    if (targetJoint != null)
                    {
                        Debug.Log($"[MatrixDebug] Found target joint '{jointName}' at: {GetPath(targetJoint)}");
                    }
                }
                else
                {
                    Debug.LogError("[MatrixDebug] Player GameObject not found in scene!");
                }
            }
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            // Show current target joint (read-only info)
            if (playerObject != null)
            {
                string unityJointName = GetUnityJointName(jointName);
                targetJoint = FindChildRecursive(playerObject.transform, unityJointName);
                
                if (targetJoint != null)
                {
                    EditorGUILayout.LabelField($"✓ Target Joint: {GetPath(targetJoint)}", EditorStyles.wordWrappedMiniLabel);
                }
                else
                {
                    EditorGUILayout.HelpBox($"⚠️ Joint '{jointName}' (Unity: '{unityJointName}') not found under Player", MessageType.Warning);
                }
            }
            
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Joint Mapping: Root→RootCombatPivot, Chest→ChestPivot, Arm_R→RightShoulderPivot", EditorStyles.wordWrappedMiniLabel);
            
            GUILayout.Space(10);

            // ========== CONVERSION MODE ==========
            EditorGUILayout.LabelField("Conversion Mode", EditorStyles.boldLabel);
            string[] modes = new string[] { 
                "0: No Conversion", 
                "1: Negate Z-axis", 
                "2: Swap Y↔Z", 
                "3: Custom (Z→X, Y→Y, X→Z)",
                "4: Transpose First",
                "5: Negate X-axis",
                "6: Z-up to X-fwd (EpicZ→X, EpicX→Y, EpicY→Z)",
                "7: Z-up to X-fwd v2 (EpicZ→X, EpicY→Y, EpicX→Z)",
                "8: Rotate 90° around Y-axis",
                "9: Just negate entire Z column",
                "10: Y-up R-handed → X-fwd L-handed (-Z→X, Y→Y, X→Z)"
            };
            conversionMode = EditorGUILayout.Popup("Mode:", conversionMode, modes);
            
            GUILayout.Space(10);

            // ========== ACTIONS ==========
            if (GUILayout.Button("Parse Matrix from JSON", GUILayout.Height(30)))
            {
                ParseMatrix();
            }
            
            EditorGUI.BeginDisabledGroup(playerObject == null);
            applyToTransform = EditorGUILayout.Toggle("Auto Apply to Transform", applyToTransform);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("📸 Store Bind Pose (Backup)", GUILayout.Height(25)))
            {
                StoreBindPose();
            }
            EditorGUI.BeginDisabledGroup(bindPoseData.Count == 0);
            if (GUILayout.Button("↩️ Restore Bind Pose", GUILayout.Height(25)))
            {
                RestoreBindPose();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.BeginDisabledGroup(targetJoint == null);
            if (GUILayout.Button("Apply to Transform NOW", GUILayout.Height(30)))
            {
                ApplyToTransform();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUI.EndDisabledGroup();
            
            GUILayout.Space(10);

            // ========== OUTPUT ==========
            EditorGUILayout.LabelField("Debug Output", EditorStyles.boldLabel);
            
            // Raw matrix values
            EditorGUILayout.LabelField("Raw Matrix (from JSON array):", EditorStyles.boldLabel);
            DrawMatrix(rawMatrix);
            
            GUILayout.Space(5);
            
            // Parsed matrix after conversion
            EditorGUILayout.LabelField($"Converted Matrix (Mode {conversionMode}):", EditorStyles.boldLabel);
            DrawMatrix(parsedMatrix);
            
            GUILayout.Space(5);
            
            // Extracted values
            EditorGUILayout.LabelField("Extracted Transform:", EditorStyles.boldLabel);
            EditorGUILayout.Vector3Field("Position:", extractedPosition);
            EditorGUILayout.Vector3Field("Euler Angles:", eulerAngles);
            
            GUILayout.Space(5);
            
            // Basis vectors
            EditorGUILayout.LabelField("Basis Vectors (from converted matrix):", EditorStyles.boldLabel);
            Vector3 right = new Vector3(parsedMatrix.m00, parsedMatrix.m10, parsedMatrix.m20);
            Vector3 up = new Vector3(parsedMatrix.m01, parsedMatrix.m11, parsedMatrix.m21);
            Vector3 forward = new Vector3(parsedMatrix.m02, parsedMatrix.m12, parsedMatrix.m22);
            
            EditorGUILayout.Vector3Field("Column 0 (Right?):", right);
            EditorGUILayout.Vector3Field("Column 1 (Up?):", up);
            EditorGUILayout.Vector3Field("Column 2 (Forward?):", forward);
            
            EditorGUILayout.EndScrollView();
        }

        private void DrawMatrix(Matrix4x4 m)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"| {m.m00:F4}  {m.m01:F4}  {m.m02:F4}  {m.m03:F4} |");
            EditorGUILayout.LabelField($"| {m.m10:F4}  {m.m11:F4}  {m.m12:F4}  {m.m13:F4} |");
            EditorGUILayout.LabelField($"| {m.m20:F4}  {m.m21:F4}  {m.m22:F4}  {m.m23:F4} |");
            EditorGUILayout.LabelField($"| {m.m30:F4}  {m.m31:F4}  {m.m32:F4}  {m.m33:F4} |");
            EditorGUILayout.EndVertical();
        }

        private void ParseMatrix()
        {
            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[MatrixDebug] File not found: {jsonPath}");
                return;
            }

            string json = File.ReadAllText(jsonPath);
            
            // Find joint by name
            int jointIdx = json.IndexOf($"\"name\": \"{jointName}\"");
            if (jointIdx < 0)
            {
                Debug.LogError($"[MatrixDebug] Joint '{jointName}' not found in JSON");
                return;
            }

            // Find transform array
            int transformIdx = json.IndexOf("\"transform\"", jointIdx);
            if (transformIdx < 0)
            {
                Debug.LogError($"[MatrixDebug] 'transform' key not found after joint '{jointName}'");
                return;
            }

            // Find opening bracket of transform array
            int arrayStart = json.IndexOf('[', transformIdx);
            if (arrayStart < 0)
            {
                Debug.LogError($"[MatrixDebug] Transform array '[' not found");
                return;
            }

            // Parse matrices
            int depth = 0;
            int matrixStart = -1;
            int currentMatrixIdx = 0;

            for (int i = arrayStart; i < json.Length; i++)
            {
                char c = json[i];
                
                if (c == '[')
                {
                    if (depth == 1) matrixStart = i;
                    depth++;
                }
                else if (c == ']')
                {
                    if (depth == 2 && matrixStart >= 0)
                    {
                        if (currentMatrixIdx == frameIndex)
                        {
                            // Parse this matrix
                            string matrixStr = json.Substring(matrixStart + 1, i - matrixStart - 1);
                            ParseMatrixString(matrixStr);
                            Debug.Log($"[MatrixDebug] Parsed matrix for joint '{jointName}' frame {frameIndex}");
                            
                            if (applyToTransform)
                            {
                                ApplyToTransform();
                            }
                            
                            return;
                        }
                        currentMatrixIdx++;
                        matrixStart = -1;
                    }
                    
                    depth--;
                    if (depth == 0) break;
                }
            }

            Debug.LogError($"[MatrixDebug] Frame {frameIndex} not found (only {currentMatrixIdx} frames available)");
        }

        private void ParseMatrixString(string matrixStr)
        {
            string[] values = matrixStr.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            if (values.Length != 16)
            {
                Debug.LogError($"[MatrixDebug] Expected 16 values, got {values.Length}");
                return;
            }

            float[] m = new float[16];
            for (int i = 0; i < 16; i++)
            {
                if (!float.TryParse(values[i].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out m[i]))
                {
                    Debug.LogError($"[MatrixDebug] Failed to parse value {i}: '{values[i]}'");
                    return;
                }
            }

            // Store raw matrix (as row-major interpretation)
            rawMatrix = new Matrix4x4();
            rawMatrix.m00 = m[0];  rawMatrix.m01 = m[1];  rawMatrix.m02 = m[2];  rawMatrix.m03 = m[3];
            rawMatrix.m10 = m[4];  rawMatrix.m11 = m[5];  rawMatrix.m12 = m[6];  rawMatrix.m13 = m[7];
            rawMatrix.m20 = m[8];  rawMatrix.m21 = m[9];  rawMatrix.m22 = m[10]; rawMatrix.m23 = m[11];
            rawMatrix.m30 = m[12]; rawMatrix.m31 = m[13]; rawMatrix.m32 = m[14]; rawMatrix.m33 = m[15];

            // Parse as column-major (correct interpretation)
            Matrix4x4 colMajor = new Matrix4x4();
            colMajor.m00 = m[0];  colMajor.m10 = m[1];  colMajor.m20 = m[2];  colMajor.m30 = m[3];
            colMajor.m01 = m[4];  colMajor.m11 = m[5];  colMajor.m21 = m[6];  colMajor.m31 = m[7];
            colMajor.m02 = m[8];  colMajor.m12 = m[9];  colMajor.m22 = m[10]; colMajor.m32 = m[11];
            colMajor.m03 = m[12]; colMajor.m13 = m[13]; colMajor.m23 = m[14]; colMajor.m33 = m[15];

            // Apply conversion based on mode
            parsedMatrix = ApplyConversion(colMajor, conversionMode);

            // Extract position and rotation
            extractedPosition = new Vector3(parsedMatrix.m03, parsedMatrix.m13, parsedMatrix.m23);
            rotation = ExtractRotation(parsedMatrix);
            eulerAngles = rotation.eulerAngles;

            Debug.Log($"[MatrixDebug] Position: {extractedPosition}, Euler: {eulerAngles}");
        }

        private Matrix4x4 ApplyConversion(Matrix4x4 input, int mode)
        {
            switch (mode)
            {
                case 0: // No conversion
                    return input;

                case 1: // Negate Z-axis
                {
                    Matrix4x4 result = input;
                    result.m02 = -input.m02;
                    result.m12 = -input.m12;
                    result.m22 = -input.m22;
                    result.m23 = -input.m23;
                    return result;
                }

                case 2: // Swap Y↔Z
                {
                    Matrix4x4 result = Matrix4x4.identity;
                    // Column 0 (Right): X stays X
                    result.m00 = input.m00;
                    result.m10 = input.m20; // Swap
                    result.m20 = input.m10; // Swap
                    // Column 1 (Up): swap Y↔Z
                    result.m01 = input.m02;
                    result.m11 = input.m22;
                    result.m21 = input.m12;
                    // Column 2 (Forward): swap Y↔Z
                    result.m02 = input.m01;
                    result.m12 = input.m21;
                    result.m22 = input.m11;
                    // Position
                    result.m03 = input.m03;
                    result.m13 = input.m23;
                    result.m23 = input.m13;
                    result.m33 = 1f;
                    return result;
                }

                case 3: // Custom (Z→X, Y→Y, X→Z)
                {
                    Matrix4x4 result = Matrix4x4.identity;
                    result.m00 = input.m02; result.m10 = input.m12; result.m20 = input.m22;
                    result.m01 = input.m01; result.m11 = input.m11; result.m21 = input.m21;
                    result.m02 = input.m00; result.m12 = input.m10; result.m22 = input.m20;
                    result.m03 = input.m23; result.m13 = input.m13; result.m23 = input.m03;
                    result.m33 = 1f;
                    return result;
                }

                case 4: // Transpose first
                {
                    return input.transpose;
                }

                case 5: // Negate X-axis
                {
                    Matrix4x4 result = input;
                    result.m00 = -input.m00;
                    result.m10 = -input.m10;
                    result.m20 = -input.m20;
                    result.m03 = -input.m03;
                    return result;
                }

                case 6: // Z-up to X-forward (Epic Z→Unity X, Epic X→Unity Y, Epic Y→Unity Z)
                {
                    Matrix4x4 result = Matrix4x4.identity;
                    // Column 0 (Unity X/Forward) = Epic Column 2 (Z-up)
                    result.m00 = input.m02;
                    result.m10 = input.m12;
                    result.m20 = input.m22;
                    // Column 1 (Unity Y/Up) = Epic Column 0 (X-right)
                    result.m01 = input.m00;
                    result.m11 = input.m10;
                    result.m21 = input.m20;
                    // Column 2 (Unity Z/Right) = Epic Column 1 (Y-forward)
                    result.m02 = input.m01;
                    result.m12 = input.m11;
                    result.m22 = input.m21;
                    // Position
                    result.m03 = input.m23; // X = Epic Z
                    result.m13 = input.m03; // Y = Epic X
                    result.m23 = input.m13; // Z = Epic Y
                    result.m33 = 1f;
                    return result;
                }

                case 7: // Z-up to X-forward v2 (Epic Z→X, Epic Y→Y, Epic X→Z)
                {
                    Matrix4x4 result = Matrix4x4.identity;
                    // Column 0 (Unity X/Forward) = Epic Column 2 (Z-up)
                    result.m00 = input.m02;
                    result.m10 = input.m12;
                    result.m20 = input.m22;
                    // Column 1 (Unity Y/Up) = Epic Column 1 (Y-forward)
                    result.m01 = input.m01;
                    result.m11 = input.m11;
                    result.m21 = input.m21;
                    // Column 2 (Unity Z/Right) = Epic Column 0 (X-right)
                    result.m02 = input.m00;
                    result.m12 = input.m10;
                    result.m22 = input.m20;
                    // Position
                    result.m03 = input.m23; // X = Epic Z
                    result.m13 = input.m13; // Y = Epic Y
                    result.m23 = input.m03; // Z = Epic X
                    result.m33 = 1f;
                    return result;
                }

                case 8: // Rotate 90° around Y-axis (turn sideways)
                {
                    // Rotation matrix: cos(90°)=0, sin(90°)=1
                    // |  0  0  1 |
                    // |  0  1  0 |
                    // | -1  0  0 |
                    Matrix4x4 result = Matrix4x4.identity;
                    result.m00 = input.m02;
                    result.m10 = input.m12;
                    result.m20 = input.m22;
                    result.m01 = input.m01;
                    result.m11 = input.m11;
                    result.m21 = input.m21;
                    result.m02 = -input.m00;
                    result.m12 = -input.m10;
                    result.m22 = -input.m20;
                    result.m03 = input.m23;
                    result.m13 = input.m13;
                    result.m23 = -input.m03;
                    result.m33 = 1f;
                    return result;
                }

                case 9: // Just negate Z column
                {
                    Matrix4x4 result = input;
                    result.m02 = -input.m02;
                    result.m12 = -input.m12;
                    result.m22 = -input.m22;
                    result.m23 = -input.m23;
                    return result;
                }

                case 10: // Y-up Z-fwd RIGHT → Y-up X-fwd LEFT (Negate Z, rotate to X)
                {
                    // Epic: Y-up, Z-forward, X-right (right-handed)
                    // Project: Y-up, X-forward, Z-right (left-handed custom)
                    // 
                    // Conversion:
                    // 1. Negate Z to flip handedness
                    // 2. Rotate 90° CCW around Y: Z→X, X→-Z, Y→Y
                    Matrix4x4 result = Matrix4x4.identity;
                    
                    // Column 0 (Project X-forward) = -Epic Column 2 (negate Z)
                    result.m00 = -input.m02;
                    result.m10 = -input.m12;
                    result.m20 = -input.m22;
                    
                    // Column 1 (Project Y-up) = Epic Column 1 (unchanged)
                    result.m01 = input.m01;
                    result.m11 = input.m11;
                    result.m21 = input.m21;
                    
                    // Column 2 (Project Z-right) = Epic Column 0 (was X-right)
                    result.m02 = input.m00;
                    result.m12 = input.m10;
                    result.m22 = input.m20;
                    
                    // Position: rotate same way
                    result.m03 = -input.m23; // X = -Epic.Z
                    result.m13 = input.m13;  // Y = Epic.Y
                    result.m23 = input.m03;  // Z = Epic.X
                    result.m33 = 1f;
                    
                    return result;
                }

                default:
                    return input;
            }
        }

        private Quaternion ExtractRotation(Matrix4x4 matrix)
        {
            Vector3 forward = new Vector3(matrix.m02, matrix.m12, matrix.m22).normalized;
            Vector3 up = new Vector3(matrix.m01, matrix.m11, matrix.m21).normalized;
            
            if (forward.sqrMagnitude < 0.01f || up.sqrMagnitude < 0.01f)
            {
                return Quaternion.identity;
            }

            return Quaternion.LookRotation(forward, up);
        }

        private void ApplyToTransform()
        {
            if (targetJoint == null)
            {
                Debug.LogError("[MatrixDebug] No target joint found");
                return;
            }

            targetJoint.localPosition = extractedPosition;
            targetJoint.localRotation = rotation;
            
            Debug.Log($"[MatrixDebug] Applied to {targetJoint.name}: pos={extractedPosition}, euler={eulerAngles}");
        }

        private void StoreBindPose()
        {
            if (playerObject == null)
            {
                Debug.LogError("[MatrixDebug] No player object assigned");
                return;
            }

            bindPoseData.Clear();
            StoreTransformRecursive(playerObject.transform);
            
            Debug.Log($"[MatrixDebug] Stored bind pose for {playerObject.name} and {bindPoseData.Count} children");
        }
        
        private void StoreTransformRecursive(Transform t)
        {
            bindPoseData[t] = new TransformData
            {
                localPosition = t.localPosition,
                localRotation = t.localRotation
            };
            
            foreach (Transform child in t)
            {
                StoreTransformRecursive(child);
            }
        }

        private void RestoreBindPose()
        {
            if (playerObject == null)
            {
                Debug.LogError("[MatrixDebug] No player object assigned");
                return;
            }

            if (bindPoseData.Count == 0)
            {
                Debug.LogWarning("[MatrixDebug] No bind pose stored yet! Click 'Store Bind Pose' first");
                return;
            }

            foreach (var kvp in bindPoseData)
            {
                if (kvp.Key != null) // Check if transform still exists
                {
                    kvp.Key.localPosition = kvp.Value.localPosition;
                    kvp.Key.localRotation = kvp.Value.localRotation;
                }
            }
            
            Debug.Log($"[MatrixDebug] Restored bind pose for {playerObject.name}");
        }
        
        private string GetUnityJointName(string epicJointName)
        {
            // Map Epic Fight joint names to Unity skeleton names
            switch (epicJointName)
            {
                case "Root": return "RootCombatPivot";
                case "Chest": return "ChestPivot";
                case "Arm_R": return "RightShoulderPivot";
                case "Arm_L": return "LeftShoulderPivot";
                case "Leg_R": return "RightThighPivot";
                case "Leg_L": return "LeftThighPivot";
                default: return epicJointName; // Try exact name
            }
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name == name) return parent;
            
            foreach (Transform child in parent)
            {
                Transform result = FindChildRecursive(child, name);
                if (result != null) return result;
            }
            
            return null;
        }

        private string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }

        private void LogChildren(Transform parent, int depth)
        {
            string indent = new string(' ', depth * 2);
            foreach (Transform child in parent)
            {
                Debug.Log($"{indent}- {child.name}");
                if (depth < 3)
                {
                    LogChildren(child, depth + 1);
                }
            }
        }
    }
}
