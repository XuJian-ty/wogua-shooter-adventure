using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WoguaShooter.Config;
using WoguaShooter.Lua;

namespace WoguaShooter.Core
{
    public class SpawnManager : MonoBehaviour
    {
        private enum TutorialChallenge
        {
            None,
            Ghost,
            SkeletonPair
        }

        [SerializeField] private Enemy[] enemyPrefabs;
        [SerializeField] private CollectibleWeapon[] weaponPrefabs;
        [SerializeField] private LuaRuntimeBridge luaRules;
        [SerializeField] private float weaponSpawnChance = 0.13f;
        [SerializeField, Min(1f)] private float tutorialGhostInterval = 3.6f;
        [SerializeField, Min(1f)] private float tutorialSkeletonInterval = 4.2f;
        [Header("金币与地形")]
        [SerializeField] private Vector2 coinSpawnIntervalRange = new Vector2(2.6f, 4.4f);
        [SerializeField] private Vector2 hazardSpawnIntervalRange = new Vector2(7f, 11f);
        [SerializeField, Range(0f, 1f)] private float enemyCoinTrailChance = 0.68f;
        [Header("后期导弹")]
        [SerializeField, Min(0f)] private float missileStartTime = 55f;
        [SerializeField] private Vector2 missileSpawnIntervalRange = new Vector2(11f, 17f);

        private Coroutine loop;
        private Coroutine tutorialLoop;
        private TutorialChallenge tutorialChallenge;
        private readonly List<Enemy> tutorialEnemies = new List<Enemy>();
        private readonly List<CoinCollectible> tutorialCoins = new List<CoinCollectible>();
        private bool tutorialMode;
        private bool spikeSpawnedThisRound;

        private void Start()
        {
            loop = StartCoroutine(SpawnLoop());
            StartCoroutine(CoinSpawnLoop());
            StartCoroutine(HazardSpawnLoop());
            StartCoroutine(MissileSpawnLoop());
        }

        private IEnumerator CoinSpawnLoop()
        {
            yield return WaitForPlaying();
            while (true)
            {
                yield return new WaitForSeconds(RandomInterval(coinSpawnIntervalRange, 2.6f, 4.4f));
                if (CanSpawnNormalGameplay())
                    SpawnCoinPattern();
            }
        }

        private IEnumerator HazardSpawnLoop()
        {
            yield return WaitForPlaying();
            yield return new WaitForSeconds(3f);
            while (true)
            {
                int levelId = GameManager.Instance != null ? GameManager.Instance.CurrentLevelId : 1;
                float interval = luaRules != null
                    ? luaRules.GetHazardSpawnInterval(hazardSpawnIntervalRange, levelId)
                    : RandomInterval(hazardSpawnIntervalRange, 5.5f, 8f);
                yield return new WaitForSeconds(interval);
                if (CanSpawnNormalGameplay())
                    SpawnTerrainHazard();
            }
        }

        private IEnumerator MissileSpawnLoop()
        {
            yield return WaitForPlaying();
            bool waitingForRound = true;
            float nextVolleyTime = 0f;
            while (true)
            {
                GameManager manager = GameManager.Instance;
                if (!CanSpawnNormalGameplay() || manager == null)
                {
                    waitingForRound = true;
                    yield return new WaitForSeconds(0.35f);
                    continue;
                }

                int levelId = manager.CurrentLevelId;
                if (waitingForRound)
                {
                    waitingForRound = false;
                    nextVolleyTime = manager.ElapsedTime + GetMissileInitialDelay(levelId);
                }

                if (manager.ElapsedTime < nextVolleyTime)
                {
                    yield return new WaitForSeconds(0.35f);
                    continue;
                }

                int volleyCount = GetMissileVolleyCount(levelId);
                for (int i = 0; i < volleyCount; i++)
                {
                    if (!CanSpawnNormalGameplay())
                        break;
                    SpawnMissile();
                    if (i < volleyCount - 1)
                        yield return new WaitForSeconds(GetMissileVolleyDelay(levelId));
                }

                manager = GameManager.Instance;
                float elapsedTime = manager != null ? manager.ElapsedTime : 0f;
                nextVolleyTime = elapsedTime + GetMissileSpawnInterval(levelId);
            }
        }

