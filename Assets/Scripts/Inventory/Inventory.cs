using System.Collections.Generic;
using UnityEngine;

namespace Unity.FantasyKingdom
{
    public class Inventory : MonoBehaviour
    {
        public ItemSO woodItem;
        public ItemSO axeItem;
        public GameObject hotbarObj;
        public GameObject inventorySlotParent;
        private List<Slot> inventorySlots = new List<Slot>();
        private List<Slot> hotbarSlots = new List<Slot>();
        private List<Slot> allSlots = new List<Slot>();

        private void Awake()
        {
            CacheSlots();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.X))
            {
                AddItem(woodItem, 3);

            }
            else if (Input.GetKeyDown(KeyCode.G))
            {
                AddItem(axeItem, 1);
            }
        }

        public void AddItem(ItemSO itemToAdd, int amount)
        {
            if (itemToAdd == null)
            {
                Debug.LogError("Cannot add a null item to the inventory.");
                return;
            }

            if (amount <= 0)
            {
                return;
            }

            if (itemToAdd.maxStackSize <= 0)
            {
                Debug.LogError($"Item '{itemToAdd.name}' has an invalid max stack size: {itemToAdd.maxStackSize}.");
                return;
            }

            if (allSlots.Count == 0)
            {
                CacheSlots();
            }

            int remaining = amount;
            foreach (Slot slot in allSlots)
            {
                if (slot.HasItem() && slot.GetItem() == itemToAdd)
                {
                    int currentAmount = slot.GetAmount();
                    int maxStack = itemToAdd.maxStackSize;
                    if (currentAmount < maxStack)
                    {
                        int spaceLeft = maxStack - currentAmount;
                        int amountToAdd = Mathf.Min(spaceLeft, remaining);
                        slot.SetItem(itemToAdd, currentAmount + amountToAdd);
                        remaining -= amountToAdd;
                        if (remaining <= 0)
                            return;
                    }
                }
            }

            foreach (Slot slot in allSlots)
            {
                if (!slot.HasItem())
                {
                    int amountToPlace = Mathf.Min(itemToAdd.maxStackSize, remaining);
                    slot.SetItem(itemToAdd, amountToPlace);
                    remaining -= amountToPlace;
                    if (remaining <= 0)
                        return;
                }
            }

            if (remaining > 0)
            {
                Debug.Log("Inventory is full, could not add " + remaining + " of " + itemToAdd.name);
            }
        }

        private void CacheSlots()
        {
            inventorySlots.Clear();
            hotbarSlots.Clear();
            allSlots.Clear();

            if (inventorySlotParent == null || hotbarObj == null)
            {
                Debug.LogError("Inventory references are missing. Assign inventorySlotParent and hotbarObj in the inspector.");
                return;
            }

            inventorySlots.AddRange(inventorySlotParent.GetComponentsInChildren<Slot>());
            hotbarSlots.AddRange(hotbarObj.GetComponentsInChildren<Slot>());

            allSlots.AddRange(inventorySlots);
            allSlots.AddRange(hotbarSlots);
        }
    }
}
