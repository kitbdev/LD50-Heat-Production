using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
[DefaultExecutionOrder(-1)]
public class Inventory : MonoBehaviour {

    [System.Serializable]
    public class ItemSlot {
        public ItemStack itemStack;
        public override string ToString() {
            return itemStack.ToString();
        }
    }

    public int numSlots = 1;
    public ItemSlot[] itemSlots;

    // public UnityEvent OnItemAddedEvent;
    // public UnityEvent OnItemRemovedEvent;
    public UnityEvent OnInventoryUpdateEvent;

    private void Awake() {
        Init(numSlots);
    }

    void Init(int numSlots) {
        itemSlots = new ItemSlot[numSlots];
        for (int i = 0; i < itemSlots.Length; i++) {
            itemSlots[i] = new ItemSlot();
            ItemSlot itemslot = itemSlots[i];
            itemslot.itemStack = new ItemStack();
            itemslot.itemStack.itemType = null;
            itemslot.itemStack.count = 0; ;
        }
        // Debug.Log($"Init {name} {numSlots}", gameObject);
    }
    IEnumerable<ItemSlot> GetItemStacksOfTypes(params ItemType[] matchingItemTypes) =>
        itemSlots.Where(sl => matchingItemTypes.Contains(sl.itemStack.itemType));
    IEnumerable<ItemSlot> GetEmptyItemSlots() =>
        itemSlots.Where(sl => sl.itemStack.IsEmpty);

    private ItemSlot GetFirstEmptySlot() => itemSlots.FirstOrDefault(sl => sl.itemStack.IsEmpty);
    private ItemSlot GetFirstNotEmptySlot() => itemSlots.FirstOrDefault(sl => !sl.itemStack.IsEmpty);
    private ItemSlot GetFirstFilledSlot() => itemSlots.FirstOrDefault(sl => sl.itemStack.IsFull);

    private ItemSlot GetFirstNotEmptySlotOfType(params ItemType[] matchingItemTypes) => itemSlots.FirstOrDefault(sl =>
        matchingItemTypes.Contains(sl.itemStack.itemType)
        && !sl.itemStack.IsEmpty);
    private ItemSlot GetFirstNotEmptyOrFullSlotOfType(params ItemType[] matchingItemTypes) => itemSlots.FirstOrDefault(sl =>
        matchingItemTypes.Contains(sl.itemStack.itemType)
        && !sl.itemStack.IsEmpty && !sl.itemStack.IsFull);


    public bool HasAnyItems() => itemSlots.Any(sl => sl.itemStack.HasItem && sl.itemStack.count > 0);
    public bool HasAnyItemsOfType(params ItemType[] matchingItemTypes) =>
        itemSlots.Any(sl => matchingItemTypes.Contains(sl.itemStack.itemType) && sl.itemStack.count > 0);

    public bool HasSpaceFor(ItemType itemType) => HasSpaceFor(itemType, 1);
    // itemSlots.Any(sl => sl.itemStack.IsEmpty || (sl.itemStack.item.itemType == itemType && !sl.itemStack.IsFull));
    public bool HasSpaceFor(ItemType itemType, int count) {
        int maxSpace = GetAvailableSpaceFor(itemType);
        return maxSpace >= count;
    }
    public bool HasItemAtLeast(ItemType itemType, int count) {
        int numItems = GetNumItems(itemType);
        return numItems >= count;
    }

    public int GetNumItems() {
        return itemSlots.Sum(sl => sl.itemStack.count);
    }
    public int GetNumItems(ItemType itemType) {
        return GetItemStacksOfTypes(itemType).Sum(sl => sl.itemStack.count);
    }

    public int GetAvailableSpaceFor(ItemType itemType) {
        // get all empty and not full matching slots
        IEnumerable<ItemSlot> emptySlots = GetEmptyItemSlots();
        IEnumerable<ItemSlot> nonFullMatchingSlots = GetItemStacksOfTypes(itemType);
        // Debug.Log("as " + itemSlots.ToStringFull());
        int maxSpace = emptySlots.Count() * itemType.itemMaxStack;
        // Debug.Log("es " + maxSpace);
        maxSpace += nonFullMatchingSlots.Sum(slot => slot.itemStack.RemainingSpace);
        // Debug.Log("ts " + maxSpace+" "+nonFullMatchingSlots.Count());
        return maxSpace;
    }
    public bool HasSpaceFor(params ItemStack[] itemStacks) {
        foreach (var itemStack in itemStacks) {
            if (!HasSpaceFor(itemStack.itemType, itemStack.count)) {
                return false;
            }
        }
        return true;
    }

    public bool HasItems(params ItemStack[] itemStacks) {
        foreach (var itemStack in itemStacks) {
            if (!HasItemAtLeast(itemStack.itemType, itemStack.count)) {
                return false;
            }
        }
        return true;
    }
    public void AddItems(params ItemStack[] itemStacks) => AddItems((IEnumerable<ItemStack>)itemStacks);
    public void AddItems(IEnumerable<ItemStack> itemStacks) {
        foreach (var itemStack in itemStacks) {
            AddItemNoEvent(itemStack.itemType, itemStack.count);
        }
        OnInventoryUpdateEvent?.Invoke();
    }
    public void AddItem(ItemType itemType, int count = 1) {
        AddItemNoEvent(itemType, count);
        OnInventoryUpdateEvent?.Invoke();
    }
    void AddItemNoEvent(ItemType itemType, int count) {
        // probably a better way of doing this...
        for (int i = 0; i < count; i++) {
            AddItemNoEvent(itemType);
        }
    }
    void AddItemNoEvent(ItemType itemType) {
        ItemSlot itemSlot = GetFirstNotEmptyOrFullSlotOfType(itemType);
        if (itemSlot == null) {
            itemSlot = GetFirstEmptySlot();
            itemSlot.itemStack.itemType = itemType;
            if (itemSlot == null) {
                Debug.LogWarning("Cant add item, inventory full!");
                return;
            }
        }
        itemSlot.itemStack.count++;
    }

