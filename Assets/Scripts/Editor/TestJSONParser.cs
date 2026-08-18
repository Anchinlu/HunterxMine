using UnityEngine;
using UnityEditor;
using System.IO;

namespace MineCraftUnity.Editor
{
    /// <summary>
    /// Simple test tool to verify JSON parsing works before full conversion.
    /// </summary>
    public class TestJSONParser : EditorWindow
    {
        [MenuItem("MineCraft/Epic Fight/Test JSON Parser")]
        public static void ShowWindow()
        {
            GetWindow<TestJSONParser>("Test JSON Parser");
        }

        private void OnGUI()
        {
            GUILayout.Label("Test JSON Parser", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Test Parse fist_auto1.json", GUILayout.Height(40)))
            {
                TestParse();
            }
        }

        private void TestParse()
        {
            string path = @"E:\Project game pk\minecraft\Docs\NhiemVu\fist_auto1.json";
            
            if (!File.Exists(path))
            {
                Debug.LogError($"File not found: {path}");
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                Debug.Log($"[Test] Loaded JSON, length: {json.Length} characters");
                
                // Test finding animation array
                int animIdx = json.IndexOf("\"animation\"");
                Debug.Log($"[Test] 'animation' key found at index: {animIdx}");
                
                if (animIdx >= 0)
                {
                    int arrayStart = json.IndexOf('[', animIdx);
                    Debug.Log($"[Test] Animation array '[' at index: {arrayStart}");
                    
                    // Count joints (count opening braces after array start)
                    int depth = 1; // Start at 1 because we're inside the '['
                    int jointCount = 0;
                    
                    for (int i = arrayStart + 1; i < json.Length; i++)
                    {
                        if (json[i] == '{')
                        {
                            if (depth == 1) jointCount++; // Direct child of array
                            depth++;
                        }
                        else if (json[i] == '}')
                        {
                            depth--;
                            if (depth == 0) break;
                        }
                        else if (json[i] == ']' && depth == 1)
                        {
                            break; // End of array
                        }
                    }
                    
                    Debug.Log($"[Test] Found {jointCount} joint objects in animation array");
                }
                
                // Test parsing a simple value with scientific notation
                string testStr = "-5e-06";
                if (float.TryParse(testStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float testFloat))
                {
                    Debug.Log($"[Test] Parsed '{testStr}' as {testFloat} ✓");
                }
                else
                {
                    Debug.LogError($"[Test] Failed to parse '{testStr}' ✗");
                }
                
                // Test extract first joint name
                int firstJoint = json.IndexOf("{", animIdx);
                int nameIdx = json.IndexOf("\"name\"", firstJoint);
                if (nameIdx > 0)
                {
                    int nameStart = json.IndexOf('\"', nameIdx + 6) + 1;
                    int nameEnd = json.IndexOf('\"', nameStart);
                    string firstName = json.Substring(nameStart, nameEnd - nameStart);
                    Debug.Log($"[Test] First joint name: '{firstName}'");
                }
                
                Debug.Log("[Test] ✓ All basic tests passed");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[Test] Exception: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}
