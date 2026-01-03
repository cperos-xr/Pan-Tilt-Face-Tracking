using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DecompressString))]
public class DecompressStringEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        DecompressString script = (DecompressString)target;

        GUILayout.Space(10);
        if (GUILayout.Button("Decompress Text (Test)"))
        {
            script.DecompressText();
        }
        if (GUILayout.Button("Compress Text (Test)"))
        {
            script.CompressText();
        }
    }
}
