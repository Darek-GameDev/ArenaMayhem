
// ##################
// V3 DEPRECATED
// ##################

// using UnityEngine;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using GiantGrey.TileWorldCreator;
// using Unity.Entities.UniversalDelegates;

// namespace TileWorldCreator4.Utilities
// {

//     /// <summary>
//     /// Creates a custom mesh collider based on the tiles
//     /// </summary>
//     public static class MeshColliderGenerator
//     {
        
//         public class BorderData
//         {
//             public Vector2 vertexPosition;
//             public bool[] neighbours;
            
//             public BorderData (Vector2 _pos, bool[] _neighbours)
//             {
//                 vertexPosition = _pos;
//                 neighbours = _neighbours;
//             }
//         }
        
//         static List<BorderData> borderData;
//         static List<Vector3> newVertices;	
//         static List<int> newTriangles;
    
//         static  int squareCount;
    
    
//         public static Mesh GenerateMeshCollider(Configuration _asset, string _layerGuid, float _height, float _heightOffset, float _borderOffset)
//         {
        
//             // WorldMap _map = _twc.GetGeneratedBlueprintMap(_layerGuid + "_UNSUBD");
//             // bool[,] _boolMap = _twc.GetMapOutputFromBlueprintLayer(_layerGuid);
            
//             borderData = new List<BorderData>();
//             newVertices = new List<Vector3>();	
//             newTriangles = new List<int>();
            
//             // foreach(var _position in _map.clusters[_cluster].Keys)
//             // {
//             //     var _tileData = _map.clusters[_cluster][_position];
                            
//             //     GenSquare((int)_tileData.position.x, (int)_tileData.position.z, _height, _heightOffset, _borderOffset, _twc.twcAsset.cellSize, _tileData.neighboursLocation);
//             //     borderData.Add(new BorderData(new Vector2(_tileData.position.x, _tileData.position.z), _tileData.neighboursLocation));
//             // }

//             var _layer = _asset.GetLayerByGuid(_layerGuid);
//             HashSet<Vector2> _positions = new HashSet<Vector2>();
//             _layer.GetAllCellPositions(_positions);

//             foreach (var _pos in _positions)
//             {
//                 var _neighbours = _layer.HasNeighbors(_pos, _positions);

//                 GenSquare((int)_pos.x, (int)_pos.y, _height, _height, _borderOffset, _asset.cellSize, _neighbours);
//                 borderData.Add(new BorderData(new Vector2(_pos.x, _pos.y), _neighbours));
//             }        
            
//             // Generate border 
//             GetBorderVertecies();
                
//             for (int i = 0; i < borderData.Count; i ++)
//             {
//                 GenBorder(borderData[i].vertexPosition, borderData[i].neighbours, _height, _heightOffset, _borderOffset, _asset.cellSize);	
//             }
    
//             var _mesh = UpdateMesh();
//             return _mesh;
//         }
        
    
        
//         static void GenSquare( int x, int y, float _height, float _heightOffset, float _borderOffset, float _cellSize, bool[] _neighbours)
//         {
            
//             float _xOffset = -.25f;
//             float _zOffset = .75f;
            
//             var _cSize = _cellSize;
//             //_cSize = (_cSize * 0.5f) + 0.5f;
//             var _cellOffset = _cSize / 4; //((_cSize * 2f) / 4f);
            
//             newVertices.Add( new Vector3 (((x + _xOffset) * _cSize) + _cellOffset, _heightOffset + _height, ((y + _zOffset) * _cSize) + _cellOffset));
//             newVertices.Add( new Vector3 (((x + 1 + _xOffset) * _cSize) + _cellOffset, _heightOffset + _height, ((y + _zOffset)* _cSize) + _cellOffset));
//             newVertices.Add( new Vector3 (((x + 1 + _xOffset) * _cSize) + _cellOffset, _heightOffset + _height, ((y - 1 + _zOffset)* _cSize) + _cellOffset));
//             newVertices.Add( new Vector3 (((x + _xOffset) * _cSize) + _cellOffset, _heightOffset + _height, ((y - 1 + _zOffset) * _cSize) + _cellOffset));
//             Triangle();
//             squareCount++;

