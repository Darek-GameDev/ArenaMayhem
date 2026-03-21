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
using GiantGrey.TileWorldCreator.Attributes;

namespace GiantGrey.TileWorldCreator
{
    [Modifier(ModifierAttribute.Category.Modifiers, "Invert", "")]
    public class Invert : BlueprintModifier
    {
        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            var _map = GetMapArray(_positions);

            HashSet<Vector2> _invertedPositions = new HashSet<Vector2>();
            for (int x = 0; x < _map.GetLength(0); x ++)
            {
                for (int y = 0; y < _map.GetLength(1); y ++)
                {
                    _map[x, y] = !_map[x, y];
                }
            }

            for (int x = 0; x < _map.GetLength(0); x ++)
            {
                for (int y = 0; y < _map.GetLength(1); y ++)
                {
                    if (_map[x, y])
                    {
                        _invertedPositions.Add(new Vector2(x, y));
                    }
                }
            }

            return _invertedPositions;
        }
    }
}