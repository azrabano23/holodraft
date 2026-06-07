using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using Newtonsoft.Json;

namespace HoloDraft.WebGL
{
    public class WebGLBridge : MonoBehaviour
    {
        [Header("WebGL Communication")]
        public CADModelLoader modelLoader;
        public RealtimeSync realtimeSync;
        
        private static WebGLBridge instance;
        public static WebGLBridge Instance => instance;

        // JavaScript interface functions
        [DllImport("__Internal")]
        private static extern void SendMessageToReact(string methodName, string data);

        [DllImport("__Internal")]
        private static extern void RegisterUnityCallbacks();

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
                RegisterUnityCallbacks();
                NotifyReactUnityReady();
            #endif
        }

        #region React Communication

        /// <summary>
        /// Notify React that Unity is ready to receive commands
        /// </summary>
        private void NotifyReactUnityReady()
        {
            var data = JsonConvert.SerializeObject(new
            {
                status = "ready",
                timestamp = DateTime.UtcNow.ToString("O"),
                features = new string[] { "modelLoading", "realtimeSync", "editing" }
            });
            
            SendMessageToReact("onUnityReady", data);
        }

        /// <summary>
        /// Send model loading progress to React
        /// </summary>
        public void NotifyModelLoadProgress(string fileId, float progress, string status)
        {
            var data = JsonConvert.SerializeObject(new
            {
                fileId = fileId,
                progress = progress,
                status = status,
                timestamp = DateTime.UtcNow.ToString("O")
            });
            
            SendMessageToReact("onModelLoadProgress", data);
        }

        /// <summary>
        /// Send model transformation updates to React
        /// </summary>
        public void NotifyModelTransformed(string fileId, Vector3 position, Vector3 rotation, Vector3 scale)
        {
            var data = JsonConvert.SerializeObject(new
            {
                fileId = fileId,
                transform = new
                {
                    position = new { x = position.x, y = position.y, z = position.z },
                    rotation = new { x = rotation.x, y = rotation.y, z = rotation.z },
                    scale = new { x = scale.x, y = scale.y, z = scale.z }
                },
                timestamp = DateTime.UtcNow.ToString("O")
            });
            
            SendMessageToReact("onModelTransformed", data);
        }

        /// <summary>
        /// Send AR session status to React
        /// </summary>
        public void NotifyARSessionStatus(bool isActive, string sessionId = null)
        {
            var data = JsonConvert.SerializeObject(new
            {
                isActive = isActive,
                sessionId = sessionId,
                timestamp = DateTime.UtcNow.ToString("O")
            });
            
            SendMessageToReact("onARSessionStatus", data);
        }

        #endregion

        #region Unity Callable Methods (From React)

        /// <summary>
        /// Called from React to load a CAD model
        /// </summary>
        /// <param name="data">JSON string with file information</param>
        public void LoadCADModel(string data)
        {
            try
            {
                var fileInfo = JsonConvert.DeserializeObject<FileLoadRequest>(data);
                StartCoroutine(modelLoader.LoadModelFromURL(fileInfo));
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing load model request: {e.Message}");
                NotifyModelLoadProgress("unknown", 0f, "error");
            }
        }

        /// <summary>
        /// Called from React to transform a model
        /// </summary>
        /// <param name="data">JSON string with transformation data</param>
        public void TransformModel(string data)
        {
            try
            {
                var transform = JsonConvert.DeserializeObject<ModelTransformRequest>(data);
                modelLoader.ApplyTransformation(transform);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing transform request: {e.Message}");
            }
        }

        /// <summary>
        /// Called from React to start AR session
        /// </summary>
        /// <param name="data">JSON string with session configuration</param>
        public void StartARSession(string data)
        {
            try
            {
                var config = JsonConvert.DeserializeObject<ARSessionConfig>(data);
                StartCoroutine(InitiateARSession(config));
            }
            catch (Exception e)
            {
                Debug.LogError($"Error starting AR session: {e.Message}");
                NotifyARSessionStatus(false);
            }
        }

        /// <summary>
        /// Called from React to stop AR session
        /// </summary>
        public void StopARSession()
        {
            // Stop AR session logic here
            NotifyARSessionStatus(false);
        }

        /// <summary>
        /// Called from React to update model materials
        /// </summary>
        /// <param name="data">JSON string with material data</param>
        public void UpdateModelMaterial(string data)
        {
            try
            {
                var materialData = JsonConvert.DeserializeObject<MaterialUpdateRequest>(data);
                modelLoader.UpdateMaterial(materialData);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error updating material: {e.Message}");
            }
        }

        /// <summary>
        /// Called from React to export modified model
        /// </summary>
        /// <param name="data">JSON string with export configuration</param>
        public void ExportModel(string data)
        {
            try
            {
                var exportConfig = JsonConvert.DeserializeObject<ExportConfig>(data);
                StartCoroutine(ExportModelToFBX(exportConfig));
            }
            catch (Exception e)
            {
                Debug.LogError($"Error exporting model: {e.Message}");
            }
        }

        #endregion

        #region Private Methods

        private IEnumerator InitiateARSession(ARSessionConfig config)
        {
            // Generate session ID
            string sessionId = Guid.NewGuid().ToString();
            
            // Initialize AR components (this would be platform-specific)
            yield return new WaitForSeconds(1f); // Simulate initialization
            
            // Start real-time sync
            realtimeSync.StartSession(sessionId, config.fileId);
            
            NotifyARSessionStatus(true, sessionId);
        }

        private IEnumerator ExportModelToFBX(ExportConfig config)
        {
            // Export logic would go here
            yield return new WaitForSeconds(2f); // Simulate export process
            
            var data = JsonConvert.SerializeObject(new
            {
                fileId = config.fileId,
                exportUrl = $"/api/exports/{config.fileId}.fbx",
                success = true,
                timestamp = DateTime.UtcNow.ToString("O")
            });
            
            SendMessageToReact("onModelExported", data);
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class FileLoadRequest
    {
        public string fileId;
        public string fileName;
        public string downloadUrl;
        public string format;
        public long fileSize;
    }

    [Serializable]
    public class ModelTransformRequest
    {
        public string fileId;
        public Vector3Data position;
        public Vector3Data rotation;
        public Vector3Data scale;
    }

    [Serializable]
    public class Vector3Data
    {
        public float x, y, z;
        
        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }

    [Serializable]
    public class ARSessionConfig
    {
        public string fileId;
        public bool enableHandTracking;
        public bool enableCollaboration;
        public string collaborationRoomId;
    }

    [Serializable]
    public class MaterialUpdateRequest
    {
        public string fileId;
        public string materialName;
        public ColorData color;
        public float metallic;
        public float roughness;
    }

    [Serializable]
    public class ColorData
    {
        public float r, g, b, a;
        
        public Color ToColor()
        {
            return new Color(r, g, b, a);
        }
    }

    [Serializable]
    public class ExportConfig
    {
        public string fileId;
        public string format;
        public bool includeAnimations;
        public bool includeMaterials;
    }

    #endregion
}
