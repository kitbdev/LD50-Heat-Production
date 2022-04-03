using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrafterBuilding : Building, IHoldsItem, IAccecptsItem {

    [Header("Crafter")]
    public ItemRecipe selectedRecipe;
    // public ItemType productionItem;
    public int productionRate;

    Timer processTimer;
    [SerializeField] Inventory inputInventory;
    [SerializeField] Inventory outputInventory;

    public Inventory FromInventory => outputInventory;
    public Inventory ToInventory => inputInventory;

    protected override void Awake() {
        processTimer = GetComponent<Timer>();
    }
    public override void OnPlaced() {
        base.OnPlaced();
        processTimer.onTimerComplete.AddListener(CraftItem);
        inputInventory.OnInventoryUpdateEvent.AddListener(InvUpdate);
        outputInventory.OnInventoryUpdateEvent.AddListener(InvUpdate);
        processTimer.StartTimer();
    }
    public override void OnRemoved() {
        processTimer.onTimerComplete.RemoveListener(CraftItem);
        inputInventory.OnInventoryUpdateEvent.RemoveListener(InvUpdate);
        outputInventory.OnInventoryUpdateEvent.RemoveListener(InvUpdate);
        processTimer.StopTimer();
        base.OnRemoved();
    }
    void InvUpdate() {
        if (!processTimer.IsRunning) {
            if (inputInventory.HasItems(selectedRecipe.requiredItems)) {
                if (outputInventory.HasSpaceFor(selectedRecipe.producedItems)) {
                    CraftItem();
                    processTimer.ResumeTimer();
                }
            }
        }
    }
    void CraftItem() {
        Debug.Log("crafting!");
        if (inputInventory.HasItems(selectedRecipe.requiredItems)) {// todo seperate types? enforce them
            if (outputInventory.HasSpaceFor(selectedRecipe.producedItems)) {
                inputInventory.TakeItems(selectedRecipe.requiredItems);
                outputInventory.AddItems(selectedRecipe.producedItems);
            } else {
                // is full
                processTimer.PauseTimer();
            }
        } else {
            // not enough items
            processTimer.StopTimer();
        }
        if (!inputInventory.HasItems(selectedRecipe.requiredItems)) {
            // not enough items next time
            processTimer.StopTimer();
        }
    }

}