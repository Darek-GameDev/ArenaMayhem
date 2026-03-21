using System.IO;
using UnityEditor;

namespace PlayModeChangesSaver.Editor
{
    internal static class PlayModeChangesSaverPaths
    {
        private const string RootFolderName = "PlayModeChangesSaver";
        private const string ScriptableObjectsFolderName = "Scriptable_Objects";

        private static string NormalizePath(string path)
        {
            return path?.Replace("\\", "/");
        }

        private static string GetRootFolder()
        {
            // Locate any known asset (asmdef preferred) and walk up to RuntimeChangesSaver folder.
            string[] asmdefGuids = AssetDatabase.FindAssets("RuntimeChangesSaver.Editor asmdef");
            string candidatePath = TryResolveRootFromGuids(asmdefGuids);

            if (string.IsNullOrEmpty(candidatePath))
            {
                string[] scriptGuids = AssetDatabase.FindAssets("t:Script RuntimeChangesSaverPaths");
                candidatePath = TryResolveRootFromGuids(scriptGuids);
            }

            if (!string.IsNullOrEmpty(candidatePath))
            {
                return candidatePath;
            }

            // Fallbacks: prefer Assets/RuntimeChangesSaver if present, otherwise Assets.
            if (AssetDatabase.IsValidFolder("Assets/" + RootFolderName))
            {
                return $"Assets/{RootFolderName}";
            }

            return "Assets";
        }

        public static string GetScriptableObjectsFolder()
        {
            string root = GetRootFolder();
            string target = $"{root}/{ScriptableObjectsFolderName}";
            EnsureFolderExists(target);
            return target;
        }

        private static string TryResolveRootFromGuids(string[] guids)
        {
            if (guids is not { Length: > 0 })
            {
                return null;
            }

            foreach (string guid in guids)
            {
                if (TryGetRootFromGuid(guid, out string root))
                {
                    return root;
                }
            }

            return null;
        }

        private static bool TryGetRootFromGuid(string guid, out string root)
        {
            root = null;
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            string directory = NormalizePath(Path.GetDirectoryName(assetPath));
            root = FindRootFolderInAncestors(directory);
            return !string.IsNullOrEmpty(root);
        }

        private static string FindRootFolderInAncestors(string directory)
        {
            while (!string.IsNullOrEmpty(directory) && directory.StartsWith("Assets"))
            {
                string folderName = Path.GetFileName(directory);
                if (folderName == RootFolderName)
                {
                    return directory;
                }

                directory = NormalizePath(Path.GetDirectoryName(directory));
            }

            return null;
        }

        private static void EnsureFolderExists(string folderPath)
        {
            folderPath = NormalizePath(folderPath);
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = NormalizePath(Path.GetDirectoryName(folderPath));
            string leaf = Path.GetFileName(folderPath);

            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                return;
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}