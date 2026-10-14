using System.Collections;
using UnityEngine;

namespace WoguaShooter.Core
{
    public class Enemy : MonoBehaviour
    {
        [SerializeField] private int score = 10;
        [SerializeField] private float bobHeight = 0.05f;
        [SerializeField] private float bobSpeed = 3f;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer headRenderer;
        [SerializeField] private GameObject headCollisionPrefab;
        [SerializeField] private SpriteRenderer shadowRenderer;
        [SerializeField] private float shadowOffsetX;
        [SerializeField] private bool floatingEnemy;
        [SerializeField] private bool splitDeath;
        [SerializeField, Min(0.5f)] private float headRollDuration = 2f;
        [SerializeField] private ParticleSystem deathEffect;

        private Vector3 startPosition;
        private Vector3 baseScale;
        private Vector3 bodyBasePosition;
        private Vector3 bodyBaseScale;
        private Quaternion bodyBaseRotation;
        private Vector3 headBasePosition;
        private Vector3 headBaseScale;
        private Vector3 headSpriteCenter;
        private Quaternion headBaseRotation;
        private Collider2D bodyDamageCollider;
        private Collider2D headDamageCollider;
        private Collider2D headPlatformCollider;
        private Rigidbody2D headPlatformBody;
        private float groundSurfaceY;
        private float rootToFootOffset;
        private float headFootContactGrace;
        private bool headStompRewarded;
        private bool dead;
        private bool tutorialTarget;
        private bool tutorialRequiresStomp;
        private Color originalColor = Color.white;
        private Color originalHeadColor = Color.white;

        public bool IsAlive => !dead;
        public bool IsFloating => floatingEnemy;
        public bool IsTutorialTarget => tutorialTarget;
        public bool CanBeHitByProjectile => !tutorialRequiresStomp;
        public static event System.Action<Enemy, bool> Defeated;
        public bool IsOnScreen
        {
            get
            {
                Camera camera = Camera.main;
                if (camera == null)
                    return true;

                Bounds bounds = new Bounds(transform.position, Vector3.zero);
                if (bodyRenderer != null && bodyRenderer.enabled)
                    bounds.Encapsulate(bodyRenderer.bounds);
                if (headRenderer != null && headRenderer.enabled)
                    bounds.Encapsulate(headRenderer.bounds);

                Vector3 min = camera.WorldToViewportPoint(bounds.min);
                Vector3 max = camera.WorldToViewportPoint(bounds.max);
                return max.x >= 0f && min.x <= 1f && max.y >= 0f && min.y <= 1f && max.z > 0f;
            }
        }

        private void Awake()
        {
            GameManager.RegisterLevelObject(gameObject);
            startPosition = transform.position;
            baseScale = transform.localScale;
            bodyDamageCollider = GetComponent<Collider2D>();
            if (bodyRenderer != null)
            {
                originalColor = bodyRenderer.color;
                bodyBasePosition = bodyRenderer.transform.localPosition;
                bodyBaseScale = bodyRenderer.transform.localScale;
                bodyBaseRotation = bodyRenderer.transform.localRotation;
            }
            if (headRenderer != null)
            {
                originalHeadColor = headRenderer.color;
                headBasePosition = headRenderer.transform.localPosition;
                headBaseScale = headRenderer.transform.localScale;
                headBaseRotation = headRenderer.transform.localRotation;
                if (headRenderer.sprite != null)
                    headSpriteCenter = Vector3.Scale(headRenderer.sprite.bounds.center, headBaseScale);
            }
            CacheGroundPlacement();
            if (!floatingEnemy)
                ApplyGroundPlacement();
            if (splitDeath && headRenderer != null)
            {
                CreateHeadPlatform();
                UpdateHeadPlatform();
            }
            UpdateShadow();
        }

