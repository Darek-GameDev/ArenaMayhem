
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

using UnityEngine;

namespace GiantGrey.TileWorldCreator.UI
{
    public enum TileWorldCreatorColor
    {
        Clear,
        White,
        Black,
        DarkGrey,
        Grey,
        LightGrey,
        Red,
        Rose,
        Orange,
        Yellow,
        Green,
        Blue,
        DarkBlue,
        Indigo,
        Coal,
        Gold, 
        PaleGreen,
        LightGreen,
        PaleBlue,
        DarkGreen,
        VeryLightGrey,
        UltraLightGrey,
        AlmostWhite,
    }

    public static class TileWorldCreatorColorExtensions
    {
        public static Color GetColor(this TileWorldCreatorColor color)
        {
            switch (color)
            {
                case TileWorldCreatorColor.Clear:
                    return new Color(0f / 255f, 0f / 255f, 0f / 255f, 0f / 255f);
                case TileWorldCreatorColor.White:
                    return new Color32(255, 255, 255, 255);
                case TileWorldCreatorColor.Black:
                    return new Color32(0, 0, 0, 255);
                case TileWorldCreatorColor.DarkGrey:
                    return new Color(40f / 255f, 40f / 255f, 40f / 255f);
                case TileWorldCreatorColor.Grey:
                    return new Color(80f / 255f, 80f / 255f, 80f / 255f);
                case TileWorldCreatorColor.LightGrey:
                    return new Color(110f / 255f, 110f / 255f, 110f / 255f);
                case TileWorldCreatorColor.Red:
                    return new Color32(243, 24, 24, 255);
                case TileWorldCreatorColor.Rose:
                    return new Color32(255, 146, 239, 255);
                case TileWorldCreatorColor.Orange:
                    return new Color32(229, 103, 41, 255);
                case TileWorldCreatorColor.Yellow:
                    return new Color32(255, 211, 0, 255);
                case TileWorldCreatorColor.Green: //#1CFF7D
                    return new Color32(28, 255, 125, 255);
                case TileWorldCreatorColor.Blue: //#16ACFF
                    return new Color32(22, 172, 255, 255);
                case TileWorldCreatorColor.DarkBlue: //#16ACFF
                    return new Color(0f / 255f, 55f / 255f,  85f / 255f, 255f / 255f);
                case TileWorldCreatorColor.Indigo: // #A353E6
                    return new Color32(163, 83, 230, 255);
                case TileWorldCreatorColor.Coal: //#755F55
                    return new Color32(35, 34, 30, 255);
                case TileWorldCreatorColor.Gold: //#D0A844
                    return new Color32(208, 168, 68, 255);
                case TileWorldCreatorColor.PaleGreen: //#93D393
                    return new Color32(147, 211, 147, 255);
                case TileWorldCreatorColor.LightGreen: //#E4F4E4  
                    return new Color32(228, 244, 228, 255);     
                case TileWorldCreatorColor.PaleBlue:
                    return new Color32 	(71,121,196, 255); //#A7C7E7
                case TileWorldCreatorColor.DarkGreen: //#5f9658
                    return new Color32  	(95,150,88, 255);
                case TileWorldCreatorColor.VeryLightGrey:
                    return new Color32(165, 165, 165, 255);
                case TileWorldCreatorColor.UltraLightGrey:
                    return new Color32(185, 185, 185, 255);
                case TileWorldCreatorColor.AlmostWhite:
                    return new Color32(215, 215, 215, 255);
                default:
                    return new Color32(0, 0, 0, 255);
            }
        }
    }
}
