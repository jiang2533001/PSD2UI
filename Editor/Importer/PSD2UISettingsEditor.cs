using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Shimmer.PSD2UI
{
    [CustomEditor(typeof(PSD2UISettings))]
    public class PSD2UISettingsEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            PSD2UISettings settings = (PSD2UISettings)target;

            settings.designWidth = EditorGUILayout.IntField("Design Width", settings.designWidth);
            settings.designHeight = EditorGUILayout.IntField("Design Height", settings.designHeight);

            settings.RootPrefab = EditorGUILayout.ObjectField("Root Prefab", settings.RootPrefab, typeof(GameObject), true) as GameObject;
            if (settings.RootPrefab == null)
            {
                EditorGUILayout.HelpBox("You must have a root prefab", MessageType.Error);
            }

            settings.atlasPath = EditorGUILayout.TextField("Atlas Path", settings.atlasPath);

            if (settings.atlasPath == "")
            {
                EditorGUILayout.HelpBox("You must have a atlas path", MessageType.Error);
            }
            else if (!AssetDatabase.IsValidFolder(settings.atlasPath))
            {
                EditorGUILayout.HelpBox("This path is not valid", MessageType.Error);
            }

            settings.texturePath = EditorGUILayout.TextField("Texture Path", settings.texturePath);
            if (settings.texturePath == "")
            {
                EditorGUILayout.HelpBox("You must have a texture path", MessageType.Error);
            }
            else if (!AssetDatabase.IsValidFolder(settings.texturePath))
            {
                EditorGUILayout.HelpBox("This path is not valid", MessageType.Error);
            }

            settings.defaultSpritePath = EditorGUILayout.TextField("Default Sprite Path", settings.defaultSpritePath);
            /*if (settings.defaultSpritePath == "")
            {
                EditorGUILayout.HelpBox("You must have a default sprite path", MessageType.Error);
            }*/

            settings.fontPath = EditorGUILayout.TextField("Foint Path", settings.fontPath);

            if (settings.fontPath == "")
            {
                EditorGUILayout.HelpBox("You must have a font path", MessageType.Error);
            }
            else if (!AssetDatabase.IsValidFolder(settings.fontPath))
            {
                EditorGUILayout.HelpBox("This path is not valid", MessageType.Error);
            }

            var fontList = serializedObject.FindProperty("normalFonts");
            EditorGUILayout.PropertyField(fontList, new GUIContent("Font List"), true);
          
            if (settings.normalFonts.Count == 0)
            {
                EditorGUILayout.HelpBox("You must have at least one pair", MessageType.Error);
            }

            settings.defaultUIBuilderRoot = EditorGUILayout.ObjectField("Root", settings.defaultUIBuilderRoot, typeof(UIBuilderBase), false) as UIBuilderBase;
            settings.defaultUIBuilderGroup = EditorGUILayout.ObjectField("Group", settings.defaultUIBuilderGroup, typeof(UIBuilderBase), false) as UIBuilderBase;
            settings.defaultUIBuilderText = EditorGUILayout.ObjectField("Text", settings.defaultUIBuilderText, typeof(UIBuilderBase), false) as UIBuilderBase;
            settings.defaultUIBuilderImage = EditorGUILayout.ObjectField("Image", settings.defaultUIBuilderImage, typeof(UIBuilderBase), false) as UIBuilderBase;
            settings.defaultUIBuilderButton = EditorGUILayout.ObjectField("Button", settings.defaultUIBuilderButton, typeof(UIBuilderBase), false) as UIBuilderBase;

            serializedObject.ApplyModifiedProperties();
        }
    }
}
