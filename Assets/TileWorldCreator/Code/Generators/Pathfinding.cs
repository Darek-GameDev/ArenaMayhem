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
using GiantGrey.TileWorldCreator.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

using UnityEngine;
using UnityEngine.UIElements;


namespace GiantGrey.TileWorldCreator
{
    [Modifier(ModifierAttribute.Category.Generators, "Pathfinding", "")]
    public class Pathfinding : BlueprintModifier
    {
        #region PublicVariables
        public enum StartTargetMode
        {
            RandomStartAndRandomTarget,
            RandomStartAndAllTargets,
            AllStartsAndRandomTarget
        }

            [HideInInspector]
            public string navigationLayerGuid;
            [HideInInspector]
            public string startLayerGuid;
            [HideInInspector]
            public string targetLayerGuid;
            [HideInInspector]
            public StartTargetMode startTargetMode;

    #endregion

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            var _navigationLayer = asset.GetBlueprintLayerByGuid(navigationLayerGuid);
            var _startLayer = asset.GetBlueprintLayerByGuid(startLayerGuid);
            var _targetLayer = asset.GetBlueprintLayerByGuid(targetLayerGuid);

            bool success;
            _positions = FindPath(_navigationLayer.allPositions, _startLayer.allPositions, _targetLayer.allPositions, startTargetMode, out success);

            if (!success)
                Debug.Log("No path found.");



            return _positions;
        }

        /// <summary>
        /// Find path with the preconfigured parameters from the layer
        /// </summary>
        /// <returns></returns>
        public bool FindPath(out HashSet<Vector2> _positions)
        {
        
            _positions = FindPath(
                asset.GetBlueprintLayerByGuid(navigationLayerGuid).allPositions,
                asset.GetBlueprintLayerByGuid(startLayerGuid).allPositions,
                asset.GetBlueprintLayerByGuid(targetLayerGuid).allPositions,
                startTargetMode,
                out bool success
            );

            return success;
        }

        HashSet<Vector2> FindPath(
            HashSet<Vector2> navigationLayer,
            HashSet<Vector2> startPositions,
            HashSet<Vector2> targetPositions,
            StartTargetMode mode,
            out bool pathFound
        )
        {
            pathFound = false;

            Vector2 start = Vector2.zero;
            Vector2 target = Vector2.zero;
            HashSet<Vector2> starts = new(startPositions);
            HashSet<Vector2> targets = new(targetPositions);

            HashSet<Vector2> resultPath = new();
            Queue<Vector2> queue = new();
            Dictionary<Vector2, Vector2> cameFrom = new();
            HashSet<Vector2> visited = new();

            // Setup starts and targets based on mode
            if (mode == StartTargetMode.RandomStartAndRandomTarget)
            {
                start = GetRandom(starts);
                target = GetRandom(targets);
                
                if (!navigationLayer.Contains(start) || !navigationLayer.Contains(target))
                    return resultPath;

                // Immediate success if start == target
                if (start == target)
                {
                    pathFound = true;
                    resultPath.Add(start);
                    return resultPath;
                }

                queue.Enqueue(start);
                visited.Add(start);
            }
            else if (mode == StartTargetMode.RandomStartAndAllTargets)
            {
                start = GetRandom(starts);
                if (!navigationLayer.Contains(start)) return resultPath;

                // Immediate success if start is in targets
                if (targets.Contains(start))
                {
                    pathFound = true;
                    resultPath.Add(start);
                    targets.Remove(start); // Remove from remaining targets
                }

                queue.Enqueue(start);
                visited.Add(start);
            }
            else if (mode == StartTargetMode.AllStartsAndRandomTarget)
            {
                target = GetRandom(targets);

                foreach (var s in starts)
                {
                    if (!navigationLayer.Contains(s))
                        continue;

                    // If start == target, no need to search
                    if (s == target)
                    {
                        pathFound = true;
                        resultPath.Add(s);
                        continue;
                    }

                    // Do a separate BFS from this start
                    Queue<Vector2> individualQueue = new();
                    Dictionary<Vector2, Vector2> individualCameFrom = new();
                    HashSet<Vector2> individualVisited = new();

                    individualQueue.Enqueue(s);
                    individualVisited.Add(s);

                    while (individualQueue.Count > 0)
                    {
                        Vector2 current = individualQueue.Dequeue();

                        if (current == target)
                        {
                            pathFound = true;
                            var path = ReconstructPath(individualCameFrom, current);
                            resultPath.UnionWith(path);
                            break;
                        }

                        foreach (var dir in Directions)
                        {
                            Vector2 neighbor = current + dir;
                            if (!navigationLayer.Contains(neighbor) || individualVisited.Contains(neighbor))
                                continue;

                            individualQueue.Enqueue(neighbor);
                            individualVisited.Add(neighbor);
                            individualCameFrom[neighbor] = current;
                        }
                    }
                }
            }

            // BFS
            while (queue.Count > 0)
            {
                Vector2 current = queue.Dequeue();

                bool reachedTarget = false;

                if (mode == StartTargetMode.RandomStartAndRandomTarget ||
                    mode == StartTargetMode.AllStartsAndRandomTarget)
                {
                    if (current == target)
                    {
                        reachedTarget = true;
                    }
                }
                else if (mode == StartTargetMode.RandomStartAndAllTargets)
                {
                    if (targets.Contains(current))
                    {
                        reachedTarget = true;
                        targets.Remove(current); // Remove target when reached
                    }
                }

                if (reachedTarget)
                {
                    pathFound = true;
                    var path = ReconstructPath(cameFrom, current);
                    resultPath.UnionWith(path);
                    continue;
                }

                foreach (var dir in Directions)
                {
                    Vector2 neighbor = current + dir;
                    if (!navigationLayer.Contains(neighbor) || visited.Contains(neighbor))
                        continue;

                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                    cameFrom[neighbor] = current;
                }
            }

            return resultPath;
        }

        HashSet<Vector2> ReconstructPath(Dictionary<Vector2, Vector2> cameFrom, Vector2 end)
        {
            HashSet<Vector2> path = new();
            Vector2 current = end;
            while (cameFrom.ContainsKey(current))
            {
                path.Add(current);
                current = cameFrom[current];
            }
            path.Add(current); // Add start
            return path;
        }

        Vector2 GetRandom(HashSet<Vector2> set)
        {
            int index = Random.Range(0, set.Count);
            foreach (var item in set)
            {
                if (index-- == 0)
                    return item;
            }
            return Vector2.zero;
        }

        readonly Vector2[] Directions = new Vector2[]
        {
            Vector2.up,
            Vector2.down,
            Vector2.left,
            Vector2.right
        };

#if UNITY_EDITOR
        public override VisualElement BuildInspector(Configuration _asset)
        {
            // return base.BuildInspector(_asset);

            var _serializedObject = new SerializedObject(this);

            var _root = new VisualElement();

            var _navigationLayerGuid = new LayerSelectDropdownElement(_asset, navigationLayerGuid, SelectNavigationLayer, "Navigation Layer");
            _root.Add(_navigationLayerGuid);

            var _startLayerGuid = new LayerSelectDropdownElement(_asset, startLayerGuid, SelectStartLayer, "Start Layer");
            _root.Add(_startLayerGuid);

            var _targetLayerGuid = new LayerSelectDropdownElement(_asset, targetLayerGuid, SelectTargetLayer, "Target Layer");
            _root.Add(_targetLayerGuid);

            var _mode = new PropertyField();
            _mode.BindProperty(_serializedObject.FindProperty(nameof(startTargetMode)));
            _root.Add(_mode);

            return _root;
        }

        void SelectNavigationLayer(string _layerName, string _layerGuid)
        {
            navigationLayerGuid = _layerGuid;
        }

        void SelectStartLayer(string _layerName, string _layerGuid)
        {
            startLayerGuid = _layerGuid;
        }

        void SelectTargetLayer(string _layerName, string _layerGuid)
        {
            targetLayerGuid = _layerGuid;
        }
#endif
    }

}