
/*

  _____ _ _    __        __         _     _  ____                _             
 |_   _(_) | __\ \      / /__  _ __| | __| |/ ___|_ __ ___  __ _| |_ ___  _ __ 
   | | | | |/ _ \ \ /\ / / _ \| '__| |/ _` | |   | '__/ _ \/ _` | __/ _ \| '__|
   | | | | |  __/\ V  V / (_) | |  | | (_| | |___| | |  __/ (_| | || (_) | |   
   |_| |_|_|\___| \_/\_/ \___/|_|  |_|\__,_|\____|_|  \___|\__,_|\__\___/|_|   
                                                                               
	TileWorldCreator V4 (c) by Giant Grey
	Author: Marc Egli

	www.giantgrey.com

*/

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using Random = Unity.Mathematics.Random;

using GiantGrey.TileWorldCreator.Utilities;

namespace GiantGrey.TileWorldCreator
{
    [System.Serializable]
    public class BlueprintLayer : ScriptableObject, ISerializationCallbackReceiver
    {
        public Random random;
        public string guid;
        public string layerName;
        public Texture2D icon;
        public bool isEnabled = true;
        public Color layerColor = Color.white;
        public Texture2D previewTexture;
        public bool randomSeedOverride;
        public bool customRandomSeed;
        public int customSeed;
        public float defaultLayerHeight = 0f;
        public bool lockFromPaint;
        public bool foldoutState;
        public List<BlueprintModifier> tileMapModifiers = new List<BlueprintModifier>();
        

        #if !TWC_DEBUG
        [HideInInspector]
        #endif
        public bool showDebugGrid;

        int lastSeed;
        int currentSeed;

        [SerializeField]
        private Configuration configuration;
    
        private HashSet<Vector2> paintGrid = new HashSet<Vector2>(); 
    
        public HashSet<Vector2> allPositions = new HashSet<Vector2>();


        [SerializeField]
        private List<Vector2> serializedPaintPositions = new List<Vector2>();

        [SerializeField]
        private List<Vector2> serializedWorldPositions = new List<Vector2>();
        

        public Configuration GetAsset()
        {
            return configuration;
        }

        public void OnBeforeSerialize() 
        {
            serializedPaintPositions.Clear();
            serializedWorldPositions.Clear(); 
            serializedWorldPositions.TrimExcess();
            serializedWorldPositions.TrimExcess();

            serializedPaintPositions = paintGrid.ToList();
            serializedWorldPositions = allPositions.ToList();
        } 

        public void OnAfterDeserialize()
        {
        
            if (IsEditorBusy())
                return;          

            if (configuration == null)
                return;                

            allPositions.Clear();
            allPositions.TrimExcess();

            paintGrid.Clear();
            paintGrid.TrimExcess();
         
            serializedPaintPositions = serializedPaintPositions.Distinct().ToList();
            foreach (var pos in serializedPaintPositions)
            {             
                paintGrid.Add(pos);
                allPositions.Add(pos);
            }

         
            serializedWorldPositions = serializedWorldPositions.Distinct().ToList();
            foreach (var pos in serializedWorldPositions)
            {
                allPositions.Add(pos);
            }
        }

        bool IsEditorBusy()
        {
            #if UNITY_EDITOR
            return EditorApplication.isCompiling;
            #else
            return false;
            #endif
        }


        // List<GameObject> tileObjects = new List<GameObject>();
        // private bool[,] generatedMap;
        private HashSet<Vector2> generatedPositions = new HashSet<Vector2>();

        public BlueprintLayer()
        {
            guid = System.Guid.NewGuid().ToString();
        }

        public void ResetLayer()
        {
            allPositions.Clear();    
            allPositions.AddRange(paintGrid);
        }

        internal void SetAsset(Configuration _asset)
        {
            configuration = _asset;
        }

        public bool HasPositionOnLayer(Vector2 _position)
		{
			return allPositions.Contains(_position);
		}

