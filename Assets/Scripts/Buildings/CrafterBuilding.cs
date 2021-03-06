using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class CrafterBuilding : Building, IHoldsItem, IAccecptsItem {

    [Header("Crafter")]
    [SerializeField] ItemRecipe[] availableRecipes = new ItemRecipe[0];
    public ItemRecipe selectedRecipe;
    [SerializeField] bool autoChooseRecipe = true;
    // public ItemType productionItem;
    public int productionRate;

    Timer processTimer;
    [SerializeField] Inventory inputInventory;
    [SerializeField] Inventory outputInventory;

    public Inventory FromInventory => outputInventory;
    public Inventory ToInventory => inputInventory;

    protected override void Awake() {
        base.Awake();
        processTimer = GetComponent<Timer>();
    }
    public override void OnPlaced() {
        base.OnPlaced();
        processTimer.onTimerComplete.AddListener(CraftItem);
        processTimer.autoRestart = false;
        inputInventory.OnInventoryUpdateEvent.AddListener(InvUpdate);
        outputInventory.OnInventoryUpdateEvent.AddListener(InvUpdate);
        processTimer.onTimerUpdate.AddListener(UpdateProgressBar);
        processTimer.StartTimer();
    }
    public override void OnRemoved() {
        processTimer.onTimerComplete.RemoveListener(CraftItem);
        inputInventory.OnInventoryUpdateEvent.RemoveListener(InvUpdate);
        outputInventory.OnInventoryUpdateEvent.RemoveListener(InvUpdate);
        processTimer.onTimerUpdate.RemoveListener(UpdateProgressBar);
        processTimer.StopTimer();
        base.OnRemoved();
    }
    void InvUpdate() {
        if (!processTimer.IsRunning) {
            if (inputInventory.HasItems(selectedRecipe.requiredItems)) {
                if (outputInventory.HasSpaceFor(selectedRecipe.producedItems)) {
                    // CraftItem();
                    processTimer.ResumeTimer();
                }
            }
        }
    }
    void CraftItem() {
        // Debug.Log("crafting!");
        if (selectedRecipe == null) {
            if (autoChooseRecipe) {
                // choose based on input item type and available recipes
                Item peekitem = inputInventory.PeekFirstItem();
                selectedRecipe = availableRecipes.FirstOrDefault(r => r.requiredItems.Any(i => i.itemType == peekitem.itemType));
            }
        }
        if (selectedRecipe == null) {
            return;
        }
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

    public override bool HasBuildingScreen => true;
    public override Inventory GetFirstInv() {
        return inputInventory;
    }
    public override Inventory GetSecondInv() {
        // Debug.Log("smelter inv 2" + outputInventory);
        return outputInventory;
    }
    public override bool CanTakeFromFirst => true;
    public override bool CanPutInFirst => true;
    public override bool CanTakeFromSecond => true;
    public override bool CanPutInSecond => false;

    UnityEvent<float> sliderEvent = new UnityEvent<float>();
    public void UpdateProgressBar(float v) => sliderEvent?.Invoke(v);
    public override UnityEvent<float> sliderUpdateAction => sliderEvent;

}