
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
    public static class TileConfigurations
    {
        public static List<int> NRMGRD_cornerFill_configurations = new List<int>
        {
            27, 31, 54, 55, 91, 95, 118, 119, 216, 217, 220, 221, 283, 287, 310, 311, 347, 351, 374, 375, 432, 433, 436, 437, 472, 473, 476, 477, 496, 497, 500, 501,
        };
        public static List<int> NRMGRD_cornerWay_configurations = new List<int>
        {
            26, 30, 50, 51, 90, 94, 114, 115, 152, 153, 156, 157, 176, 177, 180, 181, 240, 241, 244, 245, 282, 286, 306, 307, 346, 370, 371, 408, 409, 412, 413
        };

        public static List<int> NRMGRD_edgeWay_configurations = new List<int>
        {
            56, 57, 60, 61, 120, 121, 124, 125, 146, 147, 150, 151, 210, 211, 214, 215, 312, 313, 316, 317, 376, 377, 380, 381, 402, 403, 406, 407, 466, 467, 470, 471,
        };

        public static List<int> NRMGRD_edgeFill_configurations = new List<int>
        {
            63, 127, 219, 223, 319, 383, 438, 439, 475, 479, 502, 503, 504, 505, 508, 509
        };

        public static List<int> NRMGRD_threeWay_configurations = new List<int>
        {
            58, 122, 154, 158, 178, 179, 184, 185, 188, 189, 242, 243, 314, 378, 410, 414
        };

        public static List<int> NRMGRD_threeWayFill_configurations = new List<int>
        {
            191, 251, 446, 506
        };

        public static List<int> NRMGRD_deadEndWay_configurations = new List<int>
        {
            18, 19, 22, 23, 24, 25, 28, 29, 48, 49, 52, 53, 82, 83, 86, 87, 88, 89, 92, 93, 112, 113, 116, 117, 144, 145, 148, 149, 208, 209, 212, 213, 274, 275, 278, 279, 
            280, 281, 284, 285, 304, 305, 308, 309, 338, 339, 342, 343, 344, 345, 348, 349, 368, 369, 372, 373, 400, 401, 404, 405, 464, 465, 468, 469,
        };

        public static List<int> NRMGRD_single_configurations = new List<int>
        {
            16, 17, 20, 21, 80, 81, 84, 85, 272, 273, 276, 272, 277, 340, 341, 336, 337
        };

        public static List<int> NRMGRD_fill_configurations = new List<int>
        {
            511, 
        };

        // blend between fill and normal way
        public static List<int> NRMGRD_edgeCornerFill_configurations = new List<int>
        {
            59, 62, 123, 126, 155, 159, 182, 183, 218, 222, 246, 247, 248, 249, 252, 315, 318, 379, 382, 253, 411, 415, 434, 435, 440, 441, 444, 445, 474, 478, 498,499,
        };

        public static List<int> NRMGRD_threeCorner_configurations = new List<int>
        {
            187, 190, 250, 442
        };

        public static List<int> NRMGRD_doubleCorner_configurations = new List<int>
        {
            254, 443
        };

        public static List<int> NRMGRD_fourWay_configurations = new List<int>
        {
            186,
        };

        public static List<int> NRMGRD_interiorCorner_configurations = new List<int>
        {
            255, 447, 507, 510
        };



        public static List<int> NRMGRD_rotationZero_configurations = new List<int> 
        {
            // Edge Way
            312, 120, 56, 
            // Dead End
            24, 89, 280,
            // Three Way
            58,
            // Corner way
            152,
            // Four Way
            186,
            // Corner Fill
            95,
            // Edge Corner Fill
            218, 222, 434, 474, 498
        };

        public static List<int> NRMGRD_rotation90_configurations = new List<int> 
        {
            // Dead End
            22, 23, 18, 19, 82, 83, 86, 87, 274, 275, 278, 279, 338, 339, 342, 343,
            // Edge
            146, 147, 151, 210, 211, 214, 215, 403, 406, 407, 466, 467, 470, 471,
            // Edge Fill
            438, 439, 502, 503,
            // Three way
            178, 179, 242, 243,
            // Three way Fill
            191,
            // Edge Corner Fill
            54, 59, 123, 248, 249, 252, 253, 315, 379,
            // Corner Fill
            55, 118, 310, 311, 374, 375,
            // Corner Way
            26, 30, 90, 94, 119, 282, 286, 346,
            // Interior Corner 
            507,
            // Double Corner
            254,
            // Tripple Corner
            250,
        };

        public static List<int> NRMGRD_rotation180_configurations = new List<int> 
        {
            // Dead End
            48, 49, 52, 53, 112, 113, 116, 117, 304, 305, 308, 309, 368, 369, 372, 373, //180,
            // Corner Way
            50, 51, 114, 115,306, 307, 370, 371,
            // Edge Fill
            504, 505, 508, 509, // 223, 
            // Corner Fill
            432, 433, 436, 437,496, 497, 500, 501,
            // Three Corner
            187,
            // ThreeWay Fill
            184, 185, 188, 189, 446,
            // Edge Corner Fill
            155, 159, 182, 183, 246, 247, 411, 415,
            // Interior Corner
            255,
            
        };

        // -X SCALING 
        // 59, 182, 218, 222, 246, 247, 315, 444

        public static List<int> NRMGRD_rotation270_configurations = new List<int> 
        {
            // Dead End
            144, 145, 148, 149, 208, 209, 212, 213, 400, 401, 404, 405, 464, 465, 468, 469,
            // Three Way
            154, 158, 410, 414,
            // Three Way Fill
            506,
            // Corner Fill
            216, 217, 220, 472, 476, 473, 477,
            // Corner Way
            176, 177, 180, 181, 221, 240, 241, 244, 245,
            // Three Corner
            190,
            // Edge
            150, 402,
            // Edge Fill
            223, 219, 475, 479,
            // Edge Corner Fill
            62, 126, 318, 382, 440, 441, 444, 445,
            // Interior Corner
            447
        };

        // Edge Corner Fill tiles are not symmetrical, that's why we need to rotate them in some cases
        public static List<int> NRMGRD_minusXScale_configurations = new List<int>
        {
            59, 123, 182, 183, 218, 222, 246, 247, 315, 379, 440, 441, 444, 445, 474, 478
        };




        public static List<int> DUALGRD_corner_configurations = new List<int>
        {
            1, 2, 4, 8, 
        };

        public static List<int> DUALGRD_edge_configurations = new List<int>
        {
            3, 5, 10, 12,
        };

        public static List<int> DUALGRD_fill_configurations = new List<int>
        {
            15
        };

        public static List<int> DUALGRD_interiorCorner_configurations = new List<int>
        {
            7, 13, 14, 11,
        };

        public static List<int> DUALGRD_doubleInteriorCorner_configurations = new List<int>
        {
            6, 9
        };

        public static List<int> rotationZeroConfigurations = new List<int>
        { 
            // Corner
            1,
            // Edge
            3,
            // Fill
            15,
            // Interior Corner
            0
            // Double Interior Corner

        };
        public static List<int> rotation90Configurations = new List<int>
        { 
            // Corner
            2,
            // Edge
            10,
            // Interior Corner
            11,
            // Double interior corner
            6,
        };
        public static List<int> rotation180Configurations = new List<int>
        { 
            // Corner
            8,
            // Edge
            12,
            // Interior Corner
            14,
            // Double Interior Corner

        };
        public static List<int> rotation270Configurations = new List<int>
        { 
            // Corner
            4,
            // Edge
            5,
            // Interior Corner
            13,
        };
    }
}