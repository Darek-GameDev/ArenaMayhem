
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using GiantGrey.TileWorldCreator.Attributes;
using GiantGrey.TileWorldCreator.Utilities;

namespace GiantGrey.TileWorldCreator.UI
{
    public class BlueprintLayerFolderListViewElement : VisualElement
    {

        Foldout folder;
        Configuration asset;
        ConfigurationEditor editor;
        public BlueprintLayerFolder blueprintLayerFolder;


        public BlueprintLayerFolderListViewElement(Configuration _asset, ConfigurationEditor _editor)
        {
            // this.style.height = 40;
            this.style.flexGrow = 1;
            asset = _asset;
            editor = _editor;

            var _horizontalContainer = new VisualElement();
            _horizontalContainer.style.flexDirection = FlexDirection.Row;
            _horizontalContainer.style.flexGrow = 1;
            _horizontalContainer.style.flexShrink = 0;
            _horizontalContainer.SetPadding(3, 3, 3, 3);

            var _verticalContainer = new VisualElement();
            _verticalContainer.style.flexGrow = 1;

            var _folderIcon = new VisualElement ();
            _folderIcon.style.width = 20;
            _folderIcon.style.height = 20;
            _folderIcon.style.backgroundImage = TileWorldCreatorUtilities.LoadImage("folder.twc");
            _folderIcon.style.marginRight = 15;
            _folderIcon.style.marginLeft = -10;
            _folderIcon.style.unityBackgroundImageTintColor = new Color(128f / 255f, 128f / 255f, 128f / 255f);


            folder = new Foldout();
            folder.text = "Root";
            folder.Q<Label>().style.fontSize = 14;

            folder.contentContainer.style.flexGrow = 1;
            folder.contentContainer.style.flexShrink = 0;


            var _renameField = new TextField();
            _renameField.style.display = DisplayStyle.None;
        
            var _addLayerButton = new Button();
            _addLayerButton.style.position = Position.Absolute;
#if UNITY_6000_2
            _addLayerButton.style.translate = new Vector2(folder.resolvedStyle.width, 0);
#else
            _addLayerButton.transform.position = new Vector2(folder.resolvedStyle.width, 0);
#endif

            _addLayerButton.style.width = 20;
            _addLayerButton.style.height = 20;
            _addLayerButton.text = "+";
            _addLayerButton.RegisterCallback<ClickEvent>((evt) => 
            {
                editor.AddNEWBlueprintLayer(blueprintLayerFolder);
            });

            var _editButton = new Button();
            _editButton.style.position = Position.Absolute;
#if UNITY_6000_2
            _editButton.style.translate = new Vector2(folder.resolvedStyle.width, folder.resolvedStyle.translate.y);
#else
            _editButton.transform.position = new Vector2(folder.resolvedStyle.width, folder.transform.position.y);
#endif
            _editButton.style.width = 20;
            _editButton.style.height = 20;
            _editButton.text = "...";

            folder.RegisterCallback<GeometryChangedEvent>(e => 
            {
                
#if UNITY_6000_2
                _addLayerButton.style.translate = new Vector2(folder.resolvedStyle.width, 0);
                _editButton.style.translate = new Vector2(folder.resolvedStyle.width - 22, 0);
#else
                _addLayerButton.transform.position = new Vector2(folder.resolvedStyle.width, 0);
                _editButton.transform.position = new Vector2(folder.resolvedStyle.width - 22, 0);
#endif
                if (blueprintLayerFolder != null)
                {
                    blueprintLayerFolder.foldoutState = folder.value; 
                }
            });

            var _renameOkButton = new Button();
            _renameOkButton.style.height = 20;
            _renameOkButton.text = "OK";
        

            var _renameCancelButton = new Button();
            _renameCancelButton.style.height = 20;
            _renameCancelButton.text = "Cancel";
            _renameOkButton.style.display = DisplayStyle.None;
            _renameCancelButton.style.display = DisplayStyle.None;

            _renameCancelButton.RegisterCallback<ClickEvent>(evt => 
            {
                folder.style.display = DisplayStyle.Flex;
                _editButton.style.display = DisplayStyle.Flex;
                _addLayerButton.style.display = DisplayStyle.Flex;
                _renameField.style.display = DisplayStyle.None;
                _renameOkButton.style.display = DisplayStyle.None;
                _renameCancelButton.style.display = DisplayStyle.None;
                _verticalContainer.style.flexGrow = 1;
                _renameField.style.flexGrow = 0;
            });

            _renameOkButton.RegisterCallback<ClickEvent>(evt => 
            {
                blueprintLayerFolder.folderName = _renameField.value;
                folder.text = blueprintLayerFolder.folderName;
                folder.style.display = DisplayStyle.Flex;
                _editButton.style.display = DisplayStyle.Flex;
                _addLayerButton.style.display = DisplayStyle.Flex;
                _renameField.style.display = DisplayStyle.None;
                _renameOkButton.style.display = DisplayStyle.None;
                _renameCancelButton.style.display = DisplayStyle.None;
                _verticalContainer.style.flexGrow = 1;
                _renameField.style.flexGrow = 0;

                BuildLayers();
            });

            _editButton.RegisterCallback<ClickEvent>(evt => 
            {
                var _genericMenu = new GenericMenu();
                _genericMenu.AddItem(new GUIContent("Rename Folder"), false, () => 
                {
                    folder.style.display = DisplayStyle.None;
                    _renameField.style.display = DisplayStyle.Flex;
                    _verticalContainer.style.flexGrow = 0;
                    _renameField.style.flexGrow = 1;
                    _renameOkButton.style.display = DisplayStyle.Flex;
                    _renameCancelButton.style.display = DisplayStyle.Flex;
                    _editButton.style.display = DisplayStyle.None;
                    _addLayerButton.style.display = DisplayStyle.None;
                    
                    _renameField.value = blueprintLayerFolder.folderName;
                });

                if (blueprintLayerFolder.blueprintLayers.Count > 0)
                {
                    _genericMenu.AddDisabledItem(new GUIContent("Delete Folder"));
                }
                else
                {
                    _genericMenu.AddItem(new GUIContent("Delete Folder"), false, () => 
                    {
                        asset.blueprintLayerFolders.Remove(blueprintLayerFolder);
                        // BuildLayers();
                        editor.BuildBlueprintLayers();
                    });
                }

                _genericMenu.ShowAsContext();

            });

        

            _verticalContainer.Add(folder);
            _horizontalContainer.Add(_folderIcon);
            _horizontalContainer.Add(_verticalContainer);   
            _horizontalContainer.Add(_renameField);
            _horizontalContainer.Add(_renameOkButton);
            _horizontalContainer.Add(_renameCancelButton);
            _horizontalContainer.Add(_editButton);
            _horizontalContainer.Add(_addLayerButton);

            this.Add(_horizontalContainer);
        }

