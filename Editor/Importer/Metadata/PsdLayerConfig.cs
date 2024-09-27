using System;
using System.Collections.Generic;

namespace Shimmer.PSD2UI
{
    public class ConfigTag
    {
        public const string None = "none";
        public const string ScaleX = "scaleX";
        public const string ScaleY = "scaleY";
        public const string RotateX = "rotateX";
        public const string RotateY = "rotateY";
        public const string RotateZ = "rotateZ";
        public const string Pivot = "pivot";
        public const string ImageGuiPath = "imageGuiPath";
        public const string ImageTexturePath = "imageTexturePath";
        public const string ImageMainPath = "imageMainPath";
        public const string ImageSubPath = "imageSubPath";
        public const string ImageWidth = "imageWidth";
        public const string ImageHeight = "imageHeight";
        public const string Prefab = "prefab";

        public const string IsSkipped = "isSkipped";
        public const string WidgetTypePropertyTag = "widgetType";
        public const string XAnchorPropertyTag = "xAnchor";
        public const string YAnchorPropertyTag = "yAnchor";
        public const string XPivotPropertyTag = "xPivot";
        public const string YPivotPropertyTag = "yPivot";
        public const string FontMaterial = "fontMaterial";
    }

    public class PsdLayerConfig
    {
        private readonly Dictionary<string, string> _config = new Dictionary<string, string>();

        public PsdLayerConfig() { }
        public PsdLayerConfig(Dictionary<string, string> config)
        {
            _config = config;
        }

        public string GetValueOrDefault(string tag)
        {
            return _config.GetValueOrDefault(tag);
        }

        public bool GetLayerConfigAsBool(string tag)
        {
            string tagValue = _config.GetValueOrDefault(tag);
            return string.Equals(tagValue, "true", StringComparison.OrdinalIgnoreCase);
        }

        public float GetLayerConfigAsFloat(string tag, float defaultValue)
        {
            string tagValue = _config.GetValueOrDefault(tag);
            return !string.IsNullOrEmpty(tagValue) ? float.Parse(tagValue) : defaultValue;
        }

        public WidgetType GetLayerConfigAsWidgetType(string tag)
        {
            string taggedStringValue = GetValueOrDefault(tag);
            switch (taggedStringValue)
            {
                case "image": return WidgetType.Image;
                case "text": return WidgetType.Text;
                case "empty": return WidgetType.EmptyGraphic;
                default: return WidgetType.None;
            }
        }

        public XAnchorType GetLayerConfigAsXAnchorType(string tag)
        {
            string taggedStringValue = GetValueOrDefault(tag);
            switch (taggedStringValue)
            {
                case "left": return XAnchorType.Left;
                case "center": return XAnchorType.Center;
                case "right": return XAnchorType.Right;
                case "stretch": return XAnchorType.Stretch;
                default: return XAnchorType.None;
            }
        }

        public YAnchorType GetLayerConfigAsYAnchorType(string tag)
        {
            string taggedStringValue = GetValueOrDefault(tag);
            switch (taggedStringValue)
            {
                case "top": return YAnchorType.Top;
                case "middle": return YAnchorType.Middle;
                case "bottom": return YAnchorType.Bottom;
                case "stretch": return YAnchorType.Stretch;
                default: return YAnchorType.None;
            }
        }
    }
}