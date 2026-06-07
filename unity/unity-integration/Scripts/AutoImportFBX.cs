using UnityEngine;
using UnityEditor;
using System.IO;

public class AutoPlaceFBX : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            if (assetPath.EndsWith(".fbx"))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab != null)
                {
                    GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    instance.transform.position = Vector3.zero;
                    Debug.Log($"✅ Auto-placed {Path.GetFileName(assetPath)} in scene.");
                }
            }
        }
    }
}
