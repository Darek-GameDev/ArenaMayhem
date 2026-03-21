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
using System;
using System.Collections.Generic;
using System.Linq;
using GiantGrey.TileWorldCreator.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GiantGrey.TileWorldCreator.UI
{
    public class BlueprintLayerPopupSelector : PopupWindowContent
    {
        public static bool isOpen;
        public static Vector2 position;
        public static SerializedProperty property;
        public static Action onSelected;

        public static List<string> assignedBlueprintLayerGuids;
        public static List<string> assignedBlueprintLayerNames;

        public static Configuration configuration = null;


        ScrollView _scrollView;
        String _searchString = "";
        

        public BlueprintLayerPopupSelector() { }

        public class Filter
        {
            public string filterName;
            public List<string> packages;
        }

        public static void ShowPanel(Vector2 _pos, Configuration _configuration, ref List<string> _assignedBlueprintLayerGuids, ref List<string> _assignedBlueprintLayerNames, BlueprintLayerPopupSelector _panel, Action _onSelected)
        {
            position = _pos;
            onSelected = _onSelected;
            configuration = _configuration;
            assignedBlueprintLayerGuids = _assignedBlueprintLayerGuids;
            assignedBlueprintLayerNames = _assignedBlueprintLayerNames;

            UnityEditor.PopupWindow.Show(new Rect(_pos.x, _pos.y, 0, 0), _panel);
          
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 350);
        }

        public override void OnGUI(Rect rect){ }

        public override void OnOpen()
        {

            _scrollView = new ScrollView();
            _scrollView.style.flexGrow = 1;

            var root = editorWindow.rootVisualElement;
            var _searchContainer = new VisualElement();
            _searchContainer.SetPadding(4, 4, 4, 4);
            _searchContainer.SetMargin(4, 4, 4, 4);
            
            var _searchLabel = new Label("Search");

            var _searchContainerHorizontal = new VisualElement();
            _searchContainerHorizontal.style.flexDirection = FlexDirection.Row;
            var _searchField = new TextField();
            _searchField.style.flexGrow = 1;
            _searchField.style.maxWidth = 250;

            var _cancelSearch = new Button();
            _cancelSearch.text = "x";
            _cancelSearch.RegisterCallback<ClickEvent>(evt => 
            {
                _searchString = "";
                _searchField.value = "";
                RefreshScrollView();
            });

            _searchContainerHorizontal.Add(_searchField);
            _searchContainerHorizontal.Add(_cancelSearch);
            
            _searchField.RegisterValueChangedCallback((evt) => 
            {
                _searchString = evt.newValue;
                RefreshScrollView();
            });

         
            _searchContainer.Add(_searchLabel);
            _searchContainer.Add(_searchContainerHorizontal);

            root.Add(_searchContainer);
            root.Add(_scrollView);

            root.schedule.Execute(() => _searchField.Focus()).ExecuteLater(100);

            RefreshScrollView();
        }

        void RefreshScrollView()
        {
            _scrollView.Clear();

            // Get all available blueprint layers
            var _layers = configuration.blueprintLayerFolders
            .SelectMany(folder => folder.blueprintLayers)
            .Where(layer => layer != null)
            .ToList();

            foreach (var _layer in _layers)
            {
                var _break = false;
                if (!string.IsNullOrEmpty(_searchString))
                {
                    List<string> _splitted = _searchString.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(item => item.Trim())               // Remove leading/trailing spaces
                    .Where(item => !string.IsNullOrEmpty(item)) // Filter again in case only spaces were there
                    .ToList();

                    if (_splitted.Count > 1 && _splitted != null)
                    {
                        var _contains = false;
                        for (int i = 0; i < _splitted.Count; i++)
                        {
                            if (_layer.layerName.ToLower().Contains(_splitted[i].ToLower()))
                            {
                                _contains = true;
                            }
                        }
                        if (!_contains)
                        {
                            _break = true;
                        }
                    }
                    else
                    {
                        _searchString = _searchString.Replace(",", "");

                        if (!_layer.layerName.ToLower().Contains(_searchString.ToLower()))
                        {
                            _break = true;
                        }
                    }
                }

                if (_break)
                {
                    continue;
                }

                var _item = new VisualElement();
                _item.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.Grey.GetColor() : TileWorldCreatorColor.VeryLightGrey.GetColor();
                _item.style.flexDirection = FlexDirection.Row;
                _item.style.alignItems = Align.Center;
                _item.style.alignContent = Align.Center;
                _item.SetBorder(1);
                _item.SetPadding(2, 2, 2, 2);
                _item.SetMargin(1, 1, 4, 4);

               
                var _label = new Label(_layer.layerName);
                var _check = new VisualElement();
                _check.style.width = 12;
                _check.style.height = 12;
                _check.style.minWidth = 12;
                _check.style.minHeight = 12;
                _check.style.marginRight = 5;
                if (assignedBlueprintLayerGuids.Contains(_layer.guid))
                {
                    _check.style.backgroundImage = TileWorldCreatorUtilities.LoadImage("checkMark.twc");
                    _check.style.unityBackgroundImageTintColor = EditorGUIUtility.isProSkin
                        ? Color.white
                        : TileWorldCreatorColor.PaleBlue.GetColor();
                }

                _item.Add(_check);
                _item.Add(_label);
                _item.style.flexGrow = 1;

                _item.RegisterCallback<MouseEnterEvent>(evt => _item.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.LightGrey.GetColor() : TileWorldCreatorColor.AlmostWhite.GetColor());
                _item.RegisterCallback<MouseLeaveEvent>(evt =>
                    _item.style.backgroundColor = EditorGUIUtility.isProSkin
                        ? TileWorldCreatorColor.Grey.GetColor()
                        : TileWorldCreatorColor.VeryLightGrey.GetColor());
                _item.RegisterCallback<ClickEvent>(evt =>
                {

                    if (assignedBlueprintLayerGuids.Contains(_layer.guid))
                    {
                        int _index = assignedBlueprintLayerGuids.FindIndex(a => a == _layer.guid);
                        assignedBlueprintLayerNames.RemoveAt(_index);
                        assignedBlueprintLayerGuids.Remove(_layer.guid);
                    }
                    else
                    {
                        assignedBlueprintLayerGuids.Add(_layer.guid);

                        int _index = assignedBlueprintLayerGuids.FindIndex(a => a == _layer.guid);
                        if (_index >= assignedBlueprintLayerNames.Count)
                        {
                            assignedBlueprintLayerNames.Add(_layer.layerName);
                        }
                        else
                        {
                            assignedBlueprintLayerNames[_index] = _layer.layerName;
                        }
                    }


                    onSelected?.Invoke();
                    RefreshScrollView();
                });

                _scrollView.Add(_item);
            }
        }

    }
}
#endif