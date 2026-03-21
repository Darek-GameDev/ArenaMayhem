using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor.ChangesTracker.App
{
    public static class MaterialChangeHandler
    {
        public static List<string> GetRendererMaterialGuids(Renderer renderer)
        {
            var guids = new List<string>();
            if (!renderer)
            {
                return guids;
            }

            AddMaterialGuids(renderer, guids);
            return guids;
        }

        public static void ApplyMaterials(Renderer renderer, List<string> materialGuids)
        {
            if (!ShouldApplyMaterials(renderer, materialGuids))
            {
                return;
            }

            renderer.sharedMaterials = LoadMaterialsFromGuids(renderer, materialGuids);
        }

        public static bool ShouldApplyMaterials(Renderer renderer, List<string> materialGuids)
        {
            if (!renderer)
            {
                return false;
            }

            return materialGuids is { Count: > 0 };
        }

        private static void AddMaterialGuids(Renderer renderer, List<string> guids)
        {
            var materials = renderer.sharedMaterials;
            foreach (var mat in materials)
            {
                guids.Add(GetMaterialGuid(mat));
            }
        }

        private static Material[] LoadMaterialsFromGuids(Renderer renderer, List<string> guids)
        {
            var applied = new Material[guids.Count];
            var current = renderer.sharedMaterials;

            for (int i = 0; i < guids.Count; i++)
            {
                applied[i] = ResolveMaterialFromGuid(guids[i], current, i);
            }

            return applied;
        }

        private static Material ResolveMaterialFromGuid(string guid, Material[] currentMaterials, int index)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat)
            {
                return mat;
            }

            if (index < currentMaterials.Length)
            {
                return currentMaterials[index];
            }

            return null;
        }

        private static string GetMaterialGuid(Material mat)
        {
            if (!mat)
            {
                return string.Empty;
            }

            string path = AssetDatabase.GetAssetPath(mat);
            return string.IsNullOrEmpty(path) ? string.Empty : AssetDatabase.AssetPathToGUID(path);
        }
    }
}