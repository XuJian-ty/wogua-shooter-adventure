using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using WoguaShooter.Config;
using WoguaShooter.Core;

namespace WoguaShooter.UI
{
    public class TutorialGuideController : MonoBehaviour
    {
        private static readonly Vector2 PromptPosition = new Vector2(0f, 150f);

        private enum Step
        {
            Jump,
            DoubleJump,
            Glide,
            UseItem,
            DiscardItem,
            Score,
            StompScore,
            FinalMessage
        }

        private GameObject root;
        private GameObject choiceRoot;
        private RectTransform promptRect;
        private CanvasGroup promptGroup;
        private CanvasGroup choiceGroup;
        private Text instructionText;
        private Text choiceText;
        private TutorialGradientTextEffect gradientTextEffect;
        private Canvas canvas;
        private PlayerController player;
        private SpellInventory inventory;
        private SpawnManager spawner;
        private GameManager manager;
        private Coroutine transitionRoutine;
        private Step currentStep;
        private bool initialized;
        private bool active;
        private bool choosingTutorial;
        private bool acceptingAction;
        private bool promptVisible;

        public void Initialize(Canvas targetCanvas, Font font)
        {
            if (initialized || targetCanvas == null)
                return;

            initialized = true;
            canvas = targetCanvas;
            root = new GameObject("FirstLevelTutorial", typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)root.transform);

            GameObject prompt = new GameObject("FloatingPrompt", typeof(RectTransform), typeof(CanvasGroup));
            prompt.transform.SetParent(root.transform, false);
            promptRect = (RectTransform)prompt.transform;
            promptRect.anchorMin = promptRect.anchorMax = new Vector2(0.5f, 0.5f);
            promptRect.pivot = new Vector2(0.5f, 0.5f);
            promptRect.anchoredPosition = PromptPosition;
            promptRect.sizeDelta = new Vector2(1050f, 170f);
            promptGroup = prompt.GetComponent<CanvasGroup>();
            promptGroup.interactable = false;
            promptGroup.blocksRaycasts = false;

            instructionText = CreateText(
                prompt.transform,
                "GradientText",
                font,
                36,
                Vector2.zero,
                Color.white);
            gradientTextEffect = instructionText.gameObject.AddComponent<TutorialGradientTextEffect>();
            Outline textOutline = instructionText.gameObject.AddComponent<Outline>();
            textOutline.effectColor = new Color32(50, 43, 88, 225);
            textOutline.effectDistance = new Vector2(2f, -2f);
            Shadow textShadow = instructionText.gameObject.AddComponent<Shadow>();
            textShadow.effectColor = new Color(0.03f, 0.025f, 0.08f, 0.68f);
            textShadow.effectDistance = new Vector2(3f, -4f);

            choiceRoot = new GameObject("TutorialChoice", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            choiceRoot.transform.SetParent(root.transform, false);
            RectTransform choiceRect = (RectTransform)choiceRoot.transform;
            choiceRect.anchorMin = choiceRect.anchorMax = new Vector2(0.5f, 0.5f);
            choiceRect.pivot = new Vector2(0.5f, 0.5f);
            choiceRect.anchoredPosition = new Vector2(0f, 95f);
            choiceRect.sizeDelta = new Vector2(720f, 250f);
            choiceRoot.GetComponent<Image>().color = new Color(0.08f, 0.055f, 0.095f, 0.86f);
            choiceGroup = choiceRoot.GetComponent<CanvasGroup>();
            choiceGroup.interactable = true;
            choiceGroup.blocksRaycasts = true;

            choiceText = CreateChoiceText(
                choiceRoot.transform,
                "是否进行新手教程？",
                34,
                new Vector2(0f, 65f),
                new Vector2(650f, 70f),
                new Color32(255, 239, 191, 255));
            Outline choiceOutline = choiceText.gameObject.AddComponent<Outline>();
            choiceOutline.effectColor = new Color(0.03f, 0.02f, 0.07f, 0.9f);
            choiceOutline.effectDistance = new Vector2(2f, -2f);

            Button confirmButton = CreateChoiceButton(
                choiceRoot.transform,
                "√",
                new Vector2(-105f, -58f),
                new Color32(71, 151, 102, 255));
            confirmButton.onClick.AddListener(ChooseTutorial);

            Button skipButton = CreateChoiceButton(
                choiceRoot.transform,
                "×",
                new Vector2(105f, -58f),
                new Color32(135, 77, 82, 255));
            skipButton.onClick.AddListener(SkipTutorial);

            root.SetActive(false);
        }

