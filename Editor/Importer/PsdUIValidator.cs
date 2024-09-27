#if !CI_MODE
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Shimmer.PSD2UI
{
    public class PsdUIValidator : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string str in importedAssets)
            {
                Validate(str);
            }
        }

        static void Validate(string path)
        {
            if (!path.Contains(PSD2UISettings.Instance.PrefabOutput))
                return;

            var instance = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (instance == null || instance.GetComponent<PsdInfo>() == null)
                return;

            var layers = new List<PsdLayerIdTag>();
            UguiTreeUpdater.GetAllLayers(instance.transform, layers);
            var exists = new Dictionary<int, List<string>>();
            foreach (var layer in layers)
            {
                if (!exists.TryGetValue(layer.LayerId, out var list))
                {
                    list = new List<string>();
                    exists.Add(layer.LayerId, list);
                }

                list.Add(layer.gameObject.name);
            }

            foreach (var data in exists)
            {
                if (data.Value.Count > 1)
                {
                    string names = string.Empty;
                    data.Value.ForEach(n => names += $" \"{n}\" &");
                    names = names.TrimEnd('&');
                    UnityEngine.Debug.LogError($"same psd layer id {data.Key} :{names} @{instance.name}", instance);
                }
            }
        }
    }
}
#endif