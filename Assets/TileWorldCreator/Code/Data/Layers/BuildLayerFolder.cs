
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

namespace GiantGrey.TileWorldCreator
{
    [System.Serializable]
    public class BuildLayerFolder
    {   
        public string folderName;
        public string guid;
        public bool foldoutState;
        public List<BuildLayer> buildLayers = new List<BuildLayer>();

        public BuildLayerFolder (string _name)
        {
            folderName = _name;
            guid = System.Guid.NewGuid().ToString();
        }
    }
}