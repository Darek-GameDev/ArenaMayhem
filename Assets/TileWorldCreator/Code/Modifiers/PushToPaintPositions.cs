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

#if UNITY_EDITOR
using UnityEditor.UIElements;
using UnityEditor;
#endif

namespace GiantGrey.TileWorldCreator
{
    [Modifier(ModifierAttribute.Category.Modifiers, "Push to Paint Positions", "")]
    public class PushToPaintPositions : BlueprintModifier
    {
        [HideInInspector, SerializeField]
        public string layerName;
        [HideInInspector, SerializeField]
        public bool clearPreviousPaintedPositions;
        [HideInInspector, SerializeField]
        public string layerGuid;

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            if (string.IsNullOrEmpty(layerGuid))
            {
                layerGuid = asset.GetBlueprintLayerGuid(layerName);
            }

            var _otherLayer = asset.GetBlueprintLayerByGuid(layerGuid);
            if (clearPreviousPaintedPositions)
            {
                _otherLayer.ClearLayer(false);
            }

            _otherLayer.AddCells(_positions);

            return _positions;
        }

#if UNITY_EDITOR
        public override VisualElement BuildInspector(Configuration _asset)
        {
            var _root = new VisualElement();

            var _so = new SerializedObject(this);

            var _dropdown = new LayerSelectDropdownElement(_asset, layerGuid, SelectedLayer); 
            _root.Add(_dropdown);

            var _clearPreviousPos = new PropertyField();
            _clearPreviousPos.BindProperty(_so.FindProperty(nameof(clearPreviousPaintedPositions)));

            _root.Add(_clearPreviousPos);

            return _root;
        }
#endif
        void SelectedLayer(string _layerName, string _layerGuid)
        {
            layerName = _layerName;
            layerGuid = _layerGuid;
        }
    }
}