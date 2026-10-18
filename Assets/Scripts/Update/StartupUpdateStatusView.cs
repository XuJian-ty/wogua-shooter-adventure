using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WoguaShooter.Update
{
    /// <summary>游戏启动时显示在画布底部的轻量更新状态提示。</summary>
    public sealed class StartupUpdateStatusView : MonoBehaviour
    {
        private static readonly Color NormalColor = new Color32(231, 238, 224, 255);
        private static readonly Color SuccessColor = new Color32(215, 240, 190, 255);
        private static readonly Color WarningColor = new Color32(255, 220, 145, 255);

        private CanvasGroup canvasGroup;
        private Text statusText;
        private Image progressFill;
        private float targetProgress;

        public static StartupUpdateStatusView Create(Canvas canvas, Font font)
        {
            if (canvas == null)
                return null;

            Transform existing = canvas.transform.Find("StartupUpdateStatus");
            if (existing != null && existing.TryGetComponent(out StartupUpdateStatusView existingView))
                return existingView;

            GameObject root = new GameObject(
                "StartupUpdateStatus",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(StartupUpdateStatusView));
            root.transform.SetParent(canvas.transform, false);

            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 24f);
            rootRect.sizeDelta = new Vector2(580f, 48f);

            StartupUpdateStatusView view = root.GetComponent<StartupUpdateStatusView>();
            view.canvasGroup = root.GetComponent<CanvasGroup>();
            view.canvasGroup.interactable = false;
            view.canvasGroup.blocksRaycasts = false;
            view.statusText = CreateStatusText(root.transform, font);
            view.progressFill = CreateProgressBar(root.transform);
            root.transform.SetAsLastSibling();
            root.SetActive(false);
            return view;
        }

        public void ShowConnecting()
        {
            Show("正在尝试连接更新服务器...", 0.32f, NormalColor);
        }

        public void ShowChecking()
        {
            Show("服务器连接成功，正在检测资源更新...", 0.74f, NormalColor);
        }

        public void ShowUpToDate()
        {
            Show("资源已是最新版本", 1f, SuccessColor);
        }

        public void ShowConnectionFailed()
        {
            Show("连接更新服务器失败，将以离线模式继续", 1f, WarningColor);
        }

        public void ShowInvalidResponse()
        {
            Show("更新信息读取失败，将以离线模式继续", 1f, WarningColor);
        }

        public void ShowUpdateFound()
        {
            Show("检测到新的资源版本", 1f, SuccessColor);
        }

        public IEnumerator HideAfter(float seconds)
        {
            if (seconds > 0f)
                yield return new WaitForSecondsRealtime(seconds);

            float elapsed = 0f;
            float startAlpha = canvasGroup != null ? canvasGroup.alpha : 1f;
            while (elapsed < 0.18f)
            {
                elapsed += Time.unscaledDeltaTime;
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / 0.18f);
                yield return null;
            }

            gameObject.SetActive(false);
        }

        public void HideImmediately()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            if (progressFill != null)
            {
                progressFill.fillAmount = Mathf.MoveTowards(
                    progressFill.fillAmount,
                    targetProgress,
                    Time.unscaledDeltaTime * 0.9f);
            }
        }

        private void Show(string message, float progress, Color textColor)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = textColor;
            }

            targetProgress = Mathf.Clamp01(progress);
        }

        private static Text CreateStatusText(Transform parent, Font font)
        {
            GameObject textObject = new GameObject(
                "StatusText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 30f);

            Text text = textObject.GetComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static Image CreateProgressBar(Transform parent)
        {
            GameObject trackObject = new GameObject(
                "ProgressTrack",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            trackObject.transform.SetParent(parent, false);

            RectTransform trackRect = trackObject.GetComponent<RectTransform>();
            trackRect.anchorMin = new Vector2(0.5f, 0f);
            trackRect.anchorMax = new Vector2(0.5f, 0f);
            trackRect.pivot = new Vector2(0.5f, 0f);
            trackRect.anchoredPosition = new Vector2(0f, 3f);
            trackRect.sizeDelta = new Vector2(520f, 5f);

            Image track = trackObject.GetComponent<Image>();
            track.color = new Color(0f, 0f, 0f, 0.42f);
            track.raycastTarget = false;

            GameObject fillObject = new GameObject(
                "ProgressFill",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            fillObject.transform.SetParent(trackObject.transform, false);

            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(1f, 1f);
            fillRect.offsetMax = new Vector2(-1f, -1f);

            Image fill = fillObject.GetComponent<Image>();
            fill.color = new Color32(231, 184, 73, 255);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 0.04f;
            fill.raycastTarget = false;
            return fill;
        }
    }
}
