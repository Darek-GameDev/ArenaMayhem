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
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif
using UnityEngine;
using UnityEngine.UIElements;
using GiantGrey.TileWorldCreator.Attributes;

namespace GiantGrey.TileWorldCreator
{
    [Modifier(ModifierAttribute.Category.Modifiers, "Select", "")]
    public class Select : BlueprintModifier
    {
        public enum SelectionType
        {
            Random,
            BorderTiles,
            FillTiles,
            EdgeTiles,
            CornerTiles,
            InteriorCornerTiles,
            Count,
        }

        [HideInInspector]
        public SelectionType selectionType;

        [Range(0f, 1f)]
        [HideInInspector]
        public float randomSelectionWeight;
      
        [HideInInspector]
        public int count;

        BlueprintLayer layer;

        List<int> cornerTiles = new List<int> { 22, 28, 7, 13}; // direction configurations using four directions
        List<int> edgeTiles = new List<int> { 14, 15, 21, 23, 29, 30,  }; // direction configurations using four directions
        List<int> interiorCornerTiles = new List<int> { 507, 447, 443, 510, 255,  }; // direction configurations using eight directions

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            layer = _layer;

            switch (selectionType)
            {
                case SelectionType.BorderTiles:
                    return SelectBorder(_positions);
                case SelectionType.FillTiles:
                    return SelectFill(_positions);
                case SelectionType.CornerTiles:
                    return SelectCorner(_positions);
                case SelectionType.EdgeTiles:
                    return SelectEdge(_positions);
                case SelectionType.InteriorCornerTiles:
                    return SelectInteriorCorner(_positions);
                case SelectionType.Random:
                    return SelectRandom(_positions);
                case SelectionType.Count:
                    return SelectCount(_positions);
                default:
                    return _positions;
            }
        }



        HashSet<Vector2> SelectBorder(HashSet<Vector2> _positions)
        {
            var _innerPositions = GetInnerTiles(_positions);

            var _borderPositions = _positions.Except(_innerPositions).ToList();
            HashSet<Vector2> _borderPositionsSet = new HashSet<Vector2>(_borderPositions);
           
            return _borderPositionsSet;
        }

        HashSet<Vector2> SelectFill(HashSet<Vector2> _positions)
        {
            return GetInnerTiles(_positions);
        }
    
        HashSet<Vector2> SelectEdge(HashSet<Vector2> _positions)
        {
            HashSet<Vector2> _edgePositions = new HashSet<Vector2>();
            // for (int i = 0; i < _positions.Count; i ++)
            foreach (var _pos in _positions)
            {
                var _config = layer.GetConfigurationCodeFromPositionsInFourDirections(_pos, _positions);
                if (edgeTiles.Contains(_config))
                {
                    _edgePositions.Add(_pos);
                }
            }

            return _edgePositions;
        }

        HashSet<Vector2> SelectCorner(HashSet<Vector2> _positions)
        {
            HashSet<Vector2> _cornerPositions = new HashSet<Vector2>();
            // for (int i = 0; i < _positions.Count; i ++)
            foreach (var _pos in _positions)
            {
                var _config = layer.GetConfigurationCodeFromPositionsInFourDirections(_pos, _positions);
                if (cornerTiles.Contains(_config))
                {
                    _cornerPositions.Add(_pos);
                }
            }

            return _cornerPositions;
        }

        HashSet<Vector2> SelectInteriorCorner(HashSet<Vector2> _positions)
        {
            HashSet<Vector2> _interiorCornerPositions = new HashSet<Vector2>();
            // for (int i = 0; i < _positions.Count; i ++)
            foreach (var _pos in _positions)
            {
                var _config = layer.GetConfigurationCodeFromPositionsInEightDirections(_pos, _positions);
                if (interiorCornerTiles.Contains(_config))
                {
                    _interiorCornerPositions.Add(_pos);
                }
            }

            return _interiorCornerPositions;
        }

        HashSet<Vector2> SelectRandom(HashSet<Vector2> _positions)
        {
            HashSet<Vector2> _randomPositions = new HashSet<Vector2>();
            
            foreach (var _pos in _positions)
            {   
                if (layer.random.NextFloat(0f, 1f) < randomSelectionWeight)
                {
                    _randomPositions.Add(_pos);
                }
            }

            return _randomPositions;
        }

        HashSet<Vector2> GetInnerTiles(HashSet<Vector2> _positions)
        {
            HashSet<Vector2> newPositions = new HashSet<Vector2>();   
            // for (int j = 0; j < _positions.Count; j ++)
            foreach (var _pos in _positions)
            {
                var _config = layer.GetConfigurationCodeFromPositionsInEightDirections(_pos, _positions);

                if (_config == 511)
                {
                    newPositions.Add(_pos);
                }
            }

            return newPositions;
        }

        HashSet<Vector2> SelectCount(HashSet<Vector2> _positions)
        {
            HashSet<Vector2> _numberPositions = new HashSet<Vector2>();
            if (_positions.Count > 0)
            {
                if (_positions.Count == count)
                {
                    foreach(var _pos in _positions)
                    {
                        _numberPositions.Add(_pos);
                    }
                }
                else
                {
                    if (_positions.Count < count)
                    {
                        foreach(var _pos in _positions)
                        {
                            _numberPositions.Add(_pos);
                        }
                    }
                    else
                    {
                        
                        _numberPositions = GetRandomUniqueElementsInPlace(_positions, count);

                    }
                }
            }

            return _numberPositions;
        }

        public HashSet<T> GetRandomUniqueElementsInPlace<T>(HashSet<T> set, int count)
        {
            
            HashSet<T> result = new HashSet<T>(count);

            while (result.Count < count)
            {
                T item = set.ElementAt(layer.random.NextInt(set.Count));
                if (set.Remove(item))
                    result.Add(item);
            }

            return result;
        }

#if UNITY_EDITOR
        public override VisualElement BuildInspector(Configuration _asset)
        {
            var _root = new VisualElement();

            var _so = new SerializedObject(this);

            var _random = new PropertyField();
            _random.BindProperty(_so.FindProperty(nameof(randomSelectionWeight)));
            _random.style.display = selectionType == SelectionType.Random ? DisplayStyle.Flex : DisplayStyle.None;

            var _count = new PropertyField();
            _count.BindProperty(_so.FindProperty(nameof(count)));
            _count.style.display = selectionType == SelectionType.Count ? DisplayStyle.Flex : DisplayStyle.None;

            var _dropdown = new DropdownField();
            _dropdown.BindProperty(_so.FindProperty(nameof(selectionType)));
            _dropdown.RegisterValueChangedCallback(x => 
            {
                if (selectionType == SelectionType.Random)
                {
                    _random.style.display = DisplayStyle.Flex;
                } 
                else
                {
                    _random.style.display = DisplayStyle.None;  
                }
                if (selectionType == SelectionType.Count)
                {
                    _count.style.display = DisplayStyle.Flex;
                } 
                else
                {
                    _count.style.display = DisplayStyle.None;  
                }
            });

            _root.Add(_dropdown);
            _root.Add(_random);
            _root.Add(_count);

            return _root;
        }
    #endif
    }

}