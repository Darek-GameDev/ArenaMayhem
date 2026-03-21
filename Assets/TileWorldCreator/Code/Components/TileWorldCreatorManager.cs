
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
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;
using GiantGrey.TileWorldCreator.Components;
using System.Collections;

#pragma warning disable 0414
namespace GiantGrey.TileWorldCreator
{

	public enum ExecutionMode
	{
		Normal,
		FromScratch
	}


	[AddComponentMenu("TileWorldCreator/TileWorldCreatorManager")]
	public class TileWorldCreatorManager : MonoBehaviour
	{
		[FormerlySerializedAs("dualTileWorldCreatorAsset")]
		public Configuration configuration;

		public bool showClusterCellsDebug;

		private Camera sceneViewCamera;

		internal HashSet<Vector2> paintedPositions = new HashSet<Vector2>();
		// public BlueprintLayer selectedPaintLayer;
		internal float brushSize;
		internal bool paintState;


		// Debug colors
		private Color colorGrey = new Color(0.5f, 0.5f, 0.5f, 0.5f);
		private bool delayedExecutionRunning;

		public HashSet<ClusterIdentifier> availableClusters;
		public HashSet<BuildLayer> lateUpdateLayers;

		public Action<float> OnMapProgress;
		public Action OnMapReady;
		public Action OnBlueprintLayersReady;
		public Action OnBuildLayersReady;

		/// <summary>
		/// Gets toggled by the inspector
		/// </summary>
		[HideInInspector][System.NonSerialized] internal bool isInspected;

		void Start()
		{
			// Get all available clusters
			if (configuration == null)
				return;

			try
			{
				for (int i = 0; i < configuration.buildLayerFolders.Count; i++)
				{
					for (int j = 0; j < configuration.buildLayerFolders[i].buildLayers.Count; j++)
					{
						availableClusters = new HashSet<ClusterIdentifier>();
						var _layer = configuration.buildLayerFolders[i].buildLayers[j].GetLayerObject(this.gameObject);

						configuration.buildLayerFolders[i].buildLayers[j].availableClusters = _layer.GetComponentsInChildren<ClusterIdentifier>(true).ToHashSet();
					}
				}
			}catch{}
		}

		public void ClearAllBlueprintLayers()
		{
			for (int i = 0; i < configuration.blueprintLayerFolders.Count; i++)
			{
				for (int j = 0; j < configuration.blueprintLayerFolders[i].blueprintLayers.Count; j++)
				{
					configuration.blueprintLayerFolders[i].blueprintLayers[j].SetAsset(configuration);
					configuration.blueprintLayerFolders[i].blueprintLayers[j].ClearLayer();
				}
			}
		}

		/// <summary>
		/// Resets all blueprint layers (painted cells) and build layers.
		/// Should be used when building a map at runtime - Call ResetConfiguration on Start / Initialization
		/// </summary>
		public void ResetConfiguration()
		{
			for (int i = 0; i < configuration.blueprintLayerFolders.Count; i++)
			{
				for (int j = 0; j < configuration.blueprintLayerFolders[i].blueprintLayers.Count; j++)
				{
					configuration.blueprintLayerFolders[i].blueprintLayers[j].ClearLayer();
				}
			}

			for (int i = 0; i < configuration.buildLayerFolders.Count; i++)
			{
				for (int j = 0; j < configuration.buildLayerFolders[i].buildLayers.Count; j++)
				{
					configuration.buildLayerFolders[i].buildLayers[j].ResetLayer(this);
				}
			}
		}

		/// <summary>
		/// Generates a complete map. Executes blueprint layer stack and build layer stack
		/// </summary>
		public void GenerateCompleteMap()
		{

			ExecuteBlueprintLayers();
			ExecuteBuildLayers(ExecutionMode.FromScratch);

			OnMapReady?.Invoke();

		}

		internal void SetProgress(float _progress)
		{
			OnMapProgress?.Invoke(_progress);
		}

		/// <summary>
		/// Executes blueprint layer stack
		/// </summary>
		public void ExecuteBlueprintLayers()
		{
			if (configuration == null) return;

			configuration.ExecuteBlueprintLayers(this);

			OnBlueprintLayersReady?.Invoke();
		}

