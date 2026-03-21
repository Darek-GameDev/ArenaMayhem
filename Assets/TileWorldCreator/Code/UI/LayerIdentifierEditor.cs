// using GiantGrey.TileWorldCreator.Components;
// using UnityEditor;
// using UnityEngine;

// [CustomEditor(typeof(LayerIdentifier))]
// public class LayerIdentifierEditor : Editor
// {
//     void OnSceneGUI()
//     {
//         LayerIdentifier gizmo = (LayerIdentifier)target;

//         // Position above the GameObject
//         Vector3 labelPosition = gizmo.transform.position + Vector3.up * 2f;

//         // Draw the label with a pick handle
//         Handles.BeginGUI();

//         Vector3 screenPosition = HandleUtility.WorldToGUIPoint(labelPosition);
//         Rect rect = new Rect(screenPosition.x - 50, screenPosition.y - 10, 100, 20);

//         if (GUI.Button(rect, target.name))
//         {
//             Selection.activeGameObject = gizmo.gameObject;
//             EditorGUIUtility.PingObject(gizmo.gameObject);
//             // MonoBehaviour handle = (MonoBehaviour)target;

//             // GameObject[] selection = new GameObject[1];
//             // selection[0] = gizmo.transform.gameObject;
//             // Selection.objects =  selection;
//         }

//         Handles.EndGUI();

//         // Optional: Draw a line from the label to the object
//         Handles.color = Color.yellow;
//         Handles.DrawLine(gizmo.transform.position, labelPosition);
//     }
// }
