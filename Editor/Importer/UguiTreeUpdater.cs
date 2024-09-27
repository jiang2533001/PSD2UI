using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Shimmer.PSD2UI
{
    public class UguiTreeUpdater
    {
        public static void UpdateUI(GameObject source, GameObject target)
        {
            var sourceComponents = new List<PsdLayerIdTag>();
            GetAllLayers(source.transform, sourceComponents);
            var sources = new Dictionary<int, Transform>();
            sourceComponents.ForEach(s =>
            {
                // if (!sources.ContainsKey(s.LayerId))
                sources.Add(s.LayerId, s.transform);
            });

            var targetComponents = new List<PsdLayerIdTag>();
            GetAllLayers(target.transform, targetComponents);
            var targets = new Dictionary<int, Transform>();
            targetComponents.ForEach(s =>
            {
                if (targets.ContainsKey(s.LayerId))
                    UnityEngine.Debug.LogError($"{s.gameObject.name} has duplicate layer id {s.LayerId}", s.gameObject);
                else
                    targets.Add(s.LayerId, s.transform);
            });

            var handledTargetLayers = new Dictionary<int, Transform>();
            UpdatePsdLayer(target.transform, sources, handledTargetLayers);

            var deletedLayer = new HashSet<int>();
            var config = target.GetComponent<PsdInfo>();
            if (config != null)
                config.deletedLayers.ForEach(l => deletedLayer.Add(l.id));

            List<PsdLayerIdTag> nowLayers = new List<PsdLayerIdTag>();
            foreach (var remain in sources)
            {
                if (deletedLayer.Contains(remain.Key))
                {
                    remain.Value.name = $"[Deleted]{remain.Value.name}";
                    continue;
                }

                Transform trans = remain.Value;
                var tag = trans.parent.GetComponent<PsdLayerIdTag>();
                int parentId = 0;
                if (tag != null)
                    parentId = tag.LayerId;
                handledTargetLayers.TryGetValue(parentId, out var p);
                if (p == null)
                {
                    nowLayers.Clear();
                    target.GetComponentsInChildren(true, nowLayers);
                    var layer = nowLayers.Find(l => l.LayerId == parentId);
                    if (layer != null)
                        p = layer.transform;
                }

                if (p == null)
                    p = target.transform;
                Transform newTrans = Object.Instantiate(trans, p, false);
                newTrans.name = trans.name;
                newTrans.SetSiblingIndex(trans.GetSiblingIndex());
                trans.name = $"[New]{trans.name}";
                for (var i = newTrans.childCount - 1; i >= 0; --i)
                {
                    Object.DestroyImmediate(newTrans.GetChild(i));
                }
            }

            foreach (var layer in targets)
            {
                if (handledTargetLayers.ContainsKey(layer.Key))
                {
                    continue;
                }

                Transform trans = layer.Value;
                Transform newTrans = Object.Instantiate(trans, source.transform, false);
                newTrans.name = trans.name;
                newTrans.name = $"[Invalid]{trans.name}";
                newTrans.gameObject.SetActive(false);
            }
        }

        public static void GetAllLayers(Transform transform, List<PsdLayerIdTag> layers)
        {
            bool isNestedPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(transform.gameObject);
            //在Prefab保存时OnPostprocess处理中嵌套Prefab调用IsOutermostPrefabInstanceRoot会返回true（外层节点不识别为处于Prefab中而是普通GameObject）,
            //这里辅助是否为第一个节点来判断是否为子Prefab
            if (isNestedPrefab && (transform.parent == null || transform.parent.name != "PSD2UI_Preview"))
            {
                var tag = GetLayerAddedToPrefab(transform);
                if (tag)
                    layers.Add(tag);
                return;
            }

            var layer = transform.GetComponent<PsdLayerIdTag>();
            if (layer != null)
                layers.Add(layer);

            foreach (Transform child in transform)
            {
                GetAllLayers(child, layers);
            }
        }

        static PsdLayerIdTag GetLayerAddedToPrefab(Transform transform)
        {
            var tags = transform.GetComponents<PsdLayerIdTag>();
            if (tags.Length > 2) UnityEngine.Debug.LogWarning("there are more than 2 PsdLayerIdTag!", transform);
            return tags.FirstOrDefault(PrefabUtility.IsAddedComponentOverride);
        }

        static void UpdatePsdLayer(Transform transform, Dictionary<int, Transform> sources,
            Dictionary<int, Transform> allTargetLayers)
        {
            bool isNestedPrefab = PrefabUtility.IsAnyPrefabInstanceRoot(transform.gameObject) &&
                                  !PrefabUtility.IsOutermostPrefabInstanceRoot(transform.gameObject);
            if (isNestedPrefab)
            {
                var tags = transform.GetComponents<PsdLayerIdTag>();
                foreach (PsdLayerIdTag tag in tags)
                {
                    if (sources.TryGetValue(tag.LayerId, out var s) &&
                        PrefabUtility.IsAnyPrefabInstanceRoot(s.gameObject))
                    {
                        transform = UpdateNestedPrefab(s, tag);
                        break;
                    }
                }

                var coms = transform.GetComponentsInChildren<PsdLayerIdTag>(true);
                foreach (var item in coms)
                {
                    if (sources.ContainsKey(item.LayerId))
                        sources.Remove(item.LayerId);
                }

                return;
            }

            var layer = transform.GetComponent<PsdLayerIdTag>();
            if (layer != null)
            {
                if (sources.TryGetValue(layer.LayerId, out var s))
                {
                    Update(s, transform);
                    if (s.name != transform.name)
                        s.name = $"{transform.name}\"{s.name}\"";
                    if (allTargetLayers.ContainsKey(layer.LayerId))
                        UnityEngine.Debug.LogError($"{transform.name} has duplicate layer id {layer.LayerId}",
                            transform);
                    else
                        allTargetLayers.Add(layer.LayerId, transform);
                    sources.Remove(layer.LayerId);
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"{transform.name}节点在PS里被删掉了，旧UI上或许也应该删掉", transform);
                }
            }

            foreach (Transform child in transform)
            {
                UpdatePsdLayer(child, sources, allTargetLayers);
            }
        }

        static void Update(Transform source, Transform target)
        {
            UpdateText(source.GetComponent<TextMeshProUGUI>(), target.GetComponent<TextMeshProUGUI>());
            UpdateComponent<Image>(source, target);
            UpdateComponent<CanvasGroup>(source, target);
            UpdateRectTransform(source.GetComponent<RectTransform>(), target.GetComponent<RectTransform>());
        }

        static void UpdateRectTransform(RectTransform source, RectTransform target)
        {
            var pivot = target.pivot;
            var anchorMin = target.anchorMin;
            var anchorMax = target.anchorMax;

            UnityEditorInternal.ComponentUtility.CopyComponent(source);
            UnityEditorInternal.ComponentUtility.PasteComponentValues(target);
            target.position = source.position;

            var rtEditor = typeof(Editor).Assembly.GetType("UnityEditor.RectTransformEditor");
            var setAnchorSmart = rtEditor.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "SetAnchorSmart" && m.GetParameters().Length == 6);
            setAnchorSmart.Invoke(null, new object[] { target, anchorMin.x, 0, false, true, true });
            setAnchorSmart.Invoke(null, new object[] { target, anchorMin.y, 1, false, true, true });
            setAnchorSmart.Invoke(null, new object[] { target, anchorMax.x, 0, true, true, true });
            setAnchorSmart.Invoke(null, new object[] { target, anchorMax.y, 1, true, true, true });

            var setPivotSmart = rtEditor.GetMethod("SetPivotSmart");
            setPivotSmart.Invoke(null, new object[] { target, pivot.x, 0, true, false });
            setPivotSmart.Invoke(null, new object[] { target, pivot.y, 1, true, false });

            var position = target.anchoredPosition;
            target.anchoredPosition3D =
                new Vector3((float)Math.Round(position.x, 1), (float)Math.Round(position.y, 1), 0f);
        }

        static void UpdateComponent<T>(Transform source, Transform target) where T : Component
        {
            var sourceComp = source.GetComponent<T>();
            if (sourceComp == null)
                return;

            var targetComp = target.GetComponent<T>();
            if (targetComp == null)
            {
                targetComp = target.gameObject.AddComponent<T>();
            }

            UnityEditorInternal.ComponentUtility.CopyComponent(sourceComp);
            UnityEditorInternal.ComponentUtility.PasteComponentValues(targetComp);
        }

        static void UpdateText(TextMeshProUGUI source, TextMeshProUGUI target)
        {
            if (source == null)
                return;
            bool wrap = target.enableWordWrapping;
            // int align = (int)target.alignment & 0xFF00;
            var align = target.alignment;
            UnityEditorInternal.ComponentUtility.CopyComponent(source);
            UnityEditorInternal.ComponentUtility.PasteComponentValues(target);
            target.enableWordWrapping = wrap;
            // 对齐方式在psd中拿到的可能是错的（图上显示的左对齐实际数据拿到的是右对齐，左右相反），故合并时不再合并对齐方式属性，可能是人为修改的需要保留。
            // target.alignment = (TextAlignmentOptions)((int) source.alignment & 0xFF | align);
            target.alignment = align;
            //只能用shared属性，否则改不了，会自动生成instance的材质
            if (source.fontSharedMaterial.name != "DefaultMat")
            {
                var inactiveObjs = target.GetComponentsInParent<RectTransform>(true)
                    .Where(r => !r.gameObject.activeSelf).ToList();
                //需要在可见的情况下材质才能得到正确的值
                foreach (var obj in inactiveObjs)
                    obj.gameObject.SetActive(true);

                target.fontMaterial = source.fontSharedMaterial;

                //恢复激活状态
                foreach (var obj in inactiveObjs)
                    obj.gameObject.SetActive(false);
            }
        }

        static Transform UpdateNestedPrefab(Transform source, PsdLayerIdTag target)
        {
            var s = PrefabUtility.GetCorrespondingObjectFromOriginalSource(source.gameObject);
            var t = PrefabUtility.GetCorrespondingObjectFromOriginalSource(target.gameObject);
            if (s != t)
            {
                var instance = PrefabUtility.InstantiatePrefab(s, target.transform.parent) as GameObject;
                instance.transform.position = source.transform.position;
                instance.transform.localScale = source.transform.localScale;
                instance.transform.localEulerAngles = source.transform.localEulerAngles;
                instance.transform.SetSiblingIndex(target.transform.GetSiblingIndex());
                instance.AddComponent<PsdLayerIdTag>().LayerId = target.LayerId;

                var root = PrefabUtility.GetOutermostPrefabInstanceRoot(target.gameObject);
                var origi = PrefabUtility.GetCorrespondingObjectFromOriginalSource(root);
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot,
                    InteractionMode.AutomatedAction);
                Object.DestroyImmediate(target.gameObject);
                PrefabUtility.SaveAsPrefabAssetAndConnect(root, AssetDatabase.GetAssetPath(origi),
                    InteractionMode.AutomatedAction);

                return instance.transform;
            }

            target.transform.position = source.transform.position;
            target.transform.localScale = source.transform.localScale;
            target.transform.localEulerAngles = source.transform.localEulerAngles;

            return target.transform;
        }
    }
}