		/// <summary>
		/// Executes build layer stack. 
		/// Execution Mode Normal: Re-builds only changes.
		/// Execution Mode FromScratch: Forces a complete re-build of the map.
		/// </summary>
		public void ExecuteBuildLayers(ExecutionMode executionMode = ExecutionMode.Normal)
		{
			if (configuration == null) return;

			if (executionMode == ExecutionMode.Normal)
			{
				configuration.ExecuteBuildLayers(this);
			}
			else
			{
				configuration.ExecuteBuildLayers(this, true);
			}

			OnBuildLayersReady?.Invoke();
		}


#if TWC_DEBUG
		public void ExecuteAllBuildLayersDelayed()
		{
			if (configuration == null) return;

			if (!delayedExecutionRunning)
			{
				delayedExecutionRunning = true;

				StartCoroutine(ExecuteAllBuildLayersDelayedIE());
				// TileWorldCreator.Utilities.EditorCoroutines.Execute(ExecuteAllBuildLayersDelayedIE());
			}

			OnBuildLayersReady?.Invoke();
		}
#endif

		IEnumerator ExecuteAllBuildLayersDelayedIE()
		{
			var _startTime = Time.realtimeSinceStartup;
			while (Time.realtimeSinceStartup < _startTime + 1f)
			{
				yield return null;
			}
			configuration.ExecuteBuildLayers(this, true);
			delayedExecutionRunning = false;
		}

		public void ClearConfiguration()
		{
#if UNITY_EDITOR
			for (int i = 0; i < configuration.blueprintLayerFolders.Count; i++)
			{
				for (int j = 0; j < configuration.blueprintLayerFolders[i].blueprintLayers.Count; j++)
				{
					var _layer = configuration.blueprintLayerFolders[i].blueprintLayers[j];
					if (_layer != null)
					{
						AssetDatabase.RemoveObjectFromAsset(_layer);
						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();

						MonoBehaviour.DestroyImmediate(_layer);
					}
				}
			}

			for (int i = 0; i < configuration.buildLayerFolders.Count; i++)
			{
				for (int j = 0; j < configuration.buildLayerFolders[i].buildLayers.Count; j++)
				{
					var _layer = configuration.buildLayerFolders[i].buildLayers[j];
					if (_layer != null)
					{
						AssetDatabase.RemoveObjectFromAsset(_layer);
						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();

						MonoBehaviour.DestroyImmediate(_layer);
					}

					var _layers = this.GetComponentsInChildren<LayerIdentifier>(true);
					// var _layers = GameObject.FindObjectsByType<LayerIdentifier>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
					foreach (var _layerIdentifier in _layers)
					{
						if (_layerIdentifier.guid == _layer.guid)
						{
							MonoBehaviour.DestroyImmediate(_layerIdentifier.gameObject);
						}
					}
				}
			}

			configuration.blueprintLayerFolders = new List<BlueprintLayerFolder>();
			configuration.buildLayerFolders = new List<BuildLayerFolder>();
#endif
		}

		public BlueprintLayer AddNewBlueprintLayer(string _layerName = "")
		{
			var _layer = ScriptableObject.CreateInstance(typeof(BlueprintLayer));

			(_layer as BlueprintLayer).SetAsset(configuration);
			(_layer as BlueprintLayer).layerName = _layerName;


#if UNITY_EDITOR
			_layer.hideFlags = HideFlags.HideInHierarchy;
			AssetDatabase.AddObjectToAsset(_layer, configuration);
			// EditorUtility.SetDirty(_layer);

			var _so = new SerializedObject(configuration);
			_so.ApplyModifiedProperties();
			_so.Update();
#endif

			if (configuration.blueprintLayerFolders == null || configuration.blueprintLayerFolders.Count == 0)
			{
				configuration.blueprintLayerFolders.Add(new BlueprintLayerFolder("Root"));
			}

			configuration.blueprintLayerFolders.FirstOrDefault().blueprintLayers.Add(_layer as BlueprintLayer);

			return _layer as BlueprintLayer;
		}

		public T AddNewBuildLayer<T>(string _layerName = "") where T : BuildLayer
		{
			var _layer = ScriptableObject.CreateInstance(typeof(T));

			(_layer as BuildLayer).layerName = _layerName;

#if UNITY_EDITOR
			_layer.hideFlags = HideFlags.HideInHierarchy;
			AssetDatabase.AddObjectToAsset(_layer, configuration);
			// EditorUtility.SetDirty(_layer);
			var _so = new SerializedObject(configuration);
			_so.ApplyModifiedProperties();
			_so.Update();
#endif

			if (configuration.buildLayerFolders == null || configuration.buildLayerFolders.Count == 0)
			{
				configuration.buildLayerFolders.Add(new BuildLayerFolder("Root"));
			}

			configuration.buildLayerFolders.FirstOrDefault().buildLayers.Add(_layer as BuildLayer);
			return (T)_layer;
		}

