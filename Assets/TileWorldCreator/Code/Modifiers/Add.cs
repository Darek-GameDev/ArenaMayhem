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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

using GiantGrey.TileWorldCreator.Attributes;
using GiantGrey.TileWorldCreator.UI;

namespace GiantGrey.TileWorldCreator
{
    [Modifier(ModifierAttribute.Category.Modifiers, "Add", "")]
    public class Add : BlueprintModifier
    {
        [SerializeField]
        [HideInInspector]
        // OBSOLETE
        private string layerName;
        [SerializeField]
        [HideInInspector]
        // OBSOLETE
        private string layerGuid;

        [SerializeField, HideInInspector]
        private List<string> assignedBlueprintLayerGuids = new List<string>();
        [SerializeField, HideInInspector]
        private List<string> assignedBlueprintLayerNames = new List<string>();


        public void SetLayer(BlueprintLayer layer)
        {
            layerName = layer.layerName;
            layerGuid = layer.guid;

            if (!assignedBlueprintLayerGuids.Contains(layer.guid))
            {
                assignedBlueprintLayerGuids.Add(layer.guid);
                assignedBlueprintLayerNames.Add(layer.layerName);
            }
        }

        public void UnsetLayer(BlueprintLayer layer)
        {
            layerName = "";
            layerGuid = "";

            if (assignedBlueprintLayerGuids.Contains(layer.guid))
            {
                assignedBlueprintLayerGuids.Remove(layer.guid);
                assignedBlueprintLayerNames.Remove(layer.layerName);
            }
        }

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {

            if (asset == null)
            {
                asset = _layer.GetAsset();
            }

            for (int i = 0; i < assignedBlueprintLayerGuids.Count; i++)
            {

                var _assignedLayer = asset.GetBlueprintLayerByGuid(assignedBlueprintLayerGuids[i]);

                HashSet<Vector2> _layerPositions = new HashSet<Vector2>();
                _layerPositions = _assignedLayer.GetAllCellPositions(_layerPositions);


                foreach (var _pos in _layerPositions)
                {
                    _positions.Add(_pos);
                }
            }

            return _positions;

        }
        
        
#if UNITY_EDITOR
        public override VisualElement BuildInspector(Configuration _asset)
        {
            var _root = new VisualElement();


            // first time after update
            if (!string.IsNullOrEmpty(layerGuid))
            {
                assignedBlueprintLayerGuids.Add(layerGuid);
                assignedBlueprintLayerNames.Add(layerName);

                layerGuid = "";
                layerName = "";
            }

            var _selectBlueprint = new DropdownField("Blueprint Layers");
            _selectBlueprint.value = string.Join(", ", assignedBlueprintLayerNames);
            _selectBlueprint.RegisterCallback<ClickEvent>((x) =>
            {
                BlueprintLayerPopupSelector.ShowPanel(Event.current.mousePosition, _asset, ref assignedBlueprintLayerGuids, ref assignedBlueprintLayerNames, new BlueprintLayerPopupSelector(), () => _selectBlueprint.value = string.Join(", ", assignedBlueprintLayerNames));
            });

            _root.Add(_selectBlueprint);

            return _root;
        }
#endif
    }
}