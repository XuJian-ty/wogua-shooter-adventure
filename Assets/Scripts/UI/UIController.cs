using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using WoguaShooter.Core;
using WoguaShooter.Config;

namespace WoguaShooter.UI
{
    public class UIController : MonoBehaviour
    {
        private const int SpellSlotCount = SpellInventory.SlotCount;
        private const float SpellSlotWidth = 72f;
        private const float SpellSlotHeight = 74f;
        private const float SpellSlotSpacing = 6f;
        private const int SpellPanelHorizontalPadding = 8;
        private const int SpellPanelVerticalPadding = 7;

        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text timeText;
        [SerializeField] private Text weaponText;
        [SerializeField] private Text finalScoreText;
        [SerializeField] private Button pauseButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Sprite returnMenuButtonSprite;
        [SerializeField] private Slider weaponTimer;
        [SerializeField] private WeaponController weaponController;

        private SpellInventory spellInventory;
        private readonly List<SpellSlotView> spellSlots = new List<SpellSlotView>();
        private Button returnMenuButton;
        private bool weaponDisplayInitialized;
        private WeaponType displayedPrimaryType;
        private WeaponType displayedSecondaryType;
        private bool displayedSecondMuzzle;
        private bool displayedTripleShot;
        private bool displayedRepeater;
        private bool displayedDualMuzzle;
        private bool displayedWaveFlight;
        private bool weaponTimerVisible;
        private int displayedTimeTenths = -1;
        private GameObject nextLevelUnlockNotice;
        private RectTransform nextLevelUnlockPrompt;
        private CanvasGroup nextLevelUnlockGroup;
        private TutorialGradientTextEffect nextLevelUnlockTextEffect;

        private void Awake()
        {
            pauseButton?.onClick.AddListener(TogglePauseWithoutPanel);
            continueButton?.onClick.AddListener(() => GameManager.Instance?.ContinueGame());
            restartButton?.onClick.AddListener(RestartCurrentLevel);
            EnsureGameOverMenuButton();
            EnsureNextLevelUnlockNotice();

            if (weaponController == null)
                weaponController = FindFirstObjectByType<WeaponController>();
            weaponTimerVisible = weaponTimer != null && weaponTimer.gameObject.activeSelf;
            spellInventory = SpellInventory.GetOrCreate(weaponController);
            if (spellInventory != null)
            {
                spellInventory.Changed += RefreshSpellSlots;
                CreateSpellSlots();
            }
        }

        private void TogglePauseWithoutPanel()
        {
            GameManager manager = GameManager.Instance;
            if (manager == null)
                return;

            if (manager.State == GameState.Playing)
            {
                manager.PauseGameWithoutPanel();
            }
            else if (manager.State == GameState.Paused)
            {
                manager.ContinueGame();
            }
        }

        private void RestartCurrentLevel()
        {
            FlowUIController flow = FindFirstObjectByType<FlowUIController>(FindObjectsInactive.Include);
            if (flow != null)
                flow.RequestRestartLevel();
            else
                GameManager.Instance?.RestartGame();
        }

        private void ReturnToMainMenu()
        {
            FindFirstObjectByType<FlowUIController>(FindObjectsInactive.Include)?.RequestReturnToMenu();
        }

        private void EnsureGameOverMenuButton()
        {
            if (gameOverPanel == null || restartButton == null)
                return;

            Transform existing = gameOverPanel.transform.Find("ReturnMenuButton");
            bool createdAtRuntime = existing == null;
            if (existing != null)
            {
                returnMenuButton = existing.GetComponent<Button>();
            }
            else
            {
                GameObject root = Instantiate(restartButton.gameObject, gameOverPanel.transform);
                root.name = "ReturnMenuButton";
                returnMenuButton = root.GetComponent<Button>();
            }

            StyleGameOverButtons(createdAtRuntime);
            returnMenuButton?.onClick.RemoveAllListeners();
            returnMenuButton?.onClick.AddListener(ReturnToMainMenu);
        }

