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

#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace GiantGrey.TileWorldCreator.UI
{
    public static class UIElementsExtensions
    {
        public static Color colorLightGrey = new Color(80f / 255f, 80f / 255f, 80f / 255f);

        public static void SetBorder(this VisualElement _element, int _thickness, Color? _color = null)
        {
            _element.style.borderTopWidth = _thickness;
            _element.style.borderBottomWidth = _thickness;
            _element.style.borderLeftWidth = _thickness;
            _element.style.borderRightWidth = _thickness;
            _element.style.borderTopColor = _color ?? colorLightGrey;
            _element.style.borderBottomColor = _color ?? colorLightGrey;
            _element.style.borderLeftColor = _color ?? colorLightGrey;
            _element.style.borderRightColor = _color ?? colorLightGrey;
        }

        public static void SetMargin(this VisualElement _element, int _marginTop, int _marginBottom, int _marginLeft, int _marginRight)
        {
            _element.style.marginTop = _marginTop;
            _element.style.marginBottom = _marginBottom;
            _element.style.marginLeft = _marginLeft;
            _element.style.marginRight = _marginRight;
        }

        public static void SetPadding(this VisualElement _element, int _paddingTop, int _paddingBottom, int _paddingLeft, int _paddingRight)
        {
            _element.style.paddingTop = _paddingTop;
            _element.style.paddingBottom = _paddingBottom;
            _element.style.paddingLeft = _paddingLeft;
            _element.style.paddingRight = _paddingRight;
        }

        public static void SetRadius(this VisualElement _element, int _topLeftRadius, int _topRightRadius, int _bottomLeftRadius, int _bottomRightRadius)
        {
            _element.style.borderTopLeftRadius = _topLeftRadius;
            _element.style.borderTopRightRadius = _topRightRadius;
            _element.style.borderBottomLeftRadius = _bottomLeftRadius;
            _element.style.borderBottomRightRadius = _bottomRightRadius;
        }
    }
}
#endif