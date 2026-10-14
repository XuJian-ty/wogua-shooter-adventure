using UnityEngine;
using WoguaShooter.Config;

namespace WoguaShooter.Core
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float maxLifetime = 4f;
        [SerializeField] private Sprite[] explosionFrames;

        private WeaponType weaponType;
        private float speed;
        private float explosionRadius;
        private float lifetime;
        private bool hasHit;
        private Vector2 direction = Vector2.right;
        private bool waveTrajectory;
        private Vector2 waveStartPosition;
        private Vector2 wavePerpendicular;
        private float waveDistance;
        private float waveTime;
        private float waveAmplitude;
        private float waveFrequency;

        private void Awake()
        {
            GameManager.RegisterLevelObject(gameObject);
        }

        public void Launch(WeaponType type, WeaponConfig config)
        {
            Launch(type, config, Vector2.right);
        }

        public void Launch(WeaponType type, WeaponConfig config, Vector2 launchDirection)
        {
            Launch(type, config, launchDirection, false, 0f, 0f);
        }

        public void Launch(WeaponType type, WeaponConfig config, Vector2 launchDirection,
            bool useWaveTrajectory, float trajectoryAmplitude, float trajectoryFrequency)
        {
            weaponType = type;
            speed = config != null ? config.projectileSpeed : 12f;
            explosionRadius = config != null ? config.explosionRadius : 0f;
            lifetime = maxLifetime;
            hasHit = false;
            direction = launchDirection.sqrMagnitude > 0.001f ? launchDirection.normalized : Vector2.right;
            waveTrajectory = useWaveTrajectory && trajectoryAmplitude > 0f && trajectoryFrequency > 0f;
            waveStartPosition = transform.position;
            wavePerpendicular = new Vector2(-direction.y, direction.x);
            waveDistance = 0f;
            waveTime = 0f;
            waveAmplitude = Mathf.Max(0f, trajectoryAmplitude);
            waveFrequency = Mathf.Max(0f, trajectoryFrequency);
            transform.right = direction;
        }

        private void Update()
        {
            if (waveTrajectory)
            {
                waveTime += Time.deltaTime;
                waveDistance += speed * Time.deltaTime;
                float phase = waveTime * waveFrequency * Mathf.PI * 2f;
                Vector2 center = waveStartPosition + direction * waveDistance;
                transform.position = center + wavePerpendicular * (Mathf.Sin(phase) * waveAmplitude);
            }
            else
            {
                transform.position += (Vector3)(direction * (speed * Time.deltaTime));
            }
            GameManager.RequestPhysicsTransformSync();
            lifetime -= Time.deltaTime;
            if (lifetime <= 0f)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryHitEnemy(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryHitEnemy(other);
        }

        private void TryHitEnemy(Collider2D other)
        {
            if (hasHit)
                return;

            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy == null || !enemy.IsAlive || !enemy.IsOnScreen || !enemy.CanBeHitByProjectile)
                return;

            hasHit = true;
            if (weaponType == WeaponType.CornCannon)
                Explode();
            else
                enemy.Kill();

            Destroy(gameObject);
        }

        private void Explode()
        {
            AudioManager.Instance?.PlayCornExplosion();
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, explosionRadius);
            for (int i = 0; i < colliders.Length; i++)
            {
                Enemy enemy = colliders[i].GetComponentInParent<Enemy>();
                if (enemy != null && enemy.IsAlive && enemy.IsOnScreen && enemy.CanBeHitByProjectile)
                    enemy.Kill();
            }

            float effectDuration = GameManager.Instance != null && GameManager.Instance.Config != null
                ? GameManager.Instance.Config.cornExplosionEffectDuration : 0.5f;
            CombatVfx.PlayCornExplosion(transform.position, explosionRadius, effectDuration, explosionFrames);
        }

        private void OnDrawGizmosSelected()
        {
            if (weaponType != WeaponType.CornCannon)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }

        private void OnDestroy()
        {
            GameManager.UnregisterLevelObject(gameObject);
        }
    }
}