        private void StyleGameOverButtons(bool configureFallbackLayout)
        {
            if (restartButton == null || returnMenuButton == null)
                return;

            Image returnImage = returnMenuButton.GetComponent<Image>();
            if (returnImage != null && returnMenuButtonSprite != null)
            {
                returnImage.sprite = returnMenuButtonSprite;
                returnImage.type = Image.Type.Simple;
                returnImage.preserveAspect = true;
                returnMenuButton.targetGraphic = returnImage;
            }

            Text returnLabel = returnMenuButton.GetComponentInChildren<Text>(true);
            Text restartLabel = restartButton.GetComponentInChildren<Text>(true);
            if (returnLabel != null)
            {
                returnLabel.text = "返回主菜单";
                if (restartLabel != null)
                {
                    returnLabel.font = restartLabel.font;
                    returnLabel.fontSize = restartLabel.fontSize;
                    returnLabel.fontStyle = restartLabel.fontStyle;
                    returnLabel.alignment = restartLabel.alignment;
                    returnLabel.color = restartLabel.color;
                }
            }

            if (!configureFallbackLayout)
                return;

            SetGameOverButtonRect(restartButton.transform as RectTransform, new Vector2(0f, 5f), new Vector2(345f, 173f));
            SetGameOverButtonRect(returnMenuButton.transform as RectTransform, new Vector2(0f, -155f), new Vector2(345f, 124f));
            SetGameOverElementRect("Dialog", new Vector2(0f, -5f), new Vector2(500f, 550f));
            SetGameOverElementRect("Title", new Vector2(0f, 205f), new Vector2(440f, 72f));
            SetGameOverElementRect("FinalScoreText", new Vector2(0f, 130f), new Vector2(420f, 58f));

            if (returnLabel != null && returnLabel.transform is RectTransform labelRect)
            {
                labelRect.anchoredPosition = new Vector2(20.5f, -12f);
                labelRect.sizeDelta = new Vector2(240f, 56f);
            }
        }

        private void SetGameOverElementRect(string childName, Vector2 position, Vector2 size)
        {
            if (gameOverPanel.transform.Find(childName) is RectTransform rect)
                SetGameOverButtonRect(rect, position, size);
        }

        private static void SetGameOverButtonRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            if (rect == null)
                return;

            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private void Update()
        {
            if (weaponController == null)
                return;

            float remaining = weaponController.PoweredTime;
            bool powered = weaponController.CurrentType != WeaponType.Pea && remaining > 0f;
            WeaponType primaryType = weaponController.CurrentType;
            WeaponType secondaryType = weaponController.SecondaryType;
            bool hasSecondMuzzle = weaponController.HasSecondMuzzle;
            bool tripleShot = weaponController.TripleShotActive;
            bool repeater = weaponController.RepeaterActive;
            bool dualMuzzle = weaponController.DualMuzzleActive;
            bool waveFlight = weaponController.WaveFlightActive;
            bool weaponDisplayChanged = !weaponDisplayInitialized ||
                displayedPrimaryType != primaryType ||
                displayedSecondaryType != secondaryType ||
                displayedSecondMuzzle != hasSecondMuzzle ||
                displayedTripleShot != tripleShot ||
                displayedRepeater != repeater ||
                displayedDualMuzzle != dualMuzzle ||
                displayedWaveFlight != waveFlight;
            if (weaponText != null && weaponDisplayChanged)
            {
                string weaponName = GetWeaponDisplayName(primaryType);
                if (hasSecondMuzzle)
                    weaponName += " → " + GetWeaponDisplayName(secondaryType);
                if (tripleShot)
                    weaponName += " + 三向齐发";
                if (repeater)
                    weaponName += " + 双弹连发";
                if (dualMuzzle)
                    weaponName += " + 双枪联动";
                if (waveFlight)
                    weaponName += " + 波浪轨迹";
                weaponText.text = weaponName;

                weaponDisplayInitialized = true;
                displayedPrimaryType = primaryType;
                displayedSecondaryType = secondaryType;
                displayedSecondMuzzle = hasSecondMuzzle;
                displayedTripleShot = tripleShot;
                displayedRepeater = repeater;
                displayedDualMuzzle = dualMuzzle;
                displayedWaveFlight = waveFlight;
            }
            if (weaponTimer != null)
            {
                if (weaponTimerVisible != powered)
                {
                    weaponTimerVisible = powered;
                    weaponTimer.gameObject.SetActive(powered);
                }

                GameManager manager = GameManager.Instance;
                GameConfig config = manager != null ? manager.Config : null;
                weaponTimer.value = powered && config != null
                    ? remaining / config.GetWeapon(primaryType).duration
                    : 0f;
            }
        }

