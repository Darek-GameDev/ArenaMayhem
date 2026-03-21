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
using UnityEngine.UIElements;


namespace GiantGrey.TileWorldCreator.UI
{
    [CustomPropertyDrawer(typeof(BlueprintLayerDropdownAttribute))]
    public class BlueprintLayerDropdown : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var attr = (BlueprintLayerDropdownAttribute)attribute;

            // Get Configuration reference via SerializedProperty
        
            // var configProperty = property.serializedObject.FindProperty(attr.configFieldName);
            var _activeGameObject = Selection.activeGameObject;
            if (_activeGameObject == null)
            {
                return new Label("No GameObject with TileWorldCreatorManager selected");
            }
            var _manager = _activeGameObject.GetComponent<TileWorldCreatorManager>();
            Configuration _config = null;
            if (_manager != null)
            {
                _config = _manager.configuration;
            }
            else
            {
                return new Label("Missing TileWorldCreatorManager with assigned Configuration");
            }

            var container = new VisualElement();
            var dropdown = new LayerSelectDropdownElement(_config, property.stringValue, (name, guid) =>
            {
                try 
                {
                    property.stringValue = guid;
                    property.serializedObject.ApplyModifiedProperties();
                }
                catch
                {
                    property = null;
                }
            }, property.displayName);

            container.Add(dropdown);
            return container;
        }
    }
}
#endif