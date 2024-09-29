using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Shimmer.PSD2UI
{
    [CreateAssetMenu(fileName = "PSD2UI Settings")]
    public class PSD2UISettings : ScriptableObject
    {
        private static PSD2UISettings _instance;
        public static PSD2UISettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    var guid = AssetDatabase.FindAssets("t:PSD2UISettings").FirstOrDefault();
                    if (guid == null)
                    {
                        Debug.LogWarning($"Create new PSD2UISettings.asset");

                        _instance = CreateSettingData<PSD2UISettings>();

                        _instance.defaultUIBuilderRoot = CreateUIBuilder<UIBuilderRoot>("Root");
                        _instance.defaultUIBuilderGroup = CreateUIBuilder<UIBuilderGroup>("Group");
                        _instance.defaultUIBuilderText = CreateUIBuilder<UIBuilderText>("Text");
                        _instance.defaultUIBuilderImage = CreateUIBuilder<UIBuilderImage>("Image");
                        _instance.defaultUIBuilderButton = CreateUIBuilder<UIBuilderButton>("Button");

                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        return _instance;
                    }

                    _instance = AssetDatabase.LoadAssetAtPath<PSD2UISettings>(AssetDatabase.GUIDToAssetPath(guid));
                }

                return _instance;
            }
        }

        [Header("必填")]
        public int designWidth = 1080;
        public int designHeight = 1920;
        public GameObject RootPrefab;
        public string atlasPath = "Assets/Resources/UI/Atlas";
        public string texturePath = "Assets/Resources/UI/Texture";
        public string defaultSpritePath;
        public string fontPath = "Assets/Resources/Font";
        [HideInInspector]
        public string prefabOutputPath = "Assets/Resources/UI/Prefabs";
        public List<FontPair> normalFonts;
        public UIBuilderBase defaultUIBuilderRoot;
        public UIBuilderBase defaultUIBuilderGroup;
        public UIBuilderBase defaultUIBuilderText;
        public UIBuilderBase defaultUIBuilderImage;
        public UIBuilderBase defaultUIBuilderButton;

        [Header("选填")]
        public List<FontPair> hdFonts;
        public List<string> fontMaterials;
        public List<string> tiledImageTypeSpriteNameSuffixes;

        public string PsdInput
        {
            get
            {
                if (string.IsNullOrEmpty(m_psdInput))
                {
                    var d = new DirectoryInfo(Directory.GetCurrentDirectory());
                    m_psdInput = d.Parent.FullName.Replace("\\", "/");
                }

                return m_psdInput;
            }
        }

        public string PrefabOutput =>
            // if (string.IsNullOrEmpty(prefabOutput))
            // {
            //     return "Assets/AssetBundle/UI/Prefab/Window";
            // }
            prefabOutputPath;

        string m_psdInput;

        public string GetPsdPath(string psd)
        {
            return Path.Combine(PsdInput, psd);
        }

        public string GetFontMaterial(string fontName, string effectName)
        {
            if (fontMaterials.Find(f => string.Equals(f, effectName)) != null)
                return fontName + "_" + effectName;
            return null;
        }

        public string GetFont(string psFont, int fontSize)
        {
            string ret;
            if (fontSize >= 60 && hdFonts.Count > 0)
            {
                ret = FindFont(hdFonts, psFont);

                if (string.IsNullOrEmpty(ret))
                {
                    Debug.LogError($"PSD2UISettings: cant find matching font from PSD def in hdFonts:\"{psFont}\" size:{fontSize}");
                }

                return ret;
            }

            ret = FindFont(normalFonts, psFont);
            if (string.IsNullOrEmpty(ret))
            {
                Debug.LogError($"PSD2UISettings: cant find matching font from PSD def in normalFonts:\"{psFont}\" size:{fontSize}");
            }

            return ret;
        }

        string FindFont(List<FontPair> pairs, string font)
        {
            var data = pairs.Find(p => p.psFont == font);
            if (data != null)
                return data.unityFont;

            return null;
        }


        static T CreateSettingData<T>(string folder = "") where T : ScriptableObject
        {
            var settingType = typeof(T);
            var setting = ScriptableObject.CreateInstance<T>();

            var path = "Assets";
            if (folder != "")
            {
                path = $"Assets/{folder}";
                if (!AssetDatabase.IsValidFolder(path))
                    AssetDatabase.CreateFolder("Assets", folder);
            }

            string filePath = $"{path}/{settingType.Name}.asset";
            AssetDatabase.CreateAsset(setting, filePath);

            return setting;
        }

        static T CreateUIBuilder<T>(string name) where T : UIBuilderBase
        { 
            var target = ScriptableObject.CreateInstance<T>();
            target.name = name;
            AssetDatabase.AddObjectToAsset(target, _instance);

            return target;
        }
    }

    [Serializable]
    public class FontPair
    {
        public string psFont;
        public string unityFont;
    }
}