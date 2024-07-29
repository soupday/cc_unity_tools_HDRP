
using UnityEngine;
using UnityEditor;
using Reallusion.Import;

[CustomEditor(typeof(WrinkleManager))]
public class WrinkleManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI(); // Draw the default inspector

        WrinkleManager wrinkleManager = (WrinkleManager)target;

        if (GUILayout.Button("Update BlendShape Indices"))
        {
            wrinkleManager.UpdateBlendShapeIndices(); // Assuming this is a public method
        }
    }
}
