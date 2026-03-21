// Unity 2022/2023 compatibility: UI Toolkit field, appearance/usage similar to Unity 6
// Automatically disabled when upgrading to Unity 6+ to avoid name conflicts with the official class

#if UNITY_EDITOR && !UNITY_6000_0_OR_NEWER
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace GiantGrey.TileWorldCreator.UI
{

    public class RenderingLayerMaskField : MaskField
    {
      
        public RenderingLayerMaskField() : this(label: null, defaultMask: 0u) { }

        public RenderingLayerMaskField(string label, uint defaultMask = 0u)
            : base(label,
                   new List<string>(GetPrefixedRenderingLayerNames()),
                   unchecked((int)defaultMask),
                   s => s,  
                   s => s)  
        { }

       
        public new uint value
        {
            get => unchecked((uint)base.value);
            set => base.value = unchecked((int)value);
        }

      
        public void SetValueWithoutNotify(uint newValue) =>
            base.SetValueWithoutNotify(unchecked((int)newValue));

      
        public void RefreshNames()
        {
            int keep = base.value;
            choices = new List<string>(GetPrefixedRenderingLayerNames());
            base.SetValueWithoutNotify(keep);
            this.MarkDirtyRepaint();
        }

      
        static string[] GetPrefixedRenderingLayerNames()
        {
            var rpa = GraphicsSettings.currentRenderPipeline as RenderPipelineAsset;
            var names = rpa != null ? rpa.prefixedRenderingLayerMaskNames : null; // e.g. "2: Enemies"
            if (names == null || names.Length == 0)
            {
                
                var fallback = new string[32];
                for (int i = 0; i < 32; i++)
                    fallback[i] = (i == 0) ? "0: Default" : i.ToString();
                return fallback;
            }
            return names;
        }
    }
}
#endif