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
	[Modifier(ModifierAttribute.Category.Generators, "BSP Dungeon", "")]
    public class BSPDungeon : BlueprintModifier
    {
        #region LeafClass
        public class Leaf
		{
			
			public int x, y, width, height;
	
			public Leaf parentLeaf;
			public Leaf firstChild;
			public Leaf secondChild;

			public BlueprintLayer blueprintLayer;
	
			public Leaf() { }
	
			public Leaf (List<Leaf> _tree, List<Leaf> _parentTree, Leaf _parent, int _x, int _y, int _width, int _height)
			{
				x = _x;
				y = _y;
				width = _width;
				height = _height;
				parentLeaf = _parent;
	
				var _r = Split(_tree, _parentTree, this);
	
				if(!_r)
				{
					_parentTree.Add(_parent);
				}
			}
	
			// Split current leaf into two smaller leafs
			public bool Split(List<Leaf> _tree, List<Leaf> _parentTree, Leaf _leaf)
			{
				if (_splitH)
				{
					// Split horizontally
					int _newX =  (int)Random.Range((_leaf.width * 0.25f), _leaf.width - (_leaf.width * 0.25f));
	
					if (_leaf.width >= minWidth && _leaf.height >= minHeight)
					{
						_splitH = false;
						_leaf.firstChild = null;
						_leaf.secondChild = null;
						firstChild = new Leaf(_tree, _parentTree, _leaf, _newX + _leaf.x, _leaf.y, _leaf.width - _newX, _leaf.height);
						secondChild = new Leaf(_tree, _parentTree, _leaf, _leaf.x, _leaf.y, _leaf.width - firstChild.width, _leaf.height);
						return true;
					}
					else
					{
						_tree.Add(_leaf);
						return false;
					}
				}
				else
				{
					// Split vertically
					int _newY = (int)Random.Range((_leaf.height * 0.25f), _leaf.height - (_leaf.height * 0.25f));
	
					if (_leaf.width >= minWidth && _leaf.height >= minHeight)
					{
						_splitH = true;
						_leaf.firstChild = null;
						_leaf.secondChild = null;
						firstChild = new Leaf(_tree, _parentTree, _leaf, _leaf.x, _newY + _leaf.y, _leaf.width, _leaf.height - _newY);
						secondChild = new Leaf(_tree, _parentTree, _leaf, _leaf.x, _leaf.y, _leaf.width, _leaf.height - firstChild.height);
						return true;
					}
					else
					{
	
						_tree.Add(_leaf);
						return false;
					}
				}
			}
		}
        #endregion

        #region RoomClass
        [System.Serializable]
		public class Room
		{
			public int x, y, width, height;
	
			public Room(int _x, int _y, int _width, int _height)
			{
				x = _x;
				y = _y;
				width = _width;
				height = _height;
			}
		}
        #endregion


        // store tree with all smallest leafs
		public List<Leaf> tree = new List<Leaf>();
		// store all parent leafs
		public List<Leaf> parentTree = new List<Leaf>();
	
		static bool _splitH = false;
		BlueprintLayer blueprintLayer;

		int mapWidth;
		int mapHeight;
		static int minWidth;
		static int minHeight;
		int columnWidth; 

        // return values
		bool[,] dungeonMap;
		Vector3 endPosition;
		Vector3 startPosition;

        // store all new generated rooms
		List<Room> rooms = new List<Room>();
		List<Room> hallways = new List<Room>();

        public bool outputOnlyPlayerStartPosition;
		public bool outputOnlyPlayerEndPosition;
		public bool onlyCorridors;
        public bool onlyRooms;
        public int minBSPLeafHeight = 10;
		public int minBSPLeafWidth = 10;
		public int corridorWidth = 1;

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            _splitH = false;
			blueprintLayer = _layer;
	
			// assign values from config
            var _map = GetMapArray(_positions);
			mapWidth = _map.GetLength(0);
			mapHeight = _map.GetLength(1);

			minHeight = minBSPLeafHeight;
			minWidth = minBSPLeafWidth;
			columnWidth = corridorWidth;

            if (minBSPLeafWidth < 2)
			{
				minBSPLeafWidth = 2;
			}
			if (minBSPLeafHeight < 2)
			{
				minBSPLeafHeight = 2;
			}

            dungeonMap = new bool[mapWidth, mapHeight];

            // Generate the BSP tree
			GenerateBSPTree();

            // assign rooms to map
			for (int r = 0; r < rooms.Count; r++)
			{
				for (int x = rooms[r].x; x < rooms[r].x + rooms[r].width; x++)
				{
					for (int y = rooms[r].y; y < rooms[r].y + rooms[r].height; y++)
					{
						dungeonMap[x, y] = true;
					}
				}
			}
	
			// assign hallways to map
			for (int h = 0; h < hallways.Count; h++)
			{
				for (int x = hallways[h].x; x < hallways[h].x + hallways[h].width; x++)
				{
					for (int y = hallways[h].y; y < hallways[h].y + hallways[h].height; y++)
					{
						dungeonMap[x, y] = true;
					}
				}
			}
	
			// clean up borders
			for (int x = 0; x < mapWidth; x ++)
			{
				for (int y = 0; y < mapHeight; y ++)
				{
					if (x == 0 || x == 1 || x == mapWidth - 1 || x == mapWidth - 2 || y == 0 || y == 1 || y == mapHeight - 1 || y == mapHeight -2)
					{
						dungeonMap[x, y] = false;
					}
				}
			}

            // assign start and end position
			
			if (outputOnlyPlayerStartPosition || outputOnlyPlayerEndPosition)
			{
				var _startPosition = Vector3.zero;
				var _endPosition = Vector3.zero;

                // use random start room
				var _rndRoom = blueprintLayer.random.NextInt(0, rooms.Count);
                startPosition = new Vector3((rooms[_rndRoom].width / 2) + rooms[_rndRoom].x, 0, (rooms[_rndRoom].height / 2) + rooms[_rndRoom].y);
                
                _startPosition = startPosition;

                // end position
				// instead of assigning the last room we will 
				// get the room which is furthest away from the start position
				var _lastDistance = 0f;
				for (int r = 0; r < rooms.Count; r++)
				{
					var _roomCenter = new Vector3((rooms[r].width / 2) + rooms[r].x, 0, (rooms[r].height / 2) + rooms[r].y);
					float _dist = Vector3.Distance(_startPosition, _roomCenter);
					if (_dist > _lastDistance)
					{
						endPosition = _roomCenter;
						_lastDistance = _dist;
					}
				}
		
				_endPosition = endPosition;
				
				bool[,] _playerPositionOutputMap = new bool[dungeonMap.GetLength(0), dungeonMap.GetLength(1)];
			
				
				if (outputOnlyPlayerStartPosition)
				{
					_playerPositionOutputMap[(int)_startPosition.x, (int)_startPosition.z] = true;
				}
				else if (outputOnlyPlayerEndPosition)
				{
					_playerPositionOutputMap[(int)_endPosition.x, (int)_endPosition.z] = true;
				}
				
                for (int x = 0; x < _playerPositionOutputMap.GetLength(0); x++)
                {
                    for (int y = 0; y < _playerPositionOutputMap.GetLength(1); y++)
                    {
                        if (_playerPositionOutputMap[x, y])
                        {
                            _positions.Add(new Vector2(x, y));
                        }
                    }
                }

                return _positions;
            }

            // return only corridors
			if (onlyCorridors)
			{
				dungeonMap = new bool[mapWidth, mapHeight];
				for (int h = 0; h < hallways.Count; h++)
				{
					for (int x = hallways[h].x; x < hallways[h].x + hallways[h].width; x++)
					{
						for (int y = hallways[h].y; y < hallways[h].y + hallways[h].height; y++)
						{
							dungeonMap[x,y] = true;
						}
					}
				}
				
				for (int r = 0; r < rooms.Count; r++)
				{
					for (int x = rooms[r].x; x < rooms[r].x + rooms[r].width; x++)
					{
						for (int y = rooms[r].y; y < rooms[r].y + rooms[r].height; y++)
						{
							if (dungeonMap[x, y])
							{
								dungeonMap[x,y] = false;
							}
						}
					}
				}
				
			}

            if (onlyRooms) 
            {
                dungeonMap = new bool[mapWidth, mapHeight];
				for (int h = 0; h < hallways.Count; h++)
				{
					for (int x = hallways[h].x; x < hallways[h].x + hallways[h].width; x++)
					{
						for (int y = hallways[h].y; y < hallways[h].y + hallways[h].height; y++)
						{
							dungeonMap[x,y] = false;
						}
					}
				}

                for (int r = 0; r < rooms.Count; r++)
				{
					for (int x = rooms[r].x; x < rooms[r].x + rooms[r].width; x++)
					{
						for (int y = rooms[r].y; y < rooms[r].y + rooms[r].height; y++)
						{
							if (!dungeonMap[x, y])
							{
								dungeonMap[x,y] = true;
							}
						}
					}
				}
            }


            for (int i = 0; i < mapWidth; i++)
            {
                for (int j = 0; j < mapHeight; j++)
                {
                    if (dungeonMap[i, j])
                    {
                        _positions.Add(new Vector2(i, j));
                    }
                }
            }

            return _positions;
        }
		

        // First generate BSP tree
		void GenerateBSPTree()
		{
			tree = new List<Leaf>();
			parentTree = new List<Leaf>();
	
			// start generating by creating a new root leaf
			var _rootLeaf = new Leaf(tree, parentTree, null, 0, 0, mapWidth, mapHeight);
	
			BuildRooms();
		}
	
		// Build Rooms and hallways from BSP tree
		void BuildRooms()
		{
			rooms = new List<Room>();
	
			for (int i = 0; i < tree.Count; i++)
			{
	
				var _roomWidth = (int)blueprintLayer.random.NextFloat(tree[i].width * 0.5f, tree[i].width - 1);
				var _roomHeight = (int)blueprintLayer.random.NextFloat(tree[i].height * 0.5f, tree[i].height - 1);
				var _roomX = (int)blueprintLayer.random.NextFloat(tree[i].x + 1, (tree[i].width - _roomWidth - 1) + tree[i].x);
				var _roomY = (int)blueprintLayer.random.NextFloat(tree[i].y + 1, (tree[i].height - _roomHeight - 1) + tree[i].y);
	
				if (_roomWidth >= 2 && _roomHeight >= 2)
				{
					rooms.Add(new Room(_roomX, _roomY, _roomWidth, _roomHeight));
				}
			}
	
			BuildHallways();
		}
	
		// Build hallways
		void BuildHallways()
		{
			hallways = new List<Room>();
	
			for (int i = 0; i < parentTree.Count; i++)
			{
				// first connect the first child with the second child of the parent leafs
				var _centerPointFirstChild = new Vector2((parentTree[i].firstChild.width / 2) + parentTree[i].firstChild.x, (parentTree[i].firstChild.height / 2) + parentTree[i].firstChild.y);
				var _centerPointSecondChild = new Vector2((parentTree[i].secondChild.width / 2) + parentTree[i].secondChild.x, (parentTree[i].secondChild.height / 2) + parentTree[i].secondChild.y);
	
				// if first child has the same x pos as second get distance and build hallway along the z axis
				if (_centerPointFirstChild.x == _centerPointSecondChild.x)
				{
					var _distZ = Vector3.Distance(_centerPointFirstChild, _centerPointSecondChild);
	
					if (_centerPointSecondChild.y > _centerPointFirstChild.y)
					{
						hallways.Add(new Room((int)_centerPointFirstChild.x, (int)_centerPointFirstChild.y, columnWidth, (int)_distZ));
					}
					else
					{
						hallways.Add(new Room((int)_centerPointFirstChild.x, (int)_centerPointFirstChild.y - (int)_distZ, columnWidth, (int)_distZ));
					}
				}
				// build hallway along the x axis
				else
				{
					var _distX = Vector3.Distance(_centerPointFirstChild, _centerPointSecondChild);
	
					if (_centerPointSecondChild.x > _centerPointFirstChild.x)
					{
						hallways.Add(new Room((int)_centerPointFirstChild.x, (int)_centerPointFirstChild.y, (int)_distX, columnWidth));
					}
					else
					{
						hallways.Add(new Room((int)_centerPointFirstChild.x - (int)_distX, (int)_centerPointFirstChild.y, (int)_distX, columnWidth));
					}
				}
			}
	
			// now we connect all parent leafs and build hallways between them
			// to make sure all rooms are connected
			for (int i = 0; i < parentTree.Count; i ++)
			{
	
				if (i + 1 < parentTree.Count)
				{
					var _nextParentCenter = new Vector2((parentTree[i + 1].width / 2) + parentTree[i + 1].x, (parentTree[i + 1].height / 2) + parentTree[i + 1].y);
					var _parentCenter = new Vector2((parentTree[i].width / 2) + parentTree[i].x, (parentTree[i].height / 2) + parentTree[i].y);

					if (blueprintLayer.random.NextFloat(0f, 1f) <= 0.5f)
					// if (Random.Range(0f, 1f) <= 0.5f)
					{
						var _minX = (int)Mathf.Min(_parentCenter.x, _nextParentCenter.x);
						var _maxX = (int)Mathf.Max(_parentCenter.x, _nextParentCenter.x);
						var _minY = (int)Mathf.Min(_parentCenter.y, _nextParentCenter.y);
						var _maxY = (int)Mathf.Max(_parentCenter.y, _nextParentCenter.y);
						// Horizontal corridor
						hallways.Add(new Room(_minX, _minY, _maxX - _minX, columnWidth));
						// Vertical corridor
						hallways.Add(new Room(_minX, _minY, columnWidth, _maxY - _minY));
	
					}
					else
					{
						var _minX = (int)Mathf.Min(_parentCenter.x, _nextParentCenter.x);
						var _maxX = (int)Mathf.Max(_parentCenter.x, _nextParentCenter.x);
						var _minY = (int)Mathf.Min(_parentCenter.y, _nextParentCenter.y);
						var _maxY = (int)Mathf.Max(_parentCenter.y, _nextParentCenter.y);
						// Vertical corridor
						hallways.Add(new Room(_minX, _minY, columnWidth, _maxY - _minY));
						// Horizontal corridor
						hallways.Add(new Room(_minX, _minY, _maxX - _minX, columnWidth));
					}
				}
			}
		}
	
    }
}