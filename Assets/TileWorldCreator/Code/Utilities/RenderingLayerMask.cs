#if !UNITY_6000_0_OR_NEWER
namespace GiantGrey.TileWorldCreator.Utilities
{
    /// <summary>
    /// A lightweight compatibility version of RenderingLayerMask for Unity 2022/2023.
    /// - Supports implicit conversion to/from uint
    /// - Provides a name-retrieval API similar to Unity 6
    /// - Includes some bitwise operations and utility methods for everyday use
    /// </summary>
    public struct RenderingLayerMask
    {
        private uint _value;

        public RenderingLayerMask(uint value) { _value = value; }


        public uint value
        {
            readonly get => _value;
            set => _value = value;
        }

        public static implicit operator uint(RenderingLayerMask m) => m._value;
        public static implicit operator RenderingLayerMask(uint v) => new RenderingLayerMask(v);


        public static explicit operator int(RenderingLayerMask m) => unchecked((int)m._value);


        public static explicit operator RenderingLayerMask(int v) => new RenderingLayerMask(unchecked((uint)v));


        public static RenderingLayerMask FromIndex(int index) =>
            (index >= 0 && index < 32) ? new RenderingLayerMask(1u << index) : new RenderingLayerMask(0u);


        public static RenderingLayerMask FromIndices(params int[] indices)
        {
            uint m = 0u;
            if (indices != null)
            {
                for (int i = 0; i < indices.Length; i++)
                {
                    int idx = indices[i];
                    if (idx >= 0 && idx < 32) m |= (1u << idx);
                }
            }
            return new RenderingLayerMask(m);
        }


        public readonly bool HasIndex(int index) =>
            (index >= 0 && index < 32) && ((_value & (1u << index)) != 0);

        public RenderingLayerMask WithIndex(int index, bool enabled)
        {
            if (index < 0 || index >= 32) return this;
            if (enabled) _value |= (1u << index);
            else _value &= ~(1u << index);
            return this;
        }

        public static RenderingLayerMask operator |(RenderingLayerMask a, RenderingLayerMask b) =>
            new RenderingLayerMask(a._value | b._value);
        public static RenderingLayerMask operator &(RenderingLayerMask a, RenderingLayerMask b) =>
            new RenderingLayerMask(a._value & b._value);
        public static RenderingLayerMask operator ^(RenderingLayerMask a, RenderingLayerMask b) =>
            new RenderingLayerMask(a._value ^ b._value);
        public static RenderingLayerMask operator ~(RenderingLayerMask a) =>
            new RenderingLayerMask(~a._value);

        public override readonly string ToString() => $"RenderingLayerMask(0x{_value:X8})";


        public static string[] GetDefinedRenderingLayerNames()
        {
            var rpa = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline
                      as UnityEngine.Rendering.RenderPipelineAsset;

            var names = rpa != null ? rpa.renderingLayerMaskNames : null;
            if (names == null || names.Length == 0)
            {
                var fallback = new string[32];
                fallback[0] = "Default";
                for (int i = 1; i < 32; i++) fallback[i] = i.ToString();
                return fallback;
            }
            return names;
        }


        public static string[] GetPrefixedRenderingLayerNames()
        {
            var rpa = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline
                      as UnityEngine.Rendering.RenderPipelineAsset;

            var names = rpa != null ? rpa.prefixedRenderingLayerMaskNames : null;
            if (names == null || names.Length == 0)
            {
                var fallback = new string[32];
                for (int i = 0; i < 32; i++) fallback[i] = (i == 0) ? "0: Default" : $"{i}";
                return fallback;
            }
            return names;
        }
    }
}
#endif