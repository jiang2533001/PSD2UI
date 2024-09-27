using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

namespace NOAH.PSD2UI
{
    public class PsdImporter
    {
        [MenuItem("Tools/PSD2UI/To Widget From Selection", false, 100)]
        public static void ToWidgetFromSelection()
        {
            ImportSelection(false);
        }

        [MenuItem("Tools/PSD2UI/To Widget From External...", false, 100)]
        public static void ToWidgetFromExternal()
        {
            ImportExternal(false);
        }

        [MenuItem("Tools/PSD2UI/Preview Widget From External...", false, 100)]
        public static void PreviewWidgetFromExternal()
        {
            ImportExternal(false, false);
        }

        [MenuItem("Tools/PSD2UI/To Window From Selection", false, 200)]
        public static void ToWindowFromSelection()
        {
            ImportSelection(true);
        }

        [MenuItem("Tools/PSD2UI/To Window From External...", false, 200)]
        public static void ToWindowFromExternal()
        {
            ImportExternal(true);
        }

        [MenuItem("Tools/PSD2UI/Preview Window From External...", false, 200)]
        public static void PreviewWindowFromExternal()
        {
            ImportExternal(true, false);
        }

        //[MenuItem("Tools/PSD2UI/Update Prefab", false, 300)]
        public static void UpdatePrefab()
        {
            foreach (var selectedObject in Selection.gameObjects)
            {
                var info = selectedObject.GetComponent<PsdInfo>();
                if (info != null)
                {
                    var psdPath = PSD2UISettings.Instance.GetPsdPath(info.psdPath);
                    if (!File.Exists(psdPath))
                    {
                        UnityEngine.Debug.LogWarning($"{selectedObject.name} prefab related psd file {info.psdPath} doesn't exist!");
                        return;
                    }

                    //bool isWindow = selectedObject.GetComponent<UIWindow>() != null;
                    ImportSelection(true, psdPath, AssetDatabase.GetAssetPath(selectedObject));
                }
                else
                {
                    UnityEngine.Debug.LogWarning("selected object is not a psd imported prefab: " + selectedObject);
                }
            }
        }

        [MenuItem("Tools/PSD2UI/Ping Setting", false, 50)]
        public static void PingPSD2UISetting()
        {
            Selection.activeObject = PSD2UISettings.Instance;
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        public static void Sanitize()
        {
            foreach (var path in Directory.GetFiles(PSD2UISettings.Instance.PrefabOutput, "*.prefab", SearchOption.AllDirectories))
            {
                // var prefab = PrefabUtility.LoadPrefabContents(path);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    bool changed = false;

                    var psdInfo = prefab.GetComponent<PsdInfo>();
                    if (psdInfo != null)
                    {
                        changed = true;
                        UnityEngine.Object.DestroyImmediate(psdInfo, true);
                    }

                    if (changed)
                    {
                        // PrefabUtility.SaveAsPrefabAsset(prefab, path);
                    }
                    // PrefabUtility.UnloadPrefabContents(prefab);
                }
            }

            AssetDatabase.SaveAssets();
        }

        public static void ImportExternal(bool isWindow, bool savePrefab = true)
        {
            string psdPath = EditorUtility.OpenFilePanel("选择PSD文件", "", "psd");
            ImportSelection(isWindow, psdPath, null, savePrefab);
        }

        public static GameObject ImportSelection(bool isWindow, string psdPath = null, string prefabPath = null, bool savePrefab = true)
        {
            if (string.IsNullOrEmpty(psdPath))
                psdPath = _GetSelectedPsdPath();

            if (!string.IsNullOrEmpty(psdPath))
            {
                psdPath = psdPath.Replace("\\", "/");
                return ImportPsdAsPrefab(isWindow, psdPath, prefabPath, savePrefab);
            }

            return null;
        }