        internal HashSet<Vector2> GetCellPositionsInRadius(Vector2 _position, float _radius)
        {
            HashSet<Vector2> _positions = new HashSet<Vector2>();
            allPositions.Where(x => Vector2.Distance(x, _position) <= _radius).ToList().ForEach(x => _positions.Add(x));
            return _positions;
        }

        
        // Executes all modifiers for this layer and adds final result to world map 
        public void ExecuteLayer(Configuration _configuration, System.Action _onCompleteCallback)
        {
            configuration = _configuration;

            uint _seed;

            if (randomSeedOverride)
			{	
				if (customRandomSeed)
				{
					lastSeed = customSeed;
					currentSeed = customSeed;
				}
				else
				{
					lastSeed = currentSeed;
					currentSeed = System.Environment.TickCount;
				}
				
                UnityEngine.Random.InitState(currentSeed);
                random = new Unity.Mathematics.Random((uint)currentSeed);

                // random = new Random(currentSeed);
                
            }
            else
            {
                
                if (configuration.useGlobalRandomSeed)
                {
                    _seed = (uint)configuration.globalRandomSeed;
                }
                else
                {
                    _seed = configuration.currentRandomSeed;
                }

                random = new Unity.Mathematics.Random(_seed);
                UnityEngine.Random.InitState((int)_seed);

                // random = new Random((int)_seed);
            }

          


            generatedPositions = new HashSet<Vector2>();
            generatedPositions = GetPaintedCellPositions(generatedPositions);
           

            // Apply modifiers
            for (int i = 0; i < tileMapModifiers.Count; i++)
            {
                if (tileMapModifiers[i] == null)
                    continue;

                if (!tileMapModifiers[i].isEnabled)
                    continue;

                tileMapModifiers[i].asset = _configuration;          
                generatedPositions = tileMapModifiers[i].Execute(generatedPositions, this);
                
            }

            // Add positions to worldGrid  
            allPositions.Clear();
           
            foreach (var pos in generatedPositions)
            {
                allPositions.Add(pos);
            }    
        }


        public void AddCells(HashSet<Vector2> _cellPositions)
        {
            foreach (var _pos in _cellPositions)
            {

                paintGrid.Add(_pos);

                allPositions.Add(_pos);
            }
        }


        public void RemoveCells(HashSet<Vector2> _cellPositions)
        {
            
            foreach (var _pos in _cellPositions)
            {
                paintGrid.Remove(_pos);
                allPositions.Remove(_pos);  
            }
            
        }

        public void ClearLayer(bool _executeLayer = true)
        { 
            allPositions.Clear();
            paintGrid.Clear();

            if (_executeLayer)
            {
                if (configuration == null)
                    return;
                    
                ExecuteLayer(configuration, null);
            }
        }

        public void FillLayer()
        {
            HashSet<Vector2> _positions = new HashSet<Vector2>();
            for (var w = 0; w < configuration.width; w ++)
            {
                for (var h = 0; h < configuration.height; h ++)
                {
                    _positions.Add(new Vector2(w, h));
                }
            }

            
            AddCells(_positions);
        }


        /// <summary>
        /// Return all cell positions. Includes painted cells as well.
        /// </summary>
        /// <returns></returns>
        public HashSet<Vector2> GetAllCellPositions(HashSet<Vector2> _positions)
        {
            foreach (var _pos in allPositions)
            {
                _positions.Add(_pos);
            }

            return _positions;
        }

        public HashSet<Vector2> GetPositionsInCluster(int _clusterID)
        {
            HashSet<Vector2> _returnPositions = new HashSet<Vector2>();
            foreach (var _pos in allPositions)
            {
                var _tmpKey = GetHashMapKey(_pos);
                if (_tmpKey == _clusterID)
                {
                    _returnPositions.Add(_pos);
                }
            }

            return _returnPositions;
        }

        public HashSet<Vector2> GetPaintedCellPositions(HashSet<Vector2> _positions)
        {

            _positions.AddRange(paintGrid);

            return _positions;
        }

        private int GetHashMapKey(Vector2 _position)
        {
            return (int)(Mathf.Floor(_position.x / configuration.clusterCellSize) + (configuration.clusterYMultiplier * Mathf.Floor(_position.y / configuration.clusterCellSize)));
        }


        internal Texture2D UpdatePreviewTexture(Texture2D _previousTexture)
        {   
            UpdatePreviewTexture(generatedPositions, layerColor, _previousTexture);
            
            return previewTexture;
        }
        
