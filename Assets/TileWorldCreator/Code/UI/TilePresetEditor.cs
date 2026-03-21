
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
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

using GiantGrey.TileWorldCreator.UI;
using GiantGrey.TileWorldCreator.Utilities;

namespace GiantGrey.TileWorldCreator
{
    [CustomEditor(typeof(TilePreset))]
    public class TilePresetEditor : Editor
    {
        TilePreset tilePreset;
        VisualElement normalGridContainer;
        VisualElement dualGridContainer;

        Texture2D dualGridIcon;
        Texture2D normalGridIcon;
        Texture2D DUALGRD_cornerTileIcon;
        Texture2D DUALGRD_invertedCornerTileIcon;
        Texture2D DUALGRD_edgeTileIcon;
        Texture2D DUALGRD_fillTileIcon; 
        Texture2D DUALGRD_mergedCornerTileIcon;

        Texture2D NRMGRID_cornerTileIcon;
        Texture2D NRMGRID_cornerFillTileIcon;
        Texture2D NRMGRID_edgeTileIcon;
        Texture2D NRMGRID_edgeFillTileIcon;
        Texture2D NRMGRID_deadEndIcon;
        Texture2D NRMGRID_fillTileIcon;
        Texture2D NRMGRID_threeWayTileIcon;
        Texture2D NRMGRID_threeWayFillTileIcon;
        Texture2D NRMGRID_singleTileIcon;
        Texture2D NRMGRID_interiorCornerTileIcon;
        Texture2D NRMGRID_fourWayTileIcon;
        Texture2D NRMGRID_edgeCornerFillTileIcon;
        Texture2D NRMGRID_threeCornerTileIcon;
        Texture2D NRMGRID_doubleCornerTileIcon;

