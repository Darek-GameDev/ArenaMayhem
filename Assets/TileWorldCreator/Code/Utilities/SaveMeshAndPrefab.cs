/*

  _____ _ _    __        __         _     _  ____                _             
 |_   _(_) | __\ \      / /__  _ __| | __| |/ ___|_ __ ___  __ _| |_ ___  _ __ 
   | | | | |/ _ \ \ /\ / / _ \| '__| |/ _` | |   | '__/ _ \/ _` | __/ _ \| '__|
   | | | | |  __/\ V  V / (_) | |  | | (_| | |___| | |  __/ (_| | || (_) | |   
   |_| |_|_|\___| \_/\_/ \___/|_|  |_|\__,_|\____|_|  \___|\__,_|\__\___/|_|   
                                                                               
	www.giantgrey.com

*/

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace GiantGrey.TileWorldCreator.Utilities
{
    public static class SaveMeshAndPrefab  
    {
        public static void SaveMeshesAndPrefab(GameObject root)
        {
            if (root == null)
            {
                Debug.LogWarning("No root GameObject provided.");
                return;
            }

            string rootName = root.name;

            // Ask for a save location for the prefab
            string prefabPath = EditorUtility.SaveFilePanelInProject(
                "Save Prefab",
                rootName + ".prefab",
                "prefab",
                "Choose location to save prefab."
            );

            if (string.IsNullOrEmpty(prefabPath))
                return;

            string prefabFolder = Path.GetDirectoryName(prefabPath).Replace("\\", "/");
            string meshFolder = Path.Combine(prefabFolder, root.name + "_Meshes").Replace("\\", "/");

            if (!AssetDatabase.IsValidFolder(meshFolder))
            {
                AssetDatabase.CreateFolder(prefabFolder, root.name + "_Meshes");
            }

            // --- Collect meshes from children ---
            var components = new List<(Object component, Mesh mesh, bool isCollider)>();

            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh != null && mf.sharedMesh.vertexCount > 0)
                {
                    components.Add((mf, mf.sharedMesh, false));
                }
            }

            foreach (var mc in root.GetComponentsInChildren<MeshCollider>())
            {
                if (mc.sharedMesh != null && mc.sharedMesh.vertexCount > 0)
                {
                    components.Add((mc, mc.sharedMesh, true));
                }
            }

            if (components.Count == 0)
            {
                Debug.LogWarning("No valid meshes found.");
                return;
            }

            // --- Load existing meshes in folder ---
            var existingMeshes = new Dictionary<string, Mesh>();
            string[] oldAssets = AssetDatabase.FindAssets("t:Mesh", new[] { meshFolder });
            foreach (string guid in oldAssets)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
                if (mesh != null)
                    existingMeshes[assetPath] = mesh;
            }

            // --- Deduplicate + Save new/changed meshes ---
            var meshMap = new Dictionary<Mesh, Mesh>();
            bool dirty = false;

            foreach (var (component, srcMesh, isCollider) in components)
            {
                Mesh targetMesh = null;

                if (meshMap.TryGetValue(srcMesh, out targetMesh))
                {
                    // Already processed → reuse
                    if (isCollider)
                        ((MeshCollider)component).sharedMesh = targetMesh;
                    else
                        ((MeshFilter)component).sharedMesh = targetMesh;
                    // Debug.Log($"Reuse mesh: {targetMesh.name}");
                    continue;
                }

                // Build a clean name
                string baseName = string.IsNullOrEmpty(srcMesh.name) ? component.name : srcMesh.name;
                string meshName = isCollider 
                    ? (baseName.EndsWith("_Collider") ? baseName : baseName + "_Collider") 
                    : baseName;

                string assetPath = Path.Combine(meshFolder, meshName + ".asset").Replace("\\", "/");

                // Check if an existing asset exists in folder
                if (existingMeshes.TryGetValue(assetPath, out Mesh oldMesh))
                {
                    if (!MeshChanged(srcMesh, oldMesh))
                    {
                        targetMesh = oldMesh; // reuse existing asset
                        // Debug.Log($"Reuse mesh from folder: {meshName}");
                    }
                    else
                    {
                        AssetDatabase.DeleteAsset(assetPath);
                        targetMesh = Object.Instantiate(srcMesh);
                        targetMesh.name = meshName;
                        AssetDatabase.CreateAsset(targetMesh, assetPath);
                        dirty = true;
                        // Debug.Log($"Mesh changed → recreated: {meshName}");
                    }
                }
                else
                {
                    // New asset
                    targetMesh = Object.Instantiate(srcMesh);
                    targetMesh.name = meshName;
                    AssetDatabase.CreateAsset(targetMesh, assetPath);
                    dirty = true;
                    // Debug.Log($"New mesh saved: {meshName}");
                }

                meshMap[srcMesh] = targetMesh;

                // Assign to component
                if (isCollider)
                    ((MeshCollider)component).sharedMesh = targetMesh;
                else
                    ((MeshFilter)component).sharedMesh = targetMesh;
            }

            // --- Save prefab ---
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, prefabPath, InteractionMode.UserAction);

            if (dirty)
            {
                AssetDatabase.SaveAssets();
                // AssetDatabase.Refresh(); // only if you want to see assets update in Project window immediately
            }

            Debug.Log($"Saved prefab at {prefabPath} with meshes in {meshFolder}");
        }

        private static bool MeshChanged(Mesh a, Mesh b)
        {
            if (a == null || b == null) return true;
            if (a.vertexCount != b.vertexCount) return true;
            if (a.subMeshCount != b.subMeshCount) return true;
            if (a.bounds != b.bounds) return true;
            return false;
        }
    }
}
#endif