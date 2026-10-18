using UnityEngine;

namespace WoguaShooter.Core
{
    /// <summary>
    /// 移动端友好的火焰粒子特效：多层粒子系统共同形成火焰体积、内焰、火星和热雾。
    /// </summary>
    public sealed class FireParticleVfx : MonoBehaviour
    {
        private const float FireWidth = 0.82f;
        private static Material outerFireMaterial;
        private static Material innerFireMaterial;
        private static Material emberMaterial;
        private static Material hazeMaterial;
        private static Mesh flameSurfaceMesh;
        private Transform haloTransform;
        private Transform groundGlowTransform;
        private SpriteRenderer haloRenderer;
        private SpriteRenderer groundGlowRenderer;
        private float flickerSeed;
        private bool initialized;

        public int ActiveParticleCount
        {
            get
            {
                ParticleSystem[] systems = GetComponentsInChildren<ParticleSystem>(true);
                int count = 0;
                for (int i = 0; i < systems.Length; i++)
                    count += systems[i] != null ? systems[i].particleCount : 0;
                return count;
            }
        }

        public static FireParticleVfx Create(Transform parent)
        {
            GameObject root = new GameObject("FireParticleVfx");
            root.transform.SetParent(parent, false);
            FireParticleVfx vfx = root.AddComponent<FireParticleVfx>();
            vfx.Build();
            return vfx;
        }

        private void Awake()
        {
            Build();
        }

        private void Build()
        {
            if (initialized)
                return;

            initialized = true;
            flickerSeed = Random.Range(0f, 100f);
            CreateGlowLayers();
            CreateFlameSurface();
            CreateEmberLayer();
            CreateHeatHazeLayer();
        }

        private void Update()
        {
            if (!initialized || haloTransform == null || groundGlowTransform == null)
                return;

            float flicker = Mathf.PerlinNoise(flickerSeed, Time.time * 4.8f);
            float pulse = Mathf.Lerp(0.92f, 1.08f, flicker);
            haloTransform.localScale = new Vector3(1.38f * pulse, 1.02f * pulse, 1f);
            groundGlowTransform.localScale = new Vector3(1.2f * pulse, 0.27f, 1f);
            if (haloRenderer != null)
                haloRenderer.color = new Color(1f, 0.23f, 0.025f, Mathf.Lerp(0.1f, 0.18f, flicker));
            if (groundGlowRenderer != null)
                groundGlowRenderer.color = new Color(1f, 0.68f, 0.12f, Mathf.Lerp(0.16f, 0.25f, flicker));
        }

        private void CreateGlowLayers()
        {
            GameObject halo = new GameObject("FireHalo", typeof(SpriteRenderer));
            haloTransform = halo.transform;
            haloTransform.SetParent(transform, false);
            haloTransform.localPosition = new Vector3(0f, 0.39f, 0f);
            haloTransform.localScale = new Vector3(1.38f, 1.02f, 1f);
            haloRenderer = halo.GetComponent<SpriteRenderer>();
            haloRenderer.sprite = RuntimeEffectSprites.Smoke;
            haloRenderer.color = new Color(1f, 0.23f, 0.025f, 0.14f);
            haloRenderer.sortingOrder = 12;

            GameObject groundGlow = new GameObject("GroundGlow", typeof(SpriteRenderer));
            groundGlowTransform = groundGlow.transform;
            groundGlowTransform.SetParent(transform, false);
            groundGlowTransform.localPosition = new Vector3(0f, 0.055f, 0f);
            groundGlowTransform.localScale = new Vector3(1.2f, 0.27f, 1f);
            groundGlowRenderer = groundGlow.GetComponent<SpriteRenderer>();
            groundGlowRenderer.sprite = RuntimeEffectSprites.Smoke;
            groundGlowRenderer.color = new Color(1f, 0.68f, 0.12f, 0.2f);
            groundGlowRenderer.sortingOrder = 17;
        }

        private void CreateFlameSurface()
        {
            GameObject surface = new GameObject("ProceduralFireSurface", typeof(MeshFilter), typeof(MeshRenderer));
            surface.transform.SetParent(transform, false);
            surface.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            surface.transform.localScale = new Vector3(0.74f, 1.02f, 1f);
            surface.GetComponent<MeshFilter>().sharedMesh = GetFlameSurfaceMesh();
            MeshRenderer renderer = surface.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GetFireMaterial(false);
            renderer.sortingOrder = 18;
        }

