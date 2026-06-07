using UnityEngine;
using UnityEditor;

public class AutoSetupOnImport : AssetPostprocessor
{
    void OnPostprocessModel(GameObject model)
    {
        if (!assetPath.EndsWith(".fbx")) return;

        Debug.Log($"[AutoSetup] Processing imported FBX: {model.name}");

        // ✅ DragToMove
        DragToMove moveScript = model.GetComponent<DragToMove>();
        if (moveScript == null)
        {
            moveScript = model.AddComponent<DragToMove>();
            Debug.Log("Added DragToMove");
        }
        moveScript.targetObject = model.transform;

        // ✅ DragToScale
        DragToScale scaleScript = model.GetComponent<DragToScale>();
        if (scaleScript == null)
        {
            scaleScript = model.AddComponent<DragToScale>();
            Debug.Log("Added DragToScale");
        }
        scaleScript.targetObject = model.transform;

        // ✅ Collider
        if (model.GetComponent<Collider>() == null)
        {
            model.AddComponent<BoxCollider>();
            Debug.Log("Added BoxCollider");
        }
    }
}
