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
using GiantGrey.TileWorldCreator.Attributes;
using UnityEngine;

namespace GiantGrey.TileWorldCreator
{
    [Modifier(ModifierAttribute.Category.Generators, "Poisson Disc Sampling", "")]
    public class PoissonDiscSampling : BlueprintModifier
    {
        public float radius = 1f;
        public int numberSamplesBeforeRejection = 30;

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            float cellSize = radius / Mathf.Sqrt(2);
            int[,] grid = new int[Mathf.CeilToInt(asset.width / cellSize), Mathf.CeilToInt(asset.height / cellSize)];
            HashSet<Vector2> points = new HashSet<Vector2>();
            List<Vector2> spawnPoints = new List<Vector2> { new Vector2(asset.width / 2f, asset.height / 2f) };

            while (spawnPoints.Count > 0)
            {
                int spawnIndex = Random.Range(0, spawnPoints.Count);
                Vector2 spawnCenter = spawnPoints[spawnIndex];
                bool candidateAccepted = false;

                for (int i = 0; i < numberSamplesBeforeRejection; i++)
                {
                    float angle = Random.value * Mathf.PI * 2;
                    Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    Vector2 candidate = spawnCenter + dir * Random.Range(radius, 2 * radius);
                    Vector2Int candidateInt = Vector2Int.RoundToInt(candidate);

                    if (IsValid(candidate, asset.width, asset.height, cellSize, radius, points, grid))
                    {
                        points.Add(candidateInt);
                        spawnPoints.Add(candidateInt);
                        grid[(int)(candidateInt.x / cellSize), (int)(candidateInt.y / cellSize)] = points.Count;
                        candidateAccepted = true;
                        break;
                    }
                }

                if (!candidateAccepted)
                {
                    spawnPoints.RemoveAt(spawnIndex);
                }
            }
            return points;
        }


        private static bool IsValid(Vector2 candidate, int width, int height, float cellSize, float radius, HashSet<Vector2> points, int[,] grid)
        {
            if (candidate.x < 0 || candidate.y < 0 || candidate.x >= width || candidate.y >= height)
                return false;

            int cellX = (int)(candidate.x / cellSize);
            int cellY = (int)(candidate.y / cellSize);
            int searchRadius = 2;

            for (int x = Mathf.Max(0, cellX - searchRadius); x <= Mathf.Min(cellX + searchRadius, grid.GetLength(0) - 1); x++)
            {
                for (int y = Mathf.Max(0, cellY - searchRadius); y <= Mathf.Min(cellY + searchRadius, grid.GetLength(1) - 1); y++)
                {
                    int pointIndex = grid[x, y] - 1;
                    if (pointIndex != -1)
                    {
                        Vector2 existingPoint = new Vector2(x * cellSize, y * cellSize);
                        if (Vector2.Distance(candidate, existingPoint) < radius)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }
}