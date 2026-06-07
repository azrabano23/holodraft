using UnityEngine;

namespace HoloDraft.CAD
{
    /// <summary>
    /// Material manager for CAD models
    /// </summary>
    public class CADMaterialManager : MonoBehaviour
    {
        [Header("Material Settings")]
        [SerializeField] private Material defaultCADMaterial;
        [SerializeField] private Material highlightMaterial;
        [SerializeField] private Material selectedMaterial;
        [SerializeField] private Material wireframeMaterial;
        [SerializeField] private Material transparentMaterial;

        public Material GetDefaultMaterial()
        {
            return defaultCADMaterial;
        }

        public Material GetHighlightMaterial()
        {
            return highlightMaterial;
        }

        public Material GetSelectedMaterial()
        {
            return selectedMaterial;
        }

        public Material GetWireframeMaterial()
        {
            return wireframeMaterial;
        }

        public Material GetTransparentMaterial(float transparency)
        {
            if (transparentMaterial != null)
            {
                Color color = transparentMaterial.color;
                color.a = transparency;
                transparentMaterial.color = color;
            }
            return transparentMaterial;
        }

        public void ApplyMaterial(GameObject obj, Material material)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = material;
            }
        }

        public void ApplyWireframe(GameObject obj, bool enabled)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = enabled ? wireframeMaterial : defaultCADMaterial;
            }
        }
    }
}

