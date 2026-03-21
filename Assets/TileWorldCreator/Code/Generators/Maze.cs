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
	[Modifier(ModifierAttribute.Category.Generators, "Maze", "")]
    public class Maze : BlueprintModifier
    {
        public int corridorWidth = 1;
		
		public bool onlyOutputPlayerStartPos;
		public bool onlyOutputPlayerEndPos;
    
        
		Vector2Int startPosition;
		Vector2Int endPosition;

        int width;
        int height;
        bool[,] mazeMap;		
        float lastDistance = 0;
		float dist = 0;

		BlueprintLayer blueprintLayer;

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
			blueprintLayer = _layer;

            var _map = GetMapArray(_positions);

            width = _map.GetLength(0);
            height = _map.GetLength(1);

            mazeMap = new bool[width, height];
			mazeMap = GenerateMaze(height, width);


            // Find end position after generation
			var _pos = FindEndPosition(mazeMap);
			endPosition = new Vector2Int(_pos.x, _pos.y);    
	
			if (onlyOutputPlayerStartPos || onlyOutputPlayerEndPos)
			{
				mazeMap = new bool[width, height];	
			}
			
			if (onlyOutputPlayerStartPos)
			{
				mazeMap[startPosition.x, startPosition.y] = true;
			}
	
			if (onlyOutputPlayerEndPos)
			{
				mazeMap[endPosition.x, endPosition.y] = true;
			}
			


            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (mazeMap[i, j])
                    {
                        _positions.Add(new Vector2(i, j));
                    }
                }
            }

            return _positions;
        }

        bool[,] GenerateMaze(int _height, int _width)
		{
			// create temp maze
			bool[,] _tmpMaze = new bool[_width, _height];
	
			// Fill all cells
			for (int x = 0; x < _width; x++)
			{
				for (int y = 0; y < _height; y++)
				{
					_tmpMaze[x, y] = false;
				}
			}
	
	      
			int _x = 1;
			int _y = 0;
	 
			
			_x = blueprintLayer.random.NextInt(0, width - 1); 
			_y = blueprintLayer.random.NextInt(0, height -1); 			
	
			// Clear start cell
			_tmpMaze[_x, _y] = true;
	
			//create maze using DFS (Depth First Search)
			DepthFirstSearch(_tmpMaze, _x, _y);
	
	
			//return maze
			return _tmpMaze;
		}

        void DepthFirstSearch(bool[,] _maze, int r, int c)
		{
	       
			//Directions
			// 1 - West
			// 2 - North
			// 3 - East
			// 4 - South
			int[] _directions = new int[] { 1, 2, 3, 4 };
	
			//if (!linear)
			//{
				Shuffle(_directions);
			//}


			var _base = 2 + corridorWidth;

	
			// Look in a random direction 3 block ahead
			for (int i = 0; i < _directions.Length; i ++)
			{
				switch(_directions[i])
				{
				case 1: // West
					// Check whether (number of _base) cells to the left is out of maze
					if (r - _base <= 1)
						continue;
	
					if (_maze[r - _base, c ] != true)
					{  
						for (int b = 1; b <= _base; b ++)
						{
							_maze[r - b, c] = true;
						}
						// _maze[r - 4, c] = true;
						// _maze[r - 3, c] = true;
						// _maze[r - 2, c] = true;
						// _maze[r - 1, c] = true;
	
						DepthFirstSearch(_maze, r - _base, c);
					}
					break;
				case 2: // North
					// Check whether 3 cells up is out of maze
					if (c + _base >= height - 1)
						continue;
	
					if (_maze[r, c + _base] != true)
					{
						for (int b = 1; b <= _base; b ++)
						{
							_maze[r, c + b] = true;
						}
						// _maze[r, c + 4] = true;
						// _maze[r, c + 3] = true;
						// _maze[r, c + 2] = true;
						// _maze[r, c + 1] = true;

						DepthFirstSearch(_maze, r, c + _base);
					}
	
					break;
				case 3: // East
					// Check whether 3 cells to the right is out of maze
					if (r + _base >= width - 1)
						continue;
	
					if (_maze[r + _base, c] != true)
					{
						for (int b = 1; b <= _base; b ++)
						{
							_maze[r + b, c] = true;
						}
						// _maze[r + 4, c] = true;
						// _maze[r + 3, c] = true;
						// _maze[r + 2, c] = true;
						// _maze[r + 1, c] = true;
	
						DepthFirstSearch(_maze, r + _base, c);
					}
					break;
				case 4: // South
					// Check whether 3 cells down is out of maze
					if (c - _base <= 1)
						continue;
	
					if (_maze[r, c - _base] != true)
					{
						for (int b = 1; b <= _base; b ++)
						{
							_maze[r, c - b] = true;
						}
						// _maze[r, c - 4] = true;
						// _maze[r, c - 3] = true;
						// _maze[r, c - 2] = true;
						// _maze[r, c - 1] = true;
	
						DepthFirstSearch(_maze, r, c - _base);
					}
					break;
				}
			}
		}
	
		void Shuffle<T>(T[] _array)
		{
			for (int i = _array.Length; i > 1; i --)
			{
				// Pick random element to swap
				int j = blueprintLayer.random.NextInt(0, i);
				// Swap
				T _tmp = _array[j];
				_array[j] = _array[i - 1];
				_array[i - 1] = _tmp;
			}
		}

        Vector2Int FindEndPosition(bool[,] _mazeMap)
		{
			var _pos = Vector2Int.zero;
	
			lastDistance = 0;

			var _width = _mazeMap.GetLength(0);
			var _height = _mazeMap.GetLength(1);

			for (int x = 0; x < _width; x ++)
			{
				for (int y = 0; y < _height; y ++)
				{
					if (_mazeMap[x, y])
					{
						
						// we have found an end position
						// check the distance from this position to start position
						dist = Vector2Int.Distance(startPosition, new Vector2Int(x, y));
						if (dist > lastDistance)
						{
							_pos = new Vector2Int(x, y);
							lastDistance = dist;
						}
						
					}
				}
			}
	
			return _pos;
		}
    }
}