		public void RemoveBlueprintLayer(string _layerName)
		{
			var _layer = GetBlueprintLayer(_layerName);

#if UNITY_EDITOR
			AssetDatabase.RemoveObjectFromAsset(_layer);
			// AssetDatabase.SaveAssets();
			var _so = new SerializedObject(configuration);
			_so.ApplyModifiedProperties();
			_so.Update();
#endif

			for (int i = 0; i < configuration.blueprintLayerFolders.Count; i++)
			{
				for (int j = 0; j < configuration.blueprintLayerFolders[i].blueprintLayers.Count; j++)
				{
					if (configuration.blueprintLayerFolders[i].blueprintLayers[j] == _layer)
					{
						configuration.blueprintLayerFolders[i].blueprintLayers.RemoveAt(j);
					}
				}
			}

			MonoBehaviour.DestroyImmediate(_layer);
		}

		public void RemoveBuildLayer(string _layerName)
		{
			var _layer = GetBuildLayer(_layerName);

#if UNITY_EDITOR
			AssetDatabase.RemoveObjectFromAsset(_layer);
			// AssetDatabase.SaveAssets();

			var _so = new SerializedObject(configuration);
			_so.ApplyModifiedProperties();
			_so.Update();
#endif

			for (int i = 0; i < configuration.buildLayerFolders.Count; i++)
			{
				for (int j = 0; j < configuration.buildLayerFolders[i].buildLayers.Count; j++)
				{
					if (configuration.buildLayerFolders[i].buildLayers[j] == _layer)
					{
						configuration.buildLayerFolders[i].buildLayers.RemoveAt(j);
					}
				}
			}

			var _layers = this.GetComponentsInChildren<LayerIdentifier>(true);
			// var _layers = GameObject.FindObjectsByType<LayerIdentifier>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);
			foreach (var _layerIdentifier in _layers)
			{
				if (_layerIdentifier.guid == _layer.guid)
				{
					MonoBehaviour.DestroyImmediate(_layerIdentifier.gameObject);
				}
			}

			MonoBehaviour.DestroyImmediate(_layer);
		}

		public GameObject GetClusterObject(string _buildLayerName, Vector2 _atPosition)
		{
			var _layer = GetBuildLayer(_buildLayerName);
			return (_layer as TilesBuildLayer).FindClusterByPosition(_atPosition);
		}


		/// <summary>
		/// Returns a blueprint folder class by its folder name.
		/// Containing all blueprint layers in this folder
		/// </summary>
		/// <param name="_folderName"></param>
		/// <returns></returns>
		public BlueprintLayerFolder GetBlueprintLayerFolder(string _folderName)
		{
			if (configuration == null) return null;

			for (int i = 0; i < configuration.blueprintLayerFolders.Count; i++)
			{
				if (configuration.blueprintLayerFolders[i].folderName == _folderName)
				{
					return configuration.blueprintLayerFolders[i];
				}
			}

			return null;
		}

		/// <summary>
		/// Returns a BlueprintLayer object.
		/// </summary>
		/// <param name="_layerName"></param>
		/// <returns></returns>
		public BlueprintLayer GetBlueprintLayer(string _layerName)
		{
			if (configuration == null) return null;

			for (int i = 0; i < configuration.blueprintLayerFolders.Count; i++)
			{
				for (int j = 0; j < configuration.blueprintLayerFolders[i].blueprintLayers.Count; j++)
				{
					if (configuration.blueprintLayerFolders[i].blueprintLayers[j].layerName == _layerName)
					{
						return configuration.blueprintLayerFolders[i].blueprintLayers[j];
					}
				}
			}

			return null;
		}
		
		/// <summary>
		/// Returns a Blueprint Layer by its guid.
		/// </summary>
		/// <param name="_guid"></param>
		/// <returns></returns>
		public BlueprintLayer GetBlueprintLayerByGuid(string _guid)
		{
			if (configuration == null) return null;
			return configuration.GetBlueprintLayerByGuid(_guid);
		}