        private static IEnumerator WaitForPlaying()
        {
            while (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                yield return null;
        }

        private bool CanSpawnNormalGameplay()
        {
            return !tutorialMode && GameManager.Instance != null && GameManager.Instance.State == GameState.Playing;
        }

        private IEnumerator SpawnLoop()
        {
            while (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                yield return null;

            while (true)
            {
                float interval = GetSpawnInterval();
                yield return new WaitForSeconds(interval);

                if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                    continue;

                if (tutorialMode)
                    continue;

                SpawnEnemyGroup();
                if (Random.value < weaponSpawnChance)
                    SpawnWeapon();
            }
        }

        public void BeginTutorialMode()
        {
            tutorialMode = true;
            StopTutorialChallenge();
        }

        public void PrepareForLevel()
        {
            spikeSpawnedThisRound = false;
        }

        public void StartTutorialGhostChallenge()
        {
            StartTutorialChallenge(TutorialChallenge.Ghost);
        }

        public void StartTutorialCoinChallenge()
        {
            if (!tutorialMode)
                return;

            ClearTutorialCoins();
            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (config == null)
                return;

            float ground = GetGroundSurfaceY(config);
            float startX = config.enemySpawnX - 1.4f;
            const int coinCount = 6;
            for (int i = 0; i < coinCount; i++)
            {
                float progress = i / (coinCount - 1f);
                float y = ground + 0.38f + Mathf.Sin(progress * Mathf.PI) * 0.42f;
                CoinCollectible coin = CoinCollectible.Create(new Vector3(startX + i * 0.4f, y, 0f));
                coin.MarkTutorialTarget();
                tutorialCoins.Add(coin);
            }
        }

        public void StartTutorialSkeletonChallenge()
        {
            StartTutorialChallenge(TutorialChallenge.SkeletonPair);
        }

        public void EndTutorialMode()
        {
            tutorialMode = false;
            StopTutorialChallenge();
            ClearTutorialCoins();
        }

        public void ClearTutorialChallenge(Enemy completedTarget = null)
        {
            StopTutorialChallenge(completedTarget);
        }

        public void ClearTutorialCoins(CoinCollectible completedTarget = null)
        {
            for (int i = 0; i < tutorialCoins.Count; i++)
            {
                CoinCollectible coin = tutorialCoins[i];
                if (coin == null || coin == completedTarget)
                    continue;

                coin.gameObject.SetActive(false);
                Destroy(coin.gameObject);
            }
            tutorialCoins.Clear();
        }

        private void StartTutorialChallenge(TutorialChallenge challenge)
        {
            if (!tutorialMode)
                return;

            StopTutorialChallenge();
            tutorialChallenge = challenge;
            tutorialLoop = StartCoroutine(TutorialChallengeLoop(challenge));
        }

        private IEnumerator TutorialChallengeLoop(TutorialChallenge challenge)
        {
            while (tutorialMode && tutorialChallenge == challenge)
            {
                if (GameManager.Instance != null && GameManager.Instance.State == GameState.Playing)
                {
                    if (challenge == TutorialChallenge.Ghost)
                        SpawnTutorialGhost();
                    else if (challenge == TutorialChallenge.SkeletonPair)
                        SpawnTutorialSkeletonPair();
                }

                float delay = challenge == TutorialChallenge.Ghost
                    ? tutorialGhostInterval
                    : tutorialSkeletonInterval;
                yield return new WaitForSeconds(delay);
            }
        }

        private void SpawnTutorialGhost()
        {
            Enemy prefab = FindEnemyPrefab(true);
            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (prefab == null || config == null)
                return;

            float y = Mathf.Min(config.enemySpawnYRange.y, config.groundY + 2.15f);
            Enemy enemy = Instantiate(prefab, new Vector3(config.enemySpawnX, y, 0f), Quaternion.identity);
            enemy.MarkTutorialTarget(false);
            tutorialEnemies.Add(enemy);
        }

        private void SpawnTutorialSkeletonPair()
        {
            Enemy prefab = FindEnemyPrefab(false);
            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (prefab == null || config == null)
                return;

            const float spacing = 0.82f;
            for (int i = 0; i < 2; i++)
            {
                Enemy enemy = Instantiate(
                    prefab,
                    new Vector3(config.enemySpawnX + i * spacing, config.groundY, 0f),
                    Quaternion.identity);
                enemy.MarkTutorialTarget(true);
                tutorialEnemies.Add(enemy);
            }
        }

        private Enemy FindEnemyPrefab(bool floating)
        {
            if (enemyPrefabs == null)
                return null;

            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                Enemy prefab = enemyPrefabs[i];
                if (prefab != null && prefab.IsFloating == floating)
                    return prefab;
            }

            return null;
        }

        private void StopTutorialChallenge(Enemy completedTarget = null)
        {
            if (tutorialLoop != null)
            {
                StopCoroutine(tutorialLoop);
                tutorialLoop = null;
            }

            tutorialChallenge = TutorialChallenge.None;
            for (int i = 0; i < tutorialEnemies.Count; i++)
            {
                Enemy enemy = tutorialEnemies[i];
                if (enemy == null)
                    continue;
                if (enemy == completedTarget)
                    continue;

                enemy.gameObject.SetActive(false);
                Destroy(enemy.gameObject);
            }
            tutorialEnemies.Clear();
        }

        private float GetSpawnInterval()
        {
            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            float time = GameManager.Instance != null ? GameManager.Instance.ElapsedTime : 0f;
            float fallback = config == null ? 2.4f : Mathf.Max(config.enemyMinInterval, config.enemyStartInterval - time * config.enemyIntervalAcceleration);
            int levelId = GameManager.Instance != null ? GameManager.Instance.CurrentLevelId : 1;
            return luaRules != null
                ? luaRules.GetEnemySpawnInterval(time, config.enemyStartInterval, config.enemyIntervalAcceleration, config.enemyMinInterval, levelId, fallback)
                : fallback;
        }

        private void SpawnEnemyGroup()
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0)
                return;

