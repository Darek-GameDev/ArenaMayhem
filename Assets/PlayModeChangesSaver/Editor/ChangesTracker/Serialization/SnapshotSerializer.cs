using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor.ChangesTracker.Serialization
{
    public readonly struct SerializationResult
    {
        public string TypeName { get; }
        public string SerializedValue { get; }

        public SerializationResult(string typeName, string serializedValue)
        {
            TypeName = typeName;
            SerializedValue = serializedValue;
        }
    }

    [Serializable]
    internal class GradientContainer
    {
        public Gradient gradient;
    }

    [Serializable]
    internal class AnimationCurveContainer
    {
        public AnimationCurve curve;
    }

    public static class SnapshotSerializer
    {
        private static readonly NumberStyles FloatStyle = NumberStyles.Float;
        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;

        private static readonly Dictionary<Type, Func<object, SerializationResult>> SerializationMap =
            new()
            {
                { typeof(int), v => new SerializationResult("Integer", ((int)v).ToString()) },
                { typeof(bool), v => new SerializationResult("Boolean", ((bool)v).ToString()) },
                { typeof(float), v => new SerializationResult("Float", ((float)v).ToString(InvariantCulture)) },
                { typeof(string), v => new SerializationResult("String", (string)v) },
                { typeof(Color), v => new SerializationResult("Color", "#" + ColorUtility.ToHtmlStringRGBA((Color)v)) },
                { typeof(Vector2), v => new SerializationResult("Vector2", SerializeVector2((Vector2)v)) },
                { typeof(Vector3), v => new SerializationResult("Vector3", SerializeVector3((Vector3)v)) },
                { typeof(Vector4), v => new SerializationResult("Vector4", SerializeVector4((Vector4)v)) },
                { typeof(Quaternion), v => new SerializationResult("Quaternion", SerializeQuaternion((Quaternion)v)) },
                { typeof(Vector2Int), v => new SerializationResult("Vector2Int", SerializeVector2Int((Vector2Int)v)) },
                { typeof(Vector3Int), v => new SerializationResult("Vector3Int", SerializeVector3Int((Vector3Int)v)) },
                { typeof(Rect), v => new SerializationResult("Rect", SerializeRect((Rect)v)) },
                { typeof(RectInt), v => new SerializationResult("RectInt", SerializeRectInt((RectInt)v)) },
                { typeof(Bounds), v => new SerializationResult("Bounds", SerializeBounds((Bounds)v)) },
                { typeof(BoundsInt), v => new SerializationResult("BoundsInt", SerializeBoundsInt((BoundsInt)v)) }
            };

        private static bool TryParseFloatComponents(string input, int componentCount, out float[] components)
        {
            components = new float[componentCount];
            var parts = input.Split(',');

            if (parts.Length != componentCount)
            {
                return false;
            }

            for (int i = 0; i < componentCount; i++)
            {
                if (!float.TryParse(parts[i], FloatStyle, InvariantCulture, out components[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static string SerializeVector2(Vector2 v)
        {
            return $"{v.x.ToString(InvariantCulture)},{v.y.ToString(InvariantCulture)}";
        }

        private static string SerializeVector3(Vector3 v)
        {
            return
                $"{v.x.ToString(InvariantCulture)},{v.y.ToString(InvariantCulture)},{v.z.ToString(InvariantCulture)}";
        }

        private static string SerializeVector4(Vector4 v)
        {
            return
                $"{v.x.ToString(InvariantCulture)},{v.y.ToString(InvariantCulture)},{v.z.ToString(InvariantCulture)},{v.w.ToString(InvariantCulture)}";
        }

        private static string SerializeQuaternion(Quaternion q)
        {
            return $"{q.x.ToString(InvariantCulture)},{q.y.ToString(InvariantCulture)},{q.z.ToString(InvariantCulture)},{q.w.ToString(InvariantCulture)}";
        }

        private static string SerializeVector2Int(Vector2Int v)
        {
            return $"{v.x},{v.y}";
        }

        private static string SerializeVector3Int(Vector3Int v)
        {
            return $"{v.x},{v.y},{v.z}";
        }

        private static string SerializeRect(Rect r)
        {
            return $"{r.x.ToString(InvariantCulture)},{r.y.ToString(InvariantCulture)},{r.width.ToString(InvariantCulture)},{r.height.ToString(InvariantCulture)}";
        }

        private static string SerializeRectInt(RectInt r)
        {
            return $"{r.x},{r.y},{r.width},{r.height}";
        }

        private static string SerializeBounds(Bounds b)
        {
            return $"{SerializeVector3(b.center)}|{SerializeVector3(b.size)}";
        }

        private static string SerializeBoundsInt(BoundsInt b)
        {
            return $"{SerializeVector3Int(b.position)}|{SerializeVector3Int(b.size)}";
        }

        public static Vector2 DeserializeVector2(string s)
        {
            if (!TryParseFloatComponents(s, 2, out var c))
            {
                return Vector2.zero;
            }

            return new Vector2(c[0], c[1]);
        }

        public static Vector3 DeserializeVector3(string s)
        {
            if (!TryParseFloatComponents(s, 3, out var c))
            {
                return Vector3.zero;
            }

            return new Vector3(c[0], c[1], c[2]);
        }

        public static Vector4 DeserializeVector4(string s)
        {
            if (!TryParseFloatComponents(s, 4, out var c))
            {
                return Vector4.zero;
            }

            return new Vector4(c[0], c[1], c[2], c[3]);
        }

        public static Quaternion DeserializeQuaternion(string s)
        {
            if (!TryParseFloatComponents(s, 4, out var c))
            {
                return Quaternion.identity;
            }

            return new Quaternion(c[0], c[1], c[2], c[3]);
        }

        public static Vector2Int DeserializeVector2Int(string s)
        {
            var parts = s.Split(',');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var x) || !int.TryParse(parts[1], out var y))
            {
                return Vector2Int.zero;
            }
            return new Vector2Int(x, y);
        }

        public static Vector3Int DeserializeVector3Int(string s)
        {
            var parts = s.Split(',');
            if (parts.Length != 3 || !int.TryParse(parts[0], out var x) || !int.TryParse(parts[1], out var y) || !int.TryParse(parts[2], out var z))
            {
                return Vector3Int.zero;
            }
            return new Vector3Int(x, y, z);
        }

        public static Rect DeserializeRect(string s)
        {
            if (!TryParseFloatComponents(s, 4, out var c))
            {
                return new Rect();
            }
            return new Rect(c[0], c[1], c[2], c[3]);
        }

        public static RectInt DeserializeRectInt(string s)
        {
            var parts = s.Split(',');
            if (parts.Length != 4 || 
                !int.TryParse(parts[0], out var x) || 
                !int.TryParse(parts[1], out var y) || 
                !int.TryParse(parts[2], out var w) || 
                !int.TryParse(parts[3], out var h))
            {
                return new RectInt();
            }
            return new RectInt(x, y, w, h);
        }

        public static Bounds DeserializeBounds(string s)
        {
            var parts = s.Split('|');
            if (parts.Length != 2) return new Bounds();
            return new Bounds(DeserializeVector3(parts[0]), DeserializeVector3(parts[1]));
        }

        public static BoundsInt DeserializeBoundsInt(string s)
        {
            var parts = s.Split('|');
            if (parts.Length != 2) return new BoundsInt();
            return new BoundsInt(DeserializeVector3Int(parts[0]), DeserializeVector3Int(parts[1]));
        }



        public static string SerializeGradient(Gradient g)
        {
            var container = new GradientContainer { gradient = g };
            return EditorJsonUtility.ToJson(container);
        }

        public static Gradient DeserializeGradient(string s)
        {
            var container = new GradientContainer();
            EditorJsonUtility.FromJsonOverwrite(s, container);
            return container.gradient;
        }

        public static string SerializeAnimationCurve(AnimationCurve c)
        {
            var container = new AnimationCurveContainer { curve = c };
            return EditorJsonUtility.ToJson(container);
        }

        public static AnimationCurve DeserializeAnimationCurve(string s)
        {
            var container = new AnimationCurveContainer();
            EditorJsonUtility.FromJsonOverwrite(s, container);
            return container.curve;
        }

        public static SerializationResult SerializeValue(object value)
        {
            if (value==null)
            {
                return new SerializationResult(string.Empty, string.Empty);
            }

            var valueType = value.GetType();

            if (SerializationMap.TryGetValue(valueType, out var serializer))
            {
                return serializer(value);
            }

            if (valueType.IsEnum)
            {
                return new SerializationResult("Enum", Convert.ToInt32(value).ToString());
            }

            return new SerializationResult(string.Empty, string.Empty);
        }
    }
}