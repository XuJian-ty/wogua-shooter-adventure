using UnityEngine;
using WoguaShooter.Config;
using WoguaShooter.UI;

namespace WoguaShooter.Core
{
    public class CollectibleWeapon : MonoBehaviour
    {
        [SerializeField] private WeaponType weaponType = WeaponType.MachineGun;
        [SerializeField] private SpellType spellType = SpellType.None;
        [SerializeField] private SpriteRenderer itemRenderer;
        [SerializeField] private float floatHeight = 0.12f;
        [SerializeField] private float floatSpeed = 2f;

        [Header("吸附效果")]
        [Tooltip("玩家进入这个范围后，道具会开始向玩家移动。")]
        [SerializeField, Min(0f)] private float pickupAttractRadius = 1.35f;
        [Tooltip("道具吸附到玩家的移动速度。")]
        [SerializeField, Min(0.1f)] private float pickupAttractSpeed = 7.5f;
        [Tooltip("吸附移动的平滑时间，数值越小越灵敏。")]
        [SerializeField, Min(0.01f)] private float pickupAttractSmoothTime = 0.12f;
        [Tooltip("道具进入这个距离后会直接拾取，无需完全碰撞。")]
        [SerializeField, Min(0.01f)] private float pickupCollectDistance = 0.18f;
        [Tooltip("吸附时道具放大的倍数。")]
        [SerializeField, Min(1f)] private float pickupAttractScale = 1.18f;
        [Tooltip("吸附时道具的旋转速度。")]
        [SerializeField, Min(0f)] private float pickupAttractRotationSpeed = 360f;
        [SerializeField] private AnimationCurve pickupAttractEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private Vector3 startPosition;
        private Vector3 baseScale;
        private Vector3 attractVelocity;
        private WeaponController targetWeapon;
        private float attractProgress;
        private float findTargetTimer;
        private bool isAttracting;
        private bool collected;

        private void Awake()
        {
            GameManager.RegisterLevelObject(gameObject);
            startPosition = transform.position;
            baseScale = transform.localScale;
            targetWeapon = FindFirstObjectByType<WeaponController>();
            if (itemRenderer == null)
            {
                Transform item = transform.Find("Item");
                itemRenderer = item != null ? item.GetComponent<SpriteRenderer>() : null;
            }
            RefreshItemIcon();
        }

        private void Update()
        {
            if (collected)
                return;

            if (targetWeapon == null)
            {
                // 避免每一帧查找场景，只在玩家还未生成时定期重试。
                findTargetTimer -= Time.deltaTime;
                if (findTargetTimer <= 0f)
                {
                    targetWeapon = FindFirstObjectByType<WeaponController>();
                    findTargetTimer = 0.5f;
                }
            }

            if (!isAttracting && targetWeapon != null)
            {
                float distance = Vector2.Distance(transform.position, targetWeapon.transform.position);
                PlayerController player = targetWeapon.GetComponent<PlayerController>();
                bool magnetAttracts = player != null && player.CanMagnetAttract(transform.position);
                if (distance <= pickupAttractRadius || magnetAttracts)
                    isAttracting = true;
            }

            if (isAttracting && targetWeapon != null)
                MoveTowardPlayer();
            else
                MoveWithWorld();

            GameManager.RequestPhysicsTransformSync();

            if (transform.position.x < -12f)
                Destroy(gameObject);
        }

        private void MoveWithWorld()
        {
            transform.position += Vector3.left * GetWorldSpeed() * Time.deltaTime;
            transform.position = new Vector3(transform.position.x, startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight, transform.position.z);
            transform.Rotate(Vector3.forward, 18f * Time.deltaTime);
            transform.localScale = baseScale;
        }

        private void MoveTowardPlayer()
        {
            Vector3 targetPosition = targetWeapon.transform.position;
            float distance = Vector2.Distance(transform.position, targetPosition);
            if (distance <= pickupCollectDistance)
            {
                Collect(targetWeapon);
                return;
            }

            attractProgress = Mathf.MoveTowards(attractProgress, 1f, Time.deltaTime * 4f);
            float eased = pickupAttractEase != null ? pickupAttractEase.Evaluate(attractProgress) : attractProgress;
            Vector3 nextPosition = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref attractVelocity,
                pickupAttractSmoothTime,
                pickupAttractSpeed * (0.65f + eased * 0.75f),
                Time.deltaTime);
            transform.position = nextPosition;
            transform.Rotate(Vector3.forward, pickupAttractRotationSpeed * Time.deltaTime);
            transform.localScale = Vector3.Lerp(baseScale, baseScale * pickupAttractScale, eased);
        }

        private float GetWorldSpeed()
        {
            return GameManager.Instance != null ? GameManager.Instance.GetBackgroundSpeed() : 3f;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryCollect(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryCollect(other);
        }

        private void TryCollect(Collider2D other)
        {
            if (collected)
                return;

            WeaponController weapon = other.GetComponentInParent<WeaponController>();
            if (weapon == null)
                return;

            Collect(weapon);
        }

        private void Collect(WeaponController weapon)
        {
            if (collected || weapon == null)
                return;

            SpellInventory inventory = SpellInventory.GetOrCreate(weapon);
            if (inventory == null)
                return;

            SpellType type = ResolveSpellType();
            SpellInventory.AddResult result = inventory.TryAddDetailed(type);
            if (result == SpellInventory.AddResult.Added)
            {
                FinishCollection();
                return;
            }

            if (result == SpellInventory.AddResult.StackFull ||
                result == SpellInventory.AddResult.NoEmptySlot)
            {
                inventory.TryUseCollectedSpell(type);
                FinishCollection();
            }
        }

        private void FinishCollection()
        {
            collected = true;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            GameManager.UnregisterLevelObject(gameObject);
        }

        public void SetSpellType(SpellType type)
        {
            spellType = type;
            if (type == SpellType.MachineGun)
                weaponType = WeaponType.MachineGun;
            else if (type == SpellType.CornCannon)
                weaponType = WeaponType.CornCannon;
            RefreshItemIcon();
        }

        private SpellType ResolveSpellType()
        {
            if (spellType != SpellType.None)
                return spellType;

            return weaponType == WeaponType.CornCannon ? SpellType.CornCannon : SpellType.MachineGun;
        }

        private void RefreshItemIcon()
        {
            if (itemRenderer == null)
                return;

            // 机枪、玉米使用获得物目录中的真实图标；其他法术使用对应的法术图标。
            Sprite icon = SpellIconFactory.Get(ResolveSpellType());
            if (icon == null)
                return;

            itemRenderer.sprite = icon;
            // Keep generated 64px icons visually comparable with the supplied item art.
            float maxDimension = Mathf.Max(icon.bounds.size.x, icon.bounds.size.y);
            if (maxDimension > 0.001f)
            {
                float targetSize = 0.32f;
                float scale = targetSize / maxDimension;
                itemRenderer.transform.localScale = Vector3.one * scale;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.75f, 0.15f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, pickupAttractRadius);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, pickupCollectDistance);
        }
    }
}
