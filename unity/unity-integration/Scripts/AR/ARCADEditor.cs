using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using HoloDraft.Shared;

namespace HoloDraft.AR
{
    public class ARCADEditor : MonoBehaviour
    {
        [Header("AR Configuration")]
        public Camera arCamera;
        public Transform modelAnchor;
        public LayerMask interactionLayers = -1;
        
        [Header("Hand Tracking")]
        public HandTrackingController leftHand;
        public HandTrackingController rightHand;
        
        [Header("UI Components")]
        public GameObject handUI;
        public Transform uiAnchor;
        
        [Header("Model Settings")]
        public Material arDefaultMaterial;
        public Material selectionMaterial;
        public float modelScaleFactor = 0.01f; // Scale CAD models for AR
        
        private SupabaseClient supabaseClient;
        private NetworkManager networkManager;
        private CollaborativeEditor collaborativeEditor;
        
        private Dictionary<string, GameObject> arModels = new Dictionary<string, GameObject>();
        private string currentSessionId;
        private bool isARActive = false;
        
        // Events
        public event Action<string> OnModelLoaded;
        public event Action<string, Vector3, Quaternion, Vector3> OnModelTransformed;
        public event Action<bool> OnARStateChanged;

        private void Start()
        {
            InitializeARComponents();
            SetupXRConfiguration();
        }

        private void Update()
        {
            if (isARActive)
            {
                UpdateHandTracking();
                UpdateModelInteractions();
                UpdateUI();
            }
        }

        #region Initialization

        private void InitializeARComponents()
        {
            // Get or create required components
            supabaseClient = FindObjectOfType<SupabaseClient>();
            if (supabaseClient == null)
            {
                supabaseClient = gameObject.AddComponent<SupabaseClient>();
            }
            
            networkManager = FindObjectOfType<NetworkManager>();
            if (networkManager == null)
            {
                networkManager = gameObject.AddComponent<NetworkManager>();
            }
            
            collaborativeEditor = FindObjectOfType<CollaborativeEditor>();
            if (collaborativeEditor == null)
            {
                collaborativeEditor = gameObject.AddComponent<CollaborativeEditor>();
            }
            
            // Set up AR camera if not assigned
            if (arCamera == null)
            {
                arCamera = Camera.main;
            }
            
            // Create model anchor if not assigned
            if (modelAnchor == null)
            {
                GameObject anchorObj = new GameObject("ModelAnchor");
                modelAnchor = anchorObj.transform;
                modelAnchor.position = arCamera.transform.position + arCamera.transform.forward * 2f;
            }
        }

        private void SetupXRConfiguration()
        {
            // Configure XR settings for Quest
            XRSettings.enabled = true;
            
            // Enable hand tracking if available
            EnableHandTracking();
        }

        private void EnableHandTracking()
        {
            // Enable hand tracking through Oculus SDK
            OVRManager ovrManager = FindObjectOfType<OVRManager>();
            if (ovrManager != null)
            {
                ovrManager.handTrackingSupport = OVRManager.HandTrackingSupport.ControllersAndHands;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Start AR session with specified model
        /// </summary>
        public void StartARSession(string fileId, string fileName, string modelUrl)
        {
            if (isARActive)
            {
                StopARSession();
            }
            
            currentSessionId = Guid.NewGuid().ToString();
            isARActive = true;
            
            StartCoroutine(LoadModelForAR(fileId, fileName, modelUrl));
            
            // Initialize collaborative editing
            collaborativeEditor.StartSession(currentSessionId, fileId);
            
            // Show hand UI
            if (handUI != null)
            {
                handUI.SetActive(true);
            }
            
            OnARStateChanged?.Invoke(true);
            
            Debug.Log($"Started AR session: {currentSessionId} for file: {fileId}");
        }

        /// <summary>
        /// Stop current AR session
        /// </summary>
        public void StopARSession()
        {
            if (!isARActive) return;
            
            isARActive = false;
            
            // Clear loaded models
            ClearAllModels();
            
            // Stop collaborative editing
            collaborativeEditor.StopSession();
            
            // Hide hand UI
            if (handUI != null)
            {
                handUI.SetActive(false);
            }
            
            OnARStateChanged?.Invoke(false);
            
            Debug.Log($"Stopped AR session: {currentSessionId}");
            currentSessionId = null;
        }

        /// <summary>
        /// Transform model in AR space
        /// </summary>
        public void TransformModel(string fileId, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (arModels.TryGetValue(fileId, out GameObject model))
            {
                model.transform.position = position;
                model.transform.rotation = rotation;
                model.transform.localScale = scale;
                
                OnModelTransformed?.Invoke(fileId, position, rotation, scale);
                
                // Sync with collaborative editor
                collaborativeEditor.BroadcastTransformation(fileId, position, rotation, scale);
            }
        }

        /// <summary>
        /// Place model at hand position
        /// </summary>
        public void PlaceModelAtHand(string fileId, bool useLeftHand = true)
        {
            var hand = useLeftHand ? leftHand : rightHand;
            if (hand != null && arModels.TryGetValue(fileId, out GameObject model))
            {
                Vector3 handPosition = hand.GetPalmPosition();
                model.transform.position = handPosition;
                
                OnModelTransformed?.Invoke(fileId, handPosition, model.transform.rotation, model.transform.localScale);
            }
        }

        #endregion

        #region Model Management

        private IEnumerator LoadModelForAR(string fileId, string fileName, string modelUrl)
        {
            Debug.Log($"Loading model for AR: {fileName}");
            
            // Download model data
            using (var request = UnityEngine.Networking.UnityWebRequest.Get(modelUrl))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    // Create AR model representation
                    GameObject arModel = CreateARModel(fileId, fileName, request.downloadHandler.data);
                    
                    if (arModel != null)
                    {
                        arModels[fileId] = arModel;
                        OnModelLoaded?.Invoke(fileId);
                        
                        Debug.Log($"Successfully loaded AR model: {fileName}");
                    }
                }
                else
                {
                    Debug.LogError($"Failed to load model: {request.error}");
                }
            }
        }

