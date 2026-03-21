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
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using GiantGrey.TileWorldCreator.UI;
using GiantGrey.TileWorldCreator.Utilities;

namespace GiantGrey.TileWorldCreator
{
    [CustomEditor(typeof(TileWorldCreatorManager))]
    public class TileWorldCreatorManagerEditor : Editor
    {
        TileWorldCreatorManager _manager;

        VisualElement root;
    

        public void OnEnable()
        {
            try
            {
                _manager = (TileWorldCreatorManager)target;
                _manager.isInspected = true;  // mark as inspected
            }
            catch { }
        }

        void OnDisable()
        {
            try
            {
                if (_manager != null)
                    _manager.isInspected = false; // unmark when inspector deselects
            }
            catch { }
        }
        
        public override VisualElement CreateInspectorGUI()
        {
            root = new VisualElement();

            BuildEditor();

            return root;
        }

        void BuildEditor()
        {
            root.Clear();

            var _header = new VisualElement();
            _header.style.backgroundColor = new Color(36f/255f, 36f/255f,36f/255f);
            _header.style.marginBottom = 2;
            _header.style.alignItems = Align.Center;

            var _logoBanner = TileWorldCreatorUtilities.LoadImage("LogoBanner.twc");
            var _logo = new VisualElement();
            _logo.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center); //BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleToFit);
            _logo.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center); //BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleToFit);   
            _logo.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);  //BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(ScaleMode.ScaleToFit);
            _logo.style.backgroundImage = _logoBanner;
            _logo.style.width = 420;
            _logo.style.height = 108;

            _header.Add(_logo);

            root.Add(_header);

#region Configuration
            var _config = new VisualElement();
            _config.SetPadding(5, 5, 5, 5);
            _config.SetBorder(1);
            _config.style.marginBottom = 2;

            _config.Add(TileWorldCreatorUIElements.Separator("Configuration"));

            var _configContainer = new VisualElement();
            _configContainer.style.flexDirection = FlexDirection.Row;
            _configContainer.style.flexGrow = 1;

            var _asset = new ObjectField();
            _asset.label = "Configuration";
            _asset.allowSceneObjects = false;
            _asset.objectType = typeof(Configuration);

            var _createConfiguration = new Button();
            _createConfiguration.text = "Create";

            _createConfiguration.RegisterCallback<ClickEvent>(evt => 
            {
                var _path = EditorUtility.SaveFilePanelInProject("Save configuration", "TileWorldCreatorConfiguration", "asset", "", "Assets");
                if (!string.IsNullOrEmpty(_path))
                {
                    var _configuration = ScriptableObject.CreateInstance<Configuration>();
                    AssetDatabase.CreateAsset(_configuration, _path);

                    _manager.configuration = _configuration as Configuration;
                }
                
            });
            
            // Wait one frame before registering callback
            _asset.schedule.Execute (() => _asset.RegisterCallback<ChangeEvent<Object>>((evt) => 
            { 
                if (evt.previousValue as Configuration != _manager.configuration)
                {
                    BuildEditor();
                }
            }));
            _asset.BindProperty(serializedObject.FindProperty(nameof(_manager.configuration)));

            _configContainer.Add(_asset);
            _configContainer.Add(_createConfiguration);
            
            _config.Add(_configContainer);
            

            var _showClusterCells = new PropertyField();
            _showClusterCells.BindProperty(serializedObject.FindProperty(nameof(_manager.showClusterCellsDebug)));
            
            _config.Add(_showClusterCells);
#endregion

#region Export
            var _export = new VisualElement();
            _export.SetPadding(5, 5, 5, 5);
            _export.SetBorder(1);
            _export.style.marginBottom = 2;

            var _exportContainer = new VisualElement();
            _exportContainer.style.flexDirection = FlexDirection.Row;

            _export.Add(TileWorldCreatorUIElements.Separator("Prefab"));

            _export.Add(_exportContainer);

            var _exportInfo = new HelpBox();
            _exportInfo.messageType = HelpBoxMessageType.Info;
            _exportInfo.text = "Create a prefab from this TileWorldCreator object when Merge Tiles is enabled. Re-Save after rebuilding the map.";

            var _exportButton = new Button();
            _exportButton.style.height = 36;
            _exportButton.text = "Save as Prefab";
            _exportButton.tooltip = "Create a prefab from this TileWorldCreator object when Merge Tiles is enabled. Re-Save after rebuilding the map.";
            _exportButton.RegisterCallback<ClickEvent>(evt =>
            {
                SaveMeshAndPrefab.SaveMeshesAndPrefab(_manager.gameObject);
            });

            _exportContainer.Add(_exportButton);
            _exportContainer.Add(_exportInfo);

#endregion

            root.Add(_config);
            root.Add(_export);

            if (_manager.configuration != null)
            {
                var _editor = Editor.CreateEditor(_manager.configuration) as ConfigurationEditor;
                root.Add(_editor.CreateInspectorGUI());
            }
        }
    }
}
#endif