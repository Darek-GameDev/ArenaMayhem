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
using GiantGrey.TileWorldCreator.Attributes;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GiantGrey.TileWorldCreator.UI
{
    [CustomPropertyDrawer(typeof(TilePresetPopupAttribute))]
    public class TilePresetPopupPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var _root = new VisualElement();
            _root.style.flexGrow = 1;
            _root.SetBorder(1);
            _root.SetPadding(2, 2, 2, 4);
            _root.SetMargin(2, 2, 2, 2);
            _root.style.flexDirection = FlexDirection.Column;


            if (!string.IsNullOrEmpty((attribute as TilePresetPopupAttribute).displayName))
            {
                var _displayLabel = new Label();
                _displayLabel.style.marginBottom = 2;
                _displayLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                _displayLabel.text = (attribute as TilePresetPopupAttribute).displayName;
                _root.Add(_displayLabel);
            }

            var _content = new VisualElement();
            _content.style.flexDirection = FlexDirection.Row;

            _root.Add(_content);
            
            var _verticalContainer = new VisualElement();
            _verticalContainer.style.flexGrow = 1;
            _verticalContainer.style.flexDirection = FlexDirection.Column;


        
            var _propertyField = new PropertyField(property);
            _propertyField.label = "";
            _propertyField.style.flexGrow = 1;

            var _thumb = new Image();
            _thumb.style.width = 50;
            _thumb.style.height = 50;

            if (property.objectReferenceValue != null)
            {
                var _preset = property.objectReferenceValue as TilePreset;
                if (_preset.previewThumbnail != null)
                {
                    var _bs = new BackgroundSize();
                    _bs.sizeType = BackgroundSizeType.Cover;
                    _thumb.style.backgroundSize = _bs;
                    _thumb.style.backgroundImage = _preset.previewThumbnail;
                    
                }
            }
            
            _content.Add(_thumb);

            var _selectPopup = new Button();
            _selectPopup.text = "Select";
            _selectPopup.style.height = 30;
            _selectPopup.RegisterCallback<ClickEvent>(evt => 
            {
                TilePresetPopupSelector.ShowPanel(Event.current.mousePosition, property, new TilePresetPopupSelector(), (x) => 
                {
                    var _bs = new BackgroundSize();
                    _bs.sizeType = BackgroundSizeType.Cover;
                    _thumb.style.backgroundSize = _bs;
                    _thumb.style.backgroundImage = x.previewThumbnail;
                });
            });

            _verticalContainer.Add(_propertyField);
            _verticalContainer.Add(_selectPopup);

            _content.Add(_verticalContainer);

            return _root;
        }
    }
}
#endif