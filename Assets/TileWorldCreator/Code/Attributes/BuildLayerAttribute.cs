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

using System;

namespace GiantGrey.TileWorldCreator.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class BuildLayerAttribute : Attribute
    {
        
        public string layerTypeName;
        public string iconPath;

        public BuildLayerAttribute(string _layerTypeName, string _iconPath)
        {
            layerTypeName = _layerTypeName;
            iconPath = _iconPath;
        }
    }
}