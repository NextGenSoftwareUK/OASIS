using System.Collections.Generic;
using OASIS.Omniverse.UnityHost.Config;
using UnityEngine;

namespace OASIS.Omniverse.UnityHost.Runtime
{
    public static class SpaceHubBuilder
    {
        private static GlobalSettingsService _settingsService;
        
        public static void BuildHub(OmniverseHostConfig config, GlobalSettingsService settingsService = null)
        {
            _settingsService = settingsService;
            
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;

            var directionalLight = new GameObject("Directional Light");
            var light = directionalLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.7f;
            directionalLight.transform.rotation = Quaternion.Euler(35f, 30f, 0f);

            BuildStarfield();
            BuildPlayerRig();

            // Get quality level from settings
            var qualityLevel = PortalQualityManager.QualityLevel.Medium;
            if (_settingsService != null && _settingsService.CurrentSettings != null)
            {
                qualityLevel = PortalQualityManager.ParseQualityLevel(_settingsService.CurrentSettings.graphicsPreset);
            }

            foreach (var game in config.games)
            {
                var portalRoot = BuildPortal(game);
                // Apply quality settings to portal
                var portalColor = new Color(
                    Mathf.Clamp01(game.portalColorR),
                    Mathf.Clamp01(game.portalColorG),
                    Mathf.Clamp01(game.portalColorB)
                );
                PortalQualityManager.ApplyQualityToPortal(portalRoot, portalColor, qualityLevel);
            }
        }

        private static void BuildStarfield()
        {
            var stars = new GameObject("Stars");
            var particleSystem = stars.AddComponent<ParticleSystem>();
            var main = particleSystem.main;
            main.startLifetime = 1000f;
            main.startSpeed = 0f;
            main.startSize = 0.05f;
            main.maxParticles = 3500;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = particleSystem.emission;
            emission.rateOverTime = 0f;

            var shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 100f;

            particleSystem.Emit(3500);
        }

        private static void BuildPlayerRig()
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0f, 0f, 0f);

            var capsule = player.AddComponent<CharacterController>();
            capsule.radius = 0.3f;
            capsule.height = 1.8f;
            capsule.center = new Vector3(0f, 0.9f, 0f);

