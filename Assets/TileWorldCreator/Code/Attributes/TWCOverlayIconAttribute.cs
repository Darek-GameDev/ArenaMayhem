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
using System;
using System.Diagnostics;
using GiantGrey.TileWorldCreator.Utilities;

namespace GiantGrey.TileWorldCreator.UI
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    [Conditional("UNITY_EDITOR")]
    public class TWCOverlayIconAttribute : IconAttribute
    {
        public TWCOverlayIconAttribute() : base(GetRelativeIconPath()) {}
        public static string GetRelativeIconPath(string asmdef = "GiantGrey.TileWorldCreator")
        {
            return TileWorldCreatorUtilities.GetRelativeResPath() + "LogoIconSmall.png";
        }
    }
}
#endif