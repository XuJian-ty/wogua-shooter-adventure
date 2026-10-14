using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace WoguaShooter.UI
{
    /// <summary>
    /// Single-scene flow transition. It uses unscaled time so returning from a
    /// paused or game-over state can still animate to completion.
    /// </summary>
    public class LoadingOverlayController : MonoBehaviour
    {
        private GameObject overlay;
        private CanvasGroup canvasGroup;
        private RectTransform fillRect;
        private Text titleText;
        private Text progressText;
        private bool initialized;
        private int displayedPercent = -1;

        public bool IsPlaying { get; private set; }

        public void Initialize(Canvas canvas, Font font)
        {
            if (initialized || canvas == null)
                return;

            initialized = true;
            overlay = new GameObject("LoadingOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            overlay.transform.SetParent(canvas.transform, false);
            Stretch((RectTransform)overlay.transform);

            Image backdrop = overlay.GetComponent<Image>();
            backdrop.color = new Color32(25, 28, 22, 247);
            backdrop.raycastTarget = true;
            canvasGroup = overlay.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            Text brand = CreateText(overlay.transform, "Brand", font, 28, new Vector2(0f, 82f), new Vector2(700f, 50f));
            brand.text = "倭瓜射手大冒险";
            brand.color = new Color32(250, 220, 130, 255);

            titleText = CreateText(overlay.transform, "LoadingTitle", font, 34, new Vector2(0f, 24f), new Vector2(760f, 56f));
            titleText.text = "正在加载";

            GameObject track = CreateImage(overlay.transform, "ProgressTrack", new Color32(72, 50, 30, 255));
            RectTransform trackRect = (RectTransform)track.transform;
            trackRect.anchorMin = trackRect.anchorMax = new Vector2(0.5f, 0.5f);
            trackRect.anchoredPosition = new Vector2(0f, -42f);
            trackRect.sizeDelta = new Vector2(590f, 24f);
            Outline trackOutline = track.AddComponent<Outline>();
            trackOutline.effectColor = new Color32(204, 161, 74, 230);
            trackOutline.effectDistance = new Vector2(2f, -2f);

            GameObject fill = CreateImage(track.transform, "ProgressFill", new Color32(225, 171, 64, 255));
            fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = new Vector2(3f, 0f);
            fillRect.sizeDelta = new Vector2(0f, -6f);

            progressText = CreateText(overlay.transform, "ProgressText", font, 20, new Vector2(0f, -88f), new Vector2(300f, 38f));
            progressText.color = new Color32(236, 230, 205, 255);
            SetProgress(0f);
            overlay.SetActive(false);
        }

        public IEnumerator Play(string title, Action switchAction)
        {
            if (!initialized || overlay == null)
            {
                switchAction?.Invoke();
                yield break;
            }

            IsPlaying = true;
            overlay.transform.SetAsLastSibling();
            overlay.SetActive(true);
            canvasGroup.alpha = 0f;
            titleText.text = title;
            SetProgress(0f);

            yield return FadeCanvas(0f, 1f, 0.16f);
            yield return AnimateProgress(0f, 0.72f, 0.48f);

            switchAction?.Invoke();
            overlay.transform.SetAsLastSibling();
            yield return null;

            yield return AnimateProgress(0.72f, 1f, 0.34f);
            yield return WaitUnscaled(0.12f);
            yield return FadeCanvas(1f, 0f, 0.18f);

            overlay.SetActive(false);
            canvasGroup.alpha = 1f;
            IsPlaying = false;
        }

        private IEnumerator AnimateProgress(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
                SetProgress(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t)));
                yield return null;
            }
            SetProgress(to);
        }

        private IEnumerator FadeCanvas(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration)));
                yield return null;
            }
            canvasGroup.alpha = to;
        }

        private static IEnumerator WaitUnscaled(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private void SetProgress(float value)
        {
            value = Mathf.Clamp01(value);
            if (fillRect != null)
            {
                fillRect.anchorMax = new Vector2(value, 1f);
                fillRect.sizeDelta = new Vector2(value > 0f ? -6f : 0f, -6f);
            }
            int percent = Mathf.RoundToInt(value * 100f);
            if (progressText != null && percent != displayedPercent)
            {
                displayedPercent = percent;
                progressText.text = $"{percent}%";
            }
        }

        private static GameObject CreateImage(Transform parent, string name, Color color)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.transform.SetParent(parent, false);
            Image image = root.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return root;
        }

        private static Text CreateText(Transform parent, string name, Font font, int size, Vector2 position, Vector2 dimensions)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            root.transform.SetParent(parent, false);
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = dimensions;
            Text text = root.GetComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
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
