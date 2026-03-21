
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

using UnityEngine;

namespace GiantGrey.TileWorldCreator
{
   [CreateAssetMenu(menuName = "TileWorldCreator/TilePreset")]
   public class TilePreset : ScriptableObject
   {
      public Texture2D previewThumbnail;

      public enum GridType
      {
         standard,
         dual
      }

      public GridType gridtype;

      public GameObject DUALGRD_cornerTile;
      public GameObject DUALGRD_invertedCornerTile;
      public GameObject DUALGRD_edgeTile;
      public GameObject DUALGRD_fillTile;
      public GameObject DUALGRD_doubleInteriorCornerTile;


      public GameObject NRMGRD_deadEndTile;
      public GameObject NRMGRD_singleTile;
      public GameObject NRMGRD_fillTile;
      public GameObject NRMGRD_cornerWayTile;   
      public GameObject NRMGRD_cornerFillTile;
      public GameObject NRMGRD_interiorCornerTile;
      public GameObject NRMGRD_doubleCornerTile;
      public GameObject NRMGRD_edgeWayTile;
      public GameObject NRMGRD_edgeFillTile;
      public GameObject NRMGRD_threeWayTile;
      public GameObject NRMGRD_threeWayFillTile;
      public GameObject NRMGRD_edgeCornerFillTile;
      public GameObject NRMGRD_threeCornerTile;
      public GameObject NRMGRD_fourWayTile;
      
      [SerializeField, HideInInspector]
      private Material materialOverride;


      public float cornerTileYRotationOffset;
      public float invertedCornerTileYRotationOffset;
      public float edgeTileYRotationOffset;
      public float fillTileYRotationOffset;
      public float doubleInteriorCornerTileYRotationOffset;


      public float NRMGRD_deadEndTileYRotationOffset;
      public float NRMGRD_singleTileYRotationOffset;
      public float NRMGRD_fillTileYRotationOffset;
      public float NRMGRD_cornerWayTileYRotationOffset;
      public float NRMGRD_cornerFillTileYRotationOffset;
      public float NRMGRD_interiorCornerTileYRotationOffset;
      public float NRMGRD_doubleCornerTileYRotationOffset;
      public float NRMGRD_edgeWayTileYRotationOffset;
      public float NRMGRD_edgeFillTileYRotationOffset;
      public float NRMGRD_threeWayTileYRotationOffset;
      public float NRMGRD_threeWayFillTileYRotationOffset;
      public float NRMGRD_edgeCornerFillTileYRotationOffset;
      public float NRMGRD_threeCornerTileYRotationOffset;
      public float NRMGRD_fourWayTileYRotationOffset;

      public enum TileType
      {
         none,
         // Dual Grid Tile Types
         DUALGRD_corner,
         DUALGRD_interiorCorner,
         DUALGRD_doubleInteriorCorner,
         DUALGRD_edge,
         DUALGRD_fill,

         // Normal Grid Tile Types
         NRMGRD_deadEnd,
         NRMGRD_single,
         NRMGRD_fill,
         NRMGRD_cornerWay,
         NRMGRD_cornerFill,
         NRMGRD_interiorCorner,
         NRMGRD_doubleCorner,
         NRMGRD_edgeWay,
         NRMGRD_edgeFill,
         NRMGRD_threeWay,
         NRMGRD_threeWayFill,
         NRMGRD_edgeCornerFill,
         NRMGRD_threeCorner,
         NRMGRD_fourWay,
      }


      public GameObject GetTile(TileType _tileType, out float _rotationOffset) 
      {
         _rotationOffset = 0f;
         switch (_tileType) 
         {
            case TileType.DUALGRD_corner: _rotationOffset = cornerTileYRotationOffset; return DUALGRD_cornerTile;
            case TileType.DUALGRD_interiorCorner: _rotationOffset = invertedCornerTileYRotationOffset; return DUALGRD_invertedCornerTile;
            case TileType.DUALGRD_edge: _rotationOffset = edgeTileYRotationOffset; return DUALGRD_edgeTile;
            case TileType.DUALGRD_fill: _rotationOffset = fillTileYRotationOffset; return DUALGRD_fillTile;
            case TileType.DUALGRD_doubleInteriorCorner: _rotationOffset = doubleInteriorCornerTileYRotationOffset; return DUALGRD_doubleInteriorCornerTile;


            case TileType.NRMGRD_cornerFill: _rotationOffset = NRMGRD_cornerFillTileYRotationOffset; return NRMGRD_cornerFillTile;
            case TileType.NRMGRD_cornerWay: _rotationOffset = NRMGRD_cornerWayTileYRotationOffset; return NRMGRD_cornerWayTile;
            case TileType.NRMGRD_deadEnd: _rotationOffset = NRMGRD_deadEndTileYRotationOffset; return NRMGRD_deadEndTile;
            case TileType.NRMGRD_edgeFill: _rotationOffset = NRMGRD_edgeFillTileYRotationOffset; return NRMGRD_edgeFillTile;
            case TileType.NRMGRD_edgeWay: _rotationOffset = NRMGRD_edgeWayTileYRotationOffset; return NRMGRD_edgeWayTile;
            case TileType.NRMGRD_fill: _rotationOffset = NRMGRD_fillTileYRotationOffset; return NRMGRD_fillTile;
            case TileType.NRMGRD_single: _rotationOffset = NRMGRD_singleTileYRotationOffset; return NRMGRD_singleTile;
            case TileType.NRMGRD_threeCorner: _rotationOffset = NRMGRD_threeCornerTileYRotationOffset; return NRMGRD_threeCornerTile;
            case TileType.NRMGRD_threeWay: _rotationOffset = NRMGRD_threeWayTileYRotationOffset; return NRMGRD_threeWayTile;
            case TileType.NRMGRD_threeWayFill: _rotationOffset = NRMGRD_threeWayFillTileYRotationOffset; return NRMGRD_threeWayFillTile;
            case TileType.NRMGRD_edgeCornerFill: _rotationOffset = NRMGRD_edgeCornerFillTileYRotationOffset; return NRMGRD_edgeCornerFillTile;
            case TileType.NRMGRD_fourWay: _rotationOffset = NRMGRD_fourWayTileYRotationOffset; return NRMGRD_fourWayTile;
            case TileType.NRMGRD_interiorCorner: _rotationOffset = NRMGRD_interiorCornerTileYRotationOffset; return NRMGRD_interiorCornerTile;
            case TileType.NRMGRD_doubleCorner: _rotationOffset = NRMGRD_doubleCornerTileYRotationOffset; return NRMGRD_doubleCornerTile;
            


            // case TileType.none: return noneTile;
            default: return null;
         }
      }

      public Material GetMaterialOverride()
      {
         return materialOverride;
      }
   }
}