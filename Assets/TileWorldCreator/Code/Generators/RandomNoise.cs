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
    [Modifier(ModifierAttribute.Category.Generators, "Random Noise", "")]
    public class RandomNoise : BlueprintModifier
    {
    
        public float weight = 0.5f;
        private int width;
        private int height;
        
        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            width = asset.width;
            height = asset.height;
            
            for (int x = 0; x < width; x ++)
            {
                for (int y = 0; y < height; y ++)
                {
                    float sample = Mathf.PerlinNoise((float)x / (float)width + _layer.random.NextFloat(0, 100), (float)y / (float)height + _layer.random.NextFloat(0, 100));
                    
                    if (sample > weight)
                    {
                        _positions.Add(new Vector2(x, y));
                    }
                }
            }

            return _positions;
        }
    }
}