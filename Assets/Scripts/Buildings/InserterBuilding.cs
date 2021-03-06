using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// public class InserterBuilding : Building, IAccecptsItem, IHoldsItem {
public class InserterBuilding : Building, IHoldsItem {

    [Header("Inserter")]
    public float grabDur = 1;
    public float placeDur = 1;
    public Vector2Int fromBuildingLPos;
    public Vector2Int toBuildingLPos;

    public Transform grabber;
    [SerializeField, ReadOnly] DroppedItem heldItem;

    Timer processTimer;
    [SerializeField] Inventory heldInventory;
    [SerializeField, ReadOnly] Inventory fromInv = null;

    public Inventory FromInventory => heldInventory;
    bool isInProcess = false;


    private IHoldsItem GetHoldsItem(Vector2Int localPos) {
        Building building = WorldManager.Instance.GetTileAt(LocalRelPosToTilePos(localPos))?.building;
        if (building is IHoldsItem holdsItem) {
            return holdsItem;
        }
        return null;
    }
    private IAccecptsItem GetAcceptsItem(Vector2Int localPos) {
        Building building = WorldManager.Instance.GetTileAt(LocalRelPosToTilePos(localPos))?.building;
        if (building is IAccecptsItem accItem) {
            return accItem;
        }
        return null;
    }

    public IHoldsItem GetFromBuilding() {
        return GetHoldsItem(fromBuildingLPos);
    }
    public IAccecptsItem GetToBuilding() {
        return GetAcceptsItem(toBuildingLPos);
    }


    protected override void Awake() {
        base.Awake();
        processTimer = GetComponent<Timer>();
    }

    public override void OnPlaced() {
        base.OnPlaced();
        processTimer.onTimerComplete.AddListener(TimerEvent);
        heldInventory.OnInventoryUpdateEvent.AddListener(HeldInvUpdate);
        processTimer.StartTimer();
        audioSource.loop = false;
        UpdateFromInv();
    }
    public override void OnRemoved() {
        base.OnRemoved();
        processTimer.onTimerComplete.RemoveListener(TimerEvent);
        heldInventory.OnInventoryUpdateEvent.RemoveListener(HeldInvUpdate);
        processTimer.StopTimer();
    }
    public override void OnNeighborUpdated() {
        base.OnNeighborUpdated();
        UpdateFromInv();
        if (heldItem != null) {
            processTimer.ResumeTimer();
            // FinishMovingItem();
        }
    }
    void UpdateFromInv() {
        if (fromInv != null) {
            fromInv.OnInventoryUpdateEvent.RemoveListener(FromInvUpdate);
        }
        fromInv = GetFromBuilding()?.FromInventory;
        if (fromInv != null) {
            fromInv.OnInventoryUpdateEvent.AddListener(FromInvUpdate);
        }
    }
    void FromInvUpdate() {
        if (heldItem == null) {
            // processTimer.ResumeTimer();
            StartMovingItem();
        }
    }
    void HeldInvUpdate() {
        // remove dropped item if empty
        if (!heldInventory.HasAnyItems()) {
            // out item was taken
            if (heldItem != null) {
                Destroy(heldItem.gameObject);
                heldItem = null;
                // Debug.Log("clearing helditem " + heldItem);
            }
            animator.SetBool("Grabbed", false);
            audioSource.Stop();

            processTimer.duration = grabDur;
            processTimer.ResumeTimer();
        }
    }
    void TimerEvent() {
        if (heldItem != null) {
            FinishMovingItem();
        } else {
            StartMovingItem();
        }
    }
    void StartMovingItem() {
        if (heldItem != null) {
            return;
        }
        // Debug.Log("Startmoving h" + heldItem);
        // Debug.Log("try grab " + fromBuildingLPos + " " + LocalRelPosToTilePos(fromBuildingLPos));
        if (fromInv != null && fromInv.HasAnyItems() && !isInProcess) {
            // todo optional filter
            isInProcess = true;
            // Debug.Log($"taking '{fromInv}' from {fromInv.name}");
            Item item = fromInv.TakeFirstItem();
            // Debug.Log($"taking '{item}' from {fromInv.name}");
            heldItem = ItemManager.Instance.DropItem(item);
            heldItem.transform.parent = grabber;
            heldItem.transform.localPosition = Vector3.zero;
            heldItem.transform.localRotation = Quaternion.identity;
            // Debug.Log("setting helditem " + heldItem);
            // Debug.Log("Grabbed");
            processTimer.duration = placeDur;
            processTimer.StartTimer();
            audioSource.Play();
            // processTimer.ResumeTimer();
            animator.SetBool("Grabbed", true);
            isInProcess = false;
        }
        // FinishMovingItem();
    }
    void FinishMovingItem() {
        if (heldItem == null) return;
        IAccecptsItem tobuilding = GetToBuilding();
        if (tobuilding != null && !isInProcess) {
            if (tobuilding.ToInventory.HasSpaceFor(heldItem.item.itemType)) {
                isInProcess = true;
                // Debug.Log("Placed");
                tobuilding.ToInventory.AddItem(heldItem.item.itemType);
                if (heldInventory.HasItemAtLeast(heldItem.item.itemType, 1)) {
                    heldInventory.TakeFirstItem();
                }
                if (heldItem == null) {
                    Debug.LogWarning("held item changed!");
                } else {
                    Destroy(heldItem.gameObject);
                }
                heldItem = null;
                // Debug.Log("clearing helditem " + heldItem);
                processTimer.duration = grabDur;
                processTimer.StartTimer();
                animator.SetBool("Grabbed", false);
                audioSource.Stop();
                // animator.SetTrigger("Insert");
                isInProcess = false;
                return;
            }
        }
        // no building to put in, hold in inventory
        if (!heldInventory.HasAnyItems() && !isInProcess) {
            isInProcess = true;
            heldInventory.AddItem(heldItem.item.itemType);
            isInProcess = false;
        }
    }


    public override bool HasBuildingScreen => false;
}