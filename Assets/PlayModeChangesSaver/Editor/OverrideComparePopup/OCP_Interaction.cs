using System;
using System.Collections.Generic;
using PlayModeChangesSaver.Editor.ChangesTracker;
using PlayModeChangesSaver.Editor.ChangesTracker.App;
using PlayModeChangesSaver.Editor.ChangesTracker.SnapShotHelper;
using UnityEditor;
using UnityEngine;

namespace PlayModeChangesSaver.Editor.OverrideComparePopup
{
    /// <summary>
    ///     Handles user interactions: drag-drop, apply, revert buttons.
    /// </summary>
    internal class OcpInteraction
    {
        private const float DragHeaderHeight = 20f;

        private readonly Component _liveComponent;
        private readonly Component _snapshotComponent;
        private Vector2 _dragLastMousePos = Vector2.zero;
        private bool _isDragging;

        public OcpInteraction(Component liveComponent, Component snapshotComponent)
        {
            this._liveComponent = liveComponent;
            this._snapshotComponent = snapshotComponent;
        }

        /// <summary>
        ///     Handles drag-and-drop functionality for moving the popup window.
        /// </summary>
        public void HandleDragAndDrop(Rect rect, EditorWindow editorWindow)
        {
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            Rect dragHeaderRect = new(rect.x, rect.y, rect.width, DragHeaderHeight);

            if (Event.current.type == EventType.MouseDown && dragHeaderRect.Contains(Event.current.mousePosition))
            {
                GUIUtility.hotControl = controlId;
                _isDragging = true;
                _dragLastMousePos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseDrag && _isDragging && GUIUtility.hotControl == controlId)
            {
                Vector2 currentScreenPos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                Vector2 delta = currentScreenPos - _dragLastMousePos;

                Rect newRect = editorWindow.position;
                newRect.position += delta;
                editorWindow.position = newRect;

                _dragLastMousePos = currentScreenPos;
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
            {
                _isDragging = false;
                GUIUtility.hotControl = 0;
                Event.current.Use();
            }

            if (Event.current.type == EventType.Repaint)
            {
                GUI.Box(dragHeaderRect, GUIContent.none, EditorStyles.toolbar);
            }
        }

        /// <summary>
        ///     Reverts all changes made in Play Mode back to the original snapshot state.
        /// </summary>
        public void RevertToOriginal(bool openedFromBrowser = false)
        {
            if (!_snapshotComponent)
            {
                return;
            }

            string goid = GlobalObjectId.GetGlobalObjectIdSlow(_liveComponent.gameObject).ToString();

            SerializedObject sourceSo = new(_snapshotComponent);
            SerializedObject targetSo = new(_liveComponent);

            SerializedProperty sourceProp = sourceSo.GetIterator();
            bool enterChildren = true;

            while (sourceProp.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (sourceProp.name == "m_Script")
                {
                    continue;
                }

                SerializedProperty targetProp = targetSo.FindProperty(sourceProp.propertyPath);
                if (targetProp != null && targetProp.propertyType == sourceProp.propertyType)
                {
                    targetSo.CopyFromSerializedProperty(sourceProp);
                }
            }

            targetSo.ApplyModifiedProperties();

            RestoreSnapshotState(goid);
            ResetBaselinesIfPlaying();
            RemoveFromStore();

            if (openedFromBrowser)
            {
                RefreshBrowserIfOpen();
            }
        }

        private void RestoreSnapshotState(string goid)
        {
            if (_liveComponent is Transform or RectTransform)
            {
                RestoreNameSnapshot();
            }
            else if (_liveComponent is Renderer renderer)
            {
                RestoreRendererMaterials(renderer, goid);
            }
        }

        private void RestoreNameSnapshot()
        {
            var nameSnapshot = Name_SH.GetNameSnapshot(_liveComponent.gameObject);
            if (nameSnapshot != null && !string.IsNullOrEmpty(nameSnapshot.objectName))
            {
                _liveComponent.gameObject.name = nameSnapshot.objectName;
            }
        }

        private void RestoreRendererMaterials(Renderer renderer, string goid)
        {
            if (!renderer)
            {
                return;
            }

            var snapshotRenderer = _snapshotComponent as Renderer;
            var materialGuids = ResolveOriginalMaterialGuids(goid, snapshotRenderer);

            Debug.Log($"[OCP] RestoreRendererMaterials | goid={goid} | comp={renderer.GetType().Name} | guidCount={materialGuids.Count}");

            if (materialGuids is { Count: > 0 })
            {
                ApplyMaterials(renderer, materialGuids);
            }
            else
            {
            }
        }

        private List<string> ResolveOriginalMaterialGuids(string goid, Renderer snapshotRenderer)
        {
            // Prefer the snapshot renderer data (already built from original store).
            var fromSnapshot = MaterialChangeHandler.GetRendererMaterialGuids(snapshotRenderer);
            if (fromSnapshot is { Count: > 0 })
            {
                return fromSnapshot;
            }

            // Fallback: query the original store by GOID (not path) to honor identity even if path changed.
            var store = CompOriginalStore.LoadExisting();
            if (!store)
            {
                return new List<string>();
            }

            var type = _liveComponent.GetType();
            var allOfType = _liveComponent.gameObject.GetComponents(type);
            int compIndex = Array.IndexOf(allOfType, _liveComponent);
            string compType = type.AssemblyQualifiedName;

            var entry = store.entries.Find(e =>
                !string.IsNullOrEmpty(e.globalObjectId) &&
                e.globalObjectId == goid &&
                e.componentType == compType &&
                e.componentIndex == compIndex);

            if (entry != null && entry.materialGuids is { Count: > 0 })
            {
                return new List<string>(entry.materialGuids);
            }
            return new List<string>();
        }

        private void ResetBaselinesIfPlaying()
        {
            if (!Application.isPlaying || !_liveComponent)
            {
                return;
            }

            if (_liveComponent is Transform or RectTransform)
            {
                ChangesTrackerCore.ResetTransformBaseline(_liveComponent.gameObject);
            }
            else
            {
                ChangesTrackerCore.ResetComponentBaseline(_liveComponent);
            }
        }

        /// <summary>
        ///     Reverts all changes made in Play Mode back to the saved store values.
        /// </summary>
        public void RevertToSaved(bool openedFromBrowser = false)
        {
            if (!_liveComponent)
            {
                return;
            }

            var go = _liveComponent.gameObject;
            string scenePath = GetScenePathForGo(go);
            string objectPath = OcpUtilities.GetGameObjectPath(go.transform);
            string goid = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();

            if (_liveComponent is Transform or RectTransform)
            {
                RevertTransformToSaved(go, scenePath, objectPath, goid);
            }
            else
            {
                RevertComponentToSaved(go, scenePath, objectPath, goid);
            }

            if (openedFromBrowser)
            {
                RefreshBrowserIfOpen();
            }
        }

        private void RevertTransformToSaved(GameObject go, string scenePath, string objectPath, string goid)
        {
            var tStore = ChangesStore.LoadExisting();
            if (!tStore)
            {
                return;
            }

            int index = tStore.changes.FindIndex(c =>
                (!string.IsNullOrEmpty(c.globalObjectId) && c.globalObjectId == goid) ||
                (string.IsNullOrEmpty(c.globalObjectId) && c.scenePath == scenePath && c.objectPath == objectPath));
            if (index < 0)
            {
                return;
            }

            var storedChange = tStore.changes[index];
            Transform t = go.transform;

            ApplyTransformValues(t, storedChange);
            ApplyRectTransformValues(t as RectTransform, storedChange);

            if (Application.isPlaying)
            {
                ChangesTrackerCore.ResetTransformBaseline(go);
            }
        }

        private void ApplyTransformValues(Transform t, ChangesStore.TransformChange storedChange)
        {
            t.SetLocalPositionAndRotation(storedChange.position, storedChange.rotation);
            t.localScale = storedChange.scale;
        }

        private void ApplyRectTransformValues(RectTransform rt, ChangesStore.TransformChange storedChange)
        {
            if (storedChange.isRectTransform && rt != null)
            {
                rt.anchoredPosition = storedChange.anchoredPosition;
                rt.anchoredPosition3D = storedChange.anchoredPosition3D;
                rt.anchorMin = storedChange.anchorMin;
                rt.anchorMax = storedChange.anchorMax;
                rt.pivot = storedChange.pivot;
                rt.sizeDelta = storedChange.sizeDelta;
                rt.offsetMin = storedChange.offsetMin;
                rt.offsetMax = storedChange.offsetMax;
            }
        }

        private void RevertComponentToSaved(GameObject go, string scenePath, string objectPath, string goid)
        {
            var cStore = CompChangesStore.LoadExisting();
            if (!cStore)
            {
                return;
            }

            int index = FindComponentChangeIndex(go, scenePath, objectPath, goid, cStore);
            if (index < 0)
            {
                return;
            }

            var storedChange = cStore.changes[index];
            ApplyStoredComponentProperties(storedChange);
            ApplyStoredMaterialChanges(storedChange);
            ResetComponentBaselineIfPlaying();
        }

        private int FindComponentChangeIndex(GameObject go, string scenePath, string objectPath,
            string goid, CompChangesStore cStore)
        {
            return FindComponentIndexInStore(go, scenePath, objectPath, goid, cStore);
        }

        private int FindComponentIndexInStore(GameObject go, string scenePath, string objectPath,
            string goid, CompChangesStore cStore)
        {
            var type = _liveComponent.GetType();
            string componentType = type.AssemblyQualifiedName;
            var allOfType = go.GetComponents(type);
            int compIndex = Array.IndexOf(allOfType, _liveComponent);

            return cStore.changes.FindIndex(c =>
                ((!string.IsNullOrEmpty(c.globalObjectId) && c.globalObjectId == goid) ||
                 (string.IsNullOrEmpty(c.globalObjectId) && c.scenePath == scenePath && c.objectPath == objectPath)) &&
                c.componentType == componentType &&
                c.componentIndex == compIndex);
        }

        private void ApplyStoredComponentProperties(CompChangesStore.ComponentChange storedChange)
        {
            var targetSo = new SerializedObject(_liveComponent);

            for (int i = 0; i < storedChange.propertyPaths.Count; i++)
            {
                string propPath = storedChange.propertyPaths[i];
                var prop = targetSo.FindProperty(propPath);
                if (prop != null)
                {
                    OcpSerialization.ApplySerializedComponentValue(prop, storedChange.valueTypes[i],
                        storedChange.serializedValues[i]);
                }
            }

            targetSo.ApplyModifiedProperties();
        }

        private void ApplyStoredMaterialChanges(CompChangesStore.ComponentChange storedChange)
        {
            if (storedChange.includeMaterialChanges && _liveComponent is Renderer renderer)
            {
                ApplyMaterials(renderer, storedChange.materialGuids);
            }
        }

        private void ResetComponentBaselineIfPlaying()
        {
            if (Application.isPlaying)
            {
                ChangesTrackerCore.ResetComponentBaseline(_liveComponent);
            }
        }

        /// <summary>
        ///     Reverts all changes made in Play Mode back to the snapshot state.
        /// </summary>
        [Obsolete("Use RevertToOriginal instead")]
        public void RevertChanges(bool openedFromBrowser = false)
        {
            RevertToOriginal(openedFromBrowser);
        }

        /// <summary>
        ///     Applies the current Play Mode changes to the acceptance system.
        /// </summary>
        public void ApplyChanges(bool openedFromBrowser = false)
        {
            if (_liveComponent is Transform or RectTransform)
            {
                ChangesTrackerCore.AcceptTransformChanges(_liveComponent.gameObject);
            }
            else
            {
                ChangesTrackerCore.AcceptComponentChanges(_liveComponent);
            }

            // Force editor update to reflect the changes
            EditorUtility.SetDirty(_liveComponent);

            if (openedFromBrowser)
            {
                RefreshBrowserIfOpen();
            }
        }

        /// <summary>
        ///     Checks if there are unsaved changes compared to the store.
        ///     Returns true if the current live component has changes that are NOT yet saved in the store,
        ///     or if the current values differ from the stored values.
        /// </summary>
        public bool HasUnsavedChanges()
        {
            return ExecuteComponentQuery(
                HasUnsavedTransformChanges,
                HasUnsavedComponentChanges,
                false);
        }

        private TResult ExecuteComponentQuery<TResult>(
            Func<GameObject, string, string, TResult> transformAction,
            Func<GameObject, string, string, TResult> componentAction,
            TResult defaultValue)
        {
            if (!_liveComponent)
            {
                return defaultValue;
            }

            var go = _liveComponent.gameObject;
            string scenePath = GetScenePathForGo(go);
            string objectPath = OcpUtilities.GetGameObjectPath(go.transform);

            if (_liveComponent is Transform or RectTransform)
            {
                return transformAction(go, scenePath, objectPath);
            }

            return componentAction(go, scenePath, objectPath);
        }

        private bool HasUnsavedTransformChanges(GameObject go, string scenePath, string objectPath)
        {
            var tStore = ChangesStore.LoadExisting();
            if (!tStore)
            {
                return true; 
            }

            string goid = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();
            int index = tStore.changes.FindIndex(c =>
                (!string.IsNullOrEmpty(c.globalObjectId) && c.globalObjectId == goid) ||
                (string.IsNullOrEmpty(c.globalObjectId) && c.scenePath == scenePath && c.objectPath == objectPath));
            if (index < 0)
            {
                return true; 
            }

            var storedChange = tStore.changes[index];
            Transform t = go.transform;
            RectTransform rt = t as RectTransform;

            return TransformValuesChanged(storedChange, t, rt); 
        }

        private bool TransformValuesChanged(ChangesStore.TransformChange storedChange, Transform t, RectTransform rt)
        {
            if (RectTransformValuesChanged(storedChange, rt))
            {
                return true;
            }

            if (PositionChanged(storedChange, t))
            {
                return true;
            }

            if (RotationChanged(storedChange, t))
            {
                return true;
            }

            if (ScaleChanged(storedChange, t))
            {
                return true;
            }

            return false;
        }

        private bool RectTransformValuesChanged(ChangesStore.TransformChange storedChange, RectTransform rt)
        {
            if (!storedChange.isRectTransform || !rt)
            {
                return false;
            }

            return Vector2Changed(storedChange.anchoredPosition, rt.anchoredPosition) ||
                   Vector2Changed(storedChange.sizeDelta, rt.sizeDelta) ||
                   Vector2Changed(storedChange.anchorMin, rt.anchorMin) ||
                   Vector2Changed(storedChange.anchorMax, rt.anchorMax) ||
                   Vector2Changed(storedChange.pivot, rt.pivot);
        }

        private bool PositionChanged(ChangesStore.TransformChange storedChange, Transform t)
        {
            return Vector3Changed(storedChange.position, t.localPosition);
        }

        private bool RotationChanged(ChangesStore.TransformChange storedChange, Transform t)
        {
            return QuaternionChanged(storedChange.rotation, t.localRotation);
        }

        private bool ScaleChanged(ChangesStore.TransformChange storedChange, Transform t)
        {
            return Vector3Changed(storedChange.scale, t.localScale);
        }

        private bool Vector2Changed(Vector2 stored, Vector2 current)
        {
            return !Mathf.Approximately(stored.x, current.x) ||
                   !Mathf.Approximately(stored.y, current.y);
        }

        private bool Vector3Changed(Vector3 stored, Vector3 current)
        {
            return !Mathf.Approximately(stored.x, current.x) ||
                   !Mathf.Approximately(stored.y, current.y) ||
                   !Mathf.Approximately(stored.z, current.z);
        }

        private bool QuaternionChanged(Quaternion stored, Quaternion current)
        {
            return !Mathf.Approximately(stored.x, current.x) ||
                   !Mathf.Approximately(stored.y, current.y) ||
                   !Mathf.Approximately(stored.z, current.z) ||
                   !Mathf.Approximately(stored.w, current.w);
        }

        private bool HasUnsavedComponentChanges(GameObject go, string scenePath, string objectPath)
        {
            var cStore = CompChangesStore.LoadExisting();
            if (!cStore)
            {
                return true; 
            }

            string goid = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();
            int index = FindStoredComponentChangeIndex(go, scenePath, objectPath, goid, cStore);
            if (index < 0)
            {
                return true; 
            }

            var storedChange = cStore.changes[index];
            return ComponentPropertiesDifferFromStored(storedChange);
        }

        private int FindStoredComponentChangeIndex(GameObject go, string scenePath, string objectPath,
            string goid, CompChangesStore cStore)
        {
            return FindComponentIndexInStore(go, scenePath, objectPath, goid, cStore);
        }

        private bool ComponentPropertiesDifferFromStored(CompChangesStore.ComponentChange storedChange)
        {
            var liveSo = new SerializedObject(_liveComponent);

            for (int i = 0; i < storedChange.propertyPaths.Count; i++)
            {
                string propPath = storedChange.propertyPaths[i];
                var liveProp = liveSo.FindProperty(propPath);

                if (liveProp != null)
                {
                    string currentSerializedValue = OcpSerialization.SerializeProperty(liveProp);
                    if (currentSerializedValue != storedChange.serializedValues[i])
                    {
                        return true; 
                    }
                }
            }

            return false; 
        }

        /// <summary>
        ///     Returns true if a saved entry exists for the current live component in the stores.
        /// </summary>
        public bool HasSavedEntry()
        {
            return ExecuteComponentQuery(
                (go, scenePath, objectPath) => HasSavedTransformEntry(go, scenePath, objectPath),
                HasSavedComponentEntry,
                false);
        }

        private string GetScenePathForGo(GameObject go)
        {
            string scenePath = go.scene.path;
            if (string.IsNullOrEmpty(scenePath))
            {
                scenePath = go.scene.name;
            }

            return scenePath;
        }

        private bool HasSavedTransformEntry(GameObject go, string scenePath, string objectPath)
        {
            var tStore = ChangesStore.LoadExisting();
            if (!tStore)
            {
                return false;
            }

            string goid = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();
            int index = tStore.changes.FindIndex(c =>
                (!string.IsNullOrEmpty(c.globalObjectId) && c.globalObjectId == goid) ||
                (string.IsNullOrEmpty(c.globalObjectId) && c.scenePath == scenePath && c.objectPath == objectPath));
            return index >= 0;
        }

        private bool HasSavedComponentEntry(GameObject go, string scenePath, string objectPath)
        {
            var cStore = CompChangesStore.LoadExisting();
            if (!cStore)
            {
                return false;
            }

            string goid = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();
            int index = FindComponentIndexInStore(go, scenePath, objectPath, goid, cStore);
            return index >= 0;
        }

        private void RemoveFromStore()
        {
            if (!_liveComponent)
            {
                return;
            }

            var go = _liveComponent.gameObject;
            string scenePath = GetScenePathForGo(go);
            string objectPath = OcpUtilities.GetGameObjectPath(go.transform);
            string goid = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();

            if (_liveComponent is Transform or RectTransform)
            {
                RemoveTransformFromStore(scenePath, objectPath, goid);
            }
            else
            {
                RemoveComponentFromStore(go, scenePath, objectPath, goid);
            }
        }

        private void RemoveTransformFromStore(string scenePath, string objectPath, string goid)
        {
            var tStore = ChangesStore.LoadExisting();
            if (!tStore)
            {
                return;
            }

            int index = tStore.changes.FindIndex(c =>
                (!string.IsNullOrEmpty(c.globalObjectId) && c.globalObjectId == goid) ||
                (string.IsNullOrEmpty(c.globalObjectId) && c.scenePath == scenePath && c.objectPath == objectPath));
            if (index >= 0)
            {
                tStore.changes.RemoveAt(index);
                EditorUtility.SetDirty(tStore);
                AssetDatabase.SaveAssets();
            }
        }

        private void RemoveComponentFromStore(GameObject go, string scenePath, string objectPath, string goid)
        {
            var cStore = CompChangesStore.LoadExisting();
            if (!cStore)
            {
                return;
            }

            int index = FindComponentIndexInStore(go, scenePath, objectPath, goid, cStore);
            if (index >= 0)
            {
                cStore.changes.RemoveAt(index);
                EditorUtility.SetDirty(cStore);
                AssetDatabase.SaveAssets();
            }
        }

        private static void RefreshBrowserIfOpen()
        {
            if (EditorWindow.HasOpenInstances<OverridesBrowserWindow>())
            {
                OverridesBrowserWindow.Open();
            }
        }

        private static void ApplyMaterials(Renderer renderer, List<string> materialGuids)
        {
            if (!IsValidForMaterialApply(renderer, materialGuids))
            {
                return;
            }

            var current = renderer.sharedMaterials;
            var applied = new Material[materialGuids.Count];

            for (int i = 0; i < materialGuids.Count; i++)
            {
                applied[i] = ResolveMaterialAtIndex(i, materialGuids[i], current, renderer);
            }

            renderer.sharedMaterials = applied;
        }

        private static bool IsValidForMaterialApply(Renderer renderer, List<string> materialGuids)
        {
            if (!renderer)
            {
                return false;
            }

            if (materialGuids==null)
            {
                return false;
            }

            if (materialGuids.Count == 0)
            {
                return false;
            }

            return true;
        }

        private static Material ResolveMaterialAtIndex(int index, string guid, Material[] current, Renderer renderer)
        {
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guid);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (!mat && index < current.Length)
            {
                return current[index];
            }

            return mat;
        }
    }
}