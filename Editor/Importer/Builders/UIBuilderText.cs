using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace Shimmer.PSD2UI
{
    [CreateAssetMenu(menuName = "UIBuilder/Text")]
    public class UIBuilderText : UIBuilderBase
    {
        protected override void OnProcess(UINodeData data, GameObject go)
        {
            var text = go.AddComponent<TextMeshProUGUI>(); ;
            text.text = data.Text;
            text.color = data.Color;
            if (data.Bold) text.fontStyle = FontStyles.Bold;
            else if (data.Italic) text.fontStyle = FontStyles.Italic;

            // Photoshop uses 72 points per inch
            int fontSize = (int)(data.FontSize / 72 * data.HorizontalPixelPerInch);
            TMP_FontAsset font = null;
            string fontName = PSD2UISettings.Instance.GetFont(data.FontName, fontSize);
            if (!string.IsNullOrEmpty(fontName))
            {
                font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{PSD2UISettings.Instance.fontPath}/{fontName}.asset");
            }

            if (font == null || string.IsNullOrEmpty(fontName))
                Debug.LogWarning($"字体找不到: {data.FontName} @{go.name}", go);
            else
                text.font = font;
            text.fontSize = fontSize;

            //TextMeshProUGUI组件在添加时会自动修改RectTransform的大小，需要重新设置
            SetRectTransform(data, (RectTransform)go.transform, data.Rect);
            FixTextRTSize(text, data);

            text.enableWordWrapping = text.text.Split('\n').Length > 1;

            switch (data.Justification)
            {
                case 0:
                    text.alignment = TextAlignmentOptions.Left;
                    break;
                case 1:
                    text.alignment = TextAlignmentOptions.Right;
                    break;
                case 2:
                    text.alignment = TextAlignmentOptions.Center;
                    break;
                default:
                    Debug.LogWarning($"{data.Text} : 文本对齐方式不是左中右任何一种? @{go.name}", go);
                    break;
            }

            Debug.Log($"ui text 名称 : {go.name}");
        }

        void FixTextRTSize(TextMeshProUGUI text, UINodeData data)
        {
            var inactiveObjs = new List<RectTransform>();
            if (!text.gameObject.activeInHierarchy)
                inactiveObjs = text.GetComponentsInParent<RectTransform>(true).Where(r => !r.gameObject.activeSelf).ToList();
            //需要在可见的情况下text.GetPreferredValues才能得到正确的值
            foreach (var obj in inactiveObjs)
                obj.gameObject.SetActive(true);
            var rt = text.GetComponent<RectTransform>();
            //因字体文件内的英文和数字被替换了，宽度需要加一点才能显示全而不被换行
            var value = rt.sizeDelta;
            value.x += 50;
            //rt.SetSizeDeltaX(rt.sizeDelta.x + 50);
            //得到文本实际的宽度
            Vector2 vector2 = text.GetPreferredValues();
            rt.sizeDelta = vector2;
            //rt.SetSizeDelta(vector2.x, vector2.y);

            //字体材质也需要在激活状态下才能设置成功
            SetFontMaterial(text, data);

            //恢复激活状态
            foreach (var obj in inactiveObjs)
                obj.gameObject.SetActive(false);
        }

        void SetFontMaterial(TextMeshProUGUI text, UINodeData data)
        {
            int fontSize = (int)(data.FontSize / 72 * data.HorizontalPixelPerInch);
            string fontName = PSD2UISettings.Instance.GetFont(data.FontName, fontSize);

            if (!string.IsNullOrEmpty(data.FontMaterial) && !string.IsNullOrEmpty(fontName))
            {
                string matName = PSD2UISettings.Instance.GetFontMaterial(fontName, data.FontMaterial);

                Material mat = null;

                if (!string.IsNullOrEmpty(matName))
                    mat = AssetDatabase.LoadAssetAtPath<Material>($"{PSD2UISettings.Instance.fontPath}/Materials/{matName}.mat");

                if (mat != null)
                    text.fontMaterial = mat;
                else
                    Debug.LogWarning($"字体材质（描边或投影等效果）找不到: {data.FontMaterial} @{text.name}", text);
            }
        }
    }
}