        public void OnEnable()
        {
            tilePreset = (TilePreset)target;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var _root = new VisualElement();

            Color colDarkGrey = new Color(40f/255f, 40f/255f, 40f/255f);

            LoadIcons();

            var _header = new VisualElement();
            _header.style.flexDirection = FlexDirection.Row;
            _header.style.backgroundColor = EditorGUIUtility.isProSkin
                ? TileWorldCreatorColor.DarkGrey.GetColor()
                : TileWorldCreatorColor.VeryLightGrey.GetColor(); //colDarkGrey;
            _header.SetPadding (5, 5, 5, 5);

            var _presetIcon = new VisualElement();
            _presetIcon.style.backgroundImage = TileWorldCreatorUtilities.LoadImage("TilePreset.png");
            _presetIcon.style.width = 40;
            _presetIcon.style.height = 40;
            _presetIcon.style.marginRight = 10;
            _presetIcon.style.marginLeft = 15;
            _presetIcon.style.marginTop = 38;

            _header.Add(_presetIcon);


            var _title = new Label();
            _title.text = "Tile Preset";
            _title.style.fontSize = 18;
            _title.style.unityFontStyleAndWeight = FontStyle.Bold;
            _title.SetMargin(46, 10, 0, 0);

            _header.Add(_title);

            _root.Add(_header);      

             var _materialOverride = new PropertyField();
            _materialOverride.BindProperty(serializedObject.FindProperty("materialOverride"));
            _materialOverride.SetMargin( 10, 10, 0, 0);
            _materialOverride.SetPadding(5, 5, 5, 5);
            _materialOverride.style.backgroundColor = EditorGUIUtility.isProSkin
                ? TileWorldCreatorColor.DarkGrey.GetColor()
                : TileWorldCreatorColor.VeryLightGrey.GetColor();
            _root.Add(_materialOverride);


            var _thumbnailContainer = new VisualElement();

            // _thumbnailContainer.style.width = 60;
            // _thumbnailContainer.style.flexGrow = 1;
            _thumbnailContainer.style.height = 100;
            _thumbnailContainer.SetMargin( 10, 10, 0, 0);
            _thumbnailContainer.SetPadding(5, 5, 5, 5);
            _thumbnailContainer.style.backgroundColor = EditorGUIUtility.isProSkin
                ? TileWorldCreatorColor.DarkGrey.GetColor()
                : TileWorldCreatorColor.VeryLightGrey.GetColor();
            _thumbnailContainer.style.alignItems = Align.FlexEnd;

            // var _thumbnailLabel = new Label();
            // _thumbnailLabel.text = "Thumb";
            // _thumbnailLabel.style.fontSize = 12;
            // _thumbnailLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            // _thumbnailContainer.Add(_thumbnailLabel);

         

            var _previewThumbnail = new VisualElement();
            if (tilePreset.previewThumbnail != null)
            {
                _previewThumbnail.style.backgroundImage = tilePreset.previewThumbnail;
            }
            var _bs = new BackgroundSize();
            _bs.sizeType = BackgroundSizeType.Contain;
            _previewThumbnail.style.backgroundSize = _bs;
            _previewThumbnail.style.width = 256;
            _previewThumbnail.style.maxWidth = 256;
            _previewThumbnail.style.height = 120;
            var _bp = new BackgroundPosition();
            _bp.keyword = BackgroundPositionKeyword.Right;
            _previewThumbnail.style.backgroundPositionX = _bp;

            var _objField = new ObjectField { allowSceneObjects = false };
            _objField.name = "Hidden Object field";
            _objField.style.display = DisplayStyle.None;
            _objField.BindProperty(serializedObject.FindProperty(nameof(tilePreset.previewThumbnail)));
            _objField.RegisterValueChangedCallback(x =>
            {
                _previewThumbnail.style.backgroundImage = tilePreset.previewThumbnail;
            });

            // build the picker button (rip it out of the object field)
            // VisualElement _setThumbnail = _objField[0][1];
            // _setThumbnail.name = "Obj Picker Button";
            // _setThumbnail.style.flexGrow = 0;
            // _setThumbnail.style.flexShrink = 0;
            // _setThumbnail.tooltip = "Pick this asset file manually";
          

            var _setThumbnail = new PropertyField();
            _setThumbnail.label = "";
            _setThumbnail.style.width = 160;
            _setThumbnail.BindProperty(serializedObject.FindProperty(nameof(tilePreset.previewThumbnail)));
            _setThumbnail.RegisterCallback<ChangeEvent<Object>>(x =>
            {
                _previewThumbnail.style.backgroundImage = tilePreset.previewThumbnail;
            });

            _thumbnailContainer.Add(_previewThumbnail);
            _thumbnailContainer.Add(_setThumbnail);

            var _headerSeparator = new VisualElement();
            _headerSeparator.style.flexGrow = 1;

            _header.Add(_headerSeparator);
            _header.Add(_thumbnailContainer);

            var _gridTypeContainer = new VisualElement();
            _gridTypeContainer.style.flexGrow = 1;
            _gridTypeContainer.style.flexDirection = FlexDirection.Row;
            _gridTypeContainer.SetMargin( 10, 10, 0, 0);
            _gridTypeContainer.style.alignItems = Align.Center;

            var _gridTypeIcon = new VisualElement();
            _gridTypeIcon.style.backgroundImage = tilePreset.gridtype == TilePreset.GridType.dual ? dualGridIcon : normalGridIcon;
            _gridTypeIcon.style.width = 40;
            _gridTypeIcon.style.height = 40;
            _gridTypeIcon.style.marginRight = 10;

            var _gridType = new EnumField();
            _gridType.style.flexGrow = 1;
            _gridType.BindProperty(serializedObject.FindProperty(nameof(tilePreset.gridtype)));
            _gridType.RegisterValueChangedCallback(x =>
            {
                tilePreset.gridtype = (TilePreset.GridType)x.newValue; 
                normalGridContainer.style.display = tilePreset.gridtype == TilePreset.GridType.standard ? DisplayStyle.Flex : DisplayStyle.None;
                dualGridContainer.style.display = tilePreset.gridtype == TilePreset.GridType.dual ? DisplayStyle.Flex : DisplayStyle.None;
                _gridTypeIcon.style.backgroundImage = tilePreset.gridtype == TilePreset.GridType.dual ? dualGridIcon : normalGridIcon;
            });

            _gridTypeContainer.Add(_gridTypeIcon);
            _gridTypeContainer.Add(_gridType);


            BuildNormalGridUI();
            BuildDualGridUI();


            _root.Add(_gridTypeContainer);
            _root.Add(dualGridContainer);
            _root.Add(normalGridContainer);

            normalGridContainer.style.display = tilePreset.gridtype == TilePreset.GridType.standard ? DisplayStyle.Flex : DisplayStyle.None;
            dualGridContainer.style.display = tilePreset.gridtype == TilePreset.GridType.dual ? DisplayStyle.Flex : DisplayStyle.None;

            return _root;
        }

