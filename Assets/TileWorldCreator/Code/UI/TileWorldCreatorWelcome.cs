
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
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using GiantGrey.TileWorldCreator.Utilities;
using GiantGrey.TileWorldCreator.UI;
using System;

namespace GiantGrey.TileWorldCreator
{
    public class TileWorldCreatorWelcome :  EditorWindow 
    {
        VisualElement root;

        List<string> choices = new List<string>() { "On startup", "Never" };

        int selectedDropdownIndex;

      
        Color colWhiteTransparent2 = new Color(255f/255f, 255f/255f, 255f/255f, 120f/255f);
        Color colWhiteTransparent = new Color(255f/255f, 255f/255f, 255f/255f, 50f/255f);

        string assetStoreLink = "https://u3d.as/3xzT";
        string gettingStartedLink = "https://giantgrey.gitbook.io/tileworldcreator-v4-documentation/installation-and-getting-started/your-first-world";
        string documentationLink = "https://giantgrey.gitbook.io/tileworldcreator-v4-documentation";
        string discordLink = "https://discord.gg/a5uf3nM";
        string databrainLink = "https://assetstore.unity.com/packages/tools/game-toolkits/databrain-ultimate-scriptable-objects-framework-244557";
        string pathgridLink = "https://assetstore.unity.com/packages/tools/level-design/pathgrid-277374";
        string marzLink = "https://store.steampowered.com/app/682530/MarZ_Tactical_Base_Defense/";


        [InitializeOnLoadMethod]
        public static void Init() 
        {
            
            // Very first time after installation and compilation
            var _keyValueFirstTime = EditorPrefs.GetBool("TileWorldCreatorWelcomeFirstTime");

            if (!_keyValueFirstTime)
            {
                EditorPrefs.SetBool("TileWorldCreatorWelcomeFirstTime", true);
                ShowWindow();
            }  
        }

        static TileWorldCreatorWelcome()
        {
            EditorApplication.delayCall += ShowOnStartup;
        }


        private static void ShowOnStartup()
        {
            var _keyValue = EditorPrefs.GetString("TileWorldCreatorWelcome");
            var _keyValueFirstTime = SessionState.GetBool("TileWorldCreatorWelcomeFirstTime", false);

            if (!_keyValueFirstTime && _keyValue == "startup") 
            {
                SessionState.SetBool("TileWorldCreatorWelcomeFirstTime", true);
                ShowWindow();
            }
        }

        [MenuItem("Tools/TileWorldCreator/Welcome", false, 200)]
        static void ShowWindow()
        {
            EditorWindow wnd = EditorWindow.CreateWindow<TileWorldCreatorWelcome>();
            wnd.titleContent = new GUIContent("TileWorldCreator - Welcome");
            wnd.maxSize = new Vector2(550f, 900f);
            wnd.minSize = wnd.maxSize;
            wnd.Show();
        }

        public void CreateGUI()
        {
            root = rootVisualElement;
            root.style.backgroundImage = TileWorldCreatorUtilities.LoadImage("WelcomeBackground.twc");
            var _bs = new BackgroundSize();
            _bs.sizeType = BackgroundSizeType.Cover;
            root.style.backgroundSize = _bs;

            WelcomeScreen();
        }

