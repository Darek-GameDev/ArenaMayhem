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
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GiantGrey.TileWorldCreator.UI
{
    public class TilePresetPopupSelector : PopupWindowContent
    {

        public static Vector2 position;
        public static SerializedProperty property;
        public static Action<TilePreset> onSelected;

        ScrollView _scrollView;
        String _searchString = "";

        public TilePresetPopupSelector() { }

        public class Filter
        {
            public string filterName;
            public List<string> packages;
        }

        public static void ShowPanel(Vector2 _pos, SerializedProperty _property, TilePresetPopupSelector _panel, Action<TilePreset> _onSelected)
        {
            position = _pos;
            property = _property;
            onSelected = _onSelected;

            UnityEditor.PopupWindow.Show(new Rect(_pos.x, _pos.y, 0, 0), _panel);
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 350);
        }

        public override void OnGUI(Rect rect) { }

        public override void OnOpen()
        {
            // Find all tile preset assets in the project and display them in the popup window
            var _tilePresets = FindAssetsByType<TilePreset>();
            
            // Sort list by name
            _tilePresets.Sort((a, b) => a.name.CompareTo(b.name));

            _scrollView = new ScrollView();
            _scrollView.style.flexGrow = 1;

            var root = editorWindow.rootVisualElement;

            var _searchContainer = new VisualElement();
            // _searchContainer.style.flexGrow = 1;
            // _searchContainer.style.flexDirection = FlexDirection.Row;
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
                RefreshScrollView(_tilePresets);
            });

            _searchContainerHorizontal.Add(_searchField);
            _searchContainerHorizontal.Add(_cancelSearch);
            
            _searchField.RegisterValueChangedCallback((evt) => 
            {
                _searchString = evt.newValue;
                RefreshScrollView(_tilePresets);
            });

            // Search for filters
            var _filterContainer = new VisualElement();
            _filterContainer.style.flexWrap = Wrap.Wrap;
            _filterContainer.style.flexDirection = FlexDirection.Row;
            _filterContainer.SetMargin(0, 0, 5, 5);

            var _filters = FindFilters();
            foreach (var _filter in _filters)
            {
                var _filterTag = new VisualElement();
                _filterTag.style.borderTopLeftRadius = 5;
                _filterTag.style.borderTopRightRadius = 5;
                _filterTag.style.borderBottomLeftRadius = 5;
                _filterTag.style.borderBottomRightRadius = 5;
                _filterTag.style.backgroundColor = TileWorldCreatorColor.DarkGreen.GetColor();
                _filterTag.SetMargin(2, 2, 2, 2);
                _filterTag.SetPadding(2, 2, 2, 2);

                var _filterLabel = new Label(_filter.filterName);
                _filterLabel.style.color = Color.black;
                _filterTag.Add(_filterLabel);
                
                _filterContainer.Add(_filterTag);

                _filterTag.RegisterCallback<ClickEvent>(evt => 
                {
                    _searchString = "";

                    for (int i = 0; i < _filter.packages.Count; i++)
                    {
                        _searchString += _filter.packages[i] + ",";
                    }

                    // _searchString = _filter.filterName;
                    _searchField.value = _searchString; //_filter.filterName;
                    RefreshScrollView(_tilePresets);
                });
            }


            _searchContainer.Add(_searchLabel);
            _searchContainer.Add(_searchContainerHorizontal);

            root.Add(_searchContainer);
            root.Add(_filterContainer);
            root.Add(_scrollView);

            root.schedule.Execute(() => _searchField.Focus()).ExecuteLater(100);

            RefreshScrollView(_tilePresets);
        }

        void RefreshScrollView(List<TilePreset> _tilePresets)
        {
            _scrollView.Clear();

            foreach (var _tilePreset in _tilePresets)
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
                            if (_tilePreset.name.ToLower().Contains(_splitted[i].ToLower()))
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

                        if (!_tilePreset.name.ToLower().Contains(_searchString.ToLower()))
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
                _item.SetBorder(1);
                _item.SetPadding(2, 2, 2, 2);
                _item.SetMargin(2, 2, 4, 4);

                var _thumb = new VisualElement();
                _thumb.style.width = 50;
                _thumb.style.height = 50;
                var _bs = new BackgroundSize();
                _bs.sizeType = BackgroundSizeType.Cover;
                _thumb.style.backgroundSize = _bs;
                if (_tilePreset.previewThumbnail != null)
                {
                    _thumb.style.backgroundImage = _tilePreset.previewThumbnail;
                }
                var _button = new Button();
                _button.style.flexGrow = 1;
                _button.text = _tilePreset.name;
                _button.RegisterCallback<ClickEvent>(evt => 
                {
                    property.objectReferenceValue = _tilePreset;
                    property.serializedObject.ApplyModifiedProperties();
                    onSelected?.Invoke(_tilePreset);
                    editorWindow.Close();
                });

                _item.Add(_thumb);
                _item.Add(_button);

                _scrollView.Add(_item);
            }
        }

        public static List<T> FindAssetsByType<T>() where T : UnityEngine.Object
        {
            List<T> assets = new List<T>();

            string[] guids = AssetDatabase.FindAssets(string.Format("t:{0}", typeof (T).ToString().Replace("UnityEngine.", "")));
        
            for( int i = 0; i < guids.Length; i++ )
            {
                string assetPath = AssetDatabase.GUIDToAssetPath( guids[i]);

                T asset = AssetDatabase.LoadAssetAtPath<T>( assetPath );

                if (asset != null )
                {
                    assets.Add(asset);
                }

            }

            return assets;

        }

        public static List<Filter> FindFilters()
        {
            var _filters = new List<Filter>();

            string projectPath = Application.dataPath;
            string[] files = System.IO.Directory.GetFiles(projectPath, "*.twcf", System.IO.SearchOption.AllDirectories);

            foreach (var file in files)
            {
                var _filter = new Filter();
                _filter.packages = new List<string>();
                _filter.filterName = System.IO.Path.GetFileNameWithoutExtension(file.Replace("TWCFilter_", ""));

                var _text = File.ReadAllText(file);
                string[] _lines = _text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in _lines)
                {
                    _filter.packages.Add(line);
                }

                _filters.Add(_filter);
            }

            return _filters;
        }
    }
}
#endif