        void BuildNormalGridUI()
        {
            if (normalGridContainer != null)
            {
                normalGridContainer.Clear();
            }
            else
            {
                normalGridContainer = new VisualElement();
                normalGridContainer.style.flexDirection = FlexDirection.Row;
                normalGridContainer.style.flexWrap = Wrap.Wrap;
            }

            normalGridContainer.Add(Tile(nameof(tilePreset.NRMGRD_singleTile)));
            normalGridContainer.Add(Tile(nameof(tilePreset.NRMGRD_deadEndTile)));
            normalGridContainer.Add(Tile(nameof(tilePreset.NRMGRD_edgeWayTile)));
            normalGridContainer.Add(Tile (nameof(tilePreset.NRMGRD_cornerWayTile)));
            normalGridContainer.Add(Tile(nameof(tilePreset.NRMGRD_threeWayTile)));
            normalGridContainer.Add(Tile(nameof(tilePreset.NRMGRD_fourWayTile)));

            normalGridContainer.Add(Tile(nameof(tilePreset.NRMGRD_edgeFillTile)));
            normalGridContainer.Add(Tile(nameof(tilePreset.NRMGRD_threeWayFillTile)));
            normalGridContainer.Add(Tile(nameof(tilePreset.NRMGRD_cornerFillTile)));
            
            normalGridContainer.Add(Tile(nameof(tilePreset.NRMGRD_interiorCornerTile)));
            normalGridContainer.Add(Tile(nameof(tilePreset.NRMGRD_edgeCornerFillTile)));            
            normalGridContainer.Add(Tile(nameof(tilePreset.NRMGRD_fillTile)));
            
            
            normalGridContainer.Add(Tile(nameof(tilePreset.NRMGRD_doubleCornerTile)));
            normalGridContainer.Add(Tile(nameof(tilePreset.NRMGRD_threeCornerTile)));
        }

        void BuildDualGridUI()
        {
            if (dualGridContainer != null)
            {
                dualGridContainer.Clear();
            }
            else
            {
                dualGridContainer = new VisualElement();
                dualGridContainer.style.flexDirection = FlexDirection.Row;
                dualGridContainer.style.flexWrap = Wrap.Wrap;
            }
        

            dualGridContainer.Add( Tile(nameof(tilePreset.DUALGRD_edgeTile)));
            dualGridContainer.Add(Tile (nameof(tilePreset.DUALGRD_cornerTile)));
            dualGridContainer.Add(Tile(nameof(tilePreset.DUALGRD_invertedCornerTile)));
            dualGridContainer.Add(Tile(nameof(tilePreset.DUALGRD_fillTile)));
            dualGridContainer.Add(Tile(nameof(tilePreset.DUALGRD_doubleInteriorCornerTile)));

        }