        private void Update()
        {
            if (dead)
                return;

            transform.position += Vector3.left * GetWorldSpeed() * Time.deltaTime;
            if (floatingEnemy)
            {
                float phase = Time.time * bobSpeed + startPosition.x * 1.7f;
                float bob = Mathf.Sin(phase) * bobHeight;
                float sway = Mathf.Sin(phase * 0.65f) * bobHeight * 0.35f;
                transform.position = new Vector3(transform.position.x, startPosition.y + bob, transform.position.z);
                transform.localScale = baseScale * (1f + Mathf.Abs(sway) * 0.35f);
                transform.Rotate(Vector3.forward, Mathf.Sin(phase * 0.75f) * 12f * Time.deltaTime);
            }
            else
            {
                // Ground enemies keep their feet on the same world height while scrolling.
                ApplyGroundPlacement();
                transform.localScale = baseScale;
                transform.localRotation = Quaternion.identity;
            }
            UpdateHeadPlatform();
            UpdateShadow();
            GameManager.RequestPhysicsTransformSync();

            if (transform.position.x < -12f)
                Destroy(gameObject);
        }

        private float GetWorldSpeed()
        {
            return GameManager.Instance != null ? GameManager.Instance.GetBackgroundSpeed() : 3f;
        }

        public void Kill()
        {
            Kill(false, transform.position);
        }

        public void MarkTutorialTarget(bool requiresStomp)
        {
            tutorialTarget = true;
            tutorialRequiresStomp = requiresStomp;
        }

        public bool TryStomp(Collider2D contactedCollider, PlayerController player)
        {
            if (contactedCollider == null || player == null)
                return false;

            bool isHeadCollider = contactedCollider == headPlatformCollider || contactedCollider == headDamageCollider;
            if (dead && !isHeadCollider)
                return false;

            bool isFootContact = player.IsFootContacting(contactedCollider.bounds);
            if (isHeadCollider && headPlatformCollider != null)
                isFootContact |= player.IsSupportedBy(headPlatformCollider);
            if (!isFootContact)
                return false;

            if (isHeadCollider)
            {
                headFootContactGrace = 0.15f;
                if (headStompRewarded)
                    return true;
                headStompRewarded = true;
            }

            Vector3 feedbackPosition = new Vector3(
                contactedCollider.bounds.center.x,
                contactedCollider.bounds.max.y,
                transform.position.z);
            if (!dead)
            {
                Kill(true, feedbackPosition);
            }
            else
            {
                GameManager.Instance?.AddScore(score);
                AudioManager.Instance?.PlayEnemyHit();
                CombatVfx.PlayStomp(feedbackPosition);
            }

            return true;
        }

        private void Kill(bool stomped, Vector3 feedbackPosition)
        {
            if (dead)
                return;

            dead = true;
            int awardedScore = score + (stomped ? score : 0);
            GameManager.Instance?.AddScore(awardedScore);
            Defeated?.Invoke(this, stomped);
            AudioManager.Instance?.PlayEnemyHit();
            if (bodyDamageCollider != null)
                bodyDamageCollider.enabled = false;
            if (headDamageCollider != null)
                headDamageCollider.enabled = false;

            float feedbackDuration = GameManager.Instance != null && GameManager.Instance.Config != null
                ? GameManager.Instance.Config.enemyHitFlashDuration + GameManager.Instance.Config.enemyHitShakeDuration
                : 0.2f;
            if (stomped)
                CombatVfx.PlayStomp(feedbackPosition, feedbackDuration);
            else
                CombatVfx.PlayEnemyHit(feedbackPosition, feedbackDuration);
            StartCoroutine(splitDeath && headRenderer != null ? SkeletonDeathAnimation() : DeathAnimation());
        }