        public void SetScore(int score)
        {
            if (scoreText != null)
                scoreText.text = $"分数 {score:0000}";
        }

        public void SetTime(float time)
        {
            int tenths = Mathf.Max(0, Mathf.FloorToInt(time * 10f));
            if (timeText == null || tenths == displayedTimeTenths)
                return;

            displayedTimeTenths = tenths;
            timeText.text = $"时间 {tenths / 10f:0.0} 秒";
        }

        public void ShowPlaying()
        {
            HideNextLevelUnlockNotice();
            hudPanel?.SetActive(true);
            pausePanel?.SetActive(false);
            gameOverPanel?.SetActive(false);
        }

        public void ShowMenu()
        {
            HideNextLevelUnlockNotice();
            hudPanel?.SetActive(false);
            pausePanel?.SetActive(false);
            gameOverPanel?.SetActive(false);
        }

        public void ShowPause()
        {
            hudPanel?.SetActive(true);
            pausePanel?.SetActive(true);
            gameOverPanel?.SetActive(false);
        }

        public void ShowGameOver(int score)
        {
            HideNextLevelUnlockNotice();
            PrepareGameOver();
            gameOverPanel?.SetActive(true);
            if (finalScoreText != null)
                finalScoreText.text = $"积分：{score}";
        }

        public void PrepareGameOver()
        {
            FindFirstObjectByType<TutorialGuideController>(FindObjectsInactive.Include)?.StopTutorial();
            hudPanel?.SetActive(false);
            pausePanel?.SetActive(false);
            gameOverPanel?.SetActive(false);
        }

