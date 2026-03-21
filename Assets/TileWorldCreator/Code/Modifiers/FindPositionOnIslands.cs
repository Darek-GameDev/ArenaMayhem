
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
using System.Threading.Tasks;
using UnityEngine;
using GiantGrey.TileWorldCreator.Attributes;

namespace GiantGrey.TileWorldCreator
{
    /// <summary>
    /// Returns a random position inside of an island
    /// </summary>
    [Modifier(ModifierAttribute.Category.Modifiers, "Find Position On Islands", "")]
    public class FindPositionOnIslands : BlueprintModifier
    {
        int width;
        int height;
        bool[,] visited;
        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1),  // Up
            new Vector2Int(0, -1), // Down
            new Vector2Int(-1, 0), // Left
            new Vector2Int(1, 0)   // Right
        };

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            if (_positions.Count == 0) return _positions;

            var _map = GetMapArray(_positions);

            visited = new bool[asset.width, asset.height];
            List<List<Vector2Int>> islands = new List<List<Vector2Int>>();
            HashSet<Vector2> _returnPositions = new HashSet<Vector2>();
            width = asset.width;
            height = asset.height;

            void FloodFill(int x, int y, List<Vector2Int> currentIsland)
            {
                Stack<Vector2Int> stack = new Stack<Vector2Int>();
                stack.Push(new Vector2Int(x, y));

                while (stack.Count > 0)
                {
                    Vector2Int cell = stack.Pop();
                    if (visited[cell.x, cell.y]) continue;

                    visited[cell.x, cell.y] = true;
                    currentIsland.Add(cell);

                    foreach (var dir in directions)
                    {
                        int nx = cell.x + dir.x;
                        int ny = cell.y + dir.y;

                        if (nx >= 0 && nx < width && ny >= 0 && ny < height && 
                            _map[nx, ny] && !visited[nx, ny])
                        {
                            stack.Push(new Vector2Int(nx, ny));
                        }
                    }
                }
            }

            // Iterate through the grid
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (_map[x, y] && !visited[x, y])
                    {
                        // Start a new island
                        List<Vector2Int> currentIsland = new List<Vector2Int>();
                        FloodFill(x, y, currentIsland);
                        islands.Add(currentIsland);
                    }
                }
            }


        
            foreach (var island in islands)
            {
                if (island.Count <= 5) continue;
                // Get random position in island
                int randomIndex = _layer.random.NextInt(0, island.Count-1);
                Vector2Int randomPosition = island[randomIndex];
                _returnPositions.Add(new Vector2(randomPosition.x, randomPosition.y));
            }

            
            return _returnPositions;

        }

        public async Task<List<List<Vector2Int>>> FindIslandsAsync(bool[,] grid)
        {
            int width = grid.GetLength(0);
            int height = grid.GetLength(1);
            bool[,] visited = new bool[width, height];
            List<List<Vector2Int>> islands = new List<List<Vector2Int>>();

            object lockObject = new object();

            // Helper function for flood-fill
            async Task<List<Vector2Int>> FloodFillAsync(int startX, int startY)
            {
                return await Task.Run(() =>
                {
                    List<Vector2Int> island = new List<Vector2Int>();
                    Stack<Vector2Int> stack = new Stack<Vector2Int>();
                    stack.Push(new Vector2Int(startX, startY));

                    while (stack.Count > 0)
                    {
                        Vector2Int cell = stack.Pop();
                        int x = cell.x;
                        int y = cell.y;

                        if (x < 0 || x >= width || y < 0 || y >= height || visited[x, y] || !grid[x, y])
                            continue;

                        lock (lockObject)
                        {
                            visited[x, y] = true;
                        }

                        island.Add(cell);

                        // Add neighbors (4-connectivity)
                        stack.Push(new Vector2Int(x - 1, y));
                        stack.Push(new Vector2Int(x + 1, y));
                        stack.Push(new Vector2Int(x, y - 1));
                        stack.Push(new Vector2Int(x, y + 1));
                    }

                    return island;
                });
            }

            // Traverse the grid and launch flood-fill tasks for each unvisited "true" cell
            List<Task<List<Vector2Int>>> floodFillTasks = new List<Task<List<Vector2Int>>>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (grid[x, y] && !visited[x, y])
                    {
                        floodFillTasks.Add(FloodFillAsync(x, y));
                    }
                }
            }

            // Await all flood-fill tasks
            var results = await Task.WhenAll(floodFillTasks);
            islands.AddRange(results);

            return islands;
        }

    }
}