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
using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using GiantGrey.TileWorldCreator.Utilities;

namespace GiantGrey.TileWorldCreator.UI
{
    public class LayerFoldoutElement : VisualElement
    {
        private VisualElement header;
        private VisualElement selector;
        private VisualElement arrowIcon;
        private Button removeButton;
        private Label title;
        private VisualElement container;
        private Button moveDown;
        private Button moveUp;
        private Color contentBGColor = new Color(50f/255f, 50f/255f, 50f/255f, 1f);
        

        private float totalHeight = 0;
        private Texture2D arrowRight;
        private Texture2D arrowDown;

        public Action<bool> onFoldout;

        private bool _value;
        public bool Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
                OpenFoldout(_value);
            }
        }

        private List<VisualElement> items = new List<VisualElement>();

        public LayerFoldoutElement(string _title, 
        SerializedProperty _enableProperty, 
        Configuration _asset, 
        ConfigurationEditor _editor, 
        string _layerGuid, string _folderGuid, Texture2D _previewTexture, bool _showPreviewTexture,
        Action<bool> _onFoldoutCallback, 
        Action _onRemoveCallback, 
        Action _onExecuteCallback, 
        Action _onClearCallback,
        Action _onMoveUpCallback,
        Action _onMoveDownCallback,
        Action _onDuplicateCallback,
        SerializedProperty _lockPaintProperty, Texture2D _customIcon = null, IManipulator _manipulator = null,
        params Action[] _additionalButtons)
        {
            totalHeight = 0;
            arrowRight = TileWorldCreatorUtilities.LoadImage("arrowRight.twc");
            arrowDown = TileWorldCreatorUtilities.LoadImage("arrowDown.twc");
            onFoldout = _onFoldoutCallback;

            style.flexGrow = 1;

            items = new List<VisualElement>();

            header = new VisualElement();

            if (_customIcon != null)
            {
                var _iconElement = new VisualElement();
                _iconElement.style.backgroundImage = _customIcon;
                _iconElement.style.width = 30;
                _iconElement.style.height = 30;
                _iconElement.SetMargin ( 0, 0, 5, 5);

                header.Add(_iconElement);
            }


            // header.style.marginTop = 5;
            header.style.height = 40;
            header.style.flexDirection = FlexDirection.Row;
            header.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.Grey.GetColor() : TileWorldCreatorColor.VeryLightGrey.GetColor();
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = TileWorldCreatorColor.DarkGrey.GetColor();
            header.style.alignItems = Align.Center;

            selector = new VisualElement();
            selector.style.height = 40;
            selector.style.alignItems = Align.Center;
            selector.style.flexDirection = FlexDirection.Row;
            selector.style.flexGrow = 1;
            selector.RegisterCallback<ClickEvent>(click =>
            {
                OpenFoldout(container.style.display == DisplayStyle.None ? true : false);
            });

            var _dragManipulator = TileWorldCreatorUIElements.FlatButtonAsVisualElement("", TileWorldCreatorUtilities.LoadImage("moveToFolder.twc"), 22, 22);
            _dragManipulator.style.height = 39;
            _dragManipulator.style.width = 40;
            _dragManipulator.AddManipulator (_manipulator);
            _dragManipulator.style.justifyContent = Justify.Center;
            _dragManipulator.style.alignItems = Align.Center;

            
            if (_showPreviewTexture)
            {
                if (_previewTexture != null)
                {
                    var _previewTextureElement = new VisualElement();
                    _previewTextureElement.SetBorder(2, Color.grey);
                    _previewTextureElement.style.backgroundColor = Color.black;
                    _previewTextureElement.style.backgroundImage = _previewTexture;
                    _previewTextureElement.style.width = 40;
                    _previewTextureElement.style.height = 40;
                    _previewTextureElement.style.minWidth = 40;


                    _previewTextureElement.RegisterCallback<ClickEvent>(evt => 
                    {
                        var _panel = new BlueprintPreviewImagePopup(_previewTexture);
                        BlueprintPreviewImagePopup.ShowPanel(Event.current.mousePosition, _panel);

                    });

                    header.Add(_previewTextureElement);
                }
                else
                {
                    var _previewTextureElement = new VisualElement();
                    _previewTextureElement.SetBorder(2, Color.grey);
                    _previewTextureElement.style.width = 40;
                    _previewTextureElement.style.height = 40;
                    header.Add(_previewTextureElement);
                }
            }

            if (_enableProperty != null)
            {
                var _isEnableToggle = new PropertyField();
                _isEnableToggle.label = "";
                _isEnableToggle.BindProperty(_enableProperty);

                header.Add(_isEnableToggle);
            }
            // if (_showPaintOption)
            // {
            //     var _enablePaint = new Button();
            //     _enablePaint.text = " P ";
            //     _enablePaint.RegisterCallback<ClickEvent>(evt => 
            //     {
            //         _onSelectCallback?.Invoke();
            //         Select();
            //     });

            //     header.Add(_enablePaint);
            // }

            // foldoutButton = DatabrainHelpers.DatabrainButton("");
            arrowIcon = new VisualElement();
            arrowIcon.style.backgroundImage = arrowRight;
            arrowIcon.style.width = 24;
            arrowIcon.style.height = 24;

          

            // header.RegisterCallback<ClickEvent>(click =>
            // {
            //     _onSelectCallback?.Invoke();

            //     Select();
            // });

            title = new Label();
            title.style.flexGrow = 1;
            title.text = _title;
            title.style.marginLeft = 10;
            title.style.unityTextAlign = TextAnchor.MiddleLeft;


            var _executeLayer = TileWorldCreatorUIElements.FlatButton("", TileWorldCreatorUtilities.LoadImage("execute.twc"), 22, 22);
            _executeLayer.style.width = 40;
            _executeLayer.style.height = 39;
            _executeLayer.tooltip = "Execute Layer";
            _executeLayer.RegisterCallback<ClickEvent>(click => 
            {
                
                _onExecuteCallback?.Invoke();
            });


            var _clearLayerButton = TileWorldCreatorUIElements.FlatButton("", TileWorldCreatorUtilities.LoadImage("ObjectsRemove.twc"), 24, 24);
            _clearLayerButton.style.width = 40;
            _clearLayerButton.style.height = 39;
            _clearLayerButton.tooltip = "Destroy Tiles";
            _clearLayerButton.SetPadding(4, 4, 4, 4);
            _clearLayerButton.RegisterCallback<ClickEvent>(click => 
            {
                _onClearCallback?.Invoke();
            });
            

            removeButton = TileWorldCreatorUIElements.FlatButton("", TileWorldCreatorUtilities.LoadImage("remove.twc"), 24, 24);
            removeButton.style.width = 39;
            removeButton.style.height = 39;
            removeButton.tooltip = "Remove Layer";
            removeButton.RegisterCallback<ClickEvent>(click => 
            {
                if (EditorUtility.DisplayDialog("Delete Layer?", "Do you really want to delete this layer?", "Yes", "No"))
                {
                    _onRemoveCallback?.Invoke();
                    _asset.layerChanged = true;
                    _asset.NotifyLayerChanged();
                }
            });

            moveDown = TileWorldCreatorUIElements.FlatButton("", TileWorldCreatorUtilities.LoadImage("down.twc"), 12, 12);
            moveDown.style.width = 24;
            moveDown.style.height = 39;
            moveDown.RegisterCallback<ClickEvent>(click => 
            {
                _onMoveDownCallback?.Invoke(); 
                _asset.layerChanged = true;
                _asset.NotifyLayerChanged();
            });

            moveUp = TileWorldCreatorUIElements.FlatButton("", TileWorldCreatorUtilities.LoadImage("up.twc"), 12, 12);
            moveUp.style.width = 24;
            moveUp.style.height = 39;
            moveUp.RegisterCallback<ClickEvent>(click => 
            {
                _onMoveUpCallback?.Invoke();
                _asset.layerChanged = true;
                _asset.NotifyLayerChanged();
            });

            var _duplicateLayerButton = TileWorldCreatorUIElements.FlatButton("", TileWorldCreatorUtilities.LoadImage("duplicateLayer.twc"), 20, 20);
            _duplicateLayerButton.style.width = 39;
            _duplicateLayerButton.style.height = 39;
            _duplicateLayerButton.tooltip = "Duplicate Layer";
            _duplicateLayerButton.RegisterCallback<ClickEvent>(click => 
            {
                _asset.layerChanged = true;
                _onDuplicateCallback?.Invoke();
                _asset.NotifyLayerChanged();
            });

            Button _lockPaintMode = null;
            if (_lockPaintProperty != null)
            {
                
                var _paintLock = TileWorldCreatorUtilities.LoadImage("paintLock.twc");
                var _paintUnlock = TileWorldCreatorUtilities.LoadImage("paintUnlock.twc");
                _lockPaintMode = TileWorldCreatorUIElements.FlatButton("", _lockPaintProperty.boolValue ? _paintLock : _paintUnlock, 24, 24);
                _lockPaintMode.tooltip = !_lockPaintProperty.boolValue ? "Lock out from paint mode" : "Unlock from paint mode";
                _lockPaintMode.style.width = 39;
                _lockPaintMode.style.height = 39;
                _lockPaintMode.RegisterCallback<ClickEvent>(evt => 
                {
                    if (_lockPaintProperty.boolValue)
                    {
                        _lockPaintProperty.boolValue = false;
                    }
                    else
                    {
                        _lockPaintProperty.boolValue = true;
                    }
                    _lockPaintProperty.serializedObject.ApplyModifiedProperties();
                    _lockPaintMode.Q<VisualElement>("icon").style.backgroundImage = _lockPaintProperty.boolValue ?  _paintLock : _paintUnlock;
                    _lockPaintMode.tooltip = !_lockPaintProperty.boolValue ? "Lock out from paint mode" : "Unlock from paint mode";
                });
            }

            container = new VisualElement();
            container.style.transitionDuration = new List<TimeValue> { new TimeValue(0.1f) };
            //container.style.height = 0;
            container.style.overflow = Overflow.Hidden;
            container.style.display = DisplayStyle.None;
            container.SetBorder(1, TileWorldCreatorColor.Grey.GetColor());
            container.style.backgroundColor = EditorGUIUtility.isProSkin ? contentBGColor : TileWorldCreatorColor.UltraLightGrey.GetColor();

         

            selector.Add(arrowIcon);
            selector.Add(title);
            
           
            header.Add(selector);
            header.Add(moveUp);
            header.Add(moveDown);
            if (_lockPaintProperty != null)
            {
                header.Add(_lockPaintMode);
            }
            header.Add(_executeLayer);
            if (_onClearCallback != null)
            {
                header.Add(_clearLayerButton);
            }
          
            if (_onDuplicateCallback != null)
            {
                header.Add(_duplicateLayerButton);
            }
            header.Add(_dragManipulator);
            header.Add(removeButton);

            Add(header);
            Add(container);

            this.style.marginLeft = -20;

        }

        public void Select()
        {
            header.style.backgroundColor = TileWorldCreatorColor.Blue.GetColor();
        }

        public void Deselect()
        {
            header.style.backgroundColor = TileWorldCreatorColor.Grey.GetColor();
        }

        public void SetLabel(string _label)
        {
            title.text = _label;
        }

        public void AddContent(VisualElement _content)
        {
            items.Add(_content);
            this.Add(_content);
            this.RegisterCallback<GeometryChangedEvent>(GeometryChangedCallback);
        }

        private void GeometryChangedCallback(GeometryChangedEvent evt)
        {
            this.UnregisterCallback<GeometryChangedEvent>(GeometryChangedCallback);

            for (int i = 0; i < items.Count; i++)
            {
                totalHeight += items[i].resolvedStyle.height;
                container.Add(items[i]);
            }
        }


        public void OpenFoldout(bool _open)
        {
            if (_open)
            {
                _value = true;
                container.style.display = DisplayStyle.Flex;
                container.SetPadding(5, 5, 5, 5);
                arrowIcon.style.backgroundImage = arrowDown;
            }
            else
            {
                _value = false;
                container.style.display = DisplayStyle.None;
                container.SetPadding(0, 0, 0, 0);
                arrowIcon.style.backgroundImage = arrowRight; 
            }

            onFoldout?.Invoke(_open);
            //container.style.height = _open ? totalHeight : 0;
        }
    }
}
#endif