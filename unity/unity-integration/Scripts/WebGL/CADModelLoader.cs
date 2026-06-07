using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using HoloDraft.WebGL;

namespace HoloDraft.WebGL
{
    public class CADModelLoader : MonoBehaviour
    {
        [Header("Model Settings")]
        public Transform modelContainer;
        public Material defaultCADMaterial;
        public Material highlightMaterial;
        
        [Header("Loading Settings")]
        public float maxModelSize = 10f;
        public bool autoCenter = true;
        public bool autoScale = true;
        
        private Dictionary<string, GameObject> loadedModels = new Dictionary<string, GameObject>();
        private GameObject currentActiveModel;
        private string currentFileId;
        
        private void Start()
        {
            if (modelContainer == null)
            {
                modelContainer = transform;
            }
        }

        #region Public Methods

        /// <summary>
        /// Load a CAD model from a URL
        /// </summary>
        public IEnumerator LoadModelFromURL(FileLoadRequest fileInfo)
        {
            WebGLBridge.Instance.NotifyModelLoadProgress(fileInfo.fileId, 0f, "starting");
            
            yield return StartCoroutine(DownloadAndLoadModel(fileInfo));
        }

        /// <summary>
        /// Apply transformation to a loaded model
        /// </summary>
        public void ApplyTransformation(ModelTransformRequest transform)
        {
            if (loadedModels.TryGetValue(transform.fileId, out GameObject model))
            {
                model.transform.position = transform.position.ToVector3();
                model.transform.rotation = Quaternion.Euler(transform.rotation.ToVector3());
                model.transform.localScale = transform.scale.ToVector3();
                
                // Notify React of the transformation
                WebGLBridge.Instance.NotifyModelTransformed(
                    transform.fileId,
                    model.transform.position,
                    model.transform.rotation.eulerAngles,
                    model.transform.localScale
                );
            }
            else
            {
                Debug.LogWarning($"Model with ID {transform.fileId} not found");
            }
        }

