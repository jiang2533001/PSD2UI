using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Scripting;

public class PsdLayerIdTag : MonoBehaviour
{
    public int LayerId;

#if UNITY_EDITOR
    [ContextMenu("Remove Layer ID")]
    public void RemoveSelf()
    {
        Remove(false);
    }

    [ContextMenu("Remove Layer ID Include Children")]
    public void RemoveAll()
    {
        Remove(true);
    }

    public void Remove(bool all)
    {
        var info = GetComponentInParent<PsdInfo>();
        if (info != null)
        {
            if (info.gameObject.activeInHierarchy)
                info.StartCoroutine(RemoveID(info, all ? GetComponentsInChildren<PsdLayerIdTag>(true) : new[] { this }));
            else
                UnityEngine.Debug.LogWarning($"{info.name} must be activeInHierarchy!");
        }
    }

    IEnumerator RemoveID(PsdInfo info, PsdLayerIdTag[] layers)
    {
        yield return null;
        Undo.RecordObject(info, "Remove Layer ID");
        foreach (var deleted in layers)
        {
            if (info.deletedLayers.FindIndex(l => l.id == deleted.LayerId) == -1)
            {
                info.deletedLayers.Add(new LayerName { id = deleted.LayerId, name = deleted.name });
            }

            Undo.DestroyObjectImmediate(deleted);
        }
    }
#endif
}