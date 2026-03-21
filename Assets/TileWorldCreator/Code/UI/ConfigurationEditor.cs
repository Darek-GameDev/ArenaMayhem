
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
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;

using GiantGrey.TileWorldCreator.UI;
using GiantGrey.TileWorldCreator.Utilities;
using System.Reflection;
using GiantGrey.TileWorldCreator.Attributes;

namespace GiantGrey.TileWorldCreator
{
    [CustomEditor(typeof(Configuration))]
    public class ConfigurationEditor : Editor
    {
        Configuration configuration;
        TileWorldCreatorManager manager;
        VisualElement root;
        VisualElement layersRoot;

        VisualElement buildLayersRoot;

        Color darkGrey = new Color(30f/255f, 30f/255f, 30f/255f);
        Color grey = new Color(40f/255f, 40f/255f, 40f/255f);
        Color layerGreyBg = new Color(100f/255f, 100f/255f, 100f/255f) ;
        Color layerGreyMouseEnter = new Color(120f/255f, 120f/255f, 120f/255f);
        Color layerHighlightColor = new Color(200f/255f, 200f/255f, 200f/255f);

        private Color darkGreyLight = new(0.65f, 0.65f, 0.65f);
        private Color greyLight = new Color(0.73f, 0.73f, 0.73f);
        private Color layerGreyBgLight = new Color(0.84f, 0.84f, 0.84f);
        

        List<LayerFoldoutElement> layerFoldouts = new List<LayerFoldoutElement>();
        List<LayerFoldoutElement> buildLayerFoldouts = new List<LayerFoldoutElement>();
        public List<VisualElement> previewTextures = new List<VisualElement>();

        ListView blueprintLayersListView;
        ListView buildLayersListView;

        void OnEnable()
        {
            try
            {
                configuration = (Configuration)target;
            }catch{}
            
            darkGrey = EditorGUIUtility.isProSkin ? darkGrey :  darkGreyLight;
            grey = EditorGUIUtility.isProSkin ? grey : greyLight;
            layerGreyBg = EditorGUIUtility.isProSkin ? layerGreyBg :  layerGreyBgLight;
            
        }

        void UpdatePreviewTextures()
        {
            try
            {
            for (int i = 0; i < previewTextures.Count; i++)
            {
                previewTextures[i].style.backgroundImage = configuration.tileMapLayers[i].previewTexture;
            }
            }
            catch{}

            for (int i = 0; i < configuration.blueprintLayerFolders.Count; i++)
            {
                for (int j = 0; j < configuration.blueprintLayerFolders[i].blueprintLayers.Count; j ++)
                {
                    for (int p = 0; p < previewTextures.Count; p ++)
                    {
                        if (configuration.blueprintLayerFolders[i].blueprintLayers[j].guid == previewTextures[p].name)
                        {
                            previewTextures[p].style.backgroundImage = configuration.blueprintLayerFolders[i].blueprintLayers[j].previewTexture;
                        }
                    }
                }
            }
        }


