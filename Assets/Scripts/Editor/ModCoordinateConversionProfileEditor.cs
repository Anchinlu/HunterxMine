using UnityEditor;
using UnityEngine;
using MineCraftUnity.Player.Combat;

namespace MineCraftUnity.Editor
{
    [CustomEditor(typeof(ModCoordinateConversionProfile))]
    public class ModCoordinateConversionProfileEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var presetProp = serializedObject.FindProperty("Preset");
            var advancedProp = serializedObject.FindProperty("AdvancedMode");
            
            EditorGUILayout.PropertyField(presetProp);
            EditorGUILayout.PropertyField(advancedProp);
            
            EditorGUILayout.Space();
            
            GUI.enabled = advancedProp.boolValue;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SourceRightAxis"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SourceUpAxis"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("SourceForwardAxis"));
            GUI.enabled = true;
            
            EditorGUILayout.Space();
            
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ExpectedHandedness"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("TranslationScale"));
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}