        VisualElement Tile (string _propertyName)
        {
            var _tile = new VisualElement();
            _tile.style.maxWidth = 130;
            _tile.SetBorder(1);
            _tile.SetPadding(5, 5, 5, 5);
            _tile.SetMargin(5, 5, 5, 5);
            // _tile.style.backgroundColor = new Color(40f/255f, 40f/255f, 40f/255f);

            var _icon = new VisualElement();
            _icon.style.width = 120;
            _icon.style.height = 120;
            _icon.SetMargin(2, 4, 0, 0);

            var _title = new Label();
            _title.style.unityFontStyleAndWeight = FontStyle.Bold;
            _title.style.fontSize = 12;

            string _rotationPropertyName = "";
            switch (_propertyName)
            {
                // DUAL GRID
                case nameof(tilePreset.DUALGRD_edgeTile):
                    _icon.style.backgroundImage = DUALGRD_edgeTileIcon;
                    _title.text = "Edge Tile";
                    _rotationPropertyName = nameof(tilePreset.edgeTileYRotationOffset);
                    break;
                case nameof(tilePreset.DUALGRD_fillTile):
                    _icon.style.backgroundImage = DUALGRD_fillTileIcon;
                    _title.text = "Fill Tile";
                    _rotationPropertyName = nameof(tilePreset.fillTileYRotationOffset);
                    break;
                case nameof(tilePreset.DUALGRD_cornerTile):
                    _icon.style.backgroundImage = DUALGRD_cornerTileIcon;
                    _title.text = "Corner Tile";
                    _rotationPropertyName = nameof(tilePreset.cornerTileYRotationOffset);
                    break;
                case nameof(tilePreset.DUALGRD_invertedCornerTile):
                    _icon.style.backgroundImage = DUALGRD_invertedCornerTileIcon;
                    _title.text = "Int. Corner Tile";
                    _rotationPropertyName = nameof(tilePreset.invertedCornerTileYRotationOffset);
                    break;
                case nameof(tilePreset.DUALGRD_doubleInteriorCornerTile):
                   _icon.style.backgroundImage = DUALGRD_mergedCornerTileIcon;
                   _title.text = "Double Corner Tile";
                   _rotationPropertyName = nameof(tilePreset.doubleInteriorCornerTileYRotationOffset);
                    break;

                // NRM GRID
                case nameof(tilePreset.NRMGRD_cornerFillTile):
                    _icon.style.backgroundImage = NRMGRID_cornerFillTileIcon;
                    _title.text = "Corner Fill Tile";
                    _rotationPropertyName = nameof(tilePreset.NRMGRD_cornerFillTileYRotationOffset);
                    break;
                case nameof(tilePreset.NRMGRD_cornerWayTile):
                    _icon.style.backgroundImage = NRMGRID_cornerTileIcon;
                    _title.text = "Corner Tile";
                    _rotationPropertyName = nameof(tilePreset.NRMGRD_cornerWayTileYRotationOffset);
                    break;
                case nameof(tilePreset.NRMGRD_deadEndTile):
                    _icon.style.backgroundImage = NRMGRID_deadEndIcon;
                    _title.text = "Dead End";
                    _rotationPropertyName = nameof(tilePreset.NRMGRD_deadEndTileYRotationOffset);
                    break;
                case nameof(tilePreset.NRMGRD_doubleCornerTile):
                    _icon.style.backgroundImage = NRMGRID_doubleCornerTileIcon;
                    _title.text = "Double Corner Tile";
                    _rotationPropertyName = nameof(tilePreset.NRMGRD_doubleCornerTileYRotationOffset);
                    break;
                case nameof(tilePreset.NRMGRD_edgeCornerFillTile):
                    _icon.style.backgroundImage = NRMGRID_edgeCornerFillTileIcon;
                    _title.text = "Edge Corner Fill";
                    _rotationPropertyName = nameof(tilePreset.NRMGRD_edgeCornerFillTileYRotationOffset);
                    break;
                case nameof(tilePreset.NRMGRD_edgeFillTile):
                    _icon.style.backgroundImage = NRMGRID_edgeFillTileIcon;
                    _title.text = "Edge Fill Tile";
                    _rotationPropertyName = nameof(tilePreset.NRMGRD_edgeFillTileYRotationOffset);
                    break;
                case nameof(tilePreset.NRMGRD_edgeWayTile):
                    _icon.style.backgroundImage = NRMGRID_edgeTileIcon;
                    _title.text = "Edge Tile";
                    _rotationPropertyName = nameof(tilePreset.NRMGRD_edgeWayTileYRotationOffset);
                    break;
                case nameof(tilePreset.NRMGRD_fillTile):
                    _icon.style.backgroundImage = NRMGRID_fillTileIcon;
                    _title.text = "Fill Tile";
                    _rotationPropertyName = nameof(tilePreset.NRMGRD_fillTileYRotationOffset);
                    break;
                case nameof(tilePreset.NRMGRD_fourWayTile):
                    _icon.style.backgroundImage = NRMGRID_fourWayTileIcon;
                    _title.text = "Four Way Tile";
                    _rotationPropertyName = nameof(tilePreset.NRMGRD_fourWayTileYRotationOffset);
                    break;
                case nameof(tilePreset.NRMGRD_interiorCornerTile):
                    _icon.style.backgroundImage = NRMGRID_interiorCornerTileIcon;
                    _title.text = "Int. Corner Tile";
                    _rotationPropertyName = nameof(tilePreset.NRMGRD_interiorCornerTileYRotationOffset);
                    break;
                case nameof(tilePreset.NRMGRD_singleTile):
                    _icon.style.backgroundImage = NRMGRID_singleTileIcon;
                    _title.text = "Single Tile";
                    _rotationPropertyName = nameof(tilePreset.NRMGRD_singleTileYRotationOffset);
                    break;
                case nameof(tilePreset.NRMGRD_threeCornerTile):
                    _icon.style.backgroundImage = NRMGRID_threeCornerTileIcon;
                    _title.text = "Tripple Corner Tile";
                    _rotationPropertyName = nameof(tilePreset.NRMGRD_threeCornerTileYRotationOffset);
                    break;
                case nameof(tilePreset.NRMGRD_threeWayFillTile):
                    _icon.style.backgroundImage = NRMGRID_threeWayFillTileIcon;
                    _title.text = "Three Way Fill Tile";
                    _rotationPropertyName = nameof(tilePreset.NRMGRD_threeWayFillTileYRotationOffset);
                    break;
                case nameof(tilePreset.NRMGRD_threeWayTile):
                    _icon.style.backgroundImage = NRMGRID_threeWayTileIcon;
                    _title.text = "Three Way Tile";
                    _rotationPropertyName = nameof(tilePreset.NRMGRD_threeWayTileYRotationOffset);
                    break;
            }


            var _property = new PropertyField();
            _property.BindProperty(serializedObject.FindProperty(_propertyName));
            _property.label = "";
            _property.SetMargin(0, 5, 0, 0);

            var _rotationProperty = new PropertyField();
            _rotationProperty.label = "";
            _rotationProperty.BindProperty(serializedObject.FindProperty(_rotationPropertyName));

            _tile.Add(_title);
            _tile.Add(_icon);
            _tile.Add(_property);
            _tile.Add(new Label("Y Rotation Offset:"));
            _tile.Add(_rotationProperty);

            return _tile;
        }

