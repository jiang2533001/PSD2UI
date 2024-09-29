using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ntreev.Library.Psd;
using Ntreev.Library.Psd.Readers.ImageResources;
using Ntreev.Library.Psd.Readers.LayerResources;
using Ntreev.Library.Psd.Structures;

namespace Shimmer.PSD2UI
{
    public enum XAnchorType
    {
        None,
        Left,
        Center,
        Right,
        Stretch
    }

    public enum YAnchorType
    {
        None,
        Bottom,
        Middle,
        Top,
        Stretch
    }

    public enum WidgetType
    {
        None,
        Image,
        Text,
        EmptyGraphic
    }

    public enum UINodeType
    {
        Root,
        Group,
        Image,
        Text,
        Button,
        Prefab,
    }

    [Serializable]
    public class UIBuildRule
    {
        public UINodeType UIType;
        public GameObject UIPrefab; //UI模板
        public string UIBuilder; //UIHelper类型全名
        public string Comment;//注释
    }

    public class PsdParser
    {
        private static readonly XNamespace _aguguNamespace = "http://www.agugu.org/";

        private static readonly XNamespace _rdfNamespace = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

        // pivot 索引对应的 Vector2所在的位置
        // 1 2 3
        // 4 0 5
        // 6 7 8
        private static readonly Vector2[] _pivot = new Vector2[]
        {
        Vector2.one * 0.5f, Vector2.up, new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
        new Vector2(1f, 0.5f), Vector2.zero, new Vector2(0.5f, 0f), new Vector2(1f, 0f)
        };

        private const int DocumentRootMagicLayerId = -1;

        private const string ConfigRootTag = "Config";
        private const string LayersRootTag = "Layers";
        private const string BagTag = "Bag";
        private const string IdTag = "Id";
        private const string PropertiesTag = "Properties";

        public static UINodeData Parse(string psdPath)
        {
            using (var document = PsdDocument.Create(psdPath))
            {
                UINodeData root = GetRoot(psdPath, document);

                foreach (PsdLayer layer in document.Childs)
                {
                    root.Children.Add(_ParsePsdLayerRecursive(root, layer));
                }

                return root;
            }
        }

        private static UINodeData GetRoot(string psdPath, PsdDocument document)
        {
            PsdLayerConfigSet configSet = _ParseConfig(document);
            PsdLayerConfig config = configSet.GetLayerConfig(DocumentRootMagicLayerId);
            var root = new UINodeData()
            {
                Id = DocumentRootMagicLayerId,
                IsVisible = true,
                Name = Path.GetFileName(psdPath),
                IsSkipped = config.GetLayerConfigAsBool(ConfigTag.IsSkipped),

                Rect = new Rect(0, 0, document.Width, document.Height),

                Scale = Vector3.one,
                Rotate = Vector2.zero,
                Width = document.Width,
                Height = document.Height,
            };

            var imageResource = document.ImageResources;
            var resolutionProperty = imageResource["Resolution"] as Reader_ResolutionInfo;
            int horizontalResolution = Convert.ToInt32(resolutionProperty.Value["HorizontalRes"]);
            Debug.Log($"horizontalResolution : {horizontalResolution}");

            root.HorizontalPixelPerInch = horizontalResolution;

            return root;
        }


        private static PsdLayerConfigSet _ParseConfig(PsdDocument document)
        {
            IProperties imageResources = document.ImageResources;
            if (imageResources.Contains("XmpMetadata"))
            {
                var xmpImageResource = imageResources["XmpMetadata"] as Reader_XmpMetadata;
                var xmpValue = xmpImageResource.Value["Xmp"] as string;

                return ParseXmp(xmpValue);
            }
            else
            {
                return new PsdLayerConfigSet();
            }
        }

        public static PsdLayerConfigSet ParseXmp(string xmpString)
        {
            var result = new PsdLayerConfigSet();
            var xmp = XDocument.Parse(xmpString);

            XElement configRoot = xmp.Descendants(_aguguNamespace + ConfigRootTag).FirstOrDefault();
            if (configRoot == null)
            {
                return result;
            }

            XElement layersConfigRoot = configRoot.Descendants(_aguguNamespace + LayersRootTag).FirstOrDefault();
            if (layersConfigRoot == null)
            {
                return result;
            }

            XElement bag = layersConfigRoot.Element(_rdfNamespace + BagTag);
            if (bag == null)
            {
                return result;
            }

            var layerItems = bag.Elements();
            foreach (XElement listItem in layerItems)
            {
                XElement idElement = listItem.Element(_aguguNamespace + IdTag);
                if (idElement == null)
                {
                    continue;
                }

                int layerId = Int32.Parse(idElement.Value);
                var propertyDictionary = new Dictionary<string, string>();

                XElement propertiesRoot = listItem.Element(_aguguNamespace + PropertiesTag);
                if (propertiesRoot == null)
                {
                    continue;
                }

                foreach (XElement layerProperty in propertiesRoot.Elements())
                {
                    string propertyName = layerProperty.Name.LocalName;
                    string propertyValue = layerProperty.Value;

                    propertyDictionary.Add(propertyName, propertyValue);
                }

                result.SetLayerConfig(layerId, new PsdLayerConfig(propertyDictionary));
            }

            return result;
        }

