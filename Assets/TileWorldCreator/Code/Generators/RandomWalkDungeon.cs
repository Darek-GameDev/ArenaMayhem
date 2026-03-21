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

using System;
using System.Collections.Generic;
using System.Linq;
using GiantGrey.TileWorldCreator.Attributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GiantGrey.TileWorldCreator
{
    [Modifier(ModifierAttribute.Category.Generators, "Random Walk Dungeon", "")]
    public class RandomWalkDungeon : BlueprintModifier
    {
        public bool randomStartPosition;

        [SerializeField]
        public Vector2Int startPosition = Vector2Int.zero;

        [SerializeField]
        private int iterations = 10;
        public int walkLength = 10;
        public bool startRandomlyEachIteration = true;

        [SerializeField]
        private int corridorLength = 14, corridorCount = 5;
        [SerializeField]
        [Range(0.1f, 1f)]
        private float roomPercent = 0.8f;

        BlueprintLayer blueprintLayer;

        private List<Vector2Int> cardinalDirectionsList = new List<Vector2Int>()
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
        };

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            blueprintLayer = _layer;
        
            HashSet<Vector2Int> potentialRoomPositions = new HashSet<Vector2Int>();

            var corridors = CreateCorridors(potentialRoomPositions);
            var rooms = CreateRooms(potentialRoomPositions);

            List<Vector2Int> deadEnds = FindDeadEnds(corridors);
            CreateRoomsAtDeadEnd(deadEnds, rooms);

            corridors.UnionWith(rooms);
            _positions.UnionWith(corridors.Select(pos => (Vector2)pos));

            return _positions;
        }

        private HashSet<Vector2Int> CreateCorridors(HashSet<Vector2Int> potentialRoomPositions)
        {
            HashSet<Vector2Int> corridorPositions = new HashSet<Vector2Int>();
            if (randomStartPosition)
            {
                startPosition = new Vector2Int(blueprintLayer.random.NextInt(0, asset.width), blueprintLayer.random.NextInt(0, asset.height));
            }

            var currentPosition = startPosition;
            potentialRoomPositions.Add(currentPosition);

            for (int i = 0; i < corridorCount; i++) 
            {
                var corridor = RandomWalkCorridor(currentPosition, corridorLength, asset.width, asset.height);
                currentPosition = corridor.Last();
                potentialRoomPositions.Add(currentPosition);
                corridorPositions.UnionWith(corridor);
            }

            return corridorPositions;
        }

        
        private HashSet<Vector2Int> CreateRooms(HashSet<Vector2Int> potentialRoomPositions)
        {
            HashSet<Vector2Int> roomPositions = new HashSet<Vector2Int>();
            int roomToCreateCount = Mathf.RoundToInt(potentialRoomPositions.Count * roomPercent);

            List<Vector2Int> roomToCreate = potentialRoomPositions.OrderBy(a => Guid.NewGuid()).Take(roomToCreateCount).ToList();

            foreach (var roomPosition in roomToCreate)
            {
                var room = RunRandomWalk(roomPosition);
                roomPositions.UnionWith(room);
            }

            return roomPositions;
        }

        private void CreateRoomsAtDeadEnd(List<Vector2Int> deadEnds, HashSet<Vector2Int> rooms)
        {
            foreach (var deadEnd in deadEnds)
            {
                if (!rooms.Contains(deadEnd))
                {
                    var room = RunRandomWalk(deadEnd);
                    rooms.UnionWith(room);
                }
            }
        }

        private List<Vector2Int> FindDeadEnds(HashSet<Vector2Int> corridors)
        {
            List<Vector2Int> deadEnds = new List<Vector2Int>();

            foreach (var cell in corridors)
            {
                int neighborsCount = 0;
                foreach (var dir in cardinalDirectionsList)
                {
                    if (corridors.Contains(cell + dir))
                    {
                        neighborsCount++;
                    }
                }

                if (neighborsCount == 1)
                {
                    deadEnds.Add(cell);
                }
            }

            return new List<Vector2Int>(deadEnds);
        }

        private HashSet<Vector2Int> RunRandomWalk(Vector2Int position)
        {
            var currentPos = position;

            HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();

            for (int i = 0; i < iterations; i++)
            {
                var path = SimpleRandomWalk(currentPos, walkLength, asset.width, asset.height);
                floorPositions.UnionWith(path);

                if(startRandomlyEachIteration)
                {
                    currentPos = floorPositions.ElementAt(blueprintLayer.random.NextInt(0, floorPositions.Count));
                }
            }

            return floorPositions;
        }



        private HashSet<Vector2Int> SimpleRandomWalk(Vector2Int startPosition, int walkLength, int width, int height)
        {
            HashSet<Vector2Int> path = new HashSet<Vector2Int>();

            path.Add(startPosition);
            var previousPosition = startPosition;

            for (int i = 0; i < walkLength; i++)
            {
                var newPosition = previousPosition + GetRandomCardinalDirection();
                if (newPosition.x >= 0 && newPosition.x < width && newPosition.y >= 0 && newPosition.y < height)
                {
                    path.Add(newPosition);	
                    previousPosition = newPosition;
                }
            }

            return path;
        }

        private Vector2Int GetRandomCardinalDirection()
        {
            return cardinalDirectionsList[blueprintLayer.random.NextInt(0, cardinalDirectionsList.Count)];
        }

        private List<Vector2Int> RandomWalkCorridor(Vector2Int startPosition, int corridorLength, int width, int height)
        {
            List<Vector2Int> corridor = new List<Vector2Int>();
            var direction = GetRandomCardinalDirection();
            var currentPos = startPosition;
            corridor.Add(currentPos);

            for(int i = 0; i < corridorLength; i++)
            {
                currentPos += direction;
                if (currentPos.x >= 0 && currentPos.x < width && currentPos.y >= 0 && currentPos.y < height)
                {
                    corridor.Add(currentPos);
                }
            }

            return corridor;
        }
    }
}