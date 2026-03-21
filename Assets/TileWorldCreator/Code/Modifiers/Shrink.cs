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
    [Modifier(ModifierAttribute.Category.Modifiers, "Shrink", "")]
    public class Shrink : BlueprintModifier
    {
        public int shrinkCount;

        private BlueprintLayer layer;

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            layer = _layer;

            var _newPositions = _positions;
            for (int i = 0; i < shrinkCount; i ++)
            {
                if (_newPositions.Count == 0) break;
                
                _newPositions = DoShrink(_newPositions);
                
            }

            return _newPositions;
        }

        HashSet<Vector2> DoShrink(HashSet<Vector2> _positions)
        {
            HashSet<Vector2> newPositions = new HashSet<Vector2>();   

            foreach (var _pos in _positions)
            {
                var _config =layer.GetConfigurationCodeFromPositionsInEightDirections(_pos, _positions);

                if (_config == 511)
                {
                    newPositions.Add(_pos);
                }
            }

            return newPositions;
        }


        List<Vector2> GetNeighbourConfigurations(bool[,] _map)
        {
            List<Vector2> newPositions = new List<Vector2>();   
            for (int x = 0; x < _map.GetLength(0); x ++)
            {
                for (int y = 0; y < _map.GetLength(1); y ++)
                {
                    if (_map[x,y])
                    {
                        var _neighbours = 0;
                        var _configuration = 0;

                        GetNeighbours(x, y, _map, out _neighbours, out _configuration);
                        if (_configuration == 15)
                        {
                            newPositions.Add(new Vector2(x, y));
                        }
                    }
                }
            }

            return newPositions;
        }

        void GetNeighbours(int x, int y, bool[,] _map, out int _neighbours, out int _configuration)
        {
            var _north = false;
            var _south = false;
            var _west = false;
            var _east = false;

            _neighbours = 0;
            _configuration = 0;

            for (int i = -1; i < 2; i ++)
            {
                for (int j = -1; j < 2; j ++)
                {
                
                    try{
                        if (_map[x+i,y+j])
                        {
                            _neighbours ++;
                            if (i == -1 && j == 0)
                            {
                                _west = true;
                            }
                            if (i == 1 && j == 0)
                            {
                                _east = true;
                            }
                            if (i == 0 && j == -1)
                            {
                                _south = true;
                            }
                            if (i == 0 && j == 1)
                            {
                                _north = true;
                            }
                        }
                    }
                    catch{}
                    
                }
            }

            bool[] _directions = new bool[]{_north, _south, _west, _east};
            _configuration = GetConfigurationBitmask(_directions);

        }

        int GetConfigurationBitmask(bool[] connectionGrid)
        {
            int config = 0;
            for (int i = 0; i < connectionGrid.Length; i++)
            {
                if (connectionGrid[i])
                {
                    config += 1 << i;
                }
            }
            return config;
        }
    }
}