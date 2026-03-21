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
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif
using UnityEngine;
using UnityEngine.UIElements;

using GiantGrey.TileWorldCreator.UI;
using GiantGrey.TileWorldCreator.Attributes;

namespace GiantGrey.TileWorldCreator
{
    [Modifier(ModifierAttribute.Category.Modifiers, "Boolean", "")]
    public class Boolean : BlueprintModifier
    {
        [HideInInspector] [SerializeField]
        private string selectedLayerGuid;
        [HideInInspector] [SerializeField]
        private string selectedLayerName;

        public enum Operators
        {
            /// <summary>
            /// Adds selected layer
            /// </summary>
            add,
            // Subtracts selected layer from this layer
            subtract,
            // Returns only intersecting tiles (overlapping)
            intersect,
            // Returns only non-intersecting tiles. (inverted intersect)
            xor,
        }

        [HideInInspector] [SerializeField]
        private Operators operators;


        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            if (string.IsNullOrEmpty(selectedLayerGuid))
                return null;

            var _selectedLayer = asset.GetBlueprintLayerByGuid(selectedLayerGuid);

            HashSet<Vector2> _selectedLayerPositions = new HashSet<Vector2>();
            _selectedLayerPositions = _selectedLayer.GetAllCellPositions(_selectedLayerPositions);

            switch (operators)
            {
                case Operators.add:
                _positions = Add(_selectedLayerPositions, _positions);
                break;
                case Operators.subtract:
                _positions = Subtract(_selectedLayerPositions, _positions);
                break;
                case Operators.intersect:
                _positions = Intersect(_selectedLayerPositions, _positions);
                break;
                case Operators.xor:
                _positions = XOR(_selectedLayerPositions, _positions);
                break;
            }


            return _positions;
        }

        HashSet<Vector2> Add(HashSet<Vector2> _selectedLayerPositions, HashSet<Vector2> _positions)
        {
            foreach(var _pos in _selectedLayerPositions)
            {
                _positions.Add(_pos);
            }

            return _positions;
        }

        HashSet<Vector2> Subtract(HashSet<Vector2> _selectedLayerPositions, HashSet<Vector2> _positions)
        {
            foreach (var _pos in _selectedLayerPositions)
            {
                _positions.Remove(_pos);
            }

            return _positions;
        }

        HashSet<Vector2> Intersect(HashSet<Vector2> _selectedLayerPositions, HashSet<Vector2> _positions)
        {
            HashSet<Vector2> intersection = new HashSet<Vector2>(_selectedLayerPositions);
            intersection.IntersectWith(_positions);

            return intersection;
        }

        HashSet<Vector2> XOR (HashSet<Vector2> _selectedLayerPositions, HashSet<Vector2> _positions)
        {
            HashSet<Vector2> nonOverlapping = new HashSet<Vector2>(_selectedLayerPositions);
            nonOverlapping.SymmetricExceptWith(_positions);

            return nonOverlapping;
        }

        #region Inspector
        #if UNITY_EDITOR
        void SelectedLayer(string _layerName, string _layerGuid)
        {
            selectedLayerName = _layerName;
            selectedLayerGuid = _layerGuid;
        }

        public override VisualElement BuildInspector(Configuration _asset)
        {
            var _root = new VisualElement();

            var _so = new SerializedObject(this);

            var _dropdown = new LayerSelectDropdownElement(_asset, selectedLayerGuid, SelectedLayer); 
            _root.Add(_dropdown);

            var _infoBox = new HelpBox();
            _infoBox.text = GetInfoText();
            _infoBox.messageType = HelpBoxMessageType.Info;
            _root.Add(_infoBox);

            var _operators = new DropdownField();
            _operators.BindProperty(_so.FindProperty(nameof(operators)));
            _operators.RegisterValueChangedCallback(x => 
            {
                _infoBox.text = GetInfoText();
            });

            _root.Add(_operators);

            return _root;
        }

        string GetInfoText()
        {
            switch(operators)
            {
                case Operators.add:
                return "Adds selected layer";
                case Operators.subtract:
                return "Subtracts selected layer from this layer";
                case Operators.intersect:
                return "Returns only intersecting tiles (overlapping)";
                case Operators.xor:
                return "Returns only non-intersecting tiles. (inverted intersect)";
                default:
                return "Unknown operator";
            }
        }
        #endif
        #endregion
    }
}