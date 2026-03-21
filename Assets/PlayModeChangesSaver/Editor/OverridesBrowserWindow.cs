using System;
using System.Collections.Generic;
using PlayModeChangesSaver.Editor.ChangesTracker;
using PlayModeChangesSaver.Editor.OverrideComparePopup;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlayModeChangesSaver.Editor
{
    public class OverridesBrowserWindow : EditorWindow
    {
        private readonly Dictionary<int, bool> gameObjectFoldouts = new();

        private readonly Dictionary<Scene, List<GameObjectEntry>> sceneEntries = new();
        private readonly Dictionary<Scene, bool> sceneFoldouts = new();
        private Vector2 scroll;

        private void OnEnable()
        {
            RefreshData();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (DrawEmptyState())
            {
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawSceneEntries();
            EditorGUILayout.EndScrollView();
        }

        private void OnHierarchyChange()
        {
            RefreshData();
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        [MenuItem("Tools/Play Mode Overrides Browser")]
        public static void Open()
        {
            var window = GetWindow<OverridesBrowserWindow>("Play Mode Overrides");
            window.RefreshData();
            window.Show();
        }

        private void RefreshData()
        {
            sceneEntries.Clear();
            sceneFoldouts.Clear();
            if (Application.isPlaying)
            {
                RefreshDataPlayMode();
                return;
            }

            RefreshDataEditMode();
        }

        private void RefreshDataPlayMode()
        {
            ForEachTransformChange(AddChangeToEntries);
            ForEachComponentChange(AddChangeToEntries);
            ForEachNameChange(MarkNameChanged);
        }

        private void RefreshDataEditMode()
        {
            var sceneMap = new Dictionary<Scene, Dictionary<GameObject, GameObjectEntry>>();

            ForEachTransformChange(target => AddChangeToSceneMap(sceneMap, target));
            ForEachComponentChange(target => AddChangeToSceneMap(sceneMap, target));
            ForEachNameChange(address => MarkNameChangedInSceneMap(sceneMap, address));

            foreach (var kvp in sceneMap)
            {
                var scene = kvp.Key;
                var goDict = kvp.Value;
                var list = new List<GameObjectEntry>(goDict.Values);
                if (list.Count > 0)
                {
                    sceneEntries[scene] = list;
                    sceneFoldouts[scene] = true;
                }
            }
        }

        private static void ForEachTransformChange(Action<ChangeTarget> onChange)
        {
            var transformStore = ChangesStore.LoadExisting();
            if (!transformStore)
            {
                return;
            }

            foreach (var change in transformStore.changes)
            {
                if (!TryBuildTransformTarget(change, out var target))
                {
                    continue;
                }

                onChange(target);
            }
        }

        private static void ForEachComponentChange(Action<ChangeTarget> onChange)
        {
            var compStore = CompChangesStore.LoadExisting();
            if (!compStore)
            {
                return;
            }

            foreach (var change in compStore.changes)
            {
                if (!TryBuildComponentTarget(change, out var target))
                {
                    continue;
                }

                onChange(target);
            }
        }

        private static void ForEachNameChange(Action<ChangeAddress> onChange)
        {
            var nameStore = NameChangesStore.LoadExisting();
            if (!nameStore)
            {
                return;
            }

            foreach (var change in nameStore.changes)
            {
                onChange(new ChangeAddress(change.scenePath, change.globalObjectId, change.objectPath));
            }
        }

        private static bool TryBuildTransformTarget(ChangesStore.TransformChange change, out ChangeTarget target)
        {
            target = default;
            var address = new ChangeAddress(change.scenePath, change.globalObjectId, change.objectPath);
            if (!TryResolveGameObject(address, out var scene, out var go))
            {
                return false;
            }

            target = new ChangeTarget
            {
                Scene = scene,
                Guid = change.globalObjectId,
                GameObject = go,
                Component = go.transform
            };
            return true;
        }

        private static bool TryBuildComponentTarget(CompChangesStore.ComponentChange change, out ChangeTarget target)
        {
            target = default;
            var address = new ChangeAddress(change.scenePath, change.globalObjectId, change.objectPath);
            if (!TryResolveGameObject(address, out var scene, out var go))
            {
                return false;
            }

            var type = Type.GetType(change.componentType);
            if (type == null)
            {
                return false;
            }

            var allComps = go.GetComponents(type);
            if (change.componentIndex < 0 || change.componentIndex >= allComps.Length)
            {
                return false;
            }

            var component = allComps[change.componentIndex];
            if (!component)
            {
                return false;
            }

            target = new ChangeTarget
            {
                Scene = scene,
                Guid = change.globalObjectId,
                GameObject = go,
                Component = component
            };
            return true;
        }

        private static bool TryResolveGameObject(ChangeAddress address, out Scene scene, out GameObject go)
        {
            scene = GetSceneByPathOrName(address.ScenePath);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                go = null;
                return false;
            }

            go = SceneAndPathUtilities.FindGameObjectByGuidOrPath(scene, address.GlobalObjectId, address.ObjectPath);
            return go;
        }

        private void AddChangeToEntries(ChangeTarget target)
        {
            var entry = GetOrCreateEntry(target.Scene, target.Guid, target.GameObject);
            if (!entry.ChangedComponents.Contains(target.Component))
            {
                entry.ChangedComponents.Add(target.Component);
            }
        }

        private void MarkNameChanged(ChangeAddress address)
        {
            if (!TryResolveGameObject(address, out var scene, out var go))
            {
                return;
            }

            var entry = GetOrCreateEntry(scene, address.GlobalObjectId, go);
            entry.NameChanged = true;
        }

        private static void AddChangeToSceneMap(Dictionary<Scene, Dictionary<GameObject, GameObjectEntry>> sceneMap,
            ChangeTarget target)
        {
            if (!sceneMap.TryGetValue(target.Scene, out var goDict))
            {
                goDict = new Dictionary<GameObject, GameObjectEntry>();
                sceneMap[target.Scene] = goDict;
            }

            if (!goDict.TryGetValue(target.GameObject, out var entry))
            {
                entry = new GameObjectEntry { Guid = target.Guid, GameObject = target.GameObject };
                goDict[target.GameObject] = entry;
            }

            if (!entry.ChangedComponents.Contains(target.Component))
            {
                entry.ChangedComponents.Add(target.Component);
            }
        }

        private static void MarkNameChangedInSceneMap(
            Dictionary<Scene, Dictionary<GameObject, GameObjectEntry>> sceneMap, ChangeAddress address)
        {
            if (!TryResolveGameObject(address, out var scene, out var go))
            {
                return;
            }

            if (!sceneMap.TryGetValue(scene, out var goDict))
            {
                goDict = new Dictionary<GameObject, GameObjectEntry>();
                sceneMap[scene] = goDict;
            }

            if (!goDict.TryGetValue(go, out var entry))
            {
                entry = new GameObjectEntry { Guid = address.GlobalObjectId, GameObject = go };
                goDict[go] = entry;
            }

            entry.NameChanged = true;
        }

        private GameObjectEntry GetOrCreateEntry(Scene scene, string guid, GameObject go)
        {
            if (!sceneEntries.TryGetValue(scene, out var list))
            {
                list = new List<GameObjectEntry>();
                sceneEntries[scene] = list;
                sceneFoldouts[scene] = true;
            }

            var entry = list.Find(e =>
                (!string.IsNullOrEmpty(guid) && e.Guid == guid) ||
                (string.IsNullOrEmpty(guid) && e.GameObject == go));
            if (entry != null)
            {
                return entry;
            }

            entry = new GameObjectEntry { Guid = guid, GameObject = go };
            list.Add(entry);
            return entry;
        }

        private static Scene GetSceneByPathOrName(string scenePath)
        {
            var scene = SceneManager.GetSceneByPath(scenePath);
            if (!scene.IsValid())
            {
                scene = SceneManager.GetSceneByName(scenePath);
            }

            return scene;
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh", GUILayout.Width(80)))
                {
                    RefreshData();
                }

                if (GUILayout.Button("Clear", GUILayout.Width(80)))
                {
                    ClearStores();
                    RefreshData();
                }
            }
        }

        private static void ClearStores()
        {
            var tStore = ChangesStore.LoadExisting();
            if (tStore)
            {
                tStore.Clear();
            }

            var cStore = CompChangesStore.LoadExisting();
            if (cStore)
            {
                cStore.Clear();
            }

            var nameStore = NameChangesStore.LoadExisting();
            if (nameStore)
            {
                nameStore.Clear();
            }

            var nameOriginalStore = NameOriginalStore.LoadExisting();
            if (nameOriginalStore)
            {
                nameOriginalStore.Clear();
            }
        }

        private bool DrawEmptyState()
        {
            if (sceneEntries.Count > 0)
            {
                return false;
            }

            EditorGUILayout.HelpBox("No changed components found", MessageType.Info);
            return true;
        }

        private void DrawSceneEntries()
        {
            foreach (var kvp in sceneEntries)
            {
                var scene = kvp.Key;
                var entries = kvp.Value;

                var expanded = sceneFoldouts.GetValueOrDefault(scene, true);
                expanded = EditorGUILayout.Foldout(expanded, scene.name, true);
                sceneFoldouts[scene] = expanded;

                if (!expanded)
                {
                    continue;
                }

                EditorGUI.indentLevel++;
                foreach (var entry in entries)
                {
                    DrawGameObjectEntry(entry);
                }

                EditorGUI.indentLevel--;
            }
        }

        private void DrawGameObjectEntry(GameObjectEntry entry)
        {
            if (!entry.GameObject)
            {
                return;
            }

            int id = entry.GameObject.GetInstanceID();
            var goExpanded = gameObjectFoldouts.GetValueOrDefault(id, true);

            EditorGUILayout.BeginHorizontal();
            goExpanded = EditorGUILayout.Foldout(goExpanded, entry.GameObject.name, true);
            EditorGUILayout.ObjectField(entry.GameObject, typeof(GameObject), true);
            EditorGUILayout.EndHorizontal();

            gameObjectFoldouts[id] = goExpanded;

            if (!goExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;
            if (entry.NameChanged)
            {
                DrawNameRow(entry.GameObject);
            }

            foreach (var comp in entry.ChangedComponents)
            {
                DrawComponentRow(comp);
            }

            EditorGUI.indentLevel--;
        }

        private static void DrawNameRow(GameObject go)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);

            Rect objectRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.LabelField(objectRect, "GameObject Name", EditorStyles.label);
            }

            if (GUI.Button(objectRect, GUIContent.none, GUIStyle.none))
            {
                Rect popupRect = new(objectRect.x, objectRect.yMax, objectRect.width, 0f);
                PopupWindow.Show(popupRect, new NameOcpContent(go));
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawComponentRow(Component comp)
        {
            if (!comp)
            {
                return;
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);

            Rect objectRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.ObjectField(objectRect, comp, typeof(Component), true);
            }

            if (GUI.Button(objectRect, GUIContent.none, GUIStyle.none))
            {
                Rect popupRect = new(objectRect.x, objectRect.yMax, objectRect.width, 0f);
                PopupWindow.Show(popupRect, new OcpContent(comp, true));
            }

            EditorGUILayout.EndHorizontal();
        }

        private readonly struct ChangeAddress
        {
            public readonly string ScenePath;
            public readonly string GlobalObjectId;
            public readonly string ObjectPath;

            public ChangeAddress(string scenePath, string globalObjectId, string objectPath)
            {
                ScenePath = scenePath;
                GlobalObjectId = globalObjectId;
                ObjectPath = objectPath;
            }
        }

        private struct ChangeTarget
        {
            public Scene Scene;
            public string Guid;
            public GameObject GameObject;
            public Component Component;
        }

        private class GameObjectEntry
        {
            public readonly List<Component> ChangedComponents = new();
            public GameObject GameObject;
            public string Guid;
            public bool NameChanged;
        }
    }
}