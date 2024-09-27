using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NOAH.PSD2UI
{

    [CreateAssetMenu(menuName = "UIBuilder/Button")]
    public class UIBuilderButton : UIBuilderBase
    {
        protected override void OnProcess(UINodeData data, GameObject go)
        {
            var image = go.AddComponent<Image>();
            SetImage(data, image);

            go.AddComponent<Button>().targetGraphic = image;
        }
    }
}