//             // return new bool[] { _northWest, _north, _northEast, _west, true, _east, _southWest, _south, _southEast };

//             // North
//             if (!_neighbours[1])
//             {
//                 var _p = newVertices[newVertices.Count-3];
//                 _p = new Vector3(_p.x , _p.y, _p.z + _borderOffset);
//                 newVertices[newVertices.Count-3] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-4];
//                 _p2 = new Vector3(_p2.x , _p2.y, _p2.z + _borderOffset);
//                 newVertices[newVertices.Count-4	] = _p2;
//             }
//             // South
//             if (!_neighbours[7])
//             {
//                 var _p = newVertices[newVertices.Count-1];
//                 _p = new Vector3(_p.x , _p.y, _p.z - _borderOffset);
//                 newVertices[newVertices.Count-1] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-2];
//                 _p2 = new Vector3(_p2.x , _p2.y, _p2.z - _borderOffset);
//                 newVertices[newVertices.Count-2	] = _p2;
//             }
            
//             // West
//             if (!_neighbours[3])
//             {
//                 var _p = newVertices[newVertices.Count-1];
//                 _p = new Vector3(_p.x - _borderOffset, _p.y, _p.z );
//                 newVertices[newVertices.Count-1] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-4];
//                 _p2 = new Vector3(_p2.x - _borderOffset, _p2.y, _p2.z );
//                 newVertices[newVertices.Count-4	] = _p2;
//             }
            
//             // East
//             if (!_neighbours[5])
//             {
//                 var _p = newVertices[newVertices.Count-2];
//                 _p = new Vector3(_p.x + _borderOffset, _p.y, _p.z );
//                 newVertices[newVertices.Count-2] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-3];
//                 _p2 = new Vector3(_p2.x + _borderOffset, _p2.y, _p2.z );
//                 newVertices[newVertices.Count-3	] = _p2;
//             }
            
            
//             // North, NorthEast, East, SouthEast, South, SouthWest, West, NorthWest
//             if (_neighbours[1] && _neighbours[2] && _neighbours[5] && !_neighbours[8] && _neighbours[7] && _neighbours[6] && _neighbours[3] && _neighbours[0])
//             // if (_location.north && _location.northEast && _location.east && !_location.southEast && _location.south && _location.southWest && _location.west && _location.northWest)
//             {
//                 var _p = newVertices[newVertices.Count-2];
//                 _p = new Vector3(_p.x + _borderOffset, _p.y, _p.z - _borderOffset);
//                 newVertices[newVertices.Count-2] = _p;
//             }
            
//             if (_neighbours[1] && _neighbours[2] && _neighbours[5] && _neighbours[8] && _neighbours[7] && !_neighbours[6] && _neighbours[3] && _neighbours[0])
//             // if (_location.north && _location.northEast && _location.east && _location.southEast && _location.south && !_location.southWest && _location.west && _location.northWest)
//             {
//                 var _p = newVertices[newVertices.Count-1];
//                 _p = new Vector3(_p.x - _borderOffset, _p.y, _p.z - _borderOffset);
//                 newVertices[newVertices.Count-1] = _p;
//             }
            
//             if (_neighbours[1] && !_neighbours[2] && _neighbours[5] && _neighbours[8] && _neighbours[7] && _neighbours[6] && _neighbours[3] && _neighbours[0])
//             // if (_location.north && !_location.northEast && _location.east && _location.southEast && _location.south && _location.southWest && _location.west && _location.northWest)
//             {
//                 var _p = newVertices[newVertices.Count-3];
//                 _p = new Vector3(_p.x + _borderOffset, _p.y, _p.z + _borderOffset);
//                 newVertices[newVertices.Count-3] = _p;
//             }
            