		/// <summary>
		/// Returns a BuildLayer object.
		/// </summary>
		/// <param name="_layerName"></param>
		/// <returns></returns>
		public BuildLayer GetBuildLayer(string _layerName)
		{
			if (configuration == null) return null;

			for (int i = 0; i < configuration.buildLayerFolders.Count; i++)
			{
				for (int j = 0; j < configuration.buildLayerFolders[i].buildLayers.Count; j++)
				{
					if (configuration.buildLayerFolders[i].buildLayers[j].layerName == _layerName)
					{
						return configuration.buildLayerFolders[i].buildLayers[j];
					}
				}
			}

			return null;
		}

		public BuildLayer GetBuildLayerByGuid(string _guid)
		{
			if (configuration == null) return null;

			for (int i = 0; i < configuration.buildLayerFolders.Count; i++)
			{
				for (int j = 0; j < configuration.buildLayerFolders[i].buildLayers.Count; j++)
				{
					if (configuration.buildLayerFolders[i].buildLayers[j].guid == _guid)
					{
						return configuration.buildLayerFolders[i].buildLayers[j];
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Add cell positions to layer by guid
		/// </summary>
		/// <param name="_layerGuid"></param>
		/// <param name="_cellPositions"></param>
		public void AddCellsToLayerByGuid(string _layerGuid, HashSet<Vector2> _cellPositions)
		{
			var _layer = configuration.GetBlueprintLayerByGuid(_layerGuid);
			if (_layer == null)
			{
				Debug.LogError("Layer not found: " + _layerGuid);
				return;
			}
			_layer.AddCells(_cellPositions);
		}

		/// <summary>
		/// Add cell positions to layer by name
		/// </summary>
		/// <param name="_layerName"></param>
		/// <param name="_cellPositions"></param>
		public void AddCellsToLayer(string _layerName, HashSet<Vector2> _cellPositions)
		{
			var _layer = configuration.GetBlueprintLayerByGuid(configuration.GetBlueprintLayerGuid(_layerName));
			if (_layer == null)
			{
				Debug.LogError("Layer not found: " + _layerName);
				return;
			}

			_layer.AddCells(_cellPositions);
		}

		/// <summary>
		/// Remove cell positions from layer by name
		/// </summary>
		/// <param name="_layerName"></param>
		/// <param name="_cellPositions"></param>
		public void RemoveCellsFromLayer(string _layerName, HashSet<Vector2> _cellPositions)
		{
			var _layer = configuration.GetBlueprintLayerByGuid(configuration.GetBlueprintLayerGuid(_layerName));
			if (_layer == null)
			{
				Debug.LogError("Layer not found: " + _layerName);
				return;
			}
			_layer.RemoveCells(_cellPositions);
		}

		/// <summary>
		/// Clear all cell positions from a blueprint layer
		/// </summary>
		/// <param name="_layerName"></param>
		public void ClearLayer(string _layerName)
		{
			var _layer = configuration.GetBlueprintLayerByGuid(configuration.GetBlueprintLayerGuid(_layerName));
			if (_layer == null)
			{
				Debug.LogError("Layer not found: " + _layerName);
				return;
			}
			_layer.ClearLayer();
		}

		/// <summary>
		/// Fill all cells from a blueprint layer
		/// </summary>
		/// <param name="_layerName"></param>
		public void FillLayer(string _layerName)
		{
			var _layer = configuration.GetBlueprintLayerByGuid(configuration.GetBlueprintLayerGuid(_layerName));
			if (_layer == null)
			{
				Debug.LogError("Layer not found: " + _layerName);
				return;
			}
			_layer.FillLayer();
		}

		/// <summary>
		/// Convert world position to grid position relative to the TileWorldCreator Manager transforms.
		/// </summary>
		/// <param name="worldPos"></param>
		/// <returns></returns>
		public Vector2 GetRelativeGridPosition(Vector3 worldPos)
		{
			// Convert world position to local space relative to the rotated grid
			Vector3 localPos = this.transform.InverseTransformPoint(worldPos);

			// Snap to the closest grid cell
			int gridX = Mathf.RoundToInt(localPos.x / this.configuration.cellSize);
			int gridY = Mathf.RoundToInt(localPos.z / this.configuration.cellSize);

			return new Vector2(gridX, gridY);
		}

		/// <summary>
		/// Returns true or false if position is over grid.
		/// </summary>
		/// <param name="_relativePosition"></param>
		/// <returns></returns>
		public bool IsRelativePositionOverGrid(Vector2 _relativePosition)
		{
			bool _return = false;
			var _gridPos = _relativePosition;

			var _cellSize = configuration.cellSize;
			if (_gridPos.x >= 0 && _gridPos.y >= 0 && _gridPos.x < (configuration.width * _cellSize) &&
			_gridPos.y < (configuration.height * _cellSize))
			{
				_return = true;
			}

			return _return;
		}

		/// <summary>
		/// Enable or disable a blueprint layer
		/// </summary>
		/// <param name="_state"></param>
		public void SetBlueprintLayerActiveState(string _layerName, bool _state)
		{
			var _layer = configuration.GetBlueprintLayerByGuid(configuration.GetBlueprintLayerGuid(_layerName));
			if (_layer == null)
			{
				Debug.LogError("Layer not found: " + _layerName);
				return;
			}
			_layer.isEnabled = _state;
		}

		/// <summary>
		/// Enable or disable a build layer
		/// </summary>
		/// <param name="_layerName"></param>
		/// <param name="_state"></param>
		public void SetBuildLayerActiveState(string _layerName, bool _state)
		{
			var _layer = configuration.GetBuildLayerByGuid(configuration.GetBuildLayerGuid(_layerName));
			if (_layer == null)
			{
				Debug.LogError("Layer not found: " + _layerName);
				return;
			}
			_layer.isEnabled = _state;
		}

		public bool CellPositionExists(string _layerName, Vector2 _position)
		{
			var _layer = configuration.GetBlueprintLayerByGuid(configuration.GetBlueprintLayerGuid(_layerName));
			if (_layer == null)
			{
				Debug.LogError("Layer not found: " + _layerName);
				return false;
			}
			return _layer.allPositions.Contains(_position);
		}

		/// <summary>
		/// Returns a HashSet<Vector2> of all blueprint cell positions within radius from position.
		/// </summary>
		/// <param name="_layerName"></param>
		/// <param name="_position"></param>
		/// <param name="_radius"></param>
		/// <returns></returns>
		public HashSet<Vector2> GetCellPositionsInRadius(string _layerName, Vector3 _position, float _radius)
		{
			var _layer = configuration.GetBlueprintLayerByGuid(configuration.GetBlueprintLayerGuid(_layerName));
			if (_layer == null)
			{
				Debug.LogError("Layer not found: " + _layerName);
				return null;
			}

			return _layer.GetCellPositionsInRadius(GetRelativeGridPosition(_position), _radius);
		}

		/// <summary>
		/// Returns a TileData object from position
		/// </summary>
		/// <param name="_buildLayerName"></param>
		/// <param name="_position"></param>
		/// <returns></returns>
		public BuildLayer.TileData GetBuildLayerTileDataFromPosition(string _buildLayerName, Vector2 _position)
		{
			var _layer = configuration.GetBuildLayerByGuid(configuration.GetBuildLayerGuid(_buildLayerName));
			if (_layer == null)
			{
				Debug.LogError("Layer not found: " + _buildLayerName);
				return new BuildLayer.TileData();
			}

			return (_layer as BuildLayer).GetTileDataFromPosition(_position);
		}


		/// <summary>
		/// Returns the highest layer height including build layer Y offset at world position
		/// </summary>
		/// <param name="worldPos"></param>
		/// <returns></returns>
		public float SampleLayerHeight(Vector3 worldPos)
		{
			// Get default blueprint layer height
			var _defaultHeight = 0f;
			var _gridPosition = GetRelativeGridPosition(worldPos);

			for (int j = 0; j < configuration.blueprintLayerFolders.Count; j++)
			{
				for (int i = 0; i < configuration.blueprintLayerFolders[j].blueprintLayers.Count; i++)
				{

					if (configuration.blueprintLayerFolders[j].blueprintLayers[i].allPositions.Contains(_gridPosition))
					{
						if (configuration.blueprintLayerFolders[j].blueprintLayers[i].isEnabled && configuration.blueprintLayerFolders[j].blueprintLayers[i].defaultLayerHeight > _defaultHeight)
						{
							_defaultHeight = configuration.blueprintLayerFolders[j].blueprintLayers[i].defaultLayerHeight;
						}
					}
				}
			}
			
			// Get build layer Y offset
			var _layerYOffset = 0f;
			for (int j = 0; j < configuration.buildLayerFolders.Count; j++)
			{
				for (int i = 0; i < configuration.buildLayerFolders[j].buildLayers.Count; i++)
				{
					var _tileData = (configuration.buildLayerFolders[j].buildLayers[i] as TilesBuildLayer).GetTileDataFromPosition(_gridPosition);

					if (_tileData.tilePosition != Vector2.zero && (configuration.buildLayerFolders[j].buildLayers[i] as TilesBuildLayer).layerYOffset > _layerYOffset)
					{
						_layerYOffset = (configuration.buildLayerFolders[j].buildLayers[i] as TilesBuildLayer).layerYOffset;
					}
				}
			}

			return _defaultHeight + _layerYOffset;
		}


		public void DrawSceneGUI()
		{
#if TWC_DEBUG
			for (int i = 0; i < configuration.tileMapLayers.Count; i++)
			{
				configuration.tileMapLayers[i].OnDebugDraw();
			}
#endif
		}

#if UNITY_EDITOR

		public void OnDrawGizmos()
		{
			if (configuration == null) return;

#if TWC_DEBUG
			for (int i = 0; i < configuration.blueprintLayerFolders.Count; i++)
			{
				for (int j = 0; j < configuration.blueprintLayerFolders[i].blueprintLayers.Count; j++)
				{
					configuration.blueprintLayerFolders[i].blueprintLayers[j].OnDebugDraw();
				}
			}
#endif

			if (configuration.showPaintGrid)
			{
				var _gridHeight = 0f;
				if (configuration.paintLayer != null)
				{
					_gridHeight = configuration.paintLayer.defaultLayerHeight;
				}

				// Draw grid
				DrawGrid(_gridHeight);

				// Draw layer cells
				if (configuration.paintLayer != null)
				{
					foreach (var _pos in configuration.paintLayer.allPositions)
					{
						// var _pos2 = new Vector3(this.transform.localPosition.x + (_pos.x * configuration.cellSize), this.transform.localPosition.y + _gridHeight, this.transform.localPosition.z + (_pos.y * configuration.cellSize));

						// Compute the local position of the painted cell
						Vector3 localPos = new Vector3(_pos.x * configuration.cellSize,  _gridHeight + -0.05f, _pos.y * configuration.cellSize);

						// Transform it to world space
						Vector3 worldPos = transform.TransformPoint(localPos);
						// Save the current Gizmos matrix
						Matrix4x4 oldMatrix = Gizmos.matrix;
						// Apply the GameObject's transformation matrix
						Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

						Gizmos.color = Color.white; //paintState ? Color.green : Color.red;
													// Draw the painted cell
						Gizmos.DrawCube(localPos, new Vector3(configuration.cellSize - 0.1f, 0.02f, configuration.cellSize - 0.1f));

						// Restore the previous Gizmos matrix
						Gizmos.matrix = oldMatrix;
						Gizmos.color = Color.white;
					}
				}

				// Draw painted cells
				foreach (var _pos in paintedPositions)
				{
					// var _pos2 = new Vector3(this.transform.localPosition.x + (_pos.x * configuration.cellSize), this.transform.localPosition.y, this.transform.localPosition.z + (_pos.y * configuration.cellSize));

					// Compute the local position of the painted cell
					Vector3 localPos = new Vector3(_pos.x * configuration.cellSize, _gridHeight + 0.05f, _pos.y * configuration.cellSize);

					// Transform it to world space
					Vector3 worldPos = transform.TransformPoint(localPos);
					// Save the current Gizmos matrix
					Matrix4x4 oldMatrix = Gizmos.matrix;
					// Apply the GameObject's transformation matrix
					Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

					Gizmos.color = paintState ? Color.green : Color.red;
					// Draw the painted cell
					Gizmos.DrawCube(localPos, new Vector3(configuration.cellSize - 0.1f, 0.02f, configuration.cellSize - 0.1f));

					// Restore the previous Gizmos matrix
					Gizmos.matrix = oldMatrix;
					Gizmos.color = Color.white;
				}



				DrawCursor(_gridHeight);
			}

			// #if TWC_DEBUG
			if (showClusterCellsDebug)
			{
				DebugDrawClusterCells();
			}
			// #endif
		}

		void DrawGrid(float _gridHeight)
		{
		

			// Save current Gizmos matrix
				Matrix4x4 oldMatrix = Gizmos.matrix;

			// Apply GameObject's transformation
			Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

			Gizmos.color = new Color(120f / 255f, 120f / 255f, 120f / 255f, 0.5f);

			for (int x = 0; x <= configuration.width; x++)
			{
				Vector3 start = new Vector3(x * configuration.cellSize - (configuration.cellSize * 0.5f), _gridHeight, 0 - (configuration.cellSize * 0.5f));
				Vector3 end = new Vector3(x * configuration.cellSize - (configuration.cellSize * 0.5f), _gridHeight, configuration.height * configuration.cellSize - (configuration.cellSize * 0.5f));
				Gizmos.DrawLine(start, end);
			}

			for (int y = 0; y <= configuration.height; y++)
			{
				Vector3 start = new Vector3(0 - (configuration.cellSize * 0.5f), _gridHeight, y * configuration.cellSize - (configuration.cellSize * 0.5f));
				Vector3 end = new Vector3(configuration.width * configuration.cellSize - (configuration.cellSize * 0.5f), _gridHeight, y * configuration.cellSize - (configuration.cellSize * 0.5f));
				Gizmos.DrawLine(start, end);
			}

			// Restore previous Gizmos matrix
			Gizmos.matrix = oldMatrix;
		}

		void DrawCursor(float _gridHeight)
		{
			var _wp = GetWorldPosition(Event.current.mousePosition, _gridHeight);
			var _position = GetRelativeGridPosition(_wp - new Vector3(0, _gridHeight, 0));
			int _halfSize = Mathf.CeilToInt(configuration.brushSize * 0.5f);
			float _radius = configuration.brushSize * 0.5f;
			var _gridPos = new Vector2(_position.x, _position.y);

			Matrix4x4 _oldMatrix = Gizmos.matrix;

			Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
			Gizmos.color = Color.yellow;

			for (int x = -_halfSize; x <= _halfSize; x++)
			{
				for (int y = -_halfSize; y <= _halfSize; y++)
				{
					Vector2 _pos = new Vector2(_gridPos.x + x, _gridPos.y + y);


					if (Vector2.Distance(_gridPos, _pos) <= _radius)
					{
						Vector3 localPos = new Vector3(_pos.x * configuration.cellSize, _gridHeight, _pos.y * configuration.cellSize);
						Gizmos.DrawCube(localPos, new Vector3(configuration.cellSize, 0.1f, configuration.cellSize));

					}
				}
			}

			// Restore the previous Gizmos matrix
			Gizmos.matrix = _oldMatrix;
			Gizmos.color = Color.white;
		}

		Vector3 GetWorldPosition(Vector2 _mousePos, float _gridHeight = 0)
		{
			Ray _ray = HandleUtility.GUIPointToWorldRay(_mousePos);

			// Create a plane that fully aligns with the grid's rotation
			Plane gridPlane = new Plane(transform.rotation * Vector3.up, new Vector3( transform.position.x, transform.position.y + _gridHeight, transform.position.z));

			float _dist;
			if (gridPlane.Raycast(_ray, out _dist))
			{
				return _ray.GetPoint(_dist); // Get the correct position on the rotated plane
			}

			return Vector3.zero; // Fallback case
		}

		void DebugDrawClusterCells()
		{
			// if (configuration == null)
			// 	return;

			// var _clusterSize = configuration.clusterCellSize;
			//
			// if (_clusterSize <= 0)
			// {
			// 	return;
			// }
			//
			//
			// var _x = Mathf.CeilToInt((configuration.width / _clusterSize) * configuration.cellSize - configuration.cellSize * 0.5f);
			// var _y = Mathf.CeilToInt((configuration.height / _clusterSize) * configuration.cellSize - configuration.cellSize * 0.5f);
			//
			//
			// for (int x = 0; x < _x + 1; x++)
			// {
			// 	for (int y = 0; y < _y + 1; y++)
			// 	{
			//
			// 		Vector3 _pos = Vector3.zero;
			// 		_pos = new Vector3(transform.localPosition.x + (x * _clusterSize + (_clusterSize * 0.5f)) - 0.5f, transform.localPosition.y, transform.localPosition.z + (y * _clusterSize + (_clusterSize * 0.5f)) - 0.5f);
			// 		if (IsVisibleByCamera(_pos))
			// 		{
			// 			Gizmos.color = colorGrey;
			// 			Handles.Label(_pos, (Mathf.Floor(_pos.x / configuration.clusterCellSize) + (configuration.clusterYMultiplier * Mathf.Floor(_pos.z / configuration.clusterCellSize))).ToString());
			// 			Gizmos.DrawWireCube(_pos, new Vector3(_clusterSize, 0.001f, _clusterSize));
			// 			Gizmos.color = Color.white;
			// 		}
			//
			// 	}
			// }
			// if (configuration == null)
			// 	return;
			//
			// int clusterSize = configuration.clusterCellSize;
			// float cellSize = configuration.cellSize;
			//
			// if (clusterSize <= 0 || cellSize <= 0)
			// 	return;
			//
			// int clusterCountX = Mathf.CeilToInt((float)configuration.width / clusterSize);
			// int clusterCountY = Mathf.CeilToInt((float)configuration.height / clusterSize);
			//
			// for (int cx = 0; cx < clusterCountX; cx++)
			// {
			// 	for (int cy = 0; cy < clusterCountY; cy++)
			// 	{
			// 		// Center of the cluster in world space
			// 		float worldX = transform.localPosition.x - (configuration.cellSize * 0.5f) + ((cx * clusterSize + clusterSize / 2f) * cellSize);
			// 		float worldZ = transform.localPosition.z - (configuration.cellSize * 0.5f) + ((cy * clusterSize + clusterSize / 2f) * cellSize);
			// 		Vector3 pos = new Vector3(worldX, transform.localPosition.y, worldZ);
			//
			// 		if (IsVisibleByCamera(pos))
			// 		{
			// 			Gizmos.color = colorGrey;
			//
			// 			// Compute cluster ID using same logic but from cx/cy directly
			// 			int clusterID = cx + (configuration.clusterYMultiplier * cy);
			//
			// 			Handles.Label(pos, clusterID.ToString());
			// 			Gizmos.DrawWireCube(pos, new Vector3(clusterSize * cellSize, 0.001f, clusterSize * cellSize));
			//
			// 			Gizmos.color = Color.white;
			// 		}
			// 	}
			// }
			if (configuration == null)
				return;

			int clusterSize = configuration.clusterCellSize;
			float cellSize = configuration.cellSize;

			if (clusterSize <= 0 || cellSize <= 0f)
				return;

			int clusterCountX = Mathf.CeilToInt((float)configuration.width / clusterSize);
			int clusterCountY = Mathf.CeilToInt((float)configuration.height / clusterSize);

			for (int cx = 0; cx < clusterCountX; cx++)
			{
				for (int cy = 0; cy < clusterCountY; cy++)
				{
					// Center of the cluster in world space
					float worldX = transform.localPosition.x - (configuration.cellSize * 0.5f) + ((cx * clusterSize + clusterSize / 2f) * cellSize);
					float worldZ = transform.localPosition.z - (configuration.cellSize * 0.5f) + ((cy * clusterSize + clusterSize / 2f) * cellSize);
					Vector3 pos = new Vector3(worldX, transform.localPosition.y, worldZ);

					if (IsVisibleByCamera(pos))
					{
						Gizmos.color = colorGrey;

						// Compute cluster ID using same logic but from cx/cy directly
						int clusterID = cx + (configuration.clusterYMultiplier * cy);

						Handles.Label(pos, clusterID.ToString());
						Gizmos.DrawWireCube(pos, new Vector3(clusterSize * cellSize, 0.001f, clusterSize * cellSize));

						Gizmos.color = Color.white;
					}
				}
			}
		}

		bool IsVisibleByCamera(Vector3 _pos)
		{
			Vector3 viewPos = Vector3.zero;
			if (sceneViewCamera == null)
			{
				if (Camera.current != null)
				{
					sceneViewCamera = Camera.current;
				}
			}

			if (sceneViewCamera != null)
			{
				viewPos = Camera.current.WorldToViewportPoint(_pos);
			}

			if (viewPos.x >= 0.0f && viewPos.x <= 1f && viewPos.y >= 0.0f && viewPos.y <= 1f)
			{
				return true;
			}
			else
			{
				return false;
			}
		}

#endif
	}
}
#pragma warning restore 0414