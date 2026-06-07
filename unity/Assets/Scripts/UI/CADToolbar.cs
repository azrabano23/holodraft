using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace HoloDraft.CAD
{
    /// <summary>
    /// CAD Toolbar UI for user interactions
    /// </summary>
    public class CADToolbar : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Button selectModelButton;
        [SerializeField] private Button toggleWireframeButton;
        [SerializeField] private Button expandViewButton;
        [SerializeField] private Button measureDistanceButton;
        [SerializeField] private Button measureAngleButton;
        [SerializeField] private Button resetButton;
        
        private CADModelManager modelManager;
        private CADMeasurementTools measurementTools;

        private void Awake()
        {
            modelManager = FindObjectOfType<CADModelManager>();
            measurementTools = FindObjectOfType<CADMeasurementTools>();

            // Assign button click events
            selectModelButton.onClick.AddListener(SelectModel);
            toggleWireframeButton.onClick.AddListener(ToggleWireframe);
            expandViewButton.onClick.AddListener(ToggleExplodedView);
            measureDistanceButton.onClick.AddListener(EnableDistanceMeasurement);
            measureAngleButton.onClick.AddListener(EnableAngleMeasurement);
            resetButton.onClick.AddListener(ResetViews);
        }

        private void SelectModel()
        {
            Debug.Log("Select Model button clicked");
            // Handle select model logic
        }

        private void ToggleWireframe()
        {
            Debug.Log("Toggle Wireframe button clicked");
            if (modelManager != null)
            {
                modelManager.ToggleWireframe();
            }
        }

        private void ToggleExplodedView()
        {
            Debug.Log("Toggle Exploded View button clicked");
            if (modelManager != null)
            {
                modelManager.ToggleExplodedView();
            }
        }

        private void EnableDistanceMeasurement()
        {
            Debug.Log("Measure Distance button clicked");
            if (measurementTools != null)
            {
                measurementTools.StartDistanceMeasurement();
            }
        }

        private void EnableAngleMeasurement()
        {
            Debug.Log("Measure Angle button clicked");
            if (measurementTools != null)
            {
                measurementTools.StartAngleMeasurement();
            }
        }

        private void ResetViews()
        {
            Debug.Log("Reset button clicked");
            if (modelManager != null)
            {
                modelManager.ClearMeasurementsAndAnnotations();
            }
            if (measurementTools != null)
            {
                measurementTools.ClearAllMeasurements();
            }
        }
    }
}
