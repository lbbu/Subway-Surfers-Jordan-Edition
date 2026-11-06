using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR

[CustomEditor(typeof(ChunkParent))]
public class ChunkParentButton : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // draws your normal fields

        ChunkParent script = (ChunkParent)target;

        if (GUILayout.Button("Populate Coin Positions"))
        {
            script.BuildTheCoinStartPositions();
        }
    }
}

#endif