    public Item PeekFirstItem() {
        ItemSlot itemSlot = GetFirstNotEmptySlot();
        if (itemSlot == null) return null;
        return new Item(itemSlot.itemStack.itemType);
    }
    public IEnumerable<Item> TakeItems(params ItemStack[] itemStacks) {
        List<Item> items = new List<Item>();
        foreach (var itemStack in itemStacks) {
            items.AddRange(TakeFirstItemOfTypeNoNotify(itemStack.itemType, itemStack.count));
        }
        OnInventoryUpdateEvent?.Invoke();
        return items;
    }
    public IEnumerable<Item> TakeFirstItemOfType(ItemType type, int count) {
        IEnumerable<Item> enumerable = TakeFirstItemOfTypeNoNotify(type, count);
        OnInventoryUpdateEvent?.Invoke();
        return enumerable;
    }
    public IEnumerable<Item> TakeFirstItemOfTypeNoNotify(ItemType type, int count) {
        // this makes sure we dont overflow an item
        List<Item> items = new List<Item>();
        ItemSlot itemSlot = GetFirstNotEmptySlotOfType(type);
        if (itemSlot == null) {
            Debug.LogWarning("Failed to take item " + type);
            return null;
        }
        for (int i = 0; i < count; i++) {
            itemSlot.itemStack.count--;
            items.Add(new Item(type));
            if (itemSlot.itemStack.IsEmpty) {
                itemSlot.itemStack.itemType = null;
                itemSlot = GetFirstNotEmptySlotOfType(type);
            }
        }
        return items;
    }
    public Item TakeFirstItem() {
        ItemSlot itemSlot = GetFirstNotEmptySlot();
        if (itemSlot == null) {
            // empty
            Debug.LogWarning("Cant take no not empty slot!");
            return null;
        }
        itemSlot.itemStack.count--;
        ItemType itemType = itemSlot.itemStack.itemType;
        if (itemSlot.itemStack.count <= 0) {
            itemSlot.itemStack.itemType = null;
        }
        OnInventoryUpdateEvent?.Invoke();
        return new Item(itemType);
    }
    public void TransferItemsIfCan(Inventory to, params ItemStack[] itemStacks) {
        // this inventory needs to have all the from items
        if (!HasItems(itemStacks)) return;
        if (to.HasSpaceFor(itemStacks)) {
            TakeItems(itemStacks.Select(st => st.Copy()).ToArray());
            to.AddItems(itemStacks.Select(st => st.Copy()).ToArray());
        } else {
            // otherwise transfer as many as we can
            foreach (var itemStack in itemStacks) {
                if (to.HasSpaceFor(itemStack)) {
                    TakeItems(itemStack);
                    to.AddItems(itemStack);
                } else {
                    for (int i = 0; i < itemStack.count; i++) {
                        if (to.HasSpaceFor(itemStack.itemType)) {
                            TakeFirstItemOfTypeNoNotify(itemStack.itemType, 1);
                            to.AddItemNoEvent(itemStack.itemType);
                        } else {
                            break;
                        }
                    }
                    OnInventoryUpdateEvent?.Invoke();
                    to.OnInventoryUpdateEvent?.Invoke();
                }
            }
        }
    }
    public void Sort() {
        // maximize maxed stacks
        IEnumerable<ItemType> allItemTypes = itemSlots.Where(sl => sl.itemStack.HasItem).Select(sl => sl.itemStack.itemType).Distinct();
        foreach (var itemType in allItemTypes) {
            // ignore full ones
            IEnumerable<ItemSlot> slots = GetItemStacksOfTypes(itemType).Where(sl => !sl.itemStack.IsFull);
            if (slots.Count() < 1) continue;
            // take from last and add to first, until only one(or zero) is not full
            while (slots.Count() > 1) {
                slots.Last().itemStack.count--;
                slots.First().itemStack.count++;
                if (slots.Last().itemStack.IsEmpty) {
                    slots.Last().itemStack.itemType = null;
                    slots = slots.SkipLast(1);
                }
                if (slots.First().itemStack.IsFull) {
                    // remove it
                    slots = slots.Skip(1);
                }
            }
        }

        // order
        System.Array.Sort(itemSlots, (a, b) => {
            if (!a.itemStack.HasItem && !b.itemStack.HasItem) return 0;
            if (a.itemStack.HasItem && !b.itemStack.HasItem) return -1;
            if (!a.itemStack.HasItem && b.itemStack.HasItem) return 1;
            // higher first
            return -a.itemStack.itemType.sortOrder + b.itemStack.itemType.sortOrder;
        });
        OnInventoryUpdateEvent?.Invoke();
    }


    public override string ToString() {
        return "Inventory " + itemSlots.Aggregate("", (s, i) => s += i.ToString());
    }
}