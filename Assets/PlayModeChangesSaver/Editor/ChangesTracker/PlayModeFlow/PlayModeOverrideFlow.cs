using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayModeChangesSaver.Editor.ChangesTracker.PlayModeFlow
{
    public static class PlayModeOverrideFlow
    {
        public static bool IsProcessing { get; private set; }

        public static void StopProcessing()
        {
            IsProcessing = false;
        }

        public static void HandleApplyChangesFromStoreOnPlayExit()
        {
            if (ShouldSkipProcessing())
            {
                return;
            }

            var stores = LoadStores();
            if (!HasAnyChanges(stores))
            {
                return;
            }

            IsProcessing = true;

            LogEntry(stores);

            var orderedScenePaths = BuildOrderedSceneList(stores);
            string startScenePath = GetActiveScenePath();

            var applyContext = new SceneApplyProcessor.SceneApplyContext(startScenePath, stores.transformStore,
                stores.compStore, stores.nameStore);
            SceneApplyProcessor.ProcessNextSceneInQueue(orderedScenePaths, applyContext);
        }

        private static bool ShouldSkipProcessing()
        {
            return Application.isPlaying || IsProcessing;
        }

        private static (ChangesStore transformStore, CompChangesStore compStore, NameChangesStore nameStore)
            LoadStores()
        {
            return (
                ChangesStore.LoadExisting(),
                CompChangesStore.LoadExisting(),
                NameChangesStore.LoadExisting()
            );
        }

        private static bool HasAnyChanges(
            (ChangesStore transformStore, CompChangesStore compStore, NameChangesStore nameStore) stores)
        {
            return HasTransformChanges(stores.transformStore) ||
                   HasComponentChanges(stores.compStore) ||
                   HasNameChanges(stores.nameStore);
        }

        private static bool HasTransformChanges(ChangesStore store)
        {
            return store != null && store.changes != null && store.changes.Count > 0;
        }

        private static bool HasComponentChanges(CompChangesStore store)
        {
            return store != null && store.changes != null && store.changes.Count > 0;
        }

        private static bool HasNameChanges(NameChangesStore store)
        {
            return store != null && store.changes != null && store.changes.Count > 0;
        }

        private static void LogEntry(
            (ChangesStore transformStore, CompChangesStore compStore, NameChangesStore nameStore) stores)
        {
        }

        private static List<string> BuildOrderedSceneList(
            (ChangesStore transformStore, CompChangesStore compStore, NameChangesStore nameStore) stores)
        {
            var allScenePaths = new HashSet<string>();

            if (HasTransformChanges(stores.transformStore))
            {
                AddScenePaths(allScenePaths, stores.transformStore.changes.Select(c => c.scenePath));
            }

            if (HasComponentChanges(stores.compStore))
            {
                AddScenePaths(allScenePaths, stores.compStore.changes.Select(c => c.scenePath));
            }

            if (HasNameChanges(stores.nameStore))
            {
                AddScenePaths(allScenePaths, stores.nameStore.changes.Select(c => c.scenePath));
            }

            string startScenePath = GetActiveScenePath();
            var ordered = new List<string>(allScenePaths);

            if (!string.IsNullOrEmpty(startScenePath) && ordered.Contains(startScenePath))
            {
                ordered.Remove(startScenePath);
                ordered.Insert(0, startScenePath);
            }

            return ordered;
        }

        private static void AddScenePaths(HashSet<string> set, IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                var normalized = SceneAndPathUtilities.NormalizeScenePath(path);
                if (!string.IsNullOrEmpty(normalized))
                {
                    set.Add(normalized);
                }
            }
        }

        private static string GetActiveScenePath()
        {
            var startScene = SceneManager.GetActiveScene();
            return startScene.IsValid() ? SceneAndPathUtilities.NormalizeScenePath(startScene.path) : null;
        }
    }
}