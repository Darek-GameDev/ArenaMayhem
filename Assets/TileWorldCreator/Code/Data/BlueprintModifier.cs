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
using UnityEngine.UIElements;

namespace GiantGrey.TileWorldCreator
{
    [System.Serializable]
    public class BlueprintModifier : ScriptableObject
    {
        [HideInInspector]
        public Configuration asset;
        
        [HideInInspector]
        public bool isEnabled = true;
        public virtual HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer){ return null;}

        public virtual VisualElement BuildInspector(Configuration _asset){ return null; }

        public virtual void OnSceneDraw(){}


        public bool[,] GetMapArray(HashSet<Vector2> _positions)
        {
            bool[,] _map = new bool[asset.width, asset.height];

            for (int x = 0; x < asset.width; x++)
            {
                for (int y = 0; y < asset.height; y++)
                {
                    Vector2 _pos = new Vector2(x, y);
                    _map[x, y] = _positions.Contains(_pos);
                }
            }

            return _map;
        }
    }
}