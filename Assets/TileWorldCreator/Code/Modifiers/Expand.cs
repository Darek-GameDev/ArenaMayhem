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
using UnityEngine;
using GiantGrey.TileWorldCreator.Attributes;

namespace GiantGrey.TileWorldCreator
{
    [Modifier(ModifierAttribute.Category.Modifiers, "Expand", "")]
    public class Expand : BlueprintModifier
    {
        BlueprintLayer layer;

        public enum ExpandDirection
        {
            FourDirections,
            EightDirections
        }

        public ExpandDirection expandDirection = ExpandDirection.FourDirections;
        public int iterations = 1;

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            layer = _layer;

             // Expand around border
            var _expandedPositions = new HashSet<Vector2>();

            for (int i = 0; i < iterations; i++)
            {
                var _innerPositions = GetInnerTiles(_positions);
                var _borderPositions = _positions.Except(_innerPositions).ToList();

            
                foreach (var _pos in _borderPositions)
                {
                    _expandedPositions.Add(_pos);
                }

                
                for (int b = 0; b < _borderPositions.Count; b ++)
                {
                    for (int x = -1; x < 2; x ++)
                    {
                        for (int y = -1; y < 2; y ++)
                        {
                            if (expandDirection == ExpandDirection.FourDirections)
                            {
                                if (x == -1 && y == -1)continue;
                                if (x == 1 && y == -1)continue;
                                if (x == -1 && y == 1)continue;
                                if (x == 1 && y == 1)continue;
                            }

                            var _p = new Vector2(_borderPositions[b].x + x, _borderPositions[b].y + y);
                            if (_p.x >= 0 && _p.x < asset.width && _p.y >= 0 && _p.y < asset.height)
                            {
                                _expandedPositions.Add(_p);
                            }
                        }
                    }
                }
                
                foreach (var _pos in _innerPositions)
                {
                    _expandedPositions.Add(_pos);
                }

                foreach (var _pos in _expandedPositions)
                {
                    _positions.Add(_pos);
                }
            }

            return _expandedPositions;
        }


        HashSet<Vector2> GetInnerTiles(HashSet<Vector2> _positions)
        {
            HashSet<Vector2> newPositions = new HashSet<Vector2>();   
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

    }
}