            GameConfig config = GameManager.Instance.Config;
            if (config == null)
                return;
            int count = Random.value > 0.78f ? 2 : 1;
            if (Random.value < enemyCoinTrailChance)
                SpawnCoinsBeforeEnemy(config.enemySpawnX, config);
            for (int i = 0; i < count; i++)
            {
                Enemy prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
                float y;
                if (prefab != null && prefab.IsFloating)
                {
                    float minimumFloatingY = config.groundY + 0.7f;
                    float lowerBound = Mathf.Max(config.enemySpawnYRange.x, minimumFloatingY);
                    float upperBound = Mathf.Max(lowerBound, config.enemySpawnYRange.y);
                    y = Random.Range(lowerBound, upperBound);
                }
                else
                {
                    y = config.groundY;
                }
                Instantiate(prefab, new Vector3(config.enemySpawnX + i * 1.15f, y, 0f), Quaternion.identity);
            }
        }

        private void SpawnWeapon()
        {
            if (weaponPrefabs == null || weaponPrefabs.Length == 0)
                return;

            GameConfig config = GameManager.Instance.Config;
            if (config == null)
                return;
            CollectibleWeapon prefab = weaponPrefabs[Random.Range(0, weaponPrefabs.Length)];
            float y = Random.Range(0.85f, 2.1f);
            CollectibleWeapon pickup = Instantiate(prefab, new Vector3(config.enemySpawnX, y, 0f), Quaternion.identity);
            pickup.SetSpellType(GetRandomSpellType());
        }

        private SpellType GetRandomSpellType()
        {
            float roll = Random.value;
            if (roll < 0.13f)
                return SpellType.MachineGun;
            if (roll < 0.25f)
                return SpellType.CornCannon;
            if (roll < 0.39f)
                return SpellType.TripleShot;
            if (roll < 0.52f)
                return SpellType.Repeater;
            if (roll < 0.64f)
                return SpellType.DualMuzzle;
            if (roll < 0.74f)
                return SpellType.WaveFlight;
            if (roll < 0.84f)
                return SpellType.Magnet;
            if (roll < 0.92f)
                return SpellType.Shield;
            return SpellType.Growth;
        }