        /// <summary>
        /// Update material properties of a model
        /// </summary>
        public void UpdateMaterial(MaterialUpdateRequest materialData)
        {
            if (loadedModels.TryGetValue(materialData.fileId, out GameObject model))
            {
                var renderers = model.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer.material.name.Contains(materialData.materialName) || 
                        materialData.materialName == "all")
                    {
                        // Create new material instance
                        Material newMaterial = new Material(defaultCADMaterial);
                        newMaterial.color = materialData.color.ToColor();
                        
                        // Set PBR properties if using Standard shader
                        if (newMaterial.HasProperty("_Metallic"))
                            newMaterial.SetFloat("_Metallic", materialData.metallic);
                        if (newMaterial.HasProperty("_Glossiness"))
                            newMaterial.SetFloat("_Glossiness", 1f - materialData.roughness);
                        
                        renderer.material = newMaterial;
                    }
                }
            }
        }

        /// <summary>
        /// Get model bounds for camera positioning
        /// </summary>
        public Bounds GetModelBounds(string fileId)
        {
            if (loadedModels.TryGetValue(fileId, out GameObject model))
            {
                return GetObjectBounds(model);
            }
            return new Bounds();
        }

        /// <summary>
        /// Set active model for manipulation
        /// </summary>
        public void SetActiveModel(string fileId)
        {
            // Unhighlight previous model
            if (currentActiveModel != null)
            {
                SetModelHighlight(currentActiveModel, false);
            }
            
            if (loadedModels.TryGetValue(fileId, out GameObject model))
            {
                currentActiveModel = model;
                currentFileId = fileId;
                SetModelHighlight(model, true);
            }
        }

        /// <summary>
        /// Remove a loaded model
        /// </summary>
        public void RemoveModel(string fileId)
        {
            if (loadedModels.TryGetValue(fileId, out GameObject model))
            {
                if (currentActiveModel == model)
                {
                    currentActiveModel = null;
                    currentFileId = null;
                }
                
                Destroy(model);
                loadedModels.Remove(fileId);
            }
        }

        #endregion

        #region Private Methods

        private IEnumerator DownloadAndLoadModel(FileLoadRequest fileInfo)
        {
            WebGLBridge.Instance.NotifyModelLoadProgress(fileInfo.fileId, 0.1f, "downloading");
            
            using (UnityWebRequest request = UnityWebRequest.Get(fileInfo.downloadUrl))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    WebGLBridge.Instance.NotifyModelLoadProgress(fileInfo.fileId, 0.5f, "processing");
                    
                    // Save the downloaded file temporarily
                    byte[] fileData = request.downloadHandler.data;
                    yield return StartCoroutine(ProcessModelData(fileInfo, fileData));
                }
                else
                {
                    Debug.LogError($"Failed to download model: {request.error}");
                    WebGLBridge.Instance.NotifyModelLoadProgress(fileInfo.fileId, 0f, "error");
                }
            }
        }

        private IEnumerator ProcessModelData(FileLoadRequest fileInfo, byte[] fileData)
        {
            try
            {
                WebGLBridge.Instance.NotifyModelLoadProgress(fileInfo.fileId, 0.7f, "loading");
                
                GameObject loadedModel = null;
                
                // Load based on file format
                switch (fileInfo.format.ToLower())
                {
                    case "fbx":
                        loadedModel = yield return StartCoroutine(LoadFBXModel(fileInfo, fileData));
                        break;
                    case "obj":
                        loadedModel = yield return StartCoroutine(LoadOBJModel(fileInfo, fileData));
                        break;
                    default:
                        Debug.LogWarning($"Unsupported format for Unity loading: {fileInfo.format}");
                        // For other formats, create a placeholder
                        loadedModel = CreatePlaceholderModel(fileInfo);
                        break;
                }
                
                if (loadedModel != null)
                {
                    // Configure the model
                    ConfigureLoadedModel(loadedModel, fileInfo);
                    
                    // Store reference
                    loadedModels[fileInfo.fileId] = loadedModel;
                    
                    WebGLBridge.Instance.NotifyModelLoadProgress(fileInfo.fileId, 1f, "complete");
                }
                else
                {
                    WebGLBridge.Instance.NotifyModelLoadProgress(fileInfo.fileId, 0f, "error");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing model data: {e.Message}");
                WebGLBridge.Instance.NotifyModelLoadProgress(fileInfo.fileId, 0f, "error");
            }
        }

        private IEnumerator LoadFBXModel(FileLoadRequest fileInfo, byte[] fileData)
        {
            // In a real implementation, you would use a runtime FBX loader
            // For now, create a placeholder that represents the FBX structure
            
            GameObject fbxModel = CreatePlaceholderModel(fileInfo);
            fbxModel.name = $"FBX_{fileInfo.fileName}";
            
            yield return new WaitForEndOfFrame();
            return fbxModel;
        }

        private IEnumerator LoadOBJModel(FileLoadRequest fileInfo, byte[] fileData)
        {
            // In a real implementation, you would use a runtime OBJ loader
            // For now, create a placeholder
            
            GameObject objModel = CreatePlaceholderModel(fileInfo);
            objModel.name = $"OBJ_{fileInfo.fileName}";
            
            yield return new WaitForEndOfFrame();
            return objModel;
        }

        private GameObject CreatePlaceholderModel(FileLoadRequest fileInfo)
        {
            GameObject placeholder = new GameObject($"CAD_{fileInfo.fileName}");
            placeholder.transform.SetParent(modelContainer);
            
            // Create a visual representation
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetParent(placeholder.transform);
            cube.transform.localPosition = Vector3.zero;
            
            // Apply default material
            var renderer = cube.GetComponent<Renderer>();
            if (renderer != null && defaultCADMaterial != null)
            {
                renderer.material = defaultCADMaterial;
            }
            
            // Add a text label
            CreateModelLabel(placeholder, fileInfo.fileName);
            
            return placeholder;
        }

        private void CreateModelLabel(GameObject model, string fileName)
        {
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(model.transform);
            labelObj.transform.localPosition = Vector3.up * 2f;
            
            // Add TextMesh component
            TextMesh textMesh = labelObj.AddComponent<TextMesh>();
            textMesh.text = fileName;
            textMesh.characterSize = 0.1f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
            
            // Make label face camera
            labelObj.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);
        }

        private void ConfigureLoadedModel(GameObject model, FileLoadRequest fileInfo)
        {
            // Set position in container
            model.transform.SetParent(modelContainer);
            model.transform.localPosition = Vector3.zero;
            
            // Auto-center and scale if enabled
            if (autoCenter || autoScale)
            {
                Bounds bounds = GetObjectBounds(model);
                
                if (autoCenter)
                {
                    model.transform.position -= bounds.center;
                }
                
                if (autoScale && bounds.size.magnitude > maxModelSize)
                {
                    float scaleFactor = maxModelSize / bounds.size.magnitude;
                    model.transform.localScale *= scaleFactor;
                }
            }
            
            // Add CADModel component
            var cadModel = model.AddComponent<CADModel>();
            cadModel.fileId = fileInfo.fileId;
            cadModel.fileName = fileInfo.fileName;
            cadModel.originalFormat = fileInfo.format;
            
            // Add interaction components
            if (!model.GetComponent<Collider>())
            {
                var collider = model.AddComponent<BoxCollider>();
                collider.isTrigger = false;
            }
            
            model.AddComponent<ModelInteractor>();
        }

        private Bounds GetObjectBounds(GameObject obj)
        {
            Bounds bounds = new Bounds();
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            
            if (renderers.Length > 0)
            {
                bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers)
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            
            return bounds;
        }

        private void SetModelHighlight(GameObject model, bool highlighted)
        {
            var renderers = model.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (highlighted && highlightMaterial != null)
                {
                    // Store original material if not already stored
                    var cadModel = model.GetComponent<CADModel>();
                    if (cadModel != null && cadModel.originalMaterials == null)
                    {
                        cadModel.originalMaterials = new Material[renderers.Length];
                        for (int i = 0; i < renderers.Length; i++)
                        {
                            cadModel.originalMaterials[i] = renderers[i].material;
                        }
                    }
                    
                    renderer.material = highlightMaterial;
                }
                else
                {
                    // Restore original material
                    var cadModel = model.GetComponent<CADModel>();
                    if (cadModel != null && cadModel.originalMaterials != null)
                    {
                        for (int i = 0; i < renderers.Length && i < cadModel.originalMaterials.Length; i++)
                        {
                            renderers[i].material = cadModel.originalMaterials[i];
                        }
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Component attached to loaded CAD models
    /// </summary>
    public class CADModel : MonoBehaviour
    {
        [Header("Model Info")]
        public string fileId;
        public string fileName;
        public string originalFormat;
        
        [Header("Runtime Data")]
        public Material[] originalMaterials;
        public bool isSelected;
        
        private void Start()
        {
            // Initialize any model-specific settings
        }
        
        public void OnSelect()
        {
            isSelected = true;
            // Handle model selection
        }
        
        public void OnDeselect()
        {
            isSelected = false;
            // Handle model deselection
        }
    }

    /// <summary>
    /// Handles model interaction (selection, manipulation)
    /// </summary>
    public class ModelInteractor : MonoBehaviour
    {
        private CADModel cadModel;
        private bool isDragging;
        private Vector3 lastMousePosition;
        
        private void Start()
        {
            cadModel = GetComponent<CADModel>();
        }
        
        private void OnMouseDown()
        {
            if (cadModel != null)
            {
                // Set as active model
                var loader = FindObjectOfType<CADModelLoader>();
                loader?.SetActiveModel(cadModel.fileId);
                
                isDragging = true;
                lastMousePosition = Input.mousePosition;
            }
        }
        
        private void OnMouseDrag()
        {
            if (isDragging && cadModel != null)
            {
                Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
                
                // Apply rotation based on mouse movement
                transform.Rotate(Vector3.up, mouseDelta.x * 0.5f, Space.World);
                transform.Rotate(Vector3.right, -mouseDelta.y * 0.5f, Space.World);
                
                lastMousePosition = Input.mousePosition;
                
                // Notify React of transformation
                WebGLBridge.Instance?.NotifyModelTransformed(
                    cadModel.fileId,
                    transform.position,
                    transform.rotation.eulerAngles,
                    transform.localScale
                );
            }
        }
        
        private void OnMouseUp()
        {
            isDragging = false;
        }
    }
}
