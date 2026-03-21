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
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

namespace GiantGrey.TileWorldCreator.UI
{
    public static class TileWorldCreatorUIElements
    {
	    
	    /// <summary>
	    /// Returns VisualElement instead of Button
	    /// </summary>
	    /// <param name="_title"></param>
	    /// <param name="_icon"></param>
	    /// <param name="_iconWidth"></param>
	    /// <param name="_iconHeight"></param>
	    /// <returns></returns>
	    public static VisualElement FlatButtonAsVisualElement(string _title, Texture2D _icon, int _iconWidth, int _iconHeight)
	    {
		    var _b = new VisualElement();
		    
		    _b.SetRadius( 0, 0, 0, 0);
		    _b.SetMargin(1, 1, 1, 1);
		    _b.SetPadding(0, 0, 0, 0);
		    _b.SetBorder(0);
		    _b.style.flexDirection = FlexDirection.Row;
		    _b.style.alignItems = Align.Center;
		    _b.style.alignContent = Align.Center;
		    _b.style.justifyContent = Justify.Center;
		    _b.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.Grey.GetColor() : TileWorldCreatorColor.VeryLightGrey.GetColor();

		    if (!string.IsNullOrEmpty(_title))
		    {
			    var _lbl = new  Label(_title);
			    _b.Add(_lbl);
		    }
		    
		    if (_icon != null)
		    {
			    var _iconElement = new VisualElement();
			    _iconElement.name = "icon";
			    _iconElement.style.backgroundImage = _icon;
			    _iconElement.style.width = _iconWidth;
			    _iconElement.style.height = _iconHeight;
			    _b.Add(_iconElement);
		    }
	    
		    _b.RegisterCallback<MouseEnterEvent>(e => _b.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.LightGrey.GetColor() : TileWorldCreatorColor.AlmostWhite.GetColor());
		    _b.RegisterCallback<MouseLeaveEvent>(e => _b.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.Grey.GetColor() : TileWorldCreatorColor.VeryLightGrey.GetColor());
	    
		    return _b;
	    }
	    
	    
	    
       public static Button FlatButton(string _title, Texture2D _icon, int _iconWidth, int _iconHeight, IManipulator _manipulator = null)
       {
        	var _b = new Button();
	        if (_manipulator != null)
	        {
		        _b.contentContainer.AddManipulator(_manipulator);
	        }
			_b.text = _title;
			_b.SetRadius( 0, 0, 0, 0);
			_b.SetMargin(1, 1, 1, 1);
			_b.SetPadding(0, 0, 0, 0);
			_b.SetBorder(0);
            _b.style.flexDirection = FlexDirection.Row;
            _b.style.alignItems = Align.Center;
            _b.style.alignContent = Align.Center;
            _b.style.justifyContent = Justify.Center;
            _b.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.Grey.GetColor() : TileWorldCreatorColor.VeryLightGrey.GetColor();

            if (_icon != null)
            {
                var _iconElement = new VisualElement();
                _iconElement.name = "icon";
                _iconElement.style.backgroundImage = _icon;
                _iconElement.style.width = _iconWidth;
                _iconElement.style.height = _iconHeight;
                _b.Add(_iconElement);
            }

            _b.RegisterCallback<MouseEnterEvent>(e => _b.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.LightGrey.GetColor() : TileWorldCreatorColor.AlmostWhite.GetColor());
            _b.RegisterCallback<MouseLeaveEvent>(e => _b.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.Grey.GetColor() : TileWorldCreatorColor.VeryLightGrey.GetColor());

            return _b;
       }

       public static Button ToolbarButton(string _title, Texture2D _icon, int _iconWidth, int _iconHeight, bool _isToggle = false)
       {
        	var _b = new ToolbarButton();
			_b.text = _title;
			_b.SetRadius( 0, 0, 0, 0);
			_b.SetMargin(1, 1, 1, 1);
			_b.SetPadding(0, 0, 0, 0);
			_b.SetBorder(0);
            _b.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.Grey.GetColor() : TileWorldCreatorColor.VeryLightGrey.GetColor();
            _b.style.flexDirection = FlexDirection.Row;
            _b.style.alignItems = Align.Center;
            _b.style.alignContent = Align.Center;
            _b.style.justifyContent = Justify.Center;

            if (_icon != null)
            {
                var _iconElement = new VisualElement();
                _iconElement.style.backgroundImage = _icon;
                _iconElement.style.width = _iconWidth;
                _iconElement.style.height = _iconHeight;
                _b.SetPadding(5, 5, 5, 5);
                _b.Add(_iconElement);
            }

            _b.RegisterCallback<MouseEnterEvent>(e => _b.style.backgroundColor =  EditorGUIUtility.isProSkin ?  TileWorldCreatorColor.LightGrey.GetColor() : TileWorldCreatorColor.AlmostWhite.GetColor());
            _b.RegisterCallback<MouseLeaveEvent>(e => _b.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.Grey.GetColor() : TileWorldCreatorColor.VeryLightGrey.GetColor());

            return _b;
       }

       public static void SetActiveState(this Button _button, bool _active)
       {
            _button.style.backgroundColor = _active ? TileWorldCreatorColor.PaleBlue.GetColor() : EditorGUIUtility.isProSkin ? TileWorldCreatorColor.Grey.GetColor() : TileWorldCreatorColor.VeryLightGrey.GetColor();
       }

       public static VisualElement Separator(string _title = "")
       {
            var _sep = new VisualElement();
            _sep.style.height = 1;
            _sep.style.flexGrow = 1;
            _sep.style.flexShrink = 0;
            _sep.style.backgroundColor = EditorGUIUtility.isProSkin ? Color.black : TileWorldCreatorColor.AlmostWhite.GetColor();
            _sep.style.marginTop = 5;

            if (!string.IsNullOrEmpty(_title))
            {
                var _label = new Label();
                _label.style.unityFontStyleAndWeight = FontStyle.Bold;
                _label.text = _title;
                _label.style.fontSize = 14;
                _label.style.marginTop = -20;
                _sep.Add(_label);
                _sep.style.marginTop = 30;
            }

            _sep.style.marginBottom = 5;

            return _sep;
       }
    }
}
#endif