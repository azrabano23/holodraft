using System;
using UnityEngine;

namespace HoloDraft.CAD
{
    /// <summary>
    /// Core CAD Model component that stores metadata and manages model properties
    /// </summary>
    public class CADModel : MonoBehaviour
    {
        [Header("Model Information")]
        [SerializeField] private string modelId;
        [SerializeField] private string fileName;
        [SerializeField] private string filePath;
        [SerializeField] private string fileType;
        [SerializeField] private DateTime creationDate;
        [SerializeField] private DateTime lastModified;
        
        [Header("Model Properties")]
        [SerializeField] private Vector3 originalScale;
        [SerializeField] private Vector3 originalPosition;
        [SerializeField] private Quaternion originalRotation;
        [SerializeField] private Bounds modelBounds;
        [SerializeField] private float volume;
        [SerializeField] private float surfaceArea;
        
        [Header("Visual Properties")]
        [SerializeField] private Color modelColor = Color.white;
        [SerializeField] private bool isSelected = false;
        [SerializeField] private bool isHighlighted = false;
        [SerializeField] private bool isWireframe = false;
        [SerializeField] private float transparency = 1.0f;
        
        // Properties
        public string ModelId => modelId;
        public string FileName => fileName;
        public string FilePath => filePath;
        public string FileType => fileType;
        public DateTime CreationDate => creationDate;
        public DateTime LastModified => lastModified;
        public Vector3 OriginalScale => originalScale;
        public Vector3 OriginalPosition => originalPosition;
        public Quaternion OriginalRotation => originalRotation;
        public Bounds ModelBounds => modelBounds;
        public float Volume => volume;
        public float SurfaceArea => surfaceArea;
        public Color ModelColor => modelColor;
        public bool IsSelected => isSelected;
        public bool IsHighlighted => isHighlighted;
        public bool IsWireframe => isWireframe;
        public float Transparency => transparency;
        
        // Events
        public Action<CADModel> OnModelTransformed;
        public Action<CADModel> OnModelSelected;
        public Action<CADModel> OnModelDeselected;
        public Action<CADModel> OnModelDeleted;
        
        private Vector3 lastPosition;
        private Quaternion lastRotation;
        private Vector3 lastScale;
        
        public void Initialize(string id, string path)
        {
            modelId = id;
            filePath = path;
            fileName = System.IO.Path.GetFileName(path);
            fileType = System.IO.Path.GetExtension(path).ToUpper();
            creationDate = DateTime.Now;
            lastModified = DateTime.Now;
            
            // Store original transform
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            originalScale = transform.localScale;
            
            // Calculate bounds
            CalculateBounds();
            
            // Calculate volume and surface area
            CalculateVolumeAndSurfaceArea();
            
            // Store initial transform for change detection
            lastPosition = transform.position;
            lastRotation = transform.rotation;
            lastScale = transform.localScale;
        }
        
        private void Update()
        {
            // Check for transform changes
            if (HasTransformChanged())
            {
                lastModified = DateTime.Now;
                OnModelTransformed?.Invoke(this);
                
                lastPosition = transform.position;
                lastRotation = transform.rotation;
                lastScale = transform.localScale;
            }
        }
        
        private bool HasTransformChanged()
        {
            return lastPosition != transform.position ||
                   lastRotation != transform.rotation ||
                   lastScale != transform.localScale;
        }
        
        public void SetSelected(bool selected)
        {
            isSelected = selected;
            if (selected)
            {
                OnModelSelected?.Invoke(this);
            }
            else
            {
                OnModelDeselected?.Invoke(this);
            }
        }
        
        public void SetHighlighted(bool highlighted)
        {
            isHighlighted = highlighted;
        }
        
        public void SetWireframe(bool wireframe)
        {
            isWireframe = wireframe;
        }
        
        public void SetTransparency(float alpha)
        {
            transparency = Mathf.Clamp01(alpha);
        }
        
        public void SetModelColor(Color color)
        {
            modelColor = color;
        }
        
        public void ResetToOriginalTransform()
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;
            transform.localScale = originalScale;
        }
        
        private void CalculateBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                modelBounds = renderers[0].bounds;
                foreach (var renderer in renderers)
                {
                    modelBounds.Encapsulate(renderer.bounds);
                }
            }
        }
        
        private void CalculateVolumeAndSurfaceArea()
        {
            // Simple approximation using bounding box
            volume = modelBounds.size.x * modelBounds.size.y * modelBounds.size.z;
            
            // Surface area approximation
            var size = modelBounds.size;
            surfaceArea = 2 * (size.x * size.y + size.x * size.z + size.y * size.z);
        }
        
        public void DeleteModel()
        {
            OnModelDeleted?.Invoke(this);
            Destroy(gameObject);
        }
        
        public Vector3 GetCenterOfMass()
        {
            return modelBounds.center;
        }
        
        public float GetLargestDimension()
        {
            return Mathf.Max(modelBounds.size.x, modelBounds.size.y, modelBounds.size.z);
        }
    }
}
