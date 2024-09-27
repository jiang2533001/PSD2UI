using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NOAH.PSD2UI
{
    [CreateAssetMenu(menuName = "UIBuilder/Image")]
    public class UIBuilderImage : UIBuilderBase
    {
        protected override void OnProcess(UINodeData data, GameObject go)
        {
            var image = go.AddComponent<Image>();
            SetImage(data, image);
        }
    }
}