        public override VisualElement CreateInspectorGUI()
        {
            root = new VisualElement();

            layersRoot = new VisualElement();
            layersRoot.name = "layerRoot";
            layersRoot.style.borderBottomColor = layerGreyBg;
            layersRoot.style.borderTopColor = darkGrey;
            layersRoot.style.borderRightColor = layerGreyBg;
            layersRoot.style.borderLeftColor = darkGrey;
            layersRoot.style.borderBottomWidth = 1;
            layersRoot.style.borderTopWidth = 1;
            layersRoot.style.borderLeftWidth = 1;
            layersRoot.style.borderRightWidth = 1;
            // layersRoot.style.marginTop = 10;
            layersRoot.SetPadding(3, 3, 3, 3);

            buildLayersRoot = new VisualElement();
            buildLayersRoot.name = "buildLayerRoot";
            buildLayersRoot.style.borderBottomColor = layerGreyBg;
            buildLayersRoot.style.borderTopColor = darkGrey;
            buildLayersRoot.style.borderRightColor = layerGreyBg;
            buildLayersRoot.style.borderLeftColor = darkGrey;
            buildLayersRoot.style.borderBottomWidth = 1;
            buildLayersRoot.style.borderTopWidth = 1;
            buildLayersRoot.style.borderLeftWidth = 1;
            buildLayersRoot.style.borderRightWidth = 1;
            buildLayersRoot.style.marginTop = 10;
            buildLayersRoot.style.marginBottom = 10;
            buildLayersRoot.SetPadding(3, 3, 3, 3);

#if TWC_DEBUG
            var _useParallel = new PropertyField();
            _useParallel.BindProperty(serializedObject.FindProperty(nameof(configuration.useParallel)));
#endif
            var _width = new PropertyField();
            _width.BindProperty(serializedObject.FindProperty(nameof(configuration.width)));

            var _height = new PropertyField();
            _height.BindProperty(serializedObject.FindProperty(nameof(configuration.height)));

            var _cellSize = new PropertyField();
            _cellSize.BindProperty(serializedObject.FindProperty(nameof(configuration.cellSize)));

            var _clusterSize = new PropertyField();
            _clusterSize.BindProperty(serializedObject.FindProperty(nameof(configuration.clusterCellSize)));

            var _mergePreviewTextures = new PropertyField();
            _mergePreviewTextures.BindProperty(serializedObject.FindProperty(nameof(configuration.mergePreviewTextures)));



            var _shadowCastingMode = new PropertyField();
            _shadowCastingMode.BindProperty(serializedObject.FindProperty(nameof(configuration.shadowCastingMode)));

            var _objectLayer = new LayerField();
            _objectLayer.label = "Object Layer";
            _objectLayer.BindProperty(serializedObject.FindProperty(nameof(configuration.objectLayer)));

#if !UNITY_6000_0_OR_NEWER


            var _renderingLayer = new RenderingLayerMaskField("Rendering LayerMask", defaultMask: configuration.renderingLayer.value);
            _renderingLayer.RegisterValueChangedCallback(evt =>
            {
                serializedObject.Update();
                configuration.renderingLayer.value = _renderingLayer.value;
                serializedObject.ApplyModifiedProperties();
            });
#else

             var _renderingLayer = new RenderingLayerMaskField();
            _renderingLayer.label = "Rendering LayerMask";
            _renderingLayer.BindProperty(serializedObject.FindProperty(nameof(configuration.renderingLayer)));

#endif

            var _colliderType = new PropertyField();
            _colliderType.BindProperty(serializedObject.FindProperty(nameof(configuration.colliderType)));

            var _tileColliderExtrusionHeight = new PropertyField();
            _tileColliderExtrusionHeight.BindProperty(serializedObject.FindProperty(nameof(configuration.tileColliderExtrusionHeight)));

            var _tileColliderHeight = new PropertyField();
            _tileColliderHeight.BindProperty(serializedObject.FindProperty(nameof(configuration.tileColliderHeight)));

            var _invertWalls = new PropertyField();
            _invertWalls.BindProperty(serializedObject.FindProperty(nameof(configuration.invertCollisionWalls)));
            
            var _mergeTiles = new PropertyField();
            _mergeTiles.BindProperty(serializedObject.FindProperty(nameof(configuration.mergeTiles)));
            _mergeTiles.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                _shadowCastingMode.SetEnabled(evt.newValue);
                _objectLayer.SetEnabled(evt.newValue);
                _renderingLayer.SetEnabled(evt.newValue);
                _colliderType.SetEnabled(evt.newValue);
                _tileColliderExtrusionHeight.SetEnabled(evt.newValue);
                _tileColliderHeight.SetEnabled(evt.newValue);
            });