        public void WelcomeScreen()
        {
            root.Clear();

            var _main = new VisualElement();
            _main.style.flexGrow = 1;
            _main.style.alignItems = Align.Center;
            

            var _header = new VisualElement();
            _header.style.marginBottom = 2;
            _header.style.flexGrow = 1;
            _header.style.alignItems = Align.Center;
            _header.style.maxHeight = 180;
            _header.style.width = 500;

            var _logo = new VisualElement();
            _logo.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);//BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleToFit);
            _logo.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);//BackgroundPropertyHelper.ConvertScaleModeToBackgroundPosition(ScaleMode.ScaleToFit);   
            _logo.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);  //BackgroundPropertyHelper.ConvertScaleModeToBackgroundSize(ScaleMode.ScaleToFit);
            _logo.style.backgroundImage = TileWorldCreatorUtilities.LoadImage("LogoBannerWelcome.twc");
            _logo.style.width = 540;
            _logo.style.height = 140;
            _logo.style.marginTop = 20;

            _header.Add(_logo);

            var _headerLabel = new Label();
            _headerLabel.text = "Thank you for choosing TileWorldCreator ❤";
            _headerLabel.style.fontSize = 18;
            _headerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _headerLabel.style.whiteSpace = WhiteSpace.Normal;
            _headerLabel.style.marginLeft = 10;
            _headerLabel.style.marginRight = 10;
            _headerLabel.style.marginTop = 10;

            _header.Add(_headerLabel);

            _main.Add(_header);

        

            var _reviewCard = CreateCard("<size=16><b>Support us</b></size>" + "\n" +"<size=12>If you like TileWorldCreator, please consider leaving a review! This will help further development and maintenance of the plugin considerably. Thank you!</size>", assetStoreLink, TileWorldCreatorUtilities.LoadImage("RatingIcon.twc"), colWhiteTransparent);
            var _card1 = CreateCard("<size=16><b>Getting started</b></size>" + "\n" + "Check out the quick getting started video to start right away.", gettingStartedLink, TileWorldCreatorUtilities.LoadImage("blueprintLayers.twc"), colWhiteTransparent);
            var _card2 = CreateCard("<size=16><b>Documentation</b></size>" + "\n" + "Visit the official documentation to learn more about TileWorldCreator", documentationLink, TileWorldCreatorUtilities.LoadImage("buildLayers.twc"), colWhiteTransparent);
            // var _card3 = CreateCard("<size=16><b>Samples</b></size>" + "\n" + "Check out our samples to learn how to use TileWorldCreator", null, TileWorldCreatorUtilities.LoadImage("samples.twc"), colWhiteTransparent, SamplesUI);
            var _card4 = CreateCard("<size=16><b>Feedback & Support</b></size>" + "\n" + "Join our Discord community to share your creations or get help. \nOr write an email to: <b><a href='mailto:3f5J1@example.com'>MailAddress</a></b>", discordLink, TileWorldCreatorUtilities.LoadImage("discord_icon.twc"), colWhiteTransparent);

            _main.Add(_reviewCard);
            _main.Add(_card1);
            _main.Add(_card2);
            // _main.Add(_card3);
            _main.Add(_card4);
        
            var _moreLabel = new Label("More by");
            _moreLabel.style.fontSize = 14;
            _moreLabel.style.marginTop = 20;
            _moreLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            var _giantgrey = new VisualElement();
            _giantgrey.style.backgroundImage = TileWorldCreatorUtilities.LoadImage("GiantGrey.twc");
            _giantgrey.style.width = 120;
            _giantgrey.style.height = 50;

            _main.Add(_moreLabel);
            _main.Add(_giantgrey);
            var _moreContainer = new VisualElement();
            _moreContainer.style.flexDirection = FlexDirection.Row;

            var _databrain = SmallCard("<size=12><b>Databrain</size></b>", databrainLink, TileWorldCreatorUtilities.LoadImage("DatabrainIcon.twc"), colWhiteTransparent);
            var _pathgrid = SmallCard("<b>PathGrid</b>", pathgridLink, TileWorldCreatorUtilities.LoadImage("PathGridIcon.twc"), colWhiteTransparent);
            var _marz = SmallCard("<b>Tactical Base Defense</b>", marzLink, TileWorldCreatorUtilities.LoadImage("MarZIcon.twc"), colWhiteTransparent);

            
            _moreContainer.Add(_databrain);
            _moreContainer.Add(_pathgrid);
            _moreContainer.Add(_marz);

            _main.Add(_moreContainer);

            var _footer = new VisualElement(); 
            _footer.style.flexDirection = FlexDirection.Row;
            _footer.style.maxHeight = 20;
            _footer.style.height = 20;
            _footer.style.flexGrow = 1;
            _footer.style.backgroundColor = EditorGUIUtility.isProSkin ? new Color(50f/255f, 50f/255f, 50f/255f) : TileWorldCreatorColor.UltraLightGrey.GetColor();



            var _dropdown = new DropdownField(choices, selectedDropdownIndex);
            _dropdown.label = "Show window:";
            _dropdown.RegisterValueChangedCallback(x =>
            {
                if (x.newValue != x.previousValue)
                {
                    switch (_dropdown.index)
                    {
                        case 0:
                            EditorPrefs.SetString("TileWorldCreatorWelcome", "startup");
                            break;
                        case 1:
                            EditorPrefs.SetString("TileWorldCreatorWelcome", "never");
                            break;
                    }
                }
            });

            var _selectedDropdown = EditorPrefs.GetString("TileWorldCreatorWelcome");
            switch (_selectedDropdown)
            {
                case "startup":
                    _dropdown.index = 0;
                    break;
                case "never":
                    _dropdown.index = 1;
                    break;
            }

            _footer.Add(_dropdown);

            root.Add(_main);
            root.Add(_footer);
        }

        public void SamplesUI()
        {
            root.Clear();

            var _main = new VisualElement();
            _main.style.flexGrow = 1;
            _main.style.alignItems = Align.Center;

            var _backButton =  TileWorldCreatorUIElements.FlatButton("<size=18><b><</b></size>", null, 0, 0);
            _backButton.style.maxHeight = 40;
            _backButton.style.width = 550f;
            _backButton.style.flexGrow = 1;
            _backButton.RegisterCallback<ClickEvent>(evt => 
            {
                WelcomeScreen();
            });

            var _infoLabel = new Label("Samples");
            _infoLabel.text = "Samples must be installed from the Samples tab in the Package Manager. \nOpen the package manager, select - In Project - choose TileWorldCreator and click on Samples.";
            _infoLabel.style.fontSize = 14;
            _infoLabel.style.marginTop = 20;
            _infoLabel.style.backgroundColor = TileWorldCreatorColor.DarkBlue.GetColor();
            _infoLabel.style.whiteSpace = WhiteSpace.Normal;
            _infoLabel.SetPadding (10, 10, 10, 10);
            _infoLabel.SetMargin (10, 10, 10, 10);
            _infoLabel.SetRadius(10, 10, 10, 10);

            var _openButton = new Button();
            _openButton.text = "Open Package Manager";
            _openButton.style.height = 30;
            _openButton.RegisterCallback<ClickEvent>(evt => 
            {
                UnityEditor.PackageManager.UI.Window.Open("Point Grass Renderer");
            });

            var _samplesHeader = new Label();
            _samplesHeader.text = "Samples";
            _samplesHeader.style.fontSize = 18;
            _samplesHeader.style.marginTop = 20;
            _samplesHeader.style.whiteSpace = WhiteSpace.Normal;
            _samplesHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            _samplesHeader.SetMargin (20, 10, 10, 10);

            var _samplesScrollView = new ScrollView();
            _samplesScrollView.SetMargin(10, 20, 10, 10);
            _samplesScrollView.SetPadding(10, 10, 10, 10);

            var _cardSampleIsland = CreateCard("<size=16><b>Stylized Island</b></size>\nThis sample demonstrates a nice island diorama scene. It uses tile overrides to mix tilesets and waterfall objects.", "",  TileWorldCreatorUtilities.LoadImage("samplePreviewIsland.png"), colWhiteTransparent);
            var _cardSampleDeepRock = CreateCard("<size=16><b>Deep Rock</b></size>\nThis sample demonstrates runtime map modifications. You can walk around and break tiles at runtime. The map gets updated on the flow.", "", TileWorldCreatorUtilities.LoadImage("samplePreviewDeepRock.png"), colWhiteTransparent);
            var _cardSampleCliffIsland = CreateCard("<size=16><b>Cliff Island</b></size>\nA nice sample scene to demonstrate the cliff tiles with the cellular automata algorithm.", "", TileWorldCreatorUtilities.LoadImage("samplePreviewCliffIsland.png"), colWhiteTransparent);
            var _cardSampleRuntimeEditor = CreateCard("<size=16><b>Runtime Editor</b></size>\nThis sample demonstrates a runtime editor which allows you to create a tile-map at runtime using the API.", "", TileWorldCreatorUtilities.LoadImage("samplePreviewEditor.png"), colWhiteTransparent);
            var _cardSampleMixTilesets = CreateCard("<size=16><b>Mix Tilesets</b></size>\nA simple scene which demonstrates the use of tile overrides to mix tilesets.", "", TileWorldCreatorUtilities.LoadImage("samplePreviewMixTilesets.png"), colWhiteTransparent);
            var _cardSamplePathfinding = CreateCard("<size=16><b>Pathfinding</b></size>\nA sample scene which demonstrates the use of the pathfinding algorithm.", "", TileWorldCreatorUtilities.LoadImage("samplePreviewPathfinding.png"), colWhiteTransparent);
            var _cardSampleRamps = CreateCard("<size=16><b>Ramps</b></size>\nA sample scene which demonstrates how to add ramps.", "", TileWorldCreatorUtilities.LoadImage("samplePreviewRamps.png"), colWhiteTransparent);

            _samplesScrollView.Add(_cardSampleIsland);
            _samplesScrollView.Add(_cardSampleDeepRock);
            _samplesScrollView.Add(_cardSampleCliffIsland);
            _samplesScrollView.Add(_cardSampleRuntimeEditor);
            _samplesScrollView.Add(_cardSampleMixTilesets);
            _samplesScrollView.Add(_cardSamplePathfinding);
            _samplesScrollView.Add(_cardSampleRamps);
        
            _main.Add(_backButton);
            _main.Add(_samplesHeader);
            _main.Add(_infoLabel);
            _main.Add(_openButton);
            _main.Add(_samplesScrollView);

            root.Add(_main);
        }


        VisualElement CreateCard(string _text, string _link, Texture2D _icon, Color _color, Action _onClick = null)
        {
            var _card = new VisualElement();
            _card.style.backgroundColor = _color;
            _card.SetRadius(10, 10, 10, 10);
            _card.SetPadding(10, 10, 10, 10);
            _card.style.marginTop = 10;
            _card.style.width = 480;
            _card.style.height = 100;
            _card.style.flexDirection = FlexDirection.Row;
            
            _card.RegisterCallback<MouseEnterEvent>(evt => 
            {
                _card.style.backgroundColor = colWhiteTransparent2;
            });

            _card.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                _card.style.backgroundColor = _color;
            });

            _card.RegisterCallback<MouseDownEvent>(evt => 
            {
                if (!string.IsNullOrEmpty(_link))
                {
                    Application.OpenURL(_link);
                }

                _onClick?.Invoke();
            });

            var _cardImage = new VisualElement();
            _cardImage.style.backgroundImage = _icon;
            _cardImage.style.minWidth = 80;
            _cardImage.style.maxWidth = 80;
            _cardImage.style.height = 80;
            _cardImage.SetMargin(0, 0, 0, 10);
            // _card2Image.SetBorder(1);

            var _cardLabel = new Label();
            _cardLabel.text = _text;
            _cardLabel.style.whiteSpace = WhiteSpace.Normal;
            _cardLabel.style.flexShrink = 1;

            _card.Add(_cardImage);
            _card.Add(_cardLabel);

            return _card;
        }

        VisualElement SmallCard(string _text, string _link, Texture2D _icon, Color _color)
        {
            var _card = new VisualElement();
            _card.style.backgroundColor = _color;
            _card.SetRadius(10, 10, 10, 10);
            _card.SetPadding(10, 10, 10, 10);
            _card.SetMargin(0, 0, 2, 2);
            _card.style.marginTop = 10;
            _card.style.width = 100;
            _card.style.height = 120;
            _card.style.flexDirection = FlexDirection.Column;
            
            _card.RegisterCallback<MouseEnterEvent>(evt => 
            {
                _card.style.backgroundColor = colWhiteTransparent2;
            });

            _card.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                _card.style.backgroundColor = _color;
            });

            _card.RegisterCallback<MouseDownEvent>(evt => 
            {
                Application.OpenURL(_link);
            });


            var _cardImage = new VisualElement();
            _cardImage.style.backgroundImage = _icon;
            _cardImage.style.minWidth = 80;
            _cardImage.style.height = 80;
            var _bs = new BackgroundSize();
            _bs.sizeType = BackgroundSizeType.Cover;
            _cardImage.style.backgroundSize = _bs;
            _cardImage.SetMargin(0, 0, 0, 10);
            // _card2Image.SetBorder(1);

            var _cardLabel = new Label();
            _cardLabel.text = _text;
            _cardLabel.style.whiteSpace = WhiteSpace.Normal;
            _cardLabel.style.flexShrink = 1;
            _cardLabel.style.marginTop = 4;
            _cardLabel.style.unityTextAlign = TextAnchor.MiddleCenter;

            _card.Add(_cardImage);
            _card.Add(_cardLabel);

            return _card;
        }
    }
}
#endif