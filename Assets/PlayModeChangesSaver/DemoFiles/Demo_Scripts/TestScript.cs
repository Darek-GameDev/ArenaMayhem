using UnityEngine;
using System.Collections.Generic;

public class TestScript : MonoBehaviour
{
    // Custom Enum
    public enum TestEnum { ValueA = 1, ValueB = 2, ValueC = 3 }
    
    // ===== PRIMITIVES =====
    // int
    public int publicInt = 1;
    
    // float
    public float publicFloat = 1f;
    
    // bool
    public bool publicBool = false;
    
    // string
    public string publicString = "a";
    
    // char
    public char publicChar = 'a';
    
    // ===== VECTORS =====
    // Vector2
    public Vector2 publicVector2 = new Vector2(1f, 1f);
    
    // Vector3
    public Vector3 publicVector3 = new Vector3(1f, 1f, 1f);
    
    // Vector4
    public Vector4 publicVector4 = new Vector4(1f, 1f, 1f, 1f);
    
    // Vector2Int
    public Vector2Int publicVector2Int = new Vector2Int(1, 1);
    
    // Vector3Int
    public Vector3Int publicVector3Int = new Vector3Int(1, 1, 1);
    
    // ===== COLORS =====
    // Color
    public Color publicColor = new Color(1f, 1f, 1f, 1f);
    
    // Color32
    public Color32 publicColor32 = new Color32(1, 1, 1, 1);
    
    // ===== UNITY TYPES =====
    // LayerMask
    public LayerMask publicLayerMask = 1;
    
    // Gradient
    public Gradient publicGradient = new Gradient();
    
    // Rect
    public Rect publicRect = new Rect(1f, 1f, 1f, 1f);
    
    // Bounds
    public Bounds publicBounds = new Bounds(new Vector3(1f, 1f, 1f), new Vector3(1f, 1f, 1f));
    
    // Quaternion
    public Quaternion publicQuaternion = Quaternion.Euler(1f, 1f, 1f);
    
    // ===== OBJECT REFERENCES =====
    // GameObject
    public GameObject publicGameObject;
    
    // Transform
    public Transform publicTransform;
    
    // Rigidbody
    public Rigidbody publicRigidbody;
    
    // Material
    public Material publicMaterial;
    
    // Mesh
    public Mesh publicMesh;
    
    // Sprite
    public Sprite publicSprite;
    
    // Texture
    public Texture publicTexture;
    
    // AnimationClip
    public AnimationClip publicAnimationClip;
    
    // ===== ENUMS =====
    // Custom Enum
    public TestEnum publicTestEnum = TestEnum.ValueA;
    
    // Built-in Unity Enum
    public KeyCode publicKeyCode = KeyCode.A;
    
    // ===== ARRAYS =====
    // int Array
    public int[] publicIntArray = {1, 1};
    
    // float Array
    public float[] publicFloatArray = {1f, 1f};
    
    // bool Array
    public bool[] publicBoolArray = {false, false};
    
    // string Array
    public string[] publicStringArray = {"a", "a"};
    
    // Vector2 Array
    public Vector2[] publicVector2Array = {new Vector2(1f, 1f), new Vector2(1f, 1f)};
    
    // Vector3 Array
    public Vector3[] publicVector3Array = {new Vector3(1f, 1f, 1f), new Vector3(1f, 1f, 1f)};
    
    // GameObject Array
    public GameObject[] publicGameObjectArray = new GameObject[2];
    
    // Transform Array
    public Transform[] publicTransformArray = new Transform[2];
    
    // Material Array
    public Material[] publicMaterialArray = new Material[2];
    
    // ===== LISTS =====
    // int List
    public List<int> publicIntList = new List<int> {1, 1};
    
    // float List
    public List<float> publicFloatList = new List<float> {1f, 1f};
    
    // bool List
    public List<bool> publicBoolList = new List<bool> {false, false};
    
    // string List
    public List<string> publicStringList = new List<string> {"a", "a"};
    
    // Vector3 List
    public List<Vector3> publicVector3List = new List<Vector3> {new Vector3(1f, 1f, 1f), new Vector3(1f, 1f, 1f)};
    
    // Color List
    public List<Color> publicColorList = new List<Color> {new Color(1f, 1f, 1f, 1f), new Color(1f, 1f, 1f, 1f)};
    
    // GameObject List
    public List<GameObject> publicGameObjectList = new List<GameObject> {null, null};
    
    // Transform List
    public List<Transform> publicTransformList = new List<Transform> {null, null};
    
    // Material List
    public List<Material> publicMaterialList = new List<Material> {null, null};
}