            _colliderType.RegisterCallback<ChangeEvent<string>>(evt =>
           {
               if (configuration.colliderType == Configuration.ColliderType.tileCollider)
               {
                   _tileColliderHeight.SetEnabled(true);
                   _tileColliderExtrusionHeight.SetEnabled(true);
                   _invertWalls.SetEnabled(true);
               }
               else
               {
                   _tileColliderHeight.SetEnabled(false);
                   _tileColliderExtrusionHeight.SetEnabled(false);
                   _invertWalls.SetEnabled(false);
               }
           });

            // var _currentSeed = new Label();
            // _currentSeed.text = "current seed: " + configuration.currentRandomSeed.ToString();

            var _useCustomRandomSeed = new PropertyField();
            _useCustomRandomSeed.BindProperty(serializedObject.FindProperty(nameof(configuration.useGlobalRandomSeed)));

            var _globalRandomSeed = new TextField();
            _globalRandomSeed.RegisterCallback<ChangeEvent<string>>(evt => 
            {
                try
                {
                    var _parsed = uint.Parse(evt.newValue);
                    if (_parsed < 0)
                    {
                        configuration.globalRandomSeed = 1;
                    }
                    else
                    {

                        configuration.globalRandomSeed = (int)_parsed;
                    }
                }
                catch
                {
                    configuration.globalRandomSeed = 1;
                }
            });

            _globalRandomSeed.value = configuration.globalRandomSeed.ToString();
            
            var _settingsContainer = new VisualElement();
            _settingsContainer.SetBorder (1);
            _settingsContainer.SetPadding(5, 5, 5, 5);

            var _settingsFoldout = new FoldoutElement("Settings", (x) => {});            
            
            _settingsFoldout.AddContent(TileWorldCreatorUIElements.Separator("Settings"));
            _settingsFoldout.AddContent(_width);
            _settingsFoldout.AddContent(_height);
            _settingsFoldout.AddContent(_cellSize);
            _settingsFoldout.AddContent(_clusterSize);
            _settingsFoldout.AddContent(_mergePreviewTextures);
#if TWC_DEBUG
            _settingsFoldout.AddContent(_useParallel);
#endif
            
            _settingsFoldout.AddContent(TileWorldCreatorUIElements.Separator("Mesh Generation"));
            _settingsFoldout.AddContent(_mergeTiles);
            _settingsFoldout.AddContent(_shadowCastingMode);
            _settingsFoldout.AddContent(_objectLayer);
            _settingsFoldout.AddContent(_renderingLayer);
            _settingsFoldout.AddContent(_colliderType);
            _settingsFoldout.AddContent(_tileColliderHeight);
            _settingsFoldout.AddContent(_tileColliderExtrusionHeight);
            _settingsFoldout.AddContent(_invertWalls);
            

            _settingsFoldout.AddContent(TileWorldCreatorUIElements.Separator("Random seed"));
            // _settingsFoldout.AddContent(_currentSeed);
            _settingsFoldout.AddContent(_useCustomRandomSeed);
            _settingsFoldout.AddContent(_globalRandomSeed);

            root.Add(_settingsFoldout);

            var _executionButtons = new VisualElement();
            _executionButtons.style.alignContent = Align.Center;
            _executionButtons.style.justifyContent = Justify.Center;
            _executionButtons.style.backgroundColor = darkGrey;
            _executionButtons.style.flexDirection = FlexDirection.Row;
            _executionButtons.SetPadding(2, 4, 2, 2);
            _executionButtons.style.height = 46;
            _executionButtons.style.marginTop = 10;

            var _executeBlueprints = new Button();
            _executeBlueprints.style.backgroundImage = TileWorldCreatorUtilities.LoadImage("executeStack.twc");
            _executeBlueprints.style.width = 38;
            _executeBlueprints.style.height = 38;
            _executeBlueprints.SetPadding(4, 4, 4, 4);
            _executeBlueprints.RegisterCallback<ClickEvent>(evt => 
            {
                if (manager == null)
                {
                     manager = FindManager();
                }

                if (manager == null)
                {
                    Debug.LogError("No manager found");
                }
                
                configuration.ExecuteBlueprintLayers(manager);
            });

