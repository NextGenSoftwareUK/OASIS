using UnityEngine;

namespace OASIS.Omniverse.UnityHost.Runtime
{
    public class PortalSurfaceAnimation : MonoBehaviour
    {
        public Color portalColor = Color.cyan;
        
        private Material _material;
        private float _time;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");
        private Color _baseColor;

        private void Start()
        {
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                _material = renderer.material;
                if (_material != null)
                {
                    _baseColor = _material.color;
                }
            }
        }

        private void Update()
        {
            if (_material == null) return;

            _time += Time.deltaTime;
            
            // Create wormhole effect - simple pulsing color
            float pulse = Mathf.Sin(_time * 1.5f) * 0.3f + 0.7f;
            float ripple = Mathf.Sin(_time * 2.5f + transform.position.x * 0.1f) * 0.2f + 0.8f;
            
            // Animate color brightness for shimmer effect
            var surfaceColor = portalColor * pulse;
            _material.SetColor(ColorProperty, surfaceColor);
            
            // Slow rotation for wormhole effect
            transform.Rotate(0f, 0f, Time.deltaTime * -10f, Space.Self);
            
            // Add subtle scale pulsing for depth effect
            float scalePulse = 1.0f + Mathf.Sin(_time * 1.2f) * 0.05f;
            transform.localScale = new Vector3(scalePulse, scalePulse, 1f);
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}

