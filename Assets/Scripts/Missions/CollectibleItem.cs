using Unity.FantasyKingdom;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CollectibleItem : MonoBehaviour
{
    public enum CollectibleType
    {
        Mission1Sigil,
        Mission1Fragment,
        Mission2EnergyCore,
        Mission2Fragment,
        Mission3Fragment
    }

    [Header("Setup")]
    [SerializeField] private CollectibleType collectibleType;
    [SerializeField] private string collectibleId;
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private GameObject objectToDisable;
    [SerializeField] private bool destroyOnCollect = true;

    [Header("Inventory Reward")]
    [SerializeField] private ItemSO inventoryItem;
    [SerializeField] private int inventoryAmount = 1;
    [SerializeField] private Inventory inventory;

    private bool _collected;

    private void Reset()
    {
        Collider triggerCollider = GetComponent<Collider>();
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (missionManager == null)
        {
            missionManager = MissionManager.Instance;
        }

        if (string.IsNullOrWhiteSpace(collectibleId))
        {
            collectibleId = gameObject.name;
        }

        if (objectToDisable == null)
        {
            objectToDisable = gameObject;
        }

        ResolveInventoryReference();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected || !other.CompareTag("Player"))
        {
            return;
        }

        if (missionManager == null)
        {
            missionManager = MissionManager.Instance;
        }

        if (missionManager == null || !missionManager.MissionStarted)
        {
            return;
        }

        bool handled = HandleCollect();
        if (!handled)
        {
            return;
        }

        _collected = true;
        GrantInventoryReward();

        if (destroyOnCollect)
        {
            Destroy(gameObject);
            return;
        }

        if (objectToDisable != null)
        {
            objectToDisable.SetActive(false);
        }
    }

    private bool HandleCollect()
    {
        switch (collectibleType)
        {
            case CollectibleType.Mission1Sigil:
                if (missionManager.CurrentMission != MissionManager.MissionState.Mission1)
                {
                    return false;
                }

                missionManager.RegisterMission1Sigil(collectibleId);
                return true;

            case CollectibleType.Mission1Fragment:
                if (missionManager.CurrentMission != MissionManager.MissionState.Mission1)
                {
                    return false;
                }

                missionManager.RegisterMission1FragmentCollected();
                return true;

            case CollectibleType.Mission2EnergyCore:
                if (missionManager.CurrentMission != MissionManager.MissionState.Mission2)
                {
                    return false;
                }

                missionManager.RegisterMission2EnergyCoreCollected();
                return true;

            case CollectibleType.Mission2Fragment:
                if (missionManager.CurrentMission != MissionManager.MissionState.Mission2)
                {
                    return false;
                }

                missionManager.RegisterMission2FragmentCollected();
                return true;

            case CollectibleType.Mission3Fragment:
                if (missionManager.CurrentMission != MissionManager.MissionState.Mission3)
                {
                    return false;
                }

                missionManager.RegisterMission3FragmentCollected();
                return true;

            default:
                return false;
        }
    }

    private void ResolveInventoryReference()
    {
        if (inventoryItem != null && inventory == null)
        {
            inventory = FindObjectOfType<Inventory>();
        }
    }

    private void GrantInventoryReward()
    {
        if (inventoryItem == null)
        {
            return;
        }

        if (inventory == null)
        {
            ResolveInventoryReference();
        }

        if (inventory == null)
        {
            Debug.LogWarning(
                $"Collectible '{name}' is configured to add '{inventoryItem.name}' to the inventory, but no Inventory was found in the scene.",
                this);
            return;
        }

        inventory.AddItem(inventoryItem, Mathf.Max(1, inventoryAmount));
    }
}
