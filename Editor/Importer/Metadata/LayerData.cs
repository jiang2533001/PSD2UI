using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class LayerData
{
    public string version;
    public float scaleX;
    public float scaleY;
    public float rotateX;
    public float rotateY;

    public float rotateZ;

    // 1 2 3
    // 4 0 5
    // 6 7 8
    public int pivot; // 取值参考注释，居中为默认值0
    public float width;
    public float height;
    public string texturePath;
    public string guiPath;
    public bool isSkipped;
    public string prefab;
    public string fontMaterial;
}
