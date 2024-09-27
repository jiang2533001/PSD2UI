using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NOAH.PSD2UI
{
    public class UINodeData
    {
        public int Id;
        public string Name;
        public bool IsVisible;
        public bool IsSkipped;

        public Vector2 Pivot;
        public XAnchorType XAnchor;
        public YAnchorType YAnchor;
        public Rect Rect;
        public int HorizontalPixelPerInch;

        public Vector3 Scale;
        public Vector3 Rotate;
        public float Width;
        public float Height;
        public Color Color;

        public UINodeType Type;

        public string texturePath;
        public string atlasPath;
        public string prefabPath;

        public float FontSize;
        public string FontName;
        public string FontMaterial;
        public string Text;
        public int Justification;
        public bool Bold;
        public bool Italic;

        public Rect ParentRect;
        public GameObject ParentGO;

        public List<UINodeData> Children = new List<UINodeData>();

        public void BindParent(UINodeData parent, GameObject go)
        { 
            ParentRect = parent.Rect;
            HorizontalPixelPerInch = parent.HorizontalPixelPerInch;
            ParentGO = go;
        }
    }
}
