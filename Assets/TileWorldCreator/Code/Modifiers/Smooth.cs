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

namespace GiantGrey.TileWorldCreator
{
    [Modifier(ModifierAttribute.Category.Modifiers, "Smooth", "")]
    public class Smooth : BlueprintModifier
    {
        public int smoothCount; 
        private int neighbourCount = 4;
        private List<Vector2Int> cells;


        public override VisualElement BuildInspector(Configuration _asset)
        {
            return base.BuildInspector(_asset);
        }

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            cells = new List<Vector2Int>();
            neighbourCount = 4;

            var _map = GetMapArray(_positions);


            for (int i = 0; i < smoothCount; i ++)
            {
                for (int x = 0; x < _map.GetLength(0); x ++)
                {
                    for (int y = 0; y < _map.GetLength(1); y ++)
                    {
                        if (_map[x,y])
                        {
                            var _neighbours = CountNeighbours(x, y, _map);
                            
                            if (_neighbours <= neighbourCount + i)
                            {
                                cells.Add(new Vector2Int(x, y));
                            }
                        }
                    }
                }

                for (int c = 0; c < cells.Count; c++)
                {
                    _positions.Remove(new Vector2(cells[c].x, cells[c].y));
                }
            }

            return _positions;
        }

        int CountNeighbours(int x, int y, bool[,] _map)
        {
            int _neighbours = 0;
            for (int i = -1; i < 2; i ++)
            {
                for (int j = -1; j < 2; j ++)
                {

                    try{
                        if (_map[x+i,y+j])
                        {
                            _neighbours++;
                        }
                    }
                    catch{}
                    
                }
            }

            return _neighbours;
        }
    }
}