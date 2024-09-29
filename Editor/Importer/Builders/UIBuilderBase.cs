using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shimmer.PSD2UI
{

    public abstract class UIBuilderBase : ScriptableObject
    {
        [SerializeField] bool _autoNewGO = true;

        public static UIBuilderBase GetBuilder(UINodeType type)
        {
            switch (type)
            {
                case UINodeType.Root: return PSD2UISettings.Instance.defaultUIBuilderRoot;
                case UINodeType.Text: return PSD2UISettings.Instance.defaultUIBuilderText;
                case UINodeType.Image: return PSD2UISettings.Instance.defaultUIBuilderImage;
                case UINodeType.Button: return PSD2UISettings.Instance.defaultUIBuilderButton;
                default: return PSD2UISettings.Instance.defaultUIBuilderGroup;
            }
        }

        public static GameObject Start(UINodeData data, bool isWindow)
        {
            GameObject go = null;

            if (isWindow)
            {
                go = Instantiate(PSD2UISettings.Instance.RootPrefab);
                go.name = data.Name;
                GetBuilder(UINodeType.Root)?.OnProcess(data, go);
            }
            else
            {
                go = NewGameObject(data);
                RecursiveChildren(data, go);
            }

            return go;
        }

        public void Process(UINodeData data)
        {
            if (data.IsSkipped) return;

            if (_autoNewGO)
            {
                var newGO = NewGameObject(data);
                OnProcess(data, newGO);
            }
            else
            {
                OnProcess(data, null); // for prefab type
            }
        }

        protected abstract void OnProcess(UINodeData data, GameObject go);

        static GameObject NewGameObject(UINodeData data)
        {
            var go = new GameObject(data.Name)
            {
                layer = LayerMask.NameToLayer("UI")
            };

            var rectTransform = go.AddComponent<RectTransform>();
            SetRectTransform(data, rectTransform, data.Rect);

            if (data.ParentGO != null) go.transform.SetParent(data.ParentGO.transform, false);
            go.SetActive(data.IsVisible);

            return go;
        }

        protected static void SetRectTransform(UINodeData data, RectTransform rectTransform, Rect rect)
        {
            var center = Vector2.one * 0.5f;
            var pivot = data.Pivot;
            var anchorMin = center;
            var anchorMax = center;
            Vector2 anchorMinPosition = Vector2Extension.LerpUnclamped(data.ParentRect.min, data.ParentRect.max, anchorMin);
            Vector2 anchorMaxPosition = Vector2Extension.LerpUnclamped(data.ParentRect.min, data.ParentRect.max, anchorMax);
            Vector2 anchorSize = anchorMaxPosition - anchorMinPosition;
            // var asize = _parentRect.size * (anchorMax - anchorMin);
            // UnityEngine.Debug.Log($"{asize} {anchorSize}");
            Vector2 anchorReferencePosition = Vector2Extension.LerpUnclamped(anchorMinPosition,
                anchorMaxPosition, pivot);
            Vector2 pivotPosition = Vector2Extension.LerpUnclamped(rect.min, rect.max, pivot);

            rectTransform.pivot = pivot;
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = pivotPosition - anchorReferencePosition;
            Vector2 size = new(data.Width == 0f ? rect.width : data.Width, data.Height == 0f ? rect.height : data.Height);
            rectTransform.sizeDelta = size - anchorSize;

            rectTransform.localScale = data.Scale;
            rectTransform.localEulerAngles = data.Rotate;
            if (data.Rotate != Vector3.zero)
                Debug.Log($"节点有旋转，请检查对比原图，切图旋转之后是否效果正常（或许PS中未将旋转重置为0） @{data.Name}", rectTransform);
        }


        protected static void SetImage(UINodeData data, Image image)
        {
            Sprite importedSprite = GetSprite(data);

            if (importedSprite != null)
            {
                image.sprite = importedSprite;
                bool sliced = false;
                for (int i = 0; i < 4; i++)
                {
                    if (importedSprite.border[i] > 0)
                    {
                        sliced = true;
                        break;
                    }
                }

                image.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
                if (!sliced && data.Width == 0f && data.Height == 0f) image.SetNativeSize();

                // 把导入图集时，sprite的pivot数据，应用到此sprite所在的RectTransform的pivot上，以使其位置正确。再通过SetPivot还原RectTransform值（不移位置）。
                Vector2 pivot = image.rectTransform.pivot;
                image.rectTransform.pivot = new Vector2(importedSprite.pivot.x / importedSprite.rect.width, importedSprite.pivot.y / importedSprite.rect.height);
                image.rectTransform.pivot = pivot;

                foreach (var suffix in PSD2UISettings.Instance.tiledImageTypeSpriteNameSuffixes)
                {
                    if (image.sprite.name.ToLower().Contains(suffix))
                    {
                        if (image.type != Image.Type.Tiled)
                        {
                            image.type = Image.Type.Tiled;
                            Debug.Log($"ImageType of {image.sprite.name} on {image.name} is setting to Tiled.", image);
                        }

                        break;
                    }
                }
            }

            image.color = data.Color;
        }


        protected static Sprite GetSprite(UINodeData data)
        {
            Sprite sprite = null;
            string path = null;

            if (!string.IsNullOrEmpty(data.texturePath))
            {
                path = BuildPath(data.texturePath, PSD2UISettings.Instance.texturePath);
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{path}.png");
                if (sprite == null) sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{path}.jpg");
            }
            else if (!string.IsNullOrEmpty(data.atlasPath))
            {
                path = BuildPath(data.atlasPath, PSD2UISettings.Instance.atlasPath);
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{path}.png");
            }
            else
            {
                path = PSD2UISettings.Instance.defaultSpritePath;
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{path}.png");
            }

            if (sprite == null)
            {
                Debug.LogWarning($"{data.Name} 图资源找不到，图片名：{path} 不存在!");
            }

            return sprite;
        }

        static string BuildPath(string path, string parent)
        {
            var temp = path.Trim().Trim('/', '\\');
            temp = Path.Combine(parent, path);
            return temp.Replace("\\", "/");
        }

        protected static void RecursiveChildren(UINodeData data, GameObject parent)
        {
            for (int i = 0; i < data.Children.Count; i++)
            {
                var child = data.Children[i];
                child.BindParent(data, parent);
                GetBuilder(child.Type)?.Process(child);
            }
        }

        public static void SetStretch(GameObject g)
        {
            var rt = g.GetComponent<RectTransform>();
            if (rt == null)
            {
                rt = g.AddComponent<RectTransform>();
            }

            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition3D = Vector3.zero;
        }
    }
}