        private IEnumerator DeathAnimation()
        {
            float hitFlashDuration = GameManager.Instance != null && GameManager.Instance.Config != null
                ? GameManager.Instance.Config.enemyHitFlashDuration : 0.08f;
            if (bodyRenderer != null)
            {
                bodyRenderer.color = new Color(1f, 0.45f, 0.45f, 1f);
                yield return new WaitForSeconds(hitFlashDuration);
                bodyRenderer.color = originalColor;
            }

            float duration = GameManager.Instance != null && GameManager.Instance.Config != null
                ? GameManager.Instance.Config.enemyDeathDuration : 0.22f;
            float shakeDuration = GameManager.Instance != null && GameManager.Instance.Config != null
                ? GameManager.Instance.Config.enemyHitShakeDuration : 0.12f;
            float shakeStrength = GameManager.Instance != null && GameManager.Instance.Config != null
                ? GameManager.Instance.Config.enemyHitShakeStrength : 0.08f;
            Vector3 scale = transform.localScale;
            Vector3 deathPosition = transform.position;
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                float t = time / duration;
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.Lerp(scale, Vector3.zero, eased);
                if (time < shakeDuration)
                {
                    float strength = shakeStrength * (1f - time / Mathf.Max(0.01f, shakeDuration));
                    Vector2 offset = Random.insideUnitCircle * strength;
                    transform.position = deathPosition + new Vector3(offset.x, offset.y, 0f);
                }
                transform.Rotate(Vector3.forward, 360f * Time.deltaTime);
                yield return null;
            }

            if (deathEffect != null)
                Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }

        private IEnumerator SkeletonDeathAnimation()
        {
            float hitFlashDuration = GameManager.Instance != null && GameManager.Instance.Config != null
                ? GameManager.Instance.Config.enemyHitFlashDuration : 0.08f;
            SetDeathFlash(true);
            yield return new WaitForSeconds(hitFlashDuration);
            SetDeathFlash(false);

            float duration = Mathf.Max(headRollDuration, GameManager.Instance != null && GameManager.Instance.Config != null
                ? GameManager.Instance.Config.enemyDeathDuration * 3.2f : headRollDuration);
            float bodyCollapseDuration = Mathf.Min(0.42f, duration * 0.7f);
            float bodyFadeDuration = Mathf.Min(0.7f, duration);
            float headGravity = 7.5f;
            Vector3 headPosition = headBasePosition;
            PlayerController player = FindFirstObjectByType<PlayerController>();
            float rollDirection = Random.Range(0, 2) == 0 ? -1f : 1f;
            float headHorizontalSpeed = rollDirection < 0f ? 2.6f : 0.25f;
            float headRollSpeed = 520f * (headHorizontalSpeed / 0.65f);
            Vector2 headVelocity = new Vector2(rollDirection * headHorizontalSpeed, 1.35f);
            float groundLocalY = GetGroundY() - transform.position.y;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float deltaTime = Time.deltaTime;
                elapsed += deltaTime;

                // Keep the dead skeleton anchored to the scrolling ground. The
                // head's local horizontal velocity is then relative to the ground.
                transform.position += Vector3.left * GetWorldSpeed() * deltaTime;

                if (bodyRenderer != null)
                {
                    float bodyT = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, bodyCollapseDuration));
                    float eased = 1f - Mathf.Pow(1f - bodyT, 3f);
                    // The bottom-left sprite pivot anchors the corpse while it falls to the right.
                    bodyRenderer.transform.localPosition = bodyBasePosition;
                    bodyRenderer.transform.localRotation = bodyBaseRotation * Quaternion.Euler(
                        0f, 0f, Mathf.Lerp(0f, -78f, eased));
                    bodyRenderer.transform.localScale = new Vector3(
                        bodyBaseScale.x,
                        bodyBaseScale.y * Mathf.Lerp(1f, 0.82f, eased),
                        bodyBaseScale.z);

