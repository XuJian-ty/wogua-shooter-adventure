using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WoguaShooter.UI
{
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-40)]
    public class LeaderboardUIController : MonoBehaviour
    {
        private readonly List<LeaderboardRowView> rows = new List<LeaderboardRowView>();

        private GameObject leaderboardPanel;
        private GameObject celebrationPanel;
        private RectTransform leaderboardDialog;
        private RectTransform celebrationDialog;
        private CanvasGroup leaderboardCanvasGroup;
        private CanvasGroup celebrationCanvasGroup;
        private Text leaderboardLevelText;
        private Text celebrationSubtitle;
        private Text celebrationScoreText;
        private Text validationText;
        private InputField nameInput;
        private Button backButton;
        private Button levelSelectButton;
        private GameObject levelSelectionPopup;
        private Button popupLevelOneButton;
        private Button popupLevelTwoButton;
        private Button submitButton;
        private Font chineseFont;
        private Coroutine panelAnimation;
        private int displayedLevelId = 1;
        private int pendingLevelId = 1;
        private int pendingScore;

        private void Awake()
        {
            chineseFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei UI", "Microsoft YaHei", "SimSun", "Arial" }, 30);
            BuildInterface();
            BindEvents();
            HideAll();
        }

        public void ShowLeaderboard(int levelId)
        {
            displayedLevelId = Mathf.Max(1, levelId);
            celebrationPanel.SetActive(false);
            levelSelectionPopup?.SetActive(false);
            leaderboardPanel.SetActive(true);
            RefreshLeaderboard();
            PlayOpenAnimation(leaderboardDialog, leaderboardCanvasGroup);
        }

        public bool TryShowScoreEntry(int levelId, int score)
        {
            int safeLevelId = Mathf.Max(1, levelId);
            if (!LeaderboardService.Qualifies(safeLevelId, score))
                return false;

            pendingLevelId = safeLevelId;
            pendingScore = Mathf.Max(0, score);
            leaderboardPanel.SetActive(false);
            celebrationPanel.SetActive(true);
            celebrationSubtitle.text = $"你的得分进入{GetLevelName(safeLevelId)}前五";
            celebrationScoreText.text = $"{pendingScore} 分";
            validationText.text = string.Empty;
            nameInput.SetTextWithoutNotify(string.Empty);
            PlayOpenAnimation(celebrationDialog, celebrationCanvasGroup);
            StartCoroutine(FocusNameInput());
            return true;
        }

        public void HideAll()
        {
            leaderboardPanel?.SetActive(false);
            celebrationPanel?.SetActive(false);
            levelSelectionPopup?.SetActive(false);
        }

        private void BindEvents()
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(ReturnToMainMenu);
            levelSelectButton.onClick.RemoveAllListeners();
            levelSelectButton.onClick.AddListener(ToggleLevelSelection);
            popupLevelOneButton.onClick.RemoveAllListeners();
            popupLevelOneButton.onClick.AddListener(() => SelectLeaderboardLevel(1));
            popupLevelTwoButton.onClick.RemoveAllListeners();
            popupLevelTwoButton.onClick.AddListener(() => SelectLeaderboardLevel(2));
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(SubmitPendingScore);
            nameInput.onValueChanged.RemoveAllListeners();
            nameInput.onValueChanged.AddListener(_ => validationText.text = string.Empty);
            nameInput.onSubmit.RemoveAllListeners();
            nameInput.onSubmit.AddListener(_ => SubmitPendingScore());
        }

        private void ToggleLevelSelection()
        {
            if (levelSelectionPopup != null)
                levelSelectionPopup.SetActive(!levelSelectionPopup.activeSelf);
        }

        private void SelectLeaderboardLevel(int levelId)
        {
            levelSelectionPopup?.SetActive(false);
            ShowLeaderboard(levelId);
        }

        private void ReturnToMainMenu()
        {
            HideAll();
            FindFirstObjectByType<FlowUIController>(FindObjectsInactive.Include)?.ReturnFromLeaderboard();
        }

        private void SubmitPendingScore()
        {
            if (celebrationPanel == null || !celebrationPanel.activeSelf)
                return;

            if (!LeaderboardService.TryAddScore(pendingLevelId, nameInput.text, pendingScore, out string error))
            {
                validationText.text = error;
                nameInput.Select();
                nameInput.ActivateInputField();
                return;
            }

            celebrationPanel.SetActive(false);
            UIController gameUI = FindFirstObjectByType<UIController>(FindObjectsInactive.Include);
            gameUI?.ShowGameOver(pendingScore);
        }

        private void RefreshLeaderboard()
        {
            leaderboardLevelText.text = GetLevelName(displayedLevelId);
            List<LeaderboardEntry> entries = LeaderboardService.GetEntries(displayedLevelId);
            for (int i = 0; i < rows.Count; i++)
            {
                if (i < entries.Count)
                    rows[i].SetEntry(i + 1, entries[i]);
                else
                    rows[i].SetEmpty(i + 1);
            }
        }

        private static string GetLevelName(int levelId)
        {
            return $"第{Mathf.Max(1, levelId)}关";
        }

        private IEnumerator FocusNameInput()
        {
            yield return null;
            if (nameInput != null && celebrationPanel.activeSelf)
            {
                nameInput.Select();
                nameInput.ActivateInputField();
            }
        }

        private void PlayOpenAnimation(RectTransform dialog, CanvasGroup canvasGroup)
        {
            if (panelAnimation != null)
                StopCoroutine(panelAnimation);
            panelAnimation = StartCoroutine(AnimatePanel(dialog, canvasGroup));
        }

        private IEnumerator AnimatePanel(RectTransform dialog, CanvasGroup canvasGroup)
        {
            const float duration = 0.24f;
            float elapsed = 0f;
            dialog.localScale = Vector3.one * 0.88f;
            canvasGroup.alpha = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - progress, 3f);
                dialog.localScale = Vector3.one * Mathf.Lerp(0.88f, 1f, eased);
                canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, progress);
                yield return null;
            }

            dialog.localScale = Vector3.one;
            canvasGroup.alpha = 1f;
            panelAnimation = null;
        }

        private void BuildInterface()
        {
            if (transform.Find("LeaderboardPanel") != null)
            {
                BindExistingInterface();
                return;
            }

            leaderboardPanel = CreateOverlay("LeaderboardPanel", new Color(0f, 0f, 0f, 0.72f), out leaderboardCanvasGroup);
            leaderboardDialog = CreateDialog(leaderboardPanel.transform, "Dialog", new Vector2(820f, 570f));
            CreateText(leaderboardDialog, "Title", "排行榜", 42, new Vector2(0f, 232f), new Vector2(500f, 58f), TextAnchor.MiddleCenter, new Color32(255, 230, 137, 255));

            levelSelectButton = CreateButton(leaderboardDialog, "LevelOneButton", "选择关卡", new Vector2(-280f, 166f), new Vector2(180f, 50f), true);
            StyleLevelSelectButton(levelSelectButton);
            leaderboardLevelText = CreateText(leaderboardDialog, "LevelTitle", "第1关", 28, new Vector2(0f, 166f), new Vector2(300f, 50f), TextAnchor.MiddleCenter, Color.white);
            CreateText(leaderboardDialog, "RankHeader", "名次", 20, new Vector2(-312f, 112f), new Vector2(90f, 32f), TextAnchor.MiddleCenter, new Color32(188, 212, 194, 255));
            CreateText(leaderboardDialog, "NameHeader", "名字", 20, new Vector2(-70f, 112f), new Vector2(340f, 32f), TextAnchor.MiddleLeft, new Color32(188, 212, 194, 255));
            CreateText(leaderboardDialog, "ScoreHeader", "得分", 20, new Vector2(260f, 112f), new Vector2(130f, 32f), TextAnchor.MiddleRight, new Color32(188, 212, 194, 255));
            CreateImage(leaderboardDialog, "HeaderLine", new Vector2(0f, 91f), new Vector2(710f, 2f), new Color32(211, 177, 91, 180));

            for (int index = 0; index < LeaderboardService.MaximumEntries; index++)
                rows.Add(CreateLeaderboardRow(leaderboardDialog, index, 62f - index * 55f));

            backButton = CreateButton(leaderboardDialog, "BackButton", "返回主界面", new Vector2(0f, -238f), new Vector2(260f, 58f), false);
            CreateLevelSelectionPopup();

            celebrationPanel = CreateOverlay("LeaderboardCelebrationPanel", new Color(0f, 0f, 0f, 0.76f), out celebrationCanvasGroup);
            celebrationDialog = CreateDialog(celebrationPanel.transform, "Dialog", new Vector2(650f, 450f));
            CreateDecoration(celebrationDialog, new Vector2(-276f, 174f), 16f);
            CreateDecoration(celebrationDialog, new Vector2(278f, 144f), 11f);
            CreateDecoration(celebrationDialog, new Vector2(-250f, -154f), 10f);
            CreateDecoration(celebrationDialog, new Vector2(270f, -176f), 15f);
            CreateText(celebrationDialog, "Title", "恭喜入榜", 42, new Vector2(0f, 164f), new Vector2(500f, 58f), TextAnchor.MiddleCenter, new Color32(255, 224, 116, 255));
            celebrationSubtitle = CreateText(celebrationDialog, "Subtitle", string.Empty, 23, new Vector2(0f, 106f), new Vector2(540f, 40f), TextAnchor.MiddleCenter, new Color32(224, 239, 224, 255));
            celebrationScoreText = CreateText(celebrationDialog, "Score", "0 分", 38, new Vector2(0f, 57f), new Vector2(430f, 54f), TextAnchor.MiddleCenter, Color.white);
            CreateText(celebrationDialog, "Prompt", "为这次得分取个名字", 21, new Vector2(0f, 12f), new Vector2(430f, 36f), TextAnchor.MiddleCenter, new Color32(188, 212, 194, 255));
            nameInput = CreateInputField(celebrationDialog, new Vector2(0f, -42f), new Vector2(430f, 56f));
            validationText = CreateText(celebrationDialog, "ValidationText", string.Empty, 18, new Vector2(0f, -82f), new Vector2(500f, 30f), TextAnchor.MiddleCenter, new Color32(255, 142, 126, 255));
            submitButton = CreateButton(celebrationDialog, "SubmitButton", "保存成绩", new Vector2(0f, -146f), new Vector2(270f, 60f), true);
        }

        private void BindExistingInterface()
        {
            leaderboardPanel = transform.Find("LeaderboardPanel").gameObject;
            leaderboardCanvasGroup = leaderboardPanel.GetComponent<CanvasGroup>();
            leaderboardDialog = leaderboardPanel.transform.Find("Dialog") as RectTransform;
            leaderboardLevelText = leaderboardDialog.Find("LevelTitle").GetComponent<Text>();
            levelSelectButton = leaderboardDialog.Find("LevelOneButton").GetComponent<Button>();
            backButton = leaderboardDialog.Find("BackButton").GetComponent<Button>();

            Text selectorLabel = levelSelectButton.GetComponentInChildren<Text>(true);
            if (selectorLabel != null)
                selectorLabel.text = "选择关卡";
            RectTransform selectorRect = levelSelectButton.transform as RectTransform;
            if (selectorRect != null)
                selectorRect.sizeDelta = new Vector2(180f, 50f);
            StyleLevelSelectButton(levelSelectButton);

            RectTransform scoreHeader = leaderboardDialog.Find("ScoreHeader") as RectTransform;
            SetColumnRect(scoreHeader, 260f, 130f);

            rows.Clear();
            for (int index = 0; index < LeaderboardService.MaximumEntries; index++)
            {
                Transform row = leaderboardDialog.Find($"Row{index + 1}");
                RectTransform scoreRect = row.Find("Score") as RectTransform;
                SetColumnRect(scoreRect, 260f, 130f);
                rows.Add(new LeaderboardRowView(
                    row.Find("Rank").GetComponent<Text>(),
                    row.Find("Name").GetComponent<Text>(),
                    row.Find("Score").GetComponent<Text>()));
            }

            CreateLevelSelectionPopup();

            celebrationPanel = transform.Find("LeaderboardCelebrationPanel").gameObject;
            celebrationCanvasGroup = celebrationPanel.GetComponent<CanvasGroup>();
            celebrationDialog = celebrationPanel.transform.Find("Dialog") as RectTransform;
            celebrationSubtitle = celebrationDialog.Find("Subtitle").GetComponent<Text>();
            celebrationScoreText = celebrationDialog.Find("Score").GetComponent<Text>();
            validationText = celebrationDialog.Find("ValidationText").GetComponent<Text>();
            nameInput = celebrationDialog.Find("NameInput").GetComponent<InputField>();
            submitButton = celebrationDialog.Find("SubmitButton").GetComponent<Button>();

            Text[] labels = GetComponentsInChildren<Text>(true);
            for (int i = 0; i < labels.Length; i++)
                labels[i].font = chineseFont;
        }

        private GameObject CreateOverlay(string objectName, Color color, out CanvasGroup canvasGroup)
        {
            GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            root.transform.SetParent(transform, false);
            Stretch(root.GetComponent<RectTransform>());
            Image image = root.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            canvasGroup = root.GetComponent<CanvasGroup>();
            return root;
        }

        private RectTransform CreateDialog(Transform parent, string objectName, Vector2 size)
        {
            GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetRect(rect, Vector2.zero, size);
            Image image = root.GetComponent<Image>();
            image.color = new Color32(26, 66, 50, 252);
            image.raycastTarget = true;
            Outline outline = root.GetComponent<Outline>();
            outline.effectColor = new Color32(214, 177, 86, 235);
            outline.effectDistance = new Vector2(2f, -2f);
            return rect;
        }

        private LeaderboardRowView CreateLeaderboardRow(Transform parent, int index, float y)
        {
            Color background = index % 2 == 0
                ? new Color32(42, 91, 68, 225)
                : new Color32(34, 77, 58, 225);
            Image rowBackground = CreateImage(parent, $"Row{index + 1}", new Vector2(0f, y), new Vector2(710f, 46f), background);
            Text rank = CreateText(rowBackground.transform, "Rank", (index + 1).ToString(), 23, new Vector2(-312f, 0f), new Vector2(70f, 42f), TextAnchor.MiddleCenter, GetRankColor(index));
            Text playerName = CreateText(rowBackground.transform, "Name", "暂无记录", 23, new Vector2(-68f, 0f), new Vector2(350f, 42f), TextAnchor.MiddleLeft, new Color32(222, 234, 224, 255));
            Text score = CreateText(rowBackground.transform, "Score", "--", 23, new Vector2(260f, 0f), new Vector2(130f, 42f), TextAnchor.MiddleRight, Color.white);
            return new LeaderboardRowView(rank, playerName, score);
        }

        private void CreateLevelSelectionPopup()
        {
            Transform existing = leaderboardDialog.Find("LevelSelectionPopup");
            if (existing != null)
            {
                levelSelectionPopup = existing.gameObject;
                popupLevelOneButton = existing.Find("LevelOneOption").GetComponent<Button>();
                popupLevelTwoButton = existing.Find("LevelTwoOption").GetComponent<Button>();
                SetButtonLabel(popupLevelOneButton, "第1关");
                SetButtonLabel(popupLevelTwoButton, "第2关");
                levelSelectionPopup.SetActive(false);
                return;
            }

            levelSelectionPopup = new GameObject(
                "LevelSelectionPopup",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Outline));
            levelSelectionPopup.transform.SetParent(leaderboardDialog, false);
            RectTransform popupRect = levelSelectionPopup.GetComponent<RectTransform>();
            SetRect(popupRect, new Vector2(-268f, 65f), new Vector2(246f, 150f));
            Image popupImage = levelSelectionPopup.GetComponent<Image>();
            popupImage.color = new Color32(24, 58, 45, 255);
            Outline outline = levelSelectionPopup.GetComponent<Outline>();
            outline.effectColor = new Color32(218, 181, 88, 235);
            outline.effectDistance = new Vector2(2f, -2f);

            popupLevelOneButton = CreateButton(
                levelSelectionPopup.transform,
                "LevelOneOption",
                "第1关",
                new Vector2(0f, 35f),
                new Vector2(210f, 52f),
                true);
            popupLevelTwoButton = CreateButton(
                levelSelectionPopup.transform,
                "LevelTwoOption",
                "第2关",
                new Vector2(0f, -35f),
                new Vector2(210f, 52f),
                false);
            levelSelectionPopup.SetActive(false);
        }

        private static void SetColumnRect(RectTransform rect, float x, float width)
        {
            if (rect == null)
                return;

            Vector2 position = rect.anchoredPosition;
            position.x = x;
            rect.anchoredPosition = position;
            Vector2 size = rect.sizeDelta;
            size.x = width;
            rect.sizeDelta = size;
        }

        private static void StyleLevelSelectButton(Button button)
        {
            if (button == null)
                return;

            Image rootImage = button.GetComponent<Image>();
            if (rootImage != null)
                rootImage.color = Color.clear;

            Transform fill = button.transform.Find("Fill");
            if (fill != null && fill.TryGetComponent(out Image fillImage))
                fillImage.color = new Color32(55, 118, 75, 255);
        }

        private static void SetButtonLabel(Button button, string label)
        {
            Text text = button != null ? button.GetComponentInChildren<Text>(true) : null;
            if (text != null)
                text.text = label;
        }

        private Button CreateButton(Transform parent, string objectName, string labelText, Vector2 position, Vector2 size, bool primary)
        {
            GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(UIButtonMotion));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetRect(rect, position, size);
            Image border = root.GetComponent<Image>();
            border.color = primary ? new Color32(224, 185, 83, 255) : new Color32(164, 185, 163, 245);
            Button button = root.GetComponent<Button>();
            button.targetGraphic = border;

            Image fill = CreateImage(root.transform, "Fill", Vector2.zero, size - new Vector2(5f, 5f),
                primary ? new Color32(55, 118, 75, 255) : new Color32(48, 83, 65, 255));
            fill.raycastTarget = false;
            Text label = CreateText(root.transform, "Label", labelText, 24, Vector2.zero, size - new Vector2(16f, 8f), TextAnchor.MiddleCenter, Color.white);
            label.raycastTarget = false;
            return button;
        }

        private InputField CreateInputField(Transform parent, Vector2 position, Vector2 size)
        {
            GameObject root = new GameObject("NameInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(InputField));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetRect(rect, position, size);
            Image background = root.GetComponent<Image>();
            background.color = new Color32(238, 244, 235, 255);

            Text placeholder = CreateText(root.transform, "Placeholder", "请输入1-6个字", 22, Vector2.zero, size - new Vector2(34f, 10f), TextAnchor.MiddleLeft, new Color32(98, 121, 103, 180));
            placeholder.fontStyle = FontStyle.Italic;
            Text inputText = CreateText(root.transform, "Text", string.Empty, 23, Vector2.zero, size - new Vector2(34f, 10f), TextAnchor.MiddleLeft, new Color32(28, 60, 45, 255));

            InputField input = root.GetComponent<InputField>();
            input.targetGraphic = background;
            input.textComponent = inputText;
            input.placeholder = placeholder;
            input.characterLimit = LeaderboardService.MaximumNameLength;
            input.lineType = InputField.LineType.SingleLine;
            input.contentType = InputField.ContentType.Standard;
            input.caretColor = new Color32(36, 92, 61, 255);
            input.selectionColor = new Color32(106, 168, 119, 120);
            return input;
        }

        private Text CreateText(Transform parent, string objectName, string content, int fontSize, Vector2 position, Vector2 size, TextAnchor alignment, Color color)
        {
            GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetRect(rect, position, size);
            Text text = root.GetComponent<Text>();
            text.text = content;
            text.font = chineseFont;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateImage(Transform parent, string objectName, Vector2 position, Vector2 size, Color color)
        {
            GameObject root = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(parent, false);
            RectTransform rect = root.GetComponent<RectTransform>();
            SetRect(rect, position, size);
            Image image = root.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void CreateDecoration(Transform parent, Vector2 position, float size)
        {
            Image decoration = CreateImage(parent, "Decoration", position, new Vector2(size, size), new Color32(255, 220, 105, 220));
            decoration.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        private static Color GetRankColor(int index)
        {
            if (index == 0)
                return new Color32(255, 220, 90, 255);
            if (index == 1)
                return new Color32(221, 230, 225, 255);
            if (index == 2)
                return new Color32(222, 164, 104, 255);
            return new Color32(176, 200, 182, 255);
        }

        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private sealed class LeaderboardRowView
        {
            private readonly Text rankText;
            private readonly Text nameText;
            private readonly Text scoreText;

            public LeaderboardRowView(Text rankText, Text nameText, Text scoreText)
            {
                this.rankText = rankText;
                this.nameText = nameText;
                this.scoreText = scoreText;
            }

            public void SetEntry(int rank, LeaderboardEntry entry)
            {
                rankText.text = rank.ToString();
                nameText.text = entry.playerName;
                nameText.color = Color.white;
                scoreText.text = entry.score.ToString();
            }

            public void SetEmpty(int rank)
            {
                rankText.text = rank.ToString();
                nameText.text = "暂无记录";
                nameText.color = new Color32(151, 177, 157, 255);
                scoreText.text = "--";
            }
        }
    }
}