        private static Mesh GetFlameSurfaceMesh()
        {
            if (flameSurfaceMesh != null)
                return flameSurfaceMesh;

            flameSurfaceMesh = new Mesh
            {
                name = "ProceduralFireSurfaceMesh",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = new[]
                {
                    new Vector3(-0.5f, 0f, 0f),
                    new Vector3(0.5f, 0f, 0f),
                    new Vector3(-0.5f, 1f, 0f),
                    new Vector3(0.5f, 1f, 0f)
                },
                uv = new[]
                {
                    new Vector2(0f, 0f),
                    new Vector2(1f, 0f),
                    new Vector2(0f, 1f),
                    new Vector2(1f, 1f)
                },
                triangles = new[] { 0, 2, 1, 2, 3, 1 }
            };
            flameSurfaceMesh.RecalculateBounds();
            return flameSurfaceMesh;
        }

        private ParticleSystem CreateFlameLayer(
            string objectName,
            Sprite sprite,
            int sortingOrder,
            Color baseColor,
            Color tipColor,
            Vector2 sizeRange,
            Vector2 lifetimeRange,
            Vector2 emissionRange,
            float noiseStrength,
            float upwardSpeed)
        {
            GameObject layerObject = new GameObject(objectName);
            layerObject.transform.SetParent(transform, false);
            ParticleSystem particles = layerObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 1f;
            main.maxParticles = 48;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetimeRange.x, lifetimeRange.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.22f);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.34f, 0.34f);
            main.startColor = new Color(1f, 1f, 1f, baseColor.a);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(emissionRange.x, emissionRange.y);

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = new Vector3(0f, 0.14f, 0f);
            shape.scale = new Vector3(FireWidth * 0.72f, 0.08f, 0.05f);

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = false;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(0f);
            velocity.y = new ParticleSystem.MinMaxCurve(upwardSpeed * 0.82f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);
            velocity.enabled = true;

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.separateAxes = true;
            noise.strength = new ParticleSystem.MinMaxCurve(noiseStrength * 0.7f, noiseStrength);
            noise.frequency = 0.75f;
            noise.scrollSpeed = 0.55f;

