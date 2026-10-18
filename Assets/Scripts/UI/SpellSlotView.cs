using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WoguaShooter.Config;
using WoguaShooter.Core;

namespace WoguaShooter.UI
{
    public class SpellSlotView : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private static readonly Color EmptyColor = new Color(0.12f, 0.06f, 0.025f, 0.56f);
        private static readonly Color FilledColor = new Color(0.48f, 0.27f, 0.105f, 0.86f);

        private SpellInventory inventory;
        private int index;
        private Image background;
        private Image iconImage;
        private Text nameText;
        private Text countText;
        private CanvasGroup canvasGroup;
        private RectTransform dragIconRect;
        private Canvas dragCanvas;
        private bool dragging;

        public void Initialize(SpellInventory owner, int slotIndex, Image slotBackground, Image icon, Text name, Text count)
        {
            inventory = owner;
            index = slotIndex;
            background = slotBackground;
            iconImage = icon;
            nameText = name;
            countText = count;
            canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            Refresh();
        }

        public void Refresh()
        {
            if (inventory == null)
                return;

            SpellInventory.Slot slot = inventory.GetSlot(index);
            bool filled = !slot.IsEmpty;
            if (background != null)
                background.color = filled ? FilledColor : EmptyColor;
            if (nameText != null)
                nameText.text = filled ? GetDisplayName(slot.type) : "";
            if (iconImage != null)
            {
                Sprite icon = filled ? SpellIconFactory.Get(slot.type) : null;
                iconImage.sprite = icon;
                iconImage.color = Color.white;
                // Image 没有 Sprite 时会绘制默认白色矩形，因此必须同时关闭组件。
                iconImage.enabled = icon != null;
            }
            if (countText != null)
                countText.text = filled ? $"×{slot.count}" : "";
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (dragging || eventData.button != PointerEventData.InputButton.Left)
                return;

            inventory?.UseSlot(index);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (inventory == null || !inventory.InteractionEnabled || !inventory.DropInteractionEnabled ||
                inventory.GetSlot(index).IsEmpty)
                return;

            dragging = true;
            CreateDragIcon();
            UpdateDragIconPosition(eventData);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0.45f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (dragging)
                UpdateDragIconPosition(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            bool droppedOutside = !RectTransformUtility.RectangleContainsScreenPoint(
                (RectTransform)transform,
                eventData.position,
                eventData.pressEventCamera);

            if (droppedOutside)
                inventory?.DropSlot(index);

            DestroyDragIcon();
            dragging = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
        }

        private void OnDisable()
        {
            DestroyDragIcon();
        }

        private void CreateDragIcon()
        {
            Sprite sprite = iconImage != null ? iconImage.sprite : null;
            dragCanvas = GetComponentInParent<Canvas>();
            if (sprite == null || dragCanvas == null)
                return;

            GameObject dragObject = new GameObject("DraggedSpellIcon", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            dragIconRect = dragObject.GetComponent<RectTransform>();
            dragIconRect.SetParent(dragCanvas.transform, false);
            dragIconRect.SetAsLastSibling();
            dragIconRect.sizeDelta = iconImage != null
                ? iconImage.rectTransform.rect.size
                : new Vector2(46f, 38f);

            Image dragImage = dragObject.GetComponent<Image>();
            dragImage.sprite = sprite;
            dragImage.preserveAspect = true;
            dragImage.raycastTarget = false;
            dragImage.color = new Color(1f, 1f, 1f, 0.92f);

            CanvasGroup dragGroup = dragObject.GetComponent<CanvasGroup>();
            dragGroup.blocksRaycasts = false;
            dragGroup.interactable = false;
        }

        private void UpdateDragIconPosition(PointerEventData eventData)
        {
            if (dragIconRect == null || dragCanvas == null)
                return;

            RectTransform canvasRect = dragCanvas.transform as RectTransform;
            Camera eventCamera = dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : eventData.pressEventCamera;
            if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, eventData.position, eventCamera, out Vector2 localPoint))
                dragIconRect.localPosition = localPoint;
        }

        private void DestroyDragIcon()
        {
            if (dragIconRect != null)
                Destroy(dragIconRect.gameObject);
            dragIconRect = null;
            dragCanvas = null;
        }

        private static string GetDisplayName(SpellType type)
        {
            switch (type)
            {
                case SpellType.MachineGun: return "豌豆机枪";
                case SpellType.CornCannon: return "玉米大炮";
                case SpellType.WaveFlight: return "波浪轨迹";
                case SpellType.TripleShot: return "三向齐发";
                case SpellType.Repeater: return "双弹连发";
                case SpellType.DualMuzzle: return "双枪联动";
                case SpellType.Magnet: return "磁铁";
                case SpellType.Shield: return "护盾";
                case SpellType.Growth: return "变大";
                default: return "空槽";
            }
        }
    }
}