        public void BeginFirstLevel(
            PlayerController targetPlayer,
            SpellInventory targetInventory,
            SpawnManager targetSpawner)
        {
            StopTutorial();
            manager = GameManager.Instance;

            if (!initialized || targetPlayer == null || targetInventory == null)
                return;

            player = targetPlayer;
            inventory = targetInventory;
            spawner = targetSpawner;
            player.SetTutorialInputEnabled(true);
            inventory.InteractionEnabled = true;
            inventory.UseInteractionEnabled = true;
            inventory.DropInteractionEnabled = true;
            inventory.MinimumItemCountToPreserveFromUse = 0;
            manager?.PauseGameWithoutPanel();
            root.transform.SetAsLastSibling();
            root.SetActive(true);
            promptGroup.alpha = 0f;
            promptVisible = false;
            choiceRoot.SetActive(true);
            choiceRoot.transform.SetAsLastSibling();
            choosingTutorial = true;
        }

        private void ChooseTutorial()
        {
            if (!choosingTutorial || player == null || inventory == null)
                return;

            choosingTutorial = false;
            choiceRoot.SetActive(false);
            player.Jumped += OnJumped;
            player.GlideStarted += OnGlideStarted;
            inventory.SlotUsed += OnSlotUsed;
            inventory.SlotDropped += OnSlotDropped;
            Enemy.Defeated += OnEnemyDefeated;

            active = true;
            manager?.SetTutorialActive(false);
            spawner?.BeginTutorialMode();
            player.SetTutorialActionPermissions(false, false, false);
            inventory.UseInteractionEnabled = false;
            inventory.DropInteractionEnabled = false;
            inventory.MinimumItemCountToPreserveFromUse = 1;
            root.transform.SetAsLastSibling();
            root.SetActive(true);
            transitionRoutine = StartCoroutine(ShowFirstStepRoutine());
            manager?.ContinueGame();
        }

        private void SkipTutorial()
        {
            if (!choosingTutorial)
                return;

            choosingTutorial = false;
            active = false;
            acceptingAction = false;
            promptVisible = false;
            choiceRoot.SetActive(false);
            root.SetActive(false);
            spawner?.EndTutorialMode();
            manager?.SetTutorialActive(false);
            player?.SetTutorialInputEnabled(true);
            if (inventory != null)
            {
                inventory.InteractionEnabled = true;
                inventory.UseInteractionEnabled = true;
                inventory.DropInteractionEnabled = true;
                inventory.MinimumItemCountToPreserveFromUse = 0;
            }
            manager?.ContinueGame();
        }

        public void StopTutorial()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
                transitionRoutine = null;
            }

            PlayerController oldPlayer = player;
            SpellInventory oldInventory = inventory;
            bool wasChoosingTutorial = choosingTutorial;
            active = false;
            choosingTutorial = false;
            acceptingAction = false;
            promptVisible = false;
            Unsubscribe();
            spawner?.EndTutorialMode();
            manager?.SetTutorialActive(false);

            oldPlayer?.SetTutorialInputEnabled(true);
            if (oldInventory != null)
            {
                oldInventory.InteractionEnabled = true;
                oldInventory.UseInteractionEnabled = true;
                oldInventory.DropInteractionEnabled = true;
                oldInventory.MinimumItemCountToPreserveFromUse = 0;
            }
            if (root != null)
                root.SetActive(false);

            if (wasChoosingTutorial && manager != null && manager.State == GameState.Paused)
                manager.ContinueGame();