//             if (_neighbours[1] && _neighbours[2] && _neighbours[5] && _neighbours[8] && _neighbours[7] && _neighbours[6] && _neighbours[3] && !_neighbours[0])
//             // if (_location.north && _location.northEast && _location.east && _location.southEast && _location.south && _location.southWest && _location.west && !_location.northWest)
//             {
//                 var _p = newVertices[newVertices.Count-4];
//                 _p = new Vector3(_p.x - _borderOffset, _p.y, _p.z + _borderOffset);
//                 newVertices[newVertices.Count-4] = _p;
//             }
//         }
        
        
//         static void GenBorder(Vector2 _pos, bool[] _neighbours, float _height, float _heightOffset, float _borderOffset, float _cellSize)
//         {
//             int x = (int)_pos.x;
//             int y = (int)_pos.y;
        
//             float _xOffset = -.25f;
//             float _zOffset = .75f;
            
//             var _cSize = _cellSize;
//             //_cSize = (_cSize * 0.5f) + 0.5f;
//             var _cellOffset = _cSize / 4; // ((_cSize * 2f) / 4f);
            
//             // new bool[] { _northWest, _north, _northEast, _west, true, _east, _southWest, _south, _southEast };


//             // Top side
//             // North
//             if (!_neighbours[1])
//             // if (!_location.north)
//             {
//                 newVertices.Add( new Vector3 (((x + _xOffset) * _cSize) + _cellOffset, (_heightOffset + _height) - _height, (((y + _zOffset) * _cSize) + _cellOffset)  + _borderOffset));
//                 newVertices.Add( new Vector3 (((x + 1 + _xOffset) * _cSize) + _cellOffset, (_heightOffset + _height) - _height, (((y + _zOffset) * _cSize) + _cellOffset) + _borderOffset));
//                 newVertices.Add( new Vector3 (((x + 1 + _xOffset) * _cSize) + _cellOffset, _heightOffset + _height, (((y + _zOffset) * _cSize) + _cellOffset) + _borderOffset));
//                 newVertices.Add( new Vector3 (((x + _xOffset) * _cSize) + _cellOffset, _heightOffset + _height, (((y + _zOffset) * _cSize) + _cellOffset) + _borderOffset));
//                 Triangle();
//                 squareCount++;
//             }	
            
//             // Add offset to corner (corner: top - left)
//             // North && West
//             if (!_neighbours[1] && !_neighbours[3])
//             // if (!_location.north && !_location.west)
//             {
//                 var _p = newVertices[newVertices.Count-1];
//                 _p = new Vector3(_p.x - _borderOffset, _p.y, _p.z);
//                 newVertices[newVertices.Count-1] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-4];
//                 _p2 = new Vector3(_p2.x - _borderOffset, _p2.y, _p2.z);
//                 newVertices[newVertices.Count-4	] = _p2;
//             }
            
//             // Add offset to corner  (corner: top - right)
//             // North && East
//             if (!_neighbours[1] && !_neighbours[5])
//             // if (!_location.north && !_location.east)
//             {
//                 var _p = newVertices[newVertices.Count-2];
//                 _p = new Vector3(_p.x + _borderOffset, _p.y, _p.z);
//                 newVertices[newVertices.Count-2] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-3];
//                 _p2 = new Vector3(_p2.x + _borderOffset, _p2.y, _p2.z);
//                 newVertices[newVertices.Count-3	] = _p2;
//             }
            
//             // Add offset to interior corner  (corner: bottom - left)
//             // NorthEast, North, NorthWest, South, SouthWest ,SouthEast
//             if (!_neighbours[2] && !_neighbours[1] && _neighbours[0] && _neighbours[7] && _neighbours[6] && _neighbours[8])
//             // if (!_location.northEast && !_location.north && _location.northWest && _location.south && _location.southWest && _location.southEast)
//             {
//                 var _p = newVertices[newVertices.Count-1];
//                 _p = new Vector3(_p.x + _borderOffset, _p.y, _p.z);
//                 newVertices[newVertices.Count-1] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-4];
//                 _p2 = new Vector3(_p2.x + _borderOffset, _p2.y, _p2.z);
//                 newVertices[newVertices.Count-4	] = _p2;
//             }
            