        private static string _GetSelectedPsdPath()
        {
            UnityEngine.Object selectedObject = Selection.activeObject;
            string selectedObjectPath = AssetDatabase.GetAssetPath(selectedObject);

            string fileExtension = Path.GetExtension(selectedObjectPath);
            bool isPsdFile = string.Equals(fileExtension, ".psd",
                StringComparison.OrdinalIgnoreCase);

            if (!isPsdFile)
            {
                UnityEngine.Debug.LogWarning("Selected Asset is not a PSD file");
                return string.Empty;
            }

            return selectedObjectPath;
        }

        private static Transform _CreateCanvas()
        {
            var canvasGameObject = new GameObject("PSD2UI_Preview");

            var canvas = canvasGameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var canvasScaler = canvasGameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.referenceResolution = new Vector2(PSD2UISettings.Instance.designWidth, PSD2UISettings.Instance.designHeight);
            canvasScaler.matchWidthOrHeight = 1;

            canvasGameObject.AddComponent<GraphicRaycaster>();

            return canvasGameObject.transform;
        }

        public static GameObject ImportPsdAsPrefab(bool isWindow, string psdPath, string prefabPath = null, bool savePrefab = true)
        {
            UINodeData UINodeRoot = PsdParser.Parse(psdPath); // PSD的解析
            GameObject UIGameObject =  UIBuilderBase.Start(UINodeRoot, isWindow); // Prefab的构建   
            //uiGameObject.GetComponent<PsdInfo>().psdPath = psdPath.Contains(Settings.PsdInput) ? psdPath.Substring(Settings.PsdInput.Length + 1) : psdPath;

            if (string.IsNullOrEmpty(prefabPath)) prefabPath = _GetImportedPrefabSavePath(psdPath);

            _SavePrefab(prefabPath, UIGameObject, savePrefab);

            return UIGameObject;
        }

        private static void _SavePrefab(string prefabPath, GameObject uiGameObject, bool savePrefab)
        {
            var canvas = _CreateCanvas();
            uiGameObject.transform.SetParent(canvas, false);
            //不知为啥, amg中节点的anchor会自动修改,强制再设置一次
            if (uiGameObject.GetComponent<Canvas>() != null)
                UIBuilderBase.SetStretch(uiGameObject);

            if (!savePrefab)
                return;

            var prefabObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabObject != null)
            {
                var prefabGameObject = PrefabUtility.InstantiatePrefab(prefabObject, canvas) as GameObject;
                // UguiTreeMigrator.MigrateAppliedPrefabModification(prefabGameObject, uiGameObject);
                UguiTreeUpdater.UpdateUI(uiGameObject, prefabGameObject);
                // AssetAdapterUpdator.Update(prefabGameObject);
                PrefabUtility.ApplyPrefabInstance(prefabGameObject, InteractionMode.AutomatedAction);
                var value = uiGameObject.transform.localPosition;
                value.y = PSD2UISettings.Instance.designHeight;

                //uiGameObject.transform.SetLocalY(Settings.designHeight);
            }
            else
            {
                // AssetAdapterUpdator.Update(uiGameObject);
                //不知为啥, amg中直接保存prefab会自动修改根节点的缩放为0,anchor也不对,需要延迟一帧再设置
                // PrefabUtility.SaveAsPrefabAssetAndConnect(uiGameObject, prefabPath, InteractionMode.AutomatedAction);
                // EditorCoroutineUtility.StartCoroutineOwnerless(SavePrefab(uiGameObject, prefabPath));
            }
        }

        static System.Collections.IEnumerator SavePrefab(GameObject gameObject, string psdPath)
        {
            yield return null;
            PrefabUtility.SaveAsPrefabAssetAndConnect(gameObject, psdPath, InteractionMode.AutomatedAction);
        }

        private static string _GetImportedPrefabSavePath(string psdPath)
        {
            var prefabPath = string.Format("{0}.prefab", Path.GetFileNameWithoutExtension(psdPath));
            prefabPath = Path.Combine(PSD2UISettings.Instance.PrefabOutput, prefabPath);

            return prefabPath;
        }
    }
}