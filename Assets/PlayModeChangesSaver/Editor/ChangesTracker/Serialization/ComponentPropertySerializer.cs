using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor.ChangesTracker.Serialization
{
    public static class ComponentPropertySerializer
    {
        private static readonly Dictionary<SerializedPropertyType, Func<SerializedProperty, (string, string)>>
            SerializationStrategies =
                new()
                {
                    { SerializedPropertyType.Integer, p => ("Integer", p.intValue.ToString()) },
                    { SerializedPropertyType.Boolean, p => ("Boolean", p.boolValue.ToString()) },
                    {
                        SerializedPropertyType.Float,
                        p => ("Float", p.floatValue.ToString(CultureInfo.InvariantCulture))
                    },
                    { SerializedPropertyType.String, p => ("String", p.stringValue ?? string.Empty) },
                    { SerializedPropertyType.Color, p => ("Color", "#" + ColorUtility.ToHtmlStringRGBA(p.colorValue)) },
                    { SerializedPropertyType.Vector2, p => ("Vector2", SerializeVector2(p.vector2Value)) },
                    { SerializedPropertyType.Vector3, p => ("Vector3", SerializeVector3(p.vector3Value)) },
                    { SerializedPropertyType.Vector4, p => ("Vector4", SerializeVector4(p.vector4Value)) },
                    { SerializedPropertyType.Quaternion, p => ("Quaternion", SerializeQuaternion(p.quaternionValue)) },
                    { SerializedPropertyType.Enum, p => ("Enum", p.enumValueIndex.ToString()) },
                    { SerializedPropertyType.Vector2Int, p => ("Vector2Int", SnapshotSerializer.SerializeValue(p.vector2IntValue).SerializedValue) },
                    { SerializedPropertyType.Vector3Int, p => ("Vector3Int", SnapshotSerializer.SerializeValue(p.vector3IntValue).SerializedValue) },
                    { SerializedPropertyType.Rect, p => ("Rect", SnapshotSerializer.SerializeValue(p.rectValue).SerializedValue) },
                    { SerializedPropertyType.RectInt, p => ("RectInt", SnapshotSerializer.SerializeValue(p.rectIntValue).SerializedValue) },
                    { SerializedPropertyType.Bounds, p => ("Bounds", SnapshotSerializer.SerializeValue(p.boundsValue).SerializedValue) },
                    { SerializedPropertyType.BoundsInt, p => ("BoundsInt", SnapshotSerializer.SerializeValue(p.boundsIntValue).SerializedValue) },
                    { SerializedPropertyType.LayerMask, p => ("Integer", p.intValue.ToString()) },
                    { SerializedPropertyType.Gradient, p => ("Gradient", SnapshotSerializer.SerializeGradient(p.gradientValue)) },
                    { SerializedPropertyType.AnimationCurve, p => ("AnimationCurve", SnapshotSerializer.SerializeAnimationCurve(p.animationCurveValue)) },
                    { SerializedPropertyType.ObjectReference, p => ("ObjectReference", GetObjectReferenceValue(p)) }
                };

        private static readonly Dictionary<string, Action<SerializedProperty, string>> DeserializationStrategies =
            new()
            {
                {
                    "Integer", (p, v) =>
                    {
                        if (int.TryParse(v, out var val))
                        {
                            p.intValue = val;
                        }
                    }
                },
                {
                    "Boolean", (p, v) =>
                    {
                        if (bool.TryParse(v, out var val))
                        {
                            p.boolValue = val;
                        }
                    }
                },
                {
                    "Float", (p, v) =>
                    {
                        if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
                        {
                            p.floatValue = val;
                        }
                    }
                },
                { "String", (p, v) => p.stringValue = v },
                {
                    "Color", (p, v) =>
                    {
                        if (ColorUtility.TryParseHtmlString(v, out var col))
                        {
                            p.colorValue = col;
                        }
                    }
                },
                { "Vector2", (p, v) => p.vector2Value = SnapshotSerializer.DeserializeVector2(v) },
                { "Vector3", (p, v) => p.vector3Value = SnapshotSerializer.DeserializeVector3(v) },
                { "Vector4", (p, v) => p.vector4Value = SnapshotSerializer.DeserializeVector4(v) },
                { "Quaternion", (p, v) => p.quaternionValue = SnapshotSerializer.DeserializeQuaternion(v) },
                {
                    "Enum", (p, v) =>
                    {
                        if (int.TryParse(v, out var val))
                        {
                            p.enumValueIndex = val;
                        }
                    }
                },
                { "Vector2Int", (p, v) => p.vector2IntValue = SnapshotSerializer.DeserializeVector2Int(v) },
                { "Vector3Int", (p, v) => p.vector3IntValue = SnapshotSerializer.DeserializeVector3Int(v) },
                { "Rect", (p, v) => p.rectValue = SnapshotSerializer.DeserializeRect(v) },
                { "RectInt", (p, v) => p.rectIntValue = SnapshotSerializer.DeserializeRectInt(v) },
                { "Bounds", (p, v) => p.boundsValue = SnapshotSerializer.DeserializeBounds(v) },
                { "BoundsInt", (p, v) => p.boundsIntValue = SnapshotSerializer.DeserializeBoundsInt(v) },
                { 
                    "Gradient", (p, v) => 
                    {
                        var g = SnapshotSerializer.DeserializeGradient(v);
                        if (g != null) p.gradientValue = g;
                    }
                },
                { 
                    "AnimationCurve", (p, v) => 
                    {
                        var c = SnapshotSerializer.DeserializeAnimationCurve(v);
                        if (c != null) p.animationCurveValue = c;
                    } 
                },
                { "ObjectReference", (p, v) => DeserializeObjectReference(p, v) }
            };

        private static readonly Dictionary<SerializedPropertyType, Func<SerializedProperty, object>>
            GetValueStrategies =
                new()
                {
                    { SerializedPropertyType.Integer, p => p.intValue },
                    { SerializedPropertyType.Boolean, p => p.boolValue },
                    { SerializedPropertyType.Float, p => p.floatValue },
                    { SerializedPropertyType.String, p => p.stringValue },
                    { SerializedPropertyType.Color, p => p.colorValue },
                    { SerializedPropertyType.Vector2, p => p.vector2Value },
                    { SerializedPropertyType.Vector3, p => p.vector3Value },
                    { SerializedPropertyType.Vector4, p => p.vector4Value },
                    { SerializedPropertyType.Quaternion, p => p.quaternionValue },
                    { SerializedPropertyType.Enum, p => p.enumValueIndex },
                    { SerializedPropertyType.Vector2Int, p => p.vector2IntValue },
                    { SerializedPropertyType.Vector3Int, p => p.vector3IntValue },
                    { SerializedPropertyType.Rect, p => p.rectValue },
                    { SerializedPropertyType.RectInt, p => p.rectIntValue },
                    { SerializedPropertyType.Bounds, p => p.boundsValue },
                    { SerializedPropertyType.BoundsInt, p => p.boundsIntValue },
                    { SerializedPropertyType.LayerMask, p => p.intValue },
                    { SerializedPropertyType.Gradient, p => p.gradientValue },
                    { SerializedPropertyType.AnimationCurve, p => p.animationCurveValue },
                    { SerializedPropertyType.ObjectReference, p => p.objectReferenceValue }
                };

        public static void SerializeProperty(SerializedProperty prop, out string typeName, out string serializedValue)
        {
            typeName = "";
            serializedValue = "";

            if (SerializationStrategies.TryGetValue(prop.propertyType, out var strategy))
            {
                (typeName, serializedValue) = strategy(prop);
            }
        }

        public static void ApplyPropertyValue(SerializedProperty prop, string typeName, string value)
        {
            if (DeserializationStrategies.TryGetValue(typeName, out var strategy))
            {
                strategy(prop, value);
            }
        }

        private static string SerializeVector2(Vector2 v)
        {
            return
                $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)}";
        }

        private static string SerializeVector3(Vector3 v)
        {
            return
                $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)},{v.z.ToString(CultureInfo.InvariantCulture)}";
        }

        private static string SerializeVector4(Vector4 v)
        {
            return
                $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)},{v.z.ToString(CultureInfo.InvariantCulture)},{v.w.ToString(CultureInfo.InvariantCulture)}";
        }

        private static string SerializeQuaternion(Quaternion q)
        {
            return
                $"{q.x.ToString(CultureInfo.InvariantCulture)},{q.y.ToString(CultureInfo.InvariantCulture)},{q.z.ToString(CultureInfo.InvariantCulture)},{q.w.ToString(CultureInfo.InvariantCulture)}";
        }

        public static object GetPropertyValue(SerializedProperty prop)
        {
            return GetValueStrategies.TryGetValue(prop.propertyType, out var strategy) ? strategy(prop) : null;
        }

        public static bool IsTypeSupported(SerializedPropertyType type)
        {
            return SerializationStrategies.ContainsKey(type);
        }

        private static string GetObjectReferenceValue(SerializedProperty p)
        {
            if (p.objectReferenceValue == null) return string.Empty;
            
            // Try GlobalObjectId first (best for scene objects)
            string globalId = GlobalObjectId.GetGlobalObjectIdSlow(p.objectReferenceValue).ToString();
            
            // Also store asset path if possible, as backup or for pure assets
            string assetPath = AssetDatabase.GetAssetPath(p.objectReferenceValue);
            
            return $"{globalId}|{assetPath}";
        }

        private static void DeserializeObjectReference(SerializedProperty p, string v)
        {
            if (string.IsNullOrEmpty(v))
            {
                p.objectReferenceValue = null;
                return;
            }

            var parts = v.Split('|');
            string globalIdInfo = parts.Length > 0 ? parts[0] : "";
            string assetPath = parts.Length > 1 ? parts[1] : "";

            // Try resolving by GlobalObjectId
            if (!string.IsNullOrEmpty(globalIdInfo) && GlobalObjectId.TryParse(globalIdInfo, out var globalId))
            {
                var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(globalId);
                if (obj != null)
                {
                    p.objectReferenceValue = obj;
                    return;
                }
            }

            // Fallback: Load Asset by path
            if (!string.IsNullOrEmpty(assetPath))
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (obj != null)
                {
                    p.objectReferenceValue = obj;
                    return;
                }
            }

            // If we can't restore it, leave it as is or set to null? 
            // Better to not change if we can't find the target, or set to null if intended?
            // If v was not empty but we failed, it means the reference is lost. 
            // In changes tracking, if we apply a "lost" reference, it should probably be null.
            p.objectReferenceValue = null;
        }

        // Minimal JSON wrapper for Gradient/AnimationCurve
        private static class MinJsonUtility
        {
            public static string ToJson(object obj)
            {
                return EditorJsonUtility.ToJson(obj);
            }

            public static T FromJson<T>(string json) where T : new()
            {
                var obj = new T();
                EditorJsonUtility.FromJsonOverwrite(json, obj);
                return obj;
            }
        }
    }
}