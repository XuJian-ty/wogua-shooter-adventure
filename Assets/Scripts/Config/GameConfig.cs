using System;
using UnityEngine;

namespace WoguaShooter.Config
{
    public enum WeaponType
    {
        Pea,
        MachineGun,
        CornCannon
    }

    public enum SpellType
    {
        None,
        MachineGun,
        CornCannon,
        WaveFlight,
        TripleShot,
        Repeater,
        DualMuzzle,
        Magnet,
        Shield,
        Growth
    }

    [Serializable]
    public class WeaponConfig
    {
        public WeaponType type;
        [Min(0.05f)] public float fireInterval = 0.8f;
        [Min(1f)] public float projectileSpeed = 12f;
        [Min(0f)] public float explosionRadius = 0f;
        [Min(0f)] public float duration = 0f;
    }

    [CreateAssetMenu(fileName = "GameConfig", menuName = "Wogua Shooter/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("武器参数")]
        public WeaponConfig pea = new WeaponConfig
        {
            type = WeaponType.Pea,
            fireInterval = 0.6f,
            projectileSpeed = 12f
        };

        public WeaponConfig machineGun = new WeaponConfig
        {
            type = WeaponType.MachineGun,
            fireInterval = 0.5f,
            projectileSpeed = 15f,
            duration = 10f
        };

        public WeaponConfig cornCannon = new WeaponConfig
        {
            type = WeaponType.CornCannon,
            fireInterval = 0.5f,
            projectileSpeed = 11f,
            explosionRadius = 1.8f,
            duration = 10f
        };

        [Header("跳跃与玩家")]
        [Min(1f)] public float jumpForce = 10f;
        [Min(1f)] public float secondJumpForce = 8.5f;
        [Min(0.1f)] public float playerInvincibleTime = 1.2f;
        [Header("滑翔与踩踏")]
        [Tooltip("长按达到这个时长后开始滑翔，避免普通点按跳跃时误触发。")]
        [Min(0f)] public float glideHoldDelay = 0.12f;
        [Tooltip("滑翔时持续向下的速度，数值越小越平缓。")]
        [Min(0f)] public float glideDescentSpeed = 0.45f;
        [Tooltip("滑翔时角色视觉向下倾斜的角度。")]
        [Range(-45f, 0f)] public float glideVisualAngle = -12f;
        [Tooltip("判定脚底接触敌人顶部时允许的垂直误差。")]
        [Min(0f)] public float stompHeightTolerance = 0.2f;
        [Tooltip("跳跃时角色纵向拉伸幅度。")]
        [Min(0f)] public float playerJumpStretch = 0.1f;
        [Tooltip("落地时角色压缩幅度。")]
        [Range(0f, 0.4f)] public float playerLandingSquash = 0.14f;
        [Min(0.01f)] public float playerVisualEase = 12f;

        [Header("地面碰撞")]
        [Tooltip("地面碰撞体中心的 Y 坐标。数值越小，碰撞体越低。")]
        public float groundY = -1.28f;
        [Min(0.05f)] public float groundHeight = 0.3f;

        [Header("无限模式难度")]
        [Min(0f)] public float backgroundStartSpeed = 3f;
        [Tooltip("背景滚动速度逐渐接近的上限。")]
        [Min(0f)] public float backgroundMaxSpeed = 5.5f;
        [Tooltip("达到初始速度和最高速度中间值所需的秒数。")]
        [Min(0.1f)] public float backgroundSpeedHalfTime = 50f;
        [Min(0.1f)] public float enemyStartInterval = 2.1f;
        [Min(0f)] public float enemyIntervalAcceleration = 0.035f;
        [Min(0.2f)] public float enemyMinInterval = 0.6f;
        [Min(1f)] public float enemySpawnX = 10f;
        public Vector2 enemySpawnYRange = new Vector2(-1.25f, 1.35f);

        [Header("分数")]
        public int scorePerEnemy = 10;

        [Header("战斗特效")]
        [Min(0.01f)] public float enemyHitFlashDuration = 0.08f;
        [Min(0.01f)] public float enemyHitShakeDuration = 0.12f;
        [Min(0f)] public float enemyHitShakeStrength = 0.08f;
        [Min(0.05f)] public float enemyDeathDuration = 0.22f;
        [Min(0.05f)] public float cornExplosionEffectDuration = 0.5f;

        [Header("法术道具")]
        [Min(0.5f)] public float waveFlightDuration = 8f;
        [Tooltip("波浪轨迹子弹的上下摆动幅度。")]
        [Min(0.01f)] public float waveFlightAmplitude = 0.42f;
        [Tooltip("波浪轨迹子弹每秒完成的波形周期数。")]
        [Min(0.1f)] public float waveFlightFrequency = 2.4f;
        [Min(0.5f)] public float modifierDuration = 10f;
        [Min(0f)] public float spreadAngle = 18f;
        [Min(0.01f)] public float repeaterDelay = 0.2f;
        [Min(0.01f)] public float dualMuzzleDelay = 0.2f;
        [Min(0.01f)] public float machineGunBurstDelay = 0.2f;
        [Min(0f)] public float dualMuzzleOffset = 0.055f;
        [Min(0f)] public float dualMuzzleAngle = 4f;

        [Header("增益道具")]
        [Min(0.5f)] public float magnetDuration = 15f;
        [Tooltip("磁铁只会吸附已进入玩家前方该距离的金币和道具。")]
        [Min(0.1f)] public float magnetForwardRange = 1f;
        [Min(0.5f)] public float shieldDuration = 30f;
        [Min(0.1f)] public float shieldBreakInvincibleTime = 3f;
        [Min(0.5f)] public float growthDuration = 10f;
        [Min(1f)] public float growthScale = 2f;

        public float EvaluateBackgroundSpeed(float elapsedTime)
        {
            float startSpeed = Mathf.Max(0f, backgroundStartSpeed);
            float maxSpeed = Mathf.Max(startSpeed, backgroundMaxSpeed);
            float halfTime = Mathf.Max(0.1f, backgroundSpeedHalfTime);
            float normalizedTime = Mathf.Max(0f, elapsedTime) / halfTime;
            float squaredTime = normalizedTime * normalizedTime;
            float progress = squaredTime / (1f + squaredTime);
            return Mathf.Lerp(startSpeed, maxSpeed, progress);
        }

        public WeaponConfig GetWeapon(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.MachineGun:
                    return machineGun;
                case WeaponType.CornCannon:
                    return cornCannon;
                default:
                    return pea;
            }
        }
    }
}