        private GameObject CreateARModel(string fileId, string fileName, byte[] modelData)
        {
            // Create base model object
            GameObject arModel = new GameObject($"AR_{fileName}");
            arModel.transform.SetParent(modelAnchor);
            arModel.transform.localPosition = Vector3.zero;
            arModel.transform.localScale = Vector3.one * modelScaleFactor;
            
            // Add AR CAD Model component
            var arCADModel = arModel.AddComponent<ARCADModel>();
            arCADModel.fileId = fileId;
            arCADModel.fileName = fileName;
            arCADModel.editor = this;
            
            // Create visual representation (placeholder for now)
            CreateModelVisuals(arModel, modelData);
            
            // Add interaction components
            SetupModelInteractions(arModel);
            
            return arModel;
        }

        private void CreateModelVisuals(GameObject arModel, byte[] modelData)
        {
            // For now, create a placeholder cube
            // In production, you'd parse the actual model data
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(arModel.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one;
            
            // Apply AR material
            var renderer = visual.GetComponent<Renderer>();
            if (renderer != null && arDefaultMaterial != null)
            {
                renderer.material = arDefaultMaterial;
            }
            
            // Make it semi-transparent for AR
            if (renderer.material.HasProperty("_Color"))
            {
                Color color = renderer.material.color;
                color.a = 0.8f;
                renderer.material.color = color;
            }
        }

        private void SetupModelInteractions(GameObject arModel)
        {
            // Add collider for hand interactions
            var collider = arModel.GetComponentInChildren<Collider>();
            if (collider == null)
            {
                collider = arModel.AddComponent<BoxCollider>();
            }
            
            // Add AR interaction component
            arModel.AddComponent<ARModelInteractor>();
            
            // Set interaction layer
            arModel.layer = Mathf.RoundToInt(Mathf.Log(interactionLayers.value, 2));
        }

        private void ClearAllModels()
        {
            foreach (var model in arModels.Values)
            {
                if (model != null)
                {
                    Destroy(model);
                }
            }
            arModels.Clear();
        }

        #endregion

        #region Update Methods

        private void UpdateHandTracking()
        {
            if (leftHand != null)
            {
                leftHand.UpdateHandTracking();
            }
            
            if (rightHand != null)
            {
                rightHand.UpdateHandTracking();
            }
        }

        private void UpdateModelInteractions()
        {
            // Handle hand-model interactions
            foreach (var kvp in arModels)
            {
                var model = kvp.Value;
                var arCADModel = model.GetComponent<ARCADModel>();
                
                if (arCADModel != null && arCADModel.isBeingManipulated)
                {
                    // Update model position based on hand tracking
                    UpdateModelFromHands(model);
                }
            }
        }

        private void UpdateModelFromHands(GameObject model)
        {
            // Get hand positions
            Vector3 leftHandPos = leftHand?.GetPalmPosition() ?? Vector3.zero;
            Vector3 rightHandPos = rightHand?.GetPalmPosition() ?? Vector3.zero;
            
            // Calculate midpoint for positioning
            Vector3 midpoint = (leftHandPos + rightHandPos) * 0.5f;
            
            // Update model position
            model.transform.position = midpoint;
            
            // Calculate rotation based on hand orientation
            Vector3 handDirection = (rightHandPos - leftHandPos).normalized;
            if (handDirection != Vector3.zero)
            {
                model.transform.rotation = Quaternion.LookRotation(handDirection, Vector3.up);
            }
            
            // Scale based on hand distance
            float handDistance = Vector3.Distance(leftHandPos, rightHandPos);
            float scaleFactor = Mathf.Clamp(handDistance * 2f, 0.1f, 5f);
            model.transform.localScale = Vector3.one * modelScaleFactor * scaleFactor;
        }

        private void UpdateUI()
        {
            // Update hand UI position
            if (handUI != null && rightHand != null)
            {
                Vector3 handPos = rightHand.GetPalmPosition();
                Vector3 handForward = rightHand.GetPalmForward();
                
                handUI.transform.position = handPos + handForward * 0.1f;
                handUI.transform.LookAt(arCamera.transform);
            }
        }

        #endregion

        #region Voice Commands

        [ContextMenu("Place Model Here")]
        public void PlaceModelAtCenter()
        {
            Vector3 centerPosition = arCamera.transform.position + arCamera.transform.forward * 1.5f;
            
            foreach (var model in arModels.Values)
            {
                model.transform.position = centerPosition;
            }
        }

        [ContextMenu("Reset Model Scale")]
        public void ResetModelScale()
        {
            foreach (var model in arModels.Values)
            {
                model.transform.localScale = Vector3.one * modelScaleFactor;
            }
        }

        [ContextMenu("Toggle Wireframe")]
        public void ToggleWireframe()
        {
            // Toggle wireframe rendering for all models
            foreach (var model in arModels.Values)
            {
                var renderers = model.GetComponentsInChildren<Renderer>();
                foreach (var renderer in renderers)
                {
                    // Toggle between wireframe and solid
                    // This would require a wireframe shader
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Component for AR CAD models
    /// </summary>
    public class ARCADModel : MonoBehaviour
    {
        [Header("Model Data")]
        public string fileId;
        public string fileName;
        public ARCADEditor editor;
        
        [Header("Interaction State")]
        public bool isSelected;
        public bool isBeingManipulated;
        public bool isLocked;
        
        private Material originalMaterial;
        private Renderer modelRenderer;
        
        private void Start()
        {
            modelRenderer = GetComponentInChildren<Renderer>();
            if (modelRenderer != null)
            {
                originalMaterial = modelRenderer.material;
            }
        }
        
        public void SelectModel()
        {
            if (isLocked) return;
            
            isSelected = true;
            
            if (modelRenderer != null && editor.selectionMaterial != null)
            {
                modelRenderer.material = editor.selectionMaterial;
            }
        }
        
        public void DeselectModel()
        {
            isSelected = false;
            
            if (modelRenderer != null && originalMaterial != null)
            {
                modelRenderer.material = originalMaterial;
            }
        }
        
        public void StartManipulation()
        {
            if (isLocked) return;
            
            isBeingManipulated = true;
            SelectModel();
        }
        
        public void StopManipulation()
        {
            isBeingManipulated = false;
            DeselectModel();
            
            // Notify editor of final transformation
            editor.OnModelTransformed?.Invoke(
                fileId,
                transform.position,
                transform.rotation,
                transform.localScale
            );
        }
    }

    /// <summary>
    /// Handles AR-specific model interactions
    /// </summary>
    public class ARModelInteractor : MonoBehaviour
    {
        private ARCADModel arModel;
        private bool isNearHand;
        
        private void Start()
        {
            arModel = GetComponent<ARCADModel>();
        }
        
        private void Update()
        {
            CheckHandProximity();
        }
        
        private void CheckHandProximity()
        {
            // Check if hands are near this model
            var editor = arModel.editor;
            if (editor == null) return;
            
            float minDistance = float.MaxValue;
            
            if (editor.leftHand != null)
            {
                float leftDistance = Vector3.Distance(transform.position, editor.leftHand.GetPalmPosition());
                minDistance = Mathf.Min(minDistance, leftDistance);
            }
            
            if (editor.rightHand != null)
            {
                float rightDistance = Vector3.Distance(transform.position, editor.rightHand.GetPalmPosition());
                minDistance = Mathf.Min(minDistance, rightDistance);
            }
            
            bool wasNearHand = isNearHand;
            isNearHand = minDistance < 0.2f; // 20cm threshold
            
            if (isNearHand && !wasNearHand)
            {
                OnHandEnter();
            }
            else if (!isNearHand && wasNearHand)
            {
                OnHandExit();
            }
        }
        
        private void OnHandEnter()
        {
            arModel?.SelectModel();
        }
        
        private void OnHandExit()
        {
            if (!arModel.isBeingManipulated)
            {
                arModel?.DeselectModel();
            }
        }
        
        // Called by hand tracking when pinch gesture is detected
        public void OnPinchStart()
        {
            arModel?.StartManipulation();
        }
        
        public void OnPinchEnd()
        {
            arModel?.StopManipulation();
        }
    }
}
