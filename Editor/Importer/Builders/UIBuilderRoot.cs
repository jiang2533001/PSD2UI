using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Shimmer.PSD2UI
{
    [CreateAssetMenu(menuName = "UIBuilder/Root")]
    public class UIBuilderRoot : UIBuilderBase
    {
        protected override void OnProcess(UINodeData data, GameObject go)
        {
            var nodeStart = go;
            if (go.transform.childCount > 0) nodeStart = go.transform.GetChild(go.transform.childCount - 1).gameObject;
            RecursiveChildren(data, nodeStart);
        }
    }
}
