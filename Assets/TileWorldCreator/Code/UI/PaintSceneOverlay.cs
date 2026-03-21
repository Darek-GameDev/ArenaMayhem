/*

  _____ _ _    __        __         _     _  ____                _             
 |_   _(_) | __\ \      / /__  _ __| | __| |/ ___|_ __ ___  __ _| |_ ___  _ __ 
   | | | | |/ _ \ \ /\ / / _ \| '__| |/ _` | |   | '__/ _ \/ _` | __/ _ \| '__|
   | | | | |  __/\ V  V / (_) | |  | | (_| | |___| | |  __/ (_| | || (_) | |   
   |_| |_|_|\___| \_/\_/ \___/|_|  |_|\__,_|\____|_|  \___|\__,_|\__\___/|_|   
                                                                               
	TileWorldCreator V4 (c) by Giant Grey
	Author: Marc Egli

	www.giantgrey.com

*/

#if UNITY_EDITOR
using System.Collections.Generic;
using System.Net;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

using GiantGrey.TileWorldCreator.UI;
using GiantGrey.TileWorldCreator.Utilities;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;


namespace GiantGrey.TileWorldCreator
{
    [Overlay(typeof(SceneView), "TileWorldCreator", true)]
    [TWCOverlayIconAttribute()]
    [InitializeOnLoad]
    public class PaintSceneOverlay : Overlay
    {
        private VisualElement root;
        private List<TileWorldCreatorManager> managers = new();
        private static int selectedManager = 0;
        private static bool altClick;
        private static bool undoPerfomed;
        private static string currentScene;
        
        
        // private int brushSize = 1;
        private int undoGroup = -1;
        private bool lastDisplayed;
        private bool editorBusy = false;
        private Color colorDarkGrey = new(50f / 255f, 50f / 255f, 50f / 255f);
        
        // -------------------------------------------------------------------------
        // Initialization
        // -------------------------------------------------------------------------

        static PaintSceneOverlay()
        {
            currentScene = EditorSceneManager.GetActiveScene().name;

            // Global Editor Events
            EditorApplication.hierarchyChanged -= HierarchyChanged;
            EditorApplication.hierarchyChanged += HierarchyChanged;
     
            Undo.undoRedoPerformed -= OnUndoRedo;  
            Undo.undoRedoPerformed += OnUndoRedo;
     
            ObjectChangeEvents.changesPublished -= ChangesPublished;
            ObjectChangeEvents.changesPublished += ChangesPublished;
        }

        private void OnEnable()
        {
    #if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;
    #endif 
        }

        public override void OnCreated()
        {
       

            lastDisplayed = displayed;
            EditorApplication.update += CheckDisplayed;
            EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;

            
            var managers = GameObject.FindObjectsByType<TileWorldCreatorManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);

            if (managers != null)
            {
                if (managers.Length > 0)
                {
                    if (managers[selectedManager].configuration != null)
                    {
                        if (managers[selectedManager].configuration.showPaintGrid)
                        {
                            SceneView.duringSceneGui -= OnSceneGUI;
                            SceneView.duringSceneGui += OnSceneGUI;
                        }
                    }
                }
            }


            base.OnCreated();
        }

        public override void OnWillBeDestroyed()
        {
            CleanupSubscriptions();
            base.OnWillBeDestroyed();
        }

        private void CleanupSubscriptions()
        {
            EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
            EditorApplication.update -= CheckDisplayed;
            EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;

            if (selectedManager < managers.Count)
                UnsubscribeFromManager(managers[selectedManager]);
        }

        // -------------------------------------------------------------------------
        // Editor Event Handling
        // -------------------------------------------------------------------------

