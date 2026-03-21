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

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace GiantGrey.TileWorldCreator.UI
{
    public class LayerSelectDropdownElement : VisualElement
    {
        List<string>  _layerNames = new List<string>();
        List<string> _layerGuids = new List<string>();

        List<bool> _selected = new List<bool>();

        Action<string, string> onSelectedCallback;
        string label;

        /// <summary>
        /// _onSelectCallback returns selected layerName and layerGuid
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="selectedLayerGuid"></param>
        /// <param name="_onSelectedCallback"></param>
        public LayerSelectDropdownElement(Configuration asset, string selectedLayerGuid, Action<string,string> _onSelectedCallback, string _label = "")
        {
            label = _label;
            Build(asset, selectedLayerGuid, _onSelectedCallback, null, 0);
        }

        public LayerSelectDropdownElement(Configuration asset, string selectedLayerGuid, Action<string, string, int> _onSelectedCallback, int _index, string _label = "")
        {
            label = _label;
            Build(asset, selectedLayerGuid, null, _onSelectedCallback, _index);
        }

        void Build(Configuration asset, string selectedLayerGuid, Action<string,string> _onSelectedCallback, Action<string,string,int> _onSelectedCallbackWithIndex = null, int _index = 0)
        {
            #if UNITY_EDITOR
            this.Clear();
            GetChoices(asset);
            

            var _defaultIndex = 0;
            if (!string.IsNullOrEmpty(selectedLayerGuid))
            {
                // find index of layerGuid
                for (int i = 0; i < _layerGuids.Count; i ++)
                {
                    if (_layerGuids[i] == selectedLayerGuid)
                    {
                        _defaultIndex = i;
                    }
                }
            }

            var _layerDropdown = new DropdownField(_layerNames, _defaultIndex);
            _layerDropdown.style.flexGrow = 1;
            if (!string.IsNullOrEmpty(label))
            {
                _layerDropdown.label = label;
            }
            else
            {
                _layerDropdown.label = "Selected Layer";
            }
            _layerDropdown.RegisterCallback<MouseDownEvent>(evt => 
            {
                // UnityEngine.Debug.Log("check here?");
                var _oldNameCount = _layerNames.Count;
                GetChoices(asset);
                if (_layerNames.Count != _oldNameCount)
                {
                    Build(asset, selectedLayerGuid, _onSelectedCallback, _onSelectedCallbackWithIndex);
                }
                var _found = false;
                for (int l = 0; l < _layerNames.Count; l ++)
                {
                    _found = false;
                    for (int i = 0; i < asset.blueprintLayerFolders.Count; i ++)
                    {
                        for (int j = 0; j < asset.blueprintLayerFolders[i].blueprintLayers.Count; j ++)
                        {
                            if (asset.blueprintLayerFolders[i].blueprintLayers[j].layerName == _layerNames[l])
                            {
                                _found = true;
                            }
                        }
                    }
                    if (!_found)
                    {
                        break;
                    }
                }
                
                if (!_found)
                {
                    Build(asset, selectedLayerGuid, _onSelectedCallback, _onSelectedCallbackWithIndex);
                }
                
            });
            _layerDropdown.RegisterCallback<GeometryChangedEvent>(evt => 
            {
                
                _layerDropdown.choices = _layerNames;
                if (_layerDropdown.index > -1 && _layerDropdown.index < _layerNames.Count && _layerDropdown.index < _layerGuids.Count)
                {
                    _onSelectedCallback?.Invoke(_layerNames[_layerDropdown.index], _layerGuids[_layerDropdown.index]);
                    _onSelectedCallbackWithIndex?.Invoke(_layerNames[_layerDropdown.index], _layerGuids[_layerDropdown.index], _index);
                }
            });

            _layerDropdown.RegisterValueChangedCallback(evt => 
            { 
                _layerDropdown.choices = _layerNames;
                if (_layerDropdown.index > -1 && _layerDropdown.index < _layerNames.Count && _layerDropdown.index < _layerGuids.Count)
                {
                    _onSelectedCallback?.Invoke(_layerNames[_layerDropdown.index], _layerGuids[_layerDropdown.index]);
                    _onSelectedCallbackWithIndex?.Invoke(_layerNames[_layerDropdown.index], _layerGuids[_layerDropdown.index], _index);
                }
            });

            this.style.flexDirection = FlexDirection.Row;
            this.style.flexGrow = 1;

            // var _lbl = new Label("Selected Layer");
            // this.Add(_lbl);
            this.Add(_layerDropdown);
            #endif
        }


        void GetChoices(Configuration asset)
        {
            _layerNames = new List<string>();
            _layerGuids = new List<string>();

            // Get all available layers and cache layer name and layer guid
            for (int i = 0; i < asset.blueprintLayerFolders.Count; i ++)
            {
                for (int j = 0; j < asset.blueprintLayerFolders[i].blueprintLayers.Count; j ++)
                {
                    var _name = asset.blueprintLayerFolders[i].blueprintLayers[j].layerName;
                    // if (string.IsNullOrEmpty(_name))
                    // {
                    //     _name = "No Name";
                    // }
                    _layerNames.Add(_name);
                    _layerGuids.Add(asset.blueprintLayerFolders[i].blueprintLayers[j].guid);
                }
            }
        }
    }
}