
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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Profiling;
using UnityEngine.Rendering;


using GiantGrey.TileWorldCreator.Utilities;
using GiantGrey.TileWorldCreator.Attributes;
using GiantGrey.TileWorldCreator.Components;
using GiantGrey.TileWorldCreator.UI;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace GiantGrey.TileWorldCreator
{
    [System.Serializable]
    [BuildLayer("Tiles", "Tiles.twc")]
    public class TilesBuildLayer : BuildLayer, ISerializationCallbackReceiver
    {
        public Configuration configuration;
        private TileWorldCreatorManager manager;
        public bool useDualGrid = true;
        public bool meshGenerationOverride = false;
        public bool mergeTiles;
        public ShadowCastingMode shadowCastingMode;
        public LayerMask objectLayer;
        public RenderingLayerMask renderingLayer;
        public Configuration.ColliderType colliderType;
        public float tileColliderHeight;
        public float tileColliderExtrusionHeight;
        public bool invertCollisionWalls;

        // public List<TilePresetSelection> tilePresets;
        public List<TilePresetSelection> tilePresetsTop = new List<TilePresetSelection>();
        public List<TilePresetSelection> tilePresetsMiddle = new List<TilePresetSelection>();
        public List<TilePresetSelection> tilePresetsBottom = new List<TilePresetSelection>();


        [System.Serializable]
        public class TilePresetSelection
        {
            [TilePresetPopup]
            public TilePreset preset;
            /// <summary>
            /// Used for multi layers configuration
            /// </summary>
            public float tileHeight;
            [Range(0f, 1f)]
            public float weight = 1f;
        }

        public bool scaleTileToCellSize = true;
        public float layerYOffset = 0f;
        internal float tmpLayerOffset = 0f;
        public Vector3 scaleOffset = Vector3.one;

        // GRASS RENDERER - NONE EXISTENT FEATURE
        #region TWC_GRASS_RENDERER
        public bool addGrassRenderer;
        public int grassPointCount;
        public ShadowCastingMode grassShadowCastingMode;
        public Material grassMaterial_A;
        public Material grassMaterial_B;
        public Mesh grassBladeMesh_A;
        public Mesh grassBladeMesh_B;
        public float grassMeshAWeight;
        public float grassMeshBWeight;
        #endregion

        [System.Serializable]
        public class TilePresetOverride
        {
            [HideInInspector]
            public string name;
            [BlueprintLayerDropdownAttribute()]
            public string blueprintOverrideLayer;
            [TilePresetPopup]
            public TilePreset preset;
            /// <summary>
            /// This value defines how many neighboring blueprint cells are needed for a tile to be replaced by the override preset. 
            /// </summary>
            [Range(1, 4)]
            [HelpBox("Only when using DualGrid. This value defines how many neighboring blueprint cells are needed for a tile to be replaced by the override preset.")]
            public int requiredNeighbourCount = 1;

            public TilePresetOverride()
            {
                requiredNeighbourCount = 1;
            }
        }

        [System.Serializable]
        public class TileLayers
        {
            public string name;
            public float heightOffset;
            public bool ignoreFillTiles;
            public List<TilePresetOverride> layerOverrides = new List<TilePresetOverride>();

        }

        public List<TileLayers> tileLayers = new List<TileLayers>();


        private Dictionary<int, Dictionary<Vector2, TileData>> tileGrid = new Dictionary<int, Dictionary<Vector2, TileData>>();
        private List<TileData> newTiles = new List<TileData>();


        [SerializeField]
        private List<Vector2> serializedWorldPositions = new List<Vector2>();

        [SerializeField]
        private List<TileData> serializedTiles = new List<TileData>();

        [SerializeField]
        private List<Vector2> allPositions = new List<Vector2>();
        private List<Vector2> removePositions = new List<Vector2>();


        private List<int> modifiedClusters = new List<int>();
        private HashSet<int> modifiedClustersHashSet = new HashSet<int>();

        private int lastClusterGenerationKey;
        private float progress;



        public class SortedTiles
        {
            public int clusterID { get; set; }
            public List<TileData> tiles;

            public SortedTiles(int _cluster)
            {
                clusterID = _cluster;
                tiles = new List<TileData>();
            }
        }

        public class TypeConfiguration
        {
            public TilePreset.TileType tileType;
            public List<int> configurations;
        }

        public class RotationConfiguration
        {
            public int rotation;
            public List<int> configurations;
        }


        private static readonly Dictionary<int, TilePreset.TileType> configurationToTileTypeDictionary = InitializeDictionary();
        private static readonly Dictionary<int, TilePreset.TileType> configurationToTileTypeDictionaryDual = InitializeDictionaryDual();
        private static Dictionary<int, int> configurationToRotationDictionary = InitializeRotationDictionary();
        private static Dictionary<int, int> configurationToRotationDictionaryDual = InitializeRotationDictionaryDual();


        // Top left, top right, bottom left, bottom right
        bool[] tmpFourTileNeighbours = new bool[]
        {
            false, false, false, false
        };

        bool[] tmpEightTileNeighbours = new bool[]
        {
            false, false, false, false, false, false, false, false, false
        };

        private GameObject tmpLayerObject;
        // private BlueprintLayer blueprintLayer;
        private GameObject owner;

        public TilesBuildLayer()
        {
            tileLayers.Add(new TileLayers());
        }
        
        private static void AddConfigurationsToDictionary(Dictionary<int, TilePreset.TileType> _dictionary, TilePreset.TileType tileType, List<int> configurations)
        {
            foreach (var config in configurations)
            {
                if (!_dictionary.ContainsKey(config))
                {
                    _dictionary.Add(config, tileType);
                }
                // else
                // {
                //     Debug.LogWarning($"Duplicate configuration {config} found for tile type {tileType}. Skipping.");
                // }
            }
        }

        private static void AddConfigurationsToRotationDictionary(Dictionary<int, int> dictionary, int rotation, List<int> configurations)
        {

            foreach (var _config in configurations)
            {
                if (!dictionary.ContainsKey(_config))
                {
                    dictionary.Add(_config, rotation);
                }
            }
        }

        private static Dictionary<int, TilePreset.TileType> InitializeDictionary()
        {
            var dictionary = new Dictionary<int, TilePreset.TileType>();

            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.NRMGRD_cornerWay, TileConfigurations.NRMGRD_cornerWay_configurations);
            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.NRMGRD_cornerFill, TileConfigurations.NRMGRD_cornerFill_configurations);
            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.NRMGRD_edgeWay, TileConfigurations.NRMGRD_edgeWay_configurations);
            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.NRMGRD_edgeFill, TileConfigurations.NRMGRD_edgeFill_configurations);
            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.NRMGRD_fill, TileConfigurations.NRMGRD_fill_configurations);
            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.NRMGRD_single, TileConfigurations.NRMGRD_single_configurations);
            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.NRMGRD_threeWay, TileConfigurations.NRMGRD_threeWay_configurations);
            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.NRMGRD_threeWayFill, TileConfigurations.NRMGRD_threeWayFill_configurations);
            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.NRMGRD_threeCorner, TileConfigurations.NRMGRD_threeCorner_configurations);
            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.NRMGRD_deadEnd, TileConfigurations.NRMGRD_deadEndWay_configurations);
            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.NRMGRD_fourWay, TileConfigurations.NRMGRD_fourWay_configurations);
            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.NRMGRD_edgeCornerFill, TileConfigurations.NRMGRD_edgeCornerFill_configurations);
            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.NRMGRD_doubleCorner, TileConfigurations.NRMGRD_doubleCorner_configurations);
            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.NRMGRD_interiorCorner, TileConfigurations.NRMGRD_interiorCorner_configurations);

            return dictionary;
        }

        private static Dictionary<int, TilePreset.TileType> InitializeDictionaryDual()
        {
            var dictionary = new Dictionary<int, TilePreset.TileType>();

            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.DUALGRD_corner, TileConfigurations.DUALGRD_corner_configurations);
            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.DUALGRD_edge, TileConfigurations.DUALGRD_edge_configurations);
            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.DUALGRD_fill, TileConfigurations.DUALGRD_fill_configurations);
            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.DUALGRD_interiorCorner, TileConfigurations.DUALGRD_interiorCorner_configurations);
            AddConfigurationsToDictionary(dictionary, TilePreset.TileType.DUALGRD_doubleInteriorCorner, TileConfigurations.DUALGRD_doubleInteriorCorner_configurations);

            return dictionary;
        }

        private static Dictionary<int, int> InitializeRotationDictionary()
        {
            var dictionary = new Dictionary<int, int>();

            AddConfigurationsToRotationDictionary(dictionary, 0, TileConfigurations.NRMGRD_rotationZero_configurations);
            AddConfigurationsToRotationDictionary(dictionary, 90, TileConfigurations.NRMGRD_rotation90_configurations);
            AddConfigurationsToRotationDictionary(dictionary, 180, TileConfigurations.NRMGRD_rotation180_configurations);
            AddConfigurationsToRotationDictionary(dictionary, 270, TileConfigurations.NRMGRD_rotation270_configurations);

            return dictionary;
        }

        private static Dictionary<int, int> InitializeRotationDictionaryDual()
        {
            var dictionary = new Dictionary<int, int>();

            AddConfigurationsToRotationDictionary(dictionary, 0, TileConfigurations.rotationZeroConfigurations);
            AddConfigurationsToRotationDictionary(dictionary, 90, TileConfigurations.rotation90Configurations);
            AddConfigurationsToRotationDictionary(dictionary, 180, TileConfigurations.rotation180Configurations);
            AddConfigurationsToRotationDictionary(dictionary, 270, TileConfigurations.rotation270Configurations);

            return dictionary;
        }

        public TilePreset.TileType GetTileType(int _configuration, TileData _tileData, out int _rotation)
        {

            _rotation = 0;


            if (useDualGrid)
            {
                if (configurationToRotationDictionaryDual.TryGetValue(_configuration, out var _rot))
                {
                    _rotation = _rot;
                }

                if (configurationToTileTypeDictionaryDual.TryGetValue(_configuration, out var _tileType))
                {
                    return _tileType;
                }
            }
            else
            {
                if (configurationToRotationDictionary.TryGetValue(_configuration, out var _rot))
                {
                    _rotation = _rot;
                }

                if (configurationToTileTypeDictionary.TryGetValue(_configuration, out var _tileType))
                {
                    return _tileType;
                }
            }

            return TilePreset.TileType.none;
        }


        public override void ResetLayer(TileWorldCreatorManager _manager)
        {
            serializedTiles = new List<TileData>();
            serializedWorldPositions = new List<Vector2>();

            worldGrid = new Dictionary<int, Dictionary<Vector2, bool>>();
            tileGrid = new Dictionary<int, Dictionary<Vector2, TileData>>();

            if (_manager == null)
            {
                var _managers = GameObject.FindObjectsByType<TileWorldCreatorManager>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
                for (int i = 0; i < _managers.Length; i++)
                {
                    if (_managers[i].configuration == configuration)
                    {
                        _manager = _managers[i];
                    }
                }

            }
            else
            {
                manager = _manager;
            }


            if (_manager == null)
            {
                return;
            }

            tmpLayerObject = base.GetLayerObject(_manager.gameObject);
            if (tmpLayerObject != null)
            {

                var _clusters = tmpLayerObject.GetComponentsInChildren<ClusterIdentifier>(true);
                for (int c = 0; c < _clusters.Length; c++)
                {
                    var _cluster = _clusters[c];
                    DestroyImmediate(_cluster.gameObject);
                }
            }
        }

        public void AddTileLayer()
        {
            var _layer = new TileLayers();
            // _layer.presets = tileLayers[tileLayers.Count - 1].presets;
            _layer.layerOverrides = tileLayers[tileLayers.Count - 1].layerOverrides;
            _layer.heightOffset = tileLayers[tileLayers.Count - 1].heightOffset + 1;
            tileLayers.Add(_layer);
        }

        public void RemoveTileLayer(int _index)
        {
            tileLayers.RemoveAt(_index);
        }

        /// <summary>
        /// Set a new tile preset to this layer
        /// </summary>
        /// <param name="tilePreset"></param>
        public void SetNewTilePreset(TilePreset tilePreset)
        {
            tilePresetsTop.Clear();
            tilePresetsTop.Add(new TilePresetSelection() { preset = tilePreset, weight = 1 });
        }

        /// <summary>
        /// Set a new blueprint layer for this build layer
        /// </summary>
        /// <param name="_layer"></param>
        public void SetBlueprintLayer(BlueprintLayer _layer)
        {
            assignedBlueprintLayerGuid = _layer.guid;

            // if (!assignedBlueprintLayerGuids.Contains(_layer.guid))
            // {
            //     assignedBlueprintLayerGuids.Add(_layer.guid);
            // }
        }

        /// <summary>
        /// Set a new blueprint layer for this build layer
        /// </summary>
        /// <param name="_layerGuid"></param>
        public void SetBlueprintLayer(string _layerGuid)
        {
            assignedBlueprintLayerGuid = _layerGuid;

            // if (!assignedBlueprintLayerGuids.Contains(_layerGuid))
            // {
            //     assignedBlueprintLayerGuids.Add(_layerGuid);
            // }
        }


        private int GetClusterKeyFromPosition(Vector2 _position)
        {
            if (configuration == null) return 0;
            Vector2 adjustedPos = _position + new Vector2(0.5f, 0.5f);
            int clusterX = Mathf.FloorToInt(adjustedPos.x / configuration.clusterCellSize);
            int clusterY = Mathf.FloorToInt(adjustedPos.y / configuration.clusterCellSize);
            return clusterX + (configuration.clusterYMultiplier * clusterY);
        }
        


        public override void ExecuteLayer(Configuration _configuration, GameObject _owner, TileWorldCreatorManager _manager)
        {
            if (!isEnabled)
            {
                return;
            }

            configuration = _configuration;

            uint _seed;

            if (configuration.useGlobalRandomSeed)
            {
                _seed = (uint)configuration.globalRandomSeed;
            }
            else
            {
                _seed = configuration.currentRandomSeed; //(uint)(System.DateTime.Now.Ticks % uint.MaxValue);
            }

            random = new Unity.Mathematics.Random(_seed);


            if (configuration.cellSize != configuration.lastCellSize)
            {
                worldGrid = new Dictionary<int, Dictionary<Vector2, bool>>();
                tileGrid = new Dictionary<int, Dictionary<Vector2, TileData>>();

                configuration.lastCellSize = configuration.cellSize;
            }


            owner = _owner;
            manager = _manager;


            var _layer = _configuration.GetBlueprintLayerByGuid(assignedBlueprintLayerGuid);
            currentBlueprintLayer = _layer;

            if (_layer == null)
            {
                UnityEngine.Debug.Log(layerName + " - Assigned blueprint layer not found: " + assignedBlueprintLayerGuid + " - Please try to reselect it");
                return;
            }

            var _layerPositions = new HashSet<Vector2>(); 
            _layerPositions = _layer.GetAllCellPositions(_layerPositions);

            removePositions.Clear();


            List<Vector2> _newPositions = new List<Vector2>();
            var keys = new List<int>(worldGrid.Keys);
            foreach (var cls in keys)
            {
                var positions = new List<Vector2>(worldGrid[cls].Keys);
                foreach (var pos in positions)
                {
                    if (!_layerPositions.Contains(pos))
                    {
                        removePositions.Add(pos);
                        worldGrid[cls].Remove(pos);
                    }
                }
            }

            foreach (var pos in _layerPositions)
            {
                var _cluster = GetClusterKeyFromPosition(pos);
                if (!worldGrid.ContainsKey(_cluster))
                {
                    worldGrid.Add(_cluster, new Dictionary<Vector2, bool>());
                    _newPositions.Add(pos);
                }
                else
                {
                    if (!worldGrid[_cluster].ContainsKey(pos))
                    {
                        worldGrid[_cluster].Add(pos, true);
                        _newPositions.Add(pos);
                    }
                    else
                    {
                        worldGrid[_cluster][pos] = true;
                    }
                }
            }

            // Compare positions from overrides with current positions
            // This will force updating changes from assigned override layers.
            for (int l = 0; l < tileLayers.Count; l++)
            {
                for (int o = 0; o < tileLayers[l].layerOverrides.Count; o++)
                {

                    var _overrideLayer = configuration.GetBlueprintLayerByGuid(tileLayers[l].layerOverrides[o].blueprintOverrideLayer);
                    if (_overrideLayer == null)
                        continue;

                    var _overridePositions = new HashSet<Vector2>();
                    _overridePositions = _overrideLayer.GetAllCellPositions(_overridePositions);
                    foreach (var pos in _overridePositions)
                    {


                        var _cluster = GetClusterKeyFromPosition(pos);
                        if (!worldGrid.ContainsKey(_cluster) && !_newPositions.Contains(pos))
                        {
                            _newPositions.Add(pos);
                        }
                        else
                        {
                            if (worldGrid.ContainsKey(_cluster))
                            {
                                if (!worldGrid[_cluster].ContainsKey(pos) && !_newPositions.Contains(pos))
                                {
                                    _newPositions.Add(pos);
                                }
                            }
                        }
                    }
                }
            }

            // Get all clusters which needs to be updated
            modifiedClusters.Clear();
            modifiedClustersHashSet.Clear();

            foreach (var pos in _newPositions)
            {
                AddClusterNeighbors(modifiedClustersHashSet, GetClusterKeyFromPosition(pos));
                // modifiedClustersHashSet.Add(GetClusterKeyFromPosition(pos));
            }


            foreach (var pos in removePositions)
            {
                // Debug.Log(layerName + " - Remove cluster pos: " + pos + " cluster ID: " +  GetPositionHashMapKey(pos)); 
                AddClusterNeighbors(modifiedClustersHashSet, GetClusterKeyFromPosition(pos));
            }

            modifiedClusters = modifiedClustersHashSet.Where(c => c >= 0).ToList();


            List<Vector2> _positionsInCluster = new List<Vector2>();
            foreach (var cluster in modifiedClusters)
            {
                if (worldGrid.TryGetValue(cluster, out var clusterGrid))
                {
                    _positionsInCluster.AddRange(clusterGrid.Keys.Except(removePositions).Except(_newPositions));
                }
            }


            _newPositions.AddRange(_positionsInCluster);

            /// Update build layers which have same assigned blueprint layer assigned as overrides.
            for (int i = 0; i < configuration.buildLayerFolders.Count; i++)
            {
                for (int j = 0; j < configuration.buildLayerFolders[i].buildLayers.Count; j++)
                {
                    var _buildLayer = (configuration.buildLayerFolders[i].buildLayers[j] as TilesBuildLayer);
                    if (_buildLayer != null)
                    {
                        for (int m = 0; m < _buildLayer.tileLayers.Count; m++)
                        {
                            if ((_buildLayer).tileLayers[m].layerOverrides == null)
                            {
                                (_buildLayer).tileLayers[m].layerOverrides = new List<TilePresetOverride>();
                            }

                            if (manager == null)
                                continue;

                            for (int t = 0; t < (_buildLayer).tileLayers[m].layerOverrides.Count; t++)
                            {
                                if ((_buildLayer).tileLayers[m].layerOverrides[t].blueprintOverrideLayer == assignedBlueprintLayerGuid)
                                {
                                    if (_newPositions.Count > 0 && _newPositions != null)
                                    {
                                        // Add layer to the late update list. Those layers will be executed later
                                        if (manager.lateUpdateLayers == null)
                                        {
                                            manager.lateUpdateLayers = new HashSet<BuildLayer>();
                                        }

                                        manager.lateUpdateLayers.Add(configuration.buildLayerFolders[i].buildLayers[j]);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (configuration.useParallel)
            {
                if (!Application.isPlaying)
                {
#if UNITY_EDITOR
                    EditorCoroutines.Execute(GenerateTilesParallel(_newPositions, _layer));
#endif
                }
                else
                {
                    manager.StartCoroutine(GenerateTilesParallel(_newPositions, _layer));
                }
            }
            else
            {
                if (!Application.isPlaying)
                {
#if UNITY_EDITOR
                    EditorCoroutines.Execute(GenerateTiles(_newPositions, _layer));
#endif
                }
                else
                {
                    manager.StartCoroutine(GenerateTiles(_newPositions, _layer));
                }
            }
        }


        private void AddClusterNeighbors(HashSet<int> clusterSet, int clusterKey)
        {
            clusterSet.Add(clusterKey);
            clusterSet.Add(clusterKey + 1);
            clusterSet.Add(clusterKey - 1);
            clusterSet.Add(clusterKey + configuration.clusterYMultiplier);
            clusterSet.Add(clusterKey - configuration.clusterYMultiplier);
            clusterSet.Add(clusterKey + 1 + configuration.clusterYMultiplier);
            clusterSet.Add(clusterKey - 1 + configuration.clusterYMultiplier);
            clusterSet.Add(clusterKey + 1 - configuration.clusterYMultiplier);
            clusterSet.Add(clusterKey - 1 - configuration.clusterYMultiplier);
        }


        IEnumerator GenerateTilesParallel(List<Vector2> positions, BlueprintLayer _blueprintLayer)
        {

            // Profiler.BeginSample("GENERATE PARALLEL");

            var newTilesBag = new ConcurrentBag<TileData>();

            if (!Application.isPlaying)
            {
                if (availableClusters == null)
                    availableClusters = new HashSet<ClusterIdentifier>();
                else
                    availableClusters.Clear();

                if (tmpLayerObject == null)
                {
                    tmpLayerObject = GetLayerObject(manager.gameObject);
                }

                var _clusters = tmpLayerObject.gameObject.GetComponentsInChildren<ClusterIdentifier>(true);
                foreach (var cluster in _clusters)
                {
                    availableClusters.Add(cluster);
                }
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayProgressBar($"Generating {configuration.width * configuration.height} tiles", "Please wait...", 0f);
            }
#endif

            // Parallelize tile processing
            Parallel.For(0, positions.Count, i =>
            {
                Vector2 worldPosition = positions[i];
                if (isMasked(worldPosition)) return;

                if (useDualGrid)
                {
                    for (int x = -1; x < 1; x++)
                    {
                        for (int y = -1; y < 1; y++)
                        {
                            Vector2 cellPosition = new Vector2(worldPosition.x + (x + 0.5f), worldPosition.y + (y + 0.5f));
                            ProcessTileParallel(cellPosition, worldPosition, newTilesBag);
                        }
                    }
                }
                else
                {
                    ProcessTileParallel(worldPosition, worldPosition, newTilesBag);
                }
            });

            // Convert ConcurrentBag to List efficiently
            newTiles.Clear();
            // Pre-size to reduce reallocations
            if (newTiles.Capacity < newTilesBag.Count) newTiles.Capacity = newTilesBag.Count;
            foreach (var tile in newTilesBag) newTiles.Add(tile);

            // Update worldGrid sequentially to avoid ConcurrentDictionary overhead
            for (int i = 0; i < positions.Count; i++)
            {
                var worldPosition = positions[i];
                int worldGridHashmap = GetClusterKeyFromPosition(worldPosition);
                if (!worldGrid.TryGetValue(worldGridHashmap, out var subGrid))
                {
                    subGrid = new Dictionary<Vector2, bool>();
                    worldGrid[worldGridHashmap] = subGrid;
                }
                subGrid[worldPosition] = true;
            }

            // Merge tiles into tileGrid (single-threaded) to avoid contention in parallel section
            for (int i = 0; i < newTiles.Count; i++)
            {
                var t = newTiles[i];
                int hashMapKey = GetClusterKeyFromPosition(t.tilePosition);
                if (!tileGrid.TryGetValue(hashMapKey, out var subGrid))
                {
                    subGrid = new Dictionary<Vector2, TileData>();
                    tileGrid[hashMapKey] = subGrid;
                }
                // Overwrite or add; dedup naturally via dictionary
                subGrid[t.tilePosition] = t;
            }

            yield return null;

#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif
            // Refresh and sort tiles (sequential)
            RefreshAndSortTiles();

            // Profiler.EndSample();
        }

        void ProcessTileParallel(Vector2 cellPosition, Vector2 worldPosition, ConcurrentBag<TileData> newTilesBag)
        {
            // Only build tile data here; merge into dictionaries on main thread after parallel loop
            TileData newTile = new TileData
            {
                isAssigned = true,
                tilePosition = cellPosition,
                worldMapPosition = worldPosition
            };

            newTilesBag.Add(newTile);
        }



        IEnumerator GenerateTiles(List<Vector2> _positions, BlueprintLayer _blueprintLayer)
        {
            // var stopwatch = new Stopwatch();
            // stopwatch.Start();

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayProgressBar($"Generating {configuration.width * configuration.height} tiles", "Please wait...", 0f);
            }
#endif

            // Profiler.BeginSample("GENERATE");

            HashSet<Vector2> existingCellPositions = new HashSet<Vector2>(_positions.Count);
            // List<TileData> _newTiles = new List<TileData>(_positions.Count);
            newTiles.Clear();
            // Pre-size to expected upper bound (dual grid may create up to 4 tiles per position)
            int expected = useDualGrid ? _positions.Count * 4 : _positions.Count;
            if (newTiles.Capacity < expected) newTiles.Capacity = expected;

            // Only in editor mode
            if (!Application.isPlaying)
            {
                if (availableClusters != null)
                {
                    availableClusters.Clear();
                }
                else
                {
                    availableClusters = new HashSet<ClusterIdentifier>();
                }

                if (tmpLayerObject == null)
                {
                    if (manager == null)
                    {
                        var _managers = GameObject.FindObjectsByType<TileWorldCreatorManager>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
                        for (int i = 0; i < _managers.Length; i++)
                        {
                            if (_managers[i].configuration == configuration)
                            {
                                manager = _managers[i];
                                break;
                            }
                        }
                    }

                    tmpLayerObject = GetLayerObject(manager.gameObject);
                }

                ClusterIdentifier[] clusterArray = tmpLayerObject.gameObject.GetComponentsInChildren<ClusterIdentifier>(true);
                for (int c = 0; c < clusterArray.Length; c++)
                {
                    availableClusters.Add(clusterArray[c]);
                }
            }

            int posCount = _positions.Count;
            for (int i = 0; i < posCount; i++)
            {
                Vector2 _worldPosition = _positions[i];

                if (isMasked(_worldPosition)) continue;

                if (useDualGrid)
                {
                    ProcessDualGridTiles(_worldPosition, newTiles, existingCellPositions);
                }
                else
                {
                    ProcessSingleGridTile(_worldPosition, newTiles, existingCellPositions);
                }

                UpdateWorldGrid(_worldPosition);

                if ((i & 511) == 0)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        EditorUtility.DisplayProgressBar($"Generating {configuration.width * configuration.height} tiles", "Please wait...", (float)i / _positions.Count);
                    }
#endif
                    yield return null;
                }
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.ClearProgressBar();
            }
#endif
            RefreshAndSortTiles();

            // Profiler.EndSample();

            // stopwatch.Stop();
            // UnityEngine.Debug.Log($"Elapsed time: {stopwatch.ElapsedMilliseconds} ms" + " __ " + layerName);
        }

        void ProcessDualGridTiles(Vector2 worldPosition, List<TileData> newTiles, HashSet<Vector2> existingCellPositions)
        {
            for (int x = -1; x < 1; x++)
            {
                for (int y = -1; y < 1; y++)
                {
                    Vector2 cellPosition = new Vector2(worldPosition.x + (x + 0.5f), worldPosition.y + (y + 0.5f));
                    AddOrUpdateTile(cellPosition, worldPosition, newTiles, existingCellPositions);
                }
            }
        }

        void ProcessSingleGridTile(Vector2 worldPosition, List<TileData> newTiles, HashSet<Vector2> existingCellPositions)
        {
            AddOrUpdateTile(worldPosition, worldPosition, newTiles, existingCellPositions);
        }

        void AddOrUpdateTile(Vector2 cellPosition, Vector2 worldPosition, List<TileData> newTiles, HashSet<Vector2> existingCellPositions)
        {
            if (existingCellPositions.Contains(cellPosition)) return;

            Profiler.BeginSample("ADD OR UPDATE");
            int hashMapKey = GetClusterKeyFromPosition(cellPosition);

            TileData newTile = new TileData
            {
                isAssigned = true,
                // isNew = animatedPositions.Contains(worldPosition),
                tilePosition = cellPosition,
                worldMapPosition = worldPosition,
            };

            if (!tileGrid.TryGetValue(hashMapKey, out var subGrid))
            {
                subGrid = new Dictionary<Vector2, TileData>();
                tileGrid[hashMapKey] = subGrid;
            }

            if (subGrid.TryGetValue(cellPosition, out var existingTile))
            {
                existingTile.isAssigned = true;
                subGrid[cellPosition] = existingTile;
            }
            else
            {
                subGrid[cellPosition] = newTile;
            }

            newTiles.Add(newTile);
            existingCellPositions.Add(cellPosition);

            Profiler.EndSample();
        }

        void UpdateWorldGrid(Vector2 worldPosition)
        {
            int worldGridHashmap = GetClusterKeyFromPosition(worldPosition);
            if (!worldGrid.TryGetValue(worldGridHashmap, out var subGrid))
            {
                subGrid = new Dictionary<Vector2, bool>();
                worldGrid[worldGridHashmap] = subGrid;
            }
            subGrid[worldPosition] = true;
        }

        void RefreshAndSortTiles()
        {
            Profiler.BeginSample("REFRESH AND SORT");

            int _newTilesCount = newTiles.Count;
            for (int i = 0; i < _newTilesCount; i++)
            {
                newTiles[i] = RefreshPathCells2(newTiles[i], out int config);
            }

            // Preallocate capacity to reduce allocations
            Dictionary<int, SortedTiles> sortedTileMap = new Dictionary<int, SortedTiles>(_newTilesCount);

            for (int i = 0; i < _newTilesCount; i++)
            {
                TileData tile = newTiles[i];
                if (isNeighbourMasked(tile)) continue;

                int clusterID = GetClusterKeyFromPosition(tile.worldMapPosition);

                // var cluster = sortedTiles.FirstOrDefault(s => s.clusterID == clusterID);

                if (!sortedTileMap.TryGetValue(clusterID, out SortedTiles cluster))
                {
                    cluster = new SortedTiles(clusterID);
                    cluster.tiles.Add(tile);
                    sortedTileMap[clusterID] = cluster;
                }
                else
                {
                    cluster.tiles.Add(tile);
                }
            }

            Profiler.EndSample();
            if (sortedTileMap.Count > 0 || modifiedClusters.Count > 0)
            {
                if (!Application.isPlaying)
                {
#if UNITY_EDITOR
                    EditorCoroutines.Execute(InstantiateByClusters(sortedTileMap.Values.ToList()));
#endif
                }
                else
                {
                    manager.StartCoroutine(InstantiateByClusters(sortedTileMap.Values.ToList()));
                }
            }

        }

        IEnumerator InstantiateByClusters(List<SortedTiles> _sortedTiles)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayProgressBar("Instantiating tiles", "Please wait...", 0f);
            }
#endif

            for (int c = 0; c < modifiedClusters.Count; c++)
            {
                var _cluster = FindCluster(modifiedClusters[c], false);
                if (_cluster != null)
                {
                    if (Application.isPlaying)
                    {
                        GameObject.DestroyImmediate(_cluster);
                    }
                    else
                    {
                        GameObject.DestroyImmediate(_cluster);
                    }
                }
            }


            for (int t = 0; t < tileLayers.Count; t++)
            {
                if (!useMultiLayers && t > 0)
                {
                    continue;
                }

                for (int i = 0; i < _sortedTiles.Count; i++)
                {
                    for (int j = 0; j < _sortedTiles[i].tiles.Count; j++)
                    {
                        if (_sortedTiles[i].tiles[j].tileType == TilePreset.TileType.DUALGRD_fill && tileLayers[t].ignoreFillTiles) continue;
                        if (_sortedTiles[i].tiles[j].tileType == TilePreset.TileType.NRMGRD_fill && tileLayers[t].ignoreFillTiles) continue;

                        InstantiateTile(_sortedTiles[i].tiles[j], _sortedTiles[i].clusterID, t);

                    }
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        EditorUtility.DisplayProgressBar("Instantiating tiles", "Please wait...", (float)i / (float)_sortedTiles.Count);
                    }
#endif

                   
                }
            }

#if UNITY_EDITOR
            EditorUtility.ClearProgressBar();
#endif

            yield return null;

            // Merge clusters
            MergeClusters();
            
        }
        
        
        public void MergeClusters()
        {
             // Apply settings with mesh generation override
            var _mergeObjects = meshGenerationOverride ? mergeTiles : configuration.mergeTiles;
            var _colliderType = meshGenerationOverride ? colliderType : configuration.colliderType;
            var _shadowCastingMode = meshGenerationOverride ? shadowCastingMode : configuration.shadowCastingMode;
            var _objectLayer = meshGenerationOverride ? objectLayer : configuration.objectLayer;
            var _renderingLayer = meshGenerationOverride ? renderingLayer : configuration.renderingLayer;
            var _tileColliderExtrusion = meshGenerationOverride ? tileColliderExtrusionHeight : configuration.tileColliderExtrusionHeight;
            var _tileColliderHeight = meshGenerationOverride ? tileColliderHeight : configuration.tileColliderHeight;
            var _invertWalls = meshGenerationOverride ? invertCollisionWalls : configuration.invertCollisionWalls;

            if (_mergeObjects)
            {
                for (int i = 0; i < modifiedClusters.Count; i++)
                {
                    var _cluster = FindCluster(modifiedClusters[i], false);
                    
                    if (_cluster == null)
                        continue;

                    // Check if cluster still exists and is valid
                    if (_cluster == null || _cluster.Equals(null))
                        continue;

                    MeshCombiner _combiner;
                    if (!_cluster.TryGetComponent<MeshCombiner>(out _combiner))
                    {
                        _combiner = _cluster.AddComponent<MeshCombiner>();
                    }

                    _combiner.CreateMultiMaterialMesh = true;
                    _combiner.DestroyCombinedChildren = true;

                    _combiner.CombineMeshes(false);
                    
                    // Verify cluster wasn't destroyed during combine
                    if (_cluster == null || _cluster.Equals(null))
                        continue;
                    
                    var _meshFilter = _cluster.GetComponent<MeshFilter>();
                    
                    // Null checks
                    if (_meshFilter == null || _meshFilter.sharedMesh == null)
                        continue;

                    if (_meshFilter.sharedMesh.vertexCount == 0)
                        continue;

                    // Store mesh reference
                    Mesh combinedMesh = _meshFilter.sharedMesh;

                    switch (_colliderType)
                    {
                        case Configuration.ColliderType.meshCollider:
                            var _meshCollider = _cluster.AddComponent<MeshCollider>();
                            _meshCollider.sharedMesh = combinedMesh;
                            _meshCollider.cookingOptions = MeshColliderCookingOptions.None;
                            break;
                            
                        case Configuration.ColliderType.tileCollider:
                            var _layer = configuration.GetBlueprintLayerByGuid(assignedBlueprintLayerGuid);
                            if (_layer != null)
                            {
                                HashSet<Vector2> _allPositions = new HashSet<Vector2>();
                                _layer.GetAllCellPositions(_allPositions);

                                var _clusterPositions = _layer.GetPositionsInCluster(modifiedClusters[i]);
                                var _collisionMesh = GridMeshGenerator.GenerateMesh(_clusterPositions, _allPositions, configuration.cellSize, _layer.defaultLayerHeight + layerYOffset + _tileColliderHeight, _tileColliderExtrusion, _invertWalls);
                                
                                if (_collisionMesh != null && _collisionMesh.vertexCount > 0)
                                {
                                    var _meshCollider2 = _cluster.AddComponent<MeshCollider>();
                                    _meshCollider2.cookingOptions = MeshColliderCookingOptions.None;
                                    _meshCollider2.sharedMesh = _collisionMesh;
                                }
                            }
                            break;
                    }

                    var _meshRenderer = _cluster.GetComponent<MeshRenderer>();
                    if (_meshRenderer != null)
                    {
                        _meshRenderer.shadowCastingMode = _shadowCastingMode;
                        if (_renderingLayer == 0)
                        {
                            int _layerIndex = GetRenderingLayerIndex("Default");
                            _renderingLayer = (uint)(1 << _layerIndex);
                        }
                        _meshRenderer.renderingLayerMask = _renderingLayer;
                    }
                    
                    _cluster.gameObject.layer = _objectLayer.value;
                }
            }
        }

        // Merge afterwards
        // public void MergeClusters()
        // {
        //     
        //     // Apply settings with mesh generation override
        //     var _mergeTiles = meshGenerationOverride ? mergeTiles : configuration.mergeTiles;
        //     var _colliderType = meshGenerationOverride ? colliderType : configuration.colliderType;
        //     var _shadowCastingMode = meshGenerationOverride ? shadowCastingMode : configuration.shadowCastingMode;
        //     var _objectLayer = meshGenerationOverride ? objectLayer : configuration.objectLayer;
        //     var _renderingLayer = meshGenerationOverride ? renderingLayer : configuration.renderingLayer;
        //     var _tileColliderExtrusion = meshGenerationOverride ? tileColliderExtrusionHeight : configuration.tileColliderExtrusionHeight;
        //     var _tileColliderHeight = meshGenerationOverride ? tileColliderHeight : configuration.tileColliderHeight;
        //     var _invertWalls = meshGenerationOverride ? invertCollisionWalls : configuration.invertCollisionWalls;
        //
        //     if (_mergeTiles)
        //     {
        //         for (int i = 0; i < modifiedClusters.Count; i++)
        //         {
        //             var _cluster = FindCluster(modifiedClusters[i]);
        //
        //             MeshCombiner _combiner;
        //             if (!_cluster.TryGetComponent<MeshCombiner>(out _combiner))
        //             {
        //                 _combiner = _cluster.AddComponent<MeshCombiner>();
        //             }
        //
        //             _combiner.CreateMultiMaterialMesh = true;
        //             _combiner.DestroyCombinedChildren = true;
        //
        //             _combiner.CombineMeshes(false);
        //
        //             var _meshFilter = _cluster.GetComponent<MeshFilter>();
        //             var _meshRenderer = _cluster.GetComponent<MeshRenderer>();
        //             
        //             if (_meshFilter != null && _meshFilter.sharedMesh != null && _meshFilter.sharedMesh.vertexCount > 0)
        //             {
        //                 switch (_colliderType)
        //                 {
        //                     case Configuration.ColliderType.meshCollider:
        //                         var _meshCollider = _cluster.AddComponent<MeshCollider>();
        //                         _meshCollider.sharedMesh = _meshFilter.sharedMesh;
        //                         _meshCollider.cookingOptions = MeshColliderCookingOptions.None;
        //                         break;
        //                     case Configuration.ColliderType.tileCollider:
        //                         var _layer = configuration.GetBlueprintLayerByGuid(assignedBlueprintLayerGuid);
        //                         HashSet<Vector2> _allPositions = new HashSet<Vector2>();
        //                         _layer.GetAllCellPositions(_allPositions);
        //                 
        //                         var _clusterPositions = _layer.GetPositionsInCluster(modifiedClusters[i]);
        //                         var _collisionMesh = GridMeshGenerator.GenerateMesh(_clusterPositions, _allPositions, configuration.cellSize, _layer.defaultLayerHeight + layerYOffset + _tileColliderHeight, _tileColliderExtrusion, _invertWalls);
        //                         if (_collisionMesh != null && _collisionMesh.vertexCount > 0)
        //                         {
        //                             var _meshCollider2 = _cluster.AddComponent<MeshCollider>();
        //                             _meshCollider2.cookingOptions = MeshColliderCookingOptions.None;
        //                             _meshCollider2.sharedMesh = _collisionMesh;
        //                         }
        //                         break;
        //                 }
        //             
        //                 if (_meshRenderer != null)
        //                 {
        //                     _meshRenderer.shadowCastingMode = _shadowCastingMode;
        //                     if (_renderingLayer == 0)
        //                     {
        //                         int _layerIndex = GetRenderingLayerIndex("Default");
        //                         _renderingLayer = (uint)(1 << _layerIndex);
        //                     }
        //                     _meshRenderer.renderingLayerMask = _renderingLayer;
        //                 }
        //                 _cluster.gameObject.layer = _objectLayer.value;
        //             }
        //         }
        //     }
        // }
        //
        

        bool isMasked(Vector2 _position)
        {
            // loop through all assigned masks and check if the position is masked
            HashSet<Vector2> _positions = new HashSet<Vector2>();
            for (int i = 0; i < masks.Count; i++)
            {
                _positions.Clear();
                var _layer = configuration.GetBlueprintLayerByGuid(masks[i].assignedBlueprintLayerGuid);
                if (_layer != null)
                {
                    _positions = _layer.GetAllCellPositions(_positions);
                    foreach (var x in _positions)
                    {
                        if (x == _position)
                        {
                            // if (layerName == "2")
                            // {
                            //     Debug.Log("is masked");
                            // }

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        bool isNeighbourMasked(TileData _tileData)
        {
            for (int i = 0; i < masks.Count; i++)
            {
                var _layer = configuration.GetBlueprintLayerByGuid(masks[i].assignedBlueprintLayerGuid);

                // Priority exception
                bool _hasException = false;
                for (int j = 0; j < masks[i].priorityExceptions.Count; j++)
                {
                    if (_tileData.tileType == masks[i].priorityExceptions[j].tileType)
                    {
                        _hasException = true;
                    }
                }

                var _exception = _hasException ? false : masks[i].isPriority;

                if (_layer != null)
                {
                    if (_exception)
                    {
                        for (float x = -0.5f; x < 1f; x += 0.5f)
                        {
                            for (float y = -0.5f; y < 1f; y += 0.5f)
                            {
                                var _pos = new Vector2(_tileData.tilePosition.x + (x), _tileData.tilePosition.y + (y));
                                if (_layer.allPositions.TryGetValue(_pos, out var _value))
                                {
                                    // if (_layer.worldGrid.TryGetValue(_hashMapKey, out var _value))
                                    // {
                                    //     if (_layer.worldGrid[_hashMapKey].TryGetValue(_pos, out var _value2))
                                    //     {

                                    // var _tile = worldGrid[_hashMapKey][_pos];

                                    // if (_tile)
                                    // {
                                    return true;
                                    // }
                                    // }
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }


        public TileData RefreshPathCells2(TileData _tileData, out int configuration)
        {

            configuration = 0;

            // Top Left
            tmpFourTileNeighbours[0] = false;
            // Top Right
            tmpFourTileNeighbours[1] = false;
            // Bottom Left
            tmpFourTileNeighbours[2] = false;
            // Bottom Right
            tmpFourTileNeighbours[3] = false;


            tmpEightTileNeighbours[0] = false;
            tmpEightTileNeighbours[1] = false;
            tmpEightTileNeighbours[2] = false;
            tmpEightTileNeighbours[3] = false;
            tmpEightTileNeighbours[4] = true; // center
            tmpEightTileNeighbours[5] = false;
            tmpEightTileNeighbours[6] = false;
            tmpEightTileNeighbours[7] = false;
            tmpEightTileNeighbours[8] = false;

            var _offset = 0.5f;
            if (!useDualGrid)
            {
                _offset = 1f;
            }

            for (float x = -_offset; x < _offset + _offset; x += _offset)
            {
                for (float y = -_offset; y < _offset + _offset; y += _offset)
                {
                    var _pos = new Vector2(_tileData.tilePosition.x + (x), _tileData.tilePosition.y + (y));

                    var _hashMapKey = GetClusterKeyFromPosition(_pos);
                    if (worldGrid.TryGetValue(_hashMapKey, out var _value))
                    {
                        if (worldGrid[_hashMapKey].TryGetValue(_pos, out var _value2))
                        {
                            var _tile = worldGrid[_hashMapKey][_pos];
                            if (_tile)
                            {
                                if (useDualGrid)
                                {
                                    if ((x == -0.5f && y == -0.5f))
                                    {
                                        // _neighbourCount ++;
                                        tmpFourTileNeighbours[2] = true;
                                    }
                                    if ((x == -0.5f && y == 0.5f))
                                    {
                                        // _neighbourCount ++;
                                        tmpFourTileNeighbours[0] = true;
                                    }
                                    if ((x == 0.5f && y == -0.5f))
                                    {
                                        // _neighbourCount++;
                                        tmpFourTileNeighbours[3] = true;
                                    }
                                    if ((x == 0.5f && y == 0.5f))
                                    {
                                        // _neighbourCount ++;
                                        tmpFourTileNeighbours[1] = true;
                                    }
                                }
                                else
                                {
                                    if ((x == -1f && y == 1f))
                                    {
                                        // _neighbourCount ++;
                                        tmpEightTileNeighbours[0] = true;
                                    }
                                    if ((x == 0f && y == 1f))
                                    {
                                        tmpEightTileNeighbours[1] = true;
                                    }
                                    if ((x == 1f && y == 1f))
                                    {
                                        // _neighbourCount ++;
                                        tmpEightTileNeighbours[2] = true;
                                    }
                                    if (x == -1f && y == 0f)
                                    {
                                        tmpEightTileNeighbours[3] = true;
                                    }
                                    if (x == 1f && y == 0f)
                                    {
                                        tmpEightTileNeighbours[5] = true;
                                    }
                                    if ((x == -1f && y == -1f))
                                    {
                                        // _neighbourCount ++;
                                        tmpEightTileNeighbours[6] = true;
                                    }
                                    if (x == 0f && y == -1f)
                                    {
                                        tmpEightTileNeighbours[7] = true;
                                    }
                                    if ((x == 1f && y == -1f))
                                    {
                                        // _neighbourCount++;
                                        tmpEightTileNeighbours[8] = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }


            if (useDualGrid)
            {
                var _configuration = GetConfigurationBitmask(tmpFourTileNeighbours);
                configuration = _configuration;

                _tileData.configuration = _configuration;

                if (!tmpFourTileNeighbours[0] && tmpFourTileNeighbours[1] && tmpFourTileNeighbours[2] && tmpFourTileNeighbours[3])
                // if (!_topLeft && _topRight && _bottomLeft && _bottomRight)
                {
                    _tileData.yRotation = 0;
                }

                if (tmpFourTileNeighbours[0] && !tmpFourTileNeighbours[1] && tmpFourTileNeighbours[2] && tmpFourTileNeighbours[3])
                // if (_topLeft && !_topRight && _bottomLeft && _bottomRight)
                {
                    _tileData.yRotation = 90;
                }

                if (tmpFourTileNeighbours[0] && tmpFourTileNeighbours[1] && !tmpFourTileNeighbours[2] && !tmpFourTileNeighbours[3])
                // if (_topLeft && _topRight && _bottomLeft && !_bottomRight)
                {
                    _tileData.yRotation = 180;
                }

                // if (tmpFourTileNeighbours[0] && !tmpFourTileNeighbours[1] && tmpFourTileNeighbours[2] && tmpFourTileNeighbours[3])
                // // if (_topLeft && !_topRight && _bottomLeft && _bottomRight)
                // {
                //     _tileData.yRotation = 180;
                // }

                if (tmpFourTileNeighbours[0] && tmpFourTileNeighbours[1] && !tmpFourTileNeighbours[2] && tmpFourTileNeighbours[3])
                // if (_topLeft && _topRight && !_bottomLeft && _bottomRight)
                {
                    _tileData.yRotation = 270;
                }

                // if (!tmpFourTileNeighbours[0] && tmpFourTileNeighbours[1] && tmpFourTileNeighbours[2] && tmpFourTileNeighbours[3])
                // // if (!_topLeft && _topRight && _bottomLeft && _bottomRight)
                // {
                //     _tileData.yRotation = 270;
                // }
            }
            else
            {
                var _configuration = GetConfigurationBitmask(tmpEightTileNeighbours);
                configuration = _configuration;

                _tileData.configuration = _configuration;
            }

            _tileData.tileType = GetTileType(_tileData.configuration, _tileData, out _tileData.yRotation);

            var _hashMapKey2 = GetClusterKeyFromPosition(_tileData.tilePosition);
            if (tileGrid.TryGetValue(_hashMapKey2, out var _value3))
            {
                if (tileGrid[_hashMapKey2].TryGetValue(_tileData.tilePosition, out var _value4))
                {
                    tileGrid[_hashMapKey2][_tileData.tilePosition] = _tileData;
                }
            }

            return _tileData;
        }

        TilePreset GetRandomTilePreset(TileData _tileData, int _tileLayerIndex, ref Unity.Mathematics.Random _rng)
        {
            // First check if there's an override for given tile position
            for (var t = 0; t < tileLayers[_tileLayerIndex].layerOverrides.Count; t++)
            {
                var _layer = configuration.GetBlueprintLayerByGuid(tileLayers[_tileLayerIndex].layerOverrides[t].blueprintOverrideLayer);
                if (_layer != null)
                {
                    var _layerPositions = new HashSet<Vector2>();
                    _layerPositions = _layer.GetAllCellPositions(_layerPositions);

                    var _count = 0;
                    if (useDualGrid)
                    {
                        for (int x = -1; x <= 1; x++)
                        {
                            for (int y = -1; y <= 1; y++)
                            {
                                // Get all possible cell positions
                                var _pos = new Vector2(_tileData.tilePosition.x + (x * 0.5f), _tileData.tilePosition.y + (y * 0.5f));
                                if (_layerPositions.Contains(_pos))
                                {
                                    _count++;
                                }
                            }
                        }
                        
                       
                        // Somehow requiredNeighbourCount is set to 0 by default. We need to check this.
                        if (tileLayers[_tileLayerIndex].layerOverrides[t].requiredNeighbourCount == 0)
                        {
                            tileLayers[_tileLayerIndex].layerOverrides[t].requiredNeighbourCount = 1;
                        }
                       
                        if (_count >= tileLayers[_tileLayerIndex].layerOverrides[t].requiredNeighbourCount)
                        {
                            return tileLayers[_tileLayerIndex].layerOverrides[t].preset;
                        }
                    }
                    else
                    {

                        if (_layerPositions.Contains(_tileData.tilePosition))
                        {
                            return tileLayers[_tileLayerIndex].layerOverrides[t].preset;
                        }
                    }
                }

            }

            // default is top preset
            List<TilePresetSelection> _tilePreset = tilePresetsTop;

            if (_tileLayerIndex == 0)
            {
                if (tileLayers.Count == 1)
                {
                    _tilePreset = tilePresetsTop;
                }
                else if (tilePresetsBottom != null && tilePresetsBottom.Count > 0)
                {
                    _tilePreset = tilePresetsBottom;
                }
                else if (tilePresetsMiddle != null && tilePresetsMiddle.Count > 0)
                {
                    _tilePreset = tilePresetsMiddle;
                }

            }
            else if (_tileLayerIndex == tileLayers.Count - 1 && tilePresetsTop != null && tilePresetsTop.Count > 0)
            {
                // use top preset
                _tilePreset = tilePresetsTop;
            }
            else if (_tileLayerIndex > 0 && tilePresetsMiddle != null && tilePresetsMiddle.Count > 0)
            {
                _tilePreset = tilePresetsMiddle;
            }

            // Get random preset based on their weight value
            var _totalWeight = _tilePreset.Select(x => x.weight).Sum();

            if (_tilePreset.Count == 0 || _tilePreset == null)
            {
                return null;
            }

            return _tilePreset[RandomWeighted(_tilePreset, _totalWeight, ref _rng)].preset;
        }

        int RandomWeighted(List<TilePresetSelection> _tilePreset, float _total, ref Unity.Mathematics.Random _rng)
        {
            int result = 0;
            float total = 0;
            float randVal = _rng.NextFloat(min: 0f, max: _total);
            for (result = 0; result < _tilePreset.Count; result++)
            {
                total += _tilePreset[result].weight;
                if (total > randVal) break;
            }

            if (result >= _tilePreset.Count)
            {
                result = _tilePreset.Count - 1;
            }

            return result;
        }

        void InstantiateTile(TileData _tileData, int _clusterKey, int _tileLayerIndex)
        {
            var _cluster = FindCluster(_clusterKey);

            uint _seed;

            int x = Mathf.FloorToInt(_tileData.tilePosition.x * 1000f);
            int y = Mathf.FloorToInt(_tileData.tilePosition.y * 1000f);
            
          
            // Combine position with global- or current random seed using hash
            uint hash = Unity.Mathematics.math.hash(new Unity.Mathematics.int3(
                x,
                y,
                configuration.useGlobalRandomSeed ? configuration.globalRandomSeed : (int)configuration.currentRandomSeed
            ));

            _seed = hash;
            if (_seed == 0) _seed = 1;

            
            Unity.Mathematics.Random _tileRandom = new Unity.Mathematics.Random(_seed);

            // Get prefab from assigned preset
            var _tilePreset = GetRandomTilePreset(_tileData, _tileLayerIndex, ref _tileRandom);
          
            
            GameObject _prefab = null;
            Material _materialOverride = null;
            if (_tilePreset != null)
            {
                _prefab = _tilePreset.GetTile(_tileData.tileType, out _tileData.yRotationOffset);
                _materialOverride = _tilePreset.GetMaterialOverride();
            }
            if (_prefab != null)
            {

                var _newTile = GameObject.Instantiate(_prefab, Vector3.zero, Quaternion.Euler(new Vector3(0, _tileData.yRotation + _tileData.yRotationOffset, 0)));
                // new Vector3((_tileData.tilePosition.x) * configuration.cellSize + owner.transform.position.x, blueprintLayer.defaultLayerHeight + layerYOffset + (tileLayers[_tileLayerIndex].heightOffset) + owner.transform.position.y, (_tileData.tilePosition.y) * configuration.cellSize + owner.transform.position.z), Quaternion.Euler(new Vector3(0, _tileData.yRotation + _tileData.yRotationOffset, 0)));

                if (_materialOverride != null)
                {
                    _newTile.GetComponent<MeshRenderer>().material = _materialOverride;
                }


                if (scaleTileToCellSize)
                {
                    _newTile.transform.localScale = new Vector3(_newTile.transform.localScale.x * scaleOffset.x * configuration.cellSize, _newTile.transform.localScale.y * scaleOffset.y * configuration.cellSize, _newTile.transform.localScale.z * scaleOffset.z * configuration.cellSize);
                }
                else
                {
                    _newTile.transform.localScale = new Vector3(_newTile.transform.localScale.x * scaleOffset.x, _newTile.transform.localScale.y * scaleOffset.y, _newTile.transform.localScale.z * scaleOffset.z);
                }

                if (!useDualGrid)
                {
                    if (TileConfigurations.NRMGRD_minusXScale_configurations.Contains(_tileData.configuration))
                    {
                        _newTile.transform.localScale = new Vector3(_newTile.transform.localScale.x * -1, _newTile.transform.localScale.y, _newTile.transform.localScale.z);
                    }
                }
                else
                {

                }

                _newTile.transform.SetParent(_cluster.transform, false);
                // Correct position
                var _newTilePosition = new Vector3((_tileData.tilePosition.x) * configuration.cellSize, currentBlueprintLayer.defaultLayerHeight + layerYOffset + (tileLayers[_tileLayerIndex].heightOffset), (_tileData.tilePosition.y) * configuration.cellSize);
                _newTile.transform.localPosition = _newTilePosition;
                
            }


            int _hashMapKey = GetClusterKeyFromPosition(new Vector2(_tileData.tilePosition.x, _tileData.tilePosition.y));
            if (tileGrid.ContainsKey(_hashMapKey))
            {
                if (tileGrid[_hashMapKey].ContainsKey(_tileData.tilePosition))
                {
                    var _td = tileGrid[_hashMapKey][_tileData.tilePosition];
                    tileGrid[_hashMapKey][_tileData.tilePosition] = _td;
                }
            }
        }

        public TileData GetExistingTileData(Vector3 _position)
        {

            int _hashMapKey = GetClusterKeyFromPosition(new Vector2(_position.x, _position.z));
            Vector2 _cellPosition = new Vector2(_position.x, _position.z);


            if (tileGrid.ContainsKey(_hashMapKey))
            {
                if (tileGrid[_hashMapKey].ContainsKey(_cellPosition))
                {
                    var _data = tileGrid[_hashMapKey][_cellPosition];
                    return _data;
                }
            }


            return default;
        }

        GameObject CreateCluster(int _clusterKey)
        {
            var _key = layerName + "_Cluster_" + _clusterKey;
            GameObject _obj = null;

            _obj = new GameObject(_key);

            if (tmpLayerObject == null)
            {
                tmpLayerObject = GetLayerObject(manager.gameObject);
            }

            _obj.transform.SetParent(tmpLayerObject.transform, false);
            _obj.transform.localRotation = Quaternion.identity;

            var _clusterComponent = _obj.AddComponent<ClusterIdentifier>();

            _clusterComponent.clusterID = _clusterKey;
            _clusterComponent.layerGuid = guid;

            if (availableClusters == null)
            {
                availableClusters = new HashSet<ClusterIdentifier>();
            }

            availableClusters.Add(_clusterComponent);

            return _obj;
        }


        public GameObject FindClusterByPosition(Vector2 _position)
        {
            var _clusterKey = GetClusterKeyFromPosition(_position);
            return FindCluster(_clusterKey, false);
        }

        GameObject FindCluster(int _clusterKey, bool _createIfNotFound = true)
        {
            ClusterIdentifier _cluster = null;

            if (availableClusters != null)
            {
                foreach (var _cls in availableClusters)
                {
                    if (_cls.clusterID == _clusterKey)
                    {
                        _cluster = _cls;
                    }
                }
            }


            if (_cluster == null && _createIfNotFound)
            {
                return CreateCluster(_clusterKey);
            }

            return _cluster != null ? _cluster.gameObject : null;
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

        public List<TileData> GetTilesFromWorldPosition(Vector2 _position)
        {
            List<TileData> returnList = new List<TileData>();

            for (int x = -1; x < 1; x++)
            {
                for (int y = -1; y < 1; y++)
                {
                    var _pos = new Vector2(_position.x + (x + 0.5f), _position.y + (y + 0.5f));
                    int _hashMapKey = GetClusterKeyFromPosition(_pos);


                    if (tileGrid.ContainsKey(_hashMapKey))
                    {
                        if (tileGrid[_hashMapKey].ContainsKey(_pos))
                        {
                            var _data = tileGrid[_hashMapKey][_pos];

                            returnList.Add(_data);
                        }
                    }
                }
            }

            return returnList;
        }

        public override TileData GetTileDataFromPosition(Vector2 _position)
        {
            var _relativePosition = _position / configuration.cellSize;
            int _hashMapKey = GetClusterKeyFromPosition(_relativePosition);


            var _offset = 0.5f;


            if (tileGrid.ContainsKey(GetClusterKeyFromPosition(_relativePosition)))
            {
                // Check both possible positions (with and without offset)
                foreach (var _tile in tileGrid[_hashMapKey].Values)
                {
                    if (_tile.tilePosition == _relativePosition)
                    {
                        return _tile;
                    }
                }


                _relativePosition = new Vector2(_relativePosition.x - _offset, _relativePosition.y - _offset);
                foreach (var _tile in tileGrid[_hashMapKey].Values)
                {
                    if (_tile.tilePosition == _relativePosition)
                    {
                        return _tile;
                    }
                }
            }


            return new TileData();
        }

        int GetRenderingLayerIndex(string name)
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline != null)
            {
                string[] layerNames = RenderingLayerMask.GetDefinedRenderingLayerNames();
                for (int i = 0; i < layerNames.Length; i++)
                {
                    if (layerNames[i] == name)
                    {
                        return i;
                    }
                }
            }
            return -1; // Not found
        }


        public void OnBeforeSerialize()
        {
            serializedWorldPositions = new List<Vector2>();
            serializedTiles = new List<TileData>();
            serializedWorldPositions = worldGrid.SelectMany(x => x.Value.Keys).ToList();
            serializedTiles = tileGrid.SelectMany(x => x.Value.Values).ToList();
        }

        public void OnAfterDeserialize()
        {
            worldGrid = new Dictionary<int, Dictionary<Vector2, bool>>();
            tileGrid = new Dictionary<int, Dictionary<Vector2, TileData>>();
            // allPositions = new List<Vector2>();

            serializedWorldPositions = serializedWorldPositions.Distinct().ToList();
            // serializedTiles = serializedTiles.Distinct().ToList();

            HashSet<TileData> tileGridSet = new HashSet<TileData>(serializedTiles);
            serializedTiles = tileGridSet.ToList();

            for (int i = 0; i < serializedWorldPositions.Count; i++)
            {
                var pos = serializedWorldPositions[i];
                var _key = GetClusterKeyFromPosition(pos);
                if (worldGrid.ContainsKey(_key))
                {
                    if (!worldGrid[_key].ContainsKey(pos))
                    {
                        worldGrid[_key].Add(pos, true);
                    }
                }
                else
                {
                    worldGrid.Add(_key, new Dictionary<Vector2, bool>());
                    worldGrid[_key].Add(pos, true);
                }

                // allPositions.Add(pos);
            }

            for (int i = 0; i < serializedTiles.Count; i++)
            {
                var tile = serializedTiles[i];
                var _key = GetClusterKeyFromPosition(tile.tilePosition);
                if (tileGrid.ContainsKey(_key))
                {
                    if (!tileGrid[_key].ContainsKey(tile.tilePosition))
                    {
                        tileGrid[_key].Add(tile.tilePosition, tile);
                    }
                }
                else
                {
                    tileGrid.Add(_key, new Dictionary<Vector2, TileData>());
                    tileGrid[_key].Add(tile.tilePosition, tile);
                }
            }
        }

#if UNITY_EDITOR
        #region Inspector
        public override VisualElement CreateInspectorGUI(Configuration _asset, Editor _editor, LayerFoldoutElement _foldout)
        {
            var _layerItem = new VisualElement();

            BuildLayerUI(_asset, _editor, _foldout, _layerItem);

            return _layerItem;
        }
        
        void BuildLayerUI(Configuration _asset, Editor _editor, LayerFoldoutElement _foldout, VisualElement _layerItem)
        {
            _layerItem.Clear();
            var _assetEditor = _editor as ConfigurationEditor;
            var _layerSerializedObject = new SerializedObject(this);

            _layerItem.style.flexGrow = 1;


            var _textField = new TextField();
            _textField.label = "Layer Name";
            _textField.BindProperty(_layerSerializedObject.FindProperty("layerName"));
            _textField.RegisterValueChangedCallback((evt) =>
            {
                _foldout.SetLabel(evt.newValue);
            });


            var _blueprintLayer = new LayerSelectDropdownElement(_asset, assignedBlueprintLayerGuid, SelectedLayer, "Blueprint Layer");

            var _useDualGrid = new PropertyField();
            _useDualGrid.BindProperty(_layerSerializedObject.FindProperty("useDualGrid"));

            var _useMultiLayers = new PropertyField();
            _useMultiLayers.label = "Use Multiple Height Layers";
            _useMultiLayers.BindProperty(_layerSerializedObject.FindProperty("useMultiLayers"));
            _useMultiLayers.RegisterCallback<ChangeEvent<bool>>((evt) =>
            {
                if (useMultiLayers != evt.newValue)
                {
                    if (!evt.newValue)
                    {
                        tileLayers = new List<TileLayers>();
                        tileLayers.Add(new TileLayers());
                    }
                    if (evt.newValue)
                    {
                        if (tileLayers.Count == 0)
                        {
                            tileLayers.Add(new TileLayers());
                        }
                    }
                }
            });

            var _scaleTileToCellSize = new PropertyField();
            _scaleTileToCellSize.label = "Scale Tiles To Cell Size";
            _scaleTileToCellSize.BindProperty(_layerSerializedObject.FindProperty("scaleTileToCellSize"));

            var _tilePresetsTopListView = new ListView();

            _tilePresetsTopListView.itemsSource = tilePresetsTop;
            _tilePresetsTopListView.makeItem = () => { return new TilePresetsListItem(this); };
            _tilePresetsTopListView.bindItem = (element, i) =>
            {
                (element as TilePresetsListItem).Bind(i, "tilePresetsTop");
            };

            _tilePresetsTopListView.reorderable = true;
            _tilePresetsTopListView.reorderMode = ListViewReorderMode.Animated;
            _tilePresetsTopListView.showBorder = true;
            _tilePresetsTopListView.showAddRemoveFooter = true;
            _tilePresetsTopListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

            var _tilePresetsMiddleListView = new ListView();

            _tilePresetsMiddleListView.itemsSource = tilePresetsMiddle;
            _tilePresetsMiddleListView.makeItem = () => { return new TilePresetsListItem(this); };
            _tilePresetsMiddleListView.bindItem = (element, i) =>
            {
                (element as TilePresetsListItem).Bind(i, "tilePresetsMiddle");
            };

            _tilePresetsMiddleListView.reorderable = true;
            _tilePresetsMiddleListView.reorderMode = ListViewReorderMode.Animated;
            _tilePresetsMiddleListView.showBorder = true;
            _tilePresetsMiddleListView.showAddRemoveFooter = true;
            _tilePresetsMiddleListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;


            var _tilePresetsBottomListView = new ListView();

            _tilePresetsBottomListView.itemsSource = tilePresetsBottom;
            _tilePresetsBottomListView.makeItem = () => { return new TilePresetsListItem(this); };
            _tilePresetsBottomListView.bindItem = (element, i) =>
            {
                (element as TilePresetsListItem).Bind(i, "tilePresetsBottom");
            };

            _tilePresetsBottomListView.reorderable = true;
            _tilePresetsBottomListView.reorderMode = ListViewReorderMode.Animated;
            _tilePresetsBottomListView.showBorder = true;
            _tilePresetsBottomListView.showAddRemoveFooter = true;
            _tilePresetsBottomListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;


            var _tileLayersListView = new ListView();

            _tileLayersListView.itemsSource = tileLayers;
            _tileLayersListView.makeItem = () => { return new TileLayersListItem(this); };
            _tileLayersListView.bindItem = (element, i) =>
            {
                (element as TileLayersListItem).Bind(i);
            };

            _tileLayersListView.reorderable = true;
            _tileLayersListView.reorderMode = ListViewReorderMode.Animated;
            _tileLayersListView.showBorder = true;
            _tileLayersListView.showAddRemoveFooter = true;
            _tileLayersListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;


            var _tLayers = _layerSerializedObject.FindProperty("tileLayers");

            if (_tLayers.arraySize == 0)
            {
                _tLayers.InsertArrayElementAtIndex(0);
            }

            var _tileOverridesListView = new ListView();
            if (this.tileLayers.Count == 0)
            {
                this.tileLayers.Add(new TileLayers());
            }
            // _tileOverridesListView.itemsSource = this.tileLayers[0].layerOverrides;
            _tileOverridesListView.makeItem = () =>
            {
                return new TileOverridesListItem(this);
            };
            _tileOverridesListView.bindItem = (element, i) =>
            {
                (element as TileOverridesListItem).Bind(0, i);
            };
            // _tileOverridesListView.itemsAdded += (obj) =>
            // {
            //     // this.tileLayers[0].layerOverrides.Add(new TilePresetOverride());

            //     _tileOverridesListView.Rebuild();
            // };

            _tileOverridesListView.reorderable = true;
            _tileOverridesListView.reorderMode = ListViewReorderMode.Animated;
            _tileOverridesListView.showBorder = true;
            _tileOverridesListView.showAddRemoveFooter = true;
            _tileOverridesListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
            var _layerSO = new SerializedObject(this);
            var _listProperty = _layerSO.FindProperty("tileLayers").GetArrayElementAtIndex(0).FindPropertyRelative("layerOverrides");
            _tileOverridesListView.BindProperty(_listProperty);

            var _presetTagTop = new Label();
            _presetTagTop.SetRadius(5, 5, 5, 5);
            _presetTagTop.SetPadding(2, 2, 2, 2);
            _presetTagTop.SetMargin(0, 4, 0, 0);
            _presetTagTop.style.fontSize = 12;
            _presetTagTop.SetBorder(2, TileWorldCreatorColor.Blue.GetColor());
            _presetTagTop.text = "Preset Top";

            var _presetTagMiddle = new Label();
            _presetTagMiddle.SetRadius(5, 5, 5, 5);
            _presetTagMiddle.SetPadding(2, 2, 2, 2);
            _presetTagMiddle.SetMargin(0, 4, 0, 0);
            _presetTagMiddle.style.fontSize = 12;
            _presetTagMiddle.SetBorder(2, TileWorldCreatorColor.PaleBlue.GetColor());
            _presetTagMiddle.text = "Preset Middle";

            var _presetTagBottom = new Label();
            _presetTagBottom.SetRadius(5, 5, 5, 5);
            _presetTagBottom.SetPadding(2, 2, 2, 2);
            _presetTagBottom.SetMargin(0, 4, 0, 0);
            _presetTagBottom.SetBorder(2, TileWorldCreatorColor.DarkBlue.GetColor());
            _presetTagBottom.style.fontSize = 12;
            _presetTagBottom.text = "Preset Bottom";

            var _tileLayersTitle = TileWorldCreatorUIElements.Separator("Tile Layers");
            var _tileOverridesTitle = TileWorldCreatorUIElements.Separator("Tile Overrides");

            _useMultiLayers.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                _tileLayersTitle.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                _tileOverridesTitle.style.display = evt.newValue ? DisplayStyle.None : DisplayStyle.Flex;
                _presetTagTop.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                _presetTagMiddle.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                _presetTagBottom.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                _tilePresetsMiddleListView.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                _tilePresetsBottomListView.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                _tileLayersListView.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                _tileOverridesListView.style.display = evt.newValue ? DisplayStyle.None : DisplayStyle.Flex;
            });

            #region MESH
            var _shadowCastingMode = new PropertyField();
            _shadowCastingMode.BindProperty(_layerSerializedObject.FindProperty("shadowCastingMode"));

            var _objectLayer = new LayerField();
            _objectLayer.label = "Object Layer";
            _objectLayer.BindProperty(_layerSerializedObject.FindProperty("objectLayer"));

#if !UNITY_6000_0_OR_NEWER

            var _renderingLayer = new RenderingLayerMaskField("Rendering LayerMask", defaultMask: renderingLayer.value);
            _renderingLayer.RegisterValueChangedCallback(evt =>
            {
                _layerSerializedObject.Update();
                renderingLayer.value = _renderingLayer.value;
                _layerSerializedObject.ApplyModifiedProperties();
            });
#else

            var _renderingLayer = new RenderingLayerMaskField();
            _renderingLayer.label = "Rendering LayerMask";
            _renderingLayer.BindProperty(_layerSerializedObject.FindProperty("renderingLayer"));

#endif

            var _collider = new PropertyField();
            _collider.BindProperty(_layerSerializedObject.FindProperty("colliderType"));

            var _tileColliderHeight = new PropertyField();
            _tileColliderHeight.BindProperty(_layerSerializedObject.FindProperty("tileColliderHeight"));
            var _tileColliderExtrusion = new PropertyField();
            _tileColliderExtrusion.BindProperty(_layerSerializedObject.FindProperty("tileColliderExtrusionHeight"));
            var _invertWalls = new PropertyField();
            _invertWalls.BindProperty(_layerSerializedObject.FindProperty("invertCollisionWalls"));

            var _mergeTiles = new Toggle();
            _mergeTiles.label = "Merge Tiles";
            _mergeTiles.BindProperty(_layerSerializedObject.FindProperty("mergeTiles"));
            _mergeTiles.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                _shadowCastingMode.SetEnabled(evt.newValue);
                _objectLayer.SetEnabled(evt.newValue);
                _renderingLayer.SetEnabled(evt.newValue);
                _collider.SetEnabled(evt.newValue);
                _tileColliderHeight.SetEnabled(evt.newValue);
                _tileColliderExtrusion.SetEnabled(evt.newValue);
                _invertWalls.SetEnabled(evt.newValue);
            });


            var _meshGenerationOverride = new Toggle();
            _meshGenerationOverride.label = "Mesh Generation Override";
            _meshGenerationOverride.tooltip = "Use this to override global mesh generation for this layer";
            _meshGenerationOverride.BindProperty(_layerSerializedObject.FindProperty("meshGenerationOverride"));
            _meshGenerationOverride.RegisterCallback<ChangeEvent<bool>>(evt =>
            {
                if (evt.newValue)
                {
                    _mergeTiles.SetEnabled(evt.newValue);

                    _shadowCastingMode.SetEnabled(_mergeTiles.value);
                    _objectLayer.SetEnabled(_mergeTiles.value);
                    _renderingLayer.SetEnabled(_mergeTiles.value);
                    _collider.SetEnabled(_mergeTiles.value);
                    _tileColliderHeight.SetEnabled(_mergeTiles.value);
                    _tileColliderExtrusion.SetEnabled(_mergeTiles.value);

                }
                else
                {
                    _mergeTiles.SetEnabled(evt.newValue);
                    _shadowCastingMode.SetEnabled(evt.newValue);
                    _objectLayer.SetEnabled(evt.newValue);
                    _renderingLayer.SetEnabled(evt.newValue);
                    _collider.SetEnabled(evt.newValue);
                    _tileColliderHeight.SetEnabled(evt.newValue);
                    _tileColliderExtrusion.SetEnabled(evt.newValue);
                }

            });


            _collider.RegisterCallback<ChangeEvent<string>>(evt =>
           {
               if (colliderType == Configuration.ColliderType.tileCollider)
               {
                   _tileColliderHeight.SetEnabled(true);
                   _tileColliderExtrusion.SetEnabled(true);
                   _invertWalls.SetEnabled(true);
               }
               else
               {
                   _tileColliderHeight.SetEnabled(false);
                   _tileColliderExtrusion.SetEnabled(false);
                   _invertWalls.SetEnabled(false);
               }
           });

            #endregion

            #region OFFSETS
            var _layerOffset = new PropertyField();
            _layerOffset.BindProperty(_layerSerializedObject.FindProperty("layerYOffset"));

            var _scaleOffset = new PropertyField();
            _scaleOffset.BindProperty(_layerSerializedObject.FindProperty("scaleOffset"));

            #endregion

            _layerItem.Add(TileWorldCreatorUIElements.Separator("General"));
            _layerItem.Add(_textField);
            _layerItem.Add(_blueprintLayer);

            // var _selectBlueprint = new DropdownField();
            // // Add all selected layers of assignedBlueprintLayerGuids to text of button
            // _selectBlueprint.label = "Blueprint Layers";
            // _selectBlueprint.value = string.Join(", ", assignedBlueprintLayerNames);
            // _selectBlueprint.RegisterCallback<ClickEvent>((x) =>
            // {

            //     BlueprintLayerPopupSelector.ShowPanel(Event.current.mousePosition, configuration, ref assignedBlueprintLayerGuids, ref assignedBlueprintLayerNames, new BlueprintLayerPopupSelector(), () => _selectBlueprint.value = string.Join(", ", assignedBlueprintLayerNames));
                
            // });

            // _layerItem.Add(_selectBlueprint);
           
          
            _layerItem.Add(_useDualGrid);
            _layerItem.Add(_useMultiLayers);
            _layerItem.Add(_scaleTileToCellSize);
            _layerItem.Add(TileWorldCreatorUIElements.Separator("Tile Presets"));
            var _presetHelpBox = new HelpBox();
            _presetHelpBox.text = "When using multiple presets, TWC will randomly select between them based on their weight.";
            _presetHelpBox.messageType = HelpBoxMessageType.Info;
            _layerItem.Add(_presetHelpBox);
            _layerItem.Add(_presetTagTop);
            _layerItem.Add(_tilePresetsTopListView);
            _layerItem.Add(_presetTagMiddle);
            _layerItem.Add(_tilePresetsMiddleListView);
            _layerItem.Add(_presetTagBottom);
            _layerItem.Add(_tilePresetsBottomListView);
            // Tile layers
            _layerItem.Add(_tileLayersTitle);
            _layerItem.Add(_tileLayersListView);
            // Tile Override (single)
            _layerItem.Add(_tileOverridesTitle);
            _layerItem.Add(_tileOverridesListView);
            _layerItem.Add(TileWorldCreatorUIElements.Separator("Mesh Generation"));
            _layerItem.Add(_meshGenerationOverride);
            _layerItem.Add(_mergeTiles);
            _layerItem.Add(_shadowCastingMode);
            _layerItem.Add(_objectLayer);
            _layerItem.Add(_renderingLayer);
            _layerItem.Add(_collider);
            _layerItem.Add(_tileColliderHeight);
            _layerItem.Add(_tileColliderExtrusion);
            _layerItem.Add(_invertWalls);
            _layerItem.Add(TileWorldCreatorUIElements.Separator("Global Offsets"));
            _layerItem.Add(_layerOffset);
            _layerItem.Add(_scaleOffset);

        }

        void SelectedLayer(string _layerName, string _layerGuid)
        {
            assignedBlueprintLayerGuid = _layerGuid;
        }

#endregion
#endif
    }

#if UNITY_EDITOR
    public class TileLayersListItem : VisualElement
    {
        VisualElement root;
        TilesBuildLayer tilesBuildLayer;
        SerializedObject layerSerializedObject;

        public TileLayersListItem(TilesBuildLayer _buildLayer)
        {
            tilesBuildLayer = _buildLayer;
            layerSerializedObject = new SerializedObject(_buildLayer);

            root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.FlexStart;
            root.style.alignContent = Align.Center;

            this.Add(root);
        }

        public void Bind(int _index)
        {
            root.Clear();

            var _layerTag = new Label();
            _layerTag.SetRadius(5, 5, 5, 5);
            
           
            _layerTag.SetPadding(2,2,2,2);
            _layerTag.style.fontSize = 10;
            
            if (_index == tilesBuildLayer.tileLayers.Count - 1)
            {
                 _layerTag.text = "Preset Top";
                 _layerTag.SetBorder(2, TileWorldCreatorColor.Blue.GetColor());
            }
            else if (_index == 0)
            {
                 _layerTag.text = "Preset Bottom";
                 _layerTag.SetBorder(2, TileWorldCreatorColor.DarkBlue.GetColor());
            }
            else
            {
                 _layerTag.text = "Preset Middle";
                 _layerTag.SetBorder(2, TileWorldCreatorColor.PaleBlue.GetColor());
            }

            var _foldout = new Foldout();
            _foldout.style.marginTop = 1;
            _foldout.style.paddingRight = 5;
            _foldout.style.flexGrow = 1;

            _foldout.text = "Layer " + (_index + 1).ToString();
            
            var _heightOffset = new PropertyField();
            _heightOffset.BindProperty(layerSerializedObject.FindProperty("tileLayers").GetArrayElementAtIndex(_index).FindPropertyRelative("heightOffset"));

            var _ignoreFillTiles = new PropertyField();
            _ignoreFillTiles.BindProperty(layerSerializedObject.FindProperty("tileLayers").GetArrayElementAtIndex(_index).FindPropertyRelative("ignoreFillTiles"));
            
            // var _layerOverrides = new PropertyField();
            // _layerOverrides.BindProperty(layerSerializedObject.FindProperty("tileLayers").GetArrayElementAtIndex(_index).FindPropertyRelative("layerOverrides"));
            
            var _layerOverrides = new ListView();

            if (tilesBuildLayer.tileLayers == null)
            {
                tilesBuildLayer.tileLayers = new List<TilesBuildLayer.TileLayers>();
            }
            if (tilesBuildLayer.tileLayers[_index] == null)
            {
                tilesBuildLayer.tileLayers[_index] = new TilesBuildLayer.TileLayers();
            }
            if (tilesBuildLayer.tileLayers[_index].layerOverrides == null)
            {
                tilesBuildLayer.tileLayers[_index].layerOverrides = new List<TilesBuildLayer.TilePresetOverride>();
            }

            _layerOverrides.itemsSource = tilesBuildLayer.tileLayers[_index].layerOverrides;
            _layerOverrides.makeItem = () => { return new TileOverridesListItem(tilesBuildLayer);};
            _layerOverrides.bindItem = (element, i) =>
            {
                (element as TileOverridesListItem).Bind(_index, i);
            };

            _layerOverrides.reorderable = true;
            _layerOverrides.reorderMode = ListViewReorderMode.Animated;
            _layerOverrides.showBorder = true;
            _layerOverrides.showAddRemoveFooter = true;
            _layerOverrides.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;

            _foldout.Add(_heightOffset);
            _foldout.Add(_ignoreFillTiles);
            _foldout.Add(new Label("Layer Overrides"));
            _foldout.Add(_layerOverrides);


            root.Add(_foldout);
            root.Add(_layerTag);
        }
    }

    public class TileOverridesListItem : VisualElement
    {
        VisualElement root;
        TilesBuildLayer tilesBuildLayer;
        SerializedObject layerSerializedObject;

        public TileOverridesListItem(TilesBuildLayer _buildLayer)
        {
            tilesBuildLayer = _buildLayer;
            layerSerializedObject = new SerializedObject(_buildLayer);

            root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.FlexStart;
            root.style.alignContent = Align.Center;

            this.Add(root);
        }

        public void Bind(int _layerIndex, int _index)
        {
            root.Clear();

            var _foldout = new Foldout();
            _foldout.style.marginTop = 1;
            _foldout.style.paddingRight = 5;
            _foldout.style.flexGrow = 1;

            _foldout.text = "Override " + (_index + 1).ToString();

            var _blueprintOverrideLayer = new PropertyField();
            _blueprintOverrideLayer.BindProperty(layerSerializedObject.FindProperty("tileLayers").GetArrayElementAtIndex(_layerIndex).FindPropertyRelative("layerOverrides").GetArrayElementAtIndex(_index).FindPropertyRelative("blueprintOverrideLayer"));

            var _preset = new PropertyField();
            _preset.BindProperty(layerSerializedObject.FindProperty("tileLayers").GetArrayElementAtIndex(_layerIndex).FindPropertyRelative("layerOverrides").GetArrayElementAtIndex(_index).FindPropertyRelative("preset"));

            _foldout.Add(_blueprintOverrideLayer);
            _foldout.Add(_preset);

            if (tilesBuildLayer.useDualGrid)
            {
                var _requiredNeighbour = new PropertyField();
                _requiredNeighbour.BindProperty(layerSerializedObject.FindProperty("tileLayers").GetArrayElementAtIndex(_layerIndex).FindPropertyRelative("layerOverrides").GetArrayElementAtIndex(_index).FindPropertyRelative("requiredNeighbourCount"));

                _foldout.Add(_requiredNeighbour);
            }

            root.Add(_foldout);
           
        }
    }

    public class TilePresetsListItem : VisualElement
    {
        VisualElement root;
        TilesBuildLayer tilesBuildLayer;
        SerializedObject layerSerializedObject;

        public TilePresetsListItem(TilesBuildLayer _buildLayer)
        {
            tilesBuildLayer = _buildLayer;
            layerSerializedObject = new SerializedObject(_buildLayer);

            root = new VisualElement();
            root.style.flexDirection = FlexDirection.Row;
            root.style.alignItems = Align.FlexStart;
            root.style.alignContent = Align.Center;

            this.Add(root);
        }

        public void Bind(int _index, string _propertyListName)
        {
            root.Clear();

            var _foldout = new Foldout();
            _foldout.style.marginTop = 1;
            _foldout.style.paddingRight = 5;
            _foldout.style.flexGrow = 1;


            var _preset = new PropertyField();
            var _presetProp = layerSerializedObject.FindProperty(_propertyListName).GetArrayElementAtIndex(_index).FindPropertyRelative("preset");
            _preset.BindProperty(_presetProp);

            _foldout.text = _presetProp.objectReferenceValue != null ? _presetProp.objectReferenceValue.name : "Preset " + (_index + 1).ToString();;

            var _weight = new PropertyField();
            _weight.BindProperty(layerSerializedObject.FindProperty(_propertyListName).GetArrayElementAtIndex(_index).FindPropertyRelative("weight"));

          

            _foldout.Add(_preset);
            _foldout.Add(_weight);


            root.Add(_foldout);
        }
    }
    #endif
}