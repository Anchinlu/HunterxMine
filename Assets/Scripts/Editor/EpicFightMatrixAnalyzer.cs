using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace MineCraftUnity.Editor
{
    /// <summary>
    /// Tool để phân tích Epic Fight JSON matrix format và xác định convention.
    /// 
    /// Matrix 4x4 trong JSON có dạng array 16 phần tử:
    /// [m00, m01, m02, m03, m10, m11, m12, m13, m20, m21, m22, m23, m30, m31, m32, m33]
    /// 
    /// Cần xác định:
    /// 1. Row-major hay Column-major?
    /// 2. Rotation matrix handedness?
    /// 3. Translation position trong matrix?
    /// 4. Local space hay parent space?
    /// </summary>
    public class EpicFightMatrixAnalyzer : EditorWindow
    {
        private string jsonPath = @"E:\Project game pk\minecraft\Docs\NhiemVu\fist_auto1.json";
        private string analysisResult = "";
        private Vector2 scrollPos;

        [MenuItem("MineCraft/Epic Fight/Matrix Analyzer")]
        public static void ShowWindow()
        {
            GetWindow<EpicFightMatrixAnalyzer>("Matrix Analyzer");
        }

        private void OnGUI()
        {
            GUILayout.Label("Epic Fight Matrix Convention Analyzer", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.LabelField("JSON Path:");
            jsonPath = EditorGUILayout.TextField(jsonPath);

            GUILayout.Space(10);

            if (GUILayout.Button("Analyze Matrix Convention", GUILayout.Height(30)))
            {
                AnalyzeMatrixConvention();
            }

            GUILayout.Space(10);
            GUILayout.Label("Analysis Result:", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            EditorGUILayout.TextArea(analysisResult, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void AnalyzeMatrixConvention()
        {
            if (!File.Exists(jsonPath))
            {
                analysisResult = $"ERROR: File not found: {jsonPath}";
                return;
            }

            try
            {
                string jsonText = File.ReadAllText(jsonPath);
                
                // Parse using SimpleJSON or manual parsing
                var animData = ParseEpicFightJSON(jsonText);

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("=== EPIC FIGHT MATRIX ANALYSIS ===\n");

                // Phân tích joint đầu tiên (Root) để hiểu structure
                if (animData != null && animData.Count > 0)
                {
                    var rootJoint = animData[0];
                    sb.AppendLine($"Joint: {rootJoint.name}");
                    sb.AppendLine($"Keyframes: {rootJoint.times.Count}");
                    sb.AppendLine($"Time values: [{string.Join(", ", rootJoint.times)}]");
                    sb.AppendLine();

                    // Lấy matrix đầu tiên (bind pose hoặc gần bind pose)
                    if (rootJoint.matrices != null && rootJoint.matrices.Count > 0)
                    {
                        float[] m = rootJoint.matrices[0];
                        
                        sb.AppendLine("=== FRAME 0 MATRIX (16 elements) ===");
                        sb.AppendLine($"Raw: [{string.Join(", ", System.Array.ConvertAll(m, x => x.ToString("F6")))}]");
                        sb.AppendLine();

                        // Test cả 2 interpretation: row-major và column-major
                        sb.AppendLine("=== INTERPRETATION TEST ===\n");

                        // Test 1: Row-major (m00,m01,m02,m03, m10,m11,m12,m13, ...)
                        sb.AppendLine("--- ROW-MAJOR INTERPRETATION ---");
                        Matrix4x4 rowMajor = new Matrix4x4();
                        rowMajor.m00 = m[0];  rowMajor.m01 = m[1];  rowMajor.m02 = m[2];  rowMajor.m03 = m[3];
                        rowMajor.m10 = m[4];  rowMajor.m11 = m[5];  rowMajor.m12 = m[6];  rowMajor.m13 = m[7];
                        rowMajor.m20 = m[8];  rowMajor.m21 = m[9];  rowMajor.m22 = m[10]; rowMajor.m23 = m[11];
                        rowMajor.m30 = m[12]; rowMajor.m31 = m[13]; rowMajor.m32 = m[14]; rowMajor.m33 = m[15];

                        sb.AppendLine(MatrixToString(rowMajor));
                        sb.AppendLine($"Translation (m03, m13, m23): ({m[3]:F6}, {m[7]:F6}, {m[11]:F6})");
                        sb.AppendLine($"Determinant: {rowMajor.determinant:F6}");
                        AnalyzeRotationPart(sb, rowMajor, "Row-Major");
                        sb.AppendLine();

                        // Test 2: Column-major (m00,m10,m20,m30, m01,m11,m21,m31, ...)
                        sb.AppendLine("--- COLUMN-MAJOR INTERPRETATION ---");
                        Matrix4x4 colMajor = new Matrix4x4();
                        colMajor.m00 = m[0];  colMajor.m10 = m[1];  colMajor.m20 = m[2];  colMajor.m30 = m[3];
                        colMajor.m01 = m[4];  colMajor.m11 = m[5];  colMajor.m21 = m[6];  colMajor.m31 = m[7];
                        colMajor.m02 = m[8];  colMajor.m12 = m[9];  colMajor.m22 = m[10]; colMajor.m32 = m[11];
                        colMajor.m03 = m[12]; colMajor.m13 = m[13]; colMajor.m23 = m[14]; colMajor.m33 = m[15];

                        sb.AppendLine(MatrixToString(colMajor));
                        sb.AppendLine($"Translation (m03, m13, m23): ({colMajor.m03:F6}, {colMajor.m13:F6}, {colMajor.m23:F6})");
                        sb.AppendLine($"Determinant: {colMajor.determinant:F6}");
                        AnalyzeRotationPart(sb, colMajor, "Column-Major");
                        sb.AppendLine();

                        // Kiểm tra orthogonality để xác định đúng interpretation
                        sb.AppendLine("=== VERIFICATION ===");
                        float rowOrthErr = CheckOrthogonality(rowMajor);
                        float colOrthErr = CheckOrthogonality(colMajor);
                        
                        sb.AppendLine($"Row-major orthogonality error: {rowOrthErr:F6}");
                        sb.AppendLine($"Column-major orthogonality error: {colOrthErr:F6}");
                        
                        if (rowOrthErr < colOrthErr)
                        {
                            sb.AppendLine("\n✓ LIKELY ROW-MAJOR (lower orthogonality error)");
                        }
                        else
                        {
                            sb.AppendLine("\n✓ LIKELY COLUMN-MAJOR (lower orthogonality error)");
                        }
                    }

                    // Phân tích các joints khác
                    sb.AppendLine("\n\n=== ALL JOINTS SUMMARY ===");
                    foreach (var joint in animData)
                    {
                        sb.AppendLine($"- {joint.name}: {joint.times.Count} keyframes");
                    }
                }

                analysisResult = sb.ToString();
            }
            catch (System.Exception e)
            {
                analysisResult = $"ERROR: {e.Message}\n\n{e.StackTrace}";
            }
        }

        // Simple JSON parser for Epic Fight format
        private class JointData
        {
            public string name;
            public List<float> times = new List<float>();
            public List<float[]> matrices = new List<float[]>();
        }

        private List<JointData> ParseEpicFightJSON(string json)
        {
            var result = new List<JointData>();
            
            // Find "animation" array
            int animStart = json.IndexOf("\"animation\"");
            if (animStart < 0) return result;

            int arrayStart = json.IndexOf('[', animStart);
            int depth = 0;
            int objStart = -1;

            for (int i = arrayStart; i < json.Length; i++)
            {
                char c = json[i];
                
                if (c == '{')
                {
                    if (depth == 1) objStart = i; // Start of joint object
                    depth++;
                }
                else if (c == '}')
                {
                    depth--;
                    if (depth == 1 && objStart >= 0)
                    {
                        // Parse one joint object
                        string jointJson = json.Substring(objStart, i - objStart + 1);
                        var joint = ParseJoint(jointJson);
                        if (joint != null) result.Add(joint);
                        objStart = -1;
                    }
                    else if (depth == 0)
                    {
                        break; // End of animation array
                    }
                }
            }

            return result;
        }

        private JointData ParseJoint(string jointJson)
        {
            var joint = new JointData();

            // Parse name
            int nameIdx = jointJson.IndexOf("\"name\"");
            if (nameIdx >= 0)
            {
                int nameStart = jointJson.IndexOf('\"', nameIdx + 6) + 1;
                int nameEnd = jointJson.IndexOf('\"', nameStart);
                joint.name = jointJson.Substring(nameStart, nameEnd - nameStart);
            }

            // Parse time array
            int timeIdx = jointJson.IndexOf("\"time\"");
            if (timeIdx >= 0)
            {
                int timeArrStart = jointJson.IndexOf('[', timeIdx);
                int timeArrEnd = jointJson.IndexOf(']', timeArrStart);
                string timeStr = jointJson.Substring(timeArrStart + 1, timeArrEnd - timeArrStart - 1);
                foreach (string t in timeStr.Split(','))
                {
                    if (float.TryParse(t.Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float time))
                    {
                        joint.times.Add(time);
                    }
                }
            }

            // Parse transform array (array of arrays)
            int transformIdx = jointJson.IndexOf("\"transform\"");
            if (transformIdx >= 0)
            {
                int transformArrStart = jointJson.IndexOf('[', transformIdx);
                int depth = 0;
                int matrixStart = -1;

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
                            float[] matrix = new float[16];
                            string[] values = matrixStr.Split(',');
                            for (int j = 0; j < 16 && j < values.Length; j++)
                            {
                                float.TryParse(values[j].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out matrix[j]);
                            }
                            joint.matrices.Add(matrix);
                            matrixStart = -1;
                        }
                        
                        depth--;
                        if (depth == 0) break;
                    }
                }
            }

            return joint;
        }

        private string MatrixToString(Matrix4x4 m)
        {
            return $"[{m.m00:F4}, {m.m01:F4}, {m.m02:F4}, {m.m03:F4}]\n" +
                   $"[{m.m10:F4}, {m.m11:F4}, {m.m12:F4}, {m.m13:F4}]\n" +
                   $"[{m.m20:F4}, {m.m21:F4}, {m.m22:F4}, {m.m23:F4}]\n" +
                   $"[{m.m30:F4}, {m.m31:F4}, {m.m32:F4}, {m.m33:F4}]";
        }

        private void AnalyzeRotationPart(System.Text.StringBuilder sb, Matrix4x4 m, string label)
        {
            // Extract rotation part (3x3 upper-left)
            Vector3 c0 = new Vector3(m.m00, m.m10, m.m20);
            Vector3 c1 = new Vector3(m.m01, m.m11, m.m21);
            Vector3 c2 = new Vector3(m.m02, m.m12, m.m22);

            sb.AppendLine($"{label} Rotation Columns:");
            sb.AppendLine($"  Col0 magnitude: {c0.magnitude:F6} (should be ~1)");
            sb.AppendLine($"  Col1 magnitude: {c1.magnitude:F6} (should be ~1)");
            sb.AppendLine($"  Col2 magnitude: {c2.magnitude:F6} (should be ~1)");

            // Check handedness via cross product
            Vector3 cross = Vector3.Cross(c0, c1);
            float dotWithC2 = Vector3.Dot(cross, c2);
            sb.AppendLine($"  Cross(Col0, Col1) · Col2: {dotWithC2:F6}");
            if (dotWithC2 > 0.9f)
                sb.AppendLine($"  → Right-handed");
            else if (dotWithC2 < -0.9f)
                sb.AppendLine($"  → Left-handed");
            else
                sb.AppendLine($"  → Not orthogonal or scaled");
        }

        private float CheckOrthogonality(Matrix4x4 m)
        {
            Vector3 c0 = new Vector3(m.m00, m.m10, m.m20).normalized;
            Vector3 c1 = new Vector3(m.m01, m.m11, m.m21).normalized;
            Vector3 c2 = new Vector3(m.m02, m.m12, m.m22).normalized;

            float err = 0f;
            err += Mathf.Abs(Vector3.Dot(c0, c1)); // Should be 0
            err += Mathf.Abs(Vector3.Dot(c1, c2)); // Should be 0
            err += Mathf.Abs(Vector3.Dot(c2, c0)); // Should be 0

            return err;
        }
    }
}
