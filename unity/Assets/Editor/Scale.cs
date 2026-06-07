using UnityEditor;

public class FBXImportScaler : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        ModelImporter modelImporter = (ModelImporter)assetImporter;
        modelImporter.globalScale = 0.01f; // 🔽 Scale down at import
    }
}