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
using UnityEngine.UIElements;
using GiantGrey.TileWorldCreator.Attributes;

namespace GiantGrey.TileWorldCreator.UI
{
  [CustomPropertyDrawer(typeof(HelpBoxAttribute))]
  public class HelpBoxDrawer : PropertyDrawer
  {
      public override VisualElement CreatePropertyGUI(SerializedProperty property)
      {
          var helpBoxAttribute = (HelpBoxAttribute)attribute;

          var container = new VisualElement();
          container.style.height = 200;

          // Create HelpBox as a custom styled Label
          var helpBox = new HelpBox(helpBoxAttribute.message, helpBoxAttribute.messageType);
          helpBox.style.marginBottom = 4;
          container.Add(helpBox);

          // // Draw the actual property field
          var propertyField = new PropertyField(property);
          container.Add(propertyField);

          return container;
      }
  }
}
#endif