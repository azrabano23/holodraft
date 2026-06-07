using UnityEngine;

namespace HoloDraft.CAD
{
    /// <summary>
    /// CAD Annotation for adding text labels to models
    /// </summary>
    public class CADAnnotation : MonoBehaviour
    {
        [SerializeField] private string annotationText;
        [SerializeField] private Color annotationColor = Color.white;
        [SerializeField] private Vector3 positionOffset = Vector3.up;
        
        private TextMesh textMesh;

        private void Awake()
        {
            // Create and configure the TextMesh component
            textMesh = gameObject.AddComponent<TextMesh>();
            textMesh.text = annotationText;
            textMesh.color = annotationColor;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.1f;

            // Set initial position offset
            UpdatePosition();
        }

        public void SetAnnotation(Vector3 worldPosition, string text)
        {
            transform.position = worldPosition + positionOffset;
            annotationText = text;
            if (textMesh != null)
            {
                textMesh.text = annotationText;
            }
        }

        public void SetColor(Color color)
        {
            annotationColor = color;
            if (textMesh != null)
            {
                textMesh.color = annotationColor;
            }
        }

        private void UpdatePosition()
        {
            transform.position += positionOffset;
        }
    }
}

