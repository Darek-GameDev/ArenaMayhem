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
    [Modifier(ModifierAttribute.Category.Generators, "Dot Grid", "")]
    public class DotGrid : BlueprintModifier
    {
        public int gridSpace;

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            if (gridSpace == 0)
            {
                return _positions;
            }
            _positions.Clear();
            var _rowCount = 0;
            var _columnCount = 0;
            for (int x = 0; x < asset.width; x ++)
            {
                if (_rowCount % gridSpace == 0)
                {
                    for (int y = 0; y < asset.height; y ++)
                    {
                        if (_columnCount % gridSpace == 0)
                        {
                            _positions.Add(new Vector2(x, y));
                        }

                        _columnCount ++;
                    }
                }
                _rowCount ++;
            }

            return _positions;
        }
    }
}