using System;
using PlayModeChangesSaver.Editor.ChangesTracker.PlayModeFlow;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayModeChangesSaver.Editor.ChangesTracker.Lifecycle
{
    [InitializeOnLoad]
    public static class PlayModeLifecycleManager
    {
        private const string PrefsKey = "PlayModeChangesTracker_CaptureNeeded";
        private static string _startScenePathAtPlayEnter;

        static PlayModeLifecycleManager()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnEditorUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            bool needsCapture = EditorPrefs.GetBool(PrefsKey, false);
            if (!needsCapture)
            {
                return;
            }

            EditorPrefs.DeleteKey(PrefsKey);
            EditorApplication.delayCall += SnapshotManager.CaptureSnapshots;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    HandleExitingEditMode();
                    break;

                case PlayModeStateChange.EnteredPlayMode:
                    EditorPrefs.SetBool(PrefsKey, true);
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    EditorPrefs.DeleteKey(PrefsKey);
                    HandlePlayExitFlow();
                    break;
            }
        }

        private static void HandleExitingEditMode()
        {
            ChangesTrackerCore.ClearStoresOnPlayEnter();
            var startScene = SceneManager.GetActiveScene();
            _startScenePathAtPlayEnter = startScene.IsValid() ? startScene.path : null;

            SnapshotManager.CaptureSnapshots();
            EditorPrefs.SetBool(PrefsKey, true);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!RuntimeOverrideApplicator.CanApplyRuntimeOverrides(scene))
            {
                return;
            }

            RuntimeOverrideApplicator.ApplyRuntimeOverridesForScene(scene);

            EditorApplication.delayCall += SnapshotManager.CaptureSnapshots;
        }

        private static void HandlePlayExitFlow()
        {
            if (Application.isPlaying)
            {
                return;
            }

            if (!IsPlayExitSceneValid())
            {
                return;
            }

            EditorApplication.delayCall += TriggerApplyFlowAfterPlayExit;
        }

        private static bool IsPlayExitSceneValid()
        {
            var activeAfterPlay = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(_startScenePathAtPlayEnter))
            {
                _startScenePathAtPlayEnter = activeAfterPlay.IsValid() ? activeAfterPlay.path : null;
            }

            var activePathNow = activeAfterPlay.IsValid()
                ? SceneAndPathUtilities.NormalizeScenePath(activeAfterPlay.path)
                : string.Empty;
            var startPathNow = SceneAndPathUtilities.NormalizeScenePath(_startScenePathAtPlayEnter);

            if (string.IsNullOrEmpty(startPathNow) ||
                !string.Equals(activePathNow, startPathNow, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        private static bool IsApplyFlowInProgress()
        {
            return Application.isPlaying || PlayModeOverrideFlow.IsProcessing;
        }

        private static bool IsSceneNotReady(Scene scene)
        {
            return !scene.isLoaded || EditorApplication.isCompiling || EditorApplication.isUpdating;
        }

        private static void TriggerApplyFlowAfterPlayExit()
        {
            if (IsApplyFlowInProgress())
            {
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (IsSceneNotReady(activeScene))
            {
                EditorApplication.delayCall += TriggerApplyFlowAfterPlayExit;
                return;
            }

            EditorApplication.delayCall += ApplyFlowWhenStable;
        }

        private static void ApplyFlowWhenStable()
        {
            if (IsApplyFlowInProgress())
            {
                return;
            }

            PlayModeOverrideFlow.HandleApplyChangesFromStoreOnPlayExit();
        }
    }
}