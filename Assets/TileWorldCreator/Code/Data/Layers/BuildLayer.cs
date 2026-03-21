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
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UIElements;
using Random = Unity.Mathematics.Random;

using GiantGrey.TileWorldCreator.Components;
using GiantGrey.TileWorldCreator.UI;
using System;

namespace GiantGrey.TileWorldCreator
{
    [System.Serializable]
    public class BuildLayer : ScriptableObject
    {
        public Random random;
        public bool isEnabled = true;
        public string layerName;
        public string guid;
        
        public string assignedBlueprintLayerGuid;
        public BlueprintLayer currentBlueprintLayer;
        // public List<string> assignedBlueprintLayerGuids = new List<string>();
        // public List<string> assignedBlueprintLayerNames = new List<string>();

        public bool foldoutState;
        public bool useMultiLayers = false;

        public Dictionary<int, Dictionary<Vector2, bool>> worldGrid = new Dictionary<int, Dictionary<Vector2, bool>>();
        public HashSet<ClusterIdentifier> availableClusters;

        public List<BuildLayerMask> masks = new List<BuildLayerMask>();

       

        [SerializeField]
        public string hierarchyLayerID;
        [SerializeField]
        private string lastHierarchyLayerID;
        private GameObject layerObject;
        public GameObject LayerObject
        {
            get
            {
                return layerObject;
            }
        }


        [System.Serializable]
        public struct TileData : IEquatable<TileData>
        {
            public bool isAssigned;
            // public bool isNew;
            public Vector2 tilePosition;
            /// <summary>
            /// The actual position the user has clicks on. - Position from the blueprint layer cell
            /// </summary>
            public Vector2 worldMapPosition; // the actual position the user has clicked on. - position from the blueprint layer cell
            // public Vector2[] worldMapPositions;
            public int yRotation;
            public float yRotationOffset;
            public int configuration;
            public TilePreset.TileType tileType;

            [System.Serializable]
            public struct NeighboursLocation
            {
                public bool north;
                public bool west;
                public bool east;
                public bool south;
                public bool northWest;
                public bool northEast;
                public bool southWest;
                public bool southEast;
            }
            public NeighboursLocation location;

            public bool Equals(TileData other)
            {
                return tilePosition == other.tilePosition
                    && configuration == other.configuration
                    && tileType == other.tileType;
            }

            public override bool Equals(object obj)
            {
                if (obj is TileData other)
                    return Equals(other);
                return false;
            }
            
            public override int GetHashCode()
            {
                int hash = 17;
                hash = hash * 31 + tilePosition.GetHashCode();
                hash = hash * 31 + configuration.GetHashCode();
                hash = hash * 31 + tileType.GetHashCode();
                return hash;
            }
        }

        public GameObject GetLayerObject(GameObject _manager)
        {
            layerObject = GetHierarchyLayer(_manager);
            return layerObject;
        }

        GameObject GetHierarchyLayer(GameObject _manager)
        {
            var _layers = _manager.GetComponentsInChildren<LayerIdentifier>(true);

            for (int i = 0; i < _layers.Length; i++)
            {
                var _layer = _layers[i];
                if (_layer.guid == hierarchyLayerID)
                {
                    _layer.gameObject.transform.SetParent(_manager.transform, false);
                    _layer.transform.localRotation = Quaternion.identity;

                    return _layer.gameObject;
                }
            }

            // No layer found, create new layer
            var _newLayer2 = new GameObject("Layer " + layerName);
            var _component2 = _newLayer2.AddComponent<LayerIdentifier>();
            _component2.guid = guid;
            hierarchyLayerID = guid;

            _newLayer2.transform.SetParent(_manager.transform, false);
            _newLayer2.gameObject.transform.localRotation = Quaternion.identity;

            return _newLayer2;
        }


        public BuildLayer()
        {
            guid = System.Guid.NewGuid().ToString();
        }

        public virtual void ResetLayer(TileWorldCreatorManager _manager = null) {}

        public void ClearWorldPositions()
        {
            worldGrid.Clear();
        }

        public virtual void ExecuteLayer(Configuration _configuration, GameObject _owner, TileWorldCreatorManager _manager) {}

#if UNITY_EDITOR
        public virtual VisualElement CreateInspectorGUI(Configuration _asset, Editor _assetEditor, LayerFoldoutElement _layerFoldout) { return null; }
#endif 

        public virtual TileData GetTileDataFromPosition(Vector2 _position)
        {
            return new TileData();
        }
    }
}