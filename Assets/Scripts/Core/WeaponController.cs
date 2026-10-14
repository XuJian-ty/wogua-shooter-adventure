using System.Collections;
using UnityEngine;
using WoguaShooter.Config;

namespace WoguaShooter.Core
{
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private Projectile peaProjectilePrefab;
        [SerializeField] private Projectile cornProjectilePrefab;
        [SerializeField] private SpriteRenderer weaponMouth;
        [SerializeField] private Sprite normalMouth;
        [SerializeField] private Sprite machineGunMouth;
        [SerializeField] private Sprite cornMouth;

        private struct EquippedMuzzle
        {
            public WeaponType type;
            public float remaining;
            public int order;

            public bool IsActive => type != WeaponType.Pea && remaining > 0f;
        }

        private GameConfig config;
        private EquippedMuzzle firstMuzzle;
        private EquippedMuzzle secondMuzzle;
        private int nextMuzzleOrder;
        private float cooldown;
        private float dualMuzzleTime;
        private float tripleShotTime;
        private float repeaterTime;
        private float waveFlightTime;
        private SpriteRenderer secondaryWeaponMouth;
        private Vector3 mouthBaseScale;
        private Vector3 mouthBasePosition;
        private Quaternion mouthBaseRotation;
        private Color mouthBaseColor = Color.white;
        private int mouthBaseSortingOrder;
        private Vector3 muzzleBasePosition;

        public WeaponType CurrentType => firstMuzzle.IsActive ? firstMuzzle.type : WeaponType.Pea;
        public WeaponType SecondaryType => secondMuzzle.IsActive ? secondMuzzle.type : WeaponType.Pea;
        public float PoweredTime => firstMuzzle.IsActive ? firstMuzzle.remaining : 0f;
        public float SecondaryPoweredTime => secondMuzzle.IsActive ? secondMuzzle.remaining : 0f;
        public bool HasSecondMuzzle => firstMuzzle.IsActive && secondMuzzle.IsActive;
        public bool DualMuzzleActive => dualMuzzleTime > 0f;
        public float DualMuzzleTime => dualMuzzleTime;
        public bool TripleShotActive => tripleShotTime > 0f;
        public bool RepeaterActive => repeaterTime > 0f;
        public float TripleShotTime => tripleShotTime;
        public float RepeaterTime => repeaterTime;
        public bool WaveFlightActive => waveFlightTime > 0f;
        public float WaveFlightTime => waveFlightTime;

        private void Awake()
        {
            config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (weaponMouth != null)
            {
                mouthBaseScale = weaponMouth.transform.localScale;
                mouthBasePosition = weaponMouth.transform.localPosition;
                mouthBaseRotation = weaponMouth.transform.localRotation;
                mouthBaseColor = weaponMouth.color;
                mouthBaseSortingOrder = weaponMouth.sortingOrder;

                // The original prefab only needs one mouth reference. Clone it
                // at runtime so dual-muzzle visuals do not require prefab surgery.
                secondaryWeaponMouth = Instantiate(weaponMouth, weaponMouth.transform.parent);
                secondaryWeaponMouth.name = "SecondaryWeaponMouth";
                secondaryWeaponMouth.transform.localScale = mouthBaseScale;
                secondaryWeaponMouth.gameObject.SetActive(false);
            }
            if (muzzle != null)
                muzzleBasePosition = muzzle.localPosition;
            ApplyMouths();
        }

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return;

            bool mouthChanged = TickMuzzles(Time.deltaTime);
            dualMuzzleTime = Mathf.Max(0f, dualMuzzleTime - Time.deltaTime);
            tripleShotTime = Mathf.Max(0f, tripleShotTime - Time.deltaTime);
            repeaterTime = Mathf.Max(0f, repeaterTime - Time.deltaTime);
            waveFlightTime = Mathf.Max(0f, waveFlightTime - Time.deltaTime);
            if (mouthChanged)
                ApplyMouths();

            cooldown -= Time.deltaTime;
            if (cooldown <= 0f)
            {
                FireSequence();
                cooldown = GetFireInterval();
            }
        }

        public void ActivateWeapon(WeaponType type)
        {
            EquipMuzzle(type);
        }

        public void ActivateSpell(SpellType type)
        {
            float duration = config != null ? config.modifierDuration : 10f;
            switch (type)
            {
                case SpellType.MachineGun:
                    EquipMuzzle(WeaponType.MachineGun);
                    break;
                case SpellType.CornCannon:
                    EquipMuzzle(WeaponType.CornCannon);
                    break;
                case SpellType.DualMuzzle:
                    dualMuzzleTime = Mathf.Max(dualMuzzleTime, duration);
                    break;
                case SpellType.TripleShot:
                    tripleShotTime = Mathf.Max(tripleShotTime, duration);
                    break;
                case SpellType.Repeater:
                    repeaterTime = Mathf.Max(repeaterTime, duration);
                    break;
                case SpellType.WaveFlight:
                    waveFlightTime = Mathf.Max(waveFlightTime, config != null ? config.waveFlightDuration : 8f);
                    break;
            }
        }

        public void ResetWeaponState()
        {
            StopAllCoroutines();
            firstMuzzle = default;
            secondMuzzle = default;
            nextMuzzleOrder = 0;
            cooldown = 0f;
            dualMuzzleTime = 0f;
            tripleShotTime = 0f;
            repeaterTime = 0f;
            waveFlightTime = 0f;

            if (muzzle != null)
                muzzle.localPosition = muzzleBasePosition;
            if (weaponMouth != null)
            {
                weaponMouth.transform.localPosition = mouthBasePosition;
                weaponMouth.transform.localRotation = mouthBaseRotation;
                weaponMouth.transform.localScale = mouthBaseScale;
                weaponMouth.color = mouthBaseColor;
                weaponMouth.sortingOrder = mouthBaseSortingOrder;
            }
            if (secondaryWeaponMouth != null)
            {
                secondaryWeaponMouth.transform.localPosition = mouthBasePosition;
                secondaryWeaponMouth.transform.localRotation = mouthBaseRotation;
                secondaryWeaponMouth.transform.localScale = mouthBaseScale;
                secondaryWeaponMouth.color = mouthBaseColor;
                secondaryWeaponMouth.sortingOrder = mouthBaseSortingOrder + 1;
                secondaryWeaponMouth.gameObject.SetActive(false);
            }
            ApplyMouths();
        }

        private void EquipMuzzle(WeaponType type)
        {
            if (type == WeaponType.Pea)
                return;

            PromoteMuzzles();
            float duration = GetMuzzleDuration(type);
            if (firstMuzzle.IsActive && firstMuzzle.type == type)
            {
                firstMuzzle.remaining = duration;
                cooldown = 0f;
                ApplyMouths();
                return;
            }

            if (secondMuzzle.IsActive && secondMuzzle.type == type)
            {
                secondMuzzle.remaining = duration;
                cooldown = 0f;
                ApplyMouths();
                return;
            }

            EquippedMuzzle newMuzzle = new EquippedMuzzle
            {
                type = type,
                remaining = duration,
                order = ++nextMuzzleOrder
            };

            if (dualMuzzleTime > 0f)
            {
                if (!firstMuzzle.IsActive)
                    firstMuzzle = newMuzzle;
                else if (!secondMuzzle.IsActive)
                    secondMuzzle = newMuzzle;
                else if (firstMuzzle.order <= secondMuzzle.order)
                {
                    firstMuzzle = secondMuzzle;
                    secondMuzzle = newMuzzle;
                }
                else
                {
                    secondMuzzle = firstMuzzle;
                    firstMuzzle = newMuzzle;
                }
            }
            else
            {
                firstMuzzle = newMuzzle;
                secondMuzzle = default;
            }

            cooldown = 0f;
            ApplyMouths();
        }

        private bool TickMuzzles(float deltaTime)
        {
            bool changed = false;
            if (firstMuzzle.IsActive)
            {
                firstMuzzle.remaining -= deltaTime;
                if (firstMuzzle.remaining <= 0f)
                {
                    firstMuzzle = default;
                    changed = true;
                }
            }
            if (secondMuzzle.IsActive)
            {
                secondMuzzle.remaining -= deltaTime;
                if (secondMuzzle.remaining <= 0f)
                {
                    secondMuzzle = default;
                    changed = true;
                }
            }

            if (PromoteMuzzles())
                changed = true;
            return changed;
        }

        private bool PromoteMuzzles()
        {
            if (firstMuzzle.IsActive || !secondMuzzle.IsActive)
                return false;

            firstMuzzle = secondMuzzle;
            secondMuzzle = default;
            return true;
        }

        private float GetMuzzleDuration(WeaponType type)
        {
            WeaponConfig weapon = config != null ? config.GetWeapon(type) : null;
            if (weapon != null && weapon.duration > 0f)
                return weapon.duration;
            return config != null ? config.modifierDuration : 10f;
        }

        private float GetFireInterval()
        {
            WeaponConfig first = config != null ? config.GetWeapon(CurrentType) : null;
            float interval = first != null ? first.fireInterval : 0.75f;
            if (HasSecondMuzzle)
            {
                WeaponConfig second = config != null ? config.GetWeapon(SecondaryType) : null;
                if (second != null)
                    interval = Mathf.Min(interval, second.fireInterval);
            }

            GameManager manager = GameManager.Instance;
            if (config == null || manager == null)
                return interval;

            float startSpeed = Mathf.Max(0.01f, config.backgroundStartSpeed);
            float currentSpeed = Mathf.Max(startSpeed, manager.GetBackgroundSpeed());
            return Mathf.Max(0.05f, interval * startSpeed / currentSpeed);
        }

        private void ApplyMouths()
        {
            if (weaponMouth == null)
                return;

            if (!firstMuzzle.IsActive && secondMuzzle.IsActive)
                PromoteMuzzles();

            weaponMouth.gameObject.SetActive(true);
            weaponMouth.sprite = firstMuzzle.IsActive ? GetMouthSprite(firstMuzzle.type) : normalMouth;
            weaponMouth.transform.localScale = mouthBaseScale;
            weaponMouth.color = mouthBaseColor;
            weaponMouth.sortingOrder = mouthBaseSortingOrder;

            if (HasSecondMuzzle && secondaryWeaponMouth != null)
            {
                float offset = config != null ? config.dualMuzzleOffset : 0.055f;
                float angle = config != null ? config.dualMuzzleAngle : 4f;
                weaponMouth.transform.localPosition = mouthBasePosition + Vector3.up * offset;
                weaponMouth.transform.localRotation = mouthBaseRotation * Quaternion.Euler(0f, 0f, angle);
                secondaryWeaponMouth.sprite = GetMouthSprite(secondMuzzle.type);
                secondaryWeaponMouth.transform.localPosition = mouthBasePosition + Vector3.down * offset;
                secondaryWeaponMouth.transform.localRotation = mouthBaseRotation * Quaternion.Euler(0f, 0f, -angle);
                secondaryWeaponMouth.transform.localScale = mouthBaseScale;
                secondaryWeaponMouth.color = mouthBaseColor;
                secondaryWeaponMouth.sortingOrder = mouthBaseSortingOrder + 1;
                secondaryWeaponMouth.gameObject.SetActive(true);
            }
            else if (secondaryWeaponMouth != null)
            {
                secondaryWeaponMouth.gameObject.SetActive(false);
                weaponMouth.transform.localPosition = mouthBasePosition;
                weaponMouth.transform.localRotation = mouthBaseRotation;
            }
        }

        private Sprite GetMouthSprite(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.MachineGun:
                    return machineGunMouth;
                case WeaponType.CornCannon:
                    return cornMouth;
                default:
                    return normalMouth;
            }
        }

        private void FireSequence()
        {
            if (muzzle == null)
                return;

            WeaponType firstType = CurrentType;
            WeaponType secondType = SecondaryType;
            bool hasSecond = HasSecondMuzzle;
            FireMuzzle(firstType, weaponMouth, GetProjectileOrigin(false, hasSecond));
            if (hasSecond)
                StartCoroutine(DelayedMuzzle(secondType));

            if (repeaterTime > 0f)
                StartCoroutine(RepeatSequence(firstType, secondType, hasSecond));

            StartCoroutine(Recoil());
        }

        private void FireMuzzle(WeaponType type, SpriteRenderer flashRenderer, Vector3 origin)
        {
            Projectile prefab = type == WeaponType.CornCannon ? cornProjectilePrefab : peaProjectilePrefab;
            if (prefab == null)
                return;

            WeaponConfig weapon = config != null ? config.GetWeapon(type) : null;
            FireVolley(prefab, type, weapon, origin);
            AudioManager.Instance?.PlayWeaponShot(type == WeaponType.CornCannon);
            StartCoroutine(MuzzleFlash(flashRenderer));
            if (type == WeaponType.MachineGun)
                StartCoroutine(DelayedMachineGunShot(prefab, type, weapon, flashRenderer, origin));
        }

        private void FireVolley(Projectile prefab, WeaponType type, WeaponConfig weapon, Vector3 origin)
        {
            int shotCount = tripleShotTime > 0f ? 3 : 1;
            float spread = config != null ? config.spreadAngle : 18f;
            for (int i = 0; i < shotCount; i++)
            {
                float angle = shotCount == 1 ? 0f : (i - 1) * spread;
                Vector2 direction = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                Projectile projectile = Instantiate(prefab, origin, Quaternion.identity);
                float waveAmplitude = config != null ? config.waveFlightAmplitude : 0.28f;
                float waveFrequency = config != null ? config.waveFlightFrequency : 2.4f;
                projectile.Launch(type, weapon, direction, WaveFlightActive, waveAmplitude, waveFrequency);
            }
        }

        private IEnumerator DelayedMachineGunShot(Projectile prefab, WeaponType type, WeaponConfig weapon,
            SpriteRenderer flashRenderer, Vector3 origin)
        {
            float delay = config != null ? config.machineGunBurstDelay : 0.2f;
            yield return new WaitForSeconds(delay);
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                yield break;

            FireVolley(prefab, type, weapon, origin);
            AudioManager.Instance?.PlayWeaponShot(false);
            StartCoroutine(MuzzleFlash(flashRenderer));
        }

        private Vector3 GetProjectileOrigin(bool secondary, bool dual)
        {
            if (!dual || muzzle == null)
                return muzzle != null ? muzzle.position : transform.position;

            float offset = config != null ? config.dualMuzzleOffset : 0.055f;
            return muzzle.position + (secondary ? Vector3.down : Vector3.up) * offset;
        }

        private IEnumerator DelayedMuzzle(WeaponType type)
        {
            float delay = config != null ? config.dualMuzzleDelay : 0.2f;
            yield return new WaitForSeconds(delay);
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                yield break;

            FireMuzzle(type, secondaryWeaponMouth, GetProjectileOrigin(true, true));
        }

        private IEnumerator RepeatSequence(WeaponType firstType, WeaponType secondType, bool hasSecond)
        {
            float delay = config != null ? config.repeaterDelay : 0.2f;
            yield return new WaitForSeconds(delay);
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                yield break;

            FireMuzzle(firstType, weaponMouth, GetProjectileOrigin(false, hasSecond));
            if (hasSecond)
                StartCoroutine(DelayedMuzzle(secondType));
        }

        private IEnumerator MuzzleFlash(SpriteRenderer renderer)
        {
            if (renderer == null)
                yield break;

            renderer.color = Color.white;
            renderer.transform.localScale = mouthBaseScale * 1.18f;
            yield return new WaitForSeconds(0.055f);
            if (renderer != null)
            {
                renderer.color = mouthBaseColor;
                renderer.transform.localScale = mouthBaseScale;
            }
        }

        private IEnumerator Recoil()
        {
            if (muzzle == null)
                yield break;

            Vector3 start = muzzle.localPosition;
            muzzle.localPosition = start + Vector3.left * 0.08f;
            yield return new WaitForSeconds(0.05f);
            muzzle.localPosition = start;
        }
    }
}