//             // Add offset to interior corner  (corner: bottom - right)
//             if (_neighbours[2] && !_neighbours[1] && !_neighbours[0] && _neighbours[7] && _neighbours[6] && _neighbours[8])
//             // if (_location.northEast && !_location.north && !_location.northWest && _location.south && _location.southWest && _location.southEast)
//             {
//                 var _p = newVertices[newVertices.Count-2];
//                 _p = new Vector3(_p.x - _borderOffset, _p.y, _p.z);
//                 newVertices[newVertices.Count-2] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-3];
//                 _p2 = new Vector3(_p2.x - _borderOffset, _p2.y, _p2.z);
//                 newVertices[newVertices.Count-3	] = _p2;
//             }
            
//             // Bottom side
//             // South
//             if (!_neighbours[7])
//             // if (!_location.south)
//             {
//                 newVertices.Add( new Vector3 (((x + _xOffset) * _cSize) + _cellOffset , (_heightOffset + _height) - (_height), (((y - 1 + _zOffset) * _cSize) + _cellOffset) - _borderOffset));
//                 newVertices.Add( new Vector3 (((x + 1 + _xOffset) * _cSize) + _cellOffset, (_heightOffset + _height) - (_height), (((y - 1 + _zOffset) * _cSize) + _cellOffset) - _borderOffset));
//                 newVertices.Add( new Vector3 (((x + 1 + _xOffset) * _cSize) + _cellOffset, _heightOffset + _height, (((y - 1 + _zOffset) * _cSize) + _cellOffset) - _borderOffset));
//                 newVertices.Add( new Vector3 (((x + _xOffset) * _cSize) + _cellOffset, _heightOffset + _height, (((y - 1 + _zOffset) * _cSize) + _cellOffset) - _borderOffset));
//                 TriangleFlipped();
//                 squareCount++;
//             }
            
//             // Add offset to corner (corner: bottom - left)
//             // South && West
//             if (!_neighbours[7] && !_neighbours[3])
//             // if (!_location.south && !_location.west)
//             {
//                 var _p = newVertices[newVertices.Count-1];
//                 _p = new Vector3(_p.x - _borderOffset, _p.y, _p.z);
//                 newVertices[newVertices.Count-1] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-4];
//                 _p2 = new Vector3(_p2.x - _borderOffset, _p2.y, _p2.z);
//                 newVertices[newVertices.Count-4	] = _p2;
//             }
            
            
//             // Add offset to corner (corner: bottom - right)
//             // South && East
//             if (!_neighbours[7] && !_neighbours[5])
//             // if (!_location.south && !_location.east)
//             {
//                 var _p = newVertices[newVertices.Count-2];
//                 _p = new Vector3(_p.x + _borderOffset, _p.y, _p.z);
//                 newVertices[newVertices.Count-2] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-3];
//                 _p2 = new Vector3(_p2.x + _borderOffset, _p2.y, _p2.z);
//                 newVertices[newVertices.Count-3	] = _p2;
//             }
            
//             // Add offset to interior corner  (corner: top - left)
//             if (_neighbours[2] && _neighbours[1] && _neighbours[0] && !_neighbours[7] && _neighbours[6] && !_neighbours[8])
//             // if (_location.northEast && _location.north && _location.northWest && !_location.south && _location.southWest && !_location.southEast)
//             {
//                 var _p = newVertices[newVertices.Count-1];
//                 _p = new Vector3(_p.x + _borderOffset, _p.y, _p.z);
//                 newVertices[newVertices.Count-1] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-4];
//                 _p2 = new Vector3(_p2.x + _borderOffset, _p2.y, _p2.z);
//                 newVertices[newVertices.Count-4	] = _p2;
//             }
            
