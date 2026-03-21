
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
using UnityEngine.Serialization;

namespace GiantGrey.TileWorldCreator
{
    [System.Serializable]
    public class BuildLayerMask
    {
        [FormerlySerializedAs("assignedTileMapGuid")]
        public string assignedBlueprintLayerGuid; // The assigned layer to use as mask
        public bool isPriority;

        [System.Serializable]
        public class PriorityTileException
        {
            public TilePreset.TileType tileType;
        }

        public List<PriorityTileException> priorityExceptions = new List<PriorityTileException>();
        
    }
}