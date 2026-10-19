#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using RGame.RoguelikeKit;

[CustomEditor(typeof(StageManager))]
public class StageManagerEditor : Editor
{
    private float seconds;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Stage 0 Debug Jump", EditorStyles.boldLabel);
        seconds = EditorGUILayout.FloatField("Target seconds", seconds);
        if (GUILayout.Button("Jump runtime to target"))
            ((StageManager)target).EditorJumpToTime(seconds);
        EditorGUILayout.BeginHorizontal();
        foreach (var value in new[] { 180f, 240f, 420f, 660f, 720f })
            if (GUILayout.Button(value.ToString("0"))) ((StageManager)target).EditorJumpToTime(value);
        EditorGUILayout.EndHorizontal();
    }
}
#endif