//             // Add offset to interior corner  (corner: top - right)
//              if (_neighbours[2] && _neighbours[1] && _neighbours[0] && !_neighbours[7] && !_neighbours[6] && _neighbours[8])
//             // if (_location.northEast && _location.north && _location.northWest && !_location.south && !_location.southWest && _location.southEast)
//             {
//                 var _p = newVertices[newVertices.Count-2];
//                 _p = new Vector3(_p.x - _borderOffset, _p.y, _p.z);
//                 newVertices[newVertices.Count-2] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-3];
//                 _p2 = new Vector3(_p2.x - _borderOffset, _p2.y, _p2.z);
//                 newVertices[newVertices.Count-3	] = _p2;
//             }
            
            
            
//             // Left side
//             // West
//             if (!_neighbours[3])
//             // if (!_location.west)
//             {
//                 newVertices.Add( new Vector3 ((((x + _xOffset) * _cSize) + _cellOffset) - _borderOffset, (_heightOffset + _height) - (_height), ((y -1 + _zOffset) * _cSize) + _cellOffset));
//                 newVertices.Add( new Vector3 ((((x + _xOffset) * _cSize) + _cellOffset) - _borderOffset, (_heightOffset + _height) - (_height), ((y + _zOffset) * _cSize) + _cellOffset));
//                 newVertices.Add( new Vector3 ((((x + _xOffset) * _cSize) + _cellOffset) - _borderOffset, _heightOffset + _height, ((y + _zOffset) * _cSize) + _cellOffset));
//                 newVertices.Add( new Vector3 ((((x + _xOffset) * _cSize) + _cellOffset) - _borderOffset, _heightOffset + _height, ((y -1 + _zOffset) * _cSize) + _cellOffset));
//                 Triangle();
//                 squareCount++;
//             }
            
//             // Add offset to corner (corner: top - left)
//             // North && West
//             if (!_neighbours[1] && !_neighbours[3])
//             // if (!_location.north && !_location.west)
//             {
//                 var _p = newVertices[newVertices.Count-2];
//                 _p = new Vector3(_p.x, _p.y, _p.z + _borderOffset);
//                 newVertices[newVertices.Count-2] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-3];
//                 _p2 = new Vector3(_p2.x , _p2.y, _p2.z + _borderOffset);
//                 newVertices[newVertices.Count-3] = _p2;
//             }
            
//             // Add offset to corner (corner: bottom - left)
//             if (!_neighbours[7] && !_neighbours[3])
//             // if (!_location.south && !_location.west)
//             {
//                 var _p = newVertices[newVertices.Count-1];
//                 _p = new Vector3(_p.x, _p.y, _p.z - _borderOffset);
//                 newVertices[newVertices.Count-1] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-4];
//                 _p2 = new Vector3(_p2.x , _p2.y, _p2.z - _borderOffset);
//                 newVertices[newVertices.Count-4] = _p2;
//             }

//             //  { _northWest, _north, _northEast, _west, true, _east, _southWest, _south, _southEast };

            
//             // Add offset to interior corner  (corner: top - left)
//             if (_neighbours[1] && _neighbours[2] && _neighbours[5] && _neighbours[8] && _neighbours[7] && !_neighbours[6] && !_neighbours[3] && _neighbours[0])
//             // if ( _location.north && _location.northEast && _location.east && _location.southEast && _location.south && !_location.southWest && !_location.west && _location.northWest)
//             {
//                 var _p = newVertices[newVertices.Count-2];
//                 _p = new Vector3(_p.x , _p.y, _p.z - _borderOffset);
//                 newVertices[newVertices.Count-2] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-3];
//                 _p2 = new Vector3(_p2.x, _p2.y, _p2.z - _borderOffset);
//                 newVertices[newVertices.Count-3	] = _p2;
//             }
            
//             // Add offset to interior corner  (corner: top - left)
//             if (_neighbours[1] && _neighbours[2] && _neighbours[5] && _neighbours[8] && _neighbours[7] && _neighbours[6] && !_neighbours[3] && !_neighbours[0])
//             // if ( _location.north && _location.northEast && _location.east && _location.southEast && _location.south && _location.southWest && !_location.west && !_location.northWest)
//             {
//                 var _p = newVertices[newVertices.Count-1];
//                 _p = new Vector3(_p.x , _p.y, _p.z + _borderOffset);
//                 newVertices[newVertices.Count-1] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-4];
//                 _p2 = new Vector3(_p2.x, _p2.y, _p2.z + _borderOffset);
//                 newVertices[newVertices.Count-4] = _p2;
//             }
            
