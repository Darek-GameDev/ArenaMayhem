using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor.OverrideComparePopup
{
    /// <summary>
    ///     Handles serialization and deserialization of component properties.
    /// </summary>
    internal static class OcpSerialization
    {
        private static readonly Dictionary<SerializedPropertyType, Action<SerializedProperty, object>> PropertySetters =
            new()
            {
                { SerializedPropertyType.Integer, (p, v) => p.intValue = (int)v },
                { SerializedPropertyType.Boolean, (p, v) => p.boolValue = (bool)v },
                { SerializedPropertyType.Float, (p, v) => p.floatValue = (float)v },
                { SerializedPropertyType.String, (p, v) => p.stringValue = (string)v },
                { SerializedPropertyType.Color, (p, v) => p.colorValue = (Color)v },
                { SerializedPropertyType.Vector2, (p, v) => p.vector2Value = (Vector2)v },
                { SerializedPropertyType.Vector3, (p, v) => p.vector3Value = (Vector3)v },
                { SerializedPropertyType.Vector4, (p, v) => p.vector4Value = (Vector4)v },
                { SerializedPropertyType.Quaternion, (p, v) => p.quaternionValue = (Quaternion)v },
                { SerializedPropertyType.Enum, (p, v) => p.enumValueIndex = (int)v }
            };

        private static readonly Dictionary<string, Action<SerializedProperty, string>> ComponentValueAppliers =
            new()
            {
                { "Integer", ApplyIntegerValue },
                { "Boolean", ApplyBooleanValue },
                { "Float", ApplyFloatValue },
                { "String", ApplyStringValue },
                { "Color", ApplyColorValue },
                { "Vector2", ApplyVector2Value },
                { "Vector3", ApplyVector3Value },
                { "Vector4", ApplyVector4Value },
                { "Quaternion", ApplyQuaternionValue },
                { "Enum", ApplyEnumValue }
            };

        private static readonly Dictionary<SerializedPropertyType, Func<SerializedProperty, string>>
            PropertySerializers =
                new()
                {
                    { SerializedPropertyType.Integer, p => p.intValue.ToString() },
                    { SerializedPropertyType.Boolean, p => p.boolValue.ToString() },
                    { SerializedPropertyType.Float, p => p.floatValue.ToString(CultureInfo.InvariantCulture) },
                    { SerializedPropertyType.String, p => p.stringValue },
                    { SerializedPropertyType.Color, p => "#" + ColorUtility.ToHtmlStringRGBA(p.colorValue) },
                    { SerializedPropertyType.Vector2, SerializeVector2 },
                    { SerializedPropertyType.Vector3, SerializeVector3 },
                    { SerializedPropertyType.Vector4, SerializeVector4 },
                    { SerializedPropertyType.Quaternion, SerializeQuaternion },
                    { SerializedPropertyType.Enum, p => p.enumValueIndex.ToString() }
                };

        private static readonly Dictionary<SerializedPropertyType, Func<SerializedProperty, SerializedProperty, bool>>
            PropertyComparers =
                new()
                {
                    { SerializedPropertyType.Integer, (a, b) => a.intValue != b.intValue },
                    { SerializedPropertyType.Boolean, (a, b) => a.boolValue != b.boolValue },
                    { SerializedPropertyType.Float, (a, b) => !Mathf.Approximately(a.floatValue, b.floatValue) },
                    { SerializedPropertyType.String, (a, b) => a.stringValue != b.stringValue },
                    { SerializedPropertyType.Color, (a, b) => a.colorValue != b.colorValue },
                    { SerializedPropertyType.Vector2, (a, b) => a.vector2Value != b.vector2Value },
                    { SerializedPropertyType.Vector3, (a, b) => a.vector3Value != b.vector3Value },
                    { SerializedPropertyType.Vector4, (a, b) => a.vector4Value != b.vector4Value },
                    { SerializedPropertyType.Quaternion, (a, b) => a.quaternionValue != b.quaternionValue },
                    { SerializedPropertyType.Enum, (a, b) => a.enumValueIndex != b.enumValueIndex }
                };

        /// <summary>
        ///     Sets a property value from a serialized representation.
        /// </summary>
        public static void SetPropertyValue(SerializedProperty prop, object value)
        {
            if (value==null)
            {
                return;
            }

            SetPropertyValueInternal(prop, value);
        }

        private static void SetPropertyValueInternal(SerializedProperty prop, object value)
        {
            ApplyPropertyByType(prop, prop.propertyType, value);
        }

        private static void ApplyPropertyByType(SerializedProperty prop, SerializedPropertyType type, object value)
        {
            if (PropertySetters.TryGetValue(type, out var setter))
            {
                setter(prop, value);
            }
        }

        private static void ApplyIntegerValue(SerializedProperty prop, string value)
        {
            ApplyInteger(prop, value);
        }

        private static void ApplyBooleanValue(SerializedProperty prop, string value)
        {
            ApplyBoolean(prop, value);
        }

        private static void ApplyFloatValue(SerializedProperty prop, string value)
        {
            ApplyFloat(prop, value);
        }

        private static void ApplyStringValue(SerializedProperty prop, string value)
        {
            prop.stringValue = value;
        }

        private static void ApplyColorValue(SerializedProperty prop, string value)
        {
            ApplyColor(prop, value);
        }

        private static void ApplyVector2Value(SerializedProperty prop, string value)
        {
            prop.vector2Value = DeserializeVector2(value);
        }

        private static void ApplyVector3Value(SerializedProperty prop, string value)
        {
            prop.vector3Value = DeserializeVector3(value);
        }

        private static void ApplyVector4Value(SerializedProperty prop, string value)
        {
            prop.vector4Value = DeserializeVector4(value);
        }

        private static void ApplyQuaternionValue(SerializedProperty prop, string value)
        {
            prop.quaternionValue = DeserializeQuaternion(value);
        }

        private static void ApplyEnumValue(SerializedProperty prop, string value)
        {
            ApplyEnum(prop, value);
        }

        /// <summary>
        ///     Applies a serialized component value from string representation.
        /// </summary>
        public static void ApplySerializedComponentValue(SerializedProperty prop, string typeName, string value)
        {
            if (ComponentValueAppliers.TryGetValue(typeName, out var applier))
            {
                applier(prop, value);
            }
        }

        private static void ApplyInteger(SerializedProperty prop, string value)
        {
            if (int.TryParse(value, out var iVal))
            {
                prop.intValue = iVal;
            }
        }

        private static void ApplyBoolean(SerializedProperty prop, string value)
        {
            if (bool.TryParse(value, out var bVal))
            {
                prop.boolValue = bVal;
            }
        }

        private static void ApplyFloat(SerializedProperty prop, string value)
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var fVal))
            {
                prop.floatValue = fVal;
            }
        }

        private static void ApplyColor(SerializedProperty prop, string value)
        {
            if (ColorUtility.TryParseHtmlString(value, out var col))
            {
                prop.colorValue = col;
            }
        }

        private static void ApplyEnum(SerializedProperty prop, string value)
        {
            if (int.TryParse(value, out var eVal))
            {
                prop.enumValueIndex = eVal;
            }
        }

        /// <summary>
        ///     Deserializes a Vector2 from string format.
        /// </summary>
        public static Vector2 DeserializeVector2(string s)
        {
            var values = ParseFloatArray(s, 2);
            return values != null ? new Vector2(values[0], values[1]) : Vector2.zero;
        }

        /// <summary>
        ///     Deserializes a Vector3 from string format.
        /// </summary>
        public static Vector3 DeserializeVector3(string s)
        {
            var values = ParseFloatArray(s, 3);
            return values != null ? new Vector3(values[0], values[1], values[2]) : Vector3.zero;
        }

        /// <summary>
        ///     Deserializes a Vector4 from string format.
        /// </summary>
        public static Vector4 DeserializeVector4(string s)
        {
            var values = ParseFloatArray(s, 4);
            return values != null ? new Vector4(values[0], values[1], values[2], values[3]) : Vector4.zero;
        }

        /// <summary>
        ///     Deserializes a Quaternion from string format.
        /// </summary>
        public static Quaternion DeserializeQuaternion(string s)
        {
            var values = ParseFloatArray(s, 4);
            return values != null ? new Quaternion(values[0], values[1], values[2], values[3]) : Quaternion.identity;
        }

        private static float[] ParseFloatArray(string s, int expectedLength)
        {
            var parts = s.Split(',');
            if (parts.Length != expectedLength)
            {
                return null;
            }

            var values = new float[expectedLength];
            for (int i = 0; i < expectedLength; i++)
            {
                if (!float.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out values[i]))
                {
                    return null;
                }
            }

            return values;
        }

        /// <summary>
        ///     Serializes a property to string representation.
        /// </summary>
        public static string SerializeProperty(SerializedProperty prop)
        {
            return PropertySerializers.TryGetValue(prop.propertyType, out var serializer)
                ? serializer(prop)
                : string.Empty;
        }

        private static string SerializeVector2(SerializedProperty prop)
        {
            var v = prop.vector2Value;
            return $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)}";
        }

        private static string SerializeVector3(SerializedProperty prop)
        {
            var v = prop.vector3Value;
            return
                $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)},{v.z.ToString(CultureInfo.InvariantCulture)}";
        }

        private static string SerializeVector4(SerializedProperty prop)
        {
            var v = prop.vector4Value;
            return
                $"{v.x.ToString(CultureInfo.InvariantCulture)},{v.y.ToString(CultureInfo.InvariantCulture)},{v.z.ToString(CultureInfo.InvariantCulture)},{v.w.ToString(CultureInfo.InvariantCulture)}";
        }

        private static string SerializeQuaternion(SerializedProperty prop)
        {
            var q = prop.quaternionValue;
            return
                $"{q.x.ToString(CultureInfo.InvariantCulture)},{q.y.ToString(CultureInfo.InvariantCulture)},{q.z.ToString(CultureInfo.InvariantCulture)},{q.w.ToString(CultureInfo.InvariantCulture)}";
        }

        /// <summary>
        ///     Checks if two serialized properties differ in value.
        /// </summary>
        public static bool PropertiesDiffer(SerializedProperty a, SerializedProperty b)
        {
            return PropertyComparers.TryGetValue(a.propertyType, out var comparer) && comparer(a, b);
        }
    }
}