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
    [Modifier(ModifierAttribute.Category.Modifiers, "Select based on neighbours", "")]
    public class SelectBasedOnNeighbour : BlueprintModifier
    {
        public int neighbourCount;
        public string selectedLayer;
        public bool onlyFourDirectionsCheck;
        public bool invert;
        string selectedLayerGuid;

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            if (string.IsNullOrEmpty(selectedLayerGuid))
            {
                selectedLayerGuid = asset.GetBlueprintLayerGuid(selectedLayer);
            }

            var _otherLayer = asset.GetBlueprintLayerByGuid(selectedLayerGuid);
            HashSet<Vector2> _otherLayerPositions = new HashSet<Vector2>();
            _otherLayerPositions = _otherLayer.GetAllCellPositions(_otherLayerPositions);

            HashSet<Vector2> _selectedPositions = new HashSet<Vector2>();
        
            foreach (var _pos in _positions)
            {
                var _count = GetNeighborCount(_pos, _otherLayerPositions);
                if (invert)
                {
                    if (_count <= neighbourCount)
                    {
                        _selectedPositions.Add(_pos);
                    }
                }else
                {
                    if (_count >= neighbourCount)
                    {
                        _selectedPositions.Add(_pos);
                    }
                }
            }


            return _selectedPositions;
        }


        int GetNeighborCount(Vector2 _pos, HashSet<Vector2> _positions)
        {
            int count = 0;
            int x = (int)_pos.x;
            int y = (int)_pos.y;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && nx < asset.width && ny >= 0 && ny < asset.height)
                    {
                        // Only four directions
                        if (onlyFourDirectionsCheck)
                        {
                            if (dx == -1 && dy == 0 || dx == 1 && dy == 0 || dx == 0 && dy == -1 || dx == 0 && dy == 1)
                            {
                                if (invert)
                                {
                                    if (!_positions.Contains(new Vector2(nx, ny)))
                                    {
                                        count ++;
                                    }
                                }
                                else
                                {
                                    if (_positions.Contains(new Vector2(nx, ny)))
                                    {
                                        count++;
                                    }
                                }
                            }
                        }
                        else
                        {
                            if (invert)
                            {
                                if (!_positions.Contains(new Vector2(nx, ny)))
                                {
                                    count ++;
                                }
                            }
                            else
                            {
                                if (_positions.Contains(new Vector2(nx, ny)))
                                {
                                    count++;
                                }
                            }
                        }                    
                    }
                }
            }
            return count;
        }
    }
}