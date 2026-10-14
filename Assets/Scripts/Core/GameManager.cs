using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using WoguaShooter.Config;
using WoguaShooter.UI;

namespace WoguaShooter.Core
{
    public enum GameState
    {
        Menu,
        Playing,
        Paused,
        GameOver
    }

    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [SerializeField] private GameConfig config;
        [SerializeField] private UIController uiController;
        [SerializeField] private PlayerController player;
        [SerializeField] private SpawnManager spawnManager;
        [SerializeField] private BackgroundScroller backgroundScroller;

        public GameConfig Config => config;
        public GameState State { get; private set; } = GameState.Menu;
        public float ElapsedTime { get; private set; }
        public int Score { get; private set; }

        private bool restarting;
        private bool tutorialActive;
        private static bool physicsTransformsDirty;
        private static readonly HashSet<GameObject> levelObjects = new HashSet<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            physicsTransformsDirty = false;
            levelObjects.Clear();
            Time.timeScale = 1f;
        }

        private void LateUpdate()
        {
            if (!physicsTransformsDirty)
                return;

            Physics2D.SyncTransforms();
            physicsTransformsDirty = false;
        }

        internal static void RequestPhysicsTransformSync()
        {
            physicsTransformsDirty = true;
        }

        internal static void RegisterLevelObject(GameObject target)
        {
            if (target != null)
                levelObjects.Add(target);
        }

        internal static void UnregisterLevelObject(GameObject target)
        {
            if (target != null)
                levelObjects.Remove(target);
        }

        public static void DestroyLevelObjects()
        {
            foreach (GameObject target in levelObjects)
            {
                if (target != null)
                    Destroy(target);
            }
            levelObjects.Clear();
        }

        private void Start()
        {
            State = GameState.Menu;
            uiController?.SetScore(Score);
            uiController?.ShowMenu();
        }

        private void Update()
        {
            if (State != GameState.Playing)
                return;

            if (!tutorialActive)
            {
                ElapsedTime += Time.deltaTime;
                uiController?.SetTime(ElapsedTime);
            }
        }

        public float GetBackgroundSpeed()
        {
            return config != null ? config.EvaluateBackgroundSpeed(ElapsedTime) : 3f;
        }

        public void AddScore(int amount)
        {
            if (State != GameState.Playing)
                return;

            Score += amount;
            uiController?.SetScore(Score);
        }

        public void PauseGame()
        {
            if (State != GameState.Playing)
                return;

            State = GameState.Paused;
            Time.timeScale = 0f;
            uiController?.ShowPause();
        }

        public void PauseGameWithoutPanel()
        {
            if (State != GameState.Playing)
                return;

            State = GameState.Paused;
            Time.timeScale = 0f;
        }

        public void ResetForNewGame()
        {
            tutorialActive = false;
            Score = 0;
            ElapsedTime = 0f;
            State = GameState.Playing;
            Time.timeScale = 1f;
            SpellInventory.Instance?.ResetInventory();
            SpellInventory.Instance?.FillRandomDistinct();
            uiController?.SetScore(Score);
            uiController?.SetTime(ElapsedTime);
            uiController?.ShowPlaying();
        }

        public void SetTutorialActive(bool active)
        {
            tutorialActive = active;
        }

        public void EnterMenuState()
        {
            State = GameState.Menu;
            Time.timeScale = 1f;
            uiController?.ShowMenu();
        }

        public void ContinueGame()
        {
            if (State != GameState.Paused)
                return;

            State = GameState.Playing;
            Time.timeScale = 1f;
            uiController?.ShowPlaying();
        }

        public void PlayerHit()
        {
            if (State != GameState.Playing)
                return;

            State = GameState.GameOver;
            Time.timeScale = 0f;
            uiController?.PrepareGameOver();

            FlowUIController flow = FindFirstObjectByType<FlowUIController>(FindObjectsInactive.Include);
            LeaderboardUIController leaderboard = FindFirstObjectByType<LeaderboardUIController>(FindObjectsInactive.Include);
            int levelId = flow != null ? flow.CurrentLevelId : 1;
            LeaderboardService.RecordHighestScore(levelId, Score);
            leaderboard?.HideAll();
            StartCoroutine(ShowGameOverSequence(levelId, Score, leaderboard));
        }

        private IEnumerator ShowGameOverSequence(
            int levelId,
            int finalScore,
            LeaderboardUIController leaderboard)
        {
            bool unlockedNextLevel = levelId == 1 &&
                finalScore >= LeaderboardService.ScoreToUnlockNextLevel;
            if (unlockedNextLevel && uiController != null)
                yield return uiController.PlayNextLevelUnlockNotice();

            if (leaderboard == null || !leaderboard.TryShowScoreEntry(levelId, finalScore))
                uiController?.ShowGameOver(finalScore);
        }

        public void RestartGame()
        {
            if (restarting)
                return;

            restarting = true;
            Time.timeScale = 1f;
            StartCoroutine(ReloadScene());
        }

        private IEnumerator ReloadScene()
        {
            yield return SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
            Time.timeScale = 1f;
        }
    }
}