//             // Right side
//             // East
//             if (!_neighbours[5])
//             // if (!_location.east)
//             {
//                 newVertices.Add( new Vector3 ((((x + 1 + _xOffset) * _cSize) + _cellOffset) + _borderOffset, (_heightOffset + _height) - (_height), ((y + _zOffset) * _cSize) + _cellOffset));
//                 newVertices.Add( new Vector3 ((((x + 1 + _xOffset) * _cSize) + _cellOffset) + _borderOffset, (_heightOffset + _height) - (_height), ((y - 1 + _zOffset) * _cSize) + _cellOffset));
//                 newVertices.Add( new Vector3 ((((x + 1 + _xOffset) * _cSize) + _cellOffset) + _borderOffset, _heightOffset + _height, ((y - 1 + _zOffset) * _cSize) + _cellOffset));
//                 newVertices.Add( new Vector3 ((((x + 1 + _xOffset) * _cSize) + _cellOffset) + _borderOffset, _heightOffset + _height, ((y + _zOffset) * _cSize) + _cellOffset));
//                 Triangle();
//                 squareCount++;
//             }
            
//             // Add offset to corner (corner: top - right)
//             // North && East
//             if (!_neighbours[1] && !_neighbours[5])
//             // if (!_location.north && !_location.east)
//             {
//                 var _p = newVertices[newVertices.Count-1];
//                 _p = new Vector3(_p.x, _p.y, _p.z + _borderOffset);
//                 newVertices[newVertices.Count-1] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-4];
//                 _p2 = new Vector3(_p2.x , _p2.y, _p2.z + _borderOffset);
//                 newVertices[newVertices.Count-4] = _p2;
//             }
            
//             // Add offset to corner (corner: bottom - right)
//             // South && East
//             if (!_neighbours[7] && !_neighbours[5])
//             // if (!_location.south && !_location.east)
//             {
//                 var _p = newVertices[newVertices.Count-2];
//                 _p = new Vector3(_p.x, _p.y, _p.z - _borderOffset);
//                 newVertices[newVertices.Count-2] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-3];
//                 _p2 = new Vector3(_p2.x , _p2.y, _p2.z - _borderOffset);
//                 newVertices[newVertices.Count-3] = _p2;
//             }
            
            
//             // Add offset to interior corner  (corner: top - left)
//             if (_neighbours[1] && _neighbours[2] && !_neighbours[5] && !_neighbours[8] && _neighbours[7] && _neighbours[6] && _neighbours[3] && _neighbours[0])
//             // if ( _location.north && _location.northEast && !_location.east && !_location.southEast && _location.south && _location.southWest && _location.west && _location.northWest)
//             {
//                 var _p = newVertices[newVertices.Count-1];
//                 _p = new Vector3(_p.x , _p.y, _p.z - _borderOffset);
//                 newVertices[newVertices.Count-1] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-4];
//                 _p2 = new Vector3(_p2.x, _p2.y, _p2.z - _borderOffset);
//                 newVertices[newVertices.Count-4	] = _p2;
//             }
            
//             // Add offset to interior corner  (corner: top - left)
//             if (_neighbours[1] && !_neighbours[2] && !_neighbours[5] && _neighbours[8] && _neighbours[7] && _neighbours[6] && _neighbours[3] && _neighbours[0])
//             // if ( _location.north && !_location.northEast && !_location.east && _location.southEast && _location.south && _location.southWest && _location.west && _location.northWest)
//             {
//                 var _p = newVertices[newVertices.Count-2];
//                 _p = new Vector3(_p.x , _p.y, _p.z + _borderOffset);
//                 newVertices[newVertices.Count-2] = _p;
                
