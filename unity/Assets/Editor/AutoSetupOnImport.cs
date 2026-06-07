using UnityEngine;
using UnityEditor;

public class AutoSetupOnImport : AssetPostprocessor
{
    void OnPostprocessModel(GameObject model)
    {
        if (!assetPath.EndsWith(".fbx")) return;

        Debug.Log($"[AutoSetup] Processing imported FBX: {model.name}");





        // ✅ Collider
        if (model.GetComponent<Collider>() == null)
        {
            model.AddComponent<BoxCollider>();
            Debug.Log("Added BoxCollider");
        }
    }
}
