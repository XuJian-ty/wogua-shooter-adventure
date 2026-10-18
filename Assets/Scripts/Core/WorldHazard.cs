using UnityEngine;

namespace WoguaShooter.Core
{
    public enum WorldHazardType
    {
        Spikes,
        FirePit,
        Missile
    }

    public class WorldHazard : MonoBehaviour
    {
        private WorldHazardType hazardType;
        private SpriteRenderer mainRenderer;
        private FireParticleVfx fireParticleVfx;
        private float missileExtraSpeed;
        private float baseY;
        private float age;
        private bool consumed;

        public static WorldHazard Create(WorldHazardType type, Vector3 position)
        {
            GameObject hazardObject = new GameObject(type.ToString());
            hazardObject.transform.position = position;
            SpriteRenderer renderer = hazardObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = type == WorldHazardType.Missile ? 14 : 11;

            BoxCollider2D collider = hazardObject.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;

            WorldHazard hazard = hazardObject.AddComponent<WorldHazard>();
            hazard.Configure(type, renderer, collider, position.y);
            return hazard;
        }

        private void Awake()
        {
            GameManager.RegisterLevelObject(gameObject);
            mainRenderer = GetComponent<SpriteRenderer>();
            baseY = transform.position.y;
        }

        private void Configure(WorldHazardType type, SpriteRenderer renderer, BoxCollider2D collider, float spawnY)
        {
            hazardType = type;
            mainRenderer = renderer;
            baseY = transform.position.y;

            if (type == WorldHazardType.Spikes)
            {
                renderer.sprite = GameplaySpriteLibrary.Get("spikes");
                ScaleToWidth(transform, renderer.sprite, 0.9f);
                AlignRendererBottomTo(spawnY);
                collider.size = new Vector2(0.82f / transform.localScale.x, 0.34f / transform.localScale.y);
                collider.offset = new Vector2(0f, -0.08f / transform.localScale.y);
            }
            else if (type == WorldHazardType.Missile)
            {
                renderer.sprite = GameplaySpriteLibrary.Get("missile");
                ScaleToWidth(transform, renderer.sprite, 0.95f);
                collider.size = new Vector2(0.76f / transform.localScale.x, 0.3f / transform.localScale.y);
                missileExtraSpeed = Random.Range(2.8f, 4.1f);
            }
            else
            {
                renderer.sprite = RuntimeEffectSprites.FireBase;
                renderer.sortingOrder = 19;
                ScaleToWidth(transform, renderer.sprite, 1.15f);
                AlignRendererBottomTo(spawnY);
                collider.size = new Vector2(1.02f / transform.localScale.x, 0.34f / transform.localScale.y);
                collider.offset = new Vector2(0f, 0.12f / transform.localScale.y);
                fireParticleVfx = FireParticleVfx.Create(transform);
            }

            baseY = transform.position.y;
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            age += Time.deltaTime;
            float speed = GameManager.Instance.GetBackgroundSpeed();
            if (hazardType == WorldHazardType.Missile)
            {
                transform.position += Vector3.left * (speed + missileExtraSpeed) * Time.deltaTime;
                transform.position = new Vector3(transform.position.x,
                    baseY + Mathf.Sin(age * 4.2f) * 0.1f, transform.position.z);
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(age * 4.2f) * 3f);
            }
            else
            {
                transform.position += Vector3.left * speed * Time.deltaTime;
            }

            GameManager.RequestPhysicsTransformSync();
            if (transform.position.x < -12f)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HitPlayer(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HitPlayer(other);
        }

        private void HitPlayer(Collider2D other)
        {
            if (consumed || other.GetComponentInParent<PlayerController>() == null)
                return;

            GameManager.Instance?.PlayerHit();
            if (hazardType == WorldHazardType.Missile)
            {
                consumed = true;
                Destroy(gameObject);
            }
        }

        private void AlignRendererBottomTo(float surfaceY)
        {
            if (mainRenderer == null || mainRenderer.sprite == null)
                return;

            float offset = surfaceY - mainRenderer.bounds.min.y;
            transform.position += Vector3.up * offset;
        }

        private void OnDestroy()
        {
            GameManager.UnregisterLevelObject(gameObject);
        }

        private static void ScaleToWidth(Transform target, Sprite sprite, float width)
        {
            float spriteWidth = sprite != null ? sprite.bounds.size.x : 1f;
            target.localScale = Vector3.one * (width / Mathf.Max(0.01f, spriteWidth));
        }
    }
}
