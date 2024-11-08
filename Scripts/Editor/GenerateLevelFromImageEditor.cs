using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(GenerateLevelFromImage), true)]
public class GenerateLevelFromImageEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        GenerateLevelFromImage generater = (GenerateLevelFromImage)target;

        if (Application.isPlaying)
        {
            return;
        }

        if (GUILayout.Button("Generate"))
        {
            generater.Generate(generater.image, generater.settings);
        }

        if (GUILayout.Button("Delete"))
        {
            generater.DestroyChildren();
        }
    }
}
