using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using WoguaShooter.Core;
using WoguaShooter.Update;

namespace WoguaShooter.UI
{
    /// <summary>
    /// 单场景游戏的界面流程：更新检查、主界面、选关和游戏内设置。
    /// 正常使用项目中的预生成 UI 预制体，字段为空时才创建简易备用界面。
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class FlowUIController : MonoBehaviour
    {
        [Header("可选绑定")]
        [SerializeField] private GameObject gameplayRoot;
        [SerializeField] private Canvas canvas;

        [Header("界面预制体实例")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject levelPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject volumePanel;
        [SerializeField] private GameObject pauseButton;
        [SerializeField] private GameObject settingsButton;
        [SerializeField] private Button startButton;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private Button levelOneButton;
        [SerializeField] private Button levelTwoButton;
        [SerializeField] private Button levelBackButton;
        [SerializeField] private Button volumeButton;
        [SerializeField] private Button volumeBackButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button menuButton;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider soundSlider;

        private GameManager manager;
        private PlayerController player;
        private SpawnManager spawner;
        private BackgroundScroller background;
        private UpdateService updateService;
        private Rigidbody2D playerBody;
        private Vector3 playerStartPosition;

        private Font chineseFont;
        private bool gameStarted;
        private bool transitioning;
        private LoadingOverlayController loadingOverlay;
        private TutorialGuideController tutorialGuide;
        private Image levelTwoNumberPlate;
        private Sprite levelTwoLockedSprite;
        private Color levelTwoLockedColor;
        private Sprite unlockedLevelNumberPlateSprite;
        private Color unlockedLevelNumberPlateColor = Color.white;
        private Color unlockedLevelNumberTextColor = new Color32(55, 88, 61, 255);

        public int CurrentLevelId { get; private set; } = 1;

        private IEnumerator Start()
        {
            manager = FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            spawner = FindFirstObjectByType<SpawnManager>(FindObjectsInactive.Include);
            background = FindFirstObjectByType<BackgroundScroller>(FindObjectsInactive.Include);
            updateService = FindFirstObjectByType<UpdateService>(FindObjectsInactive.Include);
            if (player != null)
            {
                playerBody = player.GetComponent<Rigidbody2D>();
                playerStartPosition = player.transform.position;
            }
            if (gameplayRoot == null)
                gameplayRoot = transform.gameObject;

            if (canvas == null)
                canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                Debug.LogWarning("未找到游戏画布，无法创建主界面。");
                yield break;
            }

            chineseFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimSun", "Arial" }, 32);
            LeaderboardService.InitializeSaveData();
            TranslateExistingTexts();
            BuildPanels();
            ApplyMainMenuPresentation();
            EnsureFlowOverlays();
            SetGameplayActive(false);

            if (updateService != null)
            {
                while (!updateService.IsCheckFinished)
                    yield return null;

                // 检测到新版本时，更新弹窗会等待玩家选择，选择后再打开主界面。
                if (updateService.HasPendingUpdate)
                {
                    updateService.CheckFinished += OnUpdateCheckFinished;
                    yield break;
                }
            }

            ShowMainMenu();
        }

        private void OnDestroy()
        {
            if (updateService != null)
                updateService.CheckFinished -= OnUpdateCheckFinished;
        }

        private void OnUpdateCheckFinished(bool ignored)
        {
            if (updateService == null || updateService.HasPendingUpdate)
                return;
            ShowMainMenu();
        }

        private void BuildPanels()
        {
            if (menuPanel != null && levelPanel != null && settingsPanel != null && volumePanel != null)
            {
                if (leaderboardButton == null)
                    leaderboardButton = menuPanel.transform.Find("LeaderboardButton")?.GetComponent<Button>();
                if (levelTwoButton == null)
                    levelTwoButton = levelPanel.transform.Find("LevelList/Viewport/Content/LevelTwoLocked")?.GetComponent<Button>();
                EnsureHudButtons();
                RefreshLevelSelection();
                BindPanelEvents();
                return;
            }

            menuPanel = CreatePanel("主界面", new Color(0.02f, 0.08f, 0.06f, 0.46f));
            CreateText(menuPanel.transform, "倭瓜射手大冒险", 52, new Vector2(0f, 150f), new Vector2(700f, 90f), Color.white);
            startButton = CreateButton(menuPanel.transform, "开始游戏", new Vector2(0f, 55f), new Vector2(310f, 72f));
            leaderboardButton = CreateButton(menuPanel.transform, "排行榜", new Vector2(0f, -30f), new Vector2(310f, 64f));
            quitButton = CreateButton(menuPanel.transform, "退出游戏", new Vector2(0f, -110f), new Vector2(310f, 62f));

            levelPanel = CreatePanel("关卡选择", new Color(0.02f, 0.08f, 0.06f, 0.52f));
            CreateText(levelPanel.transform, "选择关卡", 44, new Vector2(0f, 150f), new Vector2(600f, 75f), Color.white);
            levelOneButton = CreateButton(levelPanel.transform, "第1关", new Vector2(0f, 55f), new Vector2(320f, 72f));
            levelTwoButton = CreateButton(levelPanel.transform, "第2关", new Vector2(0f, -30f), new Vector2(320f, 72f));
            levelBackButton = CreateButton(levelPanel.transform, "返回", new Vector2(0f, -120f), new Vector2(260f, 62f));

            settingsPanel = CreatePanel("设置面板", new Color(0.02f, 0.08f, 0.06f, 0.76f));
            CreateText(settingsPanel.transform, "游戏设置", 42, new Vector2(0f, 145f), new Vector2(600f, 70f), Color.white);
            volumeButton = CreateButton(settingsPanel.transform, "音量设置", new Vector2(0f, 50f), new Vector2(330f, 64f));
            continueButton = CreateButton(settingsPanel.transform, "继续游戏", new Vector2(0f, -35f), new Vector2(300f, 62f));
            menuButton = CreateButton(settingsPanel.transform, "返回主界面", new Vector2(0f, -115f), new Vector2(300f, 62f));

            volumePanel = CreatePanel("音量面板", new Color(0.02f, 0.08f, 0.06f, 0.8f));
            CreateText(volumePanel.transform, "音量设置", 42, new Vector2(0f, 155f), new Vector2(600f, 70f), Color.white);
            CreateText(volumePanel.transform, "背景音乐", 25, new Vector2(-170f, 72f), new Vector2(220f, 48f), Color.white);
            musicSlider = CreateSlider(volumePanel.transform, new Vector2(100f, 72f));
            CreateText(volumePanel.transform, "游戏音效", 25, new Vector2(-170f, -28f), new Vector2(220f, 48f), Color.white);
            soundSlider = CreateSlider(volumePanel.transform, new Vector2(100f, -28f));
            volumeBackButton = CreateButton(volumePanel.transform, "返回设置", new Vector2(0f, -135f), new Vector2(240f, 65f));

            pauseButton = CreateButton(canvas.transform, "暂停", new Vector2(-264f, -30f), new Vector2(112f, 58f)).gameObject;
            SetAnchor(pauseButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-264f, -30f));
            settingsButton = CreateButton(canvas.transform, "设置", new Vector2(-132f, -30f), new Vector2(112f, 58f)).gameObject;
            SetAnchor(settingsButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-132f, -30f));