        private static UINodeData _ParsePsdLayerRecursive(UINodeData root, PsdLayer layer)
        {
            LayerData layerData = GetLayerData(layer);

            var UINodeData = new UINodeData();
            UINodeData.Id = (int)layer.Resources["lyid.ID"];
            UINodeData.Name = layer.Name;
            UINodeData.IsVisible = layer.IsVisible;
            UINodeData.IsSkipped = layerData.isSkipped;

            UINodeData.Rotate = new Vector3(layerData.rotateX, layerData.rotateY, -layerData.rotateZ); //美术那边Z正数时为顺时针旋转更符合直觉，与unity相反
            UINodeData.Scale = new Vector3(layerData.scaleX == 0f ? 1f : layerData.scaleX, layerData.scaleY == 0f ? 1f : layerData.scaleY, 1f);
            UINodeData.Pivot = _pivot[layerData.pivot];
            UINodeData.Width = layerData.width;
            UINodeData.Height = layerData.height;
            UINodeData.Rect = new Rect()
            {
                xMin = layer.Left,
                xMax = layer.Right,
                yMin = root.Height - layer.Bottom,
                yMax = root.Height - layer.Top
            };

            var color = Color.white;
            color.a *= layer.Opacity;
            UINodeData.Color = color;

            if (layerData.isSkipped) return new UINodeData { IsSkipped = true }; //忽略跳过的节点,不再解析和保留其数据

            UINodeData.Type = _GetNodeType(layer, layerData);

            if (UINodeData.Type == UINodeType.Prefab)
            {
                UINodeData.prefabPath = layerData.prefab;
                return UINodeData;
            }
            else if (UINodeData.Type == UINodeType.Group)
            {
                var children = new List<UINodeData>();

                foreach (PsdLayer childLayer in layer.Childs)
                {
                    children.Add(_ParsePsdLayerRecursive(root, childLayer));
                }

                UINodeData.Children = children;
                return UINodeData;
            }
            else
            {
                bool hasColorOverlay = _HasColorOverlay(layer, out var newColor);
                if (hasColorOverlay) UINodeData.Color = newColor;

                if (UINodeData.Type == UINodeType.Text) SetTextNode(layer, layerData, hasColorOverlay, UINodeData);
                else if (UINodeData.Type == UINodeType.Image || UINodeData.Type == UINodeType.Button) SetImageNode(layerData, UINodeData);
            }

            return UINodeData;
        }

        private static LayerData GetLayerData(PsdLayer layer)
        {
            var sh = layer.Resources["shmd"] as Reader_shmd;
            var descriptorStructure = ((List<DescriptorStructure>)sh["Items"])[0];
            if (descriptorStructure.TryGetProperty("layerXMP", out string xmp))
            {
                // UnityEngine.Debug.Log($"{layer.Name}:{xmp}");
                if (xmp != "null")
                {
                    try
                    {
                        return JsonUtility.FromJson<LayerData>(xmp);
                    }
                    catch (Exception e)
                    {
                        UnityEngine.Debug.LogError($"{layer.Name}:{xmp}");
                        UnityEngine.Debug.LogError(e);
                    }
                }
            }

            return new LayerData();
        }


        private static UINodeType _GetNodeType(PsdLayer layer, LayerData layerData)
        {
            if (!string.IsNullOrEmpty(layerData.prefab))
            {
                return UINodeType.Prefab;
            }
            else if (_IsGroupLayer(layer) && string.IsNullOrEmpty(layerData.guiPath) && string.IsNullOrEmpty(layerData.texturePath))
            {
                return UINodeType.Group;
            }
            else if (layer.Resources.Contains("TySh"))
            {
                return UINodeType.Text;
            }
            else
            {
                if (layer.Name.StartsWith('@'))
                {
                    return UINodeType.Button;
                }
                else
                {
                    return UINodeType.Image;
                }
            }
        }

