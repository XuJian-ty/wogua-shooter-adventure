using UnityEngine;

namespace WoguaShooter.Core
{
    /// <summary>
    /// 运行时生成的轻量战斗特效。它不依赖额外资源，适合通过 Addressables 加载的预制体。
    /// </summary>
    public static class CombatVfx
    {
        private static Sprite flashSprite;
        private static Material ringMaterial;
        private static Material particleMaterial;
        private static Mesh bloodDropletMesh;

        public static void PlayEnemyHit(Vector3 position, float duration = 0.22f)
        {
            CreateBloodBurst(position, new Color(0.96f, 0.035f, 0.025f, 1f), 11, 2.6f, duration);
            CreateBloodBurst(position, new Color(0.42f, 0.008f, 0.012f, 1f), 7, 1.9f, duration * 1.45f);
            CreateFlash(position, new Color(1f, 0.35f, 0.2f, 0.9f), 0.28f, duration * 0.65f);
        }

        public static void PlayStomp(Vector3 position, float duration = 0.22f)
        {
            float life = Mathf.Max(0.16f, duration);
            CreateBurst(position, new Color(1f, 0.88f, 0.3f, 1f), 14, 2.1f, life);
            CreateBurst(position, new Color(1f, 1f, 0.85f, 1f), 8, 1.35f, life * 0.75f);
            CreateFlash(position, new Color(1f, 0.95f, 0.55f, 0.95f), 0.3f, life * 0.6f);
            CreateRing(position, 0.34f, life);
        }

        public static void PlayGrowthSmash(Vector3 position, float duration = 0.26f)
        {
            float life = Mathf.Max(0.2f, duration);
            CreateBurst(position, new Color(0.52f, 1f, 0.24f, 1f), 20, 3.2f, life);
            CreateBurst(position, new Color(1f, 0.92f, 0.28f, 1f), 12, 2.2f, life * 0.85f);
            CreateFlash(position, new Color(0.86f, 1f, 0.54f, 0.95f), 0.46f, life * 0.7f);
            CreateRing(position, 0.48f, life);
        }

        public static void PlayCornExplosion(
            Vector3 position,
            float radius,
            float duration = 0.5f,
            Sprite[] explosionFrames = null)
        {
            float damageRadius = Mathf.Max(0.35f, radius);
            float visualDiameter = Mathf.Clamp(damageRadius * 0.92f, 0.75f, 1.65f);
            float visualRadius = visualDiameter * 0.5f;
            if (explosionFrames != null && explosionFrames.Length > 0)
                CreateSpriteSequence(position, explosionFrames, visualDiameter, duration * 0.8f, 39);
            CreateBurst(position, new Color(1f, 0.66f, 0.12f, 1f), 24, 3f, duration * 0.82f);
            CreateBurst(position, new Color(1f, 0.93f, 0.58f, 1f), 12, 1.8f, duration * 0.65f);
            CreateFlash(position, Color.white, visualDiameter * 0.35f, duration * 0.28f);
            CreateRing(position, visualRadius, duration * 0.8f);
        }

        private static void CreateSpriteSequence(
            Vector3 position,
            Sprite[] frames,
            float targetDiameter,
            float duration,
            int sortingOrder)
        {
            Sprite firstFrame = FindFirstFrame(frames);
            if (firstFrame == null)
                return;

            GameObject root = new GameObject(sortingOrder == 39 ? "序列帧战斗特效" : "序列帧特效");
            root.transform.position = position;
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = firstFrame;
            renderer.sortingOrder = sortingOrder;

            SpriteSequenceVfx sequence = root.AddComponent<SpriteSequenceVfx>();
            sequence.Setup(renderer, frames, duration, targetDiameter);
        }

        private static Sprite FindFirstFrame(Sprite[] frames)
        {
            if (frames == null)
                return null;

            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                    return frames[i];
            }

            return null;
        }

