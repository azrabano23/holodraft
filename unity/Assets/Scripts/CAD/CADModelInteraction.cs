using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;

namespace HoloDraft.CAD
{
    /// <summary>
    /// CAD Model Interaction for handling hand gestures and input
    /// </summary>
    public class CADModelInteraction : MonoBehaviour, 
        IMixedRealityPointerHandler, 
        IMixedRealityFocusHandler,
        IMixedRealityInputHandler<float>
    {
        [Header("Interaction Settings")]
        [SerializeField] private bool enableSelection = true;
        [SerializeField] private bool enableManipulation = true;
        [SerializeField] private bool enableMeasurement = true;
        [SerializeField] private float selectionHighlightIntensity = 1.5f;
        
        private CADModel cadModel;
        private CADModelManager modelManager;
        private bool isPointerDown = false;
        private bool isFocused = false;
        private Material originalMaterial;
        private Renderer modelRenderer;
        
        // Measurement points
        private Vector3 firstMeasurementPoint;
        private Vector3 secondMeasurementPoint;
        private bool isMeasuring = false;
        private int measurementClickCount = 0;
        
        private void Awake()
        {
            cadModel = GetComponent<CADModel>();
            modelManager = CADModelManager.Instance;
            modelRenderer = GetComponent<Renderer>();
            
            if (modelRenderer != null)
            {
                originalMaterial = modelRenderer.material;
            }
        }
        
        private void Start()
        {
            // Ensure the object has a collider for interaction
            if (GetComponent<Collider>() == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }
        }
        
        #region IMixedRealityPointerHandler
        
        public void OnPointerDown(MixedRealityPointerEventData eventData)
        {
            if (!enableSelection) return;
            
            isPointerDown = true;
            
            // Handle model selection
            if (cadModel != null && modelManager != null)
            {
                modelManager.SelectModel(cadModel.ModelId);
            }
            
            // Handle measurement mode
            if (isMeasuring)
            {
                HandleMeasurementClick(eventData.Pointer.Result.Details.Point);
            }
        }
        
        public void OnPointerUp(MixedRealityPointerEventData eventData)
        {
            isPointerDown = false;
        }
        
        public void OnPointerClicked(MixedRealityPointerEventData eventData)
        {
            // Handle click events
            Debug.Log($"CAD Model clicked: {cadModel?.ModelId}");
        }
        
        public void OnPointerDragged(MixedRealityPointerEventData eventData)
        {
            if (!enableManipulation) return;
            
            // Handle dragging for manipulation
            // This is handled by the ObjectManipulator component
        }
        
        #endregion
        
        #region IMixedRealityFocusHandler
        
        public void OnFocusEnter(FocusEventData eventData)
        {
            isFocused = true;
            
            // Highlight the model when focused
            if (cadModel != null)
            {
                cadModel.SetHighlighted(true);
                ApplyHighlightMaterial();
            }
        }
        
        public void OnFocusExit(FocusEventData eventData)
        {
            isFocused = false;
            
            // Remove highlight when focus is lost
            if (cadModel != null)
            {
                cadModel.SetHighlighted(false);
                RemoveHighlightMaterial();
            }
        }
        
        #endregion
        
        #region IMixedRealityInputHandler<float>
        
        public void OnInputUp(InputEventData<float> eventData)
        {
            // Handle input up events (e.g., trigger release)
        }
        
        public void OnInputDown(InputEventData<float> eventData)
        {
            // Handle input down events (e.g., trigger press)
        }
        
        public void OnInputChanged(InputEventData<float> eventData)
        {
            // Handle input changes (e.g., trigger pressure)
        }
        
        #endregion
        
        #region Measurement System
        
        public void EnableMeasurementMode()
        {
            isMeasuring = true;
            measurementClickCount = 0;
            Debug.Log("Measurement mode enabled. Click two points to measure distance.");
        }
        
        public void DisableMeasurementMode()
        {
            isMeasuring = false;
            measurementClickCount = 0;
            Debug.Log("Measurement mode disabled.");
        }
        
        private void HandleMeasurementClick(Vector3 clickPoint)
        {
            if (measurementClickCount == 0)
            {
                firstMeasurementPoint = clickPoint;
                measurementClickCount = 1;
                Debug.Log($"First measurement point set: {firstMeasurementPoint}");
            }
            else if (measurementClickCount == 1)
            {
                secondMeasurementPoint = clickPoint;
                measurementClickCount = 0;
                
                // Create measurement
                if (modelManager != null)
                {
                    modelManager.AddMeasurement(firstMeasurementPoint, secondMeasurementPoint);
                }
                
                Debug.Log($"Measurement created between {firstMeasurementPoint} and {secondMeasurementPoint}");
            }
        }
        
        #endregion
        
        #region Visual Effects
        
        private void ApplyHighlightMaterial()
        {
            if (modelRenderer != null && modelManager != null)
            {
                // Apply highlight material if available
                if (modelManager.GetComponent<CADMaterialManager>() != null)
                {
                    var materialManager = modelManager.GetComponent<CADMaterialManager>();
                    modelRenderer.material = materialManager.GetHighlightMaterial();
                }
                else
                {
                    // Fallback: brighten the existing material
                    Material highlightMaterial = new Material(originalMaterial);
                    highlightMaterial.color = originalMaterial.color * selectionHighlightIntensity;
                    modelRenderer.material = highlightMaterial;
                }
            }
        }
        
        private void RemoveHighlightMaterial()
        {
            if (modelRenderer != null && originalMaterial != null)
            {
                // Only remove highlight if not selected
                if (cadModel != null && !cadModel.IsSelected)
                {
                    modelRenderer.material = originalMaterial;
                }
            }
        }
        
        #endregion
        
        #region Public Methods
        
        public void SetSelectionEnabled(bool enabled)
        {
            enableSelection = enabled;
        }
        
        public void SetManipulationEnabled(bool enabled)
        {
            enableManipulation = enabled;
        }
        
        public void SetMeasurementEnabled(bool enabled)
        {
            enableMeasurement = enabled;
        }
        
        public bool IsFocused()
        {
            return isFocused;
        }
        
        public bool IsPointerDown()
        {
            return isPointerDown;
        }
        
        #endregion
    }
}