//                 var _p2 = newVertices[newVertices.Count-3];
//                 _p2 = new Vector3(_p2.x, _p2.y, _p2.z + _borderOffset);
//                 newVertices[newVertices.Count-3] = _p2;
//             }
        
//         }
        
//         static void Triangle()
//         {
//             newTriangles.Add(squareCount*4);
//             newTriangles.Add((squareCount*4)+1);
//             newTriangles.Add((squareCount*4)+3);
//             newTriangles.Add((squareCount*4)+1);
//             newTriangles.Add((squareCount*4)+2);
//             newTriangles.Add((squareCount*4)+3);
//         }

//         static void TriangleFlipped()
//         {
//             newTriangles.Add(squareCount*4);
//             newTriangles.Add((squareCount*4)+3);
//             newTriangles.Add((squareCount*4)+1);
//             newTriangles.Add((squareCount*4)+3);
//             newTriangles.Add((squareCount*4)+2);
//             newTriangles.Add((squareCount*4)+1);
//         }
        
        
//         static void GetBorderVertecies()
//         {
    
//             borderData = borderData.Distinct().ToList();
    
//             List<Vector2> removeVertices = new List<Vector2>();
            
//             for (int i = 0; i < borderData.Count; i ++)
//             {
    
//                 int _hasNeighbours = 0;
//                 try
//                 {
//                     if (borderData[i].vertexPosition == new Vector2(borderData[i].vertexPosition.x - 1, borderData[i].vertexPosition.y - 1))
//                     {
//                         _hasNeighbours ++;
//                     }
//                     if (borderData[i].vertexPosition == new Vector2(borderData[i].vertexPosition.x, borderData[i].vertexPosition.y - 1))
//                     {
//                         _hasNeighbours ++;
//                     }
//                     if (borderData[i].vertexPosition == new Vector2(borderData[i].vertexPosition.x + 1, borderData[i].vertexPosition.y - 1))
//                     {
//                         _hasNeighbours ++;
//                     }
//                     if (borderData[i].vertexPosition == new Vector2(borderData[i].vertexPosition.x - 1, borderData[i].vertexPosition.y))
//                     {
//                         _hasNeighbours ++;
//                     }
//                     if (borderData[i].vertexPosition == new Vector2(borderData[i].vertexPosition.x + 1, borderData[i].vertexPosition.y))
//                     {
//                         _hasNeighbours ++;
//                     }
//                     if (borderData[i].vertexPosition == new Vector2(borderData[i].vertexPosition.x - 1, borderData[i].vertexPosition.y + 1))
//                     {
//                         _hasNeighbours ++;
//                     }
//                     if (borderData[i].vertexPosition == new Vector2(borderData[i].vertexPosition.x, borderData[i].vertexPosition.y + 1))
//                     {
//                         _hasNeighbours ++;
//                     }
//                     if (borderData[i].vertexPosition == new Vector2(borderData[i].vertexPosition.x + 1, borderData[i].vertexPosition.y + 1))
//                     {
//                         _hasNeighbours ++;
//                     }
//                 }catch{}
                
//                 if (_hasNeighbours == 8)
//                 {
//                     removeVertices.Add(borderData[i].vertexPosition);
//                 }
//             }
            
//             for (int j = 0; j < removeVertices.Count; j ++)
//             {
//                 for (int i = 0; i < borderData.Count; i ++)
//                 {
//                     if (borderData[i].vertexPosition == (removeVertices[j]))
//                     {
//                         borderData.RemoveAt(i);
//                     }
//                 }
//             }
//         }
        
    
        
//         static Mesh UpdateMesh()
//         {
//             var _mesh = new Mesh();
            
//             _mesh.Clear ();
//             _mesh.vertices = newVertices.ToArray();
//             _mesh.triangles = newTriangles.ToArray();
    
//             _mesh.Optimize ();
//             _mesh.RecalculateNormals ();
//             _mesh.RecalculateBounds();
            
//             squareCount=0;
//             newVertices.Clear();
//             newTriangles.Clear();
            
//             return _mesh;
//         }
        
//     }
// }