using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PsdInfo))]
public class PsdInfoEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PsdInfo info = target as PsdInfo;

        if (GUILayout.Button("Create Reference Texture"))
        {
            info.CreateReferenceTexture();
        }

        if (GUILayout.Button("Clear Reference Texture"))
        { 
            info.ClearReferenceTexture();
        }
    }
}