
/*

  _____ _ _    __        __         _     _  ____                _             
 |_   _(_) | __\ \      / /__  _ __| | __| |/ ___|_ __ ___  __ _| |_ ___  _ __ 
   | | | | |/ _ \ \ /\ / / _ \| '__| |/ _` | |   | '__/ _ \/ _` | __/ _ \| '__|
   | | | | |  __/\ V  V / (_) | |  | | (_| | |___| | |  __/ (_| | || (_) | |   
   |_| |_|_|\___| \_/\_/ \___/|_|  |_|\__,_|\____|_|  \___|\__,_|\__\___/|_|   
                                                                               
	TileWorldCreator (c) by Giant Grey
	Author: Marc Egli

	www.giantgrey.com

*/

#if UNITY_EDITOR
using GiantGrey.TileWorldCreator.Components;
using UnityEditor;
using UnityEngine;

namespace GiantGrey.TileWorldCreator.UI
{
    [InitializeOnLoad]
    public class LabelGizmoDrawer
    {

        static LabelGizmoDrawer()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        static int activeHandleID;
        static bool isDragging = false;
        static Vector3 lastDragPosition;
        static Vector3 oldPosition;

        static void OnSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
            if (e == null) return;

            

            foreach (LayerIdentifier _layerIdentifier in GameObject.FindObjectsByType<LayerIdentifier>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
            {
                if (!_layerIdentifier.isActiveAndEnabled) continue;
            

                var _manager = _layerIdentifier.transform.parent?.GetComponent<TileWorldCreatorManager>();
                if (_manager == null)
                {
                    continue;
                }
                
                var _layer = _manager.GetBuildLayerByGuid(_layerIdentifier.guid) as TilesBuildLayer;
                
                if (_layer == null)
                    continue;

                if (_manager != null)
                {
                    if (_manager.configuration != null)
                    {
                        if (!_manager.configuration.showGizmos)
                        {
                            continue;
                        }
                        else
                        {
                            if (_manager.configuration.gizmoLayer != null)
                            {
                                if (_manager.configuration.gizmoLayer.guid != _layer.guid)
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }

                Bounds bounds = GetLocalHierarchyBounds(_layerIdentifier.gameObject.transform);

                Vector3 topCenter = bounds.center + Vector3.up * (bounds.extents.y);
                Vector3 center = bounds.center;
                Vector3 size = bounds.size;
                Vector3 bottomCenter = bounds.center - Vector3.up * bounds.extents.y;
                Vector3 bottomLeft = center - new Vector3(size.x * 0.5f, size.y * 0.5f,  size.z * 0.5f);
                Vector3 offsetPosition = bottomLeft + new Vector3(0, _layer.tmpLayerOffset, 0);
                // Convert world-space center into local space of _manager
                Vector3 localCenter = Quaternion.Inverse(_manager.transform.rotation) * (bounds.center - _manager.transform.position);
                // Then apply offset (also in local space)
                Vector3 centerWithOffset = localCenter+ new Vector3(0, _layer.tmpLayerOffset, 0);
                
                if (_layer == null)
                    return;

                if (_layer.tileLayers == null)
                    return;

                    var color =  "#" + _layer.guid.Substring(0, 6);
                    Color _colorConv = Color.white; 
                    UnityEngine.ColorUtility.TryParseHtmlString(color, out _colorConv);
                    Handles.color =_colorConv;
                    
                    
                    // Save current Gizmos matrix
                    Matrix4x4 oldMatrix = Gizmos.matrix;

                    // Apply GameObject's transformation
                    Handles.matrix = Matrix4x4.TRS(_manager.transform.position, _manager.transform.rotation, Vector3.one);

                    // Handles.DrawWireCube(center, size);
                    // var _labelCenter = center;
                    // _labelCenter.y += HandleUtility.GetHandleSize(center) * 0.3f;
                    // Handles.Label(_labelCenter, new GUIContent("Tile layer: " + (i + 1).ToString() + " - Height: " +_layer.tileLayers[i].heightOffset.ToString()), EditorStyles.boldLabel);
                    float handleSize = HandleUtility.GetHandleSize(topCenter) * 0.2f; // Scale multiplier tweakable
                        
                    if (_layer.useMultiLayers)
                    {
                        EditorGUI.BeginChangeCheck(); 
                        

                        Vector3 _newPos = Handles.Slider(topCenter, Vector3.up, handleSize, Handles.CubeHandleCap, 1f);
                    
                        if (EditorGUI.EndChangeCheck())
                        {
                            
                            // Vector3 delta = _newPos - offsetPosition;
                            float deltaY = _newPos.y - oldPosition.y;
                            if (deltaY > 1f)
                            {
                                oldPosition = _newPos;
                                (_layer as TilesBuildLayer).AddTileLayer();
                                _manager.ExecuteBuildLayers(ExecutionMode.FromScratch);
                            }
                            else if (deltaY < -1f)
                            {
                                oldPosition = _newPos;
                                if (_layer.tileLayers.Count - 1 <= 0) continue;
                                (_layer as TilesBuildLayer).RemoveTileLayer(_layer.tileLayers.Count - 1);
                                _manager.ExecuteBuildLayers(ExecutionMode.FromScratch);
                            }

                            EditorUtility.SetDirty(_layerIdentifier);
                        }
                    }

                    // Layer offset Handle
                    int controlID = GUIUtility.GetControlID(FocusType.Passive);

                    EditorGUI.BeginChangeCheck();

                    Vector3 snap = Vector3.zero;
                    Vector3 newPosition = Handles.FreeMoveHandle(
                        offsetPosition,
                        handleSize,
                        snap,
                        (controlID, pos, rotation, capSize, eventType) =>
                        {
                            // Arrow pointing upward
                            // Handles.ArrowHandleCap(controlID, pos, Quaternion.LookRotation(Vector3.up), capSize, eventType);

                            Matrix4x4 oldMatrix = Handles.matrix;

                            // Thicken the arrow in X/Z, keep Y the same
                            Handles.matrix = Matrix4x4.TRS(pos, _manager.transform.rotation, new Vector3(3f, 2f, 3f));
                            Handles.ArrowHandleCap(controlID, Vector3.zero, Quaternion.LookRotation(Vector3.up), capSize, eventType);
                            Handles.matrix = oldMatrix;
                        }
                    );

                    if (EditorGUI.EndChangeCheck())
                    {
                        Vector3 delta = newPosition - offsetPosition;
                        delta.x = 0f;
                        delta.z = 0f;

                        // var _yHeight = Mathf.Round(delta.y * 10.0f) * 0.1f;
                        // var _yHeight = System.Math.Round( delta.y, 1, System.MidpointRounding.AwayFromZero);
                        float _yHeight = 0f;

                        if (delta.y < 0f)
                        {
                            _yHeight = -0.1f;
                        }
                        else if (delta.y > 0f)
                        {
                            _yHeight = 0.1f;
                        }

                        _layer.layerYOffset += _yHeight;
                        _layer.tmpLayerOffset += _yHeight;

                        isDragging = true;
                        activeHandleID = GUIUtility.hotControl;

                        

                    }

                    if (isDragging)
                    {
                        Handles.Label(offsetPosition, new GUIContent("Y Offset: " + _layer.layerYOffset), EditorStyles.boldLabel);
                    }

                    // Detect drag end when handle control is released
                    if (isDragging && GUIUtility.hotControl != activeHandleID && Event.current.type == EventType.Layout)
                    {
                        _layer.tmpLayerOffset = 0f;
                        isDragging = false;
                        foreach (LayerIdentifier _li in GameObject.FindObjectsByType<LayerIdentifier>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID))
                        {
                            var _layerA = _manager.GetBuildLayerByGuid(_li.guid) as TilesBuildLayer;
                            if (_layerA != null)
                            {
                                _layerA.tmpLayerOffset = 0f;
                            }
                        }

                        _manager.ExecuteBuildLayers(ExecutionMode.FromScratch);
                    }

                

                    // 3. Draw your wire cube at the updated center
                    Vector3 updatedCenter = center + new Vector3(0, _layer.tmpLayerOffset, 0);
                    Handles.DrawWireCube(updatedCenter, size);

                    // var _guiPos = HandleUtility.WorldToGUIPoint(center);
                    // Rect rectB = new Rect(_guiPos.x + 20, _guiPos.y - 5, 20, 20);
                    // Handles.BeginGUI();

                    //     GUILayout.BeginArea(rectB);
                        
                    //     if (GUILayout.Button("x"))
                    //     {
                    //         (_layer as TilesBuildLayer).RemoveTileLayer(i);
                    //         _manager.ExecuteAllBuildLayersFromScratch();

                    //         e.Use();
                    //     }

                    //     GUILayout.EndArea();
                    // Handles.EndGUI();

                    // Restore previous Gizmos matrix
                    Handles.matrix = oldMatrix;

            }
        }

        static Bounds GetLocalHierarchyBounds(Transform root)
        {
            var renderers = root.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.zero);

            Bounds localBounds = new Bounds();
            bool initialized = false;

            foreach (var renderer in renderers)
            {
                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (!filter || !filter.sharedMesh) continue;

                var meshBounds = filter.sharedMesh.bounds; // Local bounds of mesh
                var localToRoot = root.InverseTransformPoint(renderer.transform.position);
                var matrix = root.worldToLocalMatrix * renderer.transform.localToWorldMatrix;

                // Transform mesh bounds to root's local space
                var transformed = TransformBounds(matrix, meshBounds);

                if (!initialized)
                {
                    localBounds = transformed;
                    initialized = true;
                }
                else
                {
                    localBounds.Encapsulate(transformed);
                }
            }

            return localBounds;
        }

        static Bounds TransformBounds(Matrix4x4 matrix, Bounds bounds)
        {
            // Convert bounds to 8 corners, transform them, then encapsulate
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;

            Vector3[] corners = new Vector3[8]
            {
                center + new Vector3( extents.x,  extents.y,  extents.z),
                center + new Vector3( extents.x,  extents.y, -extents.z),
                center + new Vector3( extents.x, -extents.y,  extents.z),
                center + new Vector3( extents.x, -extents.y, -extents.z),
                center + new Vector3(-extents.x,  extents.y,  extents.z),
                center + new Vector3(-extents.x,  extents.y, -extents.z),
                center + new Vector3(-extents.x, -extents.y,  extents.z),
                center + new Vector3(-extents.x, -extents.y, -extents.z),
            };

            Bounds transformed = new Bounds(matrix.MultiplyPoint3x4(corners[0]), Vector3.zero);
            for (int i = 1; i < corners.Length; i++)
            {
                transformed.Encapsulate(matrix.MultiplyPoint3x4(corners[i]));
            }

            return transformed;
        }
        

        static Bounds GetHierarchyBounds(GameObject obj)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(obj.transform.position, Vector3.zero);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            return bounds;
        }

        static Bounds TransformBoundsToLocal(Bounds worldBounds, Matrix4x4 worldToLocal)
        {
            Vector3 center = worldToLocal.MultiplyPoint(worldBounds.center);
            Vector3 extents = worldBounds.extents;
            Vector3 axisX = worldToLocal.MultiplyVector(Vector3.right) * extents.x;
            Vector3 axisY = worldToLocal.MultiplyVector(Vector3.up) * extents.y;
            Vector3 axisZ = worldToLocal.MultiplyVector(Vector3.forward) * extents.z;

            // Approximate the new bounds size based on transformed axes
            Vector3 newExtents = new Vector3(
                Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
                Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
                Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z)
            );

            return new Bounds(center, newExtents * 2);
        }
    }
}
#endif