        private static void CreateBurst(Vector3 position, Color color, int count, float speed, float duration)
        {
            GameObject root = new GameObject("战斗粒子");
            root.transform.position = position;
            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = Mathf.Max(0.1f, duration);
            main.startLifetime = new ParticleSystem.MinMaxCurve(duration * 0.45f, duration);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.55f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.11f);
            main.startColor = color;
            main.gravityModifier = 0.15f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 200))
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.08f;

            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetParticleMaterial();
            renderer.sortingOrder = 40;
            particles.Play();
            Object.Destroy(root, Mathf.Max(0.2f, duration + 0.8f));
        }

        private static void CreateBloodBurst(Vector3 position, Color color, int count, float speed, float duration)
        {
            GameObject root = new GameObject("红色血液粒子");
            root.transform.position = position;
            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.loop = false;
            main.playOnAwake = false;
            main.duration = Mathf.Max(0.12f, duration);
            main.startLifetime = new ParticleSystem.MinMaxCurve(duration * 0.9f, duration * 2.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.48f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.065f, 0.15f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = color;
            main.gravityModifier = new ParticleSystem.MinMaxCurve(0.8f, 1.15f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 48;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = true;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 48))
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.055f;
            shape.radiusThickness = 1f;

            // 液滴整体向前飞散，并保留上下方向的随机喷溅。
            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(0.2f, 0.95f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.45f, 0.65f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            ParticleSystem.RotationOverLifetimeModule rotation = particles.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-2.8f, 2.8f);

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(Color.Lerp(color, new Color(0.12f, 0f, 0f, 1f), 0.58f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(color.a, 0f),
                    new GradientAlphaKey(color.a * 0.9f, 0.5f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = gradient;

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.55f),
                    new Keyframe(0.18f, 1f),
                    new Keyframe(1f, 0.22f)));

            ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetParticleMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = GetBloodDropletMesh();
            renderer.sortingOrder = 40;

            particles.Play();
            Object.Destroy(root, Mathf.Max(0.4f, duration * 2f + 0.8f));
        }

        private static Mesh GetBloodDropletMesh()
        {
            if (bloodDropletMesh != null)
                return bloodDropletMesh;

            const int segments = 16;
            Vector3[] vertices = new Vector3[segments + 1];
            Vector2[] uvs = new Vector2[segments + 1];
            int[] triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            uvs[0] = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                float irregularity = 1f + Mathf.Sin(angle * 3f + 0.65f) * 0.08f;
                float x = Mathf.Cos(angle) * 0.48f * irregularity;
                float yScale = Mathf.Sin(angle) > 0f ? 0.72f : 0.58f;
                float y = Mathf.Sin(angle) * yScale * irregularity;
                vertices[i + 1] = new Vector3(x, y, 0f);
                uvs[i + 1] = new Vector2(x + 0.5f, y * 0.65f + 0.5f);

                int triangle = i * 3;
                triangles[triangle] = 0;
                triangles[triangle + 1] = i + 1;
                triangles[triangle + 2] = (i + 1) % segments + 1;
            }

            bloodDropletMesh = new Mesh
            {
                name = "运行时血液液滴网格",
                hideFlags = HideFlags.HideAndDontSave,
                vertices = vertices,
                uv = uvs,
                triangles = triangles
            };
            bloodDropletMesh.RecalculateBounds();
            return bloodDropletMesh;
        }

        private static Material GetParticleMaterial()
        {
            if (particleMaterial != null)
                return particleMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Sprites/Default");
            if (shader == null)
                return null;

            particleMaterial = new Material(shader)
            {
                name = "运行时粒子材质",
                hideFlags = HideFlags.HideAndDontSave
            };
            if (particleMaterial.HasProperty("_BaseColor"))
                particleMaterial.SetColor("_BaseColor", Color.white);
            if (particleMaterial.HasProperty("_Color"))
                particleMaterial.SetColor("_Color", Color.white);
            return particleMaterial;
        }

        private static void CreateFlash(Vector3 position, Color color, float size, float duration)
        {
            GameObject root = new GameObject("命中闪光");
            root.transform.position = position;
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = GetFlashSprite();
            renderer.color = color;
            renderer.sortingOrder = 41;
            root.transform.localScale = Vector3.one * size;

            VfxFlash flash = root.AddComponent<VfxFlash>();
            flash.Setup(renderer, duration);
        }

        private static Sprite GetFlashSprite()
        {
            if (flashSprite != null)
                return flashSprite;

            const int resolution = 32;
            Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
            texture.name = "运行时闪光纹理";
            texture.filterMode = FilterMode.Bilinear;
            Color[] pixels = new Color[resolution * resolution];
            Vector2 center = new Vector2((resolution - 1) * 0.5f, (resolution - 1) * 0.5f);
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / (resolution * 0.5f);
                    float alpha = Mathf.Clamp01(1f - distance);
                    pixels[y * resolution + x] = new Color(1f, 1f, 1f, alpha * alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            flashSprite = Sprite.Create(texture, new Rect(0f, 0f, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
            return flashSprite;
        }

        private static void CreateRing(Vector3 position, float radius, float duration)
        {
            GameObject root = new GameObject("爆炸范围");
            root.transform.position = position;
            LineRenderer line = root.AddComponent<LineRenderer>();
            line.positionCount = 25;
            line.loop = true;
            line.useWorldSpace = false;
            line.widthMultiplier = 0.045f;
            line.numCapVertices = 2;
            line.sortingOrder = 42;
            line.sharedMaterial = GetRingMaterial();
            line.startColor = new Color(1f, 0.8f, 0.15f, 0.9f);
            line.endColor = new Color(1f, 0.3f, 0.04f, 0.1f);

            VfxRing ring = root.AddComponent<VfxRing>();
            ring.Setup(line, radius, duration);
        }

        private static Material GetRingMaterial()
        {
            if (ringMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                ringMaterial = new Material(shader);
                ringMaterial.name = "运行时爆炸圆环材质";
            }

            return ringMaterial;
        }
    }

    internal sealed class VfxFlash : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private float duration;
        private float elapsed;
        private Color color;
        private Vector3 baseScale;

        public void Setup(SpriteRenderer target, float life, float endScale = 1.8f)
        {
            spriteRenderer = target;
            duration = Mathf.Max(0.05f, life);
            color = spriteRenderer.color;
            baseScale = transform.localScale;
            scaleMultiplier = Mathf.Max(1f, endScale);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float fade = 1f - t;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(color.r, color.g, color.b, color.a * fade);
                transform.localScale = baseScale * Mathf.Lerp(1f, scaleMultiplier, Mathf.SmoothStep(0f, 1f, t));
            }

            if (elapsed >= duration)
                Destroy(gameObject);
        }

        private float scaleMultiplier = 1.8f;
    }

    internal sealed class SpriteSequenceVfx : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Sprite[] frames;
        private float duration;
        private float elapsed;
        private float targetDiameter;

        public void Setup(SpriteRenderer target, Sprite[] sequence, float life, float diameter)
        {
            spriteRenderer = target;
            frames = sequence;
            duration = Mathf.Max(0.05f, life);
            targetDiameter = Mathf.Max(0.05f, diameter);
            elapsed = 0f;
            ApplyFrame(FindUsableFrame(0));
        }

        private void Update()
        {
            if (spriteRenderer == null || frames == null || frames.Length == 0)
            {
                Destroy(gameObject);
                return;
            }

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            int frameIndex = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(t * frames.Length));
            ApplyFrame(FindUsableFrame(frameIndex));

            float fade = t > 0.8f ? 1f - Mathf.InverseLerp(0.8f, 1f, t) : 1f;
            Color color = spriteRenderer.color;
            spriteRenderer.color = new Color(color.r, color.g, color.b, fade);
            if (elapsed >= duration)
                Destroy(gameObject);
        }

        private Sprite FindUsableFrame(int preferredIndex)
        {
            if (frames[preferredIndex] != null)
                return frames[preferredIndex];

            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null)
                    return frames[i];
            }

            return null;
        }

        private void ApplyFrame(Sprite frame)
        {
            if (frame == null)
                return;

            spriteRenderer.sprite = frame;
            float frameSize = Mathf.Max(frame.bounds.size.x, frame.bounds.size.y);
            transform.localScale = Vector3.one * (targetDiameter / Mathf.Max(0.01f, frameSize));
        }
    }

    internal sealed class VfxRing : MonoBehaviour
    {
        private LineRenderer line;
        private float radius;
        private float duration;
        private float elapsed;

        public void Setup(LineRenderer target, float targetRadius, float life)
        {
            line = target;
            radius = Mathf.Max(0.1f, targetRadius);
            duration = Mathf.Max(0.1f, life);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float currentRadius = Mathf.Lerp(radius * 0.15f, radius, Mathf.SmoothStep(0f, 1f, t));
            if (line != null)
            {
                for (int i = 0; i < line.positionCount; i++)
                {
                    float angle = i / (float)(line.positionCount - 1) * Mathf.PI * 2f;
                    line.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * currentRadius);
                }

                float alpha = 1f - t;
                line.startColor = new Color(1f, 0.8f, 0.15f, alpha);
                line.endColor = new Color(1f, 0.3f, 0.04f, alpha * 0.2f);
            }

            if (elapsed >= duration)
                Destroy(gameObject);
        }
    }
}
