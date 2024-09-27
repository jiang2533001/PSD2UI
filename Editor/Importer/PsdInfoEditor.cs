using UnityEngine;
using UnityEditor;

namespace Shimmer.PSD2UI
{
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
}