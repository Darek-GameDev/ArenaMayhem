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
using UnityEngine;
using UnityEngine.UIElements;

namespace GiantGrey.TileWorldCreator.UI
{
    public class ToolbarButtonElement : Button
    {
        VisualElement root;
        private VisualElement iconElement;
        private bool toggleState;
        public bool Toggle
        {
            set
            {
                toggleState = value;
                if (toggleState)
                {
                    root.style.backgroundColor = TileWorldCreatorColor.PaleBlue.GetColor();
                }
                else
                {
                    root.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.Grey.GetColor() : TileWorldCreatorColor.AlmostWhite.GetColor();
                }
            }
            get
            {
                return toggleState;
            }
        }

        public ToolbarButtonElement(Vector2Int _size, string _title, Texture2D _icon, bool _isToggle = false)
        {
            
            this.SetPadding(0, 0, 0, 0);
            
            
            root = new VisualElement();
            root.style.width = _size.x;
            root.style.height = _size.y;
            root.style.backgroundColor = new Color(0, 0, 0, 0);

            // root.SetBorder(1);

            if (!string.IsNullOrEmpty(_title))
            { 
                var _label = new Label(_title);

                root.Add(_label);
            }

            if (_icon != null)
            {
                iconElement = new VisualElement();
                iconElement.style.backgroundImage = _icon;
                iconElement.style.width = _size.x - 8;
                iconElement.style.height = _size.y - 8;
                iconElement.style.unityBackgroundImageTintColor = EditorGUIUtility.isProSkin ? Color.white : TileWorldCreatorColor.Grey.GetColor();
                    // EditorGUIUtility.isProSkin ? Color.white : Color.black;
                iconElement.SetMargin(4, 4, 4, 4);
                root.Add(iconElement);
            }

            root.RegisterCallback<MouseEnterEvent>(e => root.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.LightGrey.GetColor() : TileWorldCreatorColor.White.GetColor());
            root.RegisterCallback<MouseLeaveEvent>(e => 
            {
                if (!_isToggle)
                {
                    root.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.Grey.GetColor() : TileWorldCreatorColor.AlmostWhite.GetColor();
                }
                else
                {
                    if (!toggleState)
                    {
                        root.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.Grey.GetColor() : TileWorldCreatorColor.AlmostWhite.GetColor();
                    }
                    else
                    {
                        root.style.backgroundColor = TileWorldCreatorColor.PaleBlue.GetColor();
                        iconElement.style.unityBackgroundImageTintColor = Color.white;
                    }
                }
            });
            root.RegisterCallback<ClickEvent>(e => 
            {
                if (_isToggle)
                {
                    toggleState = !toggleState;
                    if (toggleState)
                    {
                        root.style.backgroundColor = TileWorldCreatorColor.PaleBlue.GetColor();
                    }
                    else
                    {
                        root.style.backgroundColor = TileWorldCreatorColor.AlmostWhite.GetColor();
                    }
                }
            });

            this.Add(root);
        }

    }
}
#endif