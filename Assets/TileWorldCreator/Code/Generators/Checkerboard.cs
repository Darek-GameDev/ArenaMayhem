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
using System.Collections.Generic;
using UnityEngine;
using GiantGrey.TileWorldCreator.Attributes;

namespace GiantGrey.TileWorldCreator
{
    [Modifier(ModifierAttribute.Category.Generators, "Checkerboard", "")]
    public class Checkerboard : BlueprintModifier
    {
        public bool horizontal = true;
		public bool vertical = true;
		public int spacing = 2;



        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            HashSet<Vector2> _generatedPositions = new HashSet<Vector2>();

            for (int x = 0; x < asset.width; x ++)
	        {
	            for (int y = 0; y < asset.height; y ++)
	            {
	               
					if(horizontal)
                    {
                        if(y%spacing==0)
                        {
                            _generatedPositions.Add(new Vector2(x,y));
                        }
                    }

					if(vertical)
                    {
                        if(x%spacing==0)
                        {
                            _generatedPositions.Add(new Vector2(x,y)); 
                        }
                    }
	            }
	        } 

            return _generatedPositions;
        }
    }
}