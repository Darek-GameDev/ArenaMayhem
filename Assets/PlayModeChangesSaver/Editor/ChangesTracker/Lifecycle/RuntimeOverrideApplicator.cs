using System.Collections.Generic;
using PlayModeChangesSaver.Editor.ChangesTracker.App;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayModeChangesSaver.Editor.ChangesTracker.Lifecycle
{
    public static class RuntimeOverrideApplicator
    {
        public static bool CanApplyRuntimeOverrides(Scene scene)
        {
            if (!Application.isPlaying)
            {
                return false;
            }

            if (!scene.isLoaded)
            {
                return false;
            }

            return true;
        }

        public static void ApplyRuntimeOverridesForScene(Scene scene)
        {
            ApplyTransformOverridesForScene(scene);
            ApplyComponentOverridesForScene(scene);
        }

        private static bool IsStoreValid<T>(List<T> list) where T : class
        {
            return list is { Count: > 0 };
        }

        private static bool IsSceneValid(Scene sceneToCheck, Scene expectedScene)
        {
            return sceneToCheck.IsValid() && sceneToCheck == expectedScene;
        }

        private static Scene GetValidScene(string scenePath)
        {
            var changeScene = SceneManager.GetSceneByPath(scenePath);
            return changeScene.IsValid() ? changeScene : SceneManager.GetSceneByName(scenePath);
        }

        private static void ApplyTransformOverridesForScene(Scene scene)
        {
            var transformStore = ChangesStore.LoadExisting();
            if (!transformStore)
            {
                return;
            }

            if (!IsStoreValid(transformStore.changes))
            {
                return;
            }

            foreach (var change in transformStore.changes)
            {
                var changeScene = GetValidScene(change.scenePath);
                if (!IsSceneValid(changeScene, scene))
                {
                    continue;
                }

                GameObject go =
                    SceneAndPathUtilities.FindGameObjectByGuidOrPath(scene, change.globalObjectId, change.objectPath);
                if (!go)
                {
                    continue;
                }

                TransformChangeApplicator.ApplyTransformChange(go, change);
            }
        }

        private static void ApplyComponentOverridesForScene(Scene scene)
        {
            var compStore = CompChangesStore.LoadExisting();
            if (!compStore)
            {
                return;
            }

            if (!IsStoreValid(compStore.changes))
            {
                return;
            }

            foreach (var change in compStore.changes)
            {
                var changeScene = GetValidScene(change.scenePath);
                if (!IsSceneValid(changeScene, scene))
                {
                    continue;
                }

                GameObject go =
                    SceneAndPathUtilities.FindGameObjectByGuidOrPath(scene, change.globalObjectId, change.objectPath);
                if (!go)
                {
                    continue;
                }

                ComponentChangeApplicator.ApplyComponentChange(go, change);
            }
        }
    }
}