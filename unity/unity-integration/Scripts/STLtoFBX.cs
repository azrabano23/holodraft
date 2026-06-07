using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using System.IO;

public class STLtoFBXConverterEditor : EditorWindow
{
    private string stlPath = "";
    private string outputPath = "";

    [MenuItem("Tools/STL to FBX Converter")]
    public static void ShowWindow()
    {
        GetWindow<STLtoFBXConverterEditor>("STL to FBX Converter");
    }

    private void OnGUI()
    {
        GUILayout.Label("STL to FBX Converter", EditorStyles.boldLabel);

        if (GUILayout.Button("Select STL File"))
        {
            stlPath = EditorUtility.OpenFilePanel("Select STL File", "", "stl");
            outputPath = Path.Combine(Application.dataPath, "Models", Path.GetFileNameWithoutExtension(stlPath) + ".fbx");
        }

        GUILayout.Label("STL Path: " + stlPath);
        GUILayout.Label("Output FBX Path: " + outputPath);

        if (GUILayout.Button("Convert"))
        {
            RunBlenderConversion(stlPath, outputPath);
            AssetDatabase.Refresh();
        }
    }

    private void RunBlenderConversion(string inputStl, string outputFbx)
    {
        string blenderPath = @"C:\Program Files\Blender Foundation\Blender 4.4\blender.exe";
        string scriptPath = @"C:\Users\aiden\convert_stl_to_fbx.py";

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = blenderPath,
            Arguments = $"--background --python \"{scriptPath}\" -- \"{inputStl}\" \"{outputFbx}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        Process process = Process.Start(startInfo);
        process.WaitForExit();
    }
}
