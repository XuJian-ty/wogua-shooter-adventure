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

        private Coroutine loop;
        private Coroutine tutorialLoop;
        private TutorialChallenge tutorialChallenge;
        private readonly List<Enemy> tutorialEnemies = new List<Enemy>();
        private bool tutorialMode;

        private void Start()
        {
            loop = StartCoroutine(SpawnLoop());
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

        public void StartTutorialGhostChallenge()
        {
            StartTutorialChallenge(TutorialChallenge.Ghost);
        }

        public void StartTutorialSkeletonChallenge()
        {
            StartTutorialChallenge(TutorialChallenge.SkeletonPair);
        }

        public void EndTutorialMode()
        {
            tutorialMode = false;
            StopTutorialChallenge();
        }

        public void ClearTutorialChallenge(Enemy completedTarget = null)
        {
            StopTutorialChallenge(completedTarget);
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
            return luaRules != null
                ? luaRules.GetEnemySpawnInterval(time, config.enemyStartInterval, config.enemyIntervalAcceleration, config.enemyMinInterval, fallback)
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
            // Early runs stay readable; the more combinatorial modifiers appear
            // often enough to encourage experimentation without flooding slots.
            float roll = Random.value;
            if (roll < 0.2f)
                return SpellType.MachineGun;
            if (roll < 0.38f)
                return SpellType.CornCannon;
            if (roll < 0.58f)
                return SpellType.TripleShot;
            if (roll < 0.75f)
                return SpellType.Repeater;
            if (roll < 0.9f)
                return SpellType.DualMuzzle;
            return SpellType.WaveFlight;
        }
    }
}
