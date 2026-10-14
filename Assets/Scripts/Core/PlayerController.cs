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
        private GameConfig config;
        private int jumpCount;
        private float hurtCooldown;
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

        public bool IsGliding => isGliding;
        public event Action<int> Jumped;
        public event Action GlideStarted;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            if (body != null)
                body.constraints |= RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
            bodyCollider = GetComponent<Collider2D>();
            config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            solidGroundFilter = new ContactFilter2D { useTriggers = false };
            baseGravityScale = body != null ? body.gravityScale : 3.2f;
            visualBaseScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
            visualBasePosition = visualRoot != null ? visualRoot.localPosition : Vector3.zero;
            visualBaseRotation = visualRoot != null ? visualRoot.localRotation : Quaternion.identity;
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

            if (!tutorialInputEnabled)
            {
                ClearPointerInput();
                return;
            }

            if (hurtCooldown > 0f)
                hurtCooldown -= Time.deltaTime;

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
            if (body != null)
            {
                body.gravityScale = baseGravityScale;
                body.linearVelocity = new Vector2(body.linearVelocity.x, 0f);
            }
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
                visualRoot.localScale = new Vector3(
                    visualBaseScale.x * (1f - stretch * 0.45f),
                    visualBaseScale.y * (1f + stretch),
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
            visualRoot.localScale = Vector3.SmoothDamp(visualRoot.localScale, targetScale, ref visualScaleVelocity, 1f / Mathf.Max(1f, ease));

            Vector3 targetPosition = visualBasePosition + (grounded ? Vector3.up * bounce : Vector3.zero);
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
            if (enemy != null)
                enemy.TryStomp(other, this);
        }

        private void HandleEnemyCollision(Collider2D other)
        {
            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.IsAlive)
                return;

            if (TryStompEnemy(enemy, other))
                return;

            if (enemy.IsTutorialTarget)
                return;

            if (hurtCooldown > 0f)
                return;

            Collider2D playerCollider = bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
            ColliderDistance2D distance = playerCollider != null ? Physics2D.Distance(playerCollider, other) : default;
            if (playerCollider != null && (!distance.isOverlapped || distance.distance > -enemyHitMinOverlap))
                return;

            hurtCooldown = config != null ? config.playerInvincibleTime : 1.2f;
            GameManager.Instance?.PlayerHit();
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

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 point = groundCheck != null ? groundCheck.position : transform.position + Vector3.down * 0.55f;
            Gizmos.DrawWireSphere(point, groundCheckRadius);
        }
    }
}
