using UnityEngine;

namespace OASIS.Omniverse.UnityHost.Runtime
{
    public static class PortalQualityManager
    {
        public enum QualityLevel
        {
            Low,
            Medium,
            High,
            Ultra
        }

        public static void ApplyQualityToPortal(GameObject portalRoot, Color portalColor, QualityLevel quality)
        {
            if (portalRoot == null) return;

            // Find portal components
            var ring = portalRoot.transform.Find("PortalRing");
            var portalSurface = portalRoot.transform.Find("PortalSurface");
            var particles = portalRoot.transform.Find("PortalParticles");

            // Apply quality to ring
            if (ring != null)
            {
                ApplyRingQuality(ring.gameObject, portalColor, quality);
            }

            // Apply quality to portal surface
            if (portalSurface != null)
            {
                ApplySurfaceQuality(portalSurface.gameObject, portalColor, quality);
            }

            // Apply quality to particles
            if (particles != null)
            {
                ApplyParticleQuality(particles.gameObject, portalColor, quality);
            }
        }

        private static void ApplyRingQuality(GameObject ring, Color portalColor, QualityLevel quality)
        {
            var renderer = ring.GetComponent<Renderer>();
            if (renderer == null) return;

            Material material;
            switch (quality)
            {
                case QualityLevel.Low:
                    // Simple unlit shader for software rendering
                    material = new Material(Shader.Find("Unlit/Color"));
                    material.color = portalColor;
                    break;

                case QualityLevel.Medium:
                    // Standard shader with emission
                    material = new Material(Shader.Find("Standard"));
                    material.EnableKeyword("_EMISSION");
                    material.SetFloat("_Metallic", 0f);
                    material.SetFloat("_Glossiness", 0.7f);
                    material.color = portalColor * 0.4f;
                    material.SetColor("_EmissionColor", portalColor * 3.0f);
                    break;

                case QualityLevel.High:
                    // Standard shader with strong emission and glow
                    material = new Material(Shader.Find("Standard"));
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    material.SetFloat("_Metallic", 0f);
                    material.SetFloat("_Glossiness", 0.9f);
                    material.color = portalColor * 0.3f;
                    material.SetColor("_EmissionColor", portalColor * 5.0f);
                    break;
                
                case QualityLevel.Ultra:
                    // Same as High for now
                    material = new Material(Shader.Find("Standard"));
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    material.SetFloat("_Metallic", 0f);
                    material.SetFloat("_Glossiness", 0.9f);
                    material.color = portalColor * 0.3f;
                    material.SetColor("_EmissionColor", portalColor * 5.0f);
                    break;

                default:
                    material = new Material(Shader.Find("Unlit/Color"));
                    material.color = portalColor;
                    break;
            }

            renderer.material = material;
        }

        private static void ApplySurfaceQuality(GameObject surface, Color portalColor, QualityLevel quality)
        {
            var renderer = surface.GetComponent<Renderer>();
            if (renderer == null) return;

            Material material;
            switch (quality)
            {
                case QualityLevel.Low:
                    // Simple unlit shader
                    material = new Material(Shader.Find("Unlit/Color"));
                    material.color = portalColor;
                    break;

                case QualityLevel.Medium:
                    // Standard shader with transparency
                    material = new Material(Shader.Find("Standard"));
                    material.SetFloat("_Mode", 3); // Fade mode
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.renderQueue = 3000;
                    material.SetInt("_Cull", 0);
                    material.EnableKeyword("_EMISSION");
                    material.SetFloat("_Metallic", 0f);
                    material.SetFloat("_Glossiness", 0.9f);
                    var colorMed = portalColor;
                    colorMed.a = 0.6f;
                    material.color = colorMed;
                    material.SetColor("_EmissionColor", portalColor * 3.0f);
                    break;

                case QualityLevel.High:
                    // Standard shader with transparency and strong emission
                    material = new Material(Shader.Find("Standard"));
                    material.SetFloat("_Mode", 3);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.renderQueue = 3000;
                    material.SetInt("_Cull", 0);
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    material.SetFloat("_Metallic", 0f);
                    material.SetFloat("_Glossiness", 0.95f);
                    var colorHigh = portalColor;
                    colorHigh.a = 0.5f;
                    material.color = colorHigh;
                    material.SetColor("_EmissionColor", portalColor * 5.0f);
                    break;
                
                case QualityLevel.Ultra:
                    // Same as High
                    material = new Material(Shader.Find("Standard"));
                    material.SetFloat("_Mode", 3);
                    material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.EnableKeyword("_ALPHABLEND_ON");
                    material.renderQueue = 3000;
                    material.SetInt("_Cull", 0);
                    material.EnableKeyword("_EMISSION");
                    material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    material.SetFloat("_Metallic", 0f);
                    material.SetFloat("_Glossiness", 0.95f);
                    var colorUltra = portalColor;
                    colorUltra.a = 0.5f;
                    material.color = colorUltra;
                    material.SetColor("_EmissionColor", portalColor * 5.0f);
                    break;

                default:
                    material = new Material(Shader.Find("Unlit/Color"));
                    material.color = portalColor;
                    break;
            }

            renderer.material = material;
        }

        private static void ApplyParticleQuality(GameObject particles, Color portalColor, QualityLevel quality)
        {
            var particleSystem = particles.GetComponent<ParticleSystem>();
            if (particleSystem == null) return;

            var main = particleSystem.main;
            var emission = particleSystem.emission;
            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();

            switch (quality)
            {
                case QualityLevel.Low:
                    main.maxParticles = 50;
                    emission.rateOverTime = 20f;
                    main.startSize = 0.1f;
                    break;

                case QualityLevel.Medium:
                    main.maxParticles = 150;
                    emission.rateOverTime = 60f;
                    main.startSize = 0.12f;
                    break;

                case QualityLevel.High:
                    main.maxParticles = 300;
                    emission.rateOverTime = 120f;
                    main.startSize = 0.15f;
                    break;

                case QualityLevel.Ultra:
                    // Same as High
                    main.maxParticles = 300;
                    emission.rateOverTime = 120f;
                    main.startSize = 0.15f;
                    break;
            }

            // Ensure renderer uses default material for compatibility
            if (renderer != null)
            {
                renderer.material = null; // Use default
            }
        }

        public static QualityLevel ParseQualityLevel(string qualityString)
        {
            if (string.IsNullOrWhiteSpace(qualityString))
                return QualityLevel.Medium;

            switch (qualityString.ToLowerInvariant())
            {
                case "low":
                    return QualityLevel.Low;
                case "medium":
                    return QualityLevel.Medium;
                case "high":
                    return QualityLevel.High;
                case "ultra":
                    return QualityLevel.Ultra;
                default:
                    return QualityLevel.Medium;
            }
        }
    }
}

