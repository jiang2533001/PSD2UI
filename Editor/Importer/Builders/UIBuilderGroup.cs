using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Shimmer.PSD2UI
{
    [CreateAssetMenu(menuName = "UIBuilder/Group")]
    public class UIBuilderGroup : UIBuilderBase
    {
        protected override void OnProcess(UINodeData data, GameObject go)
        {
            RecursiveChildren(data, go);
        }
    }
}
