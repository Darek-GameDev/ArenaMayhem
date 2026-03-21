
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
using UnityEngine.UIElements;

namespace GiantGrey.TileWorldCreator.UI
{
    public class BlueprintLayerItemDragManipulator : Manipulator
    {

        Configuration asset;    
        ConfigurationEditor editor;
        string layerGuid;
        string folderGuid;
        bool enabled = false;

        VisualElement dragGhost;
        VisualElement root;
        VisualElement highlightElement;
        BlueprintLayerFolderListViewElement lastPickedFolder;

        public BlueprintLayerItemDragManipulator ( VisualElement _root, Configuration _asset, ConfigurationEditor _editor, string _layerGuid, string _folderGuid)
        {
            asset = _asset;
            editor = _editor;
            layerGuid = _layerGuid;
            folderGuid = _folderGuid;
            highlightElement = new VisualElement();
            highlightElement.style.position = Position.Absolute;
            highlightElement.SetBorder(2, Color.white);
            highlightElement.style.width = 30;
            highlightElement.style.height = 30;
            highlightElement.style.marginLeft = -12;
            highlightElement.style.marginTop = -2;
            highlightElement.style.display = DisplayStyle.None;

            var _layerName = "";
            for (int j = 0; j < asset.blueprintLayerFolders.Count; j ++)
            {
                for (int k = 0; k < asset.blueprintLayerFolders[j].blueprintLayers.Count; k ++)
                {
                    if (asset.blueprintLayerFolders[j].blueprintLayers[k].guid == layerGuid)
                    {
                        _layerName = asset.blueprintLayerFolders[j].blueprintLayers[k].layerName;
                    }
                }
            }

            var _lbl = new Label();
            _lbl.text = _layerName;
            _lbl.style.fontSize = 14;
            _lbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            _lbl.style.unityTextAlign = TextAnchor.MiddleLeft;
            _lbl.style.flexGrow = 1;
            _lbl.style.marginLeft = 10;

            dragGhost = new VisualElement();
            dragGhost.style.position = Position.Absolute;
            dragGhost.style.width = 150;
            dragGhost.style.height = 40;
            dragGhost.style.backgroundColor = Color.grey;
            // dragGhost.SetBorder(2, Color.black);
            dragGhost.style.borderBottomWidth = 2;
            dragGhost.style.borderBottomColor = Color.black;
            dragGhost.style.display = DisplayStyle.None;
            dragGhost.style.alignContent = Align.Center;
            // dragGhost.style.alignItems = Align.Center;
            dragGhost.Add(_lbl);

            root = _root;
            _root.Add(dragGhost);
            _root.Add(highlightElement);
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(PointerDownHandler);
            target.RegisterCallback<MouseUpEvent>(PointerUpHandler);
            target.RegisterCallback<MouseMoveEvent>(PointerMoveHandler);
            target.RegisterCallback<MouseOutEvent>(PointerCaptureOutHandler);

        
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(PointerDownHandler);
            target.UnregisterCallback<MouseUpEvent>(PointerUpHandler);
            target.UnregisterCallback<MouseMoveEvent>(PointerMoveHandler);
            target.UnregisterCallback<MouseOutEvent>(PointerCaptureOutHandler);
        }


        public void PointerDownHandler(MouseDownEvent _evt)
        {
            target.CaptureMouse();
            _evt.StopImmediatePropagation();
            enabled = true;
            dragGhost.style.display = DisplayStyle.Flex;
#if UNITY_6000_2
            dragGhost.style.translate = root.WorldToLocal(_evt.mousePosition);
#else
            dragGhost.transform.position = root.WorldToLocal(_evt.mousePosition);
#endif            
            dragGhost.BringToFront();
        }

        public void PointerUpHandler(MouseUpEvent _evt)
        {
            dragGhost.style.display = DisplayStyle.None;

            if (enabled)
            {
                highlightElement.style.display = DisplayStyle.None;
                target.ReleaseMouse();
                _evt.StopImmediatePropagation();


                var _pickElement = target.panel.Pick(_evt.mousePosition);
                if (_pickElement == null)
                {
                    enabled = false;
                    return;
                }
                

                if (lastPickedFolder != null)
                {
                    for (int i = 0; i < asset.blueprintLayerFolders.Count; i ++)
                    {
                        if (asset.blueprintLayerFolders[i].guid == lastPickedFolder.name && asset.blueprintLayerFolders[i].guid != folderGuid)
                        {
                            for (int j = 0; j < asset.blueprintLayerFolders.Count; j ++)
                            {
                                for (int k = 0; k < asset.blueprintLayerFolders[j].blueprintLayers.Count; k ++)
                                {
                                    if (asset.blueprintLayerFolders[j].blueprintLayers[k].guid == layerGuid)
                                    {
                                        var _layer = asset.blueprintLayerFolders[j].blueprintLayers[k];
                                        asset.blueprintLayerFolders[i].blueprintLayers.Add(_layer);
                                        asset.blueprintLayerFolders[j].blueprintLayers.RemoveAt(k);

                                        editor.BuildBlueprintLayers();
                                    }
                                }
                            }
                        }
                    }
                }
                

                enabled = false;

            }
        }

        public void PointerMoveHandler(MouseMoveEvent _evt)
        {
            if (!enabled)
                return;

            dragGhost.style.display = DisplayStyle.Flex;
#if UNITY_6000_2
            dragGhost.style.translate = root.WorldToLocal(_evt.mousePosition);
#else
            dragGhost.transform.position = root.WorldToLocal(_evt.mousePosition);
#endif

            var _pickElement = target.panel.Pick(_evt.mousePosition);
            if (_pickElement == null)
            {
                return;
            }
            
            var _pickedFolder = _pickElement.GetFirstAncestorOfType<BlueprintLayerFolderListViewElement>();
            if (_pickedFolder != null)
            {
                if (_pickedFolder.name != folderGuid)
                {
        
                    highlightElement.style.display = DisplayStyle.Flex;
#if UNITY_6000_2
                    highlightElement.style.translate = root.WorldToLocal(_pickedFolder.worldTransform.GetPosition());
#else
                    highlightElement.transform.position = root.WorldToLocal(_pickedFolder.worldTransform.GetPosition());
#endif
                    highlightElement.BringToFront();   

                    lastPickedFolder = _pickedFolder;
                }
            }
        }

        public void PointerCaptureOutHandler(MouseOutEvent _evt)
        {
            dragGhost.style.display = DisplayStyle.None;
        }
    }
}
#endif