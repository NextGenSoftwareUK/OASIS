using OASIS.Omniverse.UnityHost.Config;
using UnityEngine;

namespace OASIS.Omniverse.UnityHost.Runtime
{
    public static class SpaceHubBuilder
    {
        public static void BuildHub(OmniverseHostConfig config)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;

            var directionalLight = new GameObject("Directional Light");
            var light = directionalLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.7f;
            directionalLight.transform.rotation = Quaternion.Euler(35f, 30f, 0f);

            BuildStarfield();
            BuildPlayerRig();

            foreach (var game in config.games)
            {
                BuildPortal(game);
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

        private static void BuildPortal(HostedGameDefinition game)
        {
            var portalRoot = new GameObject($"Portal_{game.displayName}");
            portalRoot.transform.position = new Vector3(game.portalX, 0f, game.portalZ);

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "PortalRing";
            ring.transform.SetParent(portalRoot.transform, false);
            ring.transform.localScale = new Vector3(2.3f, 0.15f, 2.3f);

            var portalColor = new Color(game.portalColorR, game.portalColorG, game.portalColorB);
            var ringRenderer = ring.GetComponent<Renderer>();
            var ringMaterial = new Material(Shader.Find("Standard"));
            ringMaterial.EnableKeyword("_EMISSION");
            ringMaterial.color = portalColor * 0.2f;
            ringMaterial.SetColor("_EmissionColor", portalColor * 2.5f);
            ringRenderer.material = ringMaterial;

            ring.AddComponent<PortalVisualSpin>();

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
            text.characterSize = 0.35f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = portalColor;
            text.fontSize = 64;
        }
    }
}