        private static bool _IsGroupLayer(PsdLayer layer)
        {
            return layer.SectionType == SectionType.Opend ||
                   layer.SectionType == SectionType.Closed;
        }

        private static bool _HasColorOverlay(PsdLayer layer, out Color color)
        {
            bool hasColorOverlay = false;
            Properties colorOverlay = null;
            color = Color.white;
            if (layer.Resources.TryGetValue(ref colorOverlay, "lfx2.SoFi"))
            {
                hasColorOverlay = colorOverlay.TryGetProperty("enab", out bool enable) && enable;
                if (hasColorOverlay)
                {
                    color = new Color
                    {
                        r = (float)colorOverlay.ToValue<double>("Clr.Rd") / 255,
                        g = (float)colorOverlay.ToValue<double>("Clr.Grn") / 255,
                        b = (float)colorOverlay.ToValue<double>("Clr.Bl") / 255,
                        a = (float)colorOverlay.ToValue<double>("Opct.Value") / 100,
                    };
                }
            }

            return hasColorOverlay;
        }

        private static void SetTextNode(PsdLayer layer, LayerData layerData, bool hasColorOverlay, UINodeData nodeData)
        {
            var engineData = (StructureEngineData)layer.Resources["TySh.Text.EngineData"];
            var engineDict = (Properties)engineData["EngineDict"];

            var srArray = (ArrayList)engineDict["StyleRun.RunArray"];
            var firstRunArrayElement = (Properties)srArray[0];
            var firstStyleSheetData = (Properties)firstRunArrayElement["StyleSheet.StyleSheetData"];

            object fontIndex = 0;
            if (!firstStyleSheetData.TryGetValue(ref fontIndex, "Font"))
            {
                UnityEngine.Debug.LogWarning($"psd raw data has no property: Font @{layer.Name}");
            }

            var bold = false;
            var italic = false;
            firstStyleSheetData.TryGetValue(ref bold, "FauxBold");
            firstStyleSheetData.TryGetValue(ref italic, "FauxItalic");

            var fontSize = _GetFontSizeFromStyleSheetData(firstStyleSheetData);
            if (!hasColorOverlay)
                nodeData.Color = _GetTextColorFromStyleSheetData(firstStyleSheetData);

            var prArray = (ArrayList)engineDict["ParagraphRun.RunArray"];
            var data = (Properties)prArray[0];
            if (!data.TryGetProperty("ParagraphSheet.Properties.Justification", out int justification))
                justification = 0;
            var documentResources = (Properties)engineData["DocumentResources"];
            var fontSet = (ArrayList)documentResources["FontSet"];
            var font = (Properties)fontSet[(int)fontIndex];
            var fontName = (string)font["Name"];

            var tysh = layer.Resources["TySh"] as Reader_TySh;
            var text = (string)tysh["Text.Txt"];
            var scale = ((double[])tysh["Transforms"])[3];
            // var text = (string) layer.Resources["TySh.Text.Txt"];
            //psd中读取到的换行符不对，只会置于行首不会另起行
            text = text.Replace('\r', '\n');


            nodeData.FontSize = (float)(fontSize * scale);
            nodeData.FontName = fontName;
            nodeData.FontMaterial = layerData.fontMaterial;
            nodeData.Text = text;
            nodeData.Justification = justification;
            nodeData.Bold = bold;
            nodeData.Italic = italic;
        }

        private static float _GetFontSizeFromStyleSheetData(Properties styleSheetData)
        {
            // Font size could be omitted TODO: Find official default Value
            if (styleSheetData.Contains("FontSize"))
            {
                return (float)styleSheetData["FontSize"];
            }

            return 42;
        }

        private static Color _GetTextColorFromStyleSheetData(Properties styleSheetData)
        {
            // FillColor also could be omitted
            if (styleSheetData.Contains("FillColor"))
            {
                var fillColor = (Properties)styleSheetData["FillColor"];
                var fillColorValue = (ArrayList)fillColor["Values"];
                //ARGB
                var textColor = new Color((float)fillColorValue[1],
                    (float)fillColorValue[2],
                    (float)fillColorValue[3],
                    (float)fillColorValue[0]);

                return textColor;
            }

            return Color.white;
        }

        private static void SetImageNode(LayerData layerData, UINodeData nodeData)
        {
            if (!string.IsNullOrEmpty(layerData.texturePath))
            {
                nodeData.texturePath = layerData.texturePath;
            }

            if (!string.IsNullOrEmpty(layerData.guiPath))
            {
                nodeData.atlasPath = layerData.guiPath;
            }
        }
    }
}