using UnityEngine;
using UnityEngine.EventSystems;

namespace WoguaShooter.UI
{
    /// <summary>
    /// 通用按钮缓动：悬停轻微抬起，按下时缩小，松开后回弹。
    /// </summary>
    public class UIButtonMotion : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float hoverScale = 1.035f;
        [SerializeField] private float pressedScale = 0.96f;
        [SerializeField] private float animationSpeed = 18f;

        private Vector3 targetScale = Vector3.one;
        private bool pointerInside;

        private void OnEnable()
        {
            transform.localScale = Vector3.one;
            targetScale = Vector3.one;
            pointerInside = false;
        }

        private void Update()
        {
            float blend = 1f - Mathf.Exp(-animationSpeed * Time.unscaledDeltaTime);
            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, blend);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            pointerInside = true;
            targetScale = Vector3.one * hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            targetScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetScale = Vector3.one * pressedScale;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            targetScale = pointerInside
                ? Vector3.one * hoverScale
                : Vector3.one;
        }
    }
}
