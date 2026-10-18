using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using WoguaShooter.Config;

namespace WoguaShooter.Core
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        // Visual-only glide wings are kept separate from the gameplay collider.
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private SpriteRenderer glideWings;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private SpriteRenderer shadowRenderer;
        [SerializeField] private float shadowOffsetX = -0.12f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundCheckRadius = 0.12f;
        [SerializeField, Min(0f)] private float enemyHitMinOverlap = 0.03f;
        [SerializeField, Min(0f)] private float glideWingFlapSpeed = 7.5f;
        [SerializeField, Range(0f, 25f)] private float glideWingFlapAngle = 7f;
        [SerializeField, Min(0f)] private float glideWingLift = 0.025f;

        private Rigidbody2D body;
        private Collider2D bodyCollider;
        private CapsuleCollider2D growthCollider;
        private Vector2 colliderBaseSize;
        private Vector2 colliderBaseOffset;
        private GameConfig config;
        private int jumpCount;
        private float magnetTime;
        private float shieldTime;
        private float damageInvincibleTime;
        private float growthTime;
        private float growthBlend;
        private float visualFootLocalY = -0.5f;
        private Vector3 visualBaseScale;
        private Vector3 visualBasePosition;
        private Quaternion visualBaseRotation;
        private Vector3 visualScaleVelocity;
        private Vector3 visualPositionVelocity;
        private Vector3 wingBasePosition;
        private Vector3 wingBaseScale;
        private Quaternion wingBaseRotation;
        private float baseGravityScale;
        private bool wasGrounded;
        private bool isGliding;
        private bool hasGlidedThisAirborne;
        private bool pointerPressActive;
        private bool pointerPressBlockedByUi;
        private float pointerHeldTime;
        private float landingTimer;
        private float gravityScaleBeforeGlide;
        private PointerEventData pointerEventData;
        private readonly List<RaycastResult> pointerRaycastResults = new List<RaycastResult>();
        private readonly Collider2D[] solidGroundOverlaps = new Collider2D[12];
        private ContactFilter2D solidGroundFilter;
        private bool tutorialInputEnabled = true;
        private bool tutorialFirstJumpEnabled = true;
        private bool tutorialSecondJumpEnabled = true;
        private bool tutorialGlideEnabled = true;
        private Transform shieldVisualRoot;
        private SpriteRenderer shieldGlowRenderer;
        private SpriteRenderer shieldGlassRenderer;
        private SpriteRenderer shieldRingRenderer;
        private SpriteRenderer shieldHighlightRenderer;
        private SpriteRenderer[] shieldBubbleRenderers;
        private Vector3[] shieldBubbleBasePositions;
        private float[] shieldBubblePhases;
        private Vector3 shieldBaseScale = Vector3.one;
        private Coroutine shieldBreakRoutine;
        private bool shieldBreaking;
        private SpriteRenderer[] playerVisualRenderers;
        private Color[] playerVisualBaseColors;

        public bool IsGliding => isGliding;
        public bool MagnetActive => magnetTime > 0f;
        public bool ShieldActive => shieldTime > 0f;
        public bool GrowthActive => growthTime > 0f;
        public bool DamageInvincible => damageInvincibleTime > 0f || GrowthActive;
        public event Action<int> Jumped;
        public event Action GlideStarted;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            if (body != null)
                body.constraints |= RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            bodyCollider = GetComponent<Collider2D>();
            growthCollider = bodyCollider as CapsuleCollider2D;
            if (growthCollider != null)
            {
                colliderBaseSize = growthCollider.size;
                colliderBaseOffset = growthCollider.offset;
            }
            config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            solidGroundFilter = new ContactFilter2D { useTriggers = false };
            baseGravityScale = body != null ? body.gravityScale : 3.2f;
            visualBaseScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            visualBasePosition = visualRoot != null ? visualRoot.localPosition : Vector3.zero;
            visualBaseRotation = visualRoot != null ? visualRoot.localRotation : Quaternion.identity;
            CacheVisualFootOffset();
            CachePlayerVisualColors();
            if (glideWings != null)
            {
                wingBasePosition = glideWings.transform.localPosition;
                wingBaseScale = glideWings.transform.localScale;
                wingBaseRotation = glideWings.transform.localRotation;
                glideWings.enabled = false;
            }
            UpdateShadow();
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            TickPowerUps(Time.deltaTime);

            if (!tutorialInputEnabled)
            {
                ClearPointerInput();
                return;
            }

            bool grounded = IsGrounded();
            bool supported = grounded && body.linearVelocity.y <= 0.1f;
            if (supported)
            {
                jumpCount = 0;
                hasGlidedThisAirborne = false;
            }

            if (grounded && !wasGrounded)
                landingTimer = 0.14f;
            wasGrounded = grounded;

            bool pointerPressed = PointerPressed();
            bool pointerHeld = PointerHeld();
            bool pointerReleased = PointerReleased();
            if (pointerPressed)
            {
                pointerHeldTime = 0f;
                if (supported)
                {
                    // Ground presses are consumed immediately; hold duration only matters
                    // when the press begins in the air.
                    pointerPressActive = false;
                    Jump();
                    grounded = false;
                }
                else
                {
                    pointerPressActive = true;
                }
            }
            if (pointerPressActive && pointerHeld)
                pointerHeldTime += Time.deltaTime;

            float glideHoldDelay = config != null ? Mathf.Max(0.01f, config.glideHoldDelay) : 0.12f;
            if (pointerReleased)
            {
                bool isTap = pointerPressActive && pointerHeldTime < glideHoldDelay;
                pointerPressActive = false;
                pointerHeldTime = 0f;
                if (isTap)
                    Jump();
            }

            if (!grounded && !hasGlidedThisAirborne && pointerPressActive && pointerHeld && pointerHeldTime >= glideHoldDelay)
                StartGlide();

            if (isGliding && (!pointerHeld || grounded))
                StopGlide();

            if (isGliding)
                UpdateGlide();

            UpdateShadow();
            AnimateVisual(grounded);
        }

        private bool PointerPressed()
        {
            if (IsPointerOverUi())
            {
                pointerPressBlockedByUi = true;
                return false;
            }

            pointerPressBlockedByUi = false;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;

            return Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
        }

        private bool PointerReleased()
        {
            if (pointerPressBlockedByUi)
            {
                if (IsPointerReleasedNow())
                    pointerPressBlockedByUi = false;
                return false;
            }

            return IsPointerReleasedNow();
        }

        private bool PointerHeld()
        {
            if (pointerPressBlockedByUi)
                return false;

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                return true;

            return Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed;
        }

        private bool IsPointerOverUi()
        {
            if (EventSystem.current == null)
                return false;

            Vector2 position;
            int pointerId;
            if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.leftButton.isPressed))
            {
                position = Mouse.current.position.ReadValue();
                pointerId = -1;
            }
            else if (Touchscreen.current != null && (Touchscreen.current.primaryTouch.press.wasPressedThisFrame ||
                Touchscreen.current.primaryTouch.press.isPressed))
            {
                position = Touchscreen.current.primaryTouch.position.ReadValue();
                pointerId = Touchscreen.current.primaryTouch.touchId.ReadValue();
            }
            else
            {
                return false;
            }

            if (pointerEventData == null)
                pointerEventData = new PointerEventData(EventSystem.current);

            pointerEventData.Reset();
            pointerEventData.pointerId = pointerId;
            pointerEventData.position = position;
            pointerRaycastResults.Clear();
            EventSystem.current.RaycastAll(pointerEventData, pointerRaycastResults);
            for (int i = 0; i < pointerRaycastResults.Count; i++)
            {
                GameObject hit = pointerRaycastResults[i].gameObject;
                if (hit == null)
                    continue;

                // Full-screen decorative Images can receive raycasts, but should
                // not prevent gameplay input. Only actual event handlers block it.
                if (ExecuteEvents.GetEventHandler<IPointerClickHandler>(hit) != null ||
                    ExecuteEvents.GetEventHandler<IBeginDragHandler>(hit) != null ||
                    ExecuteEvents.GetEventHandler<IDragHandler>(hit) != null)
                    return true;
            }

            return false;
        }

        private static bool IsPointerReleasedNow()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
                return true;

            return Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
        }

        public void ResetSpellState()
        {
            StopGlide();
            isGliding = false;
            hasGlidedThisAirborne = false;
            pointerPressActive = false;
            pointerPressBlockedByUi = false;
            pointerHeldTime = 0f;
            magnetTime = 0f;
            shieldTime = 0f;
            damageInvincibleTime = 0f;
            growthTime = 0f;
            growthBlend = 0f;
            StopShieldBreak();
            SetShieldVisualVisible(false);
            UpdateGrowthCollider();
            UpdateDamageInvincibilityVisual();
            if (visualRoot != null)
            {
                visualRoot.localScale = visualBaseScale;
                visualRoot.localPosition = visualBasePosition;
                visualRoot.localRotation = visualBaseRotation;
            }
            if (body != null)
            {
                body.gravityScale = baseGravityScale;
                body.linearVelocity = new Vector2(body.linearVelocity.x, 0f);
            }
        }

        public bool ActivateMagnet()
        {
            float duration = config != null ? config.magnetDuration : 15f;
            magnetTime = Mathf.Max(magnetTime, duration);
            return true;
        }

        public bool ActivateShield()
        {
            if (ShieldActive)
                return false;

            StopShieldBreak();
            shieldTime = config != null ? config.shieldDuration : 30f;
            EnsureShieldVisual();
            SetShieldVisualVisible(true);
            return true;
        }

        public bool ActivateGrowth()
        {
            if (GrowthActive)
                return false;

            growthTime = config != null ? config.growthDuration : 10f;
            growthBlend = 1f;
            ApplyGrowthVisualImmediately();
            UpdateGrowthCollider();
            return true;
        }

        public bool CanMagnetAttract(Vector3 worldPosition)
        {
            if (!MagnetActive)
                return false;

            float forwardRange = config != null ? config.magnetForwardRange : 1f;
            float horizontalOffset = worldPosition.x - transform.position.x;
            return horizontalOffset <= forwardRange && horizontalOffset >= -0.75f;
        }

        public bool TryPreventFatalDamage()
        {
            if (GrowthActive || damageInvincibleTime > 0f)
                return true;

            if (!ShieldActive)
                return false;

            shieldTime = 0f;
            damageInvincibleTime = config != null ? config.shieldBreakInvincibleTime : 3f;
            PlayShieldBreak();
            return true;
        }

        public void SetTutorialInputEnabled(bool enabled)
        {
            tutorialInputEnabled = enabled;
            if (enabled)
            {
                tutorialFirstJumpEnabled = true;
                tutorialSecondJumpEnabled = true;
                tutorialGlideEnabled = true;
                return;
            }

            StopGlide();
            ClearPointerInput();
        }

        public void SetTutorialActionPermissions(
            bool firstJumpEnabled,
            bool secondJumpEnabled,
            bool glideEnabled)
        {
            tutorialFirstJumpEnabled = firstJumpEnabled;
            tutorialSecondJumpEnabled = secondJumpEnabled;
            tutorialGlideEnabled = glideEnabled;
            if (!glideEnabled)
                StopGlide();
            ClearPointerInput();
        }

        private void ClearPointerInput()
        {
            pointerPressActive = false;
            pointerHeldTime = 0f;
            if (IsPointerReleasedNow())
                pointerPressBlockedByUi = false;
        }

        private void StartGlide()
        {
            if (!tutorialGlideEnabled || isGliding || hasGlidedThisAirborne || body == null)
                return;

            hasGlidedThisAirborne = true;
            isGliding = true;
            SetGlideWingsVisible(true);
            gravityScaleBeforeGlide = body.gravityScale > 0f ? body.gravityScale : baseGravityScale;
            body.gravityScale = 0f;
            UpdateGlide();
            GlideStarted?.Invoke();
        }

        private void StopGlide()
        {
            if (!isGliding || body == null)
                return;

            isGliding = false;
            SetGlideWingsVisible(false);
            body.gravityScale = gravityScaleBeforeGlide > 0f ? gravityScaleBeforeGlide : baseGravityScale;
        }

        private void UpdateGlide()
        {
            if (body == null)
                return;

            float descentSpeed = config != null ? config.glideDescentSpeed : 0.45f;
            body.linearVelocity = new Vector2(0f, -descentSpeed);

            if (glideWings != null)
            {
                float flap = Mathf.Sin(Time.time * Mathf.Max(0.01f, glideWingFlapSpeed));
                glideWings.transform.localRotation = wingBaseRotation * Quaternion.Euler(0f, 0f, flap * glideWingFlapAngle);
                glideWings.transform.localPosition = wingBasePosition + Vector3.up * (flap * glideWingLift);
                glideWings.transform.localScale = wingBaseScale * (1f + Mathf.Abs(flap) * 0.035f);
            }
        }

        private void SetGlideWingsVisible(bool visible)
        {
            if (glideWings == null)
                return;

            glideWings.enabled = visible;
            if (!visible)
            {
                glideWings.transform.localPosition = wingBasePosition;
                glideWings.transform.localRotation = wingBaseRotation;
                glideWings.transform.localScale = wingBaseScale;
            }
        }

        private void Jump()
        {
            if (jumpCount >= 2)
                return;
            if (jumpCount == 0 && !tutorialFirstJumpEnabled)
                return;
            if (jumpCount == 1 && !tutorialSecondJumpEnabled)
                return;

            StopGlide();
            float force = jumpCount == 0 ? (config != null ? config.jumpForce : 10f) : (config != null ? config.secondJumpForce : 8.5f);
            body.linearVelocity = new Vector2(body.linearVelocity.x, force);
            jumpCount++;
            landingTimer = 0f;
            if (visualRoot != null)
            {
                float stretch = config != null ? config.playerJumpStretch : 0.1f;
                float growthScale = Mathf.Lerp(1f, config != null ? config.growthScale : 2f, growthBlend);
                visualRoot.localScale = new Vector3(
                    visualBaseScale.x * (1f - stretch * 0.45f) * growthScale,
                    visualBaseScale.y * (1f + stretch) * growthScale,
                    visualBaseScale.z);
                visualScaleVelocity = Vector3.zero;
            }
            Jumped?.Invoke(jumpCount);
        }

        private bool IsGrounded()
        {
            Vector2 point = groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.55f;
            if (Physics2D.OverlapCircle(point, groundCheckRadius, groundLayer) != null)
                return true;

            int overlapCount = Physics2D.OverlapCircle(
                point,
                groundCheckRadius,
                solidGroundFilter,
                solidGroundOverlaps);
            for (int i = 0; i < overlapCount; i++)
            {
                Collider2D hit = solidGroundOverlaps[i];
                solidGroundOverlaps[i] = null;
                if (hit == null || hit == bodyCollider || hit.attachedRigidbody == body || hit.transform.IsChildOf(transform))
                    continue;

                if (bodyCollider == null)
                    continue;

                ColliderDistance2D distance = Physics2D.Distance(bodyCollider, hit);
                if (distance.isValid && distance.distance <= 0.025f && distance.normal.y >= 0.5f)
                    return true;
            }

            return false;
        }

        private void UpdateShadow()
        {
            if (shadowRenderer == null)
                return;

            float groundY = config != null ? config.groundY : -1.28f;
            shadowRenderer.transform.position = new Vector3(transform.position.x + shadowOffsetX, groundY, transform.position.z + 0.02f);
            shadowRenderer.transform.rotation = Quaternion.identity;

            float height = Mathf.Max(0f, transform.position.y - groundY);
            float heightFactor = Mathf.Clamp01(height / 2.2f);
            shadowRenderer.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.72f, heightFactor);
            Color color = shadowRenderer.color;
            color.a = Mathf.Lerp(0.82f, 0.46f, heightFactor);
            shadowRenderer.color = color;
        }

        private void AnimateVisual(bool grounded)
        {
            if (visualRoot == null)
                return;

            float ease = config != null ? config.playerVisualEase : 12f;
            float stretch = config != null ? config.playerJumpStretch : 0.1f;
            float landingSquash = config != null ? config.playerLandingSquash : 0.14f;
            float bounce = grounded ? Mathf.Sin(Time.time * 8f) * 0.02f : 0f;
            Vector3 targetScale = visualBaseScale;

            if (!grounded)
            {
                float verticalSpeed = body != null ? body.linearVelocity.y : 0f;
                float direction = verticalSpeed >= 0f ? 1f : -0.55f;
                targetScale = new Vector3(
                    visualBaseScale.x * (1f - stretch * 0.45f * direction),
                    visualBaseScale.y * (1f + stretch * direction),
                    visualBaseScale.z);
            }
            else if (landingTimer > 0f)
            {
                landingTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(landingTimer / 0.14f);
                float squash = Mathf.SmoothStep(0f, landingSquash, t);
                targetScale = new Vector3(visualBaseScale.x * (1f + squash), visualBaseScale.y * (1f - squash), visualBaseScale.z);
            }

            targetScale.y += bounce;
            float growthScale = Mathf.Lerp(1f, config != null ? config.growthScale : 2f, growthBlend);
            targetScale = new Vector3(targetScale.x * growthScale, targetScale.y * growthScale, targetScale.z);
            visualRoot.localScale = Vector3.SmoothDamp(visualRoot.localScale, targetScale, ref visualScaleVelocity, 1f / Mathf.Max(1f, ease));

            float footAnchorOffset = -visualFootLocalY * visualBaseScale.y * (growthScale - 1f);
            Vector3 targetPosition = visualBasePosition + Vector3.up * footAnchorOffset +
                (grounded ? Vector3.up * bounce * growthScale : Vector3.zero);
            visualRoot.localPosition = Vector3.SmoothDamp(visualRoot.localPosition, targetPosition, ref visualPositionVelocity, 1f / Mathf.Max(1f, ease));

            float glideAngle = config != null ? config.glideVisualAngle : -12f;
            Quaternion targetRotation = visualBaseRotation * Quaternion.Euler(0f, 0f, isGliding ? glideAngle : 0f);
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, targetRotation, Mathf.Clamp01(Time.deltaTime * ease));
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleEnemyCollision(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandleEnemyCollision(other);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleSolidEnemyCollision(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            HandleSolidEnemyCollision(collision);
        }

        private void HandleSolidEnemyCollision(Collision2D collision)
        {
            Collider2D other = collision != null ? collision.collider : null;
            Enemy enemy = other != null ? other.GetComponentInParent<Enemy>() : null;
            if (enemy == null)
                return;

            if (enemy.TryStomp(other, this))
                return;

            if (GrowthActive && enemy.IsAlive)
                enemy.KillByGrowth(other.bounds.center);
        }

        private void HandleEnemyCollision(Collider2D other)
        {
            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.IsAlive)
                return;

            if (TryStompEnemy(enemy, other))
                return;

            if (GrowthActive)
            {
                enemy.KillByGrowth(other.bounds.center);
                return;
            }

            if (enemy.IsTutorialTarget)
                return;

            if (DamageInvincible)
                return;

            if (IsBackContacting(other.bounds))
                return;

            Collider2D playerCollider = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
            ColliderDistance2D distance = playerCollider != null ? Physics2D.Distance(playerCollider, other) : default;
            if (playerCollider != null && (!distance.isOverlapped || distance.distance > -enemyHitMinOverlap))
                return;

            GameManager.Instance?.PlayerHit();
        }

        private void TickPowerUps(float deltaTime)
        {
            magnetTime = Mathf.Max(0f, magnetTime - deltaTime);
            damageInvincibleTime = Mathf.Max(0f, damageInvincibleTime - deltaTime);

            bool shieldWasActive = shieldTime > 0f;
            shieldTime = Mathf.Max(0f, shieldTime - deltaTime);
            if (shieldWasActive && shieldTime <= 0f && !shieldBreaking)
                SetShieldVisualVisible(false);

            growthTime = Mathf.Max(0f, growthTime - deltaTime);
            growthBlend = Mathf.MoveTowards(growthBlend, GrowthActive ? 1f : 0f, deltaTime * 4f);
            UpdateGrowthCollider();
            UpdateDamageInvincibilityVisual();
            UpdateShieldVisual();
        }

        private void UpdateGrowthCollider()
        {
            if (growthCollider == null)
                return;

            float scale = Mathf.Lerp(1f, config != null ? config.growthScale : 2f, growthBlend);
            growthCollider.size = colliderBaseSize * scale;
            growthCollider.offset = new Vector2(
                colliderBaseOffset.x,
                colliderBaseOffset.y + colliderBaseSize.y * (scale - 1f) * 0.5f);
            GameManager.RequestPhysicsTransformSync();
        }

        private void ApplyGrowthVisualImmediately()
        {
            if (visualRoot == null)
                return;

            float growthScale = Mathf.Lerp(1f, config != null ? config.growthScale : 2f, growthBlend);
            visualRoot.localScale = new Vector3(
                visualBaseScale.x * growthScale,
                visualBaseScale.y * growthScale,
                visualBaseScale.z);
            float footAnchorOffset = -visualFootLocalY * visualBaseScale.y * (growthScale - 1f);
            visualRoot.localPosition = visualBasePosition + Vector3.up * footAnchorOffset;
            visualScaleVelocity = Vector3.zero;
            visualPositionVelocity = Vector3.zero;
        }

        private void CachePlayerVisualColors()
        {
            if (visualRoot == null)
                return;

            SpriteRenderer[] renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
            List<SpriteRenderer> visibleRenderers = new List<SpriteRenderer>(renderers.Length);
            List<Color> colors = new List<Color>(renderers.Length);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i] == shadowRenderer)
                    continue;
                visibleRenderers.Add(renderers[i]);
                colors.Add(renderers[i].color);
            }

            playerVisualRenderers = visibleRenderers.ToArray();
            playerVisualBaseColors = colors.ToArray();
        }

        private void UpdateDamageInvincibilityVisual()
        {
            if (playerVisualRenderers == null || playerVisualBaseColors == null)
                return;

            float alphaMultiplier = damageInvincibleTime > 0f
                ? 0.58f + Mathf.Sin(Time.unscaledTime * 11f) * 0.1f
                : 1f;
            for (int i = 0; i < playerVisualRenderers.Length; i++)
            {
                SpriteRenderer renderer = playerVisualRenderers[i];
                if (renderer == null)
                    continue;
                Color color = playerVisualBaseColors[i];
                color.a *= alphaMultiplier;
                renderer.color = color;
            }
        }

        private void CacheVisualFootOffset()
        {
            if (visualRoot == null)
                return;

            SpriteRenderer[] renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
            float minimumY = float.PositiveInfinity;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null || renderer == glideWings || renderer == shadowRenderer)
                    continue;
                minimumY = Mathf.Min(minimumY, renderer.bounds.min.y);
            }

            if (!float.IsPositiveInfinity(minimumY))
            {
                Vector3 localFoot = visualRoot.InverseTransformPoint(
                    new Vector3(visualRoot.position.x, minimumY, visualRoot.position.z));
                visualFootLocalY = localFoot.y;
            }
        }

        private void EnsureShieldVisual()
        {
            if (shieldVisualRoot != null)
                return;

            GameObject root = new GameObject("ShieldVisual");
            shieldVisualRoot = root.transform;
            shieldVisualRoot.SetParent(transform, false);
            Bounds bounds;
            if (!TryGetVisualBounds(out bounds))
            {
                bounds = bodyCollider != null
                    ? bodyCollider.bounds
                    : new Bounds(transform.position, new Vector3(0.9f, 1.1f, 0f));
            }
            shieldVisualRoot.position = bounds.center;

            GameObject glow = new GameObject("Glow", typeof(SpriteRenderer));
            glow.transform.SetParent(shieldVisualRoot, false);
            shieldGlowRenderer = glow.GetComponent<SpriteRenderer>();
            shieldGlowRenderer.sprite = RuntimeEffectSprites.ShieldGlow;
            shieldGlowRenderer.color = new Color(0.38f, 0.86f, 1f, 0.28f);
            shieldGlowRenderer.sortingOrder = 27;

            GameObject glass = new GameObject("Glass", typeof(SpriteRenderer));
            glass.transform.SetParent(shieldVisualRoot, false);
            shieldGlassRenderer = glass.GetComponent<SpriteRenderer>();
            shieldGlassRenderer.sprite = RuntimeEffectSprites.ShieldGlass;
            shieldGlassRenderer.color = Color.white;
            shieldGlassRenderer.sortingOrder = 28;

            GameObject ring = new GameObject("Ring", typeof(SpriteRenderer));
            ring.transform.SetParent(shieldVisualRoot, false);
            shieldRingRenderer = ring.GetComponent<SpriteRenderer>();
            shieldRingRenderer.sprite = RuntimeEffectSprites.ShieldRing;
            shieldRingRenderer.color = Color.white;
            shieldRingRenderer.sortingOrder = 29;

            GameObject highlight = new GameObject("GlassHighlight", typeof(SpriteRenderer));
            highlight.transform.SetParent(shieldVisualRoot, false);
            highlight.transform.localScale = Vector3.one * 0.92f;
            shieldHighlightRenderer = highlight.GetComponent<SpriteRenderer>();
            shieldHighlightRenderer.sprite = RuntimeEffectSprites.ShieldHighlight;
            shieldHighlightRenderer.color = Color.white;
            shieldHighlightRenderer.sortingOrder = 30;

            shieldBubbleRenderers = new SpriteRenderer[6];
            shieldBubbleBasePositions = new Vector3[shieldBubbleRenderers.Length];
            shieldBubblePhases = new float[shieldBubbleRenderers.Length];
            for (int i = 0; i < shieldBubbleRenderers.Length; i++)
            {
                GameObject bubble = new GameObject($"Bubble_{i}", typeof(SpriteRenderer));
                bubble.transform.SetParent(shieldVisualRoot, false);
                float x = Mathf.Lerp(-0.3f, 0.3f, i / (float)(shieldBubbleRenderers.Length - 1));
                shieldBubbleBasePositions[i] = new Vector3(x, -0.3f, 0f);
                shieldBubblePhases[i] = Mathf.Repeat(i * 0.217f + 0.08f, 1f);
                SpriteRenderer bubbleRenderer = bubble.GetComponent<SpriteRenderer>();
                bubbleRenderer.sprite = RuntimeEffectSprites.Bubble;
                bubbleRenderer.sortingOrder = 31;
                shieldBubbleRenderers[i] = bubbleRenderer;
            }

            float spriteSize = Mathf.Max(0.01f, shieldRingRenderer.sprite.bounds.size.x);
            float diameter = Mathf.Max(bounds.size.x, bounds.size.y) * 1.3f;
            shieldBaseScale = Vector3.one * (diameter / spriteSize);
            shieldVisualRoot.localScale = shieldBaseScale;
            SetShieldVisualVisible(false);
        }

        private void UpdateShieldVisual()
        {
            if (shieldVisualRoot == null || (!ShieldActive && !shieldBreaking))
                return;

            if (TryGetVisualBounds(out Bounds bounds))
            {
                shieldVisualRoot.position = bounds.center;
                float spriteSize = shieldRingRenderer != null && shieldRingRenderer.sprite != null
                    ? Mathf.Max(0.01f, shieldRingRenderer.sprite.bounds.size.x)
                    : 1f;
                float diameter = Mathf.Max(bounds.size.x, bounds.size.y) * 1.3f;
                shieldBaseScale = Vector3.one * (diameter / spriteSize);
            }

            if (shieldBreaking)
                return;

            float pulse = 1f + Mathf.Sin(Time.time * 5.5f) * 0.035f;
            shieldVisualRoot.localScale = shieldBaseScale * pulse;
            shieldVisualRoot.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.45f) * 1.2f);
            UpdateShieldBubbles();
        }

        private void UpdateShieldBubbles()
        {
            if (shieldBubbleRenderers == null)
                return;

            for (int i = 0; i < shieldBubbleRenderers.Length; i++)
            {
                SpriteRenderer bubble = shieldBubbleRenderers[i];
                if (bubble == null)
                    continue;

                float cycle = Mathf.Repeat(Time.time * (0.16f + i * 0.009f) + shieldBubblePhases[i], 1f);
                Vector3 position = shieldBubbleBasePositions[i];
                position.x += Mathf.Sin(Time.time * 1.7f + i * 1.3f) * 0.025f;
                position.y = Mathf.Lerp(-0.32f, 0.34f, cycle);
                bubble.transform.localPosition = position;
                float size = (0.045f + i % 3 * 0.012f) * (1f + Mathf.Sin(Time.time * 2.2f + i) * 0.08f);
                bubble.transform.localScale = Vector3.one * size;
                float alpha = Mathf.Sin(cycle * Mathf.PI) * 0.62f;
                bubble.color = new Color(0.82f, 0.97f, 1f, alpha);
            }
        }

        private bool TryGetVisualBounds(out Bounds bounds)
        {
            bounds = default;
            if (visualRoot == null)
                return false;

            SpriteRenderer[] renderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
            bool found = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy ||
                    renderer == glideWings || renderer == shadowRenderer)
                    continue;

                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return found;
        }

        private void SetShieldVisualVisible(bool visible)
        {
            if (shieldVisualRoot != null)
            {
                shieldVisualRoot.gameObject.SetActive(visible);
                if (visible)
                    RestoreShieldColors();
            }
        }

        private void PlayShieldBreak()
        {
            EnsureShieldVisual();
            StopShieldBreak();
            shieldBreaking = true;
            SetShieldVisualVisible(true);
            shieldBreakRoutine = StartCoroutine(ShieldBreakAnimation());
        }

        private System.Collections.IEnumerator ShieldBreakAnimation()
        {
            const float duration = 0.32f;
            float time = 0f;
            Vector3 startScale = shieldVisualRoot.localScale;
            while (time < duration)
            {
                time += Time.deltaTime;
                float progress = Mathf.Clamp01(time / duration);
                float alpha = 1f - progress;
                shieldVisualRoot.localScale = startScale * Mathf.Lerp(1f, 1.7f, progress);
                if (shieldRingRenderer != null)
                    shieldRingRenderer.color = new Color(0.65f, 0.95f, 1f, 0.85f * alpha);
                if (shieldGlowRenderer != null)
                    shieldGlowRenderer.color = new Color(0.35f, 0.88f, 1f, 0.3f * alpha);
                if (shieldGlassRenderer != null)
                    shieldGlassRenderer.color = new Color(1f, 1f, 1f, alpha);
                if (shieldHighlightRenderer != null)
                    shieldHighlightRenderer.color = new Color(1f, 1f, 1f, alpha);
                if (shieldBubbleRenderers != null)
                {
                    for (int i = 0; i < shieldBubbleRenderers.Length; i++)
                    {
                        if (shieldBubbleRenderers[i] == null)
                            continue;
                        Color bubbleColor = shieldBubbleRenderers[i].color;
                        bubbleColor.a *= alpha;
                        shieldBubbleRenderers[i].color = bubbleColor;
                    }
                }
                yield return null;
            }

            shieldBreaking = false;
            shieldBreakRoutine = null;
            RestoreShieldColors();
            SetShieldVisualVisible(false);
        }

        private void StopShieldBreak()
        {
            if (shieldBreakRoutine != null)
                StopCoroutine(shieldBreakRoutine);
            shieldBreakRoutine = null;
            shieldBreaking = false;
            RestoreShieldColors();
        }

        private void RestoreShieldColors()
        {
            if (shieldRingRenderer != null)
                shieldRingRenderer.color = Color.white;
            if (shieldGlowRenderer != null)
                shieldGlowRenderer.color = new Color(0.38f, 0.86f, 1f, 0.28f);
            if (shieldGlassRenderer != null)
                shieldGlassRenderer.color = Color.white;
            if (shieldHighlightRenderer != null)
                shieldHighlightRenderer.color = Color.white;
        }

        private bool TryStompEnemy(Enemy enemy, Collider2D enemyCollider)
        {
            if (enemyCollider == null || !IsFootContacting(enemyCollider.bounds))
                return false;

            return enemy.TryStomp(enemyCollider, this);
        }

        internal bool IsFootContacting(Bounds targetBounds)
        {
            if (bodyCollider == null)
                return false;

            Bounds playerBounds = bodyCollider.bounds;
            float tolerance = config != null ? config.stompHeightTolerance : 0.2f;
            bool horizontallyOverlapping = playerBounds.max.x >= targetBounds.min.x && playerBounds.min.x <= targetBounds.max.x;
            bool aboveTarget = playerBounds.center.y >= targetBounds.center.y;
            float footGap = playerBounds.min.y - targetBounds.max.y;
            bool feetTouchingTop = footGap >= -tolerance && footGap <= tolerance;
            return horizontallyOverlapping && aboveTarget && feetTouchingTop;
        }

        internal bool IsSupportedBy(Collider2D supportCollider)
        {
            if (supportCollider == null || body == null || bodyCollider == null)
                return false;

            Bounds playerBounds = bodyCollider.bounds;
            Bounds supportBounds = supportCollider.bounds;
            float tolerance = config != null ? config.stompHeightTolerance : 0.2f;
            bool horizontallyOverlapping = playerBounds.max.x >= supportBounds.min.x &&
                playerBounds.min.x <= supportBounds.max.x;
            bool aboveSupport = playerBounds.center.y >= supportBounds.center.y;
            bool feetAboveSurfaceCenter = playerBounds.min.y >= supportBounds.center.y - tolerance * 0.25f;
            return horizontallyOverlapping && aboveSupport && feetAboveSurfaceCenter;
        }

        internal bool IsBackContacting(Bounds targetBounds)
        {
            if (bodyCollider == null)
                return false;

            Bounds playerBounds = bodyCollider.bounds;
            float rearSafeEdge = playerBounds.center.x + playerBounds.extents.x * 0.12f;
            return targetBounds.center.x <= rearSafeEdge;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 point = groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.55f;
            Gizmos.DrawWireSphere(point, groundCheckRadius);
        }
    }
}