                    float bodyAlpha = 1f - Mathf.InverseLerp(bodyCollapseDuration * 0.45f, bodyFadeDuration, elapsed);
                    Color bodyColor = originalColor;
                    bodyColor.a *= bodyAlpha;
                    bodyRenderer.color = bodyColor;
                }

                if (headRenderer != null)
                {
                    headVelocity.y -= headGravity * deltaTime;
                    headPosition += new Vector3(headVelocity.x, headVelocity.y, 0f) * deltaTime;
                    if (headPosition.y <= groundLocalY)
                    {
                        headPosition.y = groundLocalY;
                        headVelocity.y = 0f;
                    }
                    headRenderer.transform.localPosition = headPosition;
                    Quaternion headRotation = headBaseRotation * Quaternion.Euler(
                        0f, 0f, -headRollSpeed * elapsed * rollDirection);
                    headRenderer.transform.localRotation = headRotation;
                    headRenderer.transform.localPosition = headPosition +
                        headBaseRotation * headSpriteCenter - headRotation * headSpriteCenter;
                    UpdateHeadPlatform();
                    Physics2D.SyncTransforms();
                    CheckHeadHitsPlayer(player);

                    float fadeStart = duration * 0.62f;
                    if (elapsed > fadeStart)
                    {
                        float alpha = 1f - Mathf.InverseLerp(fadeStart, duration, elapsed);
                        Color color = originalHeadColor;
                        color.a = alpha;
                        headRenderer.color = color;
                    }
                }

                if (shadowRenderer != null)
                {
                    Color color = shadowRenderer.color;
                    color.a = Mathf.Lerp(color.a, 0f, Mathf.Clamp01(deltaTime * 8f));
                    shadowRenderer.color = color;
                }
                yield return null;
            }

            if (deathEffect != null)
                Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }

        private void CreateHeadPlatform()
        {
            if (headRenderer == null || headPlatformBody != null)
                return;

            SpriteRenderer sourceHeadRenderer = headRenderer;
            bool usingPrefab = headCollisionPrefab != null;
            GameObject platform = usingPrefab
                ? Instantiate(headCollisionPrefab, transform)
                : new GameObject("HeadPlatform");
            platform.name = "HeadPlatform";

            Transform platformTransform = platform.transform;
            platformTransform.SetParent(transform, false);
            platformTransform.localPosition = sourceHeadRenderer.transform.localPosition;
            platformTransform.localRotation = sourceHeadRenderer.transform.localRotation;
            platformTransform.localScale = sourceHeadRenderer.transform.localScale;

            SpriteRenderer prefabHeadRenderer = platform.GetComponent<SpriteRenderer>();
            if (usingPrefab && prefabHeadRenderer != null)
            {
                // Use the prefab renderer in play mode so its sprite and colliders
                // always share the exact same transform shown in Prefab Mode.
                prefabHeadRenderer.sprite = sourceHeadRenderer.sprite;
                prefabHeadRenderer.color = sourceHeadRenderer.color;
                prefabHeadRenderer.flipX = sourceHeadRenderer.flipX;
                prefabHeadRenderer.flipY = sourceHeadRenderer.flipY;
                prefabHeadRenderer.sortingLayerID = sourceHeadRenderer.sortingLayerID;
                prefabHeadRenderer.sortingOrder = sourceHeadRenderer.sortingOrder;
                sourceHeadRenderer.enabled = false;
                headRenderer = prefabHeadRenderer;
            }
            else if (prefabHeadRenderer != null)
            {
                prefabHeadRenderer.enabled = false;
            }

            headPlatformBody = platform.GetComponent<Rigidbody2D>();
            if (headPlatformBody == null)
                headPlatformBody = platform.AddComponent<Rigidbody2D>();
            headPlatformBody.bodyType = RigidbodyType2D.Kinematic;
            headPlatformBody.gravityScale = 0f;
            headPlatformBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            headPlatformBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            headPlatformBody.interpolation = RigidbodyInterpolation2D.Interpolate;

            Bounds spriteBounds = headRenderer.sprite != null
                ? headRenderer.sprite.bounds
                : new Bounds(Vector3.zero, new Vector3(0.55f, 0.55f, 0f));
            CircleCollider2D platformCollider = null;
            CircleCollider2D damageCollider = null;
            CircleCollider2D[] configuredColliders = platform.GetComponents<CircleCollider2D>();
            for (int i = 0; i < configuredColliders.Length; i++)
            {
                if (configuredColliders[i].isTrigger)
                    damageCollider = configuredColliders[i];
                else
                    platformCollider = configuredColliders[i];
            }

            if (platformCollider == null)
            {
                platformCollider = platform.AddComponent<CircleCollider2D>();
                platformCollider.radius = Mathf.Min(spriteBounds.size.x, spriteBounds.size.y) * 0.44f;
                platformCollider.offset = spriteBounds.center;
            }
            platformCollider.usedByEffector = true;
            headPlatformCollider = platformCollider;

            if (damageCollider == null)
            {
                damageCollider = platform.AddComponent<CircleCollider2D>();
                damageCollider.radius = platformCollider.radius + 0.06f;
                damageCollider.offset = platformCollider.offset;
            }
            damageCollider.isTrigger = true;
            headDamageCollider = damageCollider;

            PlatformEffector2D platformEffector = platform.GetComponent<PlatformEffector2D>();
            if (platformEffector == null)
                platformEffector = platform.AddComponent<PlatformEffector2D>();
            platformEffector.useOneWay = true;
            platformEffector.surfaceArc = 150f;
        }

        private void CacheGroundPlacement()
        {
            groundSurfaceY = FindGroundSurfaceY();
            if (floatingEnemy || bodyRenderer == null)
                return;

            rootToFootOffset = transform.position.y - bodyRenderer.bounds.min.y;
        }

        private void ApplyGroundPlacement()
        {
            transform.position = new Vector3(
                transform.position.x,
                groundSurfaceY + rootToFootOffset,
                transform.position.z);
        }

        private float FindGroundSurfaceY()
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                const float rayHeight = 20f;
                RaycastHit2D hit = Physics2D.Raycast(
                    new Vector2(transform.position.x, rayHeight),
                    Vector2.down,
                    rayHeight * 2f,
                    1 << groundLayer);
                if (hit.collider != null && !hit.collider.isTrigger)
                    return hit.collider.bounds.max.y;
            }

            Collider2D[] colliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider2D collider = colliders[i];
                if (collider != null && !collider.isTrigger && collider.gameObject.name == "GroundCollider")
                    return collider.bounds.max.y;
            }

            return GetGroundY();
        }

        private void UpdateHeadPlatform()
        {
            if (headPlatformBody == null || headRenderer == null)
                return;

            // The configured prefab renders the head and owns its colliders on the
            // same transform, so normal transform propagation keeps them aligned.
            if (headPlatformBody.transform == headRenderer.transform)
                return;

            Bounds visualBounds = headRenderer.bounds;
            Vector2 platformPosition = visualBounds.center;
            headPlatformBody.position = platformPosition;
            headPlatformBody.rotation = 0f;
        }

        private void CheckHeadHitsPlayer(PlayerController player)
        {
            headFootContactGrace = Mathf.Max(0f, headFootContactGrace - Time.deltaTime);
            if (tutorialTarget || player == null || headRenderer == null || GameManager.Instance == null ||
                GameManager.Instance.State != GameState.Playing)
                return;

            Collider2D playerCollider = player.GetComponent<Collider2D>();
            if (playerCollider == null || !headRenderer.bounds.Intersects(playerCollider.bounds))
                return;

            if (player.IsFootContacting(headRenderer.bounds))
            {
                headFootContactGrace = 0.15f;
                return;
            }

            if (headPlatformCollider != null && player.IsSupportedBy(headPlatformCollider))
            {
                headFootContactGrace = 0.15f;
                return;
            }

            if (headFootContactGrace > 0f)
                return;

            GameManager.Instance.PlayerHit();
        }

        private void SetDeathFlash(bool enabled)
        {
            if (bodyRenderer != null)
                bodyRenderer.color = enabled ? new Color(1f, 0.45f, 0.45f, 1f) : originalColor;
            if (headRenderer != null)
                headRenderer.color = enabled ? new Color(1f, 0.45f, 0.45f, 1f) : originalHeadColor;
        }

        private float GetGroundY()
        {
            return GameManager.Instance != null && GameManager.Instance.Config != null
                ? GameManager.Instance.Config.groundY : -1.28f;
        }

        private void OnDestroy()
        {
            GameManager.UnregisterLevelObject(gameObject);
        }

        private void UpdateShadow()
        {
            if (shadowRenderer == null)
                return;

            shadowRenderer.transform.position = new Vector3(
                transform.position.x + shadowOffsetX,
                GetGroundY(),
                transform.position.z + 0.02f);
            shadowRenderer.transform.rotation = Quaternion.identity;
        }
    }
}
