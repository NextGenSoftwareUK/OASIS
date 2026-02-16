using UnityEngine;

namespace OASIS.Omniverse.UnityHost.Rendering
{
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public class CinematicPortalGlow : MonoBehaviour
    {
        [SerializeField] private float threshold = 0.65f;
        [SerializeField] private float intensity = 0.8f;
        [SerializeField] private Color glowTint = Color.white;

        private Material _material;

        private void OnEnable()
        {
            var shader = Shader.Find("Hidden/OASIS/PortalGlow");
            if (shader == null)
            {
                Debug.LogWarning("Portal glow shader not found.");
                return;
            }

            _material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private void OnDisable()
        {
            if (_material != null)
            {
                DestroyImmediate(_material);
            }
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (_material == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            _material.SetFloat("_Threshold", Mathf.Clamp01(threshold));
            _material.SetFloat("_Intensity", Mathf.Max(0f, intensity));
            _material.SetColor("_GlowColor", glowTint);
            Graphics.Blit(source, destination, _material);
        }
    }
}