        public Texture2D UpdatePreviewTexture(HashSet<Vector2> positions, Color color, Texture2D previousTexture)
        {
#if UNITY_EDITOR

            int w = configuration.width;
            int h = configuration.height;

            if (w <= 0 || h <= 0)
                return previousTexture;

            int size = Mathf.Max(w, h);

            // Create or reuse existing preview texture
            if (previewTexture == null || previewTexture.width != size || previewTexture.height != size)
            {
                if (previewTexture != null)
                    UnityEngine.Object.DestroyImmediate(previewTexture);

                previewTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                    hideFlags = HideFlags.HideAndDontSave  // runtime-only, no asset DB
                };
            }

            // Build pixel array
            Color32[] colors;

            if (configuration.mergePreviewTextures && previousTexture != null)
            {
                colors = previousTexture.GetPixels32();
            }
            else
            {
                colors = new Color32[size * size];
                for (int i = 0; i < colors.Length; i++)
                    colors[i] = new Color32(0, 0, 0, 0);
            }

            // Draw positions
            foreach (var pos in positions)
            {
                int x = (int)pos.x;
                int y = (int)pos.y;

                if (x < 0 || y < 0 || x >= size || y >= size)
                    continue;

                int index = y * size + x;
                colors[index] = new Color32(
                    (byte)(color.r * 255),
                    (byte)(color.g * 255),
                    (byte)(color.b * 255),
                    255
                );
            }

            previewTexture.SetPixels32(colors);
            previewTexture.Apply();

            return previewTexture;

#else
    return previousTexture;
#endif
        }

        private void UpdatePreviewTexture_OLD(HashSet<Vector2> _positions, Color color, Texture2D _previousTexture)
        {
            #if UNITY_EDITOR
            var _w = configuration.width;
            var _h = configuration.height;
            
            if (_w > 0 && _h > 0)
            {
                var _sqr = (int)Mathf.Max(_w, _h);
            
                var _previewTexture = new Texture2D(_sqr, _sqr);
                _previewTexture.wrapMode = TextureWrapMode.Clamp;
                _previewTexture.filterMode = FilterMode.Point;



                if (previewTexture == null || !UnityEditor.AssetDatabase.Contains(previewTexture) || previewTexture.width != _w || previewTexture.height != _h)
                {

                    if (previewTexture != null)
                    {
                        DestroyImmediate(previewTexture, true);
                    }

#if UNITY_EDITOR
                    UnityEditor.AssetDatabase.AddObjectToAsset(_previewTexture, this);
                    _previewTexture.hideFlags = HideFlags.HideInHierarchy;
                    UnityEditor.AssetDatabase.SaveAssets();
#endif

                    previewTexture = _previewTexture;
                }
                else
                {
                    if (previewTexture != null)
                    {
                        // Because of a bug, we need to make sure preview texture is set to hide in hierarchy.
                        previewTexture.hideFlags = HideFlags.HideInHierarchy;
                    }
                }

                Color32[] _colors = new Color32[_sqr * _sqr];
              
                if (configuration.mergePreviewTextures && _previousTexture != null)
                {
                    _colors  = _previousTexture.GetPixels32();
                }
                

                foreach (var _pos in _positions)
                {
                    var _index = (int)_pos.y * previewTexture.height + (int)_pos.x;
                    if (_index < _colors.Length && _index >= 0)
                    {
                        _colors[(int)_pos.y * previewTexture.height + (int)_pos.x] = new Color(color.r, color.g, color.b, 255f/255f);
                    }
                }
                    
                previewTexture.SetPixels32 (_colors);
                previewTexture.Apply();

            }
            #endif
        }

        bool[,] GetMapArray(List<Vector2> _positions)
        {
            if (_positions.Count == 0)
            return new bool[0,0];

            Vector2 min, max;
            min = max = _positions[0];
            
            min = new Vector3(_positions.Min(x => x.x), 0, _positions.Min(y => y.y));
            max = new Vector3(_positions.Max(x => x.x), 0, _positions.Max(y => y.y));

            var b = new Bounds(min, Vector3.zero);
            foreach (var r in _positions) 
            {
                b.Encapsulate(r);
            }


            int width = Mathf.RoundToInt(b.size.x);
            int height = Mathf.RoundToInt(b.size.y);
        
            bool[,] _map = new bool[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2 _pos = new Vector2(x, y);
                    _map[x, y] = _positions.Contains(_pos);
                }
            }