            player = null;
            inventory = null;
            spawner = null;
            manager = null;
        }

        private void Update()
        {
            if (!active || !acceptingAction || !promptVisible || promptRect == null)
                return;

            float time = Time.unscaledTime;
            float bob = Mathf.Sin(time * 2.35f) * 5f;
            float sway = Mathf.Sin(time * 1.55f) * 1.1f;
            float pulse = 1f + Mathf.Sin(time * 2.7f) * 0.01f;
            promptRect.anchoredPosition = PromptPosition + Vector2.up * bob;
            promptRect.localRotation = Quaternion.Euler(0f, 0f, sway);
            promptRect.localScale = Vector3.one * pulse;
            gradientTextEffect?.SetSnakeMotion(
                7f + Mathf.Sin(time * 1.8f) * 1.2f,
                time * 2.35f,
                2.2f);
        }

        private void OnJumped(int jumpNumber)
        {
            if (!acceptingAction)
                return;

            if (currentStep == Step.Jump && jumpNumber == 1)
                QueueStep(Step.DoubleJump);
            else if (currentStep == Step.DoubleJump && jumpNumber >= 2)
                QueueStep(Step.Glide);
        }

        private void OnGlideStarted()
        {
            if (acceptingAction && currentStep == Step.Glide)
                QueueStep(Step.UseItem);
        }

        private void OnSlotUsed(int ignoredIndex, SpellType ignoredType)
        {
            if (acceptingAction && currentStep == Step.UseItem)
                QueueStep(Step.DiscardItem);
        }

        private void OnSlotDropped(int ignoredIndex, SpellType ignoredType)
        {
            if (acceptingAction && currentStep == Step.DiscardItem)
            {
                if (inventory != null)
                {
                    inventory.UseInteractionEnabled = true;
                    inventory.DropInteractionEnabled = true;
                    inventory.MinimumItemCountToPreserveFromUse = 0;
                }
                QueueStep(Step.Score);
            }
        }

        private void OnEnemyDefeated(Enemy enemy, bool stomped)
        {
            if (!acceptingAction || enemy == null || !enemy.IsTutorialTarget)
                return;

            if (currentStep == Step.Score && enemy.IsFloating && !stomped)
            {
                spawner?.ClearTutorialChallenge(enemy);
                QueueStep(Step.StompScore);
            }
            else if (currentStep == Step.StompScore && !enemy.IsFloating && stomped)
            {
                spawner?.ClearTutorialChallenge(enemy);
                QueueStep(Step.FinalMessage);
            }
        }

        private IEnumerator ShowFirstStepRoutine()
        {
            yield return EnterStep(Step.Jump);
            transitionRoutine = null;
        }

        private void QueueStep(Step nextStep)
        {
            if (!active || transitionRoutine != null)
                return;

            acceptingAction = false;
            ApplyTransitionPermissions(nextStep);
            transitionRoutine = StartCoroutine(TransitionToStepRoutine(nextStep));
        }

        private IEnumerator TransitionToStepRoutine(Step nextStep)
        {
            yield return ExitPrompt();

            if (nextStep == Step.Score)
                PrepareCombatChallenge();
            if (nextStep == Step.UseItem)
                EnsureTutorialItems();

            yield return EnterStep(nextStep);

            if (nextStep == Step.FinalMessage)
            {
                yield return new WaitForSecondsRealtime(3f);
                yield return ExitPrompt();
                CompleteTutorial();
                yield break;
            }

            transitionRoutine = null;
        }

        private void QueueFinish()
        {
            if (!active || transitionRoutine != null)
                return;

            acceptingAction = false;
            transitionRoutine = StartCoroutine(FinishRoutine());
        }

        private IEnumerator FinishRoutine()
        {
            yield return ExitPrompt();
            CompleteTutorial();
        }

        private IEnumerator EnterStep(Step step)
        {
            acceptingAction = false;
            currentStep = step;
            ConfigurePrompt(step);
            promptVisible = true;

            float travel = GetTravelDistance();
            Vector2 start = PromptPosition + new Vector2(travel, 330f);
            Vector2 control = PromptPosition + new Vector2(travel * 0.5f, -65f);
            const float duration = 2.1f;
            promptGroup.alpha = 0f;
            promptRect.anchoredPosition = start;
            promptRect.localScale = Vector3.one * 0.94f;
            promptRect.localRotation = Quaternion.Euler(0f, 0f, 5f);

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = Mathf.SmoothStep(0f, 1f, t);
                Vector2 pathPosition = QuadraticBezier(start, control, PromptPosition, eased);
                float travelWave = Mathf.Sin(t * Mathf.PI * 3.2f) * (1f - eased) * 24f;
                promptRect.anchoredPosition = pathPosition + Vector2.up * travelWave;
                promptRect.localScale = Vector3.one * Mathf.Lerp(0.94f, 1f, eased);
                promptRect.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.Lerp(5f, 0f, eased) + Mathf.Sin(t * Mathf.PI * 4f) * (1f - eased) * 2.4f);
                promptGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 3.2f));
                gradientTextEffect?.SetSnakeMotion(
                    Mathf.Lerp(14f, 7f, eased),
                    Time.unscaledTime * 3.1f + t * 2.8f,
                    Mathf.Lerp(4.2f, 2.2f, eased));
                yield return null;
            }

            promptRect.anchoredPosition = PromptPosition;
            promptRect.localScale = Vector3.one;
            promptRect.localRotation = Quaternion.identity;
            promptGroup.alpha = 1f;
            gradientTextEffect?.SetSnakeMotion(7f, Time.unscaledTime * 2.35f, 2.2f);
            yield return new WaitForSecondsRealtime(0.12f);

            acceptingAction = true;
            ApplySettledStepPermissions(step);
            if (step == Step.Score)
                spawner?.StartTutorialGhostChallenge();
            else if (step == Step.StompScore)
                spawner?.StartTutorialSkeletonChallenge();
        }

        private IEnumerator ExitPrompt()
        {
            if (!promptVisible)
                yield break;

            Vector2 start = promptRect.anchoredPosition;
            Vector2 end = PromptPosition + new Vector2(-GetTravelDistance(), 285f);
            Vector2 control = PromptPosition + new Vector2(-GetTravelDistance() * 0.42f, -55f);
            Vector3 startScale = promptRect.localScale;
            Quaternion startRotation = promptRect.localRotation;
            const float duration = 1.02f;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t;
                Vector2 pathPosition = QuadraticBezier(start, control, end, eased);
                float travelWave = Mathf.Sin(t * Mathf.PI * 2.7f) * (1f - t * 0.4f) * 18f;
                promptRect.anchoredPosition = pathPosition + Vector2.up * travelWave;
                promptRect.localScale = Vector3.Lerp(startScale, Vector3.one * 0.9f, eased);
                promptRect.localRotation = Quaternion.Slerp(
                    startRotation,
                    Quaternion.Euler(0f, 0f, -7f),
                    eased);
                promptGroup.alpha = 1f - Mathf.SmoothStep(0.58f, 1f, t);
                gradientTextEffect?.SetSnakeMotion(
                    Mathf.Lerp(7f, 14f, eased),
                    Time.unscaledTime * 3.1f + t * 2.2f,
                    Mathf.Lerp(2.2f, 4.2f, eased));
                yield return null;
            }

            promptVisible = false;
            promptGroup.alpha = 0f;
        }

        private void ConfigurePrompt(Step step)
        {
            string message;
            switch (step)
            {
                case Step.Jump:
                    message = "点击屏幕完成第一段跳跃";
                    break;
                case Step.DoubleJump:
                    message = "还在空中时再次轻点\n完成二段跳";
                    break;
                case Step.Glide:
                    message = "在空中长按屏幕开始滑翔\n松开即可结束";
                    break;
                case Step.UseItem:
                    message = "点击右下角的道具图标\n主动使用一次道具";
                    break;
                case Step.DiscardItem:
                    message = "按住另一个道具拖出槽位\n松手将它丢弃";
                    break;
                case Step.Score:
                    message = "跳起来，让子弹命中悬浮幽灵\n击杀怪物即可得分";
                    break;
                case Step.StompScore:
                    message = "从上方踩中骷髅头顶\n击杀分 + 踩踏分，奖励翻倍";
                    break;
                default:
                    message = "接下来请尽可能多地消灭怪物来获得更高的分数吧，\n达到1000分后可开启下一关的挑战哦";
                    break;
            }

            instructionText.text = message;
        }

        private void ApplyTransitionPermissions(Step nextStep)
        {
            if (player != null)
            {
                player.SetTutorialActionPermissions(
                    nextStep != Step.Jump,
                    nextStep != Step.Jump && nextStep != Step.DoubleJump,
                    nextStep != Step.Jump && nextStep != Step.DoubleJump && nextStep != Step.Glide);
            }

            if (inventory == null)
                return;

            if (nextStep == Step.UseItem)
            {
                inventory.UseInteractionEnabled = false;
                inventory.DropInteractionEnabled = false;
                inventory.MinimumItemCountToPreserveFromUse = 1;
            }
            else if (nextStep == Step.DiscardItem)
            {
                inventory.UseInteractionEnabled = true;
                inventory.DropInteractionEnabled = false;
                inventory.MinimumItemCountToPreserveFromUse = 1;
            }
            else if (nextStep == Step.Score || nextStep == Step.StompScore || nextStep == Step.FinalMessage)
            {
                inventory.UseInteractionEnabled = true;
                inventory.DropInteractionEnabled = true;
                inventory.MinimumItemCountToPreserveFromUse = 0;
            }
        }

        private void ApplySettledStepPermissions(Step step)
        {
            if (player != null)
            {
                player.SetTutorialActionPermissions(
                    true,
                    step != Step.Jump,
                    step != Step.Jump && step != Step.DoubleJump);
            }

            if (inventory == null)
                return;

            if (step == Step.UseItem)
            {
                inventory.UseInteractionEnabled = true;
                inventory.DropInteractionEnabled = false;
                inventory.MinimumItemCountToPreserveFromUse = 1;
            }
            else if (step == Step.DiscardItem)
            {
                inventory.UseInteractionEnabled = true;
                inventory.DropInteractionEnabled = true;
                inventory.MinimumItemCountToPreserveFromUse = 1;
            }
        }

        private void EnsureTutorialItems()
        {
            if (inventory == null)
                return;

            SpellType[] candidates =
            {
                SpellType.MachineGun,
                SpellType.CornCannon,
                SpellType.WaveFlight,
                SpellType.TripleShot
            };
            for (int candidate = 0; candidate < candidates.Length && CountFilledSlots() < 2; candidate++)
                inventory.TryAddDetailed(candidates[candidate]);
        }

        private int CountFilledSlots()
        {
            int count = 0;
            for (int i = 0; i < SpellInventory.SlotCount; i++)
            {
                if (!inventory.GetSlot(i).IsEmpty)
                    count++;
            }
            return count;
        }

        private void PrepareCombatChallenge()
        {
            player?.GetComponent<WeaponController>()?.ResetWeaponState();
            Projectile[] projectiles = FindObjectsByType<Projectile>(FindObjectsSortMode.None);
            for (int i = 0; i < projectiles.Length; i++)
            {
                if (projectiles[i] == null)
                    continue;

                projectiles[i].gameObject.SetActive(false);
                Destroy(projectiles[i].gameObject);
            }
        }

        private void CompleteTutorial()
        {
            active = false;
            acceptingAction = false;
            promptVisible = false;
            transitionRoutine = null;
            Unsubscribe();
            spawner?.EndTutorialMode();
            manager?.SetTutorialActive(false);
            player?.SetTutorialInputEnabled(true);
            if (inventory != null)
            {
                inventory.InteractionEnabled = true;
                inventory.UseInteractionEnabled = true;
                inventory.DropInteractionEnabled = true;
                inventory.MinimumItemCountToPreserveFromUse = 0;
            }
            choiceRoot?.SetActive(false);
            root?.SetActive(false);
        }

        private void Unsubscribe()
        {
            if (player != null)
            {
                player.Jumped -= OnJumped;
                player.GlideStarted -= OnGlideStarted;
            }
            if (inventory != null)
            {
                inventory.SlotUsed -= OnSlotUsed;
                inventory.SlotDropped -= OnSlotDropped;
            }
            Enemy.Defeated -= OnEnemyDefeated;
        }

        private float GetTravelDistance()
        {
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            float width = canvasRect != null ? canvasRect.rect.width : 1280f;
            return Mathf.Max(900f, width * 0.75f + 360f);
        }

        private static Vector2 QuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
        {
            float inverse = 1f - t;
            return inverse * inverse * start + 2f * inverse * t * control + t * t * end;
        }

        private void OnDestroy()
        {
            StopTutorial();
        }

        private Text CreateChoiceText(
            Transform parent,
            string text,
            int size,
            Vector2 position,
            Vector2 dimensions,
            Color color)
        {
            Font font = instructionText != null ? instructionText.font : null;
            Text label = CreateText(parent, "ChoiceText", font, size, position, color);
            RectTransform rect = label.transform as RectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            label.text = text;
            return label;
        }

        private Button CreateChoiceButton(Transform parent, string label, Vector2 position, Color color)
        {
            GameObject buttonRoot = new GameObject(
                "TutorialChoice_" + (label == "√" ? "Confirm" : "Skip"),
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(UIButtonMotion));
            buttonRoot.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)buttonRoot.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(128f, 82f);

            Image image = buttonRoot.GetComponent<Image>();
            image.color = color;
            Button button = buttonRoot.GetComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colors = button.colors;
            colors.normalColor = color;
            colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(color, Color.black, 0.15f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;

            Text symbol = CreateChoiceText(
                buttonRoot.transform,
                label,
                48,
                Vector2.zero,
                new Vector2(120f, 78f),
                Color.white);
            symbol.fontStyle = FontStyle.Bold;
            Outline outline = symbol.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.03f, 0.02f, 0.07f, 0.82f);
            outline.effectDistance = new Vector2(2f, -2f);
            return button;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            Font font,
            int size,
            Vector2 position,
            Color color)
        {
            GameObject textRoot = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textRoot.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)textRoot.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.anchoredPosition = position;

            Text text = textRoot.GetComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 24;
            text.resizeTextMaxSize = size;
            text.lineSpacing = 1.05f;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
