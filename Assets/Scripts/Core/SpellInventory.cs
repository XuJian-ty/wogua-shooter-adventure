using System;
using UnityEngine;
using WoguaShooter.Config;

namespace WoguaShooter.Core
{
    /// <summary>
    /// Runtime inventory for the five spell slots shown in the HUD.
    /// Items of the same type share one slot and stack up to three. A stack is
    /// consumed one item at a time when the player activates its slot.
    /// </summary>
    public class SpellInventory : MonoBehaviour
    {
        public const int SlotCount = 5;
        public const int MaxStack = 3;

        public enum AddResult
        {
            Added,
            StackFull,
            NoEmptySlot,
            Invalid
        }

        [Serializable]
        public struct Slot
        {
            public SpellType type;
            [Tooltip("同类道具共用一个槽位，最多堆叠三个。")]
            [Range(0, MaxStack)] public int count;

            public bool IsEmpty => type == SpellType.None || count <= 0;
        }

        public static SpellInventory Instance { get; private set; }
        public bool InteractionEnabled { get; set; } = true;
        public bool UseInteractionEnabled { get; set; } = true;
        public bool DropInteractionEnabled { get; set; } = true;
        public int MinimumItemCountToPreserveFromUse { get; set; }

        public event Action Changed;
        public event Action<int, SpellType> SlotUsed;
        public event Action<int, SpellType> SlotDropped;

        private readonly Slot[] slots = new Slot[SlotCount];
        private WeaponController weaponController;
        private PlayerController playerController;
        private CollectibleWeapon pendingPickup;

        public static SpellInventory GetOrCreate(WeaponController weapon)
        {
            if (weapon == null)
                return null;

            SpellInventory inventory = weapon.GetComponent<SpellInventory>();
            if (inventory == null)
                inventory = weapon.gameObject.AddComponent<SpellInventory>();

            inventory.Bind(weapon);
            return inventory;
        }

        public Slot GetSlot(int index)
        {
            return index >= 0 && index < slots.Length ? slots[index] : default;
        }

        public bool TryAdd(SpellType type)
        {
            return TryAddDetailed(type) == AddResult.Added;
        }

        public AddResult TryAddDetailed(SpellType type)
        {
            if (type == SpellType.None)
                return AddResult.Invalid;

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].type != type)
                    continue;

                if (slots[i].count >= MaxStack)
                    return AddResult.StackFull;

                slots[i].count++;
                Changed?.Invoke();
                return AddResult.Added;
            }

            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty)
                    continue;

                slots[i] = new Slot { type = type, count = 1 };
                Changed?.Invoke();
                return AddResult.Added;
            }

            return AddResult.NoEmptySlot;
        }

        public bool UseSlot(int index)
        {
            if (!InteractionEnabled || !UseInteractionEnabled ||
                GameManager.Instance == null || GameManager.Instance.State != GameState.Playing)
                return false;

            if (index < 0 || index >= slots.Length || slots[index].IsEmpty)
                return false;

            if (WouldUseBelowMinimum())
                return false;

            SpellType type = slots[index].type;
            if (!Apply(type))
                return false;

            slots[index].count--;
            if (slots[index].count <= 0)
                slots[index] = default;

            Changed?.Invoke();
            SlotUsed?.Invoke(index, type);
            return true;
        }

        public void DropSlot(int index)
        {
            if (!InteractionEnabled || !DropInteractionEnabled ||
                index < 0 || index >= slots.Length || slots[index].IsEmpty)
                return;

            SpellType type = slots[index].type;
            slots[index] = default;
            Changed?.Invoke();
            SlotDropped?.Invoke(index, type);
        }

        public void ResetInventory()
        {
            InteractionEnabled = true;
            UseInteractionEnabled = true;
            DropInteractionEnabled = true;
            MinimumItemCountToPreserveFromUse = 0;
            if (pendingPickup != null)
                pendingPickup.CancelPendingPickup();

            for (int i = 0; i < slots.Length; i++)
                slots[i] = default;

            weaponController?.ResetWeaponState();
            playerController?.ResetSpellState();
            Changed?.Invoke();
        }

        internal void RegisterPendingPickup(CollectibleWeapon pickup)
        {
            if (pickup == null || pendingPickup == pickup)
                return;

            if (pendingPickup != null)
                pendingPickup.CancelPendingPickup();

            pendingPickup = pickup;
        }

        internal void ClearPendingPickup(CollectibleWeapon pickup)
        {
            if (pendingPickup == pickup)
                pendingPickup = null;
        }

        /// <summary>
        /// 为新的一局随机准备五种不同的道具，每种各占一个槽位。
        /// </summary>
        public void FillRandomDistinct()
        {
            SpellType[] candidates =
            {
                SpellType.MachineGun,
                SpellType.CornCannon,
                SpellType.WaveFlight,
                SpellType.TripleShot,
                SpellType.Repeater,
                SpellType.DualMuzzle
            };

            // Fisher-Yates 的前五步足够从六种道具中抽出五种且不重复。
            for (int i = 0; i < SlotCount; i++)
            {
                int swapIndex = UnityEngine.Random.Range(i, candidates.Length);
                SpellType selected = candidates[i];
                candidates[i] = candidates[swapIndex];
                candidates[swapIndex] = selected;
                slots[i] = new Slot { type = candidates[i], count = 1 };
            }

            Changed?.Invoke();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            weaponController = GetComponent<WeaponController>();
            playerController = GetComponent<PlayerController>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Bind(WeaponController weapon)
        {
            weaponController = weapon;
            playerController = weapon.GetComponent<PlayerController>();
        }

        private bool Apply(SpellType type)
        {
            switch (type)
            {
                case SpellType.MachineGun:
                case SpellType.CornCannon:
                    weaponController?.ActivateSpell(type);
                    return weaponController != null;
                case SpellType.WaveFlight:
                    weaponController?.ActivateSpell(type);
                    return weaponController != null;
                case SpellType.DualMuzzle:
                case SpellType.TripleShot:
                case SpellType.Repeater:
                    weaponController?.ActivateSpell(type);
                    return weaponController != null;
                default:
                    return false;
            }
        }

        private bool WouldUseBelowMinimum()
        {
            if (MinimumItemCountToPreserveFromUse <= 0)
                return false;

            return CountItemCount() - 1 < MinimumItemCountToPreserveFromUse;
        }

        private int CountItemCount()
        {
            int count = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (!slots[i].IsEmpty)
                    count += slots[i].count;
            }

            return count;
        }
    }
}
