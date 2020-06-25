using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(WallSegment)), CanEditMultipleObjects]
public class WallSegmentEditor : Editor
{
    SerializedProperty wallLength;
    SerializedProperty wallStart;
    SerializedProperty wallHeight;

    void OnEnable()
    {
        // Setup the SerializedProperties.
        wallLength = serializedObject.FindProperty("WallLength");
        wallStart = serializedObject.FindProperty("WallStart");
        wallHeight = serializedObject.FindProperty("WallHeight");
        //wallStart = serializedObject.FindProperty("armor");
    }
    public override void OnInspectorGUI()
    {
        // Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
        serializedObject.Update();

        // Show the custom GUI controls.
        EditorGUILayout.Slider(wallLength, 1, 100, new GUIContent("Wall Length"));
        EditorGUILayout.Slider(wallHeight, 2, 5, new GUIContent("Wall Height"));

        EditorGUILayout.PropertyField(wallStart);


       

  
        // Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
        serializedObject.ApplyModifiedProperties();
    }
}