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
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor.UIElements;
using UnityEditor;
#endif

namespace GiantGrey.TileWorldCreator
{
    [Modifier(ModifierAttribute.Category.Generators, "Cellular Automata", "")]
    public class CellularAutomata : BlueprintModifier
    {
        [HideInInspector]
        public float fillProbability = 0.45f;
        [HideInInspector]
        public int smoothingSteps = 5;
        [HideInInspector]
        public bool ensureConnected = true;
        [HideInInspector]
        public bool useMapSize = true;
        [HideInInspector]
        public Vector2Int position;
        [HideInInspector]
        public int customWidth = 5;
        [HideInInspector]
        public int customHeight = 5;
        private int width;
        private int height;
        private int[,] newMap;

        BlueprintLayer blueprintLayer;

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            if (useMapSize)
            {
                width = asset.width;
                height = asset.height;
            }
            else
            {
                width = customWidth;
                height = customHeight;
            }

            blueprintLayer = _layer;

            GenerateGrid();
            for (int i = 0; i < smoothingSteps; i++)
            {
                SmoothGrid();
            }
            if (ensureConnected)
            {
                ConnectIslands();
            }

            List<Vector2> activeCells = GetActiveCellPositions();
            for (int i = 0; i < activeCells.Count; i ++)
            {
                _positions.Add(activeCells[i]);
            }

            return _positions;
        }

        void GenerateGrid()
        {
            newMap = new int[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Use random struct from blueprint layer
                    newMap[x, y] = blueprintLayer.random.NextFloat() < fillProbability ? 1 : 0;
                }
            }
        }

        void SmoothGrid()
        {
            int[,] newGrid = new int[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int neighborCount = GetNeighborCount(x, y);
                    if (neighborCount > 4)
                        newGrid[x, y] = 1;
                    else if (neighborCount < 4)
                        newGrid[x, y] = 0;
                    else
                        newGrid[x, y] = newMap[x, y];
                }
            }
            newMap = newGrid;
        }

        int GetNeighborCount(int x, int y)
        {
            int count = 0;
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int nx = x + dx, ny = y + dy;
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        count += newMap[nx, ny];
                    }
                }
            }
            return count;
        }

        List<Vector2> GetActiveCellPositions()
        {
            List<Vector2> activeCells = new List<Vector2>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (newMap[x, y] == 1)
                    {
                        
                        if (useMapSize)
                        {
                            activeCells.Add(new Vector2(x, y));
                        }
                        // add position + position offset
                        else
                        {
                            activeCells.Add(new Vector2(x + position.x, y + position.y));
                        }
                    }
                }
            }
            return activeCells;
        }

        void ConnectIslands()
        {
            List<List<Vector2>> islands = FindIslands();
            if (islands.Count <= 1) return;

            List<Vector2> mainIsland = islands[0];
            for (int i = 1; i < islands.Count; i++)
            {
                Vector2 start = mainIsland[blueprintLayer.random.NextInt(0, mainIsland.Count)]; //Random.Range(0, mainIsland.Count)];
                Vector2 target = islands[i][blueprintLayer.random.NextInt(0, islands[i].Count)];
                CreateNaturalPath(start, target);
                mainIsland.AddRange(islands[i]);
            }
        }

        List<List<Vector2>> FindIslands()
        {
            List<List<Vector2>> islands = new List<List<Vector2>>();
            HashSet<Vector2> visited = new HashSet<Vector2>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (newMap[x, y] == 1 && !visited.Contains(new Vector2(x, y)))
                    {
                        List<Vector2> island = new List<Vector2>();
                        FloodFill(x, y, island, visited);
                        islands.Add(island);
                    }
                }
            }
            return islands;
        }

        void CreateNaturalPath(Vector2 start, Vector2 target)
        {
            Vector2 current = start;
            while (Vector2.Distance(current, target) > 1)
            {
                if (newMap[(int)current.x, (int)current.y] == 0)
                    newMap[(int)current.x, (int)current.y] = 1;

                List<Vector2> possibleMoves = GetNeighbors((int)current.x, (int)current.y);
                Vector2 nextStep = possibleMoves[blueprintLayer.random.NextInt(0, possibleMoves.Count)]; //Random.Range(0, possibleMoves.Count)];

                if (Vector2.Distance(nextStep, target) < Vector2.Distance(current, target))
                    current = nextStep;
            }
        }

        void FloodFill(int x, int y, List<Vector2> island, HashSet<Vector2> visited)
        {
            Queue<Vector2> queue = new Queue<Vector2>();
            queue.Enqueue(new Vector2(x, y));
            while (queue.Count > 0)
            {
                Vector2 current = queue.Dequeue();
                if (visited.Contains(current)) continue;
                visited.Add(current);
                island.Add(current);
                foreach (Vector2 neighbor in GetNeighbors((int)current.x, (int)current.y))
                {
                    if (!visited.Contains(neighbor) && newMap[(int)neighbor.x, (int)neighbor.y] == 1)
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        List<Vector2> GetNeighbors(int x, int y)
        {
            List<Vector2> neighbors = new List<Vector2>();
            int[] dx = { -1, 1, 0, 0 };
            int[] dy = { 0, 0, -1, 1 };
            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i], ny = y + dy[i];
                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    neighbors.Add(new Vector2(nx, ny));
                }
            }
            return neighbors;
        }

#if UNITY_EDITOR

        public override VisualElement BuildInspector(Configuration _asset)
        {
            var _so = new SerializedObject(this);
   
            var _root = new VisualElement();

            var _fillProbabilityField = new PropertyField();
            _fillProbabilityField.BindProperty(_so.FindProperty(nameof(fillProbability)));

            var _smoothSteps = new PropertyField();
            _smoothSteps.BindProperty(_so.FindProperty(nameof(smoothingSteps)));

            var _ensureConnectedField = new PropertyField();
            _ensureConnectedField.BindProperty(_so.FindProperty(nameof(ensureConnected)));

            var _positionField = new PropertyField();
            _positionField.BindProperty(_so.FindProperty(nameof(position)));

            var _customWidthField = new PropertyField();
            _customWidthField.BindProperty(_so.FindProperty(nameof(customWidth)));

            var _customHeightField = new PropertyField();
            _customHeightField.BindProperty(_so.FindProperty(nameof(customHeight)));

            var _useMapSizeField = new PropertyField();
            _useMapSizeField.BindProperty(_so.FindProperty(nameof(useMapSize)));
            _useMapSizeField.RegisterCallback<ChangeEvent<bool>>(evt => 
            {
                if (!evt.newValue)
                {
                    _customWidthField.style.display = DisplayStyle.Flex;
                    _customHeightField.style.display = DisplayStyle.Flex;
                    _positionField.style.display = DisplayStyle.Flex;
                }
                else
                {
                    _customWidthField.style.display = DisplayStyle.None;
                    _customHeightField.style.display = DisplayStyle.None;
                    _positionField.style.display = DisplayStyle.None;
                }
            });


            _root.Add(_fillProbabilityField);
            _root.Add(_smoothSteps);
            _root.Add(_ensureConnectedField);
            _root.Add(_useMapSizeField);
            _root.Add(_positionField);
            _root.Add(_customWidthField);
            _root.Add(_customHeightField);

            return _root;

        }

    #endif
    }
}