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
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace GiantGrey.TileWorldCreator
{
    [Modifier(ModifierAttribute.Category.Generators, "Shapes", "")]
    public class Shapes : BlueprintModifier
    {
        public enum ShapeType
        {
            Circle,
            Square,
            Triangle,
        }

        [System.Serializable]
        public class AvailableShapes
        {
            public ShapeType shapeType;
            public bool rndPosition;
            public Vector2Int position;
            public Vector2Int size;
            public int radius;

            public enum Orientation
            {
                Up,
                Down,
                Left,
                Right
            }

            public Orientation orientation;
        }

        [HideInInspector]
        public List<AvailableShapes> shapes = new List<AvailableShapes>();

        BlueprintLayer blueprintLayer;
        ListView listView;

        public override HashSet<Vector2> Execute(HashSet<Vector2> _positions, BlueprintLayer _layer)
        {
            blueprintLayer = _layer;

            for (int i = 0; i < shapes.Count; i ++)
            {
                switch (shapes[i].shapeType)
                {
                    case ShapeType.Circle:
                        _positions = CircleShape(shapes[i], _positions);
                        break;
                    case ShapeType.Square:
                        _positions = SquareShape(shapes[i], _positions);
                        break;
                    case ShapeType.Triangle:
                        _positions = GenerateTriangle(shapes[i], _positions);
                        break;
                }
            }
            

            return _positions;
        }


        public HashSet<Vector2> CircleShape(AvailableShapes _shape, HashSet<Vector2> _positions)
        {
            var _position = _shape.position;

            if (_shape.rndPosition)
            {
                _position = new Vector2Int(blueprintLayer.random.NextInt(0, asset.width), blueprintLayer.random.NextInt(0, asset.height));
            }


            for (int x = 0; x < asset.width; x ++)
            {
                for (int y = 0; y < asset.height; y ++)
                {
                    // Get distance to center
                    var _pos = new Vector2Int(x, y);
                    var _dist = Vector2Int.Distance(_pos, _position);
                    if (_dist <= _shape.radius)
                    { 
                        _positions.Add(_pos);
                    }
                }
            }

            return _positions;
        }

        public HashSet<Vector2> SquareShape(AvailableShapes _shape, HashSet<Vector2> _positions)
        {
            var _initPosition = _shape.position;
            if (_shape.rndPosition)
            {
                _initPosition = new Vector2Int(blueprintLayer.random.NextInt(0, asset.width), blueprintLayer.random.NextInt(0, asset.height));
            }

            for (int x = -(int)(_shape.size.x * 0.5f); x < _shape.size.x * 0.5f; x ++)
            {
                for (int y = -(int)(_shape.size.y * 0.5f); y < _shape.size.y * 0.5f; y ++)
                {
                    var _pos = new Vector2(_initPosition.x + x, _initPosition.y + y);
                    if (_pos.x >= 0 && _pos.x < asset.width && _pos.y >= 0 && _pos.y < asset.height)
                    {
                        _positions.Add(_pos);
                    }
                }
            }

            return _positions;
        }

        public HashSet<Vector2> GenerateTriangle(AvailableShapes _shape, HashSet<Vector2> _positions)
        {
            var _center = _shape.position;
            if (_shape.rndPosition)
            {
                _center = new Vector2Int(blueprintLayer.random.NextInt(0, asset.width), blueprintLayer.random.NextInt(0, asset.height));
            }

            switch (_shape.orientation)
            {
                case AvailableShapes.Orientation.Up:
                    for (int y = 0; y < _shape.radius; y++)
                    {
                        int startX = _center.x - y;
                        int endX = _center.x + y;
                        for (int x = startX; x <= endX; x++)
                        {
                            var _pos = new Vector2Int(x, _center.y - y);
                            if (_pos.x >= 0 && _pos.x < asset.width && _pos.y >= 0 && _pos.y < asset.height)
                            {
                                _positions.Add(_pos);
                            }
                        }
                    }
                    break;
                
                case AvailableShapes.Orientation.Down:
                    for (int y = 0; y < _shape.radius; y++)
                    {
                        int startX = _center.x - y;
                        int endX = _center.x + y;
                        for (int x = startX; x <= endX; x++)
                        {
                            var _pos = new Vector2Int(x, _center.y + y);
                            if (_pos.x >= 0 && _pos.x < asset.width && _pos.y >= 0 && _pos.y < asset.height)
                            {
                                _positions.Add(_pos);
                            }
                        }
                    }
                    break;
                
                case AvailableShapes.Orientation.Left:
                    for (int x = 0; x < _shape.radius; x++)
                    {
                        int startY = _center.y - x;
                        int endY = _center.y + x;
                        for (int y = startY; y <= endY; y++)
                        {
                            var _pos = new Vector2Int(_center.x - x, y);
                            if (_pos.x >= 0 && _pos.x < asset.width && _pos.y >= 0 && _pos.y < asset.height)
                            {
                                _positions.Add(_pos);
                            }
                        }
                    }
                    break;
                
                case AvailableShapes.Orientation.Right:
                    for (int x = 0; x < _shape.radius; x++)
                    {
                        int startY = _center.y - x;
                        int endY = _center.y + x;
                        for (int y = startY; y <= endY; y++)
                        {
                            var _pos = new Vector2Int(_center.x + x, y);
                            if (_pos.x >= 0 && _pos.x < asset.width && _pos.y >= 0 && _pos.y < asset.height)
                            {
                                _positions.Add(_pos);
                            }   
                        }
                    }
                    break;
            }
            
            return _positions;
        }

#if UNITY_EDITOR
        public override VisualElement BuildInspector(Configuration _asset)
        {
            var _root = new VisualElement();

            if (listView == null)
            {
                listView = new ListView();
            }
            else
            {
                listView.Rebuild();
            }
            
            listView.itemsSource = shapes;
            listView.makeItem = () => { return new ShapesListItem(this);};
            listView.bindItem = (element, i) =>
            {
                element.userData = i;
                (element as ShapesListItem).Bind(i);
            };

            listView.reorderable = true;
            listView.reorderMode = ListViewReorderMode.Animated;
            listView.showBorder = true;
            listView.showAddRemoveFooter = true;
            listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;


            _root.Add(listView);

            return _root;
        }

        public void UpdateList()
        {
            listView.Rebuild();
        }
#endif
    }

#if UNITY_EDITOR
    public class ShapesListItem : VisualElement
    {
        public Vector2Int position;
        public int radius;
        public bool rndPosition;
        Shapes shapes;

        public ShapesListItem(Shapes _shapes)
        {
            this.shapes = _shapes;
        }

        public void Bind(int _index)
        {
            this.Clear();

            var _so = new SerializedObject(shapes);
            var _serializedProperty = _so.FindProperty("shapes").GetArrayElementAtIndex(_index);

            var _type = _serializedProperty.FindPropertyRelative("shapeType");
            var _rndPosition = _serializedProperty.FindPropertyRelative("rndPosition");
            var _position = _serializedProperty.FindPropertyRelative("position");
            var _radius = _serializedProperty.FindPropertyRelative("radius");
            var _size = _serializedProperty.FindPropertyRelative("size");
            var _orientation = _serializedProperty.FindPropertyRelative("orientation");

            var _typeField = new EnumField();
            _typeField.RegisterValueChangedCallback(evt => { if (evt.newValue != evt.previousValue) {shapes.UpdateList();} });
            _typeField.BindProperty(_type);

            var _rndPositionField = new PropertyField();
            _rndPositionField.BindProperty(_rndPosition);

            var _positionField = new PropertyField();
            _positionField.BindProperty(_position);

            var _radiusField = new PropertyField();
            _radiusField.BindProperty(_radius);

            var _sizeField = new PropertyField();
            _sizeField.BindProperty(_size);

            var _orientationField = new PropertyField();
            _orientationField.BindProperty(_orientation);

            switch (_type.enumValueIndex)
            {
                case 0:
                    Add(_typeField);
                    Add(_rndPositionField);
                    Add(_positionField);
                    Add(_radiusField);
                    break;
                case 1:
                    Add(_typeField);
                    Add(_rndPositionField);
                    Add(_positionField);
                    Add(_sizeField);
                    break;
                case 2:
                    Add(_typeField);
                    Add(_rndPositionField);
                    Add(_positionField);
                    Add(_radiusField);
                    Add(_orientationField);
                    break;
            }
            
        }
    }
#endif
}