        public IEnumerator PlayNextLevelUnlockNotice()
        {
            EnsureNextLevelUnlockNotice();
            if (nextLevelUnlockNotice == null || nextLevelUnlockPrompt == null || nextLevelUnlockGroup == null)
                yield break;

            nextLevelUnlockNotice.transform.SetAsLastSibling();
            nextLevelUnlockNotice.SetActive(true);
            nextLevelUnlockGroup.alpha = 0f;

            RectTransform canvasRect = transform as RectTransform;
            float travel = Mathf.Max(960f, (canvasRect != null ? canvasRect.rect.width : 1386f) * 0.78f);
            Vector2 settledPosition = new Vector2(0f, 55f);
            Vector2 start = settledPosition + new Vector2(travel, 245f);
            Vector2 control = settledPosition + new Vector2(travel * 0.46f, -80f);
            const float enterDuration = 1.35f;

            for (float elapsed = 0f; elapsed < enterDuration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / enterDuration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                Vector2 position = QuadraticBezier(start, control, settledPosition, eased);
                float wave = Mathf.Sin(t * Mathf.PI * 3.5f) * (1f - eased) * 28f;
                nextLevelUnlockPrompt.anchoredPosition = position + Vector2.up * wave;
                nextLevelUnlockPrompt.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, eased);
                nextLevelUnlockPrompt.localRotation = Quaternion.Euler(
                    0f, 0f, Mathf.Lerp(6f, 0f, eased) + Mathf.Sin(t * Mathf.PI * 4f) * (1f - eased) * 2.5f);
                nextLevelUnlockGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 3f));
                nextLevelUnlockTextEffect?.SetSnakeMotion(
                    Mathf.Lerp(15f, 7f, eased),
                    Time.unscaledTime * 3f + t * 2.5f,
                    Mathf.Lerp(4.5f, 2.2f, eased));
                yield return null;
            }

            const float holdDuration = 1.8f;
            for (float elapsed = 0f; elapsed < holdDuration; elapsed += Time.unscaledDeltaTime)
            {
                float time = Time.unscaledTime;
                nextLevelUnlockPrompt.anchoredPosition = settledPosition + Vector2.up * (Mathf.Sin(time * 2.2f) * 6f);
                nextLevelUnlockPrompt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.4f) * 1.2f);
                nextLevelUnlockPrompt.localScale = Vector3.one * (1f + Mathf.Sin(time * 2.6f) * 0.012f);
                nextLevelUnlockTextEffect?.SetSnakeMotion(7f, time * 2.4f, 2.2f);
                yield return null;
            }

            Vector2 exitStart = nextLevelUnlockPrompt.anchoredPosition;
            Vector2 exitEnd = settledPosition + new Vector2(-travel, 220f);
            Vector2 exitControl = settledPosition + new Vector2(-travel * 0.44f, -70f);
            const float exitDuration = 0.9f;
            for (float elapsed = 0f; elapsed < exitDuration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / exitDuration);
                float eased = t * t;
                Vector2 position = QuadraticBezier(exitStart, exitControl, exitEnd, eased);
                float wave = Mathf.Sin(t * Mathf.PI * 2.8f) * 20f;
                nextLevelUnlockPrompt.anchoredPosition = position + Vector2.up * wave;
                nextLevelUnlockPrompt.localScale = Vector3.one * Mathf.Lerp(1f, 0.9f, eased);
                nextLevelUnlockPrompt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -7f, eased));
                nextLevelUnlockGroup.alpha = 1f - Mathf.SmoothStep(0.55f, 1f, t);
                nextLevelUnlockTextEffect?.SetSnakeMotion(
                    Mathf.Lerp(7f, 14f, eased),
                    Time.unscaledTime * 3f + t * 2f,
                    Mathf.Lerp(2.2f, 4.2f, eased));
                yield return null;
            }

            HideNextLevelUnlockNotice();
        }

        private void EnsureNextLevelUnlockNotice()
        {
            if (nextLevelUnlockNotice != null)
                return;

            nextLevelUnlockNotice = new GameObject(
                "NextLevelUnlockNotice",
                typeof(RectTransform),
                typeof(CanvasGroup));
            nextLevelUnlockNotice.transform.SetParent(transform, false);
            RectTransform rootRect = (RectTransform)nextLevelUnlockNotice.transform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            nextLevelUnlockGroup = nextLevelUnlockNotice.GetComponent<CanvasGroup>();
            nextLevelUnlockGroup.interactable = false;
            nextLevelUnlockGroup.blocksRaycasts = false;

            GameObject prompt = new GameObject(
                "UnlockPrompt",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            prompt.transform.SetParent(nextLevelUnlockNotice.transform, false);
            nextLevelUnlockPrompt = (RectTransform)prompt.transform;
            nextLevelUnlockPrompt.anchorMin = nextLevelUnlockPrompt.anchorMax = new Vector2(0.5f, 0.5f);
            nextLevelUnlockPrompt.pivot = new Vector2(0.5f, 0.5f);
            nextLevelUnlockPrompt.sizeDelta = new Vector2(1160f, 150f);

            Text text = prompt.GetComponent<Text>();
            text.text = "分数已达到开启下一关的条件，可继续前往挑战下一关";
            text.font = finalScoreText != null && finalScoreText.font != null
                ? finalScoreText.font
                : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 36;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 24;
            text.resizeTextMaxSize = 36;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            text.color = Color.white;

            nextLevelUnlockTextEffect = prompt.AddComponent<TutorialGradientTextEffect>();
            Outline outline = prompt.AddComponent<Outline>();
            outline.effectColor = new Color32(50, 43, 88, 235);
            outline.effectDistance = new Vector2(2f, -2f);
            Shadow shadow = prompt.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.03f, 0.025f, 0.08f, 0.72f);
            shadow.effectDistance = new Vector2(4f, -5f);
            nextLevelUnlockNotice.SetActive(false);
        }

        private void HideNextLevelUnlockNotice()
        {
            if (nextLevelUnlockNotice != null)
                nextLevelUnlockNotice.SetActive(false);
            if (nextLevelUnlockGroup != null)
                nextLevelUnlockGroup.alpha = 0f;
        }

        private static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
        }

        private void OnDestroy()
        {
            if (spellInventory != null)
                spellInventory.Changed -= RefreshSpellSlots;
        }

        private void CreateSpellSlots()
        {
            if (hudPanel == null)
                return;

            Transform oldRoot = hudPanel.transform.Find("SpellInventory");
            if (oldRoot != null)
                Destroy(oldRoot.gameObject);

            GameObject inventoryObject = new GameObject("SpellInventory", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            inventoryObject.transform.SetParent(hudPanel.transform, false);
            RectTransform inventoryRect = (RectTransform)inventoryObject.transform;
            inventoryRect.anchorMin = new Vector2(1f, 0f);
            inventoryRect.anchorMax = new Vector2(1f, 0f);
            inventoryRect.pivot = new Vector2(1f, 0f);
            inventoryRect.anchoredPosition = new Vector2(-24f, 22f);
            float panelWidth = SpellSlotCount * SpellSlotWidth
                + (SpellSlotCount - 1) * SpellSlotSpacing
                + SpellPanelHorizontalPadding * 2f;
            float panelHeight = SpellSlotHeight + SpellPanelVerticalPadding * 2f;
            inventoryRect.sizeDelta = new Vector2(panelWidth, panelHeight);

            Image panelImage = inventoryObject.GetComponent<Image>();
            panelImage.color = new Color(0.18f, 0.09f, 0.035f, 0.84f);
            panelImage.raycastTarget = true;

            HorizontalLayoutGroup layout = inventoryObject.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(
                SpellPanelHorizontalPadding,
                SpellPanelHorizontalPadding,
                SpellPanelVerticalPadding,
                SpellPanelVerticalPadding);
            layout.spacing = SpellSlotSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            spellSlots.Clear();
            for (int i = 0; i < SpellSlotCount; i++)
                CreateSpellSlot(inventoryObject.transform, i);
            RefreshSpellSlots();
        }

        private void CreateSpellSlot(Transform parent, int index)
        {
            GameObject slotObject = new GameObject(
                $"SpellSlot_{index + 1}",
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement),
                typeof(SpellSlotView));
            slotObject.transform.SetParent(parent, false);
            RectTransform slotRect = (RectTransform)slotObject.transform;
            slotRect.sizeDelta = new Vector2(SpellSlotWidth, SpellSlotHeight);

            LayoutElement slotLayout = slotObject.GetComponent<LayoutElement>();
            slotLayout.minWidth = SpellSlotWidth;
            slotLayout.preferredWidth = SpellSlotWidth;
            slotLayout.flexibleWidth = 0f;
            slotLayout.minHeight = SpellSlotHeight;
            slotLayout.preferredHeight = SpellSlotHeight;
            slotLayout.flexibleHeight = 0f;

            Image background = slotObject.GetComponent<Image>();
            background.raycastTarget = true;
            Outline border = slotObject.AddComponent<Outline>();
            border.effectColor = new Color(0.88f, 0.69f, 0.3f, 0.9f);
            border.effectDistance = new Vector2(1.25f, -1.25f);

            Text shortcut = CreateSlotText(slotObject.transform, "Shortcut", 12, TextAnchor.UpperLeft,
                new Vector2(64f, 22f), new Vector2(4f, 25f));
            shortcut.text = (index + 1).ToString();
            shortcut.color = new Color(1f, 0.86f, 0.48f, 0.95f);

            GameObject iconObject = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(slotObject.transform, false);
            RectTransform iconRect = (RectTransform)iconObject.transform;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 9f);
            iconRect.sizeDelta = new Vector2(40f, 40f);
            Image icon = iconObject.GetComponent<Image>();
            icon.type = Image.Type.Simple;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            Text name = CreateSlotText(slotObject.transform, "Name", 10, TextAnchor.MiddleCenter, new Vector2(68f, 18f), new Vector2(0f, -25f));
            Text count = CreateSlotText(slotObject.transform, "Count", 14, TextAnchor.UpperRight, new Vector2(29f, 19f), new Vector2(21f, 25f));
            SpellSlotView view = slotObject.GetComponent<SpellSlotView>();
            view.Initialize(spellInventory, index, background, icon, name, count);
            spellSlots.Add(view);
        }

        private static string GetWeaponDisplayName(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.MachineGun: return "豌豆机枪";
                case WeaponType.CornCannon: return "玉米大炮";
                default: return "普通豌豆";
            }
        }

        private static Text CreateSlotText(Transform parent, string objectName, int fontSize, TextAnchor alignment, Vector2 size, Vector2 position)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)textObject.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Text text = textObject.GetComponent<Text>();
            // Unity 6 的运行时内置字体名称是 LegacyRuntime.ttf。
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private void RefreshSpellSlots()
        {
            for (int i = 0; i < spellSlots.Count; i++)
                spellSlots[i]?.Refresh();
        }
    }
}
