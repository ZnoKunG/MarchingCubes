using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.TerrainTools;
using UnityEngine;

[CustomEditor(typeof(SphereGenerator))]
public class GenerateMeshEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SphereGenerator sphereGen = (SphereGenerator)target;

        if (GUILayout.Button("Generate Mesh"))
        {
            sphereGen.UpdateMesh();
        }
    }
}