            BindPanelEvents();
        }

        private void EnsureHudButtons()
        {
            if (canvas == null)
                return;

            if (pauseButton == null)
            {
                pauseButton = CreateButton(canvas.transform, "暂停", new Vector2(-264f, -30f), new Vector2(112f, 58f)).gameObject;
                SetAnchor(pauseButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-264f, -30f));
            }

            if (settingsButton == null)
            {
                settingsButton = CreateButton(canvas.transform, "设置", new Vector2(-132f, -30f), new Vector2(112f, 58f)).gameObject;
                SetAnchor(settingsButton.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(-132f, -30f));
            }
        }

        private void BindPanelEvents()
        {
            BindButton(startButton, ShowLevelSelect);
            BindButton(leaderboardButton, ShowLeaderboard);
            BindButton(quitButton, QuitGame);
            BindButton(levelOneButton, () => BeginGame(1));
            BindButton(levelTwoButton, () => BeginGame(2));
            BindButton(levelBackButton, ShowMainMenu);
            BindButton(volumeButton, ShowVolumeSettings);
            BindButton(volumeBackButton, HideVolumeSettings);
            BindButton(continueButton, HideSettings);
            BindButton(menuButton, ReturnToMenu);

            Button settings = settingsButton != null ? settingsButton.GetComponent<Button>() : null;
            BindButton(settings, ShowSettings);

            if (musicSlider != null)
            {
                musicSlider.onValueChanged.RemoveAllListeners();
                musicSlider.SetValueWithoutNotify(AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.7f);
                musicSlider.onValueChanged.AddListener(SetMusicVolume);
            }
            if (soundSlider != null)
            {
                soundSlider.onValueChanged.RemoveAllListeners();
                soundSlider.SetValueWithoutNotify(AudioManager.Instance != null ? AudioManager.Instance.SfxVolume : 1f);
                soundSlider.onValueChanged.AddListener(SetSoundVolume);
            }

            menuPanel.SetActive(false);
            levelPanel.SetActive(false);
            settingsPanel.SetActive(false);
            volumePanel.SetActive(false);
            SetHudButtons(false, false);
        }

        private void SetHudButtons(bool pauseActive, bool settingsActive)
        {
            if (pauseButton != null)
                pauseButton.SetActive(pauseActive);
            if (settingsButton != null)
                settingsButton.SetActive(settingsActive);
        }

        private static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void ShowMainMenu()
        {
            if (menuPanel == null)
                return;
            gameStarted = false;
            StopGameplay();
            menuPanel.SetActive(true);
            levelPanel.SetActive(false);
            settingsPanel.SetActive(false);
            volumePanel.SetActive(false);
            FindFirstObjectByType<LeaderboardUIController>(FindObjectsInactive.Include)?.HideAll();
            SetHudButtons(false, false);
        }

        private void ShowLeaderboard()
        {
            LeaderboardUIController leaderboard = FindFirstObjectByType<LeaderboardUIController>(FindObjectsInactive.Include);
            if (leaderboard == null)
            {
                Debug.LogWarning("未找到排行榜界面。");
                return;
            }

            menuPanel.SetActive(true);
            levelPanel.SetActive(false);
            settingsPanel.SetActive(false);
            volumePanel.SetActive(false);
            SetHudButtons(false, false);
            leaderboard.ShowLeaderboard(1);
        }

        public void ReturnFromLeaderboard()
        {
            ShowMainMenu();
        }

        private void ShowLevelSelect()
        {
            RefreshLevelSelection();
            menuPanel.SetActive(false);
            levelPanel.SetActive(true);
            settingsPanel.SetActive(false);
            volumePanel.SetActive(false);
            SetHudButtons(false, false);
        }

        private void RefreshLevelSelection()
        {
            if (levelTwoButton == null)
                return;

            ConfigureLevelNumberPlate(levelOneButton != null ? levelOneButton.transform : null, 1, true);
            SetLevelCardText(
                levelOneButton != null ? levelOneButton.transform : null,
                "第1关  无尽草地",
                "穿越连绵草坡，击退沿途怪物");

            bool unlocked = LeaderboardService.IsLevelUnlocked(2);
            levelTwoButton.interactable = unlocked;
            Transform card = levelTwoButton.transform;

            Text title = card.Find("Title")?.GetComponent<Text>();
            if (title != null)
                title.text = "第2关  暮色荒原";

            Text description = card.Find("Description")?.GetComponent<Text>();
            if (description != null)
                description.text = "穿越风蚀荒原，击退沿途怪物";

            Text status = card.Find("Status")?.GetComponent<Text>();
            if (status != null)
            {
                status.text = unlocked ? "可挑战" : "未解锁";
                status.color = unlocked
                    ? new Color32(255, 220, 96, 255)
                    : new Color32(172, 184, 176, 255);
            }

            ConfigureLevelNumberPlate(card, 2, unlocked);

            Transform levelThreeCard = levelPanel != null
                ? levelPanel.transform.Find("LevelList/Viewport/Content/LevelThreeLocked")
                : null;
            SetLevelCardText(
                levelThreeCard,
                "第3关  月夜墓园",
                "穿越幽暗墓园，击退沿途怪物");
        }

        private void ConfigureLevelNumberPlate(Transform card, int levelNumber, bool unlocked)
        {
            if (card == null)
                return;

            Transform numberPlate = card.Find("NumberPlate");
            if (numberPlate == null)
                return;

            CacheUnlockedLevelNumberPlateStyle();

            Image numberPlateImage = numberPlate.GetComponent<Image>();
            if (levelNumber == 2 && levelTwoNumberPlate == null)
            {
                levelTwoNumberPlate = numberPlateImage;
                if (levelTwoNumberPlate != null)
                {
                    levelTwoLockedSprite = levelTwoNumberPlate.sprite;
                    levelTwoLockedColor = levelTwoNumberPlate.color;
                }
            }

            Text numberText = numberPlate.Find("IconNumber")?.GetComponent<Text>();
            if (numberText == null && unlocked)
            {
                numberText = CreateText(numberPlate, levelNumber.ToString(), 19, new Vector2(0f, 5f), new Vector2(32f, 32f), unlockedLevelNumberTextColor);
                numberText.gameObject.name = "IconNumber";
            }

            RectTransform plateRect = numberPlate as RectTransform;
            if (plateRect != null)
                plateRect.sizeDelta = new Vector2(74f, 74f);

            if (numberPlateImage != null)
            {
                numberPlateImage.sprite = unlocked ? unlockedLevelNumberPlateSprite : levelTwoLockedSprite;
                numberPlateImage.color = unlocked ? unlockedLevelNumberPlateColor : levelTwoLockedColor;
                numberPlateImage.preserveAspect = true;
            }

            if (numberText != null)
            {
                numberText.text = levelNumber.ToString();
                numberText.fontSize = 19;
                numberText.alignment = TextAnchor.MiddleCenter;
                numberText.color = unlockedLevelNumberTextColor;
                RectTransform numberRect = numberText.rectTransform;
                numberRect.anchorMin = numberRect.anchorMax = new Vector2(0.5f, 0.5f);
                numberRect.pivot = new Vector2(0.5f, 0.5f);
                numberRect.anchoredPosition = new Vector2(0f, 5f);
                numberRect.sizeDelta = new Vector2(32f, 32f);
                numberText.gameObject.SetActive(unlocked);
            }
        }

        private void CacheUnlockedLevelNumberPlateStyle()
        {
            if (unlockedLevelNumberPlateSprite != null || levelOneButton == null)
                return;

            Transform numberPlate = levelOneButton.transform.Find("NumberPlate");
            Image plateImage = numberPlate != null ? numberPlate.GetComponent<Image>() : null;
            if (plateImage == null)
                return;

            unlockedLevelNumberPlateSprite = plateImage.sprite;
            unlockedLevelNumberPlateColor = plateImage.color;
            Text numberText = numberPlate.Find("IconNumber")?.GetComponent<Text>();
            if (numberText != null)
                unlockedLevelNumberTextColor = numberText.color;
        }

        private static string GetLevelName(int levelId)
        {
            return $"第{Mathf.Max(1, levelId)}关";
        }

        private static void SetLevelCardText(Transform card, string titleText, string descriptionText)
        {
            if (card == null)
                return;

            Text title = card.Find("Title")?.GetComponent<Text>();
            if (title != null)
                title.text = titleText;

            Text description = card.Find("Description")?.GetComponent<Text>();
            if (description != null)
                description.text = descriptionText;
        }

        private void BeginGame(int levelId)
        {
            if (!transitioning && LeaderboardService.IsLevelUnlocked(levelId))
                StartCoroutine(BeginGameRoutine(levelId));
        }

        private IEnumerator BeginGameRoutine(int levelId)
        {
            transitioning = true;
            tutorialGuide?.StopTutorial();
            Time.timeScale = 0f;

            if (loadingOverlay != null)
                yield return loadingOverlay.Play($"正在进入{GetLevelName(levelId)}", () => ActivateLevel(levelId));
            else
                ActivateLevel(levelId);

            Time.timeScale = 1f;
            if (levelId == 1)
                tutorialGuide?.BeginFirstLevel(player, SpellInventory.Instance, spawner);
            transitioning = false;
        }

        private void ActivateLevel(int levelId)
        {
            CurrentLevelId = Mathf.Clamp(levelId, 1, 2);
            gameStarted = true;
            menuPanel.SetActive(false);
            levelPanel.SetActive(false);
            settingsPanel.SetActive(false);
            volumePanel.SetActive(false);
            SetHudButtons(true, true);
            player?.GetComponent<WeaponController>()?.ResetWeaponState();
            SetGameplayActive(true);
            manager?.ResetForNewGame();
            Time.timeScale = 0f;
        }

        private void ShowSettings()
        {
            if (!gameStarted)
                return;
            settingsPanel.SetActive(true);
            volumePanel.SetActive(false);
            SetHudButtons(false, false);
            manager?.PauseGameWithoutPanel();
        }

        private void ShowVolumeSettings()
        {
            if (!gameStarted || volumePanel == null)
                return;
            settingsPanel.SetActive(false);
            volumePanel.SetActive(true);
            SetHudButtons(false, false);
            manager?.PauseGameWithoutPanel();
        }

        private void HideVolumeSettings()
        {
            if (volumePanel == null)
                return;
            volumePanel.SetActive(false);
            settingsPanel.SetActive(true);
            SetHudButtons(false, false);
        }

        private void HideSettings()
        {
            settingsPanel.SetActive(false);
            volumePanel?.SetActive(false);
            SetHudButtons(true, true);
            if (manager != null && manager.State == GameState.Paused)
                manager.ContinueGame();
        }

        private void ReturnToMenu()
        {
            RequestReturnToMenu();
        }

        public void RequestReturnToMenu()
        {
            if (!transitioning)
                StartCoroutine(ReturnToMenuRoutine());
        }

        public void RequestRestartLevel()
        {
            if (!transitioning)
                StartCoroutine(RestartLevelRoutine());
        }

        private IEnumerator RestartLevelRoutine()
        {
            transitioning = true;
            tutorialGuide?.StopTutorial();
            FindFirstObjectByType<LeaderboardUIController>(FindObjectsInactive.Include)?.HideAll();
            SetHudButtons(false, false);
            manager?.EnterMenuState();
            SetGameplayActive(false);
            ClearLevelObjects();
            Time.timeScale = 0f;
            int levelId = CurrentLevelId;

            void RestartAction()
            {
                ActivateLevel(levelId);
            }

            if (loadingOverlay != null)
                yield return loadingOverlay.Play("正在重新开始本关", RestartAction);
            else
                RestartAction();

            Time.timeScale = 1f;
            if (levelId == 1)
                tutorialGuide?.BeginFirstLevel(player, SpellInventory.Instance, spawner);
            transitioning = false;
        }

        private IEnumerator ReturnToMenuRoutine()
        {
            transitioning = true;
            tutorialGuide?.StopTutorial();
            settingsPanel.SetActive(false);
            volumePanel?.SetActive(false);
            SetHudButtons(false, false);
            Time.timeScale = 0f;

            if (loadingOverlay != null)
            {
                yield return loadingOverlay.Play("正在返回主菜单", () =>
                {
                    ClearLevelObjects();
                    ShowMainMenu();
                    Time.timeScale = 0f;
                });
            }
            else
            {
                ClearLevelObjects();
                ShowMainMenu();
            }

            Time.timeScale = 1f;
            transitioning = false;
        }

        private void StopGameplay()
        {
            if (manager != null && manager.State != GameState.Menu)
                manager.EnterMenuState();
            Time.timeScale = 1f;
            if (player != null)
            {
                player.GetComponent<WeaponController>()?.ResetWeaponState();
                player.enabled = false;
                player.gameObject.SetActive(false);
            }
            if (playerBody != null)
            {
                playerBody.linearVelocity = Vector2.zero;
                playerBody.simulated = false;
                player.transform.position = playerStartPosition;
            }
            if (spawner != null)
                spawner.enabled = false;
            if (background != null)
                background.enabled = false;
        }

        private void SetGameplayActive(bool active)
        {
            if (player != null)
            {
                player.gameObject.SetActive(active);
                player.enabled = active;
            }
            if (playerBody != null)
            {
                playerBody.simulated = active;
                playerBody.linearVelocity = Vector2.zero;
                player.transform.position = playerStartPosition;
            }
            if (spawner != null)
                spawner.enabled = active;
            if (background != null)
                background.enabled = active;
        }

        private static void ClearLevelObjects()
        {
            GameManager.DestroyLevelObjects();
        }

        private void SetMusicVolume(float value)
        {
            AudioManager.Instance?.SetMusicVolume(value);
        }

        private void SetSoundVolume(float value)
        {
            AudioManager.Instance?.SetSfxVolume(value);
        }

        private void EnsureFlowOverlays()
        {
            loadingOverlay = GetComponent<LoadingOverlayController>();
            if (loadingOverlay == null)
                loadingOverlay = gameObject.AddComponent<LoadingOverlayController>();
            loadingOverlay.Initialize(canvas, chineseFont);

            tutorialGuide = GetComponent<TutorialGuideController>();
            if (tutorialGuide == null)
                tutorialGuide = gameObject.AddComponent<TutorialGuideController>();
            tutorialGuide.Initialize(canvas, chineseFont);
        }

        private void ApplyMainMenuPresentation()
        {
            if (menuPanel == null)
                return;

            Image backgroundImage = menuPanel.GetComponent<Image>();
            if (backgroundImage != null)
            {
                if (backgroundImage.sprite == null)
                    StartCoroutine(LoadMenuBackgroundAsync(backgroundImage));
                backgroundImage.color = Color.white;
                backgroundImage.preserveAspect = false;
            }

            RectTransform frame = FindRect(menuPanel.transform, "MenuFrame");
            StyleRect(frame, new Vector2(330f, 4f), new Vector2(520f, 500f));
            if (frame != null && frame.TryGetComponent(out Image frameImage))
                frameImage.color = new Color(0.035f, 0.105f, 0.075f, 0.91f);

            RectTransform title = FindRect(menuPanel.transform, "Title");
            StyleRect(title, new Vector2(330f, 162f), new Vector2(570f, 82f));
            if (title != null && title.TryGetComponent(out Text titleText))
            {
                titleText.fontSize = 54;
                titleText.color = new Color32(255, 230, 137, 255);
            }

            RectTransform subtitle = FindRect(menuPanel.transform, "Subtitle");
            StyleRect(subtitle, new Vector2(330f, 105f), new Vector2(500f, 42f));
            if (subtitle != null && subtitle.TryGetComponent(out Text subtitleText))
            {
                subtitleText.text = "踏上草地，击退沿途怪物";
                subtitleText.fontSize = 23;
                subtitleText.color = new Color32(225, 239, 220, 255);
            }

            StyleMenuButton(startButton, new Vector2(330f, 35f), new Vector2(390f, 72f),
                new Color32(62, 118, 72, 255), new Color32(236, 190, 78, 255));
            StyleMenuButton(leaderboardButton, new Vector2(330f, -45f), new Vector2(390f, 62f),
                new Color32(52, 101, 71, 255), new Color32(198, 174, 99, 240));
            StyleMenuButton(quitButton, new Vector2(330f, -120f), new Vector2(390f, 58f),
                new Color32(75, 71, 53, 250), new Color32(181, 147, 75, 230));

            RectTransform version = FindRect(menuPanel.transform, "VersionText");
            if (version != null)
            {
                version.anchorMin = version.anchorMax = new Vector2(1f, 0f);
                version.pivot = new Vector2(1f, 0f);
                version.anchoredPosition = new Vector2(-24f, 18f);
                version.sizeDelta = new Vector2(260f, 28f);
                if (version.TryGetComponent(out Text versionText))
                {
                    versionText.alignment = TextAnchor.MiddleRight;
                    versionText.fontSize = 16;
                    versionText.color = new Color(1f, 1f, 1f, 0.78f);
                }
            }
        }

        private static IEnumerator LoadMenuBackgroundAsync(Image target)
        {
            ResourceRequest request = Resources.LoadAsync<Texture2D>("UI/MainMenuBackground");
            yield return request;
            Texture2D texture = request.asset as Texture2D;
            if (target == null || target.sprite != null || texture == null)
                yield break;

            target.sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static void StyleMenuButton(Button button, Vector2 position, Vector2 size, Color fillColor, Color borderColor)
        {
            if (button == null)
                return;

            RectTransform rect = button.transform as RectTransform;
            StyleRect(rect, position, size);
            Image rootImage = button.GetComponent<Image>();
            if (rootImage != null)
                rootImage.color = borderColor;

            Transform fill = button.transform.Find("Fill");
            if (fill != null && fill.TryGetComponent(out Image fillImage))
                fillImage.color = fillColor;

            Text label = button.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontSize = button.name.Contains("Start") ? 29 : 25;
                label.color = Color.white;
            }
        }

        private static RectTransform FindRect(Transform parent, string childName)
        {
            return parent != null ? parent.Find(childName) as RectTransform : null;
        }

        private static void StyleRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void QuitGame()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private GameObject CreatePanel(string name, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            Stretch(panel.GetComponent<RectTransform>());
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private Text CreateText(Transform parent, string text, int size, Vector2 position, Vector2 dimensions, Color color)
        {
            GameObject objectRoot = new GameObject("文字", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            objectRoot.transform.SetParent(parent, false);
            RectTransform rect = objectRoot.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            Text label = objectRoot.GetComponent<Text>();
            label.text = text;
            label.font = chineseFont;
            label.fontSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private Button CreateButton(Transform parent, string text, Vector2 position, Vector2 dimensions)
        {
            GameObject objectRoot = new GameObject("按钮_" + text, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            objectRoot.transform.SetParent(parent, false);
            RectTransform rect = objectRoot.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            Image image = objectRoot.GetComponent<Image>();
            image.color = new Color32(74, 139, 111, 245);
            Button button = objectRoot.GetComponent<Button>();
            button.targetGraphic = image;
            CreateText(objectRoot.transform, text, 26, Vector2.zero, dimensions - new Vector2(16f, 8f), Color.white);
            return button;
        }

        private Slider CreateSlider(Transform parent, Vector2 position)
        {
            GameObject root = new GameObject("音量滑块", typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(350f, 40f);
            Slider slider = root.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;
            GameObject background = new GameObject("背景", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(root.transform, false);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            backgroundRect.anchorMin = new Vector2(0f, 0.25f);
            backgroundRect.anchorMax = new Vector2(1f, 0.75f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image backgroundImage = background.GetComponent<Image>();
            backgroundImage.sprite = Resources.Load<Sprite>("UI/滑动条背景图");
            backgroundImage.color = backgroundImage.sprite != null ? Color.white : new Color32(30, 60, 48, 255);
            backgroundImage.preserveAspect = false;

            GameObject fillArea = new GameObject("填充区域", typeof(RectTransform));
            fillArea.transform.SetParent(root.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.anchoredPosition = new Vector2(-5f, 0f);
            fillAreaRect.sizeDelta = new Vector2(-20f, 0f);

            GameObject fill = new GameObject("填充", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;
            Image fillImage = fill.GetComponent<Image>();
            fillImage.sprite = Resources.Load<Sprite>("UI/滑动条填充图");
            fillImage.color = fillImage.sprite != null ? Color.white : new Color32(247, 193, 67, 255);
            fillImage.preserveAspect = false;

            GameObject handleArea = new GameObject("滑块区域", typeof(RectTransform));
            handleArea.transform.SetParent(root.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.anchoredPosition = Vector2.zero;
            handleAreaRect.sizeDelta = new Vector2(-20f, 0f);

            GameObject handle = new GameObject("滑块", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.zero;
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.anchoredPosition = Vector2.zero;
            handleRect.sizeDelta = new Vector2(38f, 38f);
            Image handleImage = handle.GetComponent<Image>();
            handleImage.sprite = Resources.Load<Sprite>("UI/滑块");
            handleImage.color = handleImage.sprite != null ? Color.white : new Color32(255, 245, 198, 255);
            handleImage.preserveAspect = false;

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;
            slider.direction = Slider.Direction.LeftToRight;
            return slider;
        }

        private void TranslateExistingTexts()
        {
            Text[] labels = FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Text label in labels)
            {
                string value = label.text.Trim().ToUpperInvariant();
                if (value.Contains("SCORE")) label.text = "分数 0000";
                else if (value.Contains("PEA SHOOTER")) label.text = "普通豌豆";
                else if (value.Contains("MACHINE GUN")) label.text = "豌豆机枪";
                else if (value.Contains("CORN CANNON")) label.text = "玉米大炮";
                else if (value == "PAUSED") label.text = "游戏暂停";
                else if (value == "CONTINUE") label.text = "继续游戏";
                else if (value.Contains("GAME OVER")) label.text = "游戏结束";
                else if (value.Contains("RESTART")) label.text = "重新开始";
                else if (value.StartsWith("SCORE:")) label.text = "分数：0";
                label.font = chineseFont;
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void SetAnchor(RectTransform rect, Vector2 anchor, Vector2 position)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = position;
        }
    }
}
