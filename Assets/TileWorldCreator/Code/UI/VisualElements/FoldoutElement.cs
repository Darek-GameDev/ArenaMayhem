
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

using UnityEngine;
using UnityEngine.UIElements;

using GiantGrey.TileWorldCreator.Utilities;
using UnityEditor;

namespace GiantGrey.TileWorldCreator.UI
{
    public class FoldoutElement : VisualElement
    {
        private VisualElement header;
        private VisualElement arrow;
        private Label title;
        private VisualElement container;

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

        public FoldoutElement(string _title, Action<bool> _onFoldoutCallback, params Action[] _additionalButtons)
        {
            totalHeight = 0;
            arrowRight = TileWorldCreatorUtilities.LoadImage("arrowRight.twc");
            arrowDown = TileWorldCreatorUtilities.LoadImage("arrowDown.twc");
            onFoldout = _onFoldoutCallback;

            style.flexGrow = 1;

            items = new List<VisualElement>();

            header = new VisualElement();

            header.style.marginTop = 5;
            header.style.flexDirection = FlexDirection.Row;
            header.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.Grey.GetColor() : TileWorldCreatorColor.UltraLightGrey.GetColor();
            header.style.borderBottomWidth = 1;
            header.style.borderBottomColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.DarkGrey.GetColor() : TileWorldCreatorColor.Grey.GetColor() ;

            arrow = new VisualElement();
            arrow.style.backgroundImage = arrowRight;
            arrow.style.width = 24;
            arrow.style.height = 24;

            title = new Label();
            title.text = _title;
            title.style.marginLeft = 10;
            title.style.unityTextAlign = TextAnchor.MiddleLeft;
            title.style.color = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            container = new VisualElement();
            container.style.transitionDuration = new List<TimeValue> { new TimeValue(0.1f) };
            container.style.overflow = Overflow.Hidden;
            container.style.display = DisplayStyle.None;
            container.SetBorder(1, TileWorldCreatorColor.Grey.GetColor());


            
            header.RegisterCallback<ClickEvent>(click => 
            {
                 OpenFoldout(container.style.display == DisplayStyle.None ? true : false);
            });

            header.RegisterCallback<MouseEnterEvent>(enter => 
            {
                header.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.LightGrey.GetColor() : TileWorldCreatorColor.AlmostWhite.GetColor();
            });

            header.RegisterCallback<MouseLeaveEvent>(enter => 
            {
                header.style.backgroundColor = EditorGUIUtility.isProSkin ? TileWorldCreatorColor.Grey.GetColor() :  TileWorldCreatorColor.UltraLightGrey.GetColor();
            });

            header.Add(arrow);
            header.Add(title);

            Add(header);
            Add(container);


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
                arrow.style.backgroundImage = arrowDown;
            }
            else
            {
                _value = false;
                container.style.display = DisplayStyle.None;
                container.SetPadding(0, 0, 0, 0);
                arrow.style.backgroundImage = arrowRight; 
            }

            onFoldout?.Invoke(_open);
        }
    }
}
#endif