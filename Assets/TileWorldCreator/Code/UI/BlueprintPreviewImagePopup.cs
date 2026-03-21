
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
    public class BlueprintPreviewImagePopup : PopupWindowContent
    {
        private VisualElement root;
        private Texture2D previewImage;
        private static Vector2 position;


        public BlueprintPreviewImagePopup(Texture2D _image)
        {
            previewImage = _image;
        }

        public static void ShowPanel(Vector2 _pos, BlueprintPreviewImagePopup _panel)
        {
            position = _pos;
            UnityEditor.PopupWindow.Show(new Rect(_pos.x, _pos.y, 0, 0), _panel);
        }

        public override Vector2 GetWindowSize()
        {
            return new Vector2(300, 300);
        }

        public override void OnGUI(Rect rect){}

        public override void OnOpen()
        {
            root = editorWindow.rootVisualElement;
            root.style.backgroundColor = Color.black;
            root.SetBorder(2, Color.grey);

            var _image = new VisualElement();
            _image.style.width = 300;
            _image.style.height = 300;
            _image.style.backgroundImage = previewImage;
            root.Add(_image);
        }
    }
}
#endif