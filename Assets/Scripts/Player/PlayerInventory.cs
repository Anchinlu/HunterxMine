using System;
using UnityEngine;

namespace MineCraftUnity.Player
{
    public struct ItemStack
    {
        public string BlockId;
        public int Count;

        public bool IsEmpty => string.IsNullOrEmpty(BlockId) || Count <= 0;
    }

    public class PlayerInventory : MonoBehaviour
    {
        public event Action InventoryChanged;

        private ItemStack[] _hotbarItems = new ItemStack[9];
        private int _selectedHotbarSlot = 0;

        public ItemStack[] HotbarItems => _hotbarItems;

        public int SelectedHotbarSlot
        {
            get => _selectedHotbarSlot;
            set
            {
                _selectedHotbarSlot = Mathf.Clamp(value, 0, 8);
                NotifyChanged();
            }
        }

        public void SetHotbarItem(int slot, string blockId, int count)
        {
            if (slot >= 0 && slot < 9)
            {
                _hotbarItems[slot] = new ItemStack { BlockId = blockId, Count = count };
                NotifyChanged();
            }
        }

        private void NotifyChanged()
        {
            InventoryChanged?.Invoke();
        }
        
        private void Update()
        {
            // Simple logic to test hotbar selection scrolling using Input System
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                var scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0f)
                {
                    SelectedHotbarSlot = (_selectedHotbarSlot - 1 + 9) % 9;
                }
                else if (scroll < 0f)
                {
                    SelectedHotbarSlot = (_selectedHotbarSlot + 1) % 9;
                }
            }

            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) SelectedHotbarSlot = 0;
                if (kb.digit2Key.wasPressedThisFrame) SelectedHotbarSlot = 1;
                if (kb.digit3Key.wasPressedThisFrame) SelectedHotbarSlot = 2;
                if (kb.digit4Key.wasPressedThisFrame) SelectedHotbarSlot = 3;
                if (kb.digit5Key.wasPressedThisFrame) SelectedHotbarSlot = 4;
                if (kb.digit6Key.wasPressedThisFrame) SelectedHotbarSlot = 5;
                if (kb.digit7Key.wasPressedThisFrame) SelectedHotbarSlot = 6;
                if (kb.digit8Key.wasPressedThisFrame) SelectedHotbarSlot = 7;
                if (kb.digit9Key.wasPressedThisFrame) SelectedHotbarSlot = 8;
            }
        }
    }
}