        private static void HierarchyChanged()
        {
            var overlay = new PaintSceneOverlay();
            overlay.CreatePanelContent();
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void ScriptReload() => altClick = false;

        private static void OnUndoRedo() => undoPerfomed = true;

        private void OnPlaymodeStateChanged(PlayModeStateChange state)
        {
            editorBusy = EditorApplication.isPlayingOrWillChangePlaymode ||
                         EditorApplication.isPlaying ||
                         EditorApplication.isCompiling;
        }

        private static void ChangesPublished(ref ObjectChangeEventStream stream)
        {
            if (!undoPerfomed) return;

            for (int i = 0; i < stream.length; ++i)
            {
                if (stream.GetEventType(i) == ObjectChangeKind.ChangeAssetObjectProperties)
                {
                    stream.GetChangeAssetObjectPropertiesEvent(i, out var e);
                    var changedObj = EditorUtility.InstanceIDToObject(e.instanceId);
                    if (changedObj is BlueprintLayer)
                    {
                        var managers = GameObject.FindObjectsByType<TileWorldCreatorManager>(
                            FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
                        if (selectedManager < managers.Length)
                            managers[selectedManager].GenerateCompleteMap();
                    }
                }
            }
            undoPerfomed = false;
        }

        private void OnSceneChanged(Scene oldScene, Scene newScene)
        {
            if (!editorBusy)
                BuildPanel();
        }

        private void CheckDisplayed()
        {
            if (displayed == lastDisplayed) return;

            if (!displayed && selectedManager < managers.Count)
            {
                var config = managers[selectedManager].configuration;
                if (config != null)
                {
                    config.showPaintGrid = false;
                    config.showGizmos = false;
                }
            }

            lastDisplayed = displayed;
        }


        // -------------------------------------------------------------------------
        // Manager Event Subscriptions
        // -------------------------------------------------------------------------

        private void SubscribeToManager(TileWorldCreatorManager manager)
        {
            if (manager?.configuration != null)
                manager.configuration.OnLayerChanged += OnManagerLayerChanged;
        }

        private void UnsubscribeFromManager(TileWorldCreatorManager manager)
        {
            if (manager?.configuration != null)
                manager.configuration.OnLayerChanged -= OnManagerLayerChanged;
        }

        private void OnManagerLayerChanged()
        {
            if (root != null)
                BuildPanel();
        }

        // -------------------------------------------------------------------------
        // UI
        // -------------------------------------------------------------------------

        #region overlayUI
        public override VisualElement CreatePanelContent()
        {
            root ??= new VisualElement();
            BuildPanel();
            return root;
        }

        private void BuildPanel()
        {
            root ??= new VisualElement();
            root.Clear();
            root.style.backgroundColor = EditorGUIUtility.isProSkin ? Color.black : Color.white;

            // Unsubscribe from all previous managers
            foreach (var m in managers)
                UnsubscribeFromManager(m);

            // Refresh managers
            managers.Clear();
            managers.AddRange(GameObject.FindObjectsByType<TileWorldCreatorManager>(
                FindObjectsInactive.Include, FindObjectsSortMode.InstanceID));

            if (managers.Count == 0)
            {
                var bannerNoManager = new VisualElement
                {
                    style =
                    {
                        backgroundImage = TileWorldCreatorUtilities.LoadImage("PaintBanner.twc"),
                        backgroundColor = EditorGUIUtility.isProSkin ? Color.black : Color.white,
                        width = 150,
                        height = 50,
                        alignSelf = Align.Center,
                        backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center),
                        backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center),
                        backgroundSize = new BackgroundSize(BackgroundSizeType.Contain)
                    }
                };

                var refresh = new Button(() =>
                {
                    BuildPanel();
                });
                refresh.text = "Refresh";
                
                root.Add(bannerNoManager);
                root.Add(new Label("No TileWorldCreator managers in scene."));
                root.Add(refresh);
                return;
            }

            if (selectedManager >= managers.Count)
                selectedManager = 0;

            SubscribeToManager(managers[selectedManager]);

            // -----------------------------------------------------------------
            // Header
            // -----------------------------------------------------------------
            
            var logo = new VisualElement
            {
                style =
                {
                    backgroundImage = TileWorldCreatorUtilities.LoadImage("PaintBanner.twc"),
                    // backgroundColor = EditorGUIUtility.isProSkin ? Color.black : Color.white,
                    width = 150,
                    height = 50,
                    alignSelf = Align.Center,
                    backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center),
                    backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center),
                    backgroundSize = new BackgroundSize(BackgroundSizeType.Contain)
                }
            };
            

            var bannerBG = new VisualElement
            {
                style =
                {
                    backgroundImage = TileWorldCreatorUtilities.LoadImage("PaintBannerBG.twc"),
                    flexGrow = 1,
                    minWidth = 150,
                    minHeight = 50,
                    backgroundSize = new  BackgroundSize(BackgroundSizeType.Cover)
                }
            };
            
            bannerBG.SetPadding(4, 4, 0, 0);
            bannerBG.Add(logo);
            root.Add(bannerBG);

            var _managersContainer = new VisualElement();
            _managersContainer.SetPadding(4, 4, 4, 4);
            _managersContainer.SetBorder(1);
            
            root.Add(_managersContainer);
                
            var managerLabel = new Label("TWC Managers")
            {
                style = { fontSize = 14, marginTop = 5 }
            };
            
            managerLabel.SetMargin(0, 4, 4, 0);
            
            _managersContainer.Add(managerLabel);

            // Manager Buttons
            for (int i = 0; i < managers.Count; i++)
            {
                int index = i;
                var button = new Button(() =>
                {
                    managers[selectedManager].configuration.showPaintGrid = false;
                    managers[selectedManager].configuration.showGizmos = false;
                    managers[selectedManager].configuration.selectedPaintLayerGuid = string.Empty;

                        
                    if (selectedManager < managers.Count)
                        UnsubscribeFromManager(managers[selectedManager]);

                    selectedManager = index;
                    EditorPrefs.SetInt("TWC_SELECTEDMANAGERINDEX", selectedManager);
                    if (managers[index].gameObject == null)
                    {
                        managers.Clear();
                        managers.AddRange(GameObject.FindObjectsByType<TileWorldCreatorManager>(
                            FindObjectsInactive.Include, FindObjectsSortMode.InstanceID));
                        return;
                    }
                    else
                    {
                        Selection.activeGameObject = managers[index].gameObject;
                    }

                    SceneView.duringSceneGui -= OnSceneGUI;
                    
                    managers[selectedManager].configuration.selectedPaintLayerGuid = string.Empty;
                    managers[selectedManager].configuration.showPaintGrid = false;
                    managers[selectedManager].configuration.showGizmos = false;
                    
                    BuildPanel();
                })
                {
                    text = managers[i].name
                };

                if (i == selectedManager)
                    button.SetBorder(2, TileWorldCreatorColor.Blue.GetColor());

                _managersContainer.Add(button);
            }

            var manager = managers[selectedManager];
            if (manager.configuration == null)
            {
                _managersContainer.Add(new Label("Selected manager does not have a configuration."));
                var refresh = new Button(() =>
                {
                    BuildPanel();
                });
                refresh.text = "Refresh";
                _managersContainer.Add(refresh);
                return;
            }

            // -----------------------------------------------------------------
            // Toolbar
            // -----------------------------------------------------------------
            var toolbar = new VisualElement
            {
                style =
                {
                    backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.DarkGrey.GetColor() : TileWorldCreatorColor.White.GetColor(), //colorDarkGrey,
                    flexDirection = FlexDirection.Row,
                    marginTop = 4,
                    marginBottom = 4,
                }
            };

            var paintButton = new ToolbarButtonElement(new Vector2Int(40, 40), "", TileWorldCreatorUtilities.LoadImage("paintMode.twc"), true)
            {
                Toggle = manager.configuration.showPaintGrid
            };

            var gizmoButton = new ToolbarButtonElement(new Vector2Int(40, 40), "", TileWorldCreatorUtilities.LoadImage("gizmos.twc"), true)
            {
                Toggle = manager.configuration.showGizmos,
                tooltip = "Enable Gizmos for selected layer"
            };

            paintButton.clickable.clicked += () =>
            {
                managers[selectedManager].configuration.showPaintGrid = true;
                managers[selectedManager].configuration.showGizmos = false;
                SceneView.duringSceneGui -= OnSceneGUI;
                SceneView.duringSceneGui += OnSceneGUI;
                gizmoButton.Toggle = false;
                paintButton.Toggle = true;
                BuildPanel();
            };

            gizmoButton.clickable.clicked += () =>
            {
                managers[selectedManager].configuration.showGizmos = true;
                managers[selectedManager].configuration.showPaintGrid = false;
                paintButton.Toggle = false;
                gizmoButton.Toggle = true;
                SceneView.duringSceneGui -= OnSceneGUI;
                BuildPanel();
            };

            toolbar.Add(paintButton);
            toolbar.Add(gizmoButton);
            root.Add(toolbar);

            // -----------------------------------------------------------------
            // Paint and Gizmo Containers
            // -----------------------------------------------------------------
            var paintContainer = new VisualElement();
            var gizmoContainer = new VisualElement();
            paintContainer.style.backgroundColor = gizmoContainer.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.DarkGrey.GetColor() : TileWorldCreatorColor.White.GetColor();
            paintContainer.SetPadding(2, 2, 4, 4);
            paintContainer.SetBorder(1);
            gizmoContainer.SetBorder(1);
            gizmoContainer.SetPadding(2, 2, 4, 4);
            paintContainer.style.display = manager.configuration.showPaintGrid ? DisplayStyle.Flex : DisplayStyle.None;
            gizmoContainer.style.display = manager.configuration.showGizmos ? DisplayStyle.Flex : DisplayStyle.None;

            root.Add(paintContainer);
            root.Add(gizmoContainer);

            // -----------------------------------------------------------------
            // Execute Button
            // -----------------------------------------------------------------
            var execute = new Button(() =>
            {
                managers[selectedManager].ExecuteBlueprintLayers();
                managers[selectedManager].ExecuteBuildLayers(ExecutionMode.FromScratch);
            })
            {
                text = "Execute All Layers",
                style = { height = 35, marginTop = 10 }
            };
            root.Add(execute);

            // -----------------------------------------------------------------
            // Brush Controls
            // -----------------------------------------------------------------
            var lblSize = new Label("Brush Size") { style = { marginTop = 5, fontSize = 14 } };
            var slider = new SliderInt(1, 10) { showInputField = true, value = managers[selectedManager].configuration.brushSize };
            slider.RegisterValueChangedCallback(evt =>
            {
                managers[selectedManager].configuration.brushSize = evt.newValue;
                // manager.brushSize = brushSize;
            });
            paintContainer.Add(lblSize);
            paintContainer.Add(slider);

            // -----------------------------------------------------------------
            // Layer Lists
            // -----------------------------------------------------------------
            BuildBlueprintLayersUI(manager, paintContainer);
            BuildBuildLayersUI(manager, gizmoContainer);
        }

        private void BuildBlueprintLayersUI(TileWorldCreatorManager manager, VisualElement parent)
        {
            var scroll = new ScrollView { style = { maxHeight = 300 } };
            parent.Add(scroll);

            foreach (var folder in managers[selectedManager].configuration.blueprintLayerFolders)
            {
                var foldout = new Foldout { text = folder.folderName };
                scroll.Add(foldout);

                foreach (var layer in folder.blueprintLayers)
                {
                    if (layer.lockFromPaint) continue;

                    var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
                    var button = new Button
                    {
                        text = layer.layerName,
                        name = layer.guid,
                        style = { flexGrow = 1 }
                    };

                    button.clickable.clicked += () =>
                    {
                    
                        managers[selectedManager].configuration.selectedPaintLayerGuid = layer.guid;
                        
                        if (layer == managers[selectedManager].configuration.paintLayer && managers[selectedManager].configuration.showPaintGrid)
                        {
                            managers[selectedManager].configuration.showPaintGrid = false;
                        }
                        else
                        {
                            managers[selectedManager].configuration.showPaintGrid = true;
                        }
                        
                        managers[selectedManager].configuration.paintLayer = layer;
                        managers[selectedManager].configuration.showGizmos = false;

                        if (managers[selectedManager].configuration.showPaintGrid)
                        {
                            SceneView.duringSceneGui -= OnSceneGUI;
                            SceneView.duringSceneGui += OnSceneGUI;
                        }
                        else
                        {
                            // if (string.IsNullOrEmpty(managers[selectedManager].configuration.selectedPaintLayerGuid))
                            // {
                                SceneView.duringSceneGui -= OnSceneGUI;
                            // }
                            // else
                            // {
                            //     managers[selectedManager].configuration.showPaintGrid = true;
                            //     SceneView.duringSceneGui -= OnSceneGUI;
                            //     SceneView.duringSceneGui += OnSceneGUI;
                            // }
                        }

                        button.SetBorder(managers[selectedManager].configuration.showPaintGrid ? 2 : 0, TileWorldCreatorColor.Blue.GetColor());
                        
                        
                        for (int i = 0; i < managers[selectedManager].configuration.blueprintLayerFolders.Count; i ++)
                        {
                            for (int j = 0; j < managers[selectedManager].configuration.blueprintLayerFolders[i].blueprintLayers.Count; j ++)
                            {
                                if (managers[selectedManager].configuration.blueprintLayerFolders[i].blueprintLayers[j].guid != managers[selectedManager].configuration.selectedPaintLayerGuid)
                                {
                                     root.Q<Button>(managers[selectedManager].configuration.blueprintLayerFolders[i].blueprintLayers[j].guid)?.SetBorder(0);
                                }
                            }
                        }
                    
                        
                        
                        // BuildPanel();
                    };
               
                    if (managers[selectedManager].configuration.selectedPaintLayerGuid == layer.guid)
                    {
                        button.SetBorder(2, TileWorldCreatorColor.Blue.GetColor());
                    }

                    var clear = new Button { style = { width = 24, height = 24, backgroundImage = TileWorldCreatorUtilities.LoadImage("clear.twc") } };
                    clear.clickable.clicked += () =>
                    {
                        layer.ClearLayer();
                        managers[selectedManager].configuration.ExecuteBlueprintLayers(managers[selectedManager]);
                        managers[selectedManager].configuration.ExecuteBuildLayers(managers[selectedManager], true);
                    };

                    var fill = new Button { style = { width = 24, height = 24, backgroundImage = TileWorldCreatorUtilities.LoadImage("fill.twc") } };
                    fill.clickable.clicked += () =>
                    {
                        layer.FillLayer();
                        managers[selectedManager].configuration.ExecuteBlueprintLayers(managers[selectedManager]);
                        managers[selectedManager].configuration.ExecuteBuildLayers(managers[selectedManager]);
                    };

                    row.Add(button);
                    row.Add(clear);
                    row.Add(fill);
                    foldout.Add(row);
                }
            }
        }

        private void BuildBuildLayersUI(TileWorldCreatorManager manager, VisualElement parent)
        {
            var scroll = new ScrollView { style = { maxHeight = 300 } };
            parent.Add(scroll);

            foreach (var folder in managers[selectedManager].configuration.buildLayerFolders)
            {
                var foldout = new Foldout { text = folder.folderName };
                scroll.Add(foldout);

                foreach (var layer in folder.buildLayers)
                {
                    var button = new Button
                    {
                        text = layer.layerName,
                        name = layer.guid,
                        style = { height = 24, flexGrow = 1 }
                    };

                    button.clickable.clicked += () =>
                    {
                        managers[selectedManager].configuration.gizmoLayer = layer as BuildLayer;
                        BuildPanel();
                    };

                    foldout.Add(button);
                }
            }
        }
        
        #endregion
        
        #region SceneGUI
    
        public void OnSceneGUI(SceneView sceneView) 
        {
            if (managers == null) return;
            if (managers.Count == 0) return;
            if (managers[selectedManager] == null) return;
            if (managers[selectedManager].configuration == null) return;
            if (managers[selectedManager].configuration.showPaintGrid == false) return;
 
            Event _event = Event.current;

            if ((_event.type == EventType.MouseDown || _event.type == EventType.MouseDrag || _event.type == EventType.MouseUp) && _event.button == 2)
            {
                return;
            }

            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            // do not paint when user is navigating    
#if UNITY_EDITOR_OSX
            if (_event.keyCode == KeyCode.LeftAlt)
            {
                altClick = true;
            }
#else
            if (_event.keyCode == KeyCode.LeftAlt || _event.keyCode == KeyCode.RightAlt)
            {
                altClick = true;
            }
#endif
          
           
            if (_event.type == EventType.MouseDown && !altClick)
            {
                undoGroup = Undo.GetCurrentGroup();
                Undo.IncrementCurrentGroup();
                Undo.SetCurrentGroupName("Paint Cells");

                managers[selectedManager].paintedPositions.Clear(); // = new List<Vector2>();

                if (_event.button == 0)
                {
                    managers[selectedManager].paintState = true;
                }
                else if (_event.button == 1)
                {
                    managers[selectedManager].paintState = false;
                }

                SetCellAt(_event.mousePosition);

                _event.Use();
            }

            if (_event.type == EventType.MouseDrag && !altClick)
            {
                if (_event.button == 0)
                {
                    managers[selectedManager].paintState = true;
                }
                else
                {
                    managers[selectedManager].paintState = false;
                }

                SetCellAt(_event.mousePosition);

                _event.Use();
            }

            if (_event.type == EventType.MouseUp && !altClick)
            {
                SetCellsFinalize(_event.button == 0 ? true : false);

                Undo.CollapseUndoOperations(undoGroup);
                undoGroup = -1;

                altClick = false;              
            }


            if (_event.GetTypeForControl(controlID) == EventType.KeyUp)
            {
                #if UNITY_EDITOR_OSX
                if (_event.control)
                {
                    altClick = false;
                }
                #else
                if (_event.keyCode == KeyCode.LeftAlt || _event.keyCode == KeyCode.RightAlt)
                {
                    altClick = false;
                }
                #endif
            }
        }
        
        #endregion
    
        void SetCellAt(Vector2 _position)
        {
            var _mousePos = new Vector2(_position.x, _position.y );
            var _isMouseOverGrid = IsMouseOverGrid(_mousePos);
            if (_isMouseOverGrid && managers[selectedManager].configuration.showPaintGrid)
            {
                var _wp = GetWorldPosition(_position);
                var _gridPos = GetGridPosition(_wp);
                var _brushSize = managers[selectedManager].configuration.brushSize;
                int _halfSize = Mathf.CeilToInt(_brushSize * 0.5f);
                float _radius = _brushSize * 0.5f;
                
                Matrix4x4 _oldMatrix = Gizmos.matrix;

                Gizmos.matrix = Matrix4x4.TRS(managers[selectedManager].transform.position, managers[selectedManager].transform.rotation, Vector3.one);
                Gizmos.color = Color.yellow;

                for (int x = -_halfSize; x <= _halfSize; x ++)
                {
                    for (int y = -_halfSize; y <= _halfSize; y ++)
                    {
                        Vector2 _pos = new Vector2(_gridPos.x + x, _gridPos.y + y);

                        
                        if (Vector2.Distance(_gridPos, _pos) <= _radius)
                        {
                            // Vector2 localPos = new Vector2(_pos.x * managers[selectedManager].configuration.cellSize, _pos.y * managers[selectedManager].configuration.cellSize);
                            
                            if (_pos.x >= 0 && _pos.x < managers[selectedManager].configuration.width && _pos.y >= 0 && _pos.y < managers[selectedManager].configuration.height)
                            {
                                if (!managers[selectedManager].paintedPositions.Contains(_pos))
                                {
                                    managers[selectedManager].paintedPositions.Add(_pos);    
                                }
                            }
                        }
                    }
                }

                // Restore the previous Gizmos matrix
                Gizmos.matrix = _oldMatrix;
                Gizmos.color = Color.white;
            }
        }

        void SetCellsFinalize(bool _state)
        {
            var _layer = managers[selectedManager].configuration.GetBlueprintLayerByGuid(managers[selectedManager].configuration.selectedPaintLayerGuid);
            // blueprintLayer = _layer;

            if (_layer != null)
            {
                // Register Undo before making changes
                // blueprintLayerState = new BlueprintLayer.BlueprintLayerState(_layer);
                Undo.RegisterCompleteObjectUndo(_layer, _state ? "Paint Cells" : "Erase Cells");

                if (_state)
                {
                    _layer.AddCells(managers[selectedManager].paintedPositions);
                }
                else
                {
                    _layer.RemoveCells(managers[selectedManager].paintedPositions);
                }

                managers[selectedManager].OnBlueprintLayersReady -= BlueprintLayersReady;
                managers[selectedManager].OnBlueprintLayersReady += BlueprintLayersReady;
                managers[selectedManager].ExecuteBlueprintLayers();

            }
            else
            {
                Debug.LogWarning("No Blueprint layer selected");
            }

            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
        }

        void BlueprintLayersReady()
        {
            managers[selectedManager].ExecuteBuildLayers();
        }
        
        #region helpers
        
        Vector2 GetGridPosition(Vector3 worldPos)
		{
			// Convert world position to local space relative to the rotated grid
			Vector3 localPos = managers[selectedManager].transform.InverseTransformPoint(worldPos);

			// Snap to the closest grid cell
			int gridX = Mathf.RoundToInt(localPos.x / managers[selectedManager].configuration.cellSize);
			int gridY = Mathf.RoundToInt(localPos.z / managers[selectedManager].configuration.cellSize);

			return new Vector2(gridX, gridY);
		}

        Vector3 GetWorldPosition(Vector2 _mousePos)
		{
			Ray _ray = HandleUtility.GUIPointToWorldRay(_mousePos);

            // Create a plane that fully aligns with the grid's rotation
            var _managerPosition = managers[selectedManager].transform.position;
            var _layer = managers[selectedManager].configuration.paintLayer;
            
			Plane gridPlane = new Plane(managers[selectedManager].transform.rotation * Vector3.up, new Vector3(_managerPosition.x, _managerPosition.y + _layer.defaultLayerHeight, _managerPosition.z));

			float _dist;
			if (gridPlane.Raycast(_ray, out _dist))
			{
				return _ray.GetPoint(_dist);
			}

			return Vector3.zero; 
        }

        bool IsMouseOverGrid(Vector2 _mousePos)
        {
            bool _return = false;

            var _wp = GetWorldPosition(_mousePos);
            var _gridPos = GetGridPosition(_wp);

         
            // var _cellSize = managers[selectedManager].configuration.cellSize;
            if (_gridPos.x >= 0 && _gridPos.y >= 0 && _gridPos.x < managers[selectedManager].configuration.width &&
            _gridPos.y < managers[selectedManager].configuration.height)
            {
                _return = true;
            }

            return _return;
        }
        
        #endregion
    }
}
#endif