            return _map;
        }


        
        // Returns unique configuration code based on surrounding cells
        public int GetConfigurationCode(Vector2 _position)
        {
            var _configuration = GetConfigurationBitmask(HasNeighbors(_position, allPositions));
            return _configuration;
        }

        static readonly Vector2[] NeighborOffsets = new Vector2[]
        {
            new Vector2(1, 0),   // Right
            new Vector2(-1, 0),  // Left
            new Vector2(0, 1),   // Up
            new Vector2(0, -1),  // Down
            // For 8-directional:
            new Vector2(1, 1),   // Top-right
            new Vector2(1, -1),  // Bottom-right
            new Vector2(-1, 1),  // Top-left
            new Vector2(-1, -1)  // Bottom-left
        };

        // Check if a cell has neighbors in the HashSet
        private bool[] HasNeighbors(Vector2 cell, HashSet<Vector2> worldGrid)
        {
            bool _north = false;
            bool _south = false;
            bool _west = false;
            bool _east = false;
            bool _northWest = false;
            bool _northEast = false;
            bool _southEast = false;
            bool _southWest = false;

            int _count = 0;
            foreach (var offset in NeighborOffsets)
            {
                if (worldGrid.Contains(cell + offset))
                {
                    switch (_count)
                    {
                        case 0:
                        _east = true;
                        break;
                        case 1:
                        _west = true;
                        break;
                        case 2:
                        _north = true;
                        break;
                        case 3:
                        _south = true;
                        break;
                        case 4:
                        _northEast = true;
                        break;
                        case 5:
                        _southEast = true;
                        break;
                        case 6:
                        _northWest = true;
                        break;
                        case 7:
                        _southWest = true;
                        break;
                    }
                }  

                _count ++;
            }

            return new bool[] { _northWest, _north, _northEast, _west, true, _east, _southWest, _south, _southEast };
            
        }

        public int GetConfigurationCodeFromPositionsInEightDirections(Vector2 _position, HashSet<Vector2> _positions)
        {
            bool _north = false;
            bool _south = false;
            bool _west = false;
            bool _east = false;
            bool _northWest = false;
            bool _northEast = false;
            bool _southEast = false;
            bool _southWest = false;

            for (int x = -1; x < 2; x ++)  
            {
                for (int y = -1; y < 2; y ++)
                {
                    var _pos = new Vector2(_position.x + (x), _position.y + (y));
                    
                    if (_positions.Contains(_pos))
                    {
                        
                        if (x == 0 && y == -1)
                        {
                            _south = true; 
                        }
                        if (x == 0 && y == 1)
                        {
                            _north = true;
                        }
                        if (x == 1 && y == 0)
                        {
                            _east = true;
                        }
                        if (x == -1 && y == 0)
                        {
                            _west = true;
                        }
                        if (x == -1 && y == -1)
                        {
                            _southWest = true;
                        }
                        if (x == 1 && y == -1)
                        {
                            _southEast = true;
                        }
                        if (x == 1 && y == 1)
                        {
                            _northEast = true;
                        }
                        if (x == -1 && y == 1)
                        {
                            _northWest = true;
                        }
                        
                    }
                }
            }

            bool [] _neighbours = { _northWest, _north, _northEast, _west, true, _east, _southWest, _south, _southEast };
            var _configuration = GetConfigurationBitmask(_neighbours);
            return _configuration;
        }
        
        public int GetConfigurationCodeFromPositionsInFourDirections(Vector2 _position, HashSet<Vector2> _positions)
        {
            bool _north = false;
            bool _south = false;
            bool _west = false;
            bool _east = false;

            for (int x = -1; x < 2; x ++)  
            {
                for (int y = -1; y < 2; y ++)
                {
                    var _pos = new Vector2(_position.x + (x), _position.y + (y));
                    
                    if (_positions.Contains(_pos))
                    {
                        
                        if (x == 0 && y == -1)
                        {
                            _south = true; 
                        }
                        if (x == 0 && y == 1)
                        {
                            _north = true;
                        }
                        if (x == 1 && y == 0)
                        {
                            _east = true;
                        }
                        if (x == -1 && y == 0)
                        {
                            _west = true;
                        } 
                    }
                }
            }

            bool [] _neighbours = { _north, _west, true, _east, _south };
            var _configuration = GetConfigurationBitmask(_neighbours);
            return _configuration;
        }

        public int GetConfigurationCodeInFourDirections(Vector2 _position)
        {
            var _neighbours = HasNeighbors(_position, allPositions);
            // _north, _west, true, _east, _south
            bool [] _fourNeighbours = { _neighbours[1], _neighbours[3], true, _neighbours[5], _neighbours[7]};
            var _configuration = GetConfigurationBitmask(_fourNeighbours);
            return _configuration;
        }

        public bool[] GetNeighbours(Vector2 _position)
        {
            return HasNeighbors(_position, allPositions); 
        }


        public int GetConfigurationBitmask(bool[] connectionGrid)
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

        public T AddModifier<T>() where T : BlueprintModifier
        {
            var _modifier = ScriptableObject.CreateInstance<T>();
            tileMapModifiers.Add(_modifier);

            #if UNITY_EDITOR
            _modifier.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(_modifier, this);
            // EditorUtility.SetDirty(_modifier);

            // string assetPath = AssetDatabase.GetAssetPath(_modifier);
            // var _guidString = AssetDatabase.AssetPathToGUID(assetPath);
            // GUID _guid = GUID.Generate();
            // GUID.TryParse(_guidString, out _guid);
            // AssetDatabase.SaveAssetIfDirty(_guid);
            var _so = new SerializedObject(this);
			_so.ApplyModifiedProperties();
			_so.Update();
            #endif
        
            return _modifier;
        }

        public void RemoveModifier<T>(T _modifier) where T : BlueprintModifier
        {
            #if UNITY_EDITOR
            AssetDatabase.RemoveObjectFromAsset(_modifier);
			// AssetDatabase.SaveAssets();
            var _so = new SerializedObject(this);
			_so.ApplyModifiedProperties();
			_so.Update();
            #endif

            tileMapModifiers.Remove(_modifier);
        }

        public T GetModifier<T>(int _index) where T : BlueprintModifier
        {
            return tileMapModifiers[_index] as T;
        }