        public void Bind(BlueprintLayerFolder _blueprintLayerFolder)
        {
            blueprintLayerFolder = _blueprintLayerFolder;
            BuildLayers();
        }

        public void BuildLayers()
        {
            folder.value = blueprintLayerFolder.foldoutState;
            folder.text = blueprintLayerFolder.folderName;
            folder.Clear();
            
            this.name = blueprintLayerFolder.guid;

            for (int i = 0; i < blueprintLayerFolder.blueprintLayers.Count; i++)
            {
                // if (blueprintLayerFolder.blueprintLayers[i].previewTexture == null)
                // {
                //     blueprintLayerFolder.blueprintLayers[i].UpdatePreviewTexture(null);
                // }

                var _index = i;
                var _blueprintLayer = blueprintLayerFolder.blueprintLayers[_index];
                var _so = new SerializedObject(_blueprintLayer);
                var _isEnabledProperty = _so.FindProperty("isEnabled");
                var _paintLockProperty = _so.FindProperty("lockFromPaint");

                var _layerItem = new LayerFoldoutElement(blueprintLayerFolder.blueprintLayers[i].layerName, _isEnabledProperty, 
                asset, editor,
                blueprintLayerFolder.blueprintLayers[i].guid, 
                blueprintLayerFolder.guid,
                blueprintLayerFolder.blueprintLayers[i].previewTexture,
                true, 
                (x) => { _blueprintLayer.foldoutState = x; },
                () => 
                { 
                    RemoveLayer(_blueprintLayer); 
                },
                // Execute layer
                () => 
                { 
                    asset.ExecuteBlueprintLayer(_blueprintLayer.guid);
                }, 
                // Clear layer
                null,
                // Move Up
                () => 
                {
                    // move layer up
                    var _iindex = i;
                    if (_index > 0)
                    {
                        var _layer = blueprintLayerFolder.blueprintLayers[_index];
                        blueprintLayerFolder.blueprintLayers.RemoveAt(_index);
                        blueprintLayerFolder.blueprintLayers.Insert(_index - 1, _layer);

                        BuildLayers();
                    }
                },
                // Move Down
                () => 
                {
                    // move layer down
                    if (_index < blueprintLayerFolder.blueprintLayers.Count - 1)
                    {
                        var _layer = blueprintLayerFolder.blueprintLayers[_index];
                        blueprintLayerFolder.blueprintLayers.RemoveAt(_index);
                        blueprintLayerFolder.blueprintLayers.Insert(_index + 1, _layer);

                        BuildLayers();
                    }
                }, 
                // Duplicate
                () => 
                {
                    DuplicateLayer(blueprintLayerFolder.blueprintLayers[_index]);

                }, _paintLockProperty, null, new BlueprintLayerItemDragManipulator(this, asset, editor,  _blueprintLayer.guid, blueprintLayerFolder.guid));

                _layerItem.OpenFoldout(_blueprintLayer.foldoutState);
                

                folder.Add(_layerItem);


                var _layerSerializedObject = new SerializedObject(blueprintLayerFolder.blueprintLayers[_index]);

                var _layerHorizontalContainer = new VisualElement();
                _layerHorizontalContainer.style.flexDirection = FlexDirection.Row;
                _layerHorizontalContainer.style.flexGrow = 1;

                var _layerVerticalContainer = new VisualElement();
                _layerVerticalContainer.style.flexDirection = FlexDirection.Column;
                _layerVerticalContainer.style.flexGrow = 1;
                

                var _previewTexture = new VisualElement();
                _previewTexture.style.backgroundColor = Color.black;
                _previewTexture.style.marginRight = 5;
                _previewTexture.style.backgroundImage = blueprintLayerFolder.blueprintLayers[_index].previewTexture;
                _previewTexture.style.width = 100;
                _previewTexture.style.height = 100;
                _previewTexture.name = _blueprintLayer.guid;

                _previewTexture.RegisterCallback<ClickEvent>(evt => 
                {
                    var _panel = new BlueprintPreviewImagePopup(blueprintLayerFolder.blueprintLayers[_index].previewTexture);
                    BlueprintPreviewImagePopup.ShowPanel(Event.current.mousePosition, _panel);

                });

                editor.previewTextures.Add(_previewTexture);

                _layerHorizontalContainer.Add(_previewTexture);
                _layerHorizontalContainer.Add(_layerVerticalContainer);


                var _layerName = new TextField();
                _layerName.BindProperty(_layerSerializedObject.FindProperty("layerName"));
                _layerName.RegisterValueChangedCallback((evt) => 
                {
                    _layerItem.SetLabel(evt.newValue);
                });

                var _icon = new PropertyField();
                _icon.BindProperty(_layerSerializedObject.FindProperty("icon"));

            
                var _layerColor = new ColorField();
                _layerColor.label = "Color";
                _layerColor.BindProperty(_layerSerializedObject.FindProperty("layerColor"));

    #if TWC_DEBUG
                var _showDebugGrid = new PropertyField();
                _showDebugGrid.BindProperty(_layerSerializedObject.FindProperty("showDebugGrid"));
    #endif

                var _lockFromPaintMode = new PropertyField();
                _lockFromPaintMode.BindProperty(_layerSerializedObject.FindProperty("lockFromPaint"));

                var _customSeed = new PropertyField();
                _customSeed.BindProperty(_layerSerializedObject.FindProperty("customSeed"));

                var _useCustomSeed = new Toggle();
                _useCustomSeed.label = "Use Custom Seed";
                _useCustomSeed.BindProperty(_layerSerializedObject.FindProperty("customRandomSeed"));
                _useCustomSeed.RegisterCallback<ChangeEvent<bool>>((evt) =>
                {
                    _customSeed.style.display = evt.newValue ?  DisplayStyle.Flex : DisplayStyle.None;
                });

                var _useRandomSeed = new Toggle();
                _useRandomSeed.label = "Random Seed Override";
                _useRandomSeed.BindProperty(_layerSerializedObject.FindProperty("randomSeedOverride"));
                _useRandomSeed.RegisterCallback<ChangeEvent<bool>>((evt) => 
                {
                    _useCustomSeed.style.display = evt.newValue ?  DisplayStyle.Flex : DisplayStyle.None;
                });

                var _defaultLayerHeight = new PropertyField();
                _defaultLayerHeight.tooltip = "The default layer height is used for the assigned build layers. + Y offset";
                _defaultLayerHeight.BindProperty(_layerSerializedObject.FindProperty("defaultLayerHeight"));

    #if TWC_DEBUG
                var _layerGuid = new TextField();
                _layerGuid.BindProperty(_layerSerializedObject.FindProperty("guid"));
    #endif
                _layerVerticalContainer.Add(_layerName);
                _layerVerticalContainer.Add(_icon);
                _layerVerticalContainer.Add(_layerColor);
    #if TWC_DEBUG
    
                _layerVerticalContainer.Add(_lockFromPaintMode);
                _layerVerticalContainer.Add(_showDebugGrid);
                _layerVerticalContainer.Add(_layerGuid);
    #endif

                _layerVerticalContainer.Add(_useRandomSeed);
                _layerVerticalContainer.Add(_useCustomSeed);
                _layerVerticalContainer.Add(_customSeed);

                _layerVerticalContainer.Add(_defaultLayerHeight);

                var _layerModifierRoot = new VisualElement();

                _layerItem.AddContent(_layerHorizontalContainer); 
                _layerItem.AddContent(_layerModifierRoot);

                var _addModifierButton = new Button();
                _addModifierButton.text = "+ Modifier";
                _addModifierButton.RegisterCallback<ClickEvent>(evt => 
                {
                    var _availableModifiers = TypeCache.GetTypesDerivedFrom(typeof(BlueprintModifier)).ToList();

                    GenericMenu _menu = new GenericMenu();
                    for (int m = 0; m < _availableModifiers.Count; m++)
                    {
                        var _mIndex = m;

                        var _attribute = _availableModifiers[m].GetCustomAttribute<ModifierAttribute>();
                        var _prefix = "Custom";
                        var _name = _availableModifiers[m].Name;
                        if (_attribute != null)
                        {
                            _prefix = _attribute.category.ToString();
                            _name = _attribute.name;
                        }
                        
                        _menu.AddItem(new GUIContent(_prefix + "/" + _name), false, () => 
                        {
                            var _so = ScriptableObject.CreateInstance(_availableModifiers[_mIndex]);
                            AssetDatabase.AddObjectToAsset(_so, asset);
                            EditorUtility.SetDirty(_so);

                            _so.hideFlags = HideFlags.HideInHierarchy;


                            string assetPath = AssetDatabase.GetAssetPath(_so);
                            var _guidString = AssetDatabase.AssetPathToGUID(assetPath);
                            GUID _guid = GUID.Generate();
                            GUID.TryParse(_guidString, out _guid);
                            AssetDatabase.SaveAssetIfDirty(_guid);

                        
                            _blueprintLayer.tileMapModifiers.Add(_so as BlueprintModifier);

                            _layerSerializedObject.ApplyModifiedProperties();
                            _layerSerializedObject.Update();

                            BuildLayers();
                        });
                    }

                    _menu.ShowAsContext();
                });

                var _modifiersListView = new ListView();

                _modifiersListView.itemsSource = _blueprintLayer.tileMapModifiers;
                _modifiersListView.makeItem = () => { return new ModifierListItem(asset, _modifiersListView, _blueprintLayer);};
                _modifiersListView.bindItem = (element, i) =>
                {
                    (element as ModifierListItem).Bind(i);
                };

                _modifiersListView.reorderable = true;
                _modifiersListView.reorderMode = ListViewReorderMode.Animated;
                _modifiersListView.showBorder = true;
                _modifiersListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

                _layerModifierRoot.Add(_addModifierButton);
                _layerModifierRoot.Add(_modifiersListView);
            }
        }

        void RemoveLayer(BlueprintLayer _layer)
        {
            if (_layer.previewTexture != null)
            {
                AssetDatabase.RemoveObjectFromAsset(_layer.previewTexture);
                MonoBehaviour.DestroyImmediate(_layer.previewTexture, true);
            }
               
            for (int i = 0; i < _layer.tileMapModifiers.Count; i++)
            {
                var _modifier = _layer.tileMapModifiers[i];
                if (_modifier != null)
                {
                    AssetDatabase.RemoveObjectFromAsset(_modifier);
                    MonoBehaviour.DestroyImmediate(_modifier, true);
                }
            }

            blueprintLayerFolder.blueprintLayers.Remove(_layer);

            

            AssetDatabase.RemoveObjectFromAsset(_layer);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            MonoBehaviour.DestroyImmediate(_layer, true);

            BuildLayers();
        }

        void DuplicateLayer(BlueprintLayer _layer)
        {
            var _newLayer = MonoBehaviour.Instantiate(_layer);
            _newLayer.guid = System.Guid.NewGuid().ToString();
            _newLayer.hideFlags = HideFlags.HideInHierarchy;

            _newLayer.tileMapModifiers = new List<BlueprintModifier>();
            
            if (_newLayer.previewTexture != null)
            {
                MonoBehaviour.DestroyImmediate(_newLayer.previewTexture, true);
            }

            _newLayer.previewTexture = null;

            for (int i = 0; i < _layer.tileMapModifiers.Count; i++)
            {
                var _modifier = _layer.tileMapModifiers[i];
                var _newModifier = ScriptableObjectCloner.CloneToAsset(_modifier, asset);
                _newModifier.hideFlags = HideFlags.HideInHierarchy;
                _newLayer.tileMapModifiers.Add(_newModifier as BlueprintModifier);
            }

            AssetDatabase.AddObjectToAsset(_newLayer, asset);
            EditorUtility.SetDirty(_newLayer);
            string assetPath = AssetDatabase.GetAssetPath(_newLayer);
            var _guidString = AssetDatabase.AssetPathToGUID(assetPath);
            GUID _guid = GUID.Generate();
            GUID.TryParse(_guidString, out _guid);
            AssetDatabase.SaveAssetIfDirty(_guid);

            blueprintLayerFolder.blueprintLayers.Add(_newLayer);

            BuildLayers();
        }
    }


    public class ModifierListItem : VisualElement
    {
        VisualElement modifierRoot;
        BlueprintLayer blueprintLayer;
        int modifierIndex;
        ListView modifierListView;
        Configuration asset;

        public ModifierListItem(Configuration _asset, ListView _modifierListView, BlueprintLayer _blueprintLayer)
        {
            
            modifierRoot = new VisualElement();
            modifierRoot.style.flexDirection = FlexDirection.Row;
            modifierRoot.style.alignItems = Align.FlexStart;
            modifierRoot.style.alignContent = Align.Center;

            asset = _asset;
            blueprintLayer = _blueprintLayer;
            modifierListView = _modifierListView;

            this.Add(modifierRoot);
        }

        public void Bind(int _modifierIndex)
        {
            modifierRoot.Clear();
            modifierIndex = _modifierIndex;

            if (blueprintLayer.tileMapModifiers[modifierIndex] == null)
                return;
            
            var _serializedObject = new SerializedObject(blueprintLayer.tileMapModifiers[modifierIndex]);
            var _modifierEnable = new Toggle();
            _modifierEnable.SetMargin(4, 0, 0, 0);
            _modifierEnable.BindProperty(_serializedObject.FindProperty("isEnabled"));

            var _modifierFoldout = new Foldout();
            _modifierFoldout.value = false;
            _modifierFoldout.style.marginTop = 1;
            _modifierFoldout.style.paddingRight = 5;
            _modifierFoldout.style.flexGrow = 1;
            _modifierFoldout.text = blueprintLayer.tileMapModifiers[modifierIndex].GetType().Name;

            blueprintLayer.tileMapModifiers[modifierIndex].asset = asset;
            var _modifierInspector = blueprintLayer.tileMapModifiers[modifierIndex].BuildInspector(asset);
            var _removeModifier = new ToolbarButton();
            _removeModifier.text = "-";
            _removeModifier.style.minWidth = 22;
            _removeModifier.style.maxWidth = 22;
            _removeModifier.RegisterCallback<ClickEvent>(evt => 
            {
                AssetDatabase.RemoveObjectFromAsset(blueprintLayer.tileMapModifiers[modifierIndex]);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                MonoBehaviour.DestroyImmediate(blueprintLayer.tileMapModifiers[modifierIndex], true);
                blueprintLayer.tileMapModifiers.RemoveAt(modifierIndex);

                modifierListView.RefreshItems();
            });

            var _foldoutToggle = _modifierFoldout.Q<Toggle>();
            _foldoutToggle.style.marginLeft = 5;
            _foldoutToggle.Add(_removeModifier);

            var _ed = Editor.CreateEditor(blueprintLayer.tileMapModifiers[_modifierIndex]);
            List<SerializedProperty> _properties = new List<SerializedProperty>();
            using (var iterator = _ed.serializedObject.GetIterator())
            {
                if (iterator.NextVisible(true))
                {
                    do
                    {
                        if (iterator.name == "m_Script") continue;

                        var _property = _ed.serializedObject.FindProperty(iterator.name);
                        var _propertyField = new PropertyField(_property);
                        _propertyField.BindProperty(_property);
                        _modifierFoldout.Add(_propertyField);
                    }
                    while (iterator.NextVisible(false));
                }
            }

            _modifierFoldout.Add(_modifierInspector);
            modifierRoot.Add(_modifierEnable);
            if (_modifierFoldout.childCount > 0)
            {
                modifierRoot.Add(_modifierFoldout);
            }
            else
            {
                var _lbl = new Label(blueprintLayer.tileMapModifiers[modifierIndex].GetType().Name);
                _lbl.style.marginLeft = 7;
                _lbl.style.marginTop = 2;
                _lbl.style.flexGrow = 1;
                modifierRoot.Add(_lbl);
                modifierRoot.Add(_removeModifier);
            }
        }
    }
}
#endif