            var _executeBuildLayers = new Button();
            _executeBuildLayers.style.backgroundImage = TileWorldCreatorUtilities.LoadImage("executeStackComplete.twc");
            _executeBuildLayers.style.width = 38;
            _executeBuildLayers.RegisterCallback<ClickEvent>(evt => 
            {
                if (manager == null)
                {
                     manager = FindManager();
                }

                if (manager == null)
                {
                    Debug.LogError("No manager found");
                }

                configuration.ExecuteBlueprintLayers(manager);
                configuration.ExecuteBuildLayers(manager, true);
            });

            _executionButtons.Add(_executeBlueprints);
            _executionButtons.Add(_executeBuildLayers);

            root.Add(_executionButtons);

            root.Add(layersRoot);
            root.Add(buildLayersRoot);

            BuildBlueprintLayers();
            BuildBuildLayers();


            return root;
        }

        TileWorldCreatorManager FindManager()
        {
            var _manager = Selection.activeGameObject.GetComponent<TileWorldCreatorManager>();

            if (_manager == null)
            {
                var _managers = GameObject.FindObjectsByType<TileWorldCreatorManager>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
                for (int i = 0; i < _managers.Length; i++)
                {
                    if (_managers[i].configuration == configuration)
                    {
                        return _managers[i];
                    }
                }
            }
            else
            {
                return _manager;
            }

            return null;
        }

    #region BlueprintLayers

        public void BuildBlueprintLayers()
        {

            layersRoot.Clear();
            layerFoldouts = new List<LayerFoldoutElement>();
            previewTextures = new List<VisualElement>();

            var _addFolder = new Button();
            _addFolder.text = "+ Folder";
            _addFolder.RegisterCallback<ClickEvent>(evt => 
            {
                configuration.blueprintLayerFolders.Add(new BlueprintLayerFolder("New Folder"));

                serializedObject.Update();
                serializedObject.ApplyModifiedProperties();

                BuildBlueprintLayers();
            });

           

            if (configuration.blueprintLayerFolders.Count == 0)
            {
                var _rootFolder = new BlueprintLayerFolder("Root");

                for (int i = 0; i < configuration.tileMapLayers.Count; i++)
                {            
                    _rootFolder.assignedBlueprintLayers.Add(configuration.tileMapLayers[i].guid);   
                }

                configuration.blueprintLayerFolders.Add(_rootFolder);
            }

            if (blueprintLayersListView == null)
            {
                var _path = TileWorldCreator.Utilities.TileWorldCreatorUtilities.GetRelativeResPath() + "/CustomListView.uss";
                var _pathLight =  TileWorldCreator.Utilities.TileWorldCreatorUtilities.GetRelativeResPath() + "/CustomListViewLight.uss";
                StyleSheet uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorGUIUtility.isProSkin ? _path : _pathLight);

                blueprintLayersListView = new ListView();
                blueprintLayersListView.SetMargin(3, 3, 3, 3);
                blueprintLayersListView.styleSheets.Add(uss);
                blueprintLayersListView.itemsSource =  configuration.blueprintLayerFolders;
                blueprintLayersListView.makeItem = () => { return new BlueprintLayerFolderListViewElement(configuration, this);};
                blueprintLayersListView.bindItem = (element, i) =>
                {
                    try
                    {
                        (element as BlueprintLayerFolderListViewElement).Bind(configuration.blueprintLayerFolders[i]);
                    }catch{}
                };

                blueprintLayersListView.reorderable = true;
                blueprintLayersListView.reorderMode = ListViewReorderMode.Animated;
                blueprintLayersListView.showBorder = true;
                blueprintLayersListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
                blueprintLayersListView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
                blueprintLayersListView.selectionType = SelectionType.Single;
                
                blueprintLayersListView.selectionChanged += (elements) =>
                {

                };

                blueprintLayersListView.itemsChosen += (elements) =>
                {
                    
                };
                blueprintLayersListView.itemIndexChanged += (int _from, int _to) => 
                {

                };

            }
            else
            {
                blueprintLayersListView.itemsSource =  configuration.blueprintLayerFolders;
                blueprintLayersListView.RefreshItems();
            }


            layersRoot.style.backgroundColor = EditorGUIUtility.isProSkin ? grey : greyLight;

            var _layerHeader = new VisualElement();
            _layerHeader.style.flexDirection = FlexDirection.Row;
            _layerHeader.style.alignItems = Align.Center;

            var _layerIcon = new VisualElement();
            _layerIcon.style.backgroundImage = TileWorldCreatorUtilities.LoadImage("blueprintLayers.twc");
            _layerIcon.style.width = 30;
            _layerIcon.style.height = 30;

            var _layerTitle = new Label();
            _layerTitle.text = " Blueprint Layers";
            _layerTitle.style.fontSize = 18;
            _layerTitle.style.flexGrow = 1;
            _layerTitle.style.unityFontStyleAndWeight = FontStyle.Bold;

            layersRoot.Add(_layerHeader);

            
            _layerHeader.Add(_layerIcon);
            _layerHeader.Add(_layerTitle);
            _layerHeader.Add(_addFolder);
            layersRoot.Add(blueprintLayersListView);

            // Build or Update preview textures
            EditorApplication.delayCall += () =>
            {
                for (int i = 0; i < configuration.blueprintLayerFolders.Count; i++)
                {
                    for (int j = 0; j < configuration.blueprintLayerFolders[i].blueprintLayers.Count; j++)
                    {
                        if (configuration.blueprintLayerFolders[i].blueprintLayers[j].previewTexture == null)
                        {
                            configuration.blueprintLayerFolders[i].blueprintLayers[j].UpdatePreviewTexture(null);
                        }
                    }
                }    
                
                blueprintLayersListView.RefreshItems();
                
                root.MarkDirtyRepaint();
            };
        }

        public void AddNEWBlueprintLayer(BlueprintLayerFolder folder = null)
        {
            var _layer = ScriptableObject.CreateInstance(typeof(BlueprintLayer));
            AssetDatabase.AddObjectToAsset(_layer, configuration);

            _layer.hideFlags = HideFlags.HideInHierarchy;
            
            EditorUtility.SetDirty(_layer);
            string assetPath = AssetDatabase.GetAssetPath(_layer);
            var _guidString = AssetDatabase.AssetPathToGUID(assetPath);
            GUID _guid = GUID.Generate();
            GUID.TryParse(_guidString, out _guid);
            AssetDatabase.SaveAssetIfDirty(_guid);

            if (folder == null)
            {
                configuration.blueprintLayerFolders.FirstOrDefault().blueprintLayers.Add(_layer as BlueprintLayer);
            }
            else
            {
                folder.blueprintLayers.Add(_layer as BlueprintLayer);
            }

           (_layer as BlueprintLayer).SetAsset(configuration);

            serializedObject.Update();
            serializedObject.ApplyModifiedProperties();
            BuildBlueprintLayers();

            configuration.layerChanged = true;
            configuration.NotifyLayerChanged();
        }

    #endregion


    #region BuildLayers
            
        public void BuildBuildLayers()
        {
            buildLayersRoot.Clear();

            buildLayerFoldouts = new List<LayerFoldoutElement>();

            var _addFolder = new Button();
            _addFolder.text = "+ Folder";
            _addFolder.RegisterCallback<ClickEvent>(evt => 
            {
                configuration.buildLayerFolders.Add(new BuildLayerFolder("New Folder"));

                serializedObject.Update();
                serializedObject.ApplyModifiedProperties();

                BuildBuildLayers();
            });


            var _layerHeader = new VisualElement();
            _layerHeader.style.flexDirection = FlexDirection.Row;
            _layerHeader.style.alignItems = Align.Center;

             var _layerIcon = new VisualElement();
            _layerIcon.style.backgroundImage = TileWorldCreatorUtilities.LoadImage("buildLayers.twc");
            _layerIcon.style.width = 30;
            _layerIcon.style.height = 30;

            var _layerTitle = new Label();
            _layerTitle.text = " Build Layers";
            _layerTitle.style.fontSize = 18;
            _layerTitle.style.flexGrow = 1;
            _layerTitle.style.unityFontStyleAndWeight = FontStyle.Bold;


            buildLayersRoot.style.backgroundColor = grey;
        

            _layerHeader.Add(_layerIcon);
            _layerHeader.Add(_layerTitle);
            _layerHeader.Add(_addFolder);

            buildLayersRoot.Add(_layerHeader);


            if (buildLayersListView == null)
            {
                var _path = TileWorldCreator.Utilities.TileWorldCreatorUtilities.GetRelativeResPath() + "/CustomListView.uss";
                var _pathLight = TileWorldCreator.Utilities.TileWorldCreatorUtilities.GetRelativeResPath() + "/CustomListViewLight.uss";
                StyleSheet uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(EditorGUIUtility.isProSkin ? _path : _pathLight);

                buildLayersListView = new ListView();
                buildLayersListView.SetMargin(3, 3, 3, 3);
                buildLayersListView.styleSheets.Add(uss);
                buildLayersListView.itemsSource = configuration.buildLayerFolders;
                buildLayersListView.makeItem = () => { return new BuildLayerFolderListViewElement(configuration, this); };
                buildLayersListView.bindItem = (element, i) =>
                {
                    try
                    {
                        if (element == null) return;
                        (element as BuildLayerFolderListViewElement).Bind(configuration.buildLayerFolders[i]);
                    }catch{}
                };
                buildLayersListView.reorderable = true;
                buildLayersListView.reorderMode = ListViewReorderMode.Animated;
                 buildLayersListView.showBorder = true;
                buildLayersListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
                buildLayersListView.showAlternatingRowBackgrounds = AlternatingRowBackground.None;
                buildLayersListView.selectionType = SelectionType.Single;
            }
            else
            {
                buildLayersListView.itemsSource =  configuration.buildLayerFolders;
                buildLayersListView.RefreshItems();
            }


            buildLayersRoot.Add(buildLayersListView);
        }

        public void AddNewBuildLayer(BuildLayerFolder folder = null)
        {
            var _availableLayers = TypeCache.GetTypesDerivedFrom(typeof(BuildLayer)).ToList();
            // Create context menu
            var _menu = new GenericMenu();
            for (int i = 0; i < _availableLayers.Count; i++)
            {
                var _layerType = _availableLayers[i];
                var _attribute = _layerType.GetCustomAttribute<BuildLayerAttribute>();
                var _layerName = _layerType.Name;
                if (_attribute != null)
                {
                    _layerName = _attribute.layerTypeName;
                }

                _menu.AddItem(new GUIContent(_layerName), false, () =>
                {
                    var _layer = ScriptableObject.CreateInstance(_layerType);
                    AssetDatabase.AddObjectToAsset(_layer, configuration);

                    _layer.hideFlags = HideFlags.HideInHierarchy;

                    EditorUtility.SetDirty(_layer);
                    string assetPath = AssetDatabase.GetAssetPath(_layer);
                    var _guidString = AssetDatabase.AssetPathToGUID(assetPath);
                    GUID _guid = GUID.Generate();
                    GUID.TryParse(_guidString, out _guid);
                    AssetDatabase.SaveAssetIfDirty(_guid);

                    if (folder == null)
                    {
                        configuration.buildLayerFolders.FirstOrDefault().buildLayers.Add(_layer as BuildLayer);
                    }
                    else
                    {
                        folder.buildLayers.Add(_layer as BuildLayer);
                    }

                    serializedObject.Update();
                    serializedObject.ApplyModifiedProperties();
                    BuildBuildLayers();
                });
            }
            
            _menu.ShowAsContext();
        }

    #endregion
    }
}
#endif