        void LoadIcons()
        {
            dualGridIcon = TileWorldCreatorUtilities.LoadImage("DualGrid.twc"); 
            normalGridIcon = TileWorldCreatorUtilities.LoadImage("NormalGrid.twc");
          

            NRMGRID_cornerTileIcon = TileWorldCreatorUtilities.LoadImage("NormalGrid_CornerTile.twc");
            NRMGRID_cornerFillTileIcon = TileWorldCreatorUtilities.LoadImage("NormalGrid_CornerFillTile.twc");
            NRMGRID_edgeTileIcon = TileWorldCreatorUtilities.LoadImage("NormalGrid_EdgeTile.twc");
            NRMGRID_edgeFillTileIcon = TileWorldCreatorUtilities.LoadImage("NormalGrid_EdgeFillTile.twc");
            NRMGRID_deadEndIcon = TileWorldCreatorUtilities.LoadImage("NormalGrid_DeadEndTile.twc");
            NRMGRID_fillTileIcon = TileWorldCreatorUtilities.LoadImage("NormalGrid_FillTile.twc");
            NRMGRID_fourWayTileIcon = TileWorldCreatorUtilities.LoadImage("NormalGrid_FourWayTile.twc");
            NRMGRID_interiorCornerTileIcon = TileWorldCreatorUtilities.LoadImage("NormalGrid_IntCornerTile.twc");
            NRMGRID_singleTileIcon = TileWorldCreatorUtilities.LoadImage("NormalGrid_SingleTile.twc");
            NRMGRID_threeCornerTileIcon = TileWorldCreatorUtilities.LoadImage("NormalGrid_ThreeCornerTile.twc");
            NRMGRID_threeWayFillTileIcon = TileWorldCreatorUtilities.LoadImage("NormalGrid_ThreeWayFillTile.twc");
            NRMGRID_threeWayTileIcon = TileWorldCreatorUtilities.LoadImage("NormalGrid_ThreeWayTile.twc");
            NRMGRID_edgeCornerFillTileIcon = TileWorldCreatorUtilities.LoadImage("NormalGrid_EdgeCornerFillTile.twc");
            NRMGRID_doubleCornerTileIcon = TileWorldCreatorUtilities.LoadImage("NormalGrid_DoubleCornerTile.twc");

            DUALGRD_edgeTileIcon = NRMGRID_edgeFillTileIcon;
            DUALGRD_fillTileIcon = NRMGRID_fillTileIcon;
            DUALGRD_cornerTileIcon = TileWorldCreatorUtilities.LoadImage("NormalGrid_CornerFillTileFlipped.twc");
            DUALGRD_invertedCornerTileIcon = NRMGRID_interiorCornerTileIcon;
            DUALGRD_mergedCornerTileIcon = NRMGRID_doubleCornerTileIcon;
        }
    }
}
#endif