            ParticleSystem.LimitVelocityOverLifetimeModule limit = particles.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = new ParticleSystem.MinMaxCurve(1.4f, 2.4f);
            limit.dampen = 0.32f;

            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(baseColor, 0f),
                    new GradientColorKey(tipColor, 0.42f),
                    new GradientColorKey(new Color(tipColor.r, tipColor.g, tipColor.b, 1f), 0.78f),
                    new GradientColorKey(new Color(0.9f, 0.08f, 0.015f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(baseColor.a, 0f),
                    new GradientAlphaKey(0.98f, 0.2f),
                    new GradientAlphaKey(0.78f, 0.74f),
                    new GradientAlphaKey(0f, 1f)
                });
            color.color = gradient;

            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.44f),
                new Keyframe(0.14f, 1f),
                new Keyframe(0.68f, 0.82f),
                new Keyframe(1f, 0.18f)));

            ParticleSystem.RotationOverLifetimeModule rotation = particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-1.6f, 1.6f);

            ParticleSystemRenderer renderer = layerObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.sharedMaterial = GetFireMaterial(objectName.StartsWith("Inner"));
            renderer.sortingOrder = sortingOrder;
            particles.Play();
            return particles;
        }

        private void CreateEmberLayer()
        {
            GameObject layerObject = new GameObject("RisingEmberParticles");
            layerObject.transform.SetParent(transform, false);
            ParticleSystem particles = layerObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 1f;
            main.maxParticles = 20;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.32f, 0.78f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.85f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.055f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
            main.startColor = new Color(1f, 0.48f, 0.04f, 1f);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(4f, 7f);
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = new Vector3(0f, 0.3f, 0f);
            shape.scale = new Vector3(FireWidth * 0.65f, 0.32f, 0.05f);
            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = false;
            velocity.x = new ParticleSystem.MinMaxCurve(0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.28f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);
            velocity.enabled = true;
            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            noise.frequency = 0.9f;
            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.95f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.18f, 0.01f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            ParticleSystemRenderer renderer = layerObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 0.28f;
            renderer.velocityScale = 0.08f;
            renderer.sharedMaterial = GetSpriteParticleMaterial(RuntimeEffectSprites.Ember.texture, true);
            renderer.sortingOrder = 27;
            particles.Play();
        }

        private void CreateHeatHazeLayer()
        {
            GameObject layerObject = new GameObject("HeatHazeParticles");
            layerObject.transform.SetParent(transform, false);
            ParticleSystem particles = layerObject.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.duration = 1f;
            main.maxParticles = 8;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.35f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.34f);
            main.startColor = new Color(1f, 0.72f, 0.38f, 0.1f);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.position = new Vector3(0f, 0.76f, 0f);
            shape.scale = new Vector3(FireWidth * 0.6f, 0.24f, 0.05f);
            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = false;
            velocity.x = new ParticleSystem.MinMaxCurve(0f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.22f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);
            velocity.enabled = true;
            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.18f, 0.36f);
            noise.frequency = 0.45f;
            ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
            color.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.7f, 0.32f), 0f), new GradientColorKey(new Color(1f, 0.36f, 0.1f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.13f, 0.24f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;
            ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.45f), new Keyframe(0.65f, 1f), new Keyframe(1f, 1.2f)));
            ParticleSystemRenderer renderer = layerObject.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = GetSpriteParticleMaterial(RuntimeEffectSprites.Smoke.texture, false);
            renderer.sortingOrder = 10;
            particles.Play();
        }

        private static Material GetFireMaterial(bool inner)
        {
            Material cached = inner ? innerFireMaterial : outerFireMaterial;
            if (cached != null)
                return cached;

            Shader shader = Resources.Load<Shader>("Gameplay/ProceduralFireParticle");
            if (shader == null)
                shader = Shader.Find("Wogua/ProceduralFireParticle");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                return null;

            Material material = new Material(shader)
            {
                name = inner ? "ProceduralInnerFire" : "ProceduralOuterFire",
                hideFlags = HideFlags.HideAndDontSave
            };
            if (material.HasProperty("_BottomColor"))
            {
                material.SetColor("_BottomColor", inner
                    ? new Color(1f, 1f, 0.78f, 1f)
                    : new Color(1f, 0.78f, 0.12f, 1f));
                material.SetColor("_MiddleColor", inner
                    ? new Color(1f, 0.66f, 0.06f, 1f)
                    : new Color(1f, 0.24f, 0.012f, 1f));
                material.SetColor("_TipColor", inner
                    ? new Color(1f, 0.18f, 0.008f, 1f)
                    : new Color(0.42f, 0.018f, 0.004f, 1f));
                material.SetFloat("_Distortion", inner ? 0.11f : 0.2f);
                material.SetFloat("_Softness", inner ? 0.065f : 0.095f);
                material.SetFloat("_Brightness", inner ? 1.45f : 1.08f);
            }

            if (inner)
                innerFireMaterial = material;
            else
                outerFireMaterial = material;
            return material;
        }

        private static Material GetFireSpriteMaterial(Texture2D texture, bool inner)
        {
            Material cached = inner ? innerFireMaterial : outerFireMaterial;
            if (cached != null && cached.shader != null && cached.shader.name == "Sprites/Default")
                return cached;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                return null;
            Material material = new Material(shader)
            {
                name = inner ? "InnerFireParticleAlpha" : "OuterFireParticleAlpha",
                hideFlags = HideFlags.HideAndDontSave,
                mainTexture = texture
            };
            material.SetTexture("_MainTex", texture);
            if (inner)
                innerFireMaterial = material;
            else
                outerFireMaterial = material;
            return material;
        }

        private static Material GetSpriteParticleMaterial(Texture2D texture, bool additive)
        {
            Material cached = additive ? emberMaterial : hazeMaterial;
            if (cached != null)
                return cached;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                return null;
            Material material = new Material(shader)
            {
                name = additive ? "FireEmberAlpha" : "FireHazeAlpha",
                hideFlags = HideFlags.HideAndDontSave,
                mainTexture = texture
            };
            if (material.HasProperty("_MainTex"))
                material.SetTexture("_MainTex", texture);
            if (additive)
                emberMaterial = material;
            else
                hazeMaterial = material;
            return material;
        }
    }
}