            player.AddComponent<SimpleFirstPersonController>();
        }

        private static GameObject BuildPortal(HostedGameDefinition game)
        {
            var portalRoot = new GameObject($"Portal_{game.displayName}");
            portalRoot.transform.position = new Vector3(game.portalX, 0f, game.portalZ);

            var ring = CreateRingMesh(2.3f, 0.15f, 32);
            ring.name = "PortalRing";
            ring.transform.SetParent(portalRoot.transform, false);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

            var portalColor = new Color(
                Mathf.Clamp01(game.portalColorR), 
                Mathf.Clamp01(game.portalColorG), 
                Mathf.Clamp01(game.portalColorB)
            );
            
            // Debug: ensure color is valid
            if (portalColor.r == 0 && portalColor.g == 0 && portalColor.b == 0)
            {
                portalColor = Color.cyan; // Fallback
            }
            // Material will be applied by PortalQualityManager based on quality settings
            // For now, use a default that will be replaced
            var ringRenderer = ring.GetComponent<Renderer>();
            var ringMaterial = new Material(Shader.Find("Unlit/Color"));
            ringMaterial.color = portalColor;
            ringRenderer.material = ringMaterial;

            ring.AddComponent<PortalVisualSpin>();

            // Create simple portal surface - use a simple approach that works in builds
            var portalSurface = CreateDiscMesh(2.0f, 32);
            portalSurface.name = "PortalSurface";
            portalSurface.transform.SetParent(portalRoot.transform, false);
            portalSurface.transform.localPosition = Vector3.zero;
            portalSurface.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            
            // Material will be applied by PortalQualityManager based on quality settings
            // For now, use a default that will be replaced
            var surfaceRenderer = portalSurface.GetComponent<Renderer>();
            var surfaceMaterial = new Material(Shader.Find("Unlit/Color"));
            surfaceMaterial.color = portalColor;
            surfaceRenderer.material = surfaceMaterial;
            
            var portalSurfaceAnim = portalSurface.AddComponent<PortalSurfaceAnimation>();
            portalSurfaceAnim.portalColor = portalColor;

            // Add point light for portal glow
            var portalLight = new GameObject("PortalLight");
            portalLight.transform.SetParent(portalRoot.transform, false);
            portalLight.transform.localPosition = Vector3.zero;
            var light = portalLight.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = portalColor;
            light.intensity = 2.5f;
            light.range = 8f;
            light.shadows = LightShadows.None;

            // Add particle effects - simplified to work properly
            var particles = new GameObject("PortalParticles");
            particles.transform.SetParent(portalRoot.transform, false);
            particles.transform.localPosition = Vector3.zero;
            var particleSystem = particles.AddComponent<ParticleSystem>();
            var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            
            // Use default particle material - this will work in builds
            renderer.material = null; // Use default
            
            var main = particleSystem.main;
            main.startLifetime = 2.5f;
            main.startSpeed = 0.8f;
            main.startSize = 0.08f;
            main.startColor = portalColor;
            main.maxParticles = 150;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startRotation = 0f;
            main.startRotation3D = false;
            
            var emission = particleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = 60f;
            
            var shape = particleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 2.3f;
            shape.radiusThickness = 0.98f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            
            var velocityOverLifetime = particleSystem.velocityOverLifetime;
            velocityOverLifetime.enabled = true;
            velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
            velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(0.4f);
            
            var sizeOverLifetime = particleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            
            var colorOverLifetime = particleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(portalColor, 0.0f), new GradientColorKey(portalColor * 0.3f, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorOverLifetime.color = gradient;

            var trigger = new GameObject("Trigger");
            trigger.transform.SetParent(portalRoot.transform, false);
            trigger.transform.localPosition = new Vector3(0f, 1.0f, 0f);
            var box = trigger.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(3f, 3f, 3f);

            var portalTrigger = trigger.AddComponent<PortalTrigger>();
            portalTrigger.GameId = game.gameId;

            var label = new GameObject("Label");
            label.transform.SetParent(portalRoot.transform, false);
            label.transform.localPosition = new Vector3(0f, 3.2f, 0f);
            var text = label.AddComponent<TextMesh>();
            text.text = game.displayName;
            text.characterSize = 0.15f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = portalColor;
            text.fontSize = 32;
            
            return portalRoot;
        }

        private static GameObject CreateRingMesh(float radius, float thickness, int segments)
        {
            var ring = new GameObject("Ring");
            var meshFilter = ring.AddComponent<MeshFilter>();
            var meshRenderer = ring.AddComponent<MeshRenderer>();
            
            var mesh = new Mesh();
            mesh.name = "RingMesh";
            
            // Create a vertical ring directly in the YZ plane (standing upright, opening along Z axis)
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            
            float innerRadius = radius - thickness;
            
            // Generate vertices for left and right faces of the ring (X is the thickness direction)
            for (int face = 0; face < 2; face++)
            {
                float x = face == 0 ? -thickness * 0.5f : thickness * 0.5f;
                
                for (int i = 0; i <= segments; i++)
                {
                    float angle = (i / (float)segments) * Mathf.PI * 2f;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);
                    
                    // Ring is in YZ plane, so Y and Z form the circle, X is the thickness
                    // Outer ring
                    vertices.Add(new Vector3(x, cos * radius, sin * radius));
                    // Inner ring
                    vertices.Add(new Vector3(x, cos * innerRadius, sin * innerRadius));
                    
                    uvs.Add(new Vector2(i / (float)segments, face));
                    uvs.Add(new Vector2(i / (float)segments, face));
                }
            }
            
            // Create side faces (connecting left and right)
            int leftBase = 0;
            int rightBase = (segments + 1) * 2;
            
            for (int i = 0; i < segments; i++)
            {
                int leftOuter = leftBase + i * 2;
                int leftInner = leftBase + i * 2 + 1;
                int leftOuterNext = leftBase + (i + 1) * 2;
                int leftInnerNext = leftBase + (i + 1) * 2 + 1;
                
                int rightOuter = rightBase + i * 2;
                int rightInner = rightBase + i * 2 + 1;
                int rightOuterNext = rightBase + (i + 1) * 2;
                int rightInnerNext = rightBase + (i + 1) * 2 + 1;
                
                // Left face triangles
                triangles.Add(leftOuter);
                triangles.Add(leftInner);
                triangles.Add(leftOuterNext);
                triangles.Add(leftInner);
                triangles.Add(leftInnerNext);
                triangles.Add(leftOuterNext);
                
                // Right face triangles (reverse winding)
                triangles.Add(rightOuter);
                triangles.Add(rightOuterNext);
                triangles.Add(rightInner);
                triangles.Add(rightInner);
                triangles.Add(rightOuterNext);
                triangles.Add(rightInnerNext);
                
                // Outer side
                triangles.Add(leftOuter);
                triangles.Add(leftOuterNext);
                triangles.Add(rightOuter);
                triangles.Add(rightOuter);
                triangles.Add(leftOuterNext);
                triangles.Add(rightOuterNext);
                
                // Inner side
                triangles.Add(leftInner);
                triangles.Add(rightInner);
                triangles.Add(leftInnerNext);
                triangles.Add(rightInner);
                triangles.Add(rightInnerNext);
                triangles.Add(leftInnerNext);
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            meshFilter.mesh = mesh;
            
            return ring;
        }

        private static GameObject CreateDiscMesh(float radius, int segments)
        {
            var disc = new GameObject("Disc");
            var meshFilter = disc.AddComponent<MeshFilter>();
            var meshRenderer = disc.AddComponent<MeshRenderer>();
            
            var mesh = new Mesh();
            mesh.name = "DiscMesh";
            
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            
            // Center vertex
            vertices.Add(Vector3.zero);
            uvs.Add(new Vector2(0.5f, 0.5f));
            
            // Create vertices around the circle
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float y = Mathf.Sin(angle) * radius;
                
                vertices.Add(new Vector3(x, y, 0f));
                
                // UV coordinates
                float u = 0.5f + Mathf.Cos(angle) * 0.5f;
                float v = 0.5f + Mathf.Sin(angle) * 0.5f;
                uvs.Add(new Vector2(u, v));
            }
            
            // Create triangles from center to edge
            for (int i = 0; i < segments; i++)
            {
                triangles.Add(0); // Center vertex
                triangles.Add(i + 1);
                triangles.Add(i + 2);
            }
            
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            
            meshFilter.mesh = mesh;
            
            return disc;
        }
    }
}

