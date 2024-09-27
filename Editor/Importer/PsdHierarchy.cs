using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


    [InitializeOnLoad]
    public class PsdHierarchy
    {
        static PsdHierarchy()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemGUI;
        }

        static GUIStyle m_btnStyle;
        static List<PsdLayerIdTag> m_layers = new List<PsdLayerIdTag>();

        static bool m_merging;
        static Transform m_root;
        static Transform m_psdUI;
        static Transform m_targetUI;
        static GameObject m_psdNode;
        static GameObject m_targetNode;
        static GUIStyle m_txtStyle;

        static void OnHierarchyWindowItemGUI(int instanceID, Rect selectionRect)
        {
            if (Application.isPlaying)
                return;

            var instance = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (instance == null)
                return;

            CheckMergingState(selectionRect, instance);

            ShowSelectionRect(selectionRect, instance);

            ShowLayerId(selectionRect, instance);

            var curWin = EditorWindow.mouseOverWindow;
            if (curWin == null || !curWin.ToString().Contains("SceneHierarchyWindow"))
                return;

            var rect = selectionRect;
            rect.xMin -= 1000f;
            rect.xMax += 1000f;
            if (!rect.Contains(Event.current.mousePosition))
                return;

            if (m_btnStyle == null)
                m_btnStyle = new GUIStyle(EditorStyles.objectFieldThumb);

            ShowSelectButton(selectionRect, instance);

            ShowDeleteButton(selectionRect, instance);
        }

        static void CheckMergingState(Rect selectionRect, GameObject instance)
        {
            if (instance.name == "PSD2UI_Preview")
            {
                Rect rect = selectionRect;
                rect.xMax += 10f;
                rect.xMin = rect.xMax - 65f;
                m_merging = GUI.Toggle(rect, m_merging, "Merge ID", GUI.skin.button);
                if (m_merging)
                {
                    if (m_psdUI == null)
                    {
                        m_root = instance.transform;
                        if (m_root.childCount != 2)
                        {
                            UnityEngine.Debug.LogError("只能针对两个UI合并，当前预览底下UI个数不对！");
                        }
                        else
                        {
                            var a = m_root.GetChild(0);
                            var b = m_root.GetChild(1);
                            if (a.GetComponent<PsdInfo>() != null)
                            {
                                m_psdUI = a;
                                m_targetUI = b;
                            }
                            else if (b.GetComponent<PsdInfo>() != null)
                            {
                                m_psdUI = b;
                                m_targetUI = a;
                            }
                            else
                            {
                                UnityEngine.Debug.LogError("预览下找不到由PSD生成的UI");
                            }
                        }

                        if (m_psdUI != null && m_targetUI != null)
                        {
                            UnityEditorInternal.ComponentUtility.CopyComponent(m_psdUI.GetComponent<PsdInfo>());
                            PsdInfo psdInfo = m_targetUI.GetComponent<PsdInfo>();
                            if (psdInfo == null)
                            {
                                psdInfo = m_targetUI.gameObject.AddComponent<PsdInfo>();
                            }

                            UnityEditorInternal.ComponentUtility.PasteComponentValues(psdInfo);
                        }
                    }
                }
                else
                {
                    m_root = null;
                    m_psdUI = null;
                    m_targetUI = null;
                    m_psdNode = null;
                    m_targetNode = null;
                }
            }
        }

        static void ShowSelectionRect(Rect selectionRect, GameObject instance)
        {
            if (m_psdNode == instance || m_targetNode == instance)
            {
                Handles.DrawSolidRectangleWithOutline(selectionRect, Color.clear, Color.green);
            }
        }

        static void ShowLayerId(Rect selectionRect, GameObject instance)
        {
            if (m_txtStyle == null)
            {
                m_txtStyle = new GUIStyle(EditorStyles.label);
                m_txtStyle.alignment = TextAnchor.MiddleRight;
                m_txtStyle.normal.textColor = Color.yellow;
            }

            if (m_merging && instance.transform != m_root && instance.transform.IsChildOf(m_root))
            {
                var tag = instance.GetComponent<PsdLayerIdTag>();
                if (tag != null)
                {
                    GUIContent txt = new GUIContent(tag.LayerId.ToString());
                    var rect = selectionRect;
                    rect.xMax -= 30f;
                    rect.xMin = rect.xMax - m_txtStyle.CalcSize(txt).x;
                    GUI.Label(rect, txt, m_txtStyle);
                }
            }
        }

        static void ShowSelectButton(Rect selectionRect, GameObject instance)
        {
            if (m_merging && instance.transform != m_root && instance.transform.IsChildOf(m_root))
            {
                GUIContent icon = EditorGUIUtility.IconContent("RectTransformBlueprint");
                icon.tooltip = null;
                Rect rect = selectionRect;
                rect.xMax -= 10f;
                rect.xMin = rect.xMax - m_btnStyle.CalcSize(icon).x;
                if (GUI.Button(rect, icon, m_btnStyle))
                {
                    SelectNode(instance);
                }
            }
        }

        static void SelectNode(GameObject go)
        {
            bool isPsdNode = go.transform.IsChildOf(m_psdUI);
            if (isPsdNode)
            {
                m_psdNode = go;
                if (m_targetNode != null)
                    Merge();
            }
            else
            {
                m_targetNode = go;
                if (m_psdNode != null)
                    Merge();
            }
        }

        static void Merge()
        {
            var psdTag = m_psdNode.GetComponent<PsdLayerIdTag>();
            var tag = m_targetNode.GetComponent<PsdLayerIdTag>();
            if (tag != null)
                Undo.RecordObject(tag, "Change Layer ID");
            else
                tag = Undo.AddComponent<PsdLayerIdTag>(m_targetNode);
            tag.LayerId = psdTag.LayerId;
            m_psdNode = null;
            m_targetNode = null;
        }

        static void ShowDeleteButton(Rect selectionRect, GameObject instance)
        {
            bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(instance);
            if (isPrefab)
                return;

            instance.GetComponentsInChildren(true, m_layers);
            if (m_layers.Count == 0 || m_layers[0].LayerId == -1)
                return;

            var deleted = instance.GetComponentInParent<PsdInfo>();
            if (deleted == null)
                return;

            GUIContent icon = EditorGUIUtility.IconContent("sv_icon_none");
            Rect rect = selectionRect;
            rect.xMax += 10f;
            rect.xMin = rect.xMax - m_btnStyle.CalcSize(icon).x;
            if (GUI.Button(rect, icon, m_btnStyle))
            {
                foreach (var layer in m_layers)
                {
                    if (deleted.deletedLayers.FindIndex(l => l.id == layer.LayerId) == -1)
                    {
                        deleted.deletedLayers.Add(new LayerName { id = layer.LayerId, name = layer.gameObject.name });
                    }
                }

                Undo.DestroyObjectImmediate(instance);
            }
        }
    }
