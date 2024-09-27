using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;


public class PsdInfo : MonoBehaviour
{
#if UNITY_EDITOR
    public string psdPath;
    public List<LayerName> deletedLayers;
    public Texture2D referenceTexture;
    public GameObject hideGameObject;
    public float height = 2400;

    private GameObject m_referenceTextureGameObject;

    public void CreateReferenceTexture()
    {
        if (hideGameObject != null)
        {
            hideGameObject.gameObject.SetActive(false);
        }

        m_referenceTextureGameObject = new GameObject("ReferenceTexture", typeof(RectTransform), typeof(RawImage));
        m_referenceTextureGameObject.transform.SetParent(transform, false);
        m_referenceTextureGameObject.transform.SetSiblingIndex(0);
        m_referenceTextureGameObject.GetComponent<RawImage>().texture = this.referenceTexture;
        ((RectTransform)m_referenceTextureGameObject.transform).sizeDelta = new Vector2(1080, height);
        ((RectTransform)m_referenceTextureGameObject.transform).anchoredPosition3D = Vector3.zero;
        EditorUtility.SetDirty(this);
    }

    public void ClearReferenceTexture()
    {
        if (hideGameObject != null)
        {
            hideGameObject.gameObject.SetActive(true);
        }

        DestroyImmediate(m_referenceTextureGameObject);
        EditorUtility.SetDirty(this);
    }
#endif
}

[Serializable]
public class LayerName
{
    public int id;
    public string name;
}