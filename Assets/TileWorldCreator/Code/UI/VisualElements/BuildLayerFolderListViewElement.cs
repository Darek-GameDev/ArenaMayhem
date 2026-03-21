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
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using GiantGrey.TileWorldCreator.Attributes;
using GiantGrey.TileWorldCreator.Utilities;
using GiantGrey.TileWorldCreator.Components;

namespace GiantGrey.TileWorldCreator.UI
{
    public class BuildLayerFolderListViewElement : VisualElement
    {
        Foldout folder;
        Configuration asset;
        ConfigurationEditor editor;
        public BuildLayerFolder buildLayerFolder;

        public BuildLayerFolderListViewElement(Configuration _asset, ConfigurationEditor _editor)
        {
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
                editor.AddNewBuildLayer(buildLayerFolder);
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
                if (buildLayerFolder == null) return;
                buildLayerFolder.foldoutState = folder.value; 
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
                buildLayerFolder.folderName = _renameField.value;
                folder.text = buildLayerFolder.folderName;
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
                    
                    _renameField.value = buildLayerFolder.folderName;
                });

                if (buildLayerFolder.buildLayers.Count > 0)
                {
                    _genericMenu.AddDisabledItem(new GUIContent("Delete Folder"));
                }
                else
                {
                    _genericMenu.AddItem(new GUIContent("Delete Folder"), false, () => 
                    {
                        asset.buildLayerFolders.Remove(buildLayerFolder);
                        // BuildLayers();
                        editor.BuildBuildLayers();
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

        public void Bind(BuildLayerFolder _buildLayerFolder)
        {
            buildLayerFolder = _buildLayerFolder;
            BuildLayers();
        }


        public void BuildLayers()
        {
            folder.text = buildLayerFolder.folderName;
            folder.Clear();
            folder.value = buildLayerFolder.foldoutState;


            this.name = buildLayerFolder.guid;

            for (int i = 0; i < buildLayerFolder.buildLayers.Count; i++)
            {
                var _attribute = buildLayerFolder.buildLayers[i].GetType().GetCustomAttribute<BuildLayerAttribute>();
                Texture2D _icon = null;
                if (_attribute != null)
                {
                    if (!string.IsNullOrEmpty(_attribute.iconPath))
                    {
                        _icon = TileWorldCreatorUtilities.LoadImage(_attribute.iconPath);
                    }
                }
                
                var _index = i;
                var _buildLayer = buildLayerFolder.buildLayers[_index];
                var _so = new SerializedObject(_buildLayer);
                var _isEnabledProperty = _so.FindProperty("isEnabled");

                var _layerItem = new LayerFoldoutElement(_buildLayer.layerName, _isEnabledProperty, 
                asset, editor,
                _buildLayer.guid, 
                buildLayerFolder.guid,
                null,
                false,
                (x) => { _buildLayer.foldoutState = x; },
                () => 
                { 
                    RemoveLayer(_buildLayer); 
                },
                // On Execute
                () => 
                { 
                    asset.ExecuteBuildLayer(_buildLayer.guid, null);
                }, 
                // Clear layer
                () => 
                {
                    _buildLayer.ResetLayer();
                },
                // Move Up
                () => 
                {
                    // move layer up
                    if (_index > 0)
                    {
                        var _layer = buildLayerFolder.buildLayers[_index];
                        buildLayerFolder.buildLayers.RemoveAt(_index);
                        buildLayerFolder.buildLayers.Insert(_index - 1, _layer);

                        BuildLayers();
                    }
                },
                // Move Down
                () => 
                {
                    // move layer down
                    if (_index < buildLayerFolder.buildLayers.Count - 1)
                    {
                        var _layer = buildLayerFolder.buildLayers[_index];
                        buildLayerFolder.buildLayers.RemoveAt(_index);
                        buildLayerFolder.buildLayers.Insert(_index + 1, _layer);

                        BuildLayers();
                    }
                },
                // Duplicate 
                () => 
                {
                   DuplicateLayer(buildLayerFolder.buildLayers[_index]);

                }, null, _icon, new BuildLayerItemDragManipulator(this, asset, editor, _buildLayer.guid, buildLayerFolder.guid));

                _layerItem.OpenFoldout(_buildLayer.foldoutState);
                

                folder.Add(_layerItem);

                _layerItem.AddContent(_buildLayer.CreateInspectorGUI(asset, editor, _layerItem));

            }

        }

        void RemoveLayer(BuildLayer _layer)
        {
            buildLayerFolder.buildLayers.Remove(_layer);
        
            AssetDatabase.RemoveObjectFromAsset(_layer);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            MonoBehaviour.DestroyImmediate(_layer);
            var _layers = GameObject.FindObjectsByType<LayerIdentifier>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
            foreach(var _layerIdentifier in _layers)
            {
                if (_layerIdentifier.guid == _layer.guid)
                {
                    MonoBehaviour.DestroyImmediate(_layerIdentifier.gameObject);
                }
            }

            BuildLayers();
        }

        void DuplicateLayer(BuildLayer _layer)
        {
            var _newLayer = MonoBehaviour.Instantiate(_layer);
            _newLayer.guid = System.Guid.NewGuid().ToString();
            _newLayer.hideFlags = HideFlags.HideInHierarchy;
            _newLayer.hierarchyLayerID = _newLayer.guid;


            AssetDatabase.AddObjectToAsset(_newLayer, asset);
            EditorUtility.SetDirty(_newLayer);
            string assetPath = AssetDatabase.GetAssetPath(_newLayer);
            var _guidString = AssetDatabase.AssetPathToGUID(assetPath);
            GUID _guid = GUID.Generate();
            GUID.TryParse(_guidString, out _guid);
            AssetDatabase.SaveAssetIfDirty(_guid);
            

            buildLayerFolder.buildLayers.Add(_newLayer);

            BuildLayers();
        }
    }

    
}
#endif