        private void SpawnCoinPattern()
        {
            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (config == null)
                return;

            float startX = config.enemySpawnX + Random.Range(-0.25f, 0.65f);
            float ground = GetGroundSurfaceY(config);
            int pattern = Random.Range(0, 4);
            if (pattern == 0)
            {
                for (int i = 0; i < 6; i++)
                    SpawnCoin(startX + i * 0.34f, ground + 0.34f);
            }
            else if (pattern == 1)
            {
                for (int i = 0; i < 7; i++)
                {
                    float progress = i / 6f;
                    float y = ground + 0.35f + Mathf.Sin(progress * Mathf.PI) * 1.05f;
                    SpawnCoin(startX + i * 0.34f, y);
                }
            }
            else if (pattern == 2)
            {
                bool ascending = Random.value > 0.5f;
                for (int i = 0; i < 6; i++)
                {
                    int step = ascending ? i : 5 - i;
                    SpawnCoin(startX + i * 0.34f, ground + 0.34f + step * 0.2f);
                }
            }
            else
            {
                Vector2[] diamond =
                {
                    new Vector2(0f, 0.75f), new Vector2(0.35f, 1.08f),
                    new Vector2(0.7f, 1.38f), new Vector2(1.05f, 1.08f),
                    new Vector2(1.4f, 0.75f), new Vector2(1.05f, 0.44f),
                    new Vector2(0.7f, 0.18f), new Vector2(0.35f, 0.44f)
                };
                for (int i = 0; i < diamond.Length; i++)
                    SpawnCoin(startX + diamond[i].x, ground + diamond[i].y);
            }
        }

        private static void SpawnCoinsBeforeEnemy(float enemyX, GameConfig config)
        {
            float ground = GetGroundSurfaceY(config);
            float startX = enemyX - 1.65f;
            for (int i = 0; i < 4; i++)
                SpawnCoin(startX + i * 0.31f, ground + 0.34f + Mathf.Sin(i / 3f * Mathf.PI) * 0.22f);
        }

        private void SpawnTerrainHazard()
        {
            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (config == null)
                return;

            float ground = GetGroundSurfaceY(config);
            int levelId = GameManager.Instance != null ? GameManager.Instance.CurrentLevelId : 1;
            float spikeChance = luaRules != null ? luaRules.GetSpikeChance(levelId) : 0.58f;
            float startX = config.enemySpawnX + Random.Range(0f, 0.8f);
            if (!spikeSpawnedThisRound || Random.value < spikeChance)
            {
                int groupCount = luaRules != null
                    ? luaRules.GetSpikeGroupCount(levelId)
                    : Random.Range(1, 4);
                for (int i = 0; i < groupCount; i++)
                {
                    WorldHazard.Create(
                        WorldHazardType.Spikes,
                        new Vector3(startX + i * 0.82f, ground, 0f));
                }
                spikeSpawnedThisRound = true;
                return;
            }

            WorldHazard.Create(WorldHazardType.FirePit, new Vector3(startX, ground, 0f));
        }

        private float GetMissileInitialDelay(int levelId)
        {
            return luaRules != null
                ? luaRules.GetMissileInitialDelay(levelId)
                : Random.Range(4f, 7f);
        }

        private float GetMissileSpawnInterval(int levelId)
        {
            return luaRules != null
                ? luaRules.GetMissileSpawnInterval(levelId)
                : Random.Range(9f, 13f);
        }

        private float GetMissileVolleyDelay(int levelId)
        {
            return luaRules != null
                ? luaRules.GetMissileVolleyDelay(levelId)
                : Random.Range(0.55f, 0.9f);
        }

        private int GetMissileVolleyCount(int levelId)
        {
            if (luaRules != null)
                return luaRules.GetMissileVolleyCount(levelId);

            float value = Random.value;
            return value < 0.5f ? 1 : value < 0.82f ? 2 : 3;
        }

        private void SpawnMissile()
        {
            GameConfig config = GameManager.Instance != null ? GameManager.Instance.Config : null;
            if (config == null)
                return;

            float ground = GetGroundSurfaceY(config);
            float y = Random.Range(ground + 0.45f, ground + 2.35f);
            WorldHazard.Create(WorldHazardType.Missile,
                new Vector3(config.enemySpawnX + Random.Range(0.6f, 1.5f), y, 0f));
        }

        private static void SpawnCoin(float x, float y)
        {
            CoinCollectible.Create(new Vector3(x, y, 0f));
        }

        private static float GetGroundSurfaceY(GameConfig config)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                RaycastHit2D hit = Physics2D.Raycast(
                    new Vector2(config.enemySpawnX, 20f),
                    Vector2.down,
                    40f,
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

            return config.groundY;
        }

        private static float RandomInterval(Vector2 configuredRange, float fallbackMin, float fallbackMax)
        {
            float minimum = configuredRange.x > 0f ? configuredRange.x : fallbackMin;
            float maximum = configuredRange.y >= minimum ? configuredRange.y : fallbackMax;
            return Random.Range(minimum, Mathf.Max(minimum, maximum));
        }
    }
}
