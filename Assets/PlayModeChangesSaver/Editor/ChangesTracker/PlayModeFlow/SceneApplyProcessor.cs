using System;
using System.Collections.Generic;
using System.Linq;
using PlayModeChangesSaver.Editor.ChangesTracker.PlayModeFlow.SceneApplyHelper;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayModeChangesSaver.Editor.ChangesTracker.PlayModeFlow
{
    public static class SceneApplyProcessor
    {
        private static readonly Dictionary<string, Action<Transform, RectTransform, ChangesStore.TransformChange>>
            TransformPropertyAppliers =
                new()
                {
                    { "position", (t, _, change) => t.localPosition = change.position },
                    { "rotation", (t, _, change) => t.localRotation = change.rotation },
                    { "scale", (t, _, change) => t.localScale = change.scale },
                    {
                        "anchoredPosition", (_, rt, change) =>
                        {
                            if (rt)
                            {
                                rt.anchoredPosition = change.anchoredPosition;
                            }
                        }
                    },
                    {
                        "anchoredPosition3D", (_, rt, change) =>
                        {
                            if (rt)
                            {
                                rt.anchoredPosition3D = change.anchoredPosition3D;
                            }
                        }
                    },
                    {
                        "anchorMin", (_, rt, change) =>
                        {
                            if (rt)
                            {
                                rt.anchorMin = change.anchorMin;
                            }
                        }
                    },
                    {
                        "anchorMax", (_, rt, change) =>
                        {
                            if (rt)
                            {
                                rt.anchorMax = change.anchorMax;
                            }
                        }
                    },
                    {
                        "pivot", (_, rt, change) =>
                        {
                            if (rt)
                            {
                                rt.pivot = change.pivot;
                            }
                        }
                    },
                    {
                        "sizeDelta", (_, rt, change) =>
                        {
                            if (rt)
                            {
                                rt.sizeDelta = change.sizeDelta;
                            }
                        }
                    },
                    {
                        "offsetMin", (_, rt, change) =>
                        {
                            if (rt)
                            {
                                rt.offsetMin = change.offsetMin;
                            }
                        }
                    },
                    {
                        "offsetMax", (_, rt, change) =>
                        {
                            if (rt)
                            {
                                rt.offsetMax = change.offsetMax;
                            }
                        }
                    }
                };


        public static void ProcessNextSceneInQueue(List<string> remainingScenes, SceneApplyContext context)
        {

            if (TryFinishQueue(remainingScenes, context))
            {
                return;
            }

            string currentPath = PopNextScene(remainingScenes);
            string activePath = SceneAndPathUtilities.NormalizeScenePath(SceneManager.GetActiveScene().path);

            if (!IsSameScenePath(activePath, currentPath))
            {
                HandleSceneSwitch(currentPath, remainingScenes, context);
                return;
            }

            PromptApplyOrDiscard(currentPath, remainingScenes, context);
        }

        private static bool TryFinishQueue(List<string> remainingScenes, SceneApplyContext context)
        {
            if (remainingScenes.Count > 0)
            {
                return false;
            }
            CheckReturnToStartScene(context.StartScenePath);
            return true;
        }

        private static string PopNextScene(List<string> remainingScenes)
        {
            string currentPath = remainingScenes[0];
            remainingScenes.RemoveAt(0);
            return currentPath;
        }

        private static void HandleSceneSwitch(string targetPath, List<string> remainingScenes,
            SceneApplyContext context)
        {
            bool switchScene = EditorUtility.DisplayDialog("Scene Switch", $"Switch to scene?\n\n{targetPath}", "Yes",
                "Discard remaining");

            if (!switchScene)
            {
                PlayModeOverrideFlow.StopProcessing();
                return;
            }
            EditorSceneManager.SaveOpenScenes();
            EditorSceneManager.OpenScene(targetPath, OpenSceneMode.Single);

            EditorApplication.delayCall += () =>
            {
                SceneView.RepaintAll();
                var newQueue = new List<string> { targetPath }.Concat(remainingScenes).ToList();
                ProcessNextSceneInQueue(newQueue, context);
            };
        }

        private static void PromptApplyOrDiscard(string currentPath, List<string> remainingScenes,
            SceneApplyContext context)
        {
            string msg = $"Apply play mode overrides for scene?\n\n{currentPath}";

            bool apply = EditorUtility.DisplayDialog("Apply Overrides", msg, "Apply", "Discard");
            if (apply)
            {
                ApplySceneChangesAndContinue(currentPath, remainingScenes, context);
                return;
            }
            ChangesStoreManager.RemoveChangesForSceneFromStore(currentPath, context.TransformStore,
                context.ComponentStore, context.NameStore);
            EditorApplication.delayCall += () => ProcessNextSceneInQueue(remainingScenes, context);
        }

        private static void ApplySceneChangesAndContinue(string currentPath, List<string> remainingScenes,
            SceneApplyContext context)
        {

            ApplyChangesFromStoreToEditModeForScene(currentPath, context.TransformStore, context.ComponentStore);
            ApplyNameChangesFromStoreToEditModeForScene(currentPath);

            EditorApplication.delayCall += () =>
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorApplication.delayCall += () =>
                {
                    SceneView.RepaintAll();
                    EditorApplication.DirtyHierarchyWindowSorting();
                    EditorApplication.delayCall += () => ProcessNextSceneInQueue(remainingScenes, context);
                };
            };
        }

        private static bool IsSameScenePath(string a, string b)
        {
            return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
        }

        public static void ApplyChangesFromStoreToEditModeForScene(string targetScenePath, ChangesStore transformStore,
            CompChangesStore compStore)
        {
            string normalizedTarget = SceneAndPathUtilities.NormalizeScenePath(targetScenePath);

            var commands = new List<IApplyCommand>();
            var tStore = transformStore != null ? transformStore : ChangesStore.LoadExisting();
            var cStore = compStore != null ? compStore : CompChangesStore.LoadExisting();

            if (tStore != null)
            {
                BuildTransformCommands(normalizedTarget, tStore, commands);
            }

            if (cStore != null)
            {
                BuildComponentCommands(normalizedTarget, cStore, commands);
            }

            ExecuteCommands(commands);
        }

        private static void BuildTransformCommands(string targetScenePath, ChangesStore store,
            List<IApplyCommand> commands)
        {
            foreach (var change in store.changes)
            {
                var request = new StoreChangeRequest(
                    targetScenePath,
                    change.scenePath,
                   // () => Debug.Log($"[Transform] Begin scene='{SceneAndPathUtilities.NormalizeScenePath(change.scenePath)}' guid='{change.globalObjectId}' path='{change.objectPath}' props={string.Join(",", change.modifiedProperties ?? new List<string>())}"),
                    () => { }, //remove line if Debug enabled
                    scene => commands.Add(new ApplyTransformChangeCommand(scene, change)));
                ProcessStoreChange(request);
            }
        }

        private static void BuildComponentCommands(string targetScenePath, CompChangesStore store,
            List<IApplyCommand> commands)
        {
            foreach (var change in store.changes)
            {
                var request = new StoreChangeRequest(
                    targetScenePath,
                    change.scenePath,
                    //() => Debug.Log($" Begin scene='{SceneAndPathUtilities.NormalizeScenePath(change.scenePath)}' guid='{change.globalObjectId}' path='{change.objectPath}' type='{change.componentType}' idx={change.componentIndex}"),
                    () => { }, //remove line if Debug enabled
                    scene => commands.Add(new ApplyComponentChangeCommand(scene, change)));
                ProcessStoreChange(request);
            }
        }

        private static void ProcessStoreChange(StoreChangeRequest request)
        {
            string normalizedChangePath = SceneAndPathUtilities.NormalizeScenePath(request.ChangeScenePath);
            if (!IsSameScenePath(normalizedChangePath, request.TargetScenePath))
            {
                return;
            }

            request.LogBegin();

            if (!TryGetScene(normalizedChangePath, request.ChangeScenePath, out var scene))
            {
                return;
            }

            request.AddCommand(scene);
        }

        private static bool TryGetScene(string normalizedPath, string fallbackName, out Scene scene)
        {
            scene = SceneManager.GetSceneByPath(normalizedPath);
            if (scene.IsValid())
            {
                return true;
            }

            scene = SceneManager.GetSceneByName(fallbackName);
            if (scene.IsValid())
            {
                return true;
            }
            return false;
        }

        private static void ExecuteCommands(IEnumerable<IApplyCommand> commands)
        {
            foreach (var command in commands)
            {
                command.Execute();
            }
        }

        public static void ApplyPropertyToTransform(Transform t, RectTransform rt, ChangesStore.TransformChange change,
            string prop)
        {
            if (TransformPropertyAppliers.TryGetValue(prop, out var apply))
            {
                apply(t, rt, change);
            }
        }

        private static void CheckReturnToStartScene(string startPath)
        {
            string currentPath = SceneAndPathUtilities.NormalizeScenePath(SceneManager.GetActiveScene().path);
            if (!string.IsNullOrEmpty(startPath) &&
                !string.Equals(currentPath, startPath, StringComparison.OrdinalIgnoreCase))
            {
                if (EditorUtility.DisplayDialog("Return to start scene?", $"Do you want to return to:\n\n{startPath}",
                        "Yes", "No"))
                {
                    EditorSceneManager.SaveOpenScenes();
                    EditorSceneManager.OpenScene(startPath, OpenSceneMode.Single);

                    ScheduleStartSceneReturnCompletion();
                    return;
                }
            }
            PlayModeOverrideFlow.StopProcessing();
        }

        private static void ScheduleStartSceneReturnCompletion()
        {
            EditorApplication.delayCall += () =>
            {
                EditorApplication.delayCall += () =>
                {
                    EditorApplication.delayCall += () =>
                    {
                        SceneView.RepaintAll();
                        PlayModeOverrideFlow.StopProcessing();
                    };
                };
            };
        }

        public static void ApplyMaterials(Renderer renderer, List<string> materialGuids)
        {
            if (!renderer || materialGuids==null)
            {
                return;
            }

            int targetCount = materialGuids.Count;
            if (targetCount == 0)
            {
                return;
            }

            var current = renderer.sharedMaterials;
            var applied = new Material[targetCount];

            for (int i = 0; i < targetCount; i++)
            {
                applied[i] = ResolveMaterial(materialGuids[i], renderer.name, i < current.Length ? current[i] : null);
            }

            renderer.sharedMaterials = applied;
        }

        private static Material ResolveMaterial(string guid, string rendererName, Material fallback)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material != null)
            {
                return material;
            }
            return fallback;
        }

        public static void ApplyNameChangesFromStoreToEditModeForScene(string targetScenePath)
        {
            targetScenePath = SceneAndPathUtilities.NormalizeScenePath(targetScenePath);

            var nameStore = NameChangesStore.LoadExisting();
            if (!nameStore || nameStore.changes.Count == 0)
            {
                return;
            }

            foreach (var change in nameStore.changes)
            {
                ProcessNameChangeIfInTargetScene(change, targetScenePath);
            }
        }

        private static void ProcessNameChangeIfInTargetScene(NameChangesStore.NameChange change, string targetScenePath)
        {
            var normalizedChangePath = SceneAndPathUtilities.NormalizeScenePath(change.scenePath);
            if (!string.Equals(normalizedChangePath, targetScenePath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (!TryResolveSceneForNameChange(normalizedChangePath, change.scenePath, out var scene))
            {
                return;
            }

            if (!TryFindTargetForNameChange(scene, change, out var go))
            {
                return;
            }

            ApplyNameChange(scene, go, change.newName);
        }

        private static bool TryResolveSceneForNameChange(string normalizedChangePath, string fallbackPath,
            out Scene scene)
        {
            return TryGetScene(normalizedChangePath, fallbackPath, out scene);
        }

        private static bool TryFindTargetForNameChange(Scene scene, NameChangesStore.NameChange change,
            out GameObject go)
        {
            go = SceneAndPathUtilities.FindGameObjectByGuidOrPath(scene, change.globalObjectId, change.objectPath);
            if (go != null)
            {
                return true;
            }
            return false;
        }

        private static void ApplyNameChange(Scene scene, GameObject go, string newName)
        {

            Undo.RecordObject(go, "Apply Play Mode Name Changes");
            go.name = newName;
            EditorUtility.SetDirty(go);
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }

        private readonly struct StoreChangeRequest
        {
            public string TargetScenePath { get; }
            public string ChangeScenePath { get; }
            public Action LogBegin { get; }
            public Action<Scene> AddCommand { get; }

            public StoreChangeRequest(string targetScenePath, string changeScenePath, Action logBegin,
                Action<Scene> addCommand)
            {
                TargetScenePath = targetScenePath;
                ChangeScenePath = changeScenePath;
                LogBegin = logBegin;
                AddCommand = addCommand;
            }
        }

        public sealed class SceneApplyContext
        {
            public SceneApplyContext(string startScenePath, ChangesStore transformStore,
                CompChangesStore componentStore, NameChangesStore nameStore)
            {
                StartScenePath = startScenePath;
                TransformStore = transformStore;
                ComponentStore = componentStore;
                NameStore = nameStore;
            }

            public string StartScenePath { get; }
            public ChangesStore TransformStore { get; }
            public CompChangesStore ComponentStore { get; }
            public NameChangesStore NameStore { get; }
        }

        public interface IApplyCommand
        {
            void Execute();
        }
    }
}