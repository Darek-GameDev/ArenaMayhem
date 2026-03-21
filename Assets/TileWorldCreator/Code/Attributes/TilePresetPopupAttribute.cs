using UnityEngine;

namespace GiantGrey.TileWorldCreator.Attributes
{
    public class TilePresetPopupAttribute : PropertyAttribute
    {
        public string displayName = "";

        public TilePresetPopupAttribute() { }
       
        public TilePresetPopupAttribute(string displayName)
        {
            this.displayName = displayName;
        }
    }
}