#if TWC_DEBUG
        public void OnDebugDraw()
        {
            if (!showDebugGrid)
                return;

            foreach (var _p in allPositions)
            {
                Gizmos.color = layerColor;
                Gizmos.DrawWireCube(new Vector3(_p.x, 0, _p.y), new Vector3(1, 0.2f, 1));
                Gizmos.color = Color.white;

                // Handles.Label(new Vector3(_p.x, 0, _p.y - 0.2f), GetConfigurationCode(_p).ToString());
                // Handles.Label(new Vector3(_p.x, 0, _p.y - 0.2f), GetConfigurationCodeFromPositionsInFourDirections(_p, allPositions).ToString());
                Handles.Label(new Vector3(_p.x, 0, _p.y - 0.4f), GetConfigurationCodeFromPositionsInEightDirections(_p, allPositions).ToString());

            }


            // foreach(var _key in worldGrid.Keys)
            // {
            //     foreach(var _p in worldGrid[_key].Keys)
            //     {
            // foreach (var _p in allPositions)
            // {
            //         Handles.Label(new Vector3(_p.x, 0, _p.y - 0.2f), GetConfigurationCode(_p).ToString());

            //         Gizmos.color = layerColor;
            //         Gizmos.DrawWireCube(new Vector3(_p.x, 0, _p.y), new Vector3(1, 0.2f, 1));
            //         Gizmos.color = Color.white;
            // }
            // }
            // }
        }
#endif

        public void OnSceneDraw()
        {
            for (int i = 0; i < tileMapModifiers.Count; i ++)
            {
                tileMapModifiers[i].OnSceneDraw